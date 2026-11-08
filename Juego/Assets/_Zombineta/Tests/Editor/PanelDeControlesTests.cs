using NUnit.Framework;
using Zombineta.Juego.Screens;
using Zombineta.Player;

namespace Zombineta.Juego.Tests
{
    /// <summary>El panel de controles de la pausa muestra teclas o botones segun el dispositivo.</summary>
    public class PanelDeControlesTests
    {
        [TestCase("Turbo", "→", "RT")]
        [TestCase("Reverse", "←", "LT")]
        [TestCase("Fire", "X", "X o RB")]
        [TestCase("Granada", "Z", "LB")]
        [TestCase("Headlight", "Espacio", "Y")]
        [TestCase("Mover", "Flechas", "Stick o cruceta")]
        [TestCase("Golpe", "X", "A")]
        [TestCase("Carril", "↑ / ↓", "Stick o cruceta ↑ / ↓")]
        [TestCase("Pausa", "Esc", "Start")]
        public void CadaAccionSeMuestraConLaTeclaOElBotonDelDispositivo(string accion, string teclado, string joystick)
        {
            Assert.AreEqual(teclado, PanelDeControles.Etiqueta(accion, ControlScheme.KeyboardMouse));
            Assert.AreEqual(joystick, PanelDeControles.Etiqueta(accion, ControlScheme.Gamepad));
        }

        [Test]
        public void UnaAccionSinEtiquetaMuestraUnGuion()
        {
            Assert.AreEqual("-", PanelDeControles.Etiqueta("NoExiste", ControlScheme.KeyboardMouse));
        }
    }
}
