using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        // Tabla unica de etiquetas por accion y esquema: la usan Accion/Resolver (marcadores
        // {Turbo}, {Reverse}, etc. en los carteles del tutorial). TextFor no sale de aca: es una
        // frase armada a mano, distinta por como combina las acciones en una sola linea.
        static readonly Dictionary<string, string> EtiquetasTeclado = new Dictionary<string, string>
        {
            { "Turbo", "D" },
            { "Reverse", "A" },
            { "LaneUp", "W/↑" },
            { "LaneDown", "S/↓" },
            { "Fire", "X o click" },
            { "Headlight", "Espacio" },
        };

        static readonly Dictionary<string, string> EtiquetasGamepad = new Dictionary<string, string>
        {
            { "Turbo", "RT" },
            { "Reverse", "LT" },
            { "LaneUp", "Stick o cruceta ↑" },
            { "LaneDown", "Stick o cruceta ↓" },
            { "Fire", "X o RB" },
            { "Headlight", "Y" },
        };

        static readonly Regex Marcador = new Regex("\\{(\\w+)\\}", RegexOptions.Compiled);

        public static string TextFor(ControlScheme scheme)
        {
            string firstLine = scheme == ControlScheme.Gamepad
                ? "Stick o cruceta carril · RT turbo · LT retroceso · X o RB disparar · Y faro · Start pausa"
                : "W/S o ↑/↓ carril · D turbo · A retroceso · X o click disparar · Espacio faro · Esc pausa";
            return firstLine + "\n" + SecondLine;
        }

        /// <summary>La tecla o el boton que hace esa accion en el esquema dado (o null si no existe).</summary>
        public static string Accion(string accion, ControlScheme scheme)
        {
            var tabla = scheme == ControlScheme.Gamepad ? EtiquetasGamepad : EtiquetasTeclado;
            return tabla.TryGetValue(accion, out var etiqueta) ? etiqueta : null;
        }

        /// <summary>
        /// Reemplaza los marcadores {Accion} de un texto (por ejemplo un cartel del tutorial) por
        /// la tecla o el boton del esquema actual. Un marcador sin accion conocida queda igual.
        /// </summary>
        public static string Resolver(string texto, ControlScheme scheme)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            return Marcador.Replace(texto, m =>
            {
                var etiqueta = Accion(m.Groups[1].Value, scheme);
                return etiqueta ?? m.Value;
            });
        }

        // Guardado aparte de InputDeviceTracker.Shared: si Shared cambiara mientras este objeto
        // esta habilitado (recarga de dominio, ciclo de Play/Edit), OnDisable debe desuscribirse
        // del MISMO tracker al que se suscribio OnEnable, no del que sea Shared en ese momento.
        InputDeviceTracker tracker;

        void OnEnable()
        {
            tracker = InputDeviceTracker.Shared;
            if (tracker == null)
                return;
            Paint(tracker.Current);
            tracker.Changed += Paint;
        }

        void OnDisable()
        {
            if (tracker != null)
                tracker.Changed -= Paint;
            tracker = null;
        }

        void Paint(ControlScheme scheme)
        {
            if (text != null)
                text.text = TextFor(scheme);
        }
    }
}
