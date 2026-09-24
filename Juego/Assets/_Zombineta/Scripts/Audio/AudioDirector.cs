using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombineta.Core;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Audio
{
    /// <summary>
    /// Vive en Boot y nunca se descarga. Dueno del pool de 16 fuentes, de los buses (Task 1) y
    /// del ducking de pausa. Resuelve cada Play contra el banco de nivel (pisa al global), elige
    /// variante y rango al azar, panea si es posicional y respeta el cooldown por Sonido.
    /// El global y el espacial quedan vacios: la Task 7 crea esos assets (con espacial null cae a
    /// 12/4/30 m).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class AudioDirector : MonoBehaviour
    {
        const int TamanioPool = 16;

        static readonly EspacialConfig EspacialPorDefecto =
            new EspacialConfig { anchoPaneo = 12f, distanciaPlena = 4f, distanciaMaxima = 30f };

        [SerializeField] BancoDeSonidos global;
        [SerializeField] EspacialAsset espacial;

        [Tooltip("Tono del motor, capa de tension, pausa y curva de volumen (Settings/Audio/Mezcla.asset).")]
        [SerializeField] MezclaAudio mezcla;

        public static AudioDirector Instance { get; private set; }

        static readonly AudioBuses fallback = new AudioBuses();

        /// <summary>Nunca null: si no hay director en escena, un AudioBuses por defecto (ganancia 1).</summary>
        public static AudioBuses Buses => Instance != null ? Instance.buses : fallback;

        /// <summary>Nunca null: el asset asignado, o los valores por defecto si falta.</summary>
        public static MezclaAudio Mezcla =>
            Instance != null && Instance.mezcla != null ? Instance.mezcla : MezclaAudio.PorDefecto;

        /// <summary>Banco del nivel actual. Lo pone el nivel al entrar; null fuera de partida.</summary>
        public BancoDeSonidos BancoNivel { get; set; }

        /// <summary>Posicion en X de la moto, en unidades de mundo. La actualiza el nivel cada cuadro.</summary>
        public float PlayerWorldX { get; set; }

        /// <summary>
        /// Cuantas unidades de mundo entran en un metro, para el paneo y la atenuacion espacial.
        /// El nivel lo pone al registrarse (Task 6); 1 por defecto (worldX ya en metros).
        /// </summary>
        public float WorldUnitsPerMeter { get; set; } = 1f;

        /// <summary>Los tres numeros de SonidoEspacial: del asset, o 12/4/30 si esta vacio.</summary>
        public EspacialConfig Espacial => espacial != null ? espacial.config : EspacialPorDefecto;

        /// <summary>Debug/Play: las 16 fuentes del pool (esten o no sonando en este instante).</summary>
        public AudioSource[] ActiveSources => sources;

        /// <summary>Debug/Play: asigna el banco global en tiempo de ejecucion (Boot lo deja vacio hasta la Task 7).</summary>
        public BancoDeSonidos DebugGlobal
        {
            get => global;
            set => global = value;
        }

        readonly AudioBuses buses = new AudioBuses();
        readonly Dictionary<Sonido, float> ultimoUso = new Dictionary<Sonido, float>();
        readonly Dictionary<Sonido, VariantPicker> pickers = new Dictionary<Sonido, VariantPicker>();

        FuenteConBus[] pool;
        AudioSource[] sources;
        float[] inicioSlot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ArmarPool();
            AplicarVolumenes();
            GameSettings.AudioSettingsChanged += AplicarVolumenes;
        }

        void OnDestroy()
        {
            if (Instance != this)
                return;
            Instance = null;
            GameSettings.AudioSettingsChanged -= AplicarVolumenes;
        }

        void Update()
        {
            bool pausado = EstaPausado();
            float objetivo = pausado ? 0f : 1f;
            var m = Mezcla;
            buses.MusicaEnPausa = m.musicaEnPausa;
            buses.ExponenteCurva = m.exponenteVolumen;
            buses.PausaDuck = Mathf.MoveTowards(buses.PausaDuck, objetivo, Time.unscaledDeltaTime / m.fundidoPausa);
        }

        void ArmarPool()
        {
            pool = new FuenteConBus[TamanioPool];
            sources = new AudioSource[TamanioPool];
            inicioSlot = new float[TamanioPool];

            for (int i = 0; i < TamanioPool; i++)
            {
                var go = new GameObject("Fuente" + i.ToString("00"));
                go.transform.SetParent(transform, false);

                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;

                pool[i] = go.AddComponent<FuenteConBus>();
                sources[i] = source;
            }
        }

        /// <summary>Copia los tres volumenes de GameSettings a los buses. Se llama en Awake y ante AudioSettingsChanged.</summary>
        public void AplicarVolumenes()
        {
            buses.General = GameSettings.Volume;
            buses.Musica = GameSettings.MusicVolume;
            buses.Efectos = GameSettings.SfxVolume;
        }

        /// <summary>
        /// Pausado si la pantalla actual es Paused, o si la escena de pausa sigue cargada debajo
        /// de Opciones (Opciones abierta desde la pausa). Se mira la escena directamente: es lo
        /// unico que el director de escenas ya deja consultar sin sumar API nueva a GameRoot.
        /// </summary>
        bool EstaPausado()
        {
            var flow = GameRoot.Flow;
            if (flow != null && flow.Current == GameScreen.Paused)
                return true;
            return SceneManager.GetSceneByName(SceneNames.Pause).isLoaded;
        }

        public void Play(SonidoClave clave, float? worldX = null) =>
            Play(BancoDeSonidos.Resolver(BancoNivel, global, clave), worldX);

        /// <summary>
        /// Resuelve una clave contra el banco de nivel y el global, sin reproducirla: la usan los
        /// loops propios (Motor, Horda), que Play() rechaza por ser loops.
        /// </summary>
        public Sonido Resolver(SonidoClave clave) => BancoDeSonidos.Resolver(BancoNivel, global, clave);

        public void Play(Sonido sonido, float? worldX = null)
        {
            if (sonido == null)
                return; // banco sin esa clave (o sin banco): no-op silencioso.

            if (sonido.loop)
            {
                Debug.LogWarning("AudioDirector.Play: '" + sonido.name +
                    "' es loop, lo maneja su propio componente en vez del pool.", sonido);
                return;
            }

            if (sonido.clips == null || sonido.clips.Length == 0)
                return;

            float ahora = Time.unscaledTime;
            if (ultimoUso.TryGetValue(sonido, out float ultimo) && ahora - ultimo < sonido.cooldown)
                return;

            if (!pickers.TryGetValue(sonido, out var picker))
            {
                // GetInstanceID esta obsoleto en esta version de Unity: GetHashCode() de un
                // Object tambien es estable y unico por instancia, y sirve igual de semilla.
                picker = new VariantPicker(sonido.GetHashCode());
                pickers[sonido] = picker;
            }
            int idx = picker.Pick(sonido.clips.Length);
            if (idx < 0)
                return;
            var clip = sonido.clips[idx];
            if (clip == null)
                return;

            float pan = 0f;
            float espacialVol = 1f;
            if (sonido.posicional && worldX.HasValue)
            {
                float metros = Mathf.Max(0.0001f, WorldUnitsPerMeter);
                float dx = (worldX.Value - PlayerWorldX) / metros;
                var cfg = Espacial;
                espacialVol = SonidoEspacial.Volumen(dx, cfg);
                if (espacialVol <= 0f)
                    return; // fuera de rango: no suena.
                pan = SonidoEspacial.Pan(dx, cfg);
            }

            ultimoUso[sonido] = ahora;

            int slot = ElegirSlot();
            var src = sources[slot];
            var fuente = pool[slot];

            src.clip = clip;
            src.loop = false;
            src.pitch = Random.Range(sonido.tono.x, sonido.tono.y);
            src.panStereo = pan;

            fuente.bus = sonido.bus;
            fuente.volumenPropio = Random.Range(sonido.volumen.x, sonido.volumen.y);
            fuente.espacial = espacialVol;

            inicioSlot[slot] = ahora;
            src.Play();
        }

        /// <summary>Una fuente libre; si no hay, la mas vieja que no sea loop.</summary>
        int ElegirSlot()
        {
            for (int i = 0; i < sources.Length; i++)
                if (!sources[i].isPlaying)
                    return i;

            int viejo = 0;
            float t = float.MaxValue;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].loop)
                    continue;
                if (inicioSlot[i] < t)
                {
                    t = inicioSlot[i];
                    viejo = i;
                }
            }
            return viejo;
        }
    }
}
