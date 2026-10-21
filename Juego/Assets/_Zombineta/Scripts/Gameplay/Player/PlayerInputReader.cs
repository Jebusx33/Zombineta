using UnityEngine;
using UnityEngine.InputSystem;
using Zombineta.Core;

namespace Zombineta.Player
{
    /// <summary>
    /// Traduce los controles (teclado o joystick) a un PlayerIntent. Lee el mapa de acciones
    /// "Moto": que tecla o boton hace que vive en el asset de acciones, no aca.
    ///
    /// Los controles son los de un beat'em up y los botones cambian segun el modo:
    ///   - Moto:  X dispara, Z lanza una granada, Espacio la luz, flechas carril y velocidad.
    ///   - A pie: X ataca, Z dispara, C lanza una granada, Espacio pone la bomba,
    ///     flechas mueven.
    /// En el joystick cada boton hace lo mismo en los dos modos (A golpe, X o RB disparo, LB granada) salvo
    /// Y: la luz en la moto y la bomba a pie.
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
        InputAction firePie, granada, granadaPie, bomba;

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
            // Opcionales: un asset de acciones viejo sin la pelea a pie ni las granadas sigue andando.
            golpe = moto.FindAction("Golpe", false);
            debugSinNafta = moto.FindAction("DebugSinNafta", false);
            mover = moto.FindAction("Mover", false);
            firePie = moto.FindAction("FirePie", false);
            granada = moto.FindAction("Granada", false);
            granadaPie = moto.FindAction("GranadaPie", false);
            bomba = moto.FindAction("Bomba", false);
            moto.Enable();
        }

        /// <param name="aPie">La protagonista esta peleando a pie: el mismo boton hace otra cosa.</param>
        public PlayerIntent Read(bool aPie = false)
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

            // Espacio: la luz en la moto, la bomba a pie. Z: la granada en la moto, el disparo a pie (la granada
            // a pie es C). En el joystick: LB es la granada en los dos modos e Y la luz o la bomba.
            intent.ToggleHeadlight = !aPie && headlight.WasPressedThisFrame();
            var lanzar = aPie && granadaPie != null ? granadaPie : granada;
            intent.Granada = lanzar != null && lanzar.WasPressedThisFrame();
            intent.Bomba = aPie && bomba != null && bomba.WasPressedThisFrame();
            var disparo = aPie && firePie != null ? firePie : fire;
            intent.Fire = disparo.WasPressedThisFrame();

            // A pie: movimiento libre con stick, cruceta o flechas. Un asset de acciones viejo
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
