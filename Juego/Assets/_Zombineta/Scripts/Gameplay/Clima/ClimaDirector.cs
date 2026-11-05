using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;
using Zombineta.Level;
using Zombineta.Player;

namespace Zombineta.Clima
{
    /// <summary>
    /// El clima del nivel: lluvia con salpicaduras, bancos de niebla en tres alturas, viento que inclina
    /// la lluvia, arrastra papeles y mueve los arboles. Sale de los tramos (lluvia, niebla y viento), como
    /// el apagon, y entra y sale con fundido. Es solo dibujo: no cambia ninguna regla. Se crea solo al
    /// dar Play y se arma unicamente en un nivel cuyos tramos tengan clima.
    /// </summary>
    public sealed class ClimaDirector : MonoBehaviour
    {
        const float SegundosBusqueda = 0.5f;
        const float SegundosArboles = 1f;

        static ClimaDirector instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarStatic()
        {
            instancia = null;
            Lluvia = Niebla = Viento = 0f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null)
                return;
            var go = new GameObject("ClimaDirector");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<ClimaDirector>();
        }

        /// <summary>El clima que se esta viendo ahora (0 a 1, ya suavizado).</summary>
        public static float Lluvia { get; private set; }
        public static float Niebla { get; private set; }
        public static float Viento { get; private set; }

        [Header("Lluvia")]
        [SerializeField] int gotasPorSegundo = 900;
        [SerializeField] Color colorGota = new Color(0.78f, 0.84f, 1f, 0.3f);
        [SerializeField] float velocidadGota = 24f;
        [SerializeField] int salpicadurasPorSegundo = 140;
        [SerializeField] Color colorSalpicadura = new Color(0.75f, 0.82f, 1f, 0.35f);

        [Header("Niebla")]
        [SerializeField] Color colorNiebla = new Color(0.62f, 0.6f, 0.78f, 1f);
        [Tooltip("Opacidad de cada banco con la niebla a pleno: lejos (sobre los edificios), sobre la calle y delante de todo.")]
        [SerializeField] Vector3 opacidadBancos = new Vector3(0.45f, 0.38f, 0.22f);
        [Tooltip("Cuanto ilumina el aire el faro de la moto con la niebla a pleno (luz volumetrica).")]
        [SerializeField] float faroEnLaNiebla = 0.06f;

        [Header("Viento")]
        [SerializeField] int papelesPorSegundo = 7;
        [SerializeField] Color colorPapel = new Color(0.55f, 0.52f, 0.5f, 0.85f);
        [Tooltip("Grados que se mecen los arboles con el viento a pleno.")]
        [SerializeField] float balanceoArboles = 1.2f;

        [Tooltip("Cuanto tarda el clima en cambiar cuando la moto cambia de tramo, en segundos.")]
        [SerializeField] float segundosCambio = 2f;

        RunController run;
        float proximaBusqueda;
        bool armado;
        ParticleSystem gotas, salpicaduras, papeles;
        readonly List<Banco> bancos = new List<Banco>();
        readonly List<Transform> arboles = new List<Transform>();
        float proximoArbolado;
        Light2D[] faro;

        sealed class Banco
        {
            public SpriteRenderer sr;
            public float parallax;
            public float abajo, arriba;
            public float opacidad;
            public float deriva;
        }

        void LateUpdate()
        {
            Buscar();
            bool hayClima = run != null && run.Sim != null && run.Config != null && Tramos.TieneClima(run.Config.tramos);
            if (!hayClima)
            {
                Lluvia = Niebla = Viento = 0f;
                if (armado)
                    Desarmar();
                return;
            }

            var cam = Camera.main;
            if (cam == null)
                return;
            if (!armado)
                Armar();

            var tramos = run.Config.tramos;
            float x = run.Sim.State.Phase == RunPhase.Pelea ? run.Sim.State.PeatonX : run.Sim.State.PlayerX;
            float fundido = run.Config.tramoDarknessFadeMeters;
            float paso = segundosCambio > 0f ? Time.deltaTime / segundosCambio : 1f;
            Lluvia = Mathf.MoveTowards(Lluvia, Tramos.LluviaAt(tramos, x, fundido), paso);
            Niebla = Mathf.MoveTowards(Niebla, Tramos.NieblaAt(tramos, x, fundido), paso);
            Viento = Mathf.MoveTowards(Viento, Tramos.VientoAt(tramos, x, fundido), paso);

            float calidad = GameSettings.LightQuality == LightQuality.Baja ? 0.4f : 1f;
            DibujarLluvia(cam, calidad);
            DibujarNiebla(cam);
            DibujarViento(cam, calidad);
            MecerArboles();
            AlumbrarLaNiebla(calidad);
        }

