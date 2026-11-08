using System;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Player;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Lista de controles del menu de pausa: cada linea muestra la tecla o el boton de una accion
    /// segun lo ultimo que uso el jugador (teclado o joystick), y se actualiza sola si cambia de
    /// dispositivo con el panel abierto. Las etiquetas salen de ControlHints, la misma tabla de
    /// la ayuda del tutorial.
    /// </summary>
    public sealed class PanelDeControles : MonoBehaviour
    {
        [Serializable]
        public sealed class Entrada
        {
            public Text texto;
            [Tooltip("Clave de ControlHints.Accion (Turbo, Fire, Granada...), o Carril / Pausa.")]
            public string accion;
        }

        [SerializeField] Entrada[] entradas = new Entrada[0];
        [Tooltip("Opcional: dice de que dispositivo son los controles que se estan mostrando.")]
        [SerializeField] Text dispositivo;

        // Guardado aparte de InputDeviceTracker.Shared por la misma razon que en ControlHints:
        // OnDisable tiene que desuscribirse del tracker al que se suscribio OnEnable.
        InputDeviceTracker tracker;

        void OnEnable()
        {
            tracker = InputDeviceTracker.Shared;
            Pintar(tracker != null ? tracker.Current : ControlScheme.KeyboardMouse);
            if (tracker != null)
                tracker.Changed += Pintar;
        }

        void OnDisable()
        {
            if (tracker != null)
                tracker.Changed -= Pintar;
            tracker = null;
        }

        /// <summary>Lo que se muestra para una accion en el esquema dado ("-" si no hay etiqueta).</summary>
        public static string Etiqueta(string accion, ControlScheme esquema)
        {
            bool joystick = esquema == ControlScheme.Gamepad;
            switch (accion)
            {
                case "Carril": return joystick ? "Stick o cruceta ↑ / ↓" : "↑ / ↓";
                case "Pausa": return joystick ? "Start" : "Esc";
            }
            return ControlHints.Accion(accion, esquema) ?? "-";
        }

        public static string NombreDelDispositivo(ControlScheme esquema) =>
            esquema == ControlScheme.Gamepad ? "Joystick" : "Teclado";

        void Pintar(ControlScheme esquema)
        {
            foreach (var e in entradas)
                if (e != null && e.texto != null)
                    e.texto.text = Etiqueta(e.accion, esquema);
            if (dispositivo != null)
                dispositivo.text = "Controles de: " + NombreDelDispositivo(esquema);
        }
    }
}
