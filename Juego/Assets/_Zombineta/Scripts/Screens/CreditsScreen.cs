using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Los creditos: arma un Text por linea de Creditos.asset dentro de un contenedor que sube
    /// solo, en tiempo real (unscaledDeltaTime), adentro de un viewport recortado (RectMask2D)
    /// que termina antes del boton Volver: el texto entra por el borde de abajo del viewport y
    /// sale recortado por el de arriba, sin pisar el boton. Submit acelera x3 mientras se lo
    /// mantiene apretado. Cuando la ultima linea pasa el borde de arriba del viewport se espera
    /// pausaFinal y se vuelve al menu; Cancel o el boton "Volver" vuelven al toque.
    /// </summary>
    public sealed class CreditsScreen : ScreenBase
    {
        [SerializeField] Creditos datos;

        [Tooltip("Recorte de arriba a un margen sobre el boton Volver: RectMask2D, por eso lo que " +
                 "sale de su rect no se ve.")]
        [SerializeField] RectTransform viewport;

        [Tooltip("Contenedor que sube: pivote arriba-centro, anclado abajo-centro del viewport.")]
        [SerializeField] RectTransform contenedor;

        [SerializeField] InputActionReference submitAction;
        [SerializeField] InputActionReference cancelAction;

        [Header("Medidas")]
        [SerializeField] float espacioSeccion = 70f;
        [SerializeField] float altoPortada = 110f;
        [SerializeField] float altoTitulo = 64f;
        [SerializeField] float altoLinea = 42f;
        [SerializeField] int tamanioPortada = 84;
        [SerializeField] int tamanioTitulo = 40;
        [SerializeField] int tamanioLinea = 30;
        [SerializeField] float anchoLinea = 1400f;
        [Tooltip("Separacion entre el nombre y el rol a cada lado del centro, en lineas \"Nombre | Rol\".")]
        [SerializeField] float medianil = 24f;

        [Header("Estilo")]
        [SerializeField] Font fuente;
        [SerializeField] Color colorTitulo = new Color32(255, 210, 26, 255);
        [SerializeField] Color colorTexto = Color.white;
        [SerializeField] Color colorRol = new Color32(250, 170, 235, 255);
        [SerializeField] Color colorSubrayado = new Color32(242, 27, 214, 255);
        [SerializeField] Color colorContorno = new Color(0.08f, 0.04f, 0.12f, 0.9f);

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

            var font = fuente != null ? fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float y = 0f;
            foreach (var seccion in datos.secciones)
            {
                if (seccion == null)
                    continue;

                if (!string.IsNullOrEmpty(seccion.titulo))
                {
                    if (seccion.portada)
                    {
                        AgregarTexto(seccion.titulo, font, tamanioPortada, FontStyle.Bold, colorTitulo,
                                     TextAnchor.MiddleCenter, 0f, anchoLinea, altoPortada, y);
                        y -= altoPortada;
                    }
                    else
                    {
                        AgregarTexto(seccion.titulo.ToUpperInvariant(), font, tamanioTitulo, FontStyle.Bold, colorTitulo,
                                     TextAnchor.MiddleCenter, 0f, anchoLinea, altoTitulo, y);
                        AgregarSubrayado(y - altoTitulo + 6f);
                        y -= altoTitulo + 14f;
                    }
                }

                if (seccion.lineas != null)
                    foreach (var linea in seccion.lineas)
                    {
                        AgregarLinea(linea ?? "", font, y);
                        y -= altoLinea;
                    }

                y -= espacioSeccion;
            }
            altoContenido = -y;

            contenedor.anchorMin = contenedor.anchorMax = new Vector2(0.5f, 0f);
            contenedor.pivot = new Vector2(0.5f, 1f);
            contenedor.anchoredPosition = Vector2.zero;
            espera = 0f;
            volviendo = false;
        }

        /// <summary>"Nombre | Rol" en dos columnas pegadas al centro; si no, una linea centrada.</summary>
        void AgregarLinea(string linea, Font font, float y)
        {
            int corte = linea.IndexOf('|');
            if (corte < 0)
            {
                AgregarTexto(linea, font, tamanioLinea, FontStyle.Normal, colorTexto,
                             TextAnchor.MiddleCenter, 0f, anchoLinea, altoLinea, y);
                return;
            }

            float mitad = anchoLinea / 2f - medianil;
            string nombre = linea.Substring(0, corte).Trim();
            string rol = linea.Substring(corte + 1).Trim();
            AgregarTexto(nombre, font, tamanioLinea, FontStyle.Bold, colorTexto,
                         TextAnchor.MiddleRight, -(medianil + mitad / 2f), mitad, altoLinea, y);
            AgregarTexto(rol, font, tamanioLinea, FontStyle.Normal, colorRol,
                         TextAnchor.MiddleLeft, medianil + mitad / 2f, mitad, altoLinea, y);
        }

        void AgregarTexto(string texto, Font font, int tamanio, FontStyle estilo, Color color,
                          TextAnchor alineacion, float x, float ancho, float alto, float y)
        {
            var go = new GameObject("Linea", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(contenedor, false);

            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = texto;
            text.fontSize = tamanio;
            text.fontStyle = estilo;
            text.alignment = alineacion;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            // El fondo es claro en el cielo: el contorno mantiene el texto legible encima.
            var contorno = go.AddComponent<Outline>();
            contorno.effectColor = colorContorno;
            contorno.effectDistance = new Vector2(2f, -2f);

            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(ancho, alto);
            rt.anchoredPosition = new Vector2(x, y);
        }

        void AgregarSubrayado(float y)
        {
            var go = new GameObject("Subrayado", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(contenedor, false);
            var image = go.GetComponent<Image>();
            image.color = colorSubrayado;
            image.raycastTarget = false;

            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(110f, 6f);
            rt.anchoredPosition = new Vector2(0f, y);
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

            float techo = viewport != null ? viewport.rect.height : ((RectTransform)transform).rect.height;
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
