using UnityEngine;
using UnityEngine.InputSystem;
using Zombineta.Core;
using Zombineta.Flow;
using Zombineta.Juego.Flow;
using Zombineta.Player;

namespace Zombineta.Juego.Levels
{
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

        [Tooltip("Color de la moto por personaje, en el orden de la pantalla de seleccion.")]
        [SerializeField] Color[] characterColors = { Color.white, new Color(0.75f, 0.9f, 1f) };

        float finaleLeft;
        bool finaleWon;
        bool finished;

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
            // Con la pausa u opciones encima, la partida espera.
            if (flow != null && flow.Current != GameScreen.Playing)
                return;

            var state = run.Sim.State;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var kb = Keyboard.current;
            if (kb != null && state.Phase == RunPhase.Running)
            {
                if (kb.f2Key.wasPressedThisFrame)
                {
                    // A la meta: el plano de victoria encuadra el refugio.
                    state.PlayerX = run.Config.goalDistance;
                    state.Phase = RunPhase.Won;
                }
                else if (kb.f3Key.wasPressedThisFrame)
                {
                    state.Phase = RunPhase.Lost;
                    state.Loss = LossReason.CaughtByHorde;
                }
            }
#endif

            if (state.Phase == RunPhase.Won || state.Phase == RunPhase.Lost)
            {
                BeginFinale(state.Phase == RunPhase.Won);
                return;
            }

            var root = GameRoot.Instance;
            if (flow != null && root != null && !root.Busy && Time.frameCount != root.LastChangeFrame &&
                pauseAction != null && pauseAction.action.WasPressedThisFrame())
                flow.Pause();
        }

        void BeginFinale(bool won)
        {
            finaleWon = won;
            float hold = 0f;
            if (cameraRig != null)
                hold = won ? cameraRig.PlayVictory() : cameraRig.PlayCatch();
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
            if (finaleWon)
                flow.LevelWon();
            else
                flow.LevelLost();
        }
    }
}
