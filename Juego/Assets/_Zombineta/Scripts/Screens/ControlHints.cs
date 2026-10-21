using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Player;

namespace Zombineta.Juego.Screens
{
    /// <summary>Pinta la ayuda de controles (teclado o joystick) segun el ultimo dispositivo usado.</summary>
    public sealed class ControlHints : MonoBehaviour
    {
        [SerializeField] Text text;

        const string SecondLine = "En el aire, turbo y retroceso inclinan la moto.";

        // Tabla unica de etiquetas por accion y esquema: la usan Accion/Resolver (marcadores
        // {Turbo}, {Reverse}, etc. en los carteles del tutorial). TextFor no sale de aca: es una
        // frase armada a mano, distinta por como combina las acciones en una sola linea.
        //
        // Controles de beat'em up. Teclado: flechas y tres teclas, X y Z cambian de funcion segun el modo.
        // WASD sigue andando como alternativa, pero no se anuncia.
        static readonly Dictionary<string, string> EtiquetasTeclado = new Dictionary<string, string>
        {
            { "Turbo", "→" },
            { "Reverse", "←" },
            { "LaneUp", "↑" },
            { "LaneDown", "↓" },
            { "Fire", "X" },            // en la moto
            { "Granada", "Z" },         // en la moto
            { "GranadaPie", "C" },      // a pie
            { "Headlight", "Espacio" }, // en la moto
            { "Mover", "Flechas" },     // a pie
            { "Golpe", "X" },           // a pie
            { "FirePie", "Z" },         // a pie: el disparo
            { "Bomba", "Espacio" },     // a pie
        };

        static readonly Dictionary<string, string> EtiquetasGamepad = new Dictionary<string, string>
        {
            { "Turbo", "RT" },
            { "Reverse", "LT" },
            { "LaneUp", "Stick o cruceta ↑" },
            { "LaneDown", "Stick o cruceta ↓" },
            { "Fire", "X o RB" },
            { "Granada", "LB" },
            { "GranadaPie", "LB" },
            { "Headlight", "Y" },
            { "Mover", "Stick o cruceta" },
            { "Golpe", "A" },
            { "FirePie", "X o RB" },
            { "Bomba", "Y" },
        };

        static readonly Regex Marcador = new Regex("\\{(\\w+)\\}", RegexOptions.Compiled);

        // "carril" (combina LaneUp+LaneDown en una sola frase distinta de sus etiquetas
        // individuales) y "pausa" (la accion de pausa no esta en la tabla de marcadores) no se
        // pueden armar a partir de EtiquetasTeclado/EtiquetasGamepad sin inventar una regla ad
        // hoc: quedan como literales minimos. Turbo/Reverse/Fire/Headlight si duplicaban la tabla
        // letra por letra, asi que salen de Accion(...) para vivir en un solo lugar.
        public static string TextFor(ControlScheme scheme)
        {
            bool gamepad = scheme == ControlScheme.Gamepad;
            string carril = gamepad ? "Stick o cruceta carril" : "↑/↓ carril";
            string pausa = gamepad ? "Start pausa" : "Esc pausa";

            string firstLine = string.Join(" · ", new[]
            {
                carril,
                Accion("Turbo", scheme) + " turbo",
                Accion("Reverse", scheme) + " retroceso",
                Accion("Fire", scheme) + " disparar",
                Accion("Granada", scheme) + " granada",
                Accion("Headlight", scheme) + " faro",
                pausa,
            });
            return firstLine + "\n" + SecondLine;
        }

        /// <summary>Ayuda de la pelea a pie, con los controles del esquema que se esta usando.</summary>
        /// <param name="colorClave">Color HTML ("#FF4D40") para resaltar las teclas de ataque y disparo, o null para texto liso.</param>
        /// <param name="colorArsenal">Color HTML para resaltar las teclas de la granada y la bomba, o null para texto liso.</param>
        public static string TextForPelea(ControlScheme scheme, string colorClave = null, string colorArsenal = null)
        {
            string golpe = Resaltar(Accion("Golpe", scheme), colorClave);
            string disparo = Resaltar(Accion("FirePie", scheme), colorClave);
            string granada = Resaltar(Accion("GranadaPie", scheme), colorArsenal);
            string bomba = Resaltar(Accion("Bomba", scheme), colorArsenal);
            return "Mover: " + Accion("Mover", scheme) + "   ·   Atacar: " + golpe +
                   " (3 seguidos = remate)   ·   Disparo: " + disparo +
                   "   ·   Granada: " + granada + "   ·   Bomba: " + bomba;
        }

        static string Resaltar(string tecla, string color) =>
            string.IsNullOrEmpty(color) || string.IsNullOrEmpty(tecla) ? tecla : "<b><color=" + color + ">" + tecla + "</color></b>";

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
