using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Flow;
using Zombineta.Juego.Flow;
using Zombineta.Player;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// El mapa de la ciudad entre niveles: se ve despues de elegir personaje y despues de cada nivel ganado.
    /// Marca con una cruz los lugares que ya se jugaron, resalta el que sigue y pide ENTER para seguir (a la
    /// cinematica de ese nivel). El mapa es una imagen provisoria (Resources/Mapa/MapaCiudad) con los puntos de
    /// los niveles encima: se arma entero por codigo, la escena solo trae el Canvas y esta pantalla.
    /// </summary>
    public sealed class MapScreen : MonoBehaviour
    {
        [Tooltip("Donde esta cada nivel sobre la imagen del mapa, en fraccion del ancho y del alto (0,0 = abajo a la " +
                 "izquierda). El primero va abajo a la izquierda; se siguen por el camino. Si el mapa cambia, se ajustan.")]
        [SerializeField] Vector2[] nodos =
        {
            new Vector2(215f / 2000f, 1f - 948f / 1116f),
            new Vector2(848f / 2000f, 1f - 945f / 1116f),
            new Vector2(1118f / 2000f, 1f - 705f / 1116f),
            new Vector2(1445f / 2000f, 1f - 838f / 1116f),
            new Vector2(1805f / 2000f, 1f - 945f / 1116f),
            new Vector2(1595f / 2000f, 1f - 412f / 1116f),
        };

        [Tooltip("Diametro de la marca de cada nivel, en pixeles del mapa a 1080 de alto.")]
        [SerializeField] float diametro = 104f;
        [SerializeField] Color colorPendiente = new Color(1f, 0.82f, 0.25f);
        [SerializeField] Color colorJugado = new Color(0.55f, 0.95f, 0.65f);
        [Tooltip("Segundos que tarda en marcarse (sello) el nivel que se acaba de ganar.")]
        [SerializeField] float segundosSello = 0.6f;

        const float AltoReferencia = 1080f;

        RectTransform mapa;
        Text titulo, leyenda;
        RectTransform anillo, selloRaiz;
        CanvasGroup selloGrupo;
        int actual = -1;
        int recienGanado = -1;
        float desde;
        bool listo;
        Sprite disco, aro;
        Texture2D texDisco, texAro;

        IEnumerator Start()
        {
            // Si se dio Play directo en esta escena, Boot tarda un frame en aparecer.
            while (GameRoot.Instance == null || GameRoot.Flow == null)
                yield return null;

            Armar();
            desde = Time.unscaledTime;
            listo = true;
        }

        void Armar()
        {
            var flow = GameRoot.Flow;
            actual = Mathf.Clamp(flow.LevelIndex, 0, Mathf.Max(0, Mathf.Min(flow.LevelCount, nodos.Length) - 1));
            recienGanado = actual > 0 ? actual - 1 : -1;

            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;   // el mapa llena el alto; si sobra ancho, queda recortado parejo

            disco = Circulo(false, out texDisco);
            aro = Circulo(true, out texAro);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // El mapa: la imagen a todo el alto, centrada.
            var imagen = Resources.Load<Sprite>("Mapa/MapaCiudad");
            float proporcion = imagen != null ? imagen.rect.width / imagen.rect.height : 16f / 9f;
            var fondo = Nuevo("Fondo", transform, typeof(Image));
            Estirar((RectTransform)fondo.transform);
            fondo.GetComponent<Image>().color = Color.black;
            fondo.GetComponent<Image>().raycastTarget = false;

            var go = Nuevo("Mapa", transform, typeof(Image));
            mapa = (RectTransform)go.transform;
            mapa.anchorMin = mapa.anchorMax = mapa.pivot = new Vector2(0.5f, 0.5f);
            mapa.sizeDelta = new Vector2(AltoReferencia * proporcion, AltoReferencia);
            var im = go.GetComponent<Image>();
            im.sprite = imagen;
            im.color = imagen != null ? Color.white : new Color(0.25f, 0.2f, 0.4f);
            im.raycastTarget = false;

            // Una franja oscura abajo y arriba para que se lean los textos sobre cualquier mapa.
            Franja("Franja abajo", new Vector2(0f, 0f), new Vector2(1f, 0.13f));
            Franja("Franja arriba", new Vector2(0f, 0.9f), new Vector2(1f, 1f));

            var flowLevels = GameRoot.Instance.Levels;
            int total = Mathf.Min(flow.LevelCount, nodos.Length);
            for (int i = 0; i < total; i++)
            {
                if (i < actual)
                    MarcaJugado(i, i == recienGanado, font);
                else if (i == actual)
                    MarcaActual(i, font, flowLevels != null && i < flowLevels.levels.Count ? flowLevels.levels[i].displayName : "Nivel " + (i + 1));
            }

            titulo = Texto("Titulo", transform, font, 54, TextAnchor.UpperCenter, new Vector2(0f, -34f), new Vector2(1500f, 80f));
            var tr = titulo.rectTransform;
            tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 1f);
            titulo.text = flowLevels != null && actual < flowLevels.levels.Count ? flowLevels.levels[actual].displayName.ToUpperInvariant() : "NIVEL " + (actual + 1);
            titulo.fontStyle = FontStyle.Bold;

            leyenda = Texto("Leyenda", transform, font, 44, TextAnchor.LowerCenter, new Vector2(0f, 38f), new Vector2(1500f, 70f));
            var lr = leyenda.rectTransform;
            lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0.5f, 0f);
            leyenda.fontStyle = FontStyle.Bold;
        }

        void Update()
        {
            if (!listo)
                return;

            float t = Time.unscaledTime;

            // El nivel que sigue late; el recien ganado se marca con un sello que cae.
            if (anillo != null)
            {
                float latido = 1f + 0.18f * Mathf.Sin(t * 4f);
                anillo.localScale = Vector3.one * latido;
                var ic = anillo.GetComponent<Image>();
                var c = ic.color;
                c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 2f));
                ic.color = c;
            }
            if (selloRaiz != null)
            {
                float k = Mathf.Clamp01((t - desde - 0.35f) / Mathf.Max(0.05f, segundosSello));
                float e = 1f - (1f - k) * (1f - k);
                selloRaiz.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, e);
                selloGrupo.alpha = e;
            }

            var esquema = InputDeviceTracker.Shared != null ? InputDeviceTracker.Shared.Current : ControlScheme.KeyboardMouse;
            string tecla = esquema == ControlScheme.Gamepad ? "A" : "ENTER";
            leyenda.text = "PRESIONÁ " + tecla + " PARA CONTINUAR";
            var lc = leyenda.color;
            lc.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 2.4f));
            leyenda.color = lc;

            if (!TieneElFoco())
                return;

            if (ContinuarPresionado())
                GameRoot.Flow?.MapContinue();
            else if (VolverPresionado())
                GameRoot.Flow?.Back();
        }

        /// <summary>Solo recibe el teclado si es la escena de arriba y el cambio de pantalla no uso ya esa tecla.</summary>
        bool TieneElFoco()
        {
            var root = GameRoot.Instance;
            if (root == null || GameRoot.Flow == null || GameRoot.Flow.Current != GameScreen.Map || root.Busy)
                return false;
            if (root.TopScene != gameObject.scene.name)
                return false;
            return Time.frameCount != root.LastChangeFrame;
        }

        static bool ContinuarPresionado()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                return true;
            var pad = Gamepad.current;
            return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
        }

        static bool VolverPresionado()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                return true;
            var pad = Gamepad.current;
            return pad != null && pad.buttonEast.wasPressedThisFrame;
        }

        // --- Las marcas -----------------------------------------------------------------

        RectTransform Marca(string nombre, int indice, Sprite sprite, Color color, float escala)
        {
            var go = Nuevo(nombre, mapa, typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = nodos[indice];
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.one * diametro * escala;
            var im = go.GetComponent<Image>();
            im.sprite = sprite;
            im.color = color;
            im.raycastTarget = false;
            return rt;
        }

        /// <summary>Un lugar ya jugado: disco verde con una cruz. El que se acaba de ganar la marca con un sello.</summary>
        void MarcaJugado(int indice, bool sello, Font font)
        {
            var disc = Marca("Jugado " + (indice + 1), indice, disco, colorJugado, 1f);
            var contorno = disc.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0f, 0f, 0f, 0.7f);
            contorno.effectDistance = new Vector2(3f, -3f);
            var cruz = new Color(0.1f, 0.12f, 0.15f);
            Barra(disc, 45f, cruz);
            Barra(disc, -45f, cruz);

            if (!sello)
                return;
            // El sello: el mismo dibujo, que arranca grande y transparente y cae sobre el lugar.
            selloRaiz = disc;
            selloGrupo = disc.gameObject.AddComponent<CanvasGroup>();
            selloGrupo.alpha = 0f;
        }

        void Barra(RectTransform padre, float grados, Color color)
        {
            var go = Nuevo("Cruz", padre, typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(padre.sizeDelta.x * 0.62f, padre.sizeDelta.x * 0.13f);
            rt.localRotation = Quaternion.Euler(0f, 0f, grados);
            var im = go.GetComponent<Image>();
            im.color = color;
            im.raycastTarget = false;
        }

        /// <summary>El que sigue: disco dorado con un aro que late y el nombre al lado.</summary>
        void MarcaActual(int indice, Font font, string nombre)
        {
            var disc = Marca("Siguiente " + (indice + 1), indice, disco, colorPendiente, 0.82f);
            var contorno = disc.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0.25f, 0.1f, 0f, 0.8f);
            contorno.effectDistance = new Vector2(3f, -3f);
            anillo = Marca("Aro", indice, aro, colorPendiente, 1.5f);

            var t = Texto("Nombre", mapa, font, 34, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(520f, 50f));
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = nodos[indice];
            rt.pivot = new Vector2(0.5f, 0f);
            // Arriba de la marca; si el punto queda muy arriba, abajo.
            bool arriba = nodos[indice].y < 0.8f;
            rt.pivot = new Vector2(0.5f, arriba ? 0f : 1f);
            rt.anchoredPosition = new Vector2(0f, (arriba ? 1f : -1f) * (diametro * 0.95f));
            t.text = nombre;
            t.fontStyle = FontStyle.Bold;
        }

        // --- Armado de UI -----------------------------------------------------------------

        void Franja(string nombre, Vector2 min, Vector2 max)
        {
            var go = Nuevo(nombre, transform, typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var im = go.GetComponent<Image>();
            im.color = new Color(0f, 0f, 0f, 0.5f);
            im.raycastTarget = false;
        }

        static GameObject Nuevo(string nombre, Transform padre, params System.Type[] tipos)
        {
            var lista = new System.Type[tipos.Length + 1];
            lista[0] = typeof(RectTransform);
            System.Array.Copy(tipos, 0, lista, 1, tipos.Length);
            var go = new GameObject(nombre, lista);
            go.transform.SetParent(padre, false);
            return go;
        }

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Text Texto(string nombre, Transform padre, Font font, int tam, TextAnchor anclaje, Vector2 pos, Vector2 tamano)
        {
            var go = Nuevo(nombre, padre, typeof(Text), typeof(Outline));
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = tam;
            t.alignment = anclaje;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamano;
            var o = go.GetComponent<Outline>();
            o.effectColor = Color.black;
            o.effectDistance = new Vector2(3f, -3f);
            return t;
        }

        /// <summary>Un circulo liso (o solo el aro) con borde suave, dibujado por codigo: no hace falta ningun asset.</summary>
        static Sprite Circulo(bool soloAro, out Texture2D textura)
        {
            const int N = 128;
            textura = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[N * N];
            float r = N * 0.5f - 1.5f;
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N * 0.5f, N * 0.5f));
                float a = Mathf.Clamp01(r - d + 0.5f);                    // borde exterior suave
                if (soloAro)
                    a = Mathf.Min(a, Mathf.Clamp01(d - (r - 9f) + 0.5f));  // y se vacia el centro
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            textura.SetPixels32(px);
            textura.Apply();
            return Sprite.Create(textura, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }

        void OnDestroy()
        {
            if (disco != null) Destroy(disco);
            if (aro != null) Destroy(aro);
            if (texDisco != null) Destroy(texDisco);
            if (texAro != null) Destroy(texAro);
        }
    }
}
