using UnityEngine;
using UnityEngine.UI;
using Zombineta.Player;

namespace Zombineta.Juego.Screens
{
    /// <summary>Pinta la ayuda de controles (teclado/mouse o joystick) segun el ultimo dispositivo usado.</summary>
    public sealed class ControlHints : MonoBehaviour
    {
        [SerializeField] Text text;

        const string SecondLine = "En el aire, turbo y retroceso inclinan la moto.";

        public static string TextFor(ControlScheme scheme)
        {
            string firstLine = scheme == ControlScheme.Gamepad
                ? "Stick o cruceta carril · RT turbo · LT retroceso · X o RB disparar · Y faro · Start pausa"
                : "W/S o ↑/↓ carril · D turbo · A retroceso · X o click disparar · Espacio faro · Esc pausa";
            return firstLine + "\n" + SecondLine;
        }

        void OnEnable()
        {
            var tracker = InputDeviceTracker.Shared;
            if (tracker == null)
                return;
            Paint(tracker.Current);
            tracker.Changed += Paint;
        }

        void OnDisable()
        {
            var tracker = InputDeviceTracker.Shared;
            if (tracker != null)
                tracker.Changed -= Paint;
        }

        void Paint(ControlScheme scheme)
        {
            if (text != null)
                text.text = TextFor(scheme);
        }
    }
}
