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
    }
}
