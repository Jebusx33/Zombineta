using NUnit.Framework;
using UnityEngine.InputSystem;
using Zombineta.Juego.Screens;
using Zombineta.Player;

namespace Zombineta.Juego.Tests
{
    public class InputDeviceTrackerTests : InputTestFixture
    {
        [Test]
        public void DevicesMapToTheirScheme()
        {
            Assert.AreEqual(ControlScheme.Gamepad, InputDeviceTracker.SchemeOf(InputSystem.AddDevice<Gamepad>()));
            Assert.AreEqual(ControlScheme.KeyboardMouse, InputDeviceTracker.SchemeOf(InputSystem.AddDevice<Keyboard>()));
            Assert.AreEqual(ControlScheme.KeyboardMouse, InputDeviceTracker.SchemeOf(InputSystem.AddDevice<Mouse>()));
        }

        [Test]
        public void UsingAnAction_SwitchesTheCurrentScheme()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            var kb = InputSystem.AddDevice<Keyboard>();
            var action = new InputAction("probe", InputActionType.Button);
            action.AddBinding("<Gamepad>/buttonSouth");
            action.AddBinding("<Keyboard>/space");
            action.Enable();

            var tracker = new InputDeviceTracker();
            tracker.Attach();
            ControlScheme? changedTo = null;
            tracker.Changed += s => changedTo = s;

            Press(pad.buttonSouth);
            Assert.AreEqual(ControlScheme.Gamepad, tracker.Current);
            Assert.AreEqual(ControlScheme.Gamepad, changedTo);

            Release(pad.buttonSouth);
            Press(kb.spaceKey);
            Assert.AreEqual(ControlScheme.KeyboardMouse, tracker.Current);

            tracker.Detach();
            action.Dispose();
        }

        [Test]
        public void HintsText_MatchesTheScheme()
        {
            StringAssert.StartsWith("W/S o ↑/↓ carril", ControlHints.TextFor(ControlScheme.KeyboardMouse));
            StringAssert.StartsWith("Stick o cruceta carril", ControlHints.TextFor(ControlScheme.Gamepad));
            StringAssert.Contains("En el aire, turbo y retroceso inclinan la moto.", ControlHints.TextFor(ControlScheme.Gamepad));
        }

        // --- Accion / Resolver: la tabla que usan los marcadores {Turbo}, etc. de los carteles ---

        [Test]
        public void Accion_DevuelveLaTeclaPorEsquema()
        {
            Assert.AreEqual("D", ControlHints.Accion("Turbo", ControlScheme.KeyboardMouse));
            Assert.AreEqual("RT", ControlHints.Accion("Turbo", ControlScheme.Gamepad));
            Assert.AreEqual("A", ControlHints.Accion("Reverse", ControlScheme.KeyboardMouse));
            Assert.AreEqual("LT", ControlHints.Accion("Reverse", ControlScheme.Gamepad));
            Assert.AreEqual("Espacio", ControlHints.Accion("Headlight", ControlScheme.KeyboardMouse));
            Assert.AreEqual("Y", ControlHints.Accion("Headlight", ControlScheme.Gamepad));
        }

        [Test]
        public void Accion_ConNombreDesconocidoDevuelveNull()
        {
            Assert.IsNull(ControlHints.Accion("Salto", ControlScheme.KeyboardMouse));
        }

        [Test]
        public void Resolver_ReemplazaTodosLosMarcadoresDelTexto()
        {
            string texto = "Mantené {Turbo} para acelerar y {Reverse} para frenar.";
            Assert.AreEqual(
                "Mantené D para acelerar y A para frenar.",
                ControlHints.Resolver(texto, ControlScheme.KeyboardMouse));
            Assert.AreEqual(
                "Mantené RT para acelerar y LT para frenar.",
                ControlHints.Resolver(texto, ControlScheme.Gamepad));
        }

        [Test]
        public void Resolver_DejaUnMarcadorDesconocidoSinTocar()
        {
            Assert.AreEqual("Probá {Salto}", ControlHints.Resolver("Probá {Salto}", ControlScheme.KeyboardMouse));
        }

        [Test]
        public void Resolver_SinMarcadoresDevuelveElMismoTexto()
        {
            Assert.AreEqual("Texto sin marcadores", ControlHints.Resolver("Texto sin marcadores", ControlScheme.KeyboardMouse));
        }

        [Test]
        public void Resolver_NuloOVacioNoRompe()
        {
            Assert.IsNull(ControlHints.Resolver(null, ControlScheme.KeyboardMouse));
            Assert.AreEqual(string.Empty, ControlHints.Resolver(string.Empty, ControlScheme.KeyboardMouse));
        }
    }
}
