using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Fx;

namespace Zombineta.Juego.Tests
{
    public class RumbleDirectorTests
    {
        RumbleConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<RumbleConfig>();
            config.Set(RumbleKind.Crash, 0.6f, 0.4f, 0.25f);
            config.Set(RumbleKind.Shot, 0.05f, 0.5f, 0.1f);
            config.Set(RumbleKind.Caught, 1f, 0.8f, 0.9f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void AnEvent_StartsAtItsStrength_AndFadesToZero()
        {
            var d = new RumbleDirector(config);
            d.Trigger(RumbleKind.Crash);
            d.Tick(0f);
            Assert.AreEqual(0.6f, d.Low, 1e-4f);
            Assert.AreEqual(0.4f, d.High, 1e-4f);

            d.Tick(0.125f);
            Assert.AreEqual(0.3f, d.Low, 1e-4f);

            d.Tick(0.2f);
            Assert.AreEqual(0f, d.Low);
            Assert.AreEqual(0f, d.High);
        }

        [Test]
        public void TwoEvents_EachMotorTakesTheStrongest()
        {
            var d = new RumbleDirector(config);
            d.Trigger(RumbleKind.Crash);
            d.Trigger(RumbleKind.Shot);
            d.Tick(0f);
            Assert.AreEqual(0.6f, d.Low, 1e-4f);
            Assert.AreEqual(0.5f, d.High, 1e-4f);
        }

        [Test]
        public void RetriggeringTheSameKind_RestartsItsFade()
        {
            var d = new RumbleDirector(config);
            d.Trigger(RumbleKind.Crash);
            d.Tick(0.2f);
            d.Trigger(RumbleKind.Crash);
            d.Tick(0f);
            Assert.AreEqual(0.6f, d.Low, 1e-4f);
        }

        [Test]
        public void Stop_SilencesEverything()
        {
            var d = new RumbleDirector(config);
            d.Trigger(RumbleKind.Caught);
            d.Stop();
            d.Tick(0f);
            Assert.AreEqual(0f, d.Low);
            Assert.AreEqual(0f, d.High);
        }

        [Test]
        public void AKindWithoutConfig_DoesNothing()
        {
            var d = new RumbleDirector(config);
            d.Trigger(RumbleKind.Explosion);
            d.Tick(0f);
            Assert.AreEqual(0f, d.Low);
        }

        [Test]
        public void RunEvents_MapToTheirRumble()
        {
            var d = new RumbleDirector(config);
            d.Trigger(RunEvent.Crashed | RunEvent.Shot | RunEvent.PickedUp);
            d.Tick(0f);
            Assert.AreEqual(0.6f, d.Low, 1e-4f);
            Assert.AreEqual(0.5f, d.High, 1e-4f);
        }
    }
}
