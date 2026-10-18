using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.Auxilio
{
    /// <summary>
    /// El HUD de la pelea a pie: vida, cuanto falta para el auxilio y ayudas que quedan, mas los
    /// carteles grandes ("¡SIN NAFTA!", "¡AUXILIO!") y la vineta roja con poca vida. Va en el
    /// Canvas del HUD y arma sus propios elementos.
    /// </summary>
    public sealed class HudPelea : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Sprite vineta;
        [Tooltip("Con esta vida o menos, los bordes de la pantalla se ponen rojos.")]
        [SerializeField] int vidaCritica = 2;
        [SerializeField] float segundosCartel = 1.4f;

        Text auxilio, vida, ayudas, cartel;
        Image barra, fondoBarra, rojo;
        GameObject panel;
        float cartelRestante;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rojo = NuevaImagen("Vineta", transform, new Color(1f, 1f, 1f, 0f));
            rojo.sprite = vineta;
            Estirar(rojo.rectTransform);

            panel = new GameObject("Pelea", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var pr = (RectTransform)panel.transform;
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 1f);
            pr.anchoredPosition = new Vector2(0f, -30f);
            pr.sizeDelta = new Vector2(620f, 130f);

            auxilio = NuevoTexto("Auxilio", panel.transform, font, 34, TextAnchor.UpperCenter, new Vector2(0f, 0f), new Vector2(620f, 44f));
            fondoBarra = NuevaImagen("Barra fondo", panel.transform, new Color(0f, 0f, 0f, 0.6f));
            Ubicar(fondoBarra.rectTransform, new Vector2(0f, -48f), new Vector2(520f, 18f));
            barra = NuevaImagen("Barra", fondoBarra.transform, new Color(1f, 0.82f, 0.25f, 1f));
            barra.rectTransform.anchorMin = new Vector2(0f, 0f);
            barra.rectTransform.anchorMax = new Vector2(0f, 1f);
            barra.rectTransform.pivot = new Vector2(0f, 0.5f);
            barra.rectTransform.anchoredPosition = Vector2.zero;
            vida = NuevoTexto("Vida", panel.transform, font, 40, TextAnchor.UpperCenter, new Vector2(0f, -72f), new Vector2(620f, 50f));
            vida.color = new Color(1f, 0.3f, 0.35f);

            ayudas = NuevoTexto("Ayudas", transform, font, 26, TextAnchor.UpperRight, Vector2.zero, new Vector2(400f, 40f));
            var ar = ayudas.rectTransform;
            ar.anchorMin = ar.anchorMax = ar.pivot = new Vector2(1f, 1f);
            ar.anchoredPosition = new Vector2(-30f, -260f);

            cartel = NuevoTexto("Cartel", transform, font, 110, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1600f, 200f));
            var cr = cartel.rectTransform;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 0.5f);
            cr.anchoredPosition = new Vector2(0f, 120f);
            cartel.fontStyle = FontStyle.Bold;
            var sombra = cartel.gameObject.AddComponent<Outline>();
            sombra.effectColor = Color.black;
            sombra.effectDistance = new Vector2(5f, -5f);
            cartel.enabled = false;
        }

        void OnEnable()
        {
            if (run != null)
                run.Stepped += AlPasar;
        }

        void OnDisable()
        {
            if (run != null)
                run.Stepped -= AlPasar;
        }

        void AlPasar(RunEvent ev)
        {
            if ((ev & RunEvent.PeleaEmpieza) != 0)
                Mostrar("¡SIN NAFTA!", new Color(1f, 0.85f, 0.2f));
            if ((ev & RunEvent.AuxilioLlega) != 0)
                Mostrar("¡AUXILIO!", new Color(0.55f, 1f, 0.55f));
        }

        void Mostrar(string texto, Color color)
        {
            cartel.text = texto;
            cartel.color = color;
            cartelRestante = segundosCartel;
        }

        void Update()
        {
            if (run == null || run.Sim == null)
                return;

            var s = run.Sim.State;
            var c = run.Config;
            bool pelea = s.Phase == RunPhase.Pelea;

            panel.SetActive(pelea);
            if (pelea)
            {
                auxilio.text = s.FurgonetaEnCamino ? "¡LLEGO EL AUXILIO!" : "AUXILIO EN " + Mathf.CeilToInt(s.PeleaRestante);
                float llegada = c.peleaSegundos > 0f ? 1f - s.PeleaRestante / c.peleaSegundos : 1f;
                barra.rectTransform.sizeDelta = new Vector2(fondoBarra.rectTransform.rect.width * Mathf.Clamp01(llegada), 0f);
                vida.text = new string('♥', Mathf.Max(0, s.Vida)) + new string('♡', Mathf.Max(0, c.vidaPelea - s.Vida));
            }

            ayudas.enabled = c.ayudas > 0;
            if (ayudas.enabled)
                ayudas.text = "AUXILIOS: " + s.Ayudas;

            float rojoObjetivo = pelea && s.Vida <= vidaCritica ? 0.28f + 0.1f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            var rc = rojo.color;
            rc.a = Mathf.MoveTowards(rc.a, rojoObjetivo, Time.unscaledDeltaTime * 2f);
            rojo.color = rc;

            if (cartelRestante > 0f)
            {
                cartelRestante -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(cartelRestante / segundosCartel);
                cartel.enabled = true;
                float escala = 1f + (1f - k) * 0.15f;
                cartel.rectTransform.localScale = Vector3.one * escala;
                var col = cartel.color;
                col.a = Mathf.Clamp01(k * 3f);
                cartel.color = col;
            }
            else if (cartel.enabled)
                cartel.enabled = false;
        }

        static Text NuevoTexto(string nombre, Transform padre, Font font, int tam, TextAnchor anclaje, Vector2 pos, Vector2 tamano)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(padre, false);
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

        static Image NuevaImagen(string nombre, Transform padre, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            var i = go.GetComponent<Image>();
            i.color = color;
            i.raycastTarget = false;
            return i;
        }

        static void Ubicar(RectTransform rt, Vector2 pos, Vector2 tamano)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamano;
        }

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
