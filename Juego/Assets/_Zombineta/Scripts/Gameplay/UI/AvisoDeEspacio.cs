using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.UI
{
    /// <summary>
    /// El cartel "¡LOS ALEJASTE!" cuando dos disparos seguidos hacen retroceder a la horda y dejan
    /// espacio para respirar. Se crea solo al dar Play (arma su propio Canvas encima del HUD), asi
    /// que no hace falta ponerlo en ninguna escena, y sigue a cualquier RunController que aparezca.
    /// </summary>
    public sealed class AvisoDeEspacio : MonoBehaviour
    {
        const float SegundosEntreBusquedas = 0.5f;
        const float SegundosEnPantalla = 1.3f;

        static AvisoDeEspacio instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarStatic() => instancia = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null)
                return;
            var go = new GameObject("AvisoDeEspacio");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<AvisoDeEspacio>();
        }

        RunController run;
        Text texto;
        float restante;
        float proximaBusqueda;

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var go = new GameObject("Cartel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            texto = go.GetComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 96;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.text = "¡LOS ALEJASTE!";
            texto.color = new Color(0.55f, 1f, 0.6f);
            var rt = texto.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 230f);
            rt.sizeDelta = new Vector2(1600f, 160f);
            var sombra = go.AddComponent<Outline>();
            sombra.effectColor = Color.black;
            sombra.effectDistance = new Vector2(5f, -5f);
            texto.enabled = false;
        }

        void Update()
        {
            if (run == null && Time.unscaledTime >= proximaBusqueda)
            {
                proximaBusqueda = Time.unscaledTime + SegundosEntreBusquedas;
                var nuevo = FindAnyObjectByType<RunController>();
                if (nuevo != null)
                {
                    run = nuevo;
                    run.Stepped += AlPasar;
                }
            }

            if (restante <= 0f)
            {
                if (texto.enabled)
                    texto.enabled = false;
                return;
            }

            restante -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(restante / SegundosEnPantalla);
            texto.enabled = true;
            // Entra grande y se asienta; al final se desvanece.
            texto.rectTransform.localScale = Vector3.one * (1f + 0.35f * Mathf.Clamp01((k - 0.8f) / 0.2f));
            var c = texto.color;
            c.a = Mathf.Clamp01(k * 3f);
            texto.color = c;
        }

        void AlPasar(RunEvent ev)
        {
            if ((ev & RunEvent.HordaRetrocede) != 0)
                restante = SegundosEnPantalla;
        }

        void OnDestroy()
        {
            if (run != null)
                run.Stepped -= AlPasar;
            if (instancia == this)
                instancia = null;
        }
    }
}
