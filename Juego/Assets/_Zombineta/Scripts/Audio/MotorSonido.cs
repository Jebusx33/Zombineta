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
    ///
    /// En capas, si hay clips: MotorRalenti suena con la moto quieta y se cruza con la marcha
    /// (Motor) al acelerar; MotorCambio suena una vez al entrar en turbo. Sin ralenti, la marcha
    /// suena sola como siempre.
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
        float volumenBase = 1f;

        // Ralenti: una segunda fuente hija, creada al arrancar (no hace falta tocar el prefab).
        AudioSource ralenti;
        FuenteConBus fuenteRalenti;
        float volumenRalenti;

        // 0-1: cuanto deja sonar el motor la nafta y el fundido de Lost (las dos capas a la vez).
        float apertura = 1f;
        DriveMode modoPrevio;

        // Si el motor arranco antes que el AudioDirector (Play desde la escena del nivel), no
        // tenia de donde sacar los clips: se reintenta apenas aparece.
        bool clipsResueltos;

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

            var go = new GameObject("Ralenti");
            go.transform.SetParent(transform, false);
            ralenti = go.AddComponent<AudioSource>();
            ralenti.loop = true;
            ralenti.playOnAwake = false;
            ralenti.spatialBlend = 0f;
            fuenteRalenti = go.AddComponent<FuenteConBus>();
            fuenteRalenti.bus = AudioBus.Efectos;
            fuenteRalenti.espacial = 1f;
            fuenteRalenti.volumenPropio = 0f;
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
            apertura = 1f;
            fuente.volumenPropio = volumenBase;
            if (source.clip != null && !source.isPlaying)
                source.Play();
            if (ralenti.clip != null && !ralenti.isPlaying)
                ralenti.Play();
        }

        void OnRestarted() => ReiniciarMotor();

        void ResolverClip()
        {
            var audio = AudioDirector.Instance;
            clipsResueltos = audio != null;
            var sonido = audio != null ? audio.Resolver(SonidoClave.Motor) : null;
            var clip = sonido != null && sonido.clips != null && sonido.clips.Length > 0 ? sonido.clips[0] : null;
            if (clip != null && source.clip != clip)
                source.clip = clip;
            volumenBase = SonidoVolumen.VolumenBase(sonido);

            var sonidoRalenti = audio != null ? audio.Resolver(SonidoClave.MotorRalenti) : null;
            var clipRalenti = sonidoRalenti != null && sonidoRalenti.clips != null && sonidoRalenti.clips.Length > 0
                ? sonidoRalenti.clips[0] : null;
            if (ralenti.clip != clipRalenti)
            {
                ralenti.Stop();
                ralenti.clip = clipRalenti;
            }
            volumenRalenti = SonidoVolumen.VolumenBase(sonidoRalenti);
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

            // El tutorial deshace un Lost (la horda nunca gana ahi): si la partida volvio a
            // Running sin pasar por Restarted, el motor vuelve a sonar. En un nivel normal Lost
            // no vuelve nunca a Running sin Restarted, asi que esto no cambia nada ahi.
            if (DebeRecuperarse(apagandoAlPerder, state.Phase) ||
                (!clipsResueltos && !apagandoAlPerder && AudioDirector.Instance != null))
                ReiniciarMotor();

            // El cambio suena una vez al pasar a turbo, con el motor andando.
            if (state.Mode == DriveMode.Turbo && modoPrevio != DriveMode.Turbo && state.Fuel > 0f &&
                state.Phase == RunPhase.Running && AudioDirector.Instance != null)
                AudioDirector.Instance.Play(SonidoClave.MotorCambio);
            modoPrevio = state.Mode;

            // El fundido de Lost es dueno exclusivo de la apertura mientras dura.
            if (!apagandoAlPerder)
                apertura = Mathf.MoveTowards(apertura, state.Fuel <= 0f ? 0f : 1f,
                    Time.unscaledDeltaTime / m.fundidoSinNafta);

            float r = ralenti.clip != null ? MotorTono.Ralenti(run.Sim.PlayerSpeed, run.Config.normalSpeed) : 0f;
            fuente.volumenPropio = volumenBase * apertura * (1f - r);
            fuenteRalenti.volumenPropio = volumenRalenti * apertura * r;
        }

        /// <summary>Si el motor quedo apagado por un Lost y la partida ya volvio a correr.</summary>
        public static bool DebeRecuperarse(bool apagandoAlPerder, RunPhase fase) =>
            apagandoAlPerder && fase == RunPhase.Running;

        void OnStepped(RunEvent events)
        {
            if ((events & RunEvent.Lost) != 0 && !apagandoAlPerder)
                StartCoroutine(ApagarAlPerder());
        }

        IEnumerator ApagarAlPerder()
        {
            apagandoAlPerder = true;
            float desde = apertura;
            float duracion = AudioDirector.Mezcla.fundidoAlPerder;
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                apertura = Mathf.Lerp(desde, 0f, t / duracion);
                yield return null;
            }
            apertura = 0f;
            source.Stop();
            ralenti.Stop();
        }
    }
}
