using UnityEngine;
using Zombineta.Clima;
using Zombineta.Core;

namespace Zombineta.Audio
{
    /// <summary>
    /// Loops que suben y bajan con lo que pasa en la partida: la lluvia y el viento siguen al clima
    /// del tramo (ClimaDirector.Lluvia y Viento, de 0 a 1) y el chisporroteo del faro suena mientras
    /// titila con la bateria baja. Cada loop arma su propia fuente al arrancar, como hijo de este
    /// objeto, asi el prefab SonidoDeNivel solo necesita este componente. Sin clip en el banco, ese
    /// loop no suena. Entra y sale con un fundido corto; con el volumen en 0 la fuente se para.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class LoopsDeNivel : MonoBehaviour
    {
        const float SegundosDeFundido = 0.6f;

        /// <summary>Los loops largos arrancan en un punto al azar: cada nivel no empieza con el mismo trueno.</summary>
        const float LargoParaArrancarAlAzar = 5f;

        [SerializeField] RunController run;

        [Tooltip("Fraccion de bateria por debajo de la cual el faro titila (la misma que HeadlightView.flickerBelow).")]
        [Range(0f, 1f)] [SerializeField] float faroTitilaDebajoDe = 0.25f;

        [Tooltip("Como sigue el volumen de la lluvia a su intensidad: 1 = en proporcion; menos de 1 = una lluvia " +
                 "media ya se oye fuerte (0,5: con la mitad de lluvia suena al 71 %). Con lluvia plena es igual.")]
        [Range(0.2f, 1f)] [SerializeField] float curvaLluvia = 0.5f;

        sealed class Loop
        {
            public SonidoClave clave;
            public float curva = 1f;
            public AudioSource source;
            public FuenteConBus fuente;
            public float volumenBase;
            public bool resuelto;
            /// <summary>Si se le dio Play. No se mira isPlaying: con el audio en pausa da false y el loop se reiniciaria cada cuadro.</summary>
            public bool sonando;
        }

        Loop lluvia, viento, faro;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();
            lluvia = Crear(SonidoClave.Lluvia, "Lluvia");
            lluvia.curva = curvaLluvia;
            viento = Crear(SonidoClave.Viento, "Viento");
            faro = Crear(SonidoClave.FaroTitila, "Faro titila");
        }

        Loop Crear(SonidoClave clave, string nombre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var fuente = go.AddComponent<FuenteConBus>();
            fuente.volumenPropio = 0f;
            fuente.espacial = 1f;
            return new Loop { clave = clave, source = source, fuente = fuente };
        }

        void OnEnable()
        {
            // El banco del nivel se engancha en el OnEnable de SonidoDeNivel: se resuelve en el primer Update.
            foreach (var l in new[] { lluvia, viento, faro })
            {
                l.resuelto = false;
                l.fuente.volumenPropio = 0f;
            }
        }

        void OnDisable()
        {
            foreach (var l in new[] { lluvia, viento, faro })
            {
                l.source.Stop();
                l.sonando = false;
            }
        }

        static void Resolver(Loop l)
        {
            var audio = AudioDirector.Instance;
            if (audio == null)
                return;
            l.resuelto = true;
            var sonido = audio.Resolver(l.clave);
            var clip = sonido != null && sonido.clips != null && sonido.clips.Length > 0 ? sonido.clips[0] : null;
            l.source.clip = clip;
            l.fuente.bus = sonido != null ? sonido.bus : AudioBus.Ambiente;
            l.volumenBase = SonidoVolumen.VolumenBase(sonido);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Avanzar(lluvia, ClimaDirector.Lluvia, dt);
            Avanzar(viento, ClimaDirector.Viento, dt);
            Avanzar(faro, FaroTitilando() ? 1f : 0f, dt);
        }

        bool FaroTitilando()
        {
            if (run == null || run.Sim == null)
                return false;
            var s = run.Sim.State;
            float carga = run.Config.batteryMax > 0f ? s.Battery / run.Config.batteryMax : 0f;
            return FaroTitila(s.Phase == RunPhase.Running, s.HeadlightOn, carga, faroTitilaDebajoDe);
        }

        /// <summary>
        /// Regla pura: el volumen de un loop con esa intensidad (0 a 1). La curva es un exponente: con 1 va en
        /// proporcion; con menos, las intensidades medias suenan mas fuerte y la plena queda igual.
        /// </summary>
        public static float VolumenDeLoop(float volumenBase, float intensidad, float curva)
        {
            float i = Mathf.Clamp01(intensidad);
            return volumenBase * (i <= 0f ? 0f : Mathf.Pow(i, Mathf.Max(0.01f, curva)));
        }

        /// <summary>Regla pura: el faro chisporrotea en carrera, prendido y con la bateria baja pero no vacia.</summary>
        public static bool FaroTitila(bool corriendo, bool prendido, float carga, float debajoDe) =>
            corriendo && prendido && carga > 0f && carga < debajoDe;

        static void Avanzar(Loop l, float intensidad, float dt)
        {
            if (!l.resuelto)
                Resolver(l);
            if (l.source.clip == null)
                return;

            float objetivo = VolumenDeLoop(l.volumenBase, intensidad, l.curva);
            l.fuente.volumenPropio = Mathf.MoveTowards(l.fuente.volumenPropio, objetivo, dt / SegundosDeFundido);

            bool suena = l.fuente.volumenPropio > 0.0001f;
            if (suena == l.sonando)
                return;
            l.sonando = suena;
            if (suena)
            {
                float largo = l.source.clip.length;
                l.source.time = largo > LargoParaArrancarAlAzar ? Random.Range(0f, largo * 0.9f) : 0f;
                l.source.Play();
            }
            else
                l.source.Stop();
        }
    }
}
