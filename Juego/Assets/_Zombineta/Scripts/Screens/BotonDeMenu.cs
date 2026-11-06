using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Aspecto de un boton del menu principal: el seleccionado muestra la tira clara con su borde
    /// rosa y el texto y el icono pasan a oscuro; los demas son texto y icono blancos sobre el
    /// panel. El mouse selecciona al pasar por encima, asi mouse y teclado muestran lo mismo.
    /// El menu se arma con MenuPrincipalBuilder.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class BotonDeMenu : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [SerializeField] GameObject resaltado;
        [SerializeField] Graphic icono;
        [SerializeField] Text texto;
        [SerializeField] Color colorNormal = Color.white;
        [SerializeField] Color colorSeleccionado = new Color(0.12f, 0.08f, 0.18f, 1f);

        void OnEnable()
        {
            var es = EventSystem.current;
            Aplicar(es != null && es.currentSelectedGameObject == gameObject);
        }

        public void OnSelect(BaseEventData eventData) => Aplicar(true);

        public void OnDeselect(BaseEventData eventData) => Aplicar(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        void Aplicar(bool seleccionado)
        {
            var color = seleccionado ? colorSeleccionado : colorNormal;
            if (resaltado != null) resaltado.SetActive(seleccionado);
            if (icono != null) icono.color = color;
            if (texto != null) texto.color = color;
        }
    }
}
