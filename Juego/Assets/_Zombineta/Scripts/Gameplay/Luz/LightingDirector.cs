using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;
using Zombineta.Juego.Levels;

namespace Zombineta.Luz
{
    /// <summary>
    /// Arma la luz ambiente del nivel a partir de un PerfilDeLuz: un hijo "Luz Global <capa>" por
    /// cada capa del juego con algo de luz (propia o de General), cada uno con una Light2D global
    /// en Multiply para no pelear con las luces puntuales (faro, etc.) que ya cubren esas capas.
    /// No existe una luz "General" propia: URP 2D solo permite una luz global por capa y estilo de
    /// mezcla, asi que esa luz se sumaria a las de capa sin ningun efecto (ver PerfilDeLuz.Resolver,
    /// que hace la suma en el color en vez de crear otra luz). Se reconstruye solo si el perfil (o
    /// la referencia asignada) cambio, asi que arte lo ve en vivo sin tocar la escena.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-150)]
    public sealed class LightingDirector : MonoBehaviour
    {
        const string ChildPrefix = "Luz Global ";

        [Tooltip("Solo para escenas sin LevelScene (por ejemplo el taller). Si este GameObject " +
                 "tiene un LevelScene, su Perfil manda siempre y este campo se pisa.")]
        [SerializeField] PerfilDeLuz perfil;

        public PerfilDeLuz Perfil
        {
            get => perfil;
            set => perfil = value;
        }

        readonly List<GameObject> hijos = new List<GameObject>();
        readonly List<(Light2D luz, Color color, string capa)> luces = new List<(Light2D, Color, string)>();
        RunController run;
        float oscuridadAplicada;
        LevelScene escena;
        bool escenaBuscada;
        PerfilDeLuz perfilConstruido;
        int versionConstruida;
        bool avisoSinPerfil;

        void Awake()
        {
            SincronizarPerfilDeLevelScene();
        }

        void OnEnable()
        {
            SincronizarPerfilDeLevelScene();
            Rebuild();
        }

        void OnValidate()
        {
            SincronizarPerfilDeLevelScene();
        }

        void Update()
        {
            SincronizarPerfilDeLevelScene();
            if (perfil != perfilConstruido || Version(perfil) != versionConstruida)
                Rebuild();

            AplicarApagon(OscuridadActual());
        }

        /// <summary>La oscuridad del tramo donde esta la moto. Fuera de juego, o sin tramos, 0.</summary>
        float OscuridadActual()
        {
            if (!Application.isPlaying)
                return 0f;
            if (run == null)
                run = FindAnyObjectByType<RunController>();
            return run != null && run.Sim != null ? run.Sim.Oscuridad : 0f;
        }

        /// <summary>
        /// Apagon: cada luz ambiente baja a la fraccion que su capa tenga en el perfil. No se
        /// reconstruye nada: solo se atenua el color sobre el que ya dejo Rebuild.
        /// </summary>
        void AplicarApagon(float oscuridad)
        {
            if (perfil == null || Mathf.Approximately(oscuridad, oscuridadAplicada))
                return;

            oscuridadAplicada = oscuridad;
            foreach (var (luz, color, capa) in luces)
            {
                if (luz == null)
                    continue;
                var atenuado = color * perfil.FactorDeApagon(capa, oscuridad);
                atenuado.a = 1f;
                luz.color = atenuado;
            }
        }

        /// <summary>
        /// Si este GameObject tiene un LevelScene, su Perfil es la unica fuente real (item 1 del
        /// arreglo final): se sincroniza siempre, no solo cuando 'perfil' esta vacio, para que
        /// tambien se note un cambio de referencia hecho en el LevelScene. Sin LevelScene (el
        /// taller, que arma un LightingDirector suelto), el campo propio manda.
        /// </summary>
        void SincronizarPerfilDeLevelScene()
        {
            if (!escenaBuscada)
            {
                escena = GetComponent<LevelScene>();
                escenaBuscada = true;
            }

            if (escena != null)
                perfil = escena.Perfil;
        }

        static int Version(PerfilDeLuz p) => p != null ? p.Version : -1;

        public void Rebuild()
        {
            DestruirHijos();
            oscuridadAplicada = 0f;
            perfilConstruido = perfil;
            versionConstruida = Version(perfil);

            if (perfil == null)
            {
                if (!avisoSinPerfil)
                {
                    Debug.LogWarning("LightingDirector: sin PerfilDeLuz asignado, no se crea ninguna luz global.");
                    avisoSinPerfil = true;
                }
                return;
            }

            avisoSinPerfil = false;

            // Una luz por capa con su color ya combinado (propio + General, ver
            // PerfilDeLuz.Resolver): sin luz "General" aparte, que URP ignoraria de todos modos.
            foreach (var resuelta in perfil.Resolver(TodasLasCapas()))
                CrearLuz(ChildPrefix + resuelta.capa, resuelta.color, resuelta.capa);
        }

        void CrearLuz(string nombre, Color color, string capa)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            go.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSave;

            var luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Global;
            luz.blendStyleIndex = 0; // Multiply: no pelea con las luces puntuales existentes.
            luz.color = color;
            luz.intensity = 1f; // La intensidad ya esta en el color (PerfilDeLuz.Resolver la aplico).

            AplicarCapas(luz, new[] { capa });
            hijos.Add(go);
            luces.Add((luz, color, capa));
        }

        static void AplicarCapas(Light2D luz, string[] nombresCapas)
        {
            var ids = new List<int>(nombresCapas.Length);
            foreach (var nombre in nombresCapas)
            {
                foreach (var capa in SortingLayer.layers)
                {
                    if (capa.name == nombre)
                    {
                        ids.Add(capa.id);
                        break;
                    }
                }
            }

            // Sin #if UNITY_EDITOR/SerializedObject: targetSortingLayers es una API publica de
            // Light2D, funciona igual en editor y en build. La version anterior con
            // SerializedObject se compilaba fuera del build, dejando sin capas asignadas a todas
            // las luces ambiente (pantalla negra en un player).
            luz.targetSortingLayers = ids.ToArray();
        }

        // Excluye "Default": si algun sprite queda sin reasignar de esa capa (la que trae Unity
        // por defecto, no una de las cinco del juego), no lo deja negro sin aviso cuando General
        // esta en 0 como en Noche.asset.
        static string[] TodasLasCapas()
        {
            var capas = SortingLayer.layers;
            var nombres = new List<string>(capas.Length);
            foreach (var capa in capas)
                if (capa.name != "Default")
                    nombres.Add(capa.name);
            return nombres.ToArray();
        }

        void DestruirHijos()
        {
            foreach (var hijo in hijos)
                if (hijo != null)
                {
                    if (Application.isPlaying)
                        Destroy(hijo);
                    else
                        DestroyImmediate(hijo);
                }
            hijos.Clear();
            luces.Clear();
        }

        void OnDisable()
        {
            DestruirHijos();
        }
    }
}
