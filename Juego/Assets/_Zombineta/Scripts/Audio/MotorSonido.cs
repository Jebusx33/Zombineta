using System.Collections;
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Audio
{
    /// <summary>
    /// Loop del motor de la moto, por el bus Efectos. El tono persigue a MotorTono.Objetivo con
    /// un suavizado de 0,15 s; sin nafta el volumen cae a 0 en 1 s; al perder se apaga con un
    /// fundido de 0,3 s y se detiene. Arranca al activarse (la escena del nivel se carga al
    /// entrar a Playing). Hijo del prefab SonidoDeNivel.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(FuenteConBus))]
    public sealed class MotorSonido : MonoBehaviour
    {
        const float SuavizadoPitch = 0.15f;
        const float FundidoSinNafta = 1f;
        const float FundidoAlPerder = 0.3f;

        [SerializeField] RunController run;

        AudioSource source;
        FuenteConBus fuente;
        float velocidadPitch;
        bool apagandoAlPerder;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            fuente = GetComponent<FuenteConBus>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            fuente.bus = AudioBus.Efectos;
            fuente.espacial = 1f;

            if (run == null)
                run = FindAnyObjectByType<RunController>();
        }

        void OnEnable()
        {
            if (run != null)
                run.Stepped += OnStepped;

            ResolverClip();
            apagandoAlPerder = false;
            fuente.volumenPropio = 1f;
            if (source.clip != null && !source.isPlaying)
                source.Play();
        }

        void OnDisable()
        {
            if (run != null)
                run.Stepped -= OnStepped;
            StopAllCoroutines();
            apagandoAlPerder = false;
        }

        void ResolverClip()
        {
            var audio = AudioDirector.Instance;
            var sonido = audio != null ? audio.Resolver(SonidoClave.Motor) : null;
            var clip = sonido != null && sonido.clips != null && sonido.clips.Length > 0 ? sonido.clips[0] : null;
            if (clip != null && source.clip != clip)
                source.clip = clip;
        }

        void Update()
        {
            if (run == null || run.Sim == null)
                return;

            var state = run.Sim.State;
            bool turbo = state.Mode == DriveMode.Turbo;
            float pitchObjetivo = MotorTono.Objetivo(run.Sim.PlayerSpeed, run.Config.normalSpeed, turbo);
            source.pitch = Mathf.SmoothDamp(source.pitch, pitchObjetivo, ref velocidadPitch,
                SuavizadoPitch, Mathf.Infinity, Time.unscaledDeltaTime);

            if (apagandoAlPerder)
                return; // el fundido de Lost es dueno exclusivo de volumenPropio mientras dura.

            float volumenObjetivo = state.Fuel <= 0f ? 0f : 1f;
            fuente.volumenPropio = Mathf.MoveTowards(fuente.volumenPropio, volumenObjetivo,
                Time.unscaledDeltaTime / FundidoSinNafta);
        }

        void OnStepped(RunEvent events)
        {
            if ((events & RunEvent.Lost) != 0 && !apagandoAlPerder)
                StartCoroutine(ApagarAlPerder());
        }

        IEnumerator ApagarAlPerder()
        {
            apagandoAlPerder = true;
            float desde = fuente.volumenPropio;
            for (float t = 0f; t < FundidoAlPerder; t += Time.unscaledDeltaTime)
            {
                fuente.volumenPropio = Mathf.Lerp(desde, 0f, t / FundidoAlPerder);
                yield return null;
            }
            fuente.volumenPropio = 0f;
            source.Stop();
        }
    }
}
