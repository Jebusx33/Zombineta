using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zombineta.Fx;
using Zombineta.Player;

namespace Zombineta.Juego.EditorTools
{
    /// <summary>
    /// Arma los efectos del juego por capas, con el Particle System (la Web no tiene compute
    /// shaders para el VFX Graph): cada efecto combina destello, nucleo, humo, chispas, gotas o
    /// escombros, cada uno con su forma, color y tiempo, y los sub-emisores encadenan efectos
    /// (la gota que toca el piso deja su salpicadura). El humo y el fuego usan atlas con varias
    /// variantes, elegidas al azar por particula, para que ninguna bocanada se repita.
    ///
    /// Reescribe los prefabs de Prefabs/ (conservando el sistema raiz, que es lo que las escenas
    /// referencian), crea el fogonazo y el humo de la moto, y los engancha en las escenas de nivel.
    /// Medidas en unidades de mundo: la camara ve 12 de alto y un zombie mide ~1,3.
    /// </summary>
    public static class FxBuilder
    {
        const string PrefabsDir = "Assets/_Zombineta/Prefabs/";
        const string GenDir = "Assets/_Zombineta/Art/Fx/Generado/";
        const string KenneyDir = "Assets/_Zombineta/Art/Fx/ParticlePack/PNG (Transparent)/";

        static readonly string[] Escenas =
        {
            "Assets/_Zombineta/Scenes/Level_01.unity",
            "Assets/_Zombineta/Scenes/Level_01_Auxilio.unity",
            "Assets/_Zombineta/Scenes/Level_01_Auxilio_v2.unity",
            "Assets/_Zombineta/Scenes/Level_02.unity",
            "Assets/_Zombineta/Scenes/Tutorial.unity",
            "Assets/_Zombineta/Scenes/Templates/NivelBase.unity",
        };

        // FxManager ubica los efectos a esta altura sobre el piso del carril (effectHeight) y el
        // disparo a la de la mano (tracerHeight): el "piso" de cada prefab queda a esa distancia.
        const float AlturaCuerpo = 0.5f;
        const float AlturaArma = 0.55f;

        // Colores de la paleta: sangre oscura (de noche el rojo saturado se ve plastico).
        static readonly Color Sangre = new Color(0.42f, 0.02f, 0.04f, 1f);
        static readonly Color SangreNiebla = new Color(0.5f, 0.05f, 0.07f, 0.55f);
        static readonly Color Fuego = new Color(1f, 0.62f, 0.2f, 1f);

        static Material matHumo, matFuego, matDestello, matChispa, matGota, matAnillo, matEscombro, matFogonazo, matCasquillo, matTraza;

        [MenuItem("Zombineta/Fx/Reconstruir efectos")]
        static void Rebuild()
        {
            if (!EditorUtility.DisplayDialog("Efectos",
                    "Reescribe los prefabs de efectos y los engancha en Level_01, Level_02, Tutorial y NivelBase " +
                    "(guarda esas escenas). Conviene guardar lo abierto antes.",
                    "Reconstruir", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Build();
        }

        public static void Build()
        {
            CrearMateriales();

            Reescribir("FxImpacto", ArmarImpacto);
            Reescribir("FxMuerte", ArmarMuerte);
            Reescribir("FxAtropello", ArmarAtropello);
            Reescribir("FxExplosion", ArmarExplosion);
            var disparo = Crear("FxDisparo", ArmarDisparo);
            var humo = Crear("FxHumoMoto", ArmarHumoMoto);

            foreach (var escena in Escenas)
                Enganchar(escena, disparo, humo);

            AssetDatabase.SaveAssets();
            Debug.Log("Efectos reconstruidos y enganchados en " + Escenas.Length + " escenas.");
        }

        // --- Efectos -------------------------------------------------------------------

        /// <summary>Balazo que no mata: destello, chorro de gotas en la direccion de la bala y niebla.</summary>
        static void ArmarImpacto(ParticleSystem raiz)
        {
            var piso = Piso(raiz.transform, AlturaCuerpo);

            Destello(raiz, matDestello, 0.55f, 0.06f, new Color(1f, 0.92f, 0.75f, 1f), 3);

            var gotas = Gotas(Capa(raiz.transform, "Gotas", matGota, 1), piso, 16, 22, 3f, 6.5f, 0.035f, 0.08f, 22f, 180f);
            Salpicadura(gotas, -2);

            Niebla(Capa(raiz.transform, "Niebla", matHumo, 0), 3, 0.3f, 0.6f, 0.35f, 0.55f, 180f);
        }

        /// <summary>Muerte a balazo: como el impacto pero mas grande, con un chorro hacia arriba y trozos.</summary>
        static void ArmarMuerte(ParticleSystem raiz)
        {
            var piso = Piso(raiz.transform, AlturaCuerpo);

            Destello(raiz, matDestello, 0.9f, 0.08f, new Color(1f, 0.9f, 0.7f, 1f), 3);

            var gotas = Gotas(Capa(raiz.transform, "Gotas", matGota, 1), piso, 30, 40, 3f, 8f, 0.04f, 0.1f, 28f, 180f);
            Salpicadura(gotas, -2);

            // Segundo chorro hacia arriba y atras, apenas despues: el cuerpo "explota" en dos tiempos.
            var arriba = Gotas(Capa(raiz.transform, "Chorro", matGota, 1), piso, 14, 20, 3.5f, 6f, 0.04f, 0.09f, 30f, 125f);
            var em = arriba.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.06f, 14, 20) });
            Salpicadura(arriba, -2);

