using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.Jefe
{
    /// <summary>
    /// El HUD del duelo: la vida del jefe arriba al centro, los corazones de la moto y el aviso de que se
    /// lo puede lastimar. Va en el Canvas del HUD y arma sus propios elementos, como HudPelea. Provisorio
    /// hasta que arte dibuje el suyo.
    /// </summary>
    public sealed class HudJefe : MonoBehaviour
    {
        const float AnchoBarra = 620f;
        const float AltoBarra = 22f;

        [SerializeField] RunController run;
        [SerializeField] Color colorVida = new Color(0.86f, 0.13f, 0.1f);
        [SerializeField] Color colorVulnerable = new Color(1f, 0.82f, 0.25f);
        [SerializeField] Color colorCorazones = new Color(1f, 0.3f, 0.35f);

        GameObject panel;
        Image vida;
        Text corazones, aviso;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();

            panel = new GameObject("Duelo", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var r = (RectTransform)panel.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -36f);
            r.sizeDelta = new Vector2(AnchoBarra, 140f);

            var fondo = NuevaImagen("Fondo", new Color(0f, 0f, 0f, 0.65f));
            Ubicar(fondo.rectTransform, Vector2.zero, new Vector2(AnchoBarra + 8f, AltoBarra + 8f));
            vida = NuevaImagen("Vida", colorVida);
            Ubicar(vida.rectTransform, new Vector2(-AnchoBarra * 0.5f, -4f), new Vector2(AnchoBarra, AltoBarra));
            vida.rectTransform.pivot = new Vector2(0f, 1f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            corazones = NuevoTexto("Corazones", font, 40, new Vector2(0f, -32f));
            corazones.color = colorCorazones;
            aviso = NuevoTexto("Aviso", font, 34, new Vector2(0f, -80f));
            aviso.fontStyle = FontStyle.Bold;
            aviso.text = "¡DISPARALE!";
            var borde = aviso.gameObject.AddComponent<Outline>();
            borde.effectColor = Color.black;
            borde.effectDistance = new Vector2(2f, -2f);
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
            float fraccion = jefe.Fase == FaseDeJefe.Entrada ? avance : jefe.Vida / (float)cfg.vida;
            vida.rectTransform.sizeDelta = new Vector2(AnchoBarra * fraccion, AltoBarra);

            bool vulnerable = jefe.Vulnerable;
            vida.color = vulnerable ? colorVulnerable : colorVida;
            corazones.text = new string('♥', jefe.Corazones) + new string('♡', cfg.corazones - jefe.Corazones);
            aviso.enabled = vulnerable;
            if (vulnerable)
                aviso.color = Color.Lerp(colorVulnerable, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f));
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

        Text NuevoTexto(string nombre, Font font, int tam, Vector2 pos)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel.transform, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = tam;
            t.alignment = TextAnchor.UpperCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Ubicar(t.rectTransform, pos, new Vector2(AnchoBarra, 50f));
            return t;
        }

        static void Ubicar(RectTransform rt, Vector2 pos, Vector2 tamano)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamano;
        }
    }
}
