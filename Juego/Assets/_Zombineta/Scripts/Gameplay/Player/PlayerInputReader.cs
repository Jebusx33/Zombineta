using UnityEngine;
using UnityEngine.InputSystem;
using Zombineta.Core;

namespace Zombineta.Player
{
    /// <summary>
    /// Traduce los controles (teclado, mouse o joystick) a un PlayerIntent. Lee el mapa de
    /// acciones "Moto": que tecla o boton hace que vive en el asset de acciones, no aca.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        /// <summary>Desde cuanto cuenta un gatillo como apretado.</summary>
        public const float TriggerThreshold = 0.3f;

        [SerializeField] InputActionAsset actions;

        InputActionMap map;
        InputAction laneUp, laneDown, turbo, reverse, fire, headlight, quickRestart, debugWin, debugLose;

        void OnEnable()
        {
            if (map == null && actions != null)
                Bind(actions.FindActionMap("Moto", true));
            map?.Enable();
        }

        void OnDisable() => map?.Disable();

        public void Bind(InputActionMap moto)
        {
            map = moto;
            laneUp = moto.FindAction("LaneUp", true);
            laneDown = moto.FindAction("LaneDown", true);
            turbo = moto.FindAction("Turbo", true);
            reverse = moto.FindAction("Reverse", true);
            fire = moto.FindAction("Fire", true);
            headlight = moto.FindAction("Headlight", true);
            quickRestart = moto.FindAction("QuickRestart", true);
            debugWin = moto.FindAction("DebugWin", true);
            debugLose = moto.FindAction("DebugLose", true);
            moto.Enable();
        }

        public PlayerIntent Read()
        {
            var intent = PlayerIntent.Idle;
            if (map == null)
                return intent;

            if (laneUp.WasPressedThisFrame())
                intent.LaneDelta += 1;
            if (laneDown.WasPressedThisFrame())
                intent.LaneDelta -= 1;

            bool isTurbo = turbo.ReadValue<float>() >= TriggerThreshold;
            bool isReverse = reverse.ReadValue<float>() >= TriggerThreshold;
            if (isTurbo && !isReverse)
                intent.Mode = DriveMode.Turbo;
            else if (isReverse && !isTurbo)
                intent.Mode = DriveMode.Reverse;

            intent.ToggleHeadlight = headlight.WasPressedThisFrame();
            intent.Fire = fire.WasPressedThisFrame();
            return intent;
        }

        public bool RestartPressed => quickRestart != null && quickRestart.WasPressedThisFrame();
        public bool DebugWinPressed => debugWin != null && debugWin.WasPressedThisFrame();
        public bool DebugLosePressed => debugLose != null && debugLose.WasPressedThisFrame();
    }
}