            Trozos(Capa(raiz.transform, "Trozos", matEscombro, 2), piso, Sangre, 6, 9, 2.5f, 5f, 0.07f, 0.14f, 150f);
            Niebla(Capa(raiz.transform, "Niebla", matHumo, 0), 6, 0.45f, 0.9f, 0.5f, 0.8f, 180f);
        }

        /// <summary>Atropello: todo sale hacia adelante (la moto empuja), con polvo del golpe.</summary>
        static void ArmarAtropello(ParticleSystem raiz)
        {
            var piso = Piso(raiz.transform, AlturaCuerpo);

            Destello(raiz, matDestello, 0.7f, 0.07f, new Color(1f, 0.95f, 0.85f, 1f), 3);

            var gotas = Gotas(Capa(raiz.transform, "Gotas", matGota, 1), piso, 26, 34, 4f, 9f, 0.04f, 0.1f, 30f, 25f);
            Salpicadura(gotas, -2);

            Trozos(Capa(raiz.transform, "Trozos", matEscombro, 2), piso, Sangre, 5, 8, 3f, 6f, 0.07f, 0.14f, 35f);
            Niebla(Capa(raiz.transform, "Niebla", matHumo, 0), 5, 0.4f, 0.8f, 0.45f, 0.7f, 0f);

            // Polvo del golpe contra el asfalto, siempre gris.
            var polvo = Capa(raiz.transform, "Polvo", matHumo, -1);
            polvo.transform.localPosition = new Vector3(0f, -AlturaCuerpo + 0.15f, 0f);
            var m = Main(polvo, 1f, 0.6f, 1f, 0.2f, 1f, 0.4f, 0.7f, new Color(0.5f, 0.47f, 0.43f, 0.5f));
            Rafaga(polvo, 0f, 5, 7);
            var sh = polvo.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(0.8f, 0.05f, 0.1f);
            Crecer(polvo, 2.2f);
            Desvanecer(polvo, 0.1f);
            Atlas(polvo, 4, 2);
            GiroAlAzar(polvo, 30f);
        }

