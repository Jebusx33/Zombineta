using System.Collections;
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Audio
{
    /// <summary>
    /// Loop del motor de la moto, por el bus Efectos. El tono persigue a MotorTono.Objetivo con
    /// un suavizado; sin nafta el volumen cae a 0; al perder se apaga con un fundido y se
    /// detiene. Tonos y tiempos salen de AudioDirector.Mezcla (Settings/Audio/Mezcla.asset). Arranca al activarse (la escena del nivel se carga al
    /// entrar a Playing). Tambien escucha RunController.Restarted (reintento o nivel nuevo, sin
    /// recargar la escena) para deshacer el apagado de Lost: si no, un reintento despues de
    /// perder dejaria el motor mudo para siempre. Hijo del prefab SonidoDeNivel.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(FuenteConBus))]
    public sealed class MotorSonido : MonoBehaviour
    {
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
            {
                run.Stepped += OnStepped;
                run.Restarted += OnRestarted;
            }

            ReiniciarMotor();
        }

        void OnDisable()
        {
            if (run != null)
            {
                run.Stepped -= OnStepped;
                run.Restarted -= OnRestarted;
            }
            StopAllCoroutines();
            apagandoAlPerder = false;
        }

        /// <summary>
        /// Deshace el apagado de Lost y vuelve a arrancar el loop: lo usan tanto OnEnable como
        /// Restarted (reintento o nivel nuevo sin recargar la escena, asi que OnEnable no vuelve
        /// a correr).
        /// </summary>
        void ReiniciarMotor()
        {
            StopAllCoroutines();
            ResolverClip();
            apagandoAlPerder = false;
            fuente.volumenPropio = 1f;
            if (source.clip != null && !source.isPlaying)
                source.Play();
        }

        void OnRestarted() => ReiniciarMotor();

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
            var m = AudioDirector.Mezcla;
            float pitchObjetivo = MotorTono.Objetivo(run.Sim.PlayerSpeed, run.Config.normalSpeed, turbo,
                m.tonoQuieta, m.tonoNormal, m.tonoTurbo);
            source.pitch = Mathf.SmoothDamp(source.pitch, pitchObjetivo, ref velocidadPitch,
                m.suavizadoTono, Mathf.Infinity, Time.unscaledDeltaTime);

            if (apagandoAlPerder)
                return; // el fundido de Lost es dueno exclusivo de volumenPropio mientras dura.

            float volumenObjetivo = state.Fuel <= 0f ? 0f : 1f;
            fuente.volumenPropio = Mathf.MoveTowards(fuente.volumenPropio, volumenObjetivo,
                Time.unscaledDeltaTime / m.fundidoSinNafta);
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
            float duracion = AudioDirector.Mezcla.fundidoAlPerder;
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                fuente.volumenPropio = Mathf.Lerp(desde, 0f, t / duracion);
                yield return null;
            }
            fuente.volumenPropio = 0f;
            source.Stop();
        }
    }
}
