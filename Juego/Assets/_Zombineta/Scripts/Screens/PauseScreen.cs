using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>Capa encima del nivel. La accion Pausa tambien la cierra.</summary>
    public sealed class PauseScreen : ScreenBase
    {
        [SerializeField] InputActionReference pauseAction;

        [Header("Saltear tutorial")]
        [Tooltip("Boton 'Saltear tutorial': se muestra solo mientras Flow.EnTutorial.")]
        [SerializeField] Selectable skipTutorialButton;
        [Tooltip("Boton de arriba en la cadena de navegacion (Reintentar).")]
        [SerializeField] Selectable retryButton;
        [Tooltip("Boton de abajo en la cadena de navegacion (Opciones).")]
        [SerializeField] Selectable optionsButtonNav;

        void OnEnable()
        {
            pauseAction?.action.Enable();
            ActualizarSaltearTutorial();
        }

        /// <summary>
        /// El boton "Saltear tutorial" solo se ve con Flow.EnTutorial. Cuando esta oculto, la
        /// cadena de navegacion salta directo de Reintentar a Opciones (y viceversa) para no
        /// dejar un boton inactivo enganchado en el medio.
        /// </summary>
        void ActualizarSaltearTutorial()
        {
            if (skipTutorialButton == null)
                return;

            bool mostrar = Flow != null && Flow.EnTutorial;
            skipTutorialButton.gameObject.SetActive(mostrar);

            if (retryButton == null || optionsButtonNav == null)
                return;

            var retryNav = retryButton.navigation;
            retryNav.selectOnDown = mostrar ? skipTutorialButton : optionsButtonNav;
            retryButton.navigation = retryNav;

            var optionsNav = optionsButtonNav.navigation;
            optionsNav.selectOnUp = mostrar ? skipTutorialButton : retryButton;
            optionsButtonNav.navigation = optionsNav;

            if (mostrar)
            {
                var skipNav = skipTutorialButton.navigation;
                skipNav.selectOnUp = retryButton;
                skipNav.selectOnDown = optionsButtonNav;
                skipTutorialButton.navigation = skipNav;
            }
        }

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

        public void SkipTutorial() => Flow?.SkipTutorial();

        public void OpenOptions() => Flow?.OpenOptions();

        public void ToMenu() => Flow?.ToMainMenu();
    }
}