        /// <summary>
        /// Explosion de barril en capas, como en los ejemplos del libro: destello que encandila,
        /// bola de fuego, onda expansiva en el piso, chispas que rebotan, escombros, humo que
        /// sube despues y brasas que quedan flotando.
        /// </summary>
        static void ArmarExplosion(ParticleSystem raiz)
        {
            var piso = Piso(raiz.transform, AlturaCuerpo);

            Destello(raiz, matDestello, 4.2f, 0.14f, new Color(1f, 0.85f, 0.55f, 1f), 5);

            // Bola de fuego.
            var fuego = Capa(raiz.transform, "Fuego", matFuego, 3);
            Main(fuego, 1f, 0.35f, 0.65f, 0.5f, 2.5f, 0.9f, 1.8f, Color.white);
            Rafaga(fuego, 0f, 12, 16);
            Esfera(fuego, 0.35f);
            Crecer(fuego, 1.6f);
            Gradiente(fuego, new[] { (new Color(1f, 0.96f, 0.8f), 0f), (Fuego, 0.3f), (new Color(0.55f, 0.1f, 0.04f), 0.75f) },
                      new[] { (1f, 0f), (1f, 0.5f), (0f, 1f) });
            Atlas(fuego, 2, 2);
            GiroAlAzar(fuego, 90f);
            Frenar(fuego, 3f);

            // Onda expansiva sobre el asfalto, aplastada por la perspectiva.
            var onda = Capa(raiz.transform, "Onda", matAnillo, -1);
            onda.transform.localPosition = new Vector3(0f, -AlturaCuerpo + 0.05f, 0f);
            var mo = Main(onda, 1f, 0.32f, 0.32f, 0f, 0f, 5f, 5f, new Color(1f, 0.8f, 0.55f, 0.75f));
            mo.startSize3D = true;
            mo.startSizeX = 5f;
            mo.startSizeY = 1.6f;
            mo.startSizeZ = 1f;
            Rafaga(onda, 0f, 1, 1);
            var so = onda.sizeOverLifetime;
            so.enabled = true;
            so.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.1f, 0f, 3f), new Keyframe(1f, 1f, 0f, 0f)));
            Desvanecer(onda, 0f);

            // Chispas que salen disparadas, caen y rebotan en el piso.
            var chispas = Capa(raiz.transform, "Chispas", matChispa, 4);
            Main(chispas, 1f, 0.45f, 0.9f, 5f, 10f, 0.05f, 0.1f, Color.white);
            Gravedad(chispas, 1.6f);
            Rafaga(chispas, 0f, 34, 46);
            Esfera(chispas, 0.2f);
            Estirar(chispas, 0.05f);
            Gradiente(chispas, new[] { (new Color(1f, 0.95f, 0.7f), 0f), (new Color(1f, 0.55f, 0.15f), 0.6f), (new Color(0.8f, 0.2f, 0.05f), 1f) },
                      new[] { (1f, 0f), (1f, 0.7f), (0f, 1f) });
            Chocar(chispas, piso, 0.45f, 0.3f);

            // Escombros que vuelan hacia arriba y caen.
            Trozos(Capa(raiz.transform, "Escombros", matEscombro, 2), piso, new Color(0.16f, 0.14f, 0.13f, 1f),
                   10, 14, 4f, 7.5f, 0.1f, 0.22f, 90f, 55f);

            // Humo negro que tapa el fuego al apagarse y sube.
            var humo = Capa(raiz.transform, "Humo", matHumo, -3);
            Main(humo, 1f, 1.4f, 2.2f, 0.6f, 1.6f, 1.2f, 2.2f, new Color(0.13f, 0.11f, 0.12f, 0.85f));
            Rafaga(humo, 0.08f, 9, 12);
            Esfera(humo, 0.4f);
            Crecer(humo, 2f);
            Gradiente(humo, new[] { (new Color(0.5f, 0.32f, 0.2f), 0f), (Color.white, 0.25f), (Color.white, 1f) },
                      new[] { (0f, 0f), (1f, 0.12f), (0.7f, 0.5f), (0f, 1f) });
            Subir(humo, 0.9f);
            Atlas(humo, 4, 2);
            GiroAlAzar(humo, 25f);
            Ruido(humo, 0.35f, 0.4f);
            Frenar(humo, 1.5f);

            // Brasas que quedan flotando y titilan.
            var brasas = Capa(raiz.transform, "Brasas", matChispa, 4);
            Main(brasas, 1f, 1f, 2f, 0.5f, 1.8f, 0.03f, 0.06f, new Color(1f, 0.6f, 0.2f, 1f));
            Rafaga(brasas, 0.1f, 14, 20);
            Esfera(brasas, 0.6f);
            Subir(brasas, 0.5f);
            Ruido(brasas, 0.8f, 1.2f);
            Gradiente(brasas, new[] { (Color.white, 0f), (Color.white, 1f) },
                      new[] { (1f, 0f), (0.4f, 0.3f), (1f, 0.5f), (0.3f, 0.75f), (0f, 1f) });
        }

        /// <summary>Fogonazo del arma: la llama hacia los zombies, un destello, humito y el casquillo.</summary>
        static void ArmarDisparo(ParticleSystem raiz)
        {
            var piso = Piso(raiz.transform, AlturaArma);

            // La llama: la base en la boca del arma, la punta hacia la izquierda.
            var m = Main(raiz, 1f, 0.05f, 0.06f, 0f, 0f, 0.6f, 0.75f, new Color(1f, 0.88f, 0.55f, 1f));
            m.startSize3D = true;
            m.startSizeX = new ParticleSystem.MinMaxCurve(0.6f, 0.75f);
            m.startSizeY = new ParticleSystem.MinMaxCurve(0.38f, 0.48f);
            m.startSizeZ = 1f;
            Rafaga(raiz, 0f, 1, 1);
            var sh = raiz.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.001f;
            sh.position = new Vector3(-0.3f, 0f, 0f);
            Desvanecer(raiz, 0.3f);
            Pintar(raiz, matFogonazo, 2);

            Destello(Capa(raiz.transform, "Destello", matDestello, 3), matDestello, 0.4f, 0.04f, new Color(1f, 0.95f, 0.8f, 1f), 3);

            var humo = Capa(raiz.transform, "Humo", matHumo, 0);
            Main(humo, 1f, 0.4f, 0.65f, 0.3f, 0.9f, 0.15f, 0.25f, new Color(0.75f, 0.75f, 0.78f, 0.35f));
            Rafaga(humo, 0.02f, 2, 3);
            Cono(humo, 15f, 180f, 0.02f);
            humo.transform.localPosition = new Vector3(-0.15f, 0f, 0f);
            Crecer(humo, 2.6f);
            Desvanecer(humo, 0.1f);
            Subir(humo, 0.35f);
            Atlas(humo, 4, 2);
            GiroAlAzar(humo, 40f);

            // El casquillo salta hacia arriba y atras, gira y rebota en el piso.
            var casquillo = Capa(raiz.transform, "Casquillo", matCasquillo, 1);
            var mc = Main(casquillo, 1f, 0.6f, 0.6f, 1.8f, 2.6f, 0.07f, 0.07f, new Color(0.9f, 0.68f, 0.25f, 1f));
            mc.startSize3D = true;
            mc.startSizeX = 0.07f;
            mc.startSizeY = 0.035f;
            mc.startSizeZ = 1f;
            mc.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            Gravedad(casquillo, 2.2f);
            Rafaga(casquillo, 0f, 1, 1);
            Cono(casquillo, 20f, 70f, 0.01f);
            var rot = casquillo.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-20f, 20f);
            Chocar(casquillo, piso, 0.35f, 0f);
            Desvanecer(casquillo, 0.8f);
        }

        /// <summary>
        /// Humo de la moto: polvo de la rueda (la raiz, su cantidad la maneja WheelDustView),
        /// humo del escape y llamaradas del escape en turbo.
        /// </summary>
        static void ArmarHumoMoto(ParticleSystem raiz)
        {
            var m = Main(raiz, 1f, 0.6f, 1f, 0.2f, 0.7f, 0.25f, 0.45f, new Color(0.55f, 0.5f, 0.45f, 0.45f));
            m.loop = true;
            m.playOnAwake = true;
            m.maxParticles = 150;
            Continuo(raiz);
            Cono(raiz, 20f, 165f, 0.08f);
            Crecer(raiz, 2.4f);
            Desvanecer(raiz, 0.1f);
            Subir(raiz, 0.2f);
            Atlas(raiz, 4, 2);
            GiroAlAzar(raiz, 35f);
            Ruido(raiz, 0.2f, 0.6f);
            Pintar(raiz, matHumo, 0);

            var escape = Capa(raiz.transform, "Escape", matHumo, -1);
            escape.transform.localPosition = new Vector3(0.1f, 0.22f, 0f);
            var me = Main(escape, 1f, 0.8f, 1.3f, 0.3f, 0.7f, 0.1f, 0.18f, new Color(0.33f, 0.33f, 0.37f, 0.4f));
            me.loop = true;
            me.playOnAwake = true;
            me.maxParticles = 80;
            Continuo(escape);
            Cono(escape, 12f, 180f, 0.02f);
            Crecer(escape, 3.5f);
            Desvanecer(escape, 0.05f);
            Subir(escape, 0.3f);
            Atlas(escape, 4, 2);
            GiroAlAzar(escape, 45f);
            Ruido(escape, 0.25f, 0.8f);

            var turbo = Capa(raiz.transform, "Turbo", matFuego, 1);
            turbo.transform.localPosition = new Vector3(0.05f, 0.22f, 0f);
            var mt = Main(turbo, 1f, 0.08f, 0.16f, 2f, 3.2f, 0.16f, 0.3f, Color.white);
            mt.loop = true;
            mt.playOnAwake = true;
            mt.maxParticles = 60;
            Continuo(turbo);
            Cono(turbo, 8f, 180f, 0.02f);
            Crecer(turbo, 0.4f);
            Gradiente(turbo, new[] { (new Color(0.75f, 0.85f, 1f), 0f), (new Color(1f, 0.7f, 0.25f), 0.35f), (new Color(0.9f, 0.25f, 0.05f), 1f) },
                      new[] { (1f, 0f), (0.9f, 0.5f), (0f, 1f) });
            Atlas(turbo, 2, 2);
            GiroAlAzar(turbo, 180f);
        }

        // --- Capas comunes -----------------------------------------------------------------

        static void Destello(ParticleSystem ps, Material mat, float tam, float vida, Color color, int orden)
        {
            Main(ps, 1f, vida, vida, 0f, 0f, tam, tam * 1.15f, color);
            Rafaga(ps, 0f, 1, 1);
            var s = ps.sizeOverLifetime;
            s.enabled = true;
            s.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.8f)));
            Desvanecer(ps, 0.2f);
            GiroAlAzar(ps, 0f);
            Pintar(ps, mat, orden);
        }

        /// <summary>Gotas estiradas por la velocidad, con gravedad, que mueren al tocar el piso.</summary>
        static ParticleSystem Gotas(ParticleSystem ps, Transform piso, int min, int max, float vMin, float vMax,
                                    float tMin, float tMax, float angulo, float direccion)
        {
            Main(ps, 1f, 0.5f, 0.9f, vMin, vMax, tMin, tMax, Sangre);
            Gravedad(ps, 1.4f);
            Rafaga(ps, 0f, min, max);
            Cono(ps, angulo, direccion, 0.08f);
            Estirar(ps, 0.035f);
            Desvanecer(ps, 0.85f);
            Chocar(ps, piso, 0f, 1f);
            return ps;
        }

        /// <summary>Sub-emisor: cada gota que toca el piso deja una salpicadura chata que se seca (orden: debajo de todo).</summary>
        static void Salpicadura(ParticleSystem gotas, int orden)
        {
            var s = Capa(gotas.transform, "Salpicadura", matGota, orden);
            var m = Main(s, 1f, 1.2f, 1.8f, 0f, 0f, 0.07f, 0.13f, Sangre);
            m.startSize3D = true;
            m.startSizeX = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            m.startSizeY = new ParticleSystem.MinMaxCurve(0.025f, 0.045f);
            m.startSizeZ = 1f;
            Rafaga(s, 0f, 1, 1);
            Desvanecer(s, 0.7f);

            var sub = gotas.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(s, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing);
        }

        static void Trozos(ParticleSystem ps, Transform piso, Color color, int min, int max, float vMin, float vMax,
                           float tMin, float tMax, float direccion, float angulo = 35f)
        {
            var m = Main(ps, 1f, 0.9f, 1.4f, vMin, vMax, tMin, tMax, color);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            Gravedad(ps, 2.2f);
            Rafaga(ps, 0f, min, max);
            Cono(ps, angulo, direccion, 0.1f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-12f, 12f);
            Chocar(ps, piso, 0.25f, 0f);
            Desvanecer(ps, 0.75f);
        }

        static void Niebla(ParticleSystem ps, int cantidad, float tMin, float tMax, float vidaMin, float vidaMax, float direccion)
        {
            Main(ps, 1f, vidaMin, vidaMax, 0.4f, 1.2f, tMin, tMax, SangreNiebla);
            Rafaga(ps, 0f, cantidad, cantidad + 2);
            Cono(ps, 35f, direccion, 0.1f);
            Crecer(ps, 1.8f);
            Desvanecer(ps, 0f);
            Atlas(ps, 4, 2);
            GiroAlAzar(ps, 30f);
            Frenar(ps, 2f);
        }

        // --- Modulos --------------------------------------------------------------------

        /// <summary>Hijo con su sistema y renderer, limpio y en espacio de mundo.</summary>
        static ParticleSystem Capa(Transform parent, string name, Material mat, int orden)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            Limpiar(ps);
            Pintar(ps, mat, orden);
            return ps;
        }

        /// <summary>El plano de colision, en el piso del carril.</summary>
        static Transform Piso(Transform raiz, float altura)
        {
            var go = new GameObject("Piso");
            go.transform.SetParent(raiz, false);
            go.transform.localPosition = new Vector3(0f, -altura, 0f);
            return go.transform;
        }

        /// <summary>Apaga todos los modulos: cada efecto prende solo los que usa.</summary>
        static void Limpiar(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var shape = ps.shape; shape.enabled = false;
            var vel = ps.velocityOverLifetime; vel.enabled = false;
            var lim = ps.limitVelocityOverLifetime; lim.enabled = false;
            var inh = ps.inheritVelocity; inh.enabled = false;
            var force = ps.forceOverLifetime; force.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = false;
            var cbs = ps.colorBySpeed; cbs.enabled = false;
            var size = ps.sizeOverLifetime; size.enabled = false;
            var sbs = ps.sizeBySpeed; sbs.enabled = false;
            var rot = ps.rotationOverLifetime; rot.enabled = false;
            var rbs = ps.rotationBySpeed; rbs.enabled = false;
            var ext = ps.externalForces; ext.enabled = false;
            var noise = ps.noise; noise.enabled = false;
            var colli = ps.collision; colli.enabled = false;
            var trig = ps.trigger; trig.enabled = false;
            var sub = ps.subEmitters;
            for (int i = sub.subEmittersCount - 1; i >= 0; i--)
                sub.RemoveSubEmitter(i);
            sub.enabled = false;
            var tsa = ps.textureSheetAnimation; tsa.enabled = false;
            var lights = ps.lights; lights.enabled = false;
            var trails = ps.trails; trails.enabled = false;
            var custom = ps.customData; custom.enabled = false;
            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = 0f;
            em.rateOverDistance = 0f;
            em.SetBursts(new ParticleSystem.Burst[0]);
        }

        static ParticleSystem.MainModule Main(ParticleSystem ps, float duracion, float vidaMin, float vidaMax,
                                              float vMin, float vMax, float tMin, float tMax, Color color)
        {
            var m = ps.main;
            m.duration = duracion;
            m.loop = false;
            m.prewarm = false;
            m.playOnAwake = false;
            m.startDelay = 0f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(vidaMin, vidaMax);
            m.startSpeed = new ParticleSystem.MinMaxCurve(vMin, vMax);
            m.startSize3D = false;
            m.startSize = new ParticleSystem.MinMaxCurve(tMin, tMax);
            m.startRotation3D = false;
            m.startRotation = 0f;
            m.flipRotation = 0f;
            m.startColor = color;
            m.gravityModifier = 0f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.simulationSpeed = 1f;
            m.useUnscaledTime = false;
            m.scalingMode = ParticleSystemScalingMode.Hierarchy;
            m.maxParticles = 100;
            m.stopAction = ParticleSystemStopAction.None;
            m.cullingMode = ParticleSystemCullingMode.Automatic;
            return m;
        }

        static void Continuo(ParticleSystem ps)
        {
            var em = ps.emission;
            em.rateOverTime = 0f; // lo maneja WheelDustView
        }

        static void Rafaga(ParticleSystem ps, float cuando, int min, int max)
        {
            var em = ps.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(cuando, (short)min, (short)max) });
            var m = ps.main;
            m.maxParticles = Mathf.Max(m.maxParticles, max * 2);
        }

        static void Gravedad(ParticleSystem ps, float g)
        {
            var m = ps.main;
            m.gravityModifier = g;
        }

        /// <summary>
        /// Cono que apunta en el plano de la pantalla: direccion en grados (0 derecha, 90 arriba,
        /// 180 izquierda, hacia donde viene la horda). El cono nace sobre +Z; con la rotacion
        /// (-direccion, 90, 0) de Unity (Z, despues X, despues Y) +Z queda en (cos d, sin d, 0).
        /// </summary>
        static void Cono(ParticleSystem ps, float angulo, float direccion, float radio)
        {
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = angulo;
            sh.radius = radio;
            sh.radiusThickness = 1f;
            sh.rotation = new Vector3(-direccion, 90f, 0f);
        }

        static void Esfera(ParticleSystem ps, float radio)
        {
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = radio;
            sh.radiusThickness = 1f;
            sh.arc = 360f;
        }

        static void Crecer(ParticleSystem ps, float hasta)
        {
            var s = ps.sizeOverLifetime;
            s.enabled = true;
            s.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f, 0f, (hasta - 1f) * 2f), new Keyframe(1f, hasta, 0f, 0f)));
        }

        /// <summary>Alfa constante hasta "desde" (0..1 de la vida) y despues a cero.</summary>
        static void Desvanecer(ParticleSystem ps, float desde)
        {
            var c = ps.colorOverLifetime;
            c.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      desde <= 0f
                          ? new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
                          : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, desde), new GradientAlphaKey(0f, 1f) });
            c.color = g;
        }

        static void Gradiente(ParticleSystem ps, (Color color, float t)[] colores, (float alfa, float t)[] alfas)
        {
            var c = ps.colorOverLifetime;
            c.enabled = true;
            var g = new Gradient();
            var ck = new GradientColorKey[colores.Length];
            for (int i = 0; i < colores.Length; i++) ck[i] = new GradientColorKey(colores[i].color, colores[i].t);
            var ak = new GradientAlphaKey[alfas.Length];
            for (int i = 0; i < alfas.Length; i++) ak[i] = new GradientAlphaKey(alfas[i].alfa, alfas[i].t);
            g.SetKeys(ck, ak);
            c.color = g;
        }

        /// <summary>Una variante del atlas al azar por particula, sin animar.</summary>
        static void Atlas(ParticleSystem ps, int x, int y)
        {
            var t = ps.textureSheetAnimation;
            t.enabled = true;
            t.mode = ParticleSystemAnimationMode.Grid;
            t.numTilesX = x;
            t.numTilesY = y;
            t.animation = ParticleSystemAnimationType.WholeSheet;
            t.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);
            t.cycleCount = 1;
        }

        static void GiroAlAzar(ParticleSystem ps, float gradosPorSegundo)
        {
            var m = ps.main;
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            if (gradosPorSegundo <= 0f)
                return;
            var r = ps.rotationOverLifetime;
            r.enabled = true;
            r.z = new ParticleSystem.MinMaxCurve(-gradosPorSegundo * Mathf.Deg2Rad, gradosPorSegundo * Mathf.Deg2Rad);
        }

        static void Subir(ParticleSystem ps, float velocidad)
        {
            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            // Los tres ejes en el mismo modo (dos constantes): Unity no acepta mezclarlos.
            v.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            v.y = new ParticleSystem.MinMaxCurve(velocidad * 0.6f, velocidad);
            v.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        static void Frenar(ParticleSystem ps, float arrastre)
        {
            var l = ps.limitVelocityOverLifetime;
            l.enabled = true;
            l.limit = 100f;
            l.drag = arrastre;
            l.multiplyDragByParticleSize = false;
            l.multiplyDragByParticleVelocity = false;
        }

        static void Ruido(ParticleSystem ps, float fuerza, float frecuencia)
        {
            var n = ps.noise;
            n.enabled = true;
            n.strength = fuerza;
            n.frequency = frecuencia;
            n.scrollSpeed = 0.3f;
            n.damping = true;
            n.quality = ParticleSystemNoiseQuality.Low;
        }

        static void Chocar(ParticleSystem ps, Transform piso, float rebote, float perdidaDeVida)
        {
            var c = ps.collision;
            c.enabled = true;
            c.type = ParticleSystemCollisionType.Planes;
            c.AddPlane(piso);
            c.dampen = 0.5f;
            c.bounce = rebote;
            c.lifetimeLoss = perdidaDeVida;
            c.radiusScale = 0.2f;
            c.sendCollisionMessages = false;
        }

        static void Estirar(ParticleSystem ps, float porVelocidad)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 1.2f;
            r.velocityScale = porVelocidad;
            r.cameraVelocityScale = 0f;
        }

        static void Pintar(ParticleSystem ps, Material mat, int orden)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.alignment = ParticleSystemRenderSpace.View;
            r.sharedMaterial = mat;
            r.sortingOrder = orden;
            r.minParticleSize = 0f;
            r.maxParticleSize = 1f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.flip = Vector3.zero;
        }

        // --- Prefabs y escenas ----------------------------------------------------------

        /// <summary>Rearma un prefab existente sin cambiar su sistema raiz (las escenas lo referencian).</summary>
        static void Reescribir(string nombre, System.Action<ParticleSystem> armar)
        {
            string path = PrefabsDir + nombre + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                var ps = root.GetComponent<ParticleSystem>();
                Limpiar(ps);
                armar(ps);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static ParticleSystem Crear(string nombre, System.Action<ParticleSystem> armar)
        {
            string path = PrefabsDir + nombre + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Reescribir(nombre, armar);
            }
            else
            {
                var go = new GameObject(nombre);
                var ps = go.AddComponent<ParticleSystem>();
                Limpiar(ps);
                armar(ps);
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ParticleSystem>();
        }

        static void Enganchar(string escenaPath, ParticleSystem disparo, ParticleSystem humo)
        {
            var scene = EditorSceneManager.OpenScene(escenaPath, OpenSceneMode.Single);

            var fx = Object.FindAnyObjectByType<FxManager>();
            if (fx != null)
            {
                var so = new SerializedObject(fx);
                so.FindProperty("muzzlePrefab").objectReferenceValue = disparo;
                so.FindProperty("bloodSprite").objectReferenceValue = SpriteKenney("dirt_03");
                so.FindProperty("scorchSprite").objectReferenceValue = SpriteKenney("scorch_03");
                so.FindProperty("tracerSeconds").floatValue = 0.09f;
                so.ApplyModifiedPropertiesWithoutUndo();

                var tracer = so.FindProperty("tracer").objectReferenceValue as LineRenderer;
                if (tracer != null)
                    ArmarTraza(tracer);
            }
            else
            {
                Debug.LogWarning("Sin FxManager en " + escenaPath);
            }

            var rueda = Object.FindAnyObjectByType<WheelDustView>();
            if (rueda != null)
                CambiarHumo(rueda, humo);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>La traza: fina en el arma, mas gruesa y brillante hacia el blanco, aditiva.</summary>
        static void ArmarTraza(LineRenderer tracer)
        {
            tracer.sharedMaterial = matTraza;
            tracer.textureMode = LineTextureMode.Stretch;
            tracer.alignment = LineAlignment.View;
            tracer.numCapVertices = 2;
            tracer.widthMultiplier = 1f;
            tracer.widthCurve = new AnimationCurve(new Keyframe(0f, 0.015f), new Keyframe(0.5f, 0.045f), new Keyframe(1f, 0.06f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.75f, 0.35f), 0f), new GradientColorKey(new Color(1f, 0.97f, 0.8f), 1f) },
                      new[] { new GradientAlphaKey(0.15f, 0f), new GradientAlphaKey(0.9f, 0.4f), new GradientAlphaKey(1f, 1f) });
            tracer.colorGradient = g;
            EditorUtility.SetDirty(tracer);
        }

        /// <summary>Cambia el polvo viejo de la rueda por el prefab de humo, en el mismo lugar.</summary>
        static void CambiarHumo(WheelDustView rueda, ParticleSystem prefab)
        {
            var so = new SerializedObject(rueda);
            var viejo = so.FindProperty("dust").objectReferenceValue as ParticleSystem;
            if (viejo == null)
            {
                Debug.LogWarning("WheelDustView sin polvo asignado en " + rueda.gameObject.scene.name);
                return;
            }

            var padre = viejo.transform.parent;
            var pos = viejo.transform.localPosition;
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, padre);
            instancia.transform.localPosition = pos;
            instancia.transform.localRotation = Quaternion.identity;
            instancia.transform.localScale = Vector3.one;
            instancia.transform.SetSiblingIndex(viejo.transform.GetSiblingIndex());

            // Solo se borra si es un objeto de polvo suelto (no el prefab nuevo de una corrida anterior
            // ni algo con mas cosas colgadas).
            if (viejo.gameObject != instancia)
                Object.DestroyImmediate(viejo.gameObject);

            var ps = instancia.GetComponent<ParticleSystem>();
            so.FindProperty("dust").objectReferenceValue = ps;
            so.FindProperty("exhaust").objectReferenceValue = instancia.transform.Find("Escape")?.GetComponent<ParticleSystem>();
            so.FindProperty("turboFlames").objectReferenceValue = instancia.transform.Find("Turbo")?.GetComponent<ParticleSystem>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Materiales y texturas -----------------------------------------------------

        static void CrearMateriales()
        {
            foreach (var t in new[] { "humo_atlas", "fuego_atlas", "fogonazo", "traza", "casquillo" })
                ImportarTextura(GenDir + t + ".png");

            matHumo = CrearMaterial("Fx_Humo", Tex(GenDir + "humo_atlas.png"), false);
            matFuego = CrearMaterial("Fx_Fuego", Tex(GenDir + "fuego_atlas.png"), true);
            matDestello = CrearMaterial("Fx_Destello", Tex(KenneyDir + "star_09.png"), true);
            matChispa = CrearMaterial("Fx_Chispa", Tex(KenneyDir + "star_05.png"), true);
            matGota = CrearMaterial("Fx_Gota", Tex(KenneyDir + "circle_05.png"), false);
            matAnillo = CrearMaterial("Fx_Anillo", Tex(KenneyDir + "circle_02.png"), true);
            matEscombro = CrearMaterial("Fx_Escombro", Tex(KenneyDir + "dirt_01.png"), false);
            matFogonazo = CrearMaterial("Fx_Fogonazo", Tex(GenDir + "fogonazo.png"), true);
            matCasquillo = CrearMaterial("Fx_Casquillo", Tex(GenDir + "casquillo.png"), false);
            matTraza = CrearMaterial("Fx_Traza", Tex(GenDir + "traza.png"), true);
        }

        static void ImportarTextura(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null)
            {
                Debug.LogError("Falta la textura " + path);
                return;
            }
            imp.textureType = TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }

        static Texture2D Tex(string path)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t == null)
                Debug.LogError("No se pudo cargar " + path);
            return t;
        }

        static Sprite SpriteKenney(string nombre) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(KenneyDir + nombre + ".png");

        /// <summary>Particles/Unlit de URP, transparente: mezcla alfa (humo, sangre) o aditiva (fuego, luz).</summary>
        static Material CrearMaterial(string nombre, Texture2D tex, bool aditivo)
        {
            string path = GenDir + nombre + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", aditivo ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(aditivo ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)(aditivo ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
