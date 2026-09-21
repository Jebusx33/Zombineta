using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Audio
{
    /// <summary>
    /// Vive en Boot y nunca se descarga. Un tema por pantalla (MusicaDelJuego) y el tema del
    /// nivel (AudioDeNivel), con fundido cruzado entre dos fuentes que alternan. Una tercera
    /// fuente de tension arranca sincronizada al tema del nivel y sigue la amenaza de la horda
    /// (Task 6, via SetAmenaza). MusicaRegla decide si hay que cruzar o seguir con lo que sonaba.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class MusicDirector : MonoBehaviour
    {
        // Adelanto del PlayScheduled: alcanza para que el motor de audio programe la fuente y
        // deja a la tension arrancar en el mismo dspTime exacto que el tema del nivel.
        const double AdelantoProgramado = 0.1;

        // Si no hay Musica del juego asignada (Task 7 la crea), el fundido cruzado por defecto.
        const float FundidoPorDefecto = 1.5f;

        const float ToleranciaDuracionTension = 0.05f;
        const float SuavizadoAmenaza = 0.5f;

        [SerializeField] MusicaDelJuego musica;

        public static MusicDirector Instance { get; private set; }

        /// <summary>Debug/Play: asigna la Musica del juego en tiempo de ejecucion (Boot la deja vacia hasta la Task 7).</summary>
        public MusicaDelJuego DebugMusica
        {
            get => musica;
            set => musica = value;
        }

        /// <summary>Debug/Play: la fuente con el tema actualmente sonando (o el ultimo que sono), null si ninguna arranco.</summary>
        public AudioSource Actual => indiceActual >= 0 ? temaSources[indiceActual] : null;

        /// <summary>Debug/Play: la fuente de la capa de tension.</summary>
        public AudioSource Tension => sourceTension;

        FuenteConBus[] temaFuentes;
        AudioSource[] temaSources;
        int indiceActual = -1;

        FuenteConBus fuenteTension;
        AudioSource sourceTension;
        bool tensionActiva;
        bool apagandoTension;
        float amenazaObjetivo;
        float velocidadAmenaza;

        object actualClip;
        Coroutine fundidoRoutine;
        GameFlow flujoSuscrito;

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

            CrearFuentes();

            flujoSuscrito = GameRoot.Flow;
            if (flujoSuscrito != null)
                flujoSuscrito.Changed += OnChanged;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (flujoSuscrito != null)
                flujoSuscrito.Changed -= OnChanged;
        }

        void Start()
        {
            // GameRoot.Start (orden -1000) ya corrio JumpTo a la pantalla inicial sin avisar por
            // Changed: hay que resolver esa pantalla a mano aca.
            var flow = GameRoot.Flow;
            if (flow != null)
                Aplicar(flow.Current);
        }

        void Update()
        {
            // Mientras se apaga (fundido de salida), solo Fundir escribe volumenPropio: si el
            // SmoothDamp tambien escribiera, competiria con el fundido y la tension no llegaria a 0.
            if (!tensionActiva || apagandoTension)
                return;
            fuenteTension.volumenPropio = Mathf.SmoothDamp(fuenteTension.volumenPropio, amenazaObjetivo,
                ref velocidadAmenaza, SuavizadoAmenaza, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        /// <summary>La llama el nivel (Task 6) cada cuadro con 0 (lejos) a 1 (encima de la moto).</summary>
        public void SetAmenaza(float amenaza01) => amenazaObjetivo = Mathf.Clamp01(amenaza01);

        void CrearFuentes()
        {
            temaFuentes = new FuenteConBus[2];
            temaSources = new AudioSource[2];
            temaFuentes[0] = CrearFuente("TemaA", out temaSources[0]);
            temaFuentes[1] = CrearFuente("TemaB", out temaSources[1]);
            fuenteTension = CrearFuente("Tension", out sourceTension);
        }

        FuenteConBus CrearFuente(string nombre, out AudioSource source)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);

            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;

            var fuente = go.AddComponent<FuenteConBus>();
            fuente.bus = AudioBus.Musica;
            fuente.volumenPropio = 0f;
            return fuente;
        }

        void OnChanged(GameScreen from, GameScreen to) => Aplicar(to);

        void Aplicar(GameScreen pantalla)
        {
            AudioClip clip = ClipPara(pantalla, out AudioDeNivel nivelParaTension);
            if (MusicaRegla.Decidir(actualClip, clip) == MusicaAccion.Seguir)
                return;

            Cruzar(clip, pantalla, nivelParaTension);
        }

        AudioClip ClipPara(GameScreen pantalla, out AudioDeNivel nivelParaTension)
        {
            nivelParaTension = null;

            if (pantalla == GameScreen.Playing)
            {
                nivelParaTension = NivelActual();
                return nivelParaTension != null ? nivelParaTension.musica : null;
            }
            if (pantalla == GameScreen.Paused)
                return null; // la pausa no cambia el tema, solo lo atenua el bus.
            if (pantalla == GameScreen.Options && PausaDebajo())
                return null; // opciones abiertas desde la pausa: el nivel sigue debajo, no cambia el tema.

            return musica != null ? musica.Para(pantalla) : null;
        }

        static AudioDeNivel NivelActual()
        {
            var root = GameRoot.Instance;
            var nivel = root != null ? root.CurrentLevel : null;
            return nivel != null ? nivel.audio as AudioDeNivel : null;
        }

        /// <summary>
        /// Mismo criterio que AudioDirector.EstaPausado: la pantalla actual es Paused, o la
        /// escena de pausa sigue cargada debajo de Opciones (Opciones abierta desde la pausa).
        /// </summary>
        static bool PausaDebajo()
        {
            var flow = GameRoot.Flow;
            if (flow != null && flow.Current == GameScreen.Paused)
                return true;
            return SceneManager.GetSceneByName(SceneNames.Pause).isLoaded;
        }

        void Cruzar(AudioClip clip, GameScreen pantalla, AudioDeNivel nivelParaTension)
        {
            int indiceEntrante = indiceActual == 0 ? 1 : 0;
            var entrante = temaFuentes[indiceEntrante];
            var entranteSource = temaSources[indiceEntrante];

            FuenteConBus saliente = indiceActual >= 0 ? temaFuentes[indiceActual] : null;
            AudioSource salienteSource = indiceActual >= 0 ? temaSources[indiceActual] : null;

            double horaInicio = AudioSettings.dspTime + AdelantoProgramado;

            entranteSource.Stop();
            entranteSource.clip = clip;
            entrante.volumenPropio = 0f;
            entranteSource.PlayScheduled(horaInicio);

            bool debeIniciarTension = pantalla == GameScreen.Playing && nivelParaTension != null &&
                nivelParaTension.tension != null;
            bool apagarTension = tensionActiva && !debeIniciarTension;
            apagandoTension = apagarTension;

            if (debeIniciarTension)
                IniciarTension(nivelParaTension, horaInicio);

            float segundos = musica != null ? musica.fundido : FundidoPorDefecto;
            if (fundidoRoutine != null)
                StopCoroutine(fundidoRoutine);
            fundidoRoutine = StartCoroutine(Fundir(saliente, salienteSource, entrante, segundos, apagarTension));

            indiceActual = indiceEntrante;
            actualClip = clip;
        }

        void IniciarTension(AudioDeNivel nivel, double horaInicio)
        {
            if (nivel.musica != null &&
                Mathf.Abs(nivel.tension.length - nivel.musica.length) > ToleranciaDuracionTension)
            {
                Debug.LogWarning("MusicDirector: la tension de '" + nivel.name +
                    "' dura distinto que su tema (" + nivel.tension.length + " vs " + nivel.musica.length + " s).",
                    nivel);
            }

            sourceTension.Stop();
            sourceTension.clip = nivel.tension;
            fuenteTension.volumenPropio = 0f;
            sourceTension.PlayScheduled(horaInicio);

            tensionActiva = true;
            velocidadAmenaza = 0f;
            amenazaObjetivo = 0f; // que no herede la amenaza del nivel anterior: arranca en 0 y sigue a SetAmenaza.
        }

        IEnumerator Fundir(FuenteConBus saliente, AudioSource salienteSource, FuenteConBus entrante, float segundos,
            bool apagarTension)
        {
            float desdeSaliente = saliente != null ? saliente.volumenPropio : 0f;
            float desdeTension = apagarTension ? fuenteTension.volumenPropio : 0f;

            for (float t = 0f; t < segundos; t += Time.unscaledDeltaTime)
            {
                float f = Mathf.Clamp01(t / segundos);
                if (saliente != null)
                    saliente.volumenPropio = Mathf.Lerp(desdeSaliente, 0f, f);
                entrante.volumenPropio = f;
                if (apagarTension)
                    fuenteTension.volumenPropio = Mathf.Lerp(desdeTension, 0f, f);
                yield return null;
            }

            if (saliente != null)
            {
                saliente.volumenPropio = 0f;
                salienteSource.Stop();
            }
            entrante.volumenPropio = 1f;

            if (apagarTension)
            {
                fuenteTension.volumenPropio = 0f;
                sourceTension.Stop();
                tensionActiva = false;
                apagandoTension = false;
            }

            fundidoRoutine = null;
        }
    }
}
