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
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(FuenteConBus))]
    public sealed class HordaSonido : MonoBehaviour
    {
        [SerializeField] RunController run;

        AudioSource source;
        FuenteConBus fuente;

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
            if (source.clip != null && !source.isPlaying)
                source.Play();
        }

        void ResolverClip()
        {
            var audio = AudioDirector.Instance;
            var sonido = audio != null ? audio.Resolver(SonidoClave.Horda) : null;
            var clip = sonido != null && sonido.clips != null && sonido.clips.Length > 0 ? sonido.clips[0] : null;
            if (clip != null && source.clip != clip)
                source.clip = clip;
        }

        void Update()
        {
            var audio = AudioDirector.Instance;
            if (run == null || run.Sim == null || audio == null)
                return;

            var state = run.Sim.State;
            float dx = state.HordeX - state.PlayerX; // ya en metros: nada de conversion de mundo.
            var cfg = audio.Espacial;
            source.panStereo = SonidoEspacial.Pan(dx, cfg);
            fuente.espacial = SonidoEspacial.Volumen(dx, cfg);
        }
    }
}
