using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Zombineta.Juego.EditorTools.Framing
{
    /// <summary>
    /// Superpone una imagen de referencia sobre el Game view para comparar el encuadre del nivel
    /// contra el arte de escala. El overlay vive fuera de la escena (HideFlags.DontSave): nunca se
    /// guarda ni la marca como modificada, y en Play sobrevive a los cambios de escena aditivos.
    /// </summary>
    [InitializeOnLoad]
    public static class FramingGuide
    {
        const string GameObjectName = "__GuiaEncuadre";

        const string VisibleKey = "zombineta.framing.visible";
        const string PathKey = "zombineta.framing.path";
        const string OpacityKey = "zombineta.framing.opacity";

        const float DefaultOpacity = 0.4f;
        const float MinOpacity = 0.05f;
        const float MaxOpacity = 1f;
        const float OpacityStep = 0.1f;

        const int SortingOrder = 2000;

        static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        static FramingGuide()
        {
            EditorSceneManager.activeSceneChangedInEditMode += (a, b) => { if (Visible) Ensure(); };
            SceneManager.activeSceneChanged += (a, b) => { if (Visible) Ensure(); };
            EditorApplication.playModeStateChanged += _ => { if (Visible) Ensure(); };

            // El editor puede reabrirse con la guia activa de una sesion anterior: recrearla ahora
            // en vez de esperar al proximo cambio de escena.
            if (Visible)
                Ensure();
        }

        // --- Estado (EditorPrefs) -----------------------------------------------------

        public static bool Visible
        {
            get => EditorPrefs.GetBool(VisibleKey, false);
            private set => EditorPrefs.SetBool(VisibleKey, value);
        }

        public static string ImagePath
        {
            get => EditorPrefs.GetString(PathKey, DefaultImagePath);
            private set => EditorPrefs.SetString(PathKey, value);
        }

        public static float Opacity
        {
            get => EditorPrefs.GetFloat(OpacityKey, DefaultOpacity);
            private set => EditorPrefs.SetFloat(OpacityKey, Mathf.Clamp(value, MinOpacity, MaxOpacity));
        }

        /// <summary>Ruta por defecto: la carpeta de arte del proyecto, fuera de Assets/.</summary>
        static string DefaultImagePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../../Arte/Bocetos/Concept Art/Mapa_Escala.png"));

        // --- Menu ----------------------------------------------------------------------

        [MenuItem("Zombineta/Encuadre/Mostrar referencia")]
        static void MenuShow() => Show(ImagePath, Opacity);

        [MenuItem("Zombineta/Encuadre/Ocultar referencia")]
        static void MenuHide() => Hide();

        [MenuItem("Zombineta/Encuadre/Elegir imagen…")]
        static void MenuChooseImage()
        {
            var startDir = File.Exists(ImagePath) ? Path.GetDirectoryName(ImagePath) : "";
            var chosen = EditorUtility.OpenFilePanel("Elegir imagen de referencia", startDir, "png");
            if (string.IsNullOrEmpty(chosen))
                return;
            Show(chosen, Opacity);
        }

        [MenuItem("Zombineta/Encuadre/Opacidad +")]
        static void MenuOpacityUp() => ChangeOpacity(OpacityStep);

        [MenuItem("Zombineta/Encuadre/Opacidad -")]
        static void MenuOpacityDown() => ChangeOpacity(-OpacityStep);

        static void ChangeOpacity(float delta)
        {
            Opacity += delta;
            if (Visible)
                Ensure();
        }

        // --- API publica ------------------------------------------------------------

        public static void Show(string imagePath, float opacity)
        {
            ImagePath = imagePath;
            Opacity = opacity;
            Visible = true;
            Ensure();
        }

        public static void Hide()
        {
            var go = GameObject.Find(GameObjectName);
            if (go != null)
                Object.DestroyImmediate(go);
            Visible = false;
        }

        // --- Overlay -------------------------------------------------------------------

        /// <summary>Crea (si hace falta) y actualiza el overlay para que muestre la imagen actual.</summary>
        static void Ensure()
        {
            var texture = LoadTexture(ImagePath);
            if (texture == null)
                return;

            var go = GameObject.Find(GameObjectName);
            if (go == null)
                go = CreateOverlay();

            var image = go.GetComponentInChildren<RawImage>(true);
            image.texture = texture;
            image.color = new Color(1f, 1f, 1f, Opacity);

            // La escena de nivel se descarga y se carga otra aditivamente durante Play: sin esto
            // la guia desaparece con la escena que la creo.
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(go);
        }

        static GameObject CreateOverlay()
        {
            var go = new GameObject(GameObjectName, typeof(Canvas), typeof(CanvasScaler));
            go.hideFlags = HideFlags.DontSave;
            go.tag = "EditorOnly";

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var child = new GameObject("Imagen", typeof(RectTransform), typeof(RawImage));
            child.hideFlags = HideFlags.DontSave;
            child.transform.SetParent(go.transform, false);

            var rt = (RectTransform)child.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            child.GetComponent<RawImage>().raycastTarget = false;

            return go;
        }

        /// <summary>Carga la textura desde disco (esta fuera de Assets/, no es un asset importado) y la cachea por ruta.</summary>
        static Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            if (textureCache.TryGetValue(path, out var cached) && cached != null)
                return cached;

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (IOException e)
            {
                Debug.LogWarning("FramingGuide: no se pudo leer " + path + " (" + e.Message + ")");
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            if (!texture.LoadImage(bytes))
            {
                Object.DestroyImmediate(texture);
                Debug.LogWarning("FramingGuide: la imagen no se pudo decodificar: " + path);
                return null;
            }

            textureCache[path] = texture;
            return texture;
        }
    }
}
