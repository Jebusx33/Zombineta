using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Izquierda/derecha cambian el valor de un toggle o un desplegable, como ya hace el slider:
    /// en el menu de opciones "Ajustar" funciona igual en todas las filas.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class AjusteConFlechas : MonoBehaviour, IMoveHandler
    {
        public void OnMove(AxisEventData eventData)
        {
            int paso = eventData.moveDir == MoveDirection.Left ? -1
                     : eventData.moveDir == MoveDirection.Right ? 1
                     : 0;
            if (paso == 0)
                return;

            if (TryGetComponent(out Toggle toggle) && toggle.IsInteractable())
            {
                toggle.isOn = !toggle.isOn;
            }
            else if (TryGetComponent(out Dropdown dropdown) && dropdown.IsInteractable())
            {
                int count = dropdown.options.Count;
                if (count > 0)
                    dropdown.value = (dropdown.value + paso + count) % count;
            }
        }
    }
}
