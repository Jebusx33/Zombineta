using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Zombineta.Core;
using Zombineta.Player;

namespace Zombineta.Juego.Tests
{
    public class PlayerInputReaderTests : InputTestFixture
    {
        const string ActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        Gamepad pad;
        Keyboard kb;
        Mouse mouse;
        InputActionAsset actions;
        GameObject go;
        PlayerInputReader reader;

        public override void Setup()
        {
            base.Setup();
            pad = InputSystem.AddDevice<Gamepad>();
            kb = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();

            // Copia del asset: los tests no habilitan el asset compartido del proyecto.
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            actions = InputActionAsset.FromJson(source.ToJson());

            go = new GameObject("reader");
            reader = go.AddComponent<PlayerInputReader>();
            reader.Bind(actions.FindActionMap("Moto", true));
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(actions);
            base.TearDown();
        }

        // --- Joystick ---------------------------------------------------------------

        [Test]
        public void StickUp_ChangesOneLane_EvenIfHeld()
        {
            Set(pad.leftStick, new Vector2(0f, 0.9f));
            Assert.AreEqual(1, reader.Read().LaneDelta);
            InputSystem.Update();
            Assert.AreEqual(0, reader.Read().LaneDelta);
        }

        [Test]
        public void StickMustComeBackBeforeTheNextLane()
        {
            Set(pad.leftStick, new Vector2(0f, 0.9f));
            reader.Read();
            Set(pad.leftStick, new Vector2(0f, 0.45f));   // todavia por encima de la liberacion
            Set(pad.leftStick, new Vector2(0f, 0.9f));
            Assert.AreEqual(0, reader.Read().LaneDelta);
            Set(pad.leftStick, new Vector2(0f, 0.1f));
            Set(pad.leftStick, new Vector2(0f, 0.9f));
            Assert.AreEqual(1, reader.Read().LaneDelta);
        }

        [Test]
        public void ASoftStickPush_DoesNotChangeLane()
        {
            Set(pad.leftStick, new Vector2(0f, 0.4f));
            Assert.AreEqual(0, reader.Read().LaneDelta);
        }

        [Test]
        public void DpadDown_GoesDownOneLane()
        {
            Press(pad.dpad.down);
            Assert.AreEqual(-1, reader.Read().LaneDelta);
        }

        [Test]
        public void RightTrigger_IsTurbo_FromThreshold()
        {
            Set(pad.rightTrigger, 0.2f);
            Assert.AreEqual(DriveMode.Normal, reader.Read().Mode);
            Set(pad.rightTrigger, 0.35f);
            Assert.AreEqual(DriveMode.Turbo, reader.Read().Mode);
        }

        [Test]
        public void LeftTrigger_IsReverse()
        {
            Set(pad.leftTrigger, 1f);
            Assert.AreEqual(DriveMode.Reverse, reader.Read().Mode);
        }

        [Test]
        public void BothTriggers_AreNormal()
        {
            Set(pad.leftTrigger, 1f);
            Set(pad.rightTrigger, 1f);
            Assert.AreEqual(DriveMode.Normal, reader.Read().Mode);
        }

        [Test]
        public void WestButtonAndRightShoulder_Fire()
        {
            Press(pad.buttonWest);
            Assert.IsTrue(reader.Read().Fire);
            Release(pad.buttonWest);
            Press(pad.rightShoulder);
            Assert.IsTrue(reader.Read().Fire);
        }

        [Test]
        public void NorthButton_TogglesTheHeadlight()
        {
            Press(pad.buttonNorth);
            Assert.IsTrue(reader.Read().ToggleHeadlight);
        }

        [Test]
        public void DebugWin_NeedsSelectHeld()
        {
            Press(pad.rightShoulder);
            Assert.IsFalse(reader.DebugWinPressed);
            Release(pad.rightShoulder);
            Press(pad.selectButton);
            Press(pad.rightShoulder);
            Assert.IsTrue(reader.DebugWinPressed);
        }

        [Test]
        public void DebugLose_IsSelectPlusLeftShoulder()
        {
            Press(pad.selectButton);
            Press(pad.leftShoulder);
            Assert.IsTrue(reader.DebugLosePressed);
        }

