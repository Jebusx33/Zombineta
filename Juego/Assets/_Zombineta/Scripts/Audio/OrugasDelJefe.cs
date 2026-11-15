using UnityEngine;
using Zombineta.Core;
using Zombineta.Jefe;

namespace Zombineta.Audio
{
    /// <summary>
    /// El ruido mecanico del jefe rodando (boss_tank), en loop mientras dura el duelo. Suena desde donde esta
    /// el jefe (detras de la moto, y adelante cuando la embestida la pasa) y cambia con lo que hace: acelera de
    /// a poco en el aviso de la embestida, va a fondo mientras embiste, afloja al volver y casi se para cuando
    /// queda aturdido. Al caer derrotado se apaga con un fundido (el quejido lo pone JefeDerrotado). Vive en el
    /// prefab SonidoDelNivel; sin clip en el banco no suena.
    /// Tambien el jadeo (boss_tired, JefeCansado): arranca cuando el jefe queda aturdido, que es cuando se le
    /// puede pegar, y se apaga con un fundido corto cuando se recupera, aunque el archivo dure mas.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class OrugasDelJefe : MonoBehaviour
    {
        [SerializeField] RunController run;
        [Tooltip("Cuanto tarda el tono en llegar a lo que pide la fase (por segundo de diferencia).")]
        [SerializeField] float rapidezTono = 3f;
        [Tooltip("Cuanto tarda el volumen en llegar a lo que pide la fase (por segundo de diferencia).")]
        [SerializeField] float rapidezVolumen = 3f;
        [Tooltip("Cuanto lo baja la distancia (0 = nada, 1 = como cualquier sonido posicional). El jefe esta siempre " +
                 "a unos 11 m: atenuado del todo quedaba al 73 % y se perdia.")]
        [Range(0f, 1f)] [SerializeField] float cuantoAtenua = 0.4f;
        [Tooltip("Cuanto se va al costado (1 = paneo completo). Del todo a la izquierda se oia por un solo oido.")]
        [Range(0f, 1f)] [SerializeField] float paneo = 0.5f;
        [Tooltip("Segundos en que se apaga el jadeo cuando el jefe deja de estar aturdido.")]
        [SerializeField] float fundidoJadeo = 0.3f;
        [Tooltip("Cuando el jefe dispara, las orugas bajan a esto y vuelven solas: que el disparo se oiga.")]
        [Range(0f, 1f)] [SerializeField] float agacheAlDisparar = 0.3f;
        [Tooltip("Segundos en que las orugas vuelven a su volumen despues del disparo.")]
        [SerializeField] float vueltaDelAgache = 0.7f;

        AudioSource source;
        FuenteConBus fuente;
        float volumenBase = 1f;
        bool resuelto, sonando;
        float tono = 1f, volumen;
        float agache = 1f;

        AudioSource jadeo;
        FuenteConBus fuenteJadeo;
        float volumenJadeo = 1f;
        bool jadeando;

        /// <summary>Regla pura: el jadeo suena mientras el jefe esta aturdido y la partida sigue.</summary>
        public static bool Jadea(FaseDeJefe fase, RunPhase partida) =>
            fase == FaseDeJefe.Aturdido && partida == RunPhase.Running;

        /// <summary>Tono (x) y volumen de 0 a 1 (y) de las orugas en cada momento del duelo.</summary>
        public static Vector2 Ritmo(FaseDeJefe fase, float tiempoFase, float segundosAviso)
        {
            switch (fase)
            {
                case FaseDeJefe.Inactivo:
                case FaseDeJefe.Derrotado:
                    return new Vector2(1f, 0f);
                case FaseDeJefe.AvisoEmbestida:
                    // Se acelera en el lugar: el motor sube de tono y llega al volumen pleno mientras avisa
                    // (encima, Rugido le suma los acelerones).
                    float k = segundosAviso > 0f ? Mathf.Clamp01(tiempoFase / segundosAviso) : 1f;
                    return new Vector2(Mathf.Lerp(1f, 1.45f, k), Mathf.Lerp(0.6f, 1f, k));
                case FaseDeJefe.Embestida:
                    // A fondo: casi el doble de agudo y unos 4 dB mas fuerte que rodando normal.
                    return new Vector2(1.8f, 1f);
                case FaseDeJefe.Regreso:
                    return new Vector2(0.9f, 0.6f);
                case FaseDeJefe.Aturdido:
                    return new Vector2(0.75f, 0.4f);
                case FaseDeJefe.Entrada:
                    return new Vector2(1.05f, 0.7f);
                default:
                    return new Vector2(1f, 0.6f);
            }
        }

        /// <summary>
        /// Los acelerones del aviso de embestida, sumados al tono: tres tirones del motor, cada uno mas fuerte,
        /// como quien acelera parado antes de largar. Fuera del aviso, 0.
        /// </summary>
        public static float Rugido(FaseDeJefe fase, float tiempoFase, float segundosAviso)
        {
            if (fase != FaseDeJefe.AvisoEmbestida || segundosAviso <= 0f)
                return 0f;
            float k = Mathf.Clamp01(tiempoFase / segundosAviso);
            float tiron = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)); // tres tirones en el aviso
            return tiron * Mathf.Lerp(0.12f, 0.3f, k);
        }

        void OnEnable()
        {
            resuelto = false;
            if (run != null)
                run.Stepped += AlPasar;
        }

        void AlPasar(RunEvent ev)
        {
            if ((ev & RunEvent.JefeDispara) != 0)
                agache = agacheAlDisparar;
        }

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            fuente = gameObject.AddComponent<FuenteConBus>();
            fuente.volumenPropio = 0f;

            // El jadeo va en un hijo: FuenteConBus maneja la AudioSource de su propio objeto.
            var go = new GameObject("Jadeo");
            go.transform.SetParent(transform, false);
            jadeo = go.AddComponent<AudioSource>();
            jadeo.loop = false;
            jadeo.playOnAwake = false;
            jadeo.spatialBlend = 0f;
            fuenteJadeo = go.AddComponent<FuenteConBus>();
            fuenteJadeo.volumenPropio = 0f;
        }

        void OnDisable()
        {
            if (run != null)
                run.Stepped -= AlPasar;
            source.Stop();
            sonando = false;
            volumen = 0f;
            jadeo.Stop();
            jadeando = false;
        }

        void Resolver()
        {
            var audio = AudioDirector.Instance;
            if (audio == null)
                return;
            resuelto = true;
            var sonido = audio.Resolver(SonidoClave.JefeOrugas);
            source.clip = sonido != null && sonido.clips != null && sonido.clips.Length > 0 ? sonido.clips[0] : null;
            fuente.bus = sonido != null ? sonido.bus : AudioBus.Efectos;
            volumenBase = SonidoVolumen.VolumenBase(sonido);

            var cansado = audio.Resolver(SonidoClave.JefeCansado);
            jadeo.clip = cansado != null && cansado.clips != null && cansado.clips.Length > 0 ? cansado.clips[0] : null;
            fuenteJadeo.bus = cansado != null ? cansado.bus : AudioBus.Efectos;
            volumenJadeo = SonidoVolumen.VolumenBase(cansado);
        }

        void Jadear(FaseDeJefe fase, RunPhase partida, float dt)
        {
            if (jadeo.clip == null)
                return;
            if (Jadea(fase, partida))
            {
                if (!jadeando)
                {
                    jadeando = true;
                    jadeo.time = 0f;
                    jadeo.Play();
                }
                fuenteJadeo.volumenPropio = volumenJadeo;
                return;
            }
            if (!jadeando)
                return;
            fuenteJadeo.volumenPropio = Mathf.MoveTowards(fuenteJadeo.volumenPropio, 0f,
                volumenJadeo * dt / Mathf.Max(0.01f, fundidoJadeo));
            if (fuenteJadeo.volumenPropio <= 0f)
            {
                jadeo.Stop();
                jadeando = false;
            }
        }

        void Update()
        {
            if (!resuelto)
                Resolver();
            var audio = AudioDirector.Instance;
            var jefe = run != null && run.Sim != null ? run.Sim.Jefe : null;
            if (source.clip == null || audio == null || jefe == null)
                return;

            Jadear(jefe.Fase, run.Sim.State.Phase, Time.unscaledDeltaTime);

            var cfg = run.Config != null ? run.Config.jefe : null;
            var objetivo = Ritmo(jefe.Fase, jefe.TiempoFase, cfg != null ? cfg.avisoEmbestida : 1f);
            // Ganada o perdida la partida, se apagan aunque el jefe siga en su fase hasta que se descargue el nivel.
            var fasePartida = run.Sim.State.Phase;
            if (fasePartida == RunPhase.Lost || fasePartida == RunPhase.Won)
                objetivo.y = 0f;
            float dt = Time.unscaledDeltaTime;
            tono = Mathf.MoveTowards(tono, objetivo.x, rapidezTono * dt);
            volumen = Mathf.MoveTowards(volumen, objetivo.y, rapidezVolumen * dt);

            // Desde donde esta el jefe, oido desde la moto.
            float dx = AudioDirector.Espaciado(run.ToWorldX(jefe.X), audio.PlayerWorldX, audio.WorldUnitsPerMeter);
            fuente.espacial = Mathf.Lerp(1f, SonidoEspacial.Volumen(dx, audio.Espacial), cuantoAtenua);
            source.panStereo = SonidoEspacial.Pan(dx, audio.Espacial) * paneo;
            source.pitch = tono + Rugido(jefe.Fase, jefe.TiempoFase, cfg != null ? cfg.avisoEmbestida : 1f);
            agache = Mathf.MoveTowards(agache, 1f, dt / Mathf.Max(0.01f, vueltaDelAgache));
            fuente.volumenPropio = volumenBase * volumen * agache;

            bool suena = volumen > 0.0001f;
            if (suena == sonando)
                return;
            sonando = suena;
            if (suena)
            {
                source.time = Random.Range(0f, source.clip.length * 0.9f);
                source.Play();
            }
            else
                source.Stop();
        }
    }
}
