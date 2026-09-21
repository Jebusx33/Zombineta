using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Los creditos: arma un Text por linea de Creditos.asset dentro de un contenedor que sube
    /// solo, en tiempo real (unscaledDeltaTime). Submit acelera x3 mientras se lo mantiene
    /// apretado. Cuando la ultima linea pasa el borde de arriba se espera pausaFinal y se
    /// vuelve al menu; Cancel o el boton "Volver" vuelven al toque.
    /// </summary>
    public sealed class CreditsScreen : ScreenBase
    {
        [SerializeField] Creditos datos;

        [Tooltip("Contenedor que sube: pivote arriba-centro, anclado abajo-centro del canvas.")]
        [SerializeField] RectTransform contenedor;

        [SerializeField] InputActionReference submitAction;
        [SerializeField] InputActionReference cancelAction;

        [SerializeField] float espacioSeccion = 60f;
        [SerializeField] float altoTitulo = 56f;
        [SerializeField] float altoLinea = 40f;
        [SerializeField] int tamanioTitulo = 44;
        [SerializeField] int tamanioLinea = 32;
        [SerializeField] float anchoLinea = 1400f;

        float altoContenido;
        float espera;
        bool volviendo;

        void Awake() => ArmarLineas();

        void OnEnable()
        {
            submitAction?.action.Enable();
            cancelAction?.action.Enable();
        }

        void ArmarLineas()
        {
            if (contenedor == null || datos == null)
                return;

            // Por si se vuelve a abrir la escena sin recargarla (dominio no recargado en el editor).
            for (int i = contenedor.childCount - 1; i >= 0; i--)
                Destroy(contenedor.GetChild(i).gameObject);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float y = 0f;
            foreach (var seccion in datos.secciones)
            {
                if (seccion == null)
                    continue;

                if (!string.IsNullOrEmpty(seccion.titulo))
                    AgregarLinea(seccion.titulo, font, altoTitulo, tamanioTitulo, FontStyle.Bold, ref y);

                if (seccion.lineas != null)
                    foreach (var linea in seccion.lineas)
                        AgregarLinea(linea, font, altoLinea, tamanioLinea, FontStyle.Normal, ref y);

                y -= espacioSeccion;
            }
            altoContenido = -y;

            contenedor.anchorMin = contenedor.anchorMax = new Vector2(0.5f, 0f);
            contenedor.pivot = new Vector2(0.5f, 1f);
            contenedor.anchoredPosition = Vector2.zero;
            espera = 0f;
            volviendo = false;
        }

        void AgregarLinea(string texto, Font font, float alto, int tamanio, FontStyle estilo, ref float y)
        {
            var go = new GameObject("Linea", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(contenedor, false);

            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = texto;
            text.fontSize = tamanio;
            text.fontStyle = estilo;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(anchoLinea, alto);
            rt.anchoredPosition = new Vector2(0f, y);

            y -= alto;
        }

        protected override void Update()
        {
            base.Update();

            var root = GameRoot.Instance;
            if (root == null || Flow == null || volviendo || contenedor == null)
                return;
            if (root.TopScene != gameObject.scene.name)
                return;

            if (!InputConsumed && cancelAction != null && cancelAction.action.WasPressedThisFrame())
            {
                Volver();
                return;
            }

            bool rapido = submitAction != null && submitAction.action.IsPressed();
            float velocidad = (datos != null ? datos.velocidad : 60f) * (rapido ? 3f : 1f);

            var pos = contenedor.anchoredPosition;
            pos.y += velocidad * Time.unscaledDeltaTime;
            contenedor.anchoredPosition = pos;

            float techo = ((RectTransform)transform).rect.height;
            if (pos.y - altoContenido >= techo)
            {
                espera += Time.unscaledDeltaTime;
                if (espera >= (datos != null ? datos.pausaFinal : 2f))
                    Volver();
            }
        }

        public void Volver()
        {
            if (volviendo)
                return;
            volviendo = true;
            Flow?.ToMainMenu();
        }
    }
}
