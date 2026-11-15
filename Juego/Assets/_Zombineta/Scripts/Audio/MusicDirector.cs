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

        /// <summary>Debug/Play: la fuente de la capa del jefe.</summary>
        public AudioSource CapaJefe => sourceJefe;

        FuenteConBus[] temaFuentes;
        AudioSource[] temaSources;
        int indiceActual = -1;

        FuenteConBus fuenteTension;
        AudioSource sourceTension;
        bool tensionActiva;
        bool apagandoTension;
        float amenazaObjetivo;
        float velocidadAmenaza;
        float topeTension = 1f;

        // Capa del jefe: se carga con el tema del nivel y arranca desde su comienzo cuando el jefe esta por
        // aparecer (anticipa su llegada), sube enseguida y se apaga despacio al derrotarlo.
        FuenteConBus fuenteJefe;
        AudioSource sourceJefe;
        bool jefeActivo;
        bool jefeSonando;
        const float SubidaJefe = 0.15f;
        float jefeObjetivo;
        float velocidadJefe;
        float topeJefe = 1f;
        const float SuavizadoJefe = 1.5f;

        // El tema del nivel baja de volumenTema a volumenTemaConJefe a medida que sube la capa del jefe.
        bool temaDeNivel;
        float volumenTema = 1f, volumenTemaConJefe = 1f;
        float jefeSuave, velocidadJefeSuave; // la intensidad del jefe, sin saltos (al derrotarlo baja de golpe)

        /// <summary>Volumen del tema del nivel para esa intensidad de la capa del jefe (0 sin jefe, 1 en el duelo).</summary>
        public static float VolumenDelTema(float normal, float conJefe, float jefe01) =>
            Mathf.Lerp(normal, conJefe, Mathf.Clamp01(jefe01));

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
            if (flow == null)
                return;

            // Durante la intro del estudio no suena el tema del menu: arranca cuando termina.
            var root = GameRoot.Instance;
            if (root != null && root.IntroEnCurso)
            {
                root.IntroTerminada += AplicarPantallaActual;
                return;
            }

            Aplicar(flow.Current);
        }

        void AplicarPantallaActual()
        {
            if (GameRoot.Instance != null)
                GameRoot.Instance.IntroTerminada -= AplicarPantallaActual;
            if (GameRoot.Flow != null)
                Aplicar(GameRoot.Flow.Current);
        }

        void Update()
        {
            // Mientras se apaga (fundido de salida), solo Fundir escribe volumenPropio: si el
            // SmoothDamp tambien escribiera, competiria con el fundido y la tension no llegaria a 0.
            if (jefeActivo)
                AvanzarCapaDelJefe();

            // Fuera del fundido cruzado, el tema del nivel sigue a la capa del jefe (jefeObjetivo ya viene de a poco).
            jefeSuave = Mathf.SmoothDamp(jefeSuave, jefeObjetivo, ref velocidadJefeSuave, SuavizadoJefe,
                Mathf.Infinity, Time.unscaledDeltaTime);
            if (temaDeNivel && fundidoRoutine == null && indiceActual >= 0)
                temaFuentes[indiceActual].volumenPropio = VolumenDelTema(volumenTema, volumenTemaConJefe, jefeSuave);

            if (!tensionActiva || apagandoTension)
                return;
            fuenteTension.volumenPropio = Mathf.SmoothDamp(fuenteTension.volumenPropio, amenazaObjetivo * topeTension,
                ref velocidadAmenaza, AudioDirector.Mezcla.suavizadoTension, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// La cinematica avisa cuando empieza a desvanecerse: el tema del nivel arranca ahi, antes de que
        /// se vea el nivel. Cuando el flujo pasa a Playing, el tema ya es el mismo y sigue sin cortarse.
        /// </summary>
        public void AdelantarNivel()
        {
            var flow = GameRoot.Flow;
            if (flow == null || flow.Current != GameScreen.Cinematic)
                return;
            Traza("adelanta el tema del nivel");
            Aplicar(GameScreen.Playing);
        }

        /// <summary>La llama el nivel cada cuadro: 0 sin jefe a la vista, 1 con el duelo en marcha (CapaDelJefe).</summary>
        public void SetJefe(float intensidad01) => jefeObjetivo = Mathf.Clamp01(intensidad01);

        /// <summary>
        /// Volumen al que va la capa del jefe: todo apenas el jefe se anuncia (el tema del jefe empieza con un
        /// golpe que se tiene que oir), nada sin el. El tema del nivel, en cambio, baja de a poco con la intensidad.
        /// </summary>
        public static float VolumenDeLaCapaDelJefe(float intensidad01, float tope) => intensidad01 > 0f ? tope : 0f;

        void AvanzarCapaDelJefe()
        {
            float objetivo = VolumenDeLaCapaDelJefe(jefeObjetivo, topeJefe);
            if (objetivo > 0f && !jefeSonando)
            {
                // Desde el principio cada vez que el jefe se anuncia, tambien al reintentar.
                sourceJefe.Stop();
                sourceJefe.time = 0f;
                sourceJefe.Play();
                jefeSonando = true;
                velocidadJefe = 0f;
            }
            float suavizado = objetivo > fuenteJefe.volumenPropio ? SubidaJefe : SuavizadoJefe;
            fuenteJefe.volumenPropio = Mathf.SmoothDamp(fuenteJefe.volumenPropio, objetivo,
                ref velocidadJefe, suavizado, Mathf.Infinity, Time.unscaledDeltaTime);

            // Ya apagada (derrotado, o el reintento volvio al principio del nivel): queda lista para la proxima.
            if (jefeSonando && objetivo <= 0f && fuenteJefe.volumenPropio < 0.001f)
            {
                sourceJefe.Stop();
                jefeSonando = false;
            }
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
            fuenteJefe = CrearFuente("Jefe", out sourceJefe);
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        static void Traza(string mensaje) => Debug.Log("[Musica] " + mensaje + " (t=" + Time.realtimeSinceStartup.ToString("0.0") + ")");

        void Aplicar(GameScreen pantalla)
        {
            Traza("aplicar " + pantalla);
            AudioClip clip = ClipPara(pantalla, out AudioDeNivel nivelParaTension);
            if (MusicaRegla.Decidir(actualClip, clip) == MusicaAccion.Seguir)
            {
                Traza("sigue el mismo tema");
                return;
            }

            Traza("cruza a " + (clip != null ? clip.name : "(nada)"));
            Cruzar(clip, pantalla, nivelParaTension);
            Traza("cruce listo");
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
            temaDeNivel = pantalla == GameScreen.Playing && nivelParaTension != null;
            volumenTema = temaDeNivel ? Mathf.Clamp01(nivelParaTension.volumenMusica) : 1f;
            volumenTemaConJefe = temaDeNivel ? Mathf.Clamp01(nivelParaTension.volumenMusicaConJefe) : 1f;
            float volumenEntrante = volumenTema;

            // La capa del jefe: arranca con el tema de un nivel que la tiene; al irse del nivel, se va.
            if (pantalla == GameScreen.Playing && nivelParaTension != null && nivelParaTension.jefe != null)
                IniciarJefe(nivelParaTension);
            else if (jefeActivo)
                apagarJefe = true;

            bool debeIniciarTension = pantalla == GameScreen.Playing && nivelParaTension != null &&
                nivelParaTension.tension != null;
            bool apagarTension = tensionActiva && !debeIniciarTension;
            apagandoTension = apagarTension;

            if (debeIniciarTension)
                IniciarTension(nivelParaTension, horaInicio);

            float segundos = musica != null ? musica.fundido : FundidoPorDefecto;
            if (fundidoRoutine != null)
                StopCoroutine(fundidoRoutine);
            fundidoRoutine = StartCoroutine(Fundir(saliente, salienteSource, entrante, segundos, apagarTension, volumenEntrante));

            indiceActual = indiceEntrante;
            actualClip = clip;
        }

        void IniciarTension(AudioDeNivel nivel, double horaInicio)
        {
            if (nivel.musica != null && !nivel.tensionLibre &&
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
            topeTension = Mathf.Clamp01(nivel.volumenTension);
            velocidadAmenaza = 0f;
            amenazaObjetivo = 0f; // que no herede la amenaza del nivel anterior: arranca en 0 y sigue a SetAmenaza.
        }

        bool apagarJefe;

        void IniciarJefe(AudioDeNivel nivel)
        {
            sourceJefe.Stop();
            sourceJefe.clip = nivel.jefe;
            fuenteJefe.volumenPropio = 0f;
            jefeSonando = false; // suena recien cuando el jefe se anuncia (AvanzarCapaDelJefe)
            jefeActivo = true;
            apagarJefe = false;
            topeJefe = Mathf.Clamp01(nivel.volumenJefe);
            velocidadJefe = 0f;
            jefeObjetivo = 0f; // arranca callada: la sube SetJefe cuando el jefe esta por aparecer.
        }

        IEnumerator Fundir(FuenteConBus saliente, AudioSource salienteSource, FuenteConBus entrante, float segundos,
            bool apagarTension, float volumenEntrante)
        {
            float desdeSaliente = saliente != null ? saliente.volumenPropio : 0f;
            float desdeTension = apagarTension ? fuenteTension.volumenPropio : 0f;
            bool apagaJefe = apagarJefe;
            if (apagaJefe)
                jefeActivo = false; // el fundido de aca manda: que el SmoothDamp no la vuelva a subir.
            float desdeJefe = apagaJefe ? fuenteJefe.volumenPropio : 0f;

            for (float t = 0f; t < segundos; t += Time.unscaledDeltaTime)
            {
                float f = Mathf.Clamp01(t / segundos);
                if (saliente != null)
                    saliente.volumenPropio = Mathf.Lerp(desdeSaliente, 0f, f);
                entrante.volumenPropio = f * volumenEntrante;
                if (apagarTension)
                    fuenteTension.volumenPropio = Mathf.Lerp(desdeTension, 0f, f);
                if (apagaJefe)
                    fuenteJefe.volumenPropio = Mathf.Lerp(desdeJefe, 0f, f);
                yield return null;
            }

            if (saliente != null)
            {
                saliente.volumenPropio = 0f;
                salienteSource.Stop();
            }
            entrante.volumenPropio = volumenEntrante;
            if (apagaJefe)
            {
                fuenteJefe.volumenPropio = 0f;
                sourceJefe.Stop();
                apagarJefe = false;
            }

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
