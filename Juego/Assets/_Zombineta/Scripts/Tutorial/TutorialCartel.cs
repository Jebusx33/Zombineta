using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Zombineta.Tutorial
{
    /// <summary>
    /// La UI del tutorial: el cartel del paso actual (con fundido), el panel de avisos (mas
    /// chico, se muestra un rato y se apaga solo) y el marco que titila sobre la barra del HUD
    /// que senala el paso. No sabe nada de pasos, recursos ni horda: el TutorialDirector le dice
    /// que texto poner y que barra resaltar.
    /// </summary>
    public sealed class TutorialCartel : MonoBehaviour
    {
        [Header("Cartel del paso")]
        [SerializeField] CanvasGroup pasoGroup;
        [SerializeField] Text pasoText;
        [SerializeField] float duracionFade = 0.35f;

        [Header("Aviso")]
        [SerializeField] CanvasGroup avisoGroup;
        [SerializeField] Text avisoText;
        [SerializeField] float duracionFadeAviso = 0.25f;

        [Header("Resaltado")]
        [SerializeField] RectTransform resaltadoFrame;
        [SerializeField] Image resaltadoImage;
        [SerializeField] float resaltadoPadding = 8f;
        [SerializeField] float resaltadoBlinkSpeed = 4f;
        [SerializeField] float resaltadoAlphaMin = 0.35f;
        [SerializeField] float resaltadoAlphaMax = 1f;

        Coroutine pasoRoutine;
        Coroutine avisoRoutine;
        RectTransform[] resaltadoObjetivos;

        // El texto que "deberia" estar mostrandose ahora mismo: lo actualizan tanto MostrarPaso
        // como MostrarPasoInmediato, y es lo unico que lee FundirYCambiar en el momento del
        // cambio (no un string capturado al arrancar la corrutina). Asi, si un cambio de esquema
        // llega a mitad de un fundido, el ultimo texto pedido es siempre el que queda escrito,
        // sin importar el orden de llegada.
        string textoPendiente;

        void Awake()
        {
            if (pasoGroup != null) pasoGroup.alpha = 0f;
            if (avisoGroup != null) avisoGroup.alpha = 0f;
            if (resaltadoFrame != null)
            {
                // Pivot + anchors a punto fijo para que sizeDelta sea tamano absoluto y
                // position sea el centro. sizeDelta=zero evita el flash de un frame al
                // activar antes de que PosicionarResaltado lo corrija.
                resaltadoFrame.pivot = new Vector2(0.5f, 0.5f);
                resaltadoFrame.anchorMin = new Vector2(0.5f, 0.5f);
                resaltadoFrame.anchorMax = new Vector2(0.5f, 0.5f);
                resaltadoFrame.sizeDelta = Vector2.zero;
                resaltadoFrame.gameObject.SetActive(false);
            }
        }

        /// <summary>Cambia el cartel del paso con un fundido: se apaga, cambia el texto y se prende.</summary>
        public void MostrarPaso(string texto)
        {
            textoPendiente = texto;

            if (pasoGroup == null)
            {
                MostrarPasoInmediato(texto);
                return;
            }

            if (pasoRoutine != null)
                StopCoroutine(pasoRoutine);
            pasoRoutine = StartCoroutine(FundirYCambiar(pasoGroup, pasoText, duracionFade));
        }

        /// <summary>
        /// Cambia el texto sin fundido: para cuando solo cambio el dispositivo (los marcadores).
        /// Tambien actualiza textoPendiente, asi que si llega a mitad de un fundido en curso
        /// (MostrarPaso todavia corriendo), ese fundido escribe este texto en vez del que tenia
        /// capturado al arrancar.
        /// </summary>
        public void MostrarPasoInmediato(string texto)
        {
            textoPendiente = texto;
            if (pasoText != null)
                pasoText.text = texto;
            if (pasoGroup != null && pasoRoutine == null)
                pasoGroup.alpha = 1f;
        }

        /// <summary>Un aviso en el panel chico, visible "segundos" y despues se apaga solo.</summary>
        public void MostrarAviso(string texto, float segundos)
        {
            if (avisoText != null)
                avisoText.text = texto;
            if (avisoGroup == null)
                return;

            if (avisoRoutine != null)
                StopCoroutine(avisoRoutine);
            avisoRoutine = StartCoroutine(MostrarYOcultar(avisoGroup, segundos, duracionFadeAviso));
        }

        /// <summary>
        /// Marco titilante sobre uno o mas RectTransform (la barra del HUD, o las balas juntas).
        /// Null o vacio apaga el marco.
        /// </summary>
        public void Resaltar(RectTransform[] objetivos)
        {
            // Filtra nulls: un objetivo null (ej. ThreatBarRect cuando el campo no esta asignado)
            // hace que huboAlguno quede false y el frame quede activo con su tamano de canvas.
            resaltadoObjetivos = null;
            if (objetivos != null)
            {
                int count = 0;
                foreach (var o in objetivos) if (o != null) count++;
                if (count > 0)
                {
                    resaltadoObjetivos = new RectTransform[count];
                    int idx = 0;
                    foreach (var o in objetivos) if (o != null) resaltadoObjetivos[idx++] = o;
                }
            }

            if (resaltadoFrame == null)
                return;

            if (resaltadoObjetivos != null)
            {
                // Activar primero (SetSizeWithCurrentAnchors necesita que el parent este activo
                // para leer su tamano real). sizeDelta=zero en Awake evita el flash de un frame.
                resaltadoFrame.gameObject.SetActive(true);
                PosicionarResaltado();
            }
            else
            {
                resaltadoFrame.gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (resaltadoObjetivos == null || resaltadoFrame == null)
                return;

            PosicionarResaltado();

            if (resaltadoImage != null)
            {
                float onda = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * resaltadoBlinkSpeed);
                var c = resaltadoImage.color;
                c.a = Mathf.Lerp(resaltadoAlphaMin, resaltadoAlphaMax, onda);
                resaltadoImage.color = c;
            }
        }

        // Se reusa en cada cuadro: GetWorldCorners escribe sobre el mismo arreglo.
        readonly Vector3[] esquinas = new Vector3[4];

        void PosicionarResaltado()
        {
            bool huboAlguno = false;
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;
            var corners = esquinas;

            foreach (var objetivo in resaltadoObjetivos)
            {
                if (objetivo == null)
                    continue;

                objetivo.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    float x = corners[i].x;
                    float y = corners[i].y;
                    if (!huboAlguno)
                    {
                        minX = maxX = x;
                        minY = maxY = y;
                        huboAlguno = true;
                    }
                    else
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (!huboAlguno)
            {
                resaltadoFrame.gameObject.SetActive(false);
                return;
            }

            // Screen Space - Overlay: coordenadas de mundo == coordenadas de pantalla.
            // SetSizeWithCurrentAnchors calcula el sizeDelta correcto para cualquier
            // configuracion de anchors, evitando el bug donde anchors stretch producen
            // un frame del tamano de la pantalla completa.
            // position (no anchoredPosition) acepta coordenadas de mundo directamente,
            // tambien independiente de anchors y pivot.
             float pad = resaltadoPadding;
            resaltadoFrame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxX - minX + pad );
            resaltadoFrame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxY - minY + pad );
             resaltadoFrame.position = new Vector3(0f, 0f, 0f);
            resaltadoFrame.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        }

        IEnumerator FundirYCambiar(CanvasGroup grupo, Text texto, float duracion)
        {
            float mitad = Mathf.Max(0.01f, duracion * 0.5f);
            yield return Fundir(grupo, grupo.alpha, 0f, mitad);
            // Lee textoPendiente ahora, no un valor capturado al arrancar: si MostrarPasoInmediato
            // se llamo mientras se apagaba (cambio de esquema a mitad del fundido), gana su texto.
            if (texto != null)
                texto.text = textoPendiente;
            yield return Fundir(grupo, 0f, 1f, mitad);
            pasoRoutine = null;
        }

        IEnumerator MostrarYOcultar(CanvasGroup grupo, float segundos, float duracionFadeOut)
        {
            grupo.alpha = 1f;
            float t = 0f;
            while (t < segundos)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return Fundir(grupo, grupo.alpha, 0f, duracionFadeOut);
            avisoRoutine = null;
        }

        static IEnumerator Fundir(CanvasGroup grupo, float desde, float hasta, float duracion)
        {
            if (duracion <= 0f)
            {
                grupo.alpha = hasta;
                yield break;
            }

            float t = 0f;
            while (t < duracion)
            {
                t += Time.unscaledDeltaTime;
                grupo.alpha = Mathf.Lerp(desde, hasta, t / duracion);
                yield return null;
            }
            grupo.alpha = hasta;
        }
    }
}
