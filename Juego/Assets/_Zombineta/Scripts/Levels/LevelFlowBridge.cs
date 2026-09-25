using UnityEngine;
using UnityEngine.InputSystem;
using Zombineta.Core;
using Zombineta.Flow;
using Zombineta.Juego.Flow;
using Zombineta.Player;
using Zombineta.Tutorial;

namespace Zombineta.Juego.Levels
{
    /// <summary>Que final de partida arrancar este cuadro.</summary>
    public enum FinalDePartida
    {
        Ninguno,
        Victoria,
        Atrapada,
    }

    /// <summary>
    /// Une la partida con el flujo de pantallas: el final de camara (plano cerrado y camara lenta
    /// al ser atrapada, plano abierto al llegar) y despues avisar que se gano o se perdio; la
    /// accion Pausa; F2 y F3 en el editor; y el color del personaje elegido.
    /// Es lo que en el prototipo hacia ScreenFlow durante la partida.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class LevelFlowBridge : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] CameraFollow cameraRig;
        [SerializeField] ScooterView scooter;
        [SerializeField] InputActionReference pauseAction;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Color de la moto por personaje, en el orden de la pantalla de seleccion.")]
        [SerializeField] Color[] characterColors = { Color.white, new Color(0.75f, 0.9f, 1f) };

        [Tooltip("En el tutorial, la victoria espera al menos esto para que el cartel final " +
                 "('Listo! Ya sabes jugar') se llegue a leer, aunque el plano de camara sea mas corto.")]
        [SerializeField] float tutorialVictoryMinHold = 2f;

        float finaleLeft;
        bool finaleWon;
        bool finished;
        TutorialDirector tutorial;

        /// <summary>
        /// Decide el final segun la fase de la partida. Fuera del tutorial, Won es victoria y Lost
        /// es atrapada. En el tutorial nunca hay atrapada: un Lost es transitorio y lo deshace
        /// TutorialSesion (si arrancara el plano de "atrapada", quedaria pegado esperando un final
        /// que nunca llega). Y Won solo es victoria si el tutorial esta en su ultimo paso; si no
        /// (F2, un salto de tiempo), el director lo deshace y rebobina.
        /// </summary>
        public static FinalDePartida DecidirFinal(RunPhase fase, bool enTutorial, bool tutorialPermiteGanar)
        {
            if (fase == RunPhase.Won)
                return !enTutorial || tutorialPermiteGanar ? FinalDePartida.Victoria : FinalDePartida.Ninguno;
            if (fase == RunPhase.Lost)
                return enTutorial ? FinalDePartida.Ninguno : FinalDePartida.Atrapada;
            return FinalDePartida.Ninguno;
        }

        void OnEnable()
        {
            if (pauseAction != null)
                pauseAction.action.Enable();
        }

        void Start()
        {
            if (run != null)
                run.QuickRestartEnabled = false;

            var flow = GameRoot.Flow;
            if (flow != null && flow.EnTutorial)
                tutorial = FindAnyObjectByType<TutorialDirector>();

            if (scooter != null && flow != null && characterColors.Length > 0)
                scooter.SetCharacterColor(characterColors[Mathf.Clamp(flow.CharacterIndex, 0, characterColors.Length - 1)]);
        }

        void Update()
        {
            if (run == null || run.Sim == null || finished)
                return;

            if (finaleLeft > 0f)
            {
                // En tiempo real: durante la camara lenta el reloj del juego va mas lento.
                finaleLeft -= Time.unscaledDeltaTime;
                if (finaleLeft <= 0f)
                    Finish();
                return;
            }

            var flow = GameRoot.Flow;
            // Con la pausa u opciones encima, la partida espera y el congelado de un choque
            // no le devuelve el tiempo.
            bool held = flow != null && flow.Current != GameScreen.Playing;
            if (cameraRig != null)
                cameraRig.TimeHeld = held;
            if (held)
                return;

            var state = run.Sim.State;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (input != null && state.Phase == RunPhase.Running)
            {
                if (input.DebugWinPressed)
                {
                    // A la meta: el plano de victoria encuadra el refugio.
                    state.PlayerX = run.Config.goalDistance;
                    state.Phase = RunPhase.Won;
                }
                else if (input.DebugLosePressed && !(flow != null && flow.EnTutorial))
                {
                    // En el tutorial no se puede perder: F3 no hace nada.
                    state.Phase = RunPhase.Lost;
                    state.Loss = LossReason.CaughtByHorde;
                }
            }
#endif

            bool enTutorial = flow != null && flow.EnTutorial;
            var final = DecidirFinal(state.Phase, enTutorial, tutorial == null || tutorial.PermiteGanar);
            if (final != FinalDePartida.Ninguno)
            {
                BeginFinale(final == FinalDePartida.Victoria);
                return;
            }

            var root = GameRoot.Instance;
            if (flow != null && root != null && !root.Busy && Time.frameCount != root.LastChangeFrame &&
                pauseAction != null && pauseAction.action.WasPressedThisFrame() && flow.Pause() && cameraRig != null)
                cameraRig.TimeHeld = true;
        }

        void BeginFinale(bool won)
        {
            finaleWon = won;
            float hold = 0f;
            if (cameraRig != null)
                hold = won ? cameraRig.PlayVictory() : cameraRig.PlayCatch();

            // En el tutorial, ganar tiene que dar tiempo a leer el cartel final del
            // TutorialDirector: si el plano de victoria es mas corto que eso, se estira el final.
            var flow = GameRoot.Flow;
            if (won && flow != null && flow.EnTutorial)
                hold = Mathf.Max(hold, tutorialVictoryMinHold);

            if (hold <= 0f)
                Finish();
            else
                finaleLeft = hold;
        }

        void Finish()
        {
            finaleLeft = 0f;
            finished = true;
            if (cameraRig != null)
                cameraRig.EndFinale();
            Time.timeScale = 1f;

            var flow = GameRoot.Flow;
            if (flow == null)
                return;

            if (flow.EnTutorial)
            {
                // Ganar termina el tutorial; perder no llega aca (DecidirFinal nunca arranca la
                // atrapada en el tutorial).
                if (finaleWon)
                    flow.TutorialFinished();
                return;
            }

            if (finaleWon)
                flow.LevelWon();
            else
                flow.LevelLost();
        }
    }
}
