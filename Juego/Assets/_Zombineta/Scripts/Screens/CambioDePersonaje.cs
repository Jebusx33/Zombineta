using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// En el selector de personaje, izquierda y derecha cambian de repartidora en vez de mover el
    /// foco (arriba y abajo siguen navegando, hasta Volver).
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class CambioDePersonaje : MonoBehaviour, IMoveHandler
    {
        [SerializeField] CharacterSelectScreen pantalla;

        public void OnMove(AxisEventData eventData)
        {
            if (pantalla == null)
                return;
            if (eventData.moveDir == MoveDirection.Left)
                pantalla.Cambiar(-1);
            else if (eventData.moveDir == MoveDirection.Right)
                pantalla.Cambiar(1);
        }
    }
}
