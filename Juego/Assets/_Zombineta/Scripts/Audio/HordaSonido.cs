using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Audio
{
    /// <summary>
    /// Loop posicional en el frente de la horda (HordeX), por el bus Efectos. El paneo y la
    /// atenuacion salen de SonidoEspacial con dx = HordeX - PlayerX en metros (la horda casi
    /// siempre esta atras: panea a la izquierda). Vive en su propio objeto, sin hijos, dentro
    /// del prefab SonidoDeNivel: nunca en "Nivel" (ver la trampa de HordeGlow). No mueve su
    /// transform: no hace falta, el paneo ya sigue a la horda por audio.
    ///
    /// En la pelea a pie el loop sigue al zombie mas cercano a la protagonista (HordaCercana).
    /// Y con la horda cerca, cada tanto se le escapa un gemido suelto (ZombieAdelante) desde
    /// donde viene: el loop solo es un colchon, los gemidos le dan cuerpo.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(FuenteConBus))]
    public sealed class HordaSonido : MonoBehaviour
    {
        [SerializeField] RunController run;

        AudioSource source;
        FuenteConBus fuente;
        float volumenBase = 1f;

        [Header("Gemidos sueltos")]
        [Tooltip("Segundos entre dos gemidos de la horda (al azar entre los dos).")]
        [SerializeField] Vector2 gemidoCada = new Vector2(2.5f, 6f);
        [Tooltip("Atenuacion espacial (0-1) desde la que la horda gime: mas cerca, mas seguido no; solo que se oye.")]
        [Range(0f, 1f)] [SerializeField] float gemidoDesde = 0.25f;

        float proximoGemido;

        // Si arranco antes que el AudioDirector (Play desde la escena del nivel), se reintenta.
        bool clipResuelto;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            fuente = GetComponent<FuenteConBus>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            fuente.bus = AudioBus.Efectos;

            if (run == null)
                run = FindAnyObjectByType<RunController>();
        }

        void OnEnable()
        {
            ResolverClip();
            fuente.volumenPropio = volumenBase;
            if (source.clip != null && !source.isPlaying)
                source.Play();
        }

        void ResolverClip()
        {
            var audio = AudioDirector.Instance;
            clipResuelto = audio != null;
            var sonido = audio != null ? audio.Resolver(SonidoClave.Horda) : null;
            var clip = sonido != null && sonido.clips != null && sonido.clips.Length > 0 ? sonido.clips[0] : null;
            if (clip != null && source.clip != clip)
                source.clip = clip;
            volumenBase = SonidoVolumen.VolumenBase(sonido);
        }

        void Update()
        {
            var audio = AudioDirector.Instance;
            if (run == null || run.Sim == null || audio == null)
                return;

            if (!clipResuelto)
                OnEnable();

            var sim = run.Sim;
            var state = sim.State;
            bool enPelea = state.Phase == RunPhase.Pelea && sim.Pelea != null;
            float oyente = enPelea ? state.PeatonX : state.PlayerX;
            // Ya en metros: nada de conversion de mundo.
            float? dx = HordaCercana.Dx(enPelea, oyente, state.HordeX, sim.Horde.Units,
                enPelea ? sim.Pelea.Activo : (System.Func<int, bool>)null);
            var cfg = audio.Espacial;
            if (!dx.HasValue)
            {
                fuente.espacial = 0f; // pelea sin zombies vivos: la horda calla
                return;
            }
            source.panStereo = SonidoEspacial.Pan(dx.Value, cfg);
            fuente.espacial = SonidoEspacial.Volumen(dx.Value, cfg);

            bool corre = state.Phase == RunPhase.Running || enPelea;
            if (corre && fuente.espacial >= gemidoDesde && Time.time >= proximoGemido)
            {
                audio.Play(SonidoClave.ZombieAdelante, run.ToWorldX(oyente + dx.Value));
                proximoGemido = Time.time + Random.Range(gemidoCada.x, gemidoCada.y);
            }
        }
    }
}
