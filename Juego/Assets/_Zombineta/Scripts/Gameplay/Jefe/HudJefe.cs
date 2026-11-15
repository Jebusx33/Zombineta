using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.Jefe
{
    /// <summary>
    /// El HUD del duelo: la vida del jefe arriba al centro y los corazones de la moto. Va en el Canvas del HUD y arma sus propios elementos, como HudPelea.
    /// La barra tiene marco oscuro con las puntas cortadas y borde rosa (como los paneles del menu), un
    /// relleno en degrade de carmesi a rosa y naranja con un brillo arriba, marcas cada cuarto y una estela
    /// clara que baja con retraso despues de cada golpe, para que se vea cuanto se le saco. Cuando se lo
    /// puede lastimar, el relleno pasa a dorado. Los dibujos salen de texturas hechas por codigo: no hace
    /// falta ningun asset.
    /// </summary>
    public sealed class HudJefe : MonoBehaviour
    {
        const float AnchoBarra = 700f;
        const float AltoBarra = 48f;
        const float Margen = 10f;   // entre el marco y el relleno

        [SerializeField] RunController run;
        [Header("Degrade de la vida (de la ultima punta a la primera)")]
        [SerializeField] Color vidaFinal = new Color32(140, 14, 58, 255);
        [SerializeField] Color vidaMedia = new Color32(235, 38, 110, 255);
        [SerializeField] Color vidaInicio = new Color32(255, 128, 84, 255);
        [Header("Cuando se lo puede lastimar")]
        [SerializeField] Color colorVulnerable = new Color(1f, 0.82f, 0.25f);
        [SerializeField] Color colorCorazones = new Color(1f, 0.3f, 0.35f);
        [Header("Estela del dano")]
        [Tooltip("Segundos que espera la estela antes de empezar a bajar.")]
        [SerializeField] float retrasoEstela = 0.45f;
        [Tooltip("Fraccion de la barra que baja la estela por segundo.")]
        [SerializeField] float velocidadEstela = 0.35f;

        GameObject panel;
        Image vida, estela, marco;
        Text corazones;
        Sprite spriteVida, spriteVulnerable, spriteMarco;
        Texture2D texVida, texVulnerable, texMarco;
        float fraccionEstela = 1f;
        float esperaEstela;
        float fraccionAnterior = 1f;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();

            spriteVida = Degradado(vidaFinal, vidaMedia, vidaInicio, out texVida);
            spriteVulnerable = Degradado(new Color(0.8f, 0.5f, 0.05f), colorVulnerable, new Color(1f, 0.97f, 0.7f), out texVulnerable);
            spriteMarco = Marco(out texMarco);

            panel = new GameObject("Duelo", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var r = (RectTransform)panel.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -30f);
            r.sizeDelta = new Vector2(AnchoBarra, 140f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // El nombre, arriba a la izquierda de la barra.
            var nombre = NuevoTexto("Nombre", font, 28, new Vector2(-AnchoBarra * 0.5f + 6f, 0f), TextAnchor.UpperLeft, new Vector2(300f, 32f));
            nombre.rectTransform.pivot = new Vector2(0f, 1f);
            nombre.text = "JEFE";
            nombre.fontStyle = FontStyle.Bold;
            nombre.color = new Color32(255, 170, 214, 255);
            Contorno(nombre.gameObject, 2f);

            // Marco, estela y relleno.
            marco = NuevaImagen("Marco", Color.white);
            marco.sprite = spriteMarco;
            marco.type = Image.Type.Sliced;
            Ubicar(marco.rectTransform, new Vector2(0f, -32f), new Vector2(AnchoBarra, AltoBarra));

            float ancho = AnchoBarra - Margen * 2f, alto = AltoBarra - Margen * 2f;
            estela = NuevaImagen("Estela", new Color(1f, 0.85f, 0.9f, 0.85f));
            Rellenar(estela, spriteVida, ancho, alto);
            vida = NuevaImagen("Vida", Color.white);
            Rellenar(vida, spriteVida, ancho, alto);

            // Marcas cada cuarto, sobre el relleno.
            for (int i = 1; i <= 3; i++)
            {
                var marca = NuevaImagen("Marca " + i, new Color(0f, 0f, 0f, 0.35f));
                var mr = marca.rectTransform;
                mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 1f);
                mr.pivot = new Vector2(0.5f, 1f);
                mr.anchoredPosition = new Vector2(-ancho * 0.5f + ancho * i * 0.25f, -32f - Margen);
                mr.sizeDelta = new Vector2(2f, alto);
            }

            corazones = NuevoTexto("Corazones", font, 40, new Vector2(0f, -92f), TextAnchor.UpperCenter, new Vector2(AnchoBarra, 50f));
            corazones.color = colorCorazones;
            panel.SetActive(false);
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            var jefe = run.Sim.Jefe;
            bool visible = jefe.Peleando && run.Sim.State.Phase == RunPhase.Running;
            if (panel.activeSelf != visible)
                panel.SetActive(visible);
            if (!visible)
                return;

            var cfg = run.Config.jefe;
            float avance = Mathf.Clamp01(jefe.TiempoFase / cfg.segundosEntrada);
            float fraccion = jefe.Fase == FaseDeJefe.Entrada ? avance : Mathf.Clamp01(jefe.Vida / (float)cfg.vida);
            vida.fillAmount = fraccion;

            // La estela: sube con la vida (la entrada) y, si baja, espera un momento y la alcanza.
            if (fraccion > fraccionEstela)
                fraccionEstela = fraccion;
            else if (fraccion < fraccionAnterior - 0.0001f)
                esperaEstela = retrasoEstela;
            if (fraccionEstela > fraccion)
            {
                if (esperaEstela > 0f)
                    esperaEstela -= Time.unscaledDeltaTime;
                else
                    fraccionEstela = Mathf.MoveTowards(fraccionEstela, fraccion, velocidadEstela * Time.unscaledDeltaTime);
            }
            fraccionAnterior = fraccion;
            estela.fillAmount = fraccionEstela;

            bool vulnerable = jefe.Vulnerable;
            vida.sprite = vulnerable ? spriteVulnerable : spriteVida;
            corazones.text = new string('♥', jefe.Corazones) + new string('♡', cfg.corazones - jefe.Corazones);
        }

        void OnDestroy()
        {
            if (spriteVida != null) Destroy(spriteVida);
            if (spriteVulnerable != null) Destroy(spriteVulnerable);
            if (spriteMarco != null) Destroy(spriteMarco);
            if (texVida != null) Destroy(texVida);
            if (texVulnerable != null) Destroy(texVulnerable);
            if (texMarco != null) Destroy(texMarco);
        }

        // --- Dibujos por codigo ---------------------------------------------------------------

        /// <summary>
        /// El relleno: degrade horizontal de tres colores, con una banda de brillo en la mitad de arriba y
        /// un borde mas oscuro abajo, para que no se vea como una barra plana.
        /// </summary>
        static Sprite Degradado(Color a, Color b, Color c, out Texture2D textura)
        {
            const int W = 256, H = 32;
            textura = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = y / (H - 1f);   // 0 abajo, 1 arriba
                for (int x = 0; x < W; x++)
                {
                    float u = x / (W - 1f);
                    var col = u < 0.5f ? Color.Lerp(a, b, u * 2f) : Color.Lerp(b, c, (u - 0.5f) * 2f);
                    if (v > 0.55f)
                        col = Color.Lerp(col, Color.white, 0.22f);        // brillo
                    else if (v < 0.22f)
                        col = Color.Lerp(col, Color.black, 0.25f);        // sombra del borde de abajo
                    col.a = 1f;
                    px[y * W + x] = col;
                }
            }
            textura.SetPixels32(px);
            textura.Apply();
            return Sprite.Create(textura, new Rect(0, 0, W, H), new Vector2(0f, 0.5f), 100f);
        }

        /// <summary>
        /// El marco: violeta oscuro con borde rosa claro y las cuatro puntas cortadas, como los paneles del
        /// menu. Se estira por los bordes (9-slice).
        /// </summary>
        static Sprite Marco(out Texture2D textura)
        {
            const int N = 48;
            const float corte = 12f, borde = 3f;
            textura = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var relleno = new Color(0.11f, 0.08f, 0.2f, 0.9f);
            var linea = new Color(0.84f, 0.48f, 0.7f, 1f);
            const float raiz2 = 1.41421356f;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                // Distancia hacia adentro desde el borde mas cercano, contando las puntas cortadas.
                float d = Mathf.Min(Mathf.Min(fx, N - fx), Mathf.Min(fy, N - fy));
                d = Mathf.Min(d, (fx + fy - corte) / raiz2);
                d = Mathf.Min(d, ((N - fx) + fy - corte) / raiz2);
                d = Mathf.Min(d, (fx + (N - fy) - corte) / raiz2);
                d = Mathf.Min(d, ((N - fx) + (N - fy) - corte) / raiz2);
                float cobertura = Mathf.Clamp01(d + 0.5f);
                var col = Color.Lerp(linea, relleno, Mathf.Clamp01(d - borde + 0.5f));
                col.a *= cobertura;
                px[y * N + x] = col;
            }
            textura.SetPixels32(px);
            textura.Apply();
            return Sprite.Create(textura, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
        }

        // --- Armado de UI ---------------------------------------------------------------------

        /// <summary>Un relleno que se recorta de izquierda a derecha, dentro del marco.</summary>
        static void Rellenar(Image img, Sprite sprite, float ancho, float alto)
        {
            img.sprite = sprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(-ancho * 0.5f, -32f - Margen);
            rt.sizeDelta = new Vector2(ancho, alto);
        }

        Image NuevaImagen(string nombre, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel.transform, false);
            var i = go.GetComponent<Image>();
            i.color = color;
            i.raycastTarget = false;
            return i;
        }

        Text NuevoTexto(string nombre, Font font, int tam, Vector2 pos, TextAnchor anclaje, Vector2 tamano)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel.transform, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = tam;
            t.alignment = anclaje;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Ubicar(t.rectTransform, pos, tamano);
            return t;
        }

        static void Contorno(GameObject go, float grosor)
        {
            var borde = go.AddComponent<Outline>();
            borde.effectColor = Color.black;
            borde.effectDistance = new Vector2(grosor, -grosor);
        }

        static void Ubicar(RectTransform rt, Vector2 pos, Vector2 tamano)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamano;
        }
    }
}