        void Buscar()
        {
            if (run != null || Time.unscaledTime < proximaBusqueda)
                return;
            proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
            run = FindAnyObjectByType<RunController>();
            faro = null;
            arboles.Clear();
        }

        // --- Armado ------------------------------------------------------------------

        void Armar()
        {
            armado = true;
            gotas = NuevoSistema("Lluvia", TexturasDeClima.Gota, "Frente", 3000);
            var r = gotas.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.026f;
            r.lengthScale = 1f;
            var main = gotas.main;
            main.startLifetime = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.016f, 0.026f);
            main.startColor = colorGota;
            main.maxParticles = 1500;
            var forma = gotas.shape;
            forma.shapeType = ParticleSystemShapeType.Box;

            salpicaduras = NuevoSistema("Salpicaduras", TexturasDeClima.Salpicadura, "Calle", 500);
            main = salpicaduras.main;
            main.startLifetime = 0.25f;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startColor = colorSalpicadura;
            main.maxParticles = 400;
            var tam = salpicaduras.sizeOverLifetime;
            tam.enabled = true;
            tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.3f, 1f, 1f));
            var alfa = salpicaduras.colorOverLifetime;
            alfa.enabled = true;
            alfa.color = Desvanecer();
            forma = salpicaduras.shape;
            forma.shapeType = ParticleSystemShapeType.Box;

            papeles = NuevoSistema("Papeles al viento", TexturasDeClima.Papel, LaneSorting.GameLayer, LaneSorting.Order(0f, SortSlot.Effect) + 5);
            main = papeles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startColor = colorPapel;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 60;
            var giro = papeles.rotationOverLifetime;
            giro.enabled = true;
            giro.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var ruido = papeles.noise;
            ruido.enabled = true;
            ruido.strength = 1.4f;
            ruido.frequency = 0.6f;
            forma = papeles.shape;
            forma.shapeType = ParticleSystemShapeType.Box;

            bancos.Add(NuevoBanco("Niebla lejos", "Fondo", 100, 0.75f, 1.6f, 7.5f, opacidadBancos.x, 0.25f));
            bancos.Add(NuevoBanco("Niebla en la calle", "Calle", 300, 1f, -2.6f, 1.8f, opacidadBancos.y, 0.6f));
            bancos.Add(NuevoBanco("Niebla delante", "Frente", 2500, 1.35f, -5f, -1.2f, opacidadBancos.z, 1f));
        }

        static Gradient Desvanecer()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        ParticleSystem NuevoSistema(string nombre, Material material, string capa, int orden)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.sortingLayerName = capa;
            r.sortingOrder = orden;
            var emision = ps.emission;
            emision.rateOverTime = 0f;
            ps.Play();
            return ps;
        }

        Banco NuevoBanco(string nombre, string capa, int orden, float parallax, float abajo, float arriba, float opacidad, float deriva)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            // Sin luz: con el material iluminado de siempre, de noche la niebla quedaba negra.
            sr.sharedMaterial = TexturasDeClima.SinLuz;
            sr.sprite = TexturasDeClima.Niebla;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.sortingLayerName = capa;
            sr.sortingOrder = orden;
            sr.enabled = false;
            return new Banco { sr = sr, parallax = parallax, abajo = abajo, arriba = arriba, opacidad = opacidad, deriva = deriva };
        }

        void Desarmar()
        {
            armado = false;
            foreach (Transform hijo in transform)
                Destroy(hijo.gameObject);
            bancos.Clear();
            arboles.Clear();
            gotas = salpicaduras = papeles = null;
        }

        // --- Cada cuadro -------------------------------------------------------------

        void DibujarLluvia(Camera cam, float calidad)
        {
            float alto = cam.orthographicSize;
            float ancho = alto * cam.aspect;
            var c = cam.transform.position;

            // Las gotas quedan en el mundo mientras caen: la camara se adelanta y las ve inclinadas, como desde
            // una moto. Por eso salen tambien por delante de la pantalla.
            gotas.transform.position = new Vector3(c.x + ancho * 0.6f, c.y + alto + 1f, 0f);
            var forma = gotas.shape;
            forma.scale = new Vector3(ancho * 3.2f, 0.1f, 1f);
            var vel = gotas.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-9f * Viento - 1f);
            vel.y = new ParticleSystem.MinMaxCurve(-velocidadGota);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            var e = gotas.emission;
            e.rateOverTime = gotasPorSegundo * Lluvia * calidad;

            // Las salpicaduras, sobre el asfalto de los tres carriles.
            float abajo = run.LaneToWorldY(-0.6f), arriba = run.LaneToWorldY(2.4f);
            salpicaduras.transform.position = new Vector3(c.x + ancho * 0.2f, (abajo + arriba) * 0.5f, 0f);
            forma = salpicaduras.shape;
            forma.scale = new Vector3(ancho * 2.4f, arriba - abajo, 1f);
            e = salpicaduras.emission;
            e.rateOverTime = salpicadurasPorSegundo * Lluvia * calidad;
        }

        void DibujarNiebla(Camera cam)
        {
            float ancho = cam.orthographicSize * cam.aspect * 2f;
            var c = cam.transform.position;
            foreach (var b in bancos)
            {
                float alfa = b.opacidad * Niebla;
                b.sr.enabled = alfa > 0.005f;
                if (!b.sr.enabled)
                    continue;

                var col = colorNiebla;
                col.a = alfa;
                b.sr.color = col;

                float alto = b.arriba - b.abajo;
                float escala = alto / b.sr.sprite.bounds.size.y;
                b.sr.transform.localScale = new Vector3(escala, escala, 1f);
                float tile = b.sr.sprite.bounds.size.x * escala;
                b.sr.size = new Vector2((ancho + tile * 3f) / escala, b.sr.sprite.bounds.size.y);

                // El dibujo se mueve con su parallax y ademas lo arrastra el viento.
                b.deriva += Time.deltaTime * (0.15f + 1.2f * Viento) * b.parallax;
                float origen = c.x * (1f - b.parallax) - b.deriva;
                float x = origen + Mathf.Round((c.x - origen) / tile) * tile;
                b.sr.transform.position = new Vector3(x, (b.abajo + b.arriba) * 0.5f + c.y * (1f - b.parallax) * 0.3f, 0f);
            }
        }

        void DibujarViento(Camera cam, float calidad)
        {
            float alto = cam.orthographicSize;
            float ancho = alto * cam.aspect;
            var c = cam.transform.position;
            papeles.transform.position = new Vector3(c.x + ancho + 1.5f, run.LaneToWorldY(1.5f), 0f);
            var forma = papeles.shape;
            forma.scale = new Vector3(0.5f, alto * 1.2f, 1f);
            var vel = papeles.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-14f * Viento, -6f * Viento);
            vel.y = new ParticleSystem.MinMaxCurve(-0.6f, 0.9f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            var e = papeles.emission;
            e.rateOverTime = papelesPorSegundo * Viento * Viento * calidad;
        }

        // Los arboles del escenario se mecen sobre su base. Se buscan cada tanto: el escenario los recicla.
        void MecerArboles()
        {
            if (Time.time >= proximoArbolado)
            {
                proximoArbolado = Time.time + SegundosArboles;
                arboles.Clear();
                foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                    if (sr.sprite != null && sr.sprite.name.Contains("arbol") && sr.transform.parent != null &&
                        sr.transform.parent.GetComponentInParent<Zombineta.Scenery.SceneryManager>() != null)
                        arboles.Add(sr.transform);
            }

            foreach (var t in arboles)
            {
                if (t == null)
                    continue;
                float fase = t.position.x * 0.37f;
                float grados = Viento * balanceoArboles * (0.6f + 0.4f * Mathf.Sin(Time.time * 1.3f + fase));
                t.localRotation = Quaternion.Euler(0f, 0f, grados);
            }
        }

        // El faro se ve en el aire cuando hay niebla o lluvia.
        void AlumbrarLaNiebla(float calidad)
        {
            if (faro == null)
            {
                var vista = FindAnyObjectByType<HeadlightView>();
                faro = vista != null ? vista.GetComponentsInChildren<Light2D>(true) : new Light2D[0];
            }
            float aire = calidad < 1f ? 0f : faroEnLaNiebla * Mathf.Clamp01(Niebla + Lluvia * 0.4f);
            foreach (var l in faro)
            {
                if (l == null)
                    continue;
                l.volumeIntensityEnabled = aire > 0.001f;
                l.volumeIntensity = aire;
            }
        }
    }
}
