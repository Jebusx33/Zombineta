using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Una fila del menu de opciones: se resalta mientras su control esta seleccionado, toma la
    /// seleccion al pasar el mouse y muestra el valor a la derecha (50%, Si/No).
    /// </summary>
    public sealed class OpcionFila : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] Selectable control;
        [SerializeField] Graphic resaltado;
        [SerializeField] Text etiqueta;
        [Tooltip("Opcional: el valor del slider (en %) o del toggle (Si/No).")]
        [SerializeField] Text valor;
        [SerializeField] Color etiquetaNormal = Color.white;
        [SerializeField] Color etiquetaSeleccionada = new Color(1f, 0.82f, 0.1f);

        public Selectable Control => control;

        void LateUpdate()
        {
            var es = EventSystem.current;
            bool on = es != null && control != null && es.currentSelectedGameObject == control.gameObject;

            if (resaltado != null)
                resaltado.enabled = on;
            if (etiqueta != null)
                etiqueta.color = on ? etiquetaSeleccionada : etiquetaNormal;
            if (valor != null)
                valor.text = Valor();
        }

        string Valor()
        {
            if (control is Slider s)
                return Mathf.RoundToInt(Mathf.InverseLerp(s.minValue, s.maxValue, s.value) * 100f) + "%";
            if (control is Toggle t)
                return t.isOn ? "Sí" : "No";
            return valor.text;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var es = EventSystem.current;
            if (es != null && control != null && control.IsInteractable())
                es.SetSelectedGameObject(control.gameObject);
        }
    }
}
