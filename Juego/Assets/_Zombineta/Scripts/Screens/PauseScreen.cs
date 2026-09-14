using UnityEngine;
using UnityEngine.InputSystem;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>Capa encima del nivel. La accion Pausa tambien la cierra.</summary>
    public sealed class PauseScreen : ScreenBase
    {
        [SerializeField] InputActionReference pauseAction;

        void OnEnable() => pauseAction?.action.Enable();

        protected override void Update()
        {
            base.Update();

            // Solo si la pausa esta arriba: con las opciones abiertas, Esc es de las opciones.
            var root = GameRoot.Instance;
            if (root == null || Flow == null || root.TopScene != gameObject.scene.name || InputConsumed)
                return;
            if (pauseAction != null && pauseAction.action.WasPressedThisFrame())
                Flow.Resume();
        }

        public void Resume() => Flow?.Resume();

        public void Retry() => Flow?.Retry();

        public void OpenOptions() => Flow?.OpenOptions();

        public void ToMenu() => Flow?.ToMainMenu();
    }
}