        // --- Movimiento libre a pie -----------------------------------------------------

        [Test]
        public void TheLeftStick_MovesFreelyInBothAxes()
        {
            Set(pad.leftStick, new Vector2(0.9f, -0.9f));
            var intent = reader.Read();

            Assert.IsTrue(intent.MovimientoLibre);
            Assert.Greater(intent.MoveX, 0.5f);
            Assert.Less(intent.MoveY, -0.5f);
        }

        [Test]
        public void TheDpad_MovesLeftAndRight_WithoutTheTriggers()
        {
            Press(pad.dpad.left);
            Assert.AreEqual(-1f, reader.Read().MoveX, 0.01f);
        }

        [Test]
        public void TheTriggers_NoLongerWalk()
        {
            Set(pad.rightTrigger, 1f);
            var intent = reader.Read();
            Assert.AreEqual(0f, intent.MoveX, 0.001f);
            Assert.AreEqual(DriveMode.Turbo, intent.Mode, "siguen siendo turbo en la moto");
        }

        [Test]
        public void ASmallStickDrift_IsIgnored()
        {
            Set(pad.leftStick, new Vector2(0.1f, 0.12f));
            var intent = reader.Read();
            Assert.AreEqual(0f, intent.MoveX, 0.001f);
            Assert.AreEqual(0f, intent.MoveY, 0.001f);
        }

        [Test]
        public void WasdAndArrows_Walk()
        {
            Press(kb.dKey);
            Press(kb.wKey);
            var intent = reader.Read();
            Assert.Greater(intent.MoveX, 0.5f);
            Assert.Greater(intent.MoveY, 0.5f);
            Assert.LessOrEqual(new Vector2(intent.MoveX, intent.MoveY).magnitude, 1.001f, "la diagonal no corre mas");
            Release(kb.dKey);
            Release(kb.wKey);

            Press(kb.leftArrowKey);
            Press(kb.downArrowKey);
            intent = reader.Read();
            Assert.Less(intent.MoveX, -0.5f);
            Assert.Less(intent.MoveY, -0.5f);
        }

        [Test]
        public void TheDeadzone_StartsFromZeroAndReachesOne()
        {
            Assert.AreEqual(Vector2.zero, PlayerInputReader.ApplyDeadzone(new Vector2(0.2f, 0f)));
            var apenas = PlayerInputReader.ApplyDeadzone(new Vector2(PlayerInputReader.MoverDeadzone + 0.01f, 0f));
            Assert.Less(apenas.x, 0.05f, "pasada la zona muerta arranca suave");
            Assert.AreEqual(1f, PlayerInputReader.ApplyDeadzone(new Vector2(1f, 0f)).magnitude, 0.001f);
            Assert.AreEqual(1f, PlayerInputReader.ApplyDeadzone(new Vector2(1f, 1f)).magnitude, 0.001f, "la diagonal del stick no pasa de 1");
        }

        // --- Teclado y mouse (lo de siempre sigue andando) --------------------------

        [Test]
        public void Keyboard_LanesModesHeadlightAndFire()
        {
            Press(kb.wKey);
            Assert.AreEqual(1, reader.Read().LaneDelta);
            Release(kb.wKey);

            Press(kb.dKey);
            Assert.AreEqual(DriveMode.Turbo, reader.Read().Mode);
            Press(kb.aKey);
            Assert.AreEqual(DriveMode.Normal, reader.Read().Mode);
            Release(kb.dKey);
            Assert.AreEqual(DriveMode.Reverse, reader.Read().Mode);
            Release(kb.aKey);

            Press(kb.spaceKey);
            Assert.IsTrue(reader.Read().ToggleHeadlight);
            Press(kb.xKey);
            Assert.IsTrue(reader.Read().Fire);
        }

        [Test]
        public void MouseClick_Fires()
        {
            Press(mouse.leftButton);
            Assert.IsTrue(reader.Read().Fire);
        }

        [Test]
        public void F2AndR_StillWork()
        {
            Press(kb.f2Key);
            Assert.IsTrue(reader.DebugWinPressed);
            Press(kb.rKey);
            Assert.IsTrue(reader.RestartPressed);
        }
    }
}
