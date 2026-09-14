using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Base de las pantallas con menu. Selecciona el primer boton al abrirse, y lo recupera si
    /// la seleccion se pierde, solo si esta es la escena de arriba: una pausa tapada por las
    /// opciones no le roba el foco.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        [Tooltip("Boton que queda seleccionado al abrir: con teclado o joystick se navega desde ahi.")]
        [SerializeField] protected Selectable firstSelected;

        protected static GameFlow Flow => GameRoot.Flow;

        /// <summary>Una tecla del mismo frame en que cambio la pantalla ya la uso otro.</summary>
        protected static bool InputConsumed =>
            GameRoot.Instance == null || Time.frameCount == GameRoot.Instance.LastChangeFrame;

        protected virtual void Update()
        {
            var es = EventSystem.current;
            var root = GameRoot.Instance;
            if (es == null || root == null || firstSelected == null)
                return;
            if (root.TopScene != gameObject.scene.name)
                return;

            var current = es.currentSelectedGameObject;
            if (current == null || current.scene != gameObject.scene || !current.activeInHierarchy)
                es.SetSelectedGameObject(firstSelected.gameObject);
        }
    }
}
