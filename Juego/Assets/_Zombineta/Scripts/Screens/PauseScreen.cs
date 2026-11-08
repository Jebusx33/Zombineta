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

        [Header("Controles")]
        [Tooltip("Lo que se ve en la pausa normal; se oculta mientras estan los controles.")]
        [SerializeField] GameObject panelPrincipal;
        [Tooltip("Panel con la lista de controles (PanelDeControles). Arranca apagado.")]
        [SerializeField] GameObject panelControles;
        [Tooltip("Fila 'Controles' de la pausa: recupera la seleccion al cerrar el panel.")]
        [SerializeField] Selectable controlesButton;
        [Tooltip("Fila 'Volver' del panel de controles: toma la seleccion al abrirlo.")]
        [SerializeField] Selectable volverControlesButton;

        bool ControlesAbiertos => panelControles != null && panelControles.activeSelf;

        void OnEnable()
        {
            pauseAction?.action.Enable();
            ActualizarSaltearTutorial();
            // Siempre se abre en la pausa normal.
            if (panelControles != null)
                panelControles.SetActive(false);
            if (panelPrincipal != null)
                panelPrincipal.SetActive(true);
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

            // Con los controles abiertos, el foco vive en su fila 'Volver': la base apunta a la
            // primera fila de la pausa, que esta oculta.
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (ControlesAbiertos && es != null && volverControlesButton != null &&
                (es.currentSelectedGameObject == null || !es.currentSelectedGameObject.activeInHierarchy))
                es.SetSelectedGameObject(volverControlesButton.gameObject);

            // Solo si la pausa esta arriba: con las opciones abiertas, Esc es de las opciones.
            var root = GameRoot.Instance;
            if (root == null || Flow == null || root.TopScene != gameObject.scene.name || InputConsumed)
                return;
            if (pauseAction != null && pauseAction.action.WasPressedThisFrame())
            {
                // Esc con los controles abiertos vuelve a la pausa; no sigue el juego.
                if (ControlesAbiertos)
                    CerrarControles();
                else
                    Flow.Resume();
            }
        }

        public void AbrirControles()
        {
            if (panelControles == null)
                return;
            if (panelPrincipal != null)
                panelPrincipal.SetActive(false);
            panelControles.SetActive(true);
            SeleccionarSiHay(volverControlesButton);
        }

        public void CerrarControles()
        {
            if (panelControles == null)
                return;
            panelControles.SetActive(false);
            if (panelPrincipal != null)
                panelPrincipal.SetActive(true);
            SeleccionarSiHay(controlesButton);
        }

        static void SeleccionarSiHay(Selectable s)
        {
            if (s != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(s.gameObject);
        }

        public void Resume() => Flow?.Resume();

        public void Retry() => Flow?.Retry();

        public void SkipTutorial() => Flow?.SkipTutorial();

        public void OpenOptions() => Flow?.OpenOptions();

        public void ToMenu() => Flow?.ToMainMenu();
    }
}
