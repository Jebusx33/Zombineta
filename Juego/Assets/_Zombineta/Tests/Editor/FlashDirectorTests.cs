using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    public class FlashDirectorTests
    {
        FlashConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<FlashConfig>();
            config.Set(FlashKind.Shot, Color.white, 1.5f, 2f, 0.06f);
            config.Set(FlashKind.Explosion, new Color(1f, 0.6f, 0.2f), 3f, 6f, 0.3f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void AFlash_StartsFull_AndFadesOut()
        {
            var d = new FlashDirector(config);
            d.Trigger(FlashKind.Explosion, new Vector2(10f, 1f));
            d.Tick(0f);
            Assert.AreEqual(1, d.Count);
            Assert.IsTrue(d.TryGet(0, out var at, out _, out float intensity, out float radius));
            Assert.AreEqual(new Vector2(10f, 1f), at);
            Assert.AreEqual(3f, intensity, 1e-4f);
            Assert.AreEqual(6f, radius, 1e-4f);

            d.Tick(0.15f);
            Assert.IsTrue(d.TryGet(0, out _, out _, out float half, out _));
            Assert.AreEqual(1.5f, half, 1e-3f);

            d.Tick(0.2f);
            Assert.AreEqual(0, d.Count);
        }

        [Test]
        public void SeveralFlashesLiveAtOnce()
        {
            var d = new FlashDirector(config);
            d.Trigger(FlashKind.Shot, Vector2.zero);
            d.Trigger(FlashKind.Explosion, new Vector2(5f, 0f));
            d.Tick(0f);
            Assert.AreEqual(2, d.Count);
        }

        [Test]
        public void RunEventsBecomeFlashes()
        {
            var d = new FlashDirector(config);
            d.Trigger(RunEvent.Explosion | RunEvent.PickedUp, new Vector2(3f, 0f));
            d.Tick(0f);
            Assert.AreEqual(1, d.Count);
        }

        [Test]
        public void AKindWithoutConfig_IsIgnored()
        {
            var d = new FlashDirector(config);
            d.Trigger(FlashKind.Crash, Vector2.zero);
            d.Tick(0f);
            Assert.AreEqual(0, d.Count);
        }

        [Test]
        public void OverCapacity_TheOldestIsDropped()
        {
            var d = new FlashDirector(config, capacity: 2);
            d.Trigger(FlashKind.Shot, new Vector2(1f, 0f));
            d.Trigger(FlashKind.Shot, new Vector2(2f, 0f));
            d.Trigger(FlashKind.Shot, new Vector2(3f, 0f));
            d.Tick(0f);
            Assert.AreEqual(2, d.Count);
            Assert.IsTrue(d.TryGet(0, out var first, out _, out _, out _));
            Assert.AreNotEqual(1f, first.x);
        }
    }
}
