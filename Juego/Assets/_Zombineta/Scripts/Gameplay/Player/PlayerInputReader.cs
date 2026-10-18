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
        /// <summary>Radio del stick por debajo del cual no cuenta (el joystick nunca queda en cero justo).</summary>
        public const float MoverDeadzone = 0.25f;

        InputAction laneUp, laneDown, turbo, reverse, fire, headlight, quickRestart, debugWin, debugLose, golpe, debugSinNafta, mover;

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
            // Opcionales: un asset de acciones viejo sin la pelea a pie sigue andando.
            golpe = moto.FindAction("Golpe", false);
            debugSinNafta = moto.FindAction("DebugSinNafta", false);
            mover = moto.FindAction("Mover", false);
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

            // A pie: movimiento libre con stick, cruceta, flechas o WASD. Un asset de acciones viejo
            // sin "Mover" sigue andando como antes: turbo y retroceso son derecha e izquierda.
            if (mover != null)
            {
                var v = ApplyDeadzone(mover.ReadValue<Vector2>());
                intent.MoveX = v.x;
                intent.MoveY = v.y;
                intent.MovimientoLibre = true;
            }
            else
            {
                intent.MoveX = Mathf.Clamp(turbo.ReadValue<float>() - reverse.ReadValue<float>(), -1f, 1f);
            }
            intent.Golpe = golpe != null && golpe.WasPressedThisFrame();
            return intent;
        }

        /// <summary>
        /// Zona muerta circular y reescalada: justo pasado el borde de la zona el movimiento
        /// arranca en cero, no de golpe, y el maximo sigue siendo 1.
        /// </summary>
        public static Vector2 ApplyDeadzone(Vector2 v, float deadzone = MoverDeadzone)
        {
            float m = v.magnitude;
            if (m <= deadzone)
                return Vector2.zero;
            float k = Mathf.Min(1f, (m - deadzone) / (1f - deadzone)) / m;
            return v * k;
        }

        public bool RestartPressed => quickRestart != null && quickRestart.WasPressedThisFrame();
        public bool DebugWinPressed => debugWin != null && debugWin.WasPressedThisFrame();
        public bool DebugLosePressed => debugLose != null && debugLose.WasPressedThisFrame();
        public bool DebugSinNaftaPressed => debugSinNafta != null && debugSinNafta.WasPressedThisFrame();
    }
}
