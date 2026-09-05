using UnityEngine;
using UnityEngine.InputSystem;
using Zombineta.Core;

namespace Zombineta.Player
{
    /// <summary>
    /// Traduce teclado y mouse a un PlayerIntent. Es el unico lugar del proyecto
    /// que sabe que teclas existen: cambiar el esquema de control no toca reglas.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public PlayerIntent Read()
        {
            var intent = PlayerIntent.Idle;

            var kb = Keyboard.current;
            if (kb == null)
                return intent;

            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                intent.LaneDelta += 1;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                intent.LaneDelta -= 1;

            bool turbo = kb.rightArrowKey.isPressed || kb.dKey.isPressed;
            bool reverse = kb.leftArrowKey.isPressed || kb.aKey.isPressed;

            if (turbo && !reverse)
                intent.Mode = DriveMode.Turbo;
            else if (reverse && !turbo)
                intent.Mode = DriveMode.Reverse;
            else
                intent.Mode = DriveMode.Normal;

            intent.ToggleHeadlight = kb.spaceKey.wasPressedThisFrame;

            var mouse = Mouse.current;
            intent.Fire = kb.xKey.wasPressedThisFrame
                          || (mouse != null && mouse.leftButton.wasPressedThisFrame);

            return intent;
        }

        public bool RestartPressed =>
            Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
    }
}
