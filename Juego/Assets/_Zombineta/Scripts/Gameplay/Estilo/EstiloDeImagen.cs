using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Zombineta.Juego.Levels;

namespace Zombineta.Estilo
{
    public enum EstiloVisual
    {
        Normal,
        BlancoYNegro,
        PeliculaVieja,
    }

    /// <summary>
    /// El estilo de la imagen: blanco y negro y pelicula vieja. Es un Volume de post-processing armado
    /// por codigo (color, grano, vineta) mas una capa encima del juego con las rayas, el polvo y el
    /// parpadeo del proyector. El HUD no se toca: esta en un Canvas Overlay, que se dibuja despues.
    /// Se crea solo y vive toda la partida. El estilo base lo pone el nivel (LevelScene); encima se
    /// puede pedir uno de paso (Desaturar, al ser atrapada). Cada nivel arranca en blanco y negro y
    /// despues toma color. Sin estilo, el post-processing se apaga.
    /// </summary>
    public sealed class EstiloDeImagen : MonoBehaviour
    {
        const string RutaPelicula = "Estilo/PeliculaVieja";
        const float SegundosBusqueda = 0.5f;
        static readonly Color Sepia = new Color(1f, 0.86f, 0.66f);

        static EstiloDeImagen instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarStatic() => instancia = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null)
                return;
            var go = new GameObject("EstiloDeImagen");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<EstiloDeImagen>();
        }

        /// <summary>Pide una desaturacion de paso (0 a 1) por encima del estilo del nivel. 0 la saca.</summary>
        public static void Desaturar(float cuanto)
        {
            if (instancia != null)
            {
                instancia.desaturacion = Mathf.Clamp01(cuanto);
                instancia.inicio = false;
            }
        }

        /// <summary>El estilo que se ve ahora (o hacia el que va).</summary>
        public static EstiloVisual Actual => instancia != null ? instancia.objetivo : EstiloVisual.Normal;

        /// <summary>Lo que mezcla cada estilo: saturacion perdida (0 a 1), sepia, grano, vineta y capa del proyector.</summary>
        public struct Mezcla
        {
            public float sinColor, sepia, grano, vineta, proyector;

            public bool Algo => sinColor > 0.001f || sepia > 0.001f || grano > 0.001f || vineta > 0.001f || proyector > 0.001f;
        }

        /// <summary>La mezcla de un estilo a pleno, y una desaturacion de paso encima.</summary>
        public static Mezcla MezclaDe(EstiloVisual estilo, float desaturacion)
        {
            var m = new Mezcla();
            switch (estilo)
            {
                case EstiloVisual.BlancoYNegro:
                    m.sinColor = 1f;
                    m.vineta = 0.25f;
                    break;
                case EstiloVisual.PeliculaVieja:
                    m.sinColor = 1f;
                    m.sepia = 1f;
                    m.grano = 1f;
                    m.vineta = 0.45f;
                    m.proyector = 1f;
                    break;
            }
            m.sinColor = Mathf.Max(m.sinColor, Mathf.Clamp01(desaturacion));
            return m;
        }

        [Tooltip("Segundos que tarda en pasar de un estilo a otro.")]
        [SerializeField] float segundosCambio = 0.6f;

        [Header("Arranque del nivel")]
        [Tooltip("Segundos que el nivel se ve en blanco y negro al empezar, antes de tomar color.")]
        [SerializeField] float segundosEnBlancoYNegro = 1.2f;
        [Tooltip("Segundos que tarda en tomar el color.")]
        [SerializeField] float segundosHastaElColor = 2.5f;

        Volume volumen;
        ColorAdjustments color;
        FilmGrain grano;
        Vignette vineta;
        Material pelicula;
        RawImage proyector;
        GameObject capaProyector;

        EstiloVisual objetivo;
        EstiloVisual? elegidoAMano;
        LevelScene nivel;
        LevelScene nivelDelAtajo;
        float proximaBusqueda;
        float desaturacion;
        Mezcla actual;
        bool inicio;
        float inicioRestante;

        void Awake()
        {
            var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            perfil.name = "Estilo (runtime)";
            color = perfil.Add<ColorAdjustments>(true);
            grano = perfil.Add<FilmGrain>(true);
            grano.type.value = FilmGrainLookup.Medium3;
            vineta = perfil.Add<Vignette>(true);
            vineta.smoothness.value = 0.45f;

            volumen = gameObject.AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.priority = 100f;
            volumen.sharedProfile = perfil;
            Aplicar(new Mezcla());

            CrearCapaDelProyector();
            // Un nivel recien cargado se busca enseguida: si no, se veria un instante en color.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => proximaBusqueda = 0f;
        }

        // Debajo del HUD (que va en 0 o mas) y del fundido (1000): las rayas no tapan los numeros.
        void CrearCapaDelProyector()
        {
            var shader = Resources.Load<Shader>(RutaPelicula);
            if (shader == null)
                return;
            pelicula = new Material(shader) { name = "Pelicula vieja (runtime)" };

            capaProyector = new GameObject("Proyector", typeof(Canvas));
            capaProyector.transform.SetParent(transform, false);
            var canvas = capaProyector.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -50;

            var img = new GameObject("Rayas y polvo", typeof(RectTransform), typeof(RawImage));
            img.transform.SetParent(capaProyector.transform, false);
            proyector = img.GetComponent<RawImage>();
            proyector.material = pelicula;
            proyector.raycastTarget = false;
            var r = proyector.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            capaProyector.SetActive(false);
        }

        void Update()
        {
            BuscarNivel();
            AtajoDePrueba();

            float paso = segundosCambio > 0f ? Time.unscaledDeltaTime / segundosCambio : 1f;
            float pasoColor = paso;
            if (inicio)
            {
                // Con el tiempo del juego: la pausa tambien congela el arranque.
                inicioRestante -= Time.deltaTime;
                if (inicioRestante <= 0f)
                    desaturacion = 0f;
                pasoColor = segundosHastaElColor > 0f ? Time.deltaTime / segundosHastaElColor : 1f;
            }

            var base_ = elegidoAMano ?? (nivel != null ? nivel.Estilo : EstiloVisual.Normal);
            objetivo = base_;
            var meta = MezclaDe(base_, desaturacion);
            if (inicio && inicioRestante <= 0f && actual.sinColor <= meta.sinColor + 0.001f)
                inicio = false;

            actual.sinColor = Mathf.MoveTowards(actual.sinColor, meta.sinColor, pasoColor);
            actual.sepia = Mathf.MoveTowards(actual.sepia, meta.sepia, paso);
            actual.grano = Mathf.MoveTowards(actual.grano, meta.grano, paso);
            actual.vineta = Mathf.MoveTowards(actual.vineta, meta.vineta, paso);
            actual.proyector = Mathf.MoveTowards(actual.proyector, meta.proyector, paso);
            Aplicar(actual);
        }

        void BuscarNivel()
        {
            if (Time.unscaledTime < proximaBusqueda)
                return;
            proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
            var encontrado = FindAnyObjectByType<LevelScene>();
            if (encontrado != nivel)
            {
                nivel = encontrado;
                // Otro nivel (o un reintento): lo elegido a mano y la desaturacion de paso no siguen.
                if (nivelDelAtajo != nivel)
                    elegidoAMano = null;
                desaturacion = 0f;
                inicio = nivel != null;
                if (inicio)
                {
                    desaturacion = 1f;
                    actual.sinColor = 1f;
                    inicioRestante = segundosEnBlancoYNegro;
                }
            }
        }

        // Pruebas: F6 recorre los estilos en el nivel que se esta jugando.
        void AtajoDePrueba()
        {
#if UNITY_EDITOR || DEBUG
            if (nivel == null || Keyboard.current == null || !Keyboard.current.f6Key.wasPressedThisFrame)
                return;
            var actualElegido = elegidoAMano ?? nivel.Estilo;
            elegidoAMano = (EstiloVisual)(((int)actualElegido + 1) % 3);
            nivelDelAtajo = nivel;
            Debug.Log("[Estilo] " + elegidoAMano);
#endif
        }

        void Aplicar(Mezcla m)
        {
            color.saturation.Override(-100f * m.sinColor);
            color.colorFilter.Override(Color.Lerp(Color.white, Sepia, m.sepia));
            color.contrast.Override(12f * m.sepia);
            grano.intensity.Override(0.65f * m.grano);
            vineta.intensity.Override(m.vineta);
            volumen.weight = m.Algo ? 1f : 0f;

            if (capaProyector != null)
            {
                bool ver = m.proyector > 0.001f;
                if (capaProyector.activeSelf != ver)
                    capaProyector.SetActive(ver);
                if (ver)
                {
                    pelicula.SetFloat("_Fuerza", m.proyector);
                    pelicula.SetFloat("_Aspecto", Screen.height > 0 ? (float)Screen.width / Screen.height : 1.7777f);
                }
            }

            // El post-processing cuesta: la camara lo prende solo mientras hay algun estilo.
            var seguidora = FindAnyObjectByType<Zombineta.Core.CameraFollow>();
            var cam = seguidora != null ? seguidora.GetComponent<Camera>() : Camera.main;
            if (cam != null && cam.TryGetComponent<UniversalAdditionalCameraData>(out var datos) &&
                datos.renderPostProcessing != m.Algo)
                datos.renderPostProcessing = m.Algo;
        }

        void OnDestroy()
        {
            if (instancia == this)
                instancia = null;
            if (pelicula != null)
                Destroy(pelicula);
        }
    }
}
