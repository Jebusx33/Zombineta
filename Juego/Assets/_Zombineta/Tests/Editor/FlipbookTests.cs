using NUnit.Framework;
using Zombineta.Enemies;

namespace Zombineta.Juego.Tests
{
    public class FlipbookTests
    {
        [Test]
        public void WalkLoops_FasterWithSpeed()
        {
            var slow = new Flipbook(8, 1, 4, 8f);
            var fast = new Flipbook(8, 1, 4, 8f);
            for (int i = 0; i < 10; i++) { slow.Tick(0.05f, 1f); fast.Tick(0.05f, 2f); }
            Assert.AreEqual(4, slow.Frame);   // 0,5 s × 8 fps
            Assert.AreEqual(0, fast.Frame);   // 8 cuadros: dio la vuelta justo
            Assert.AreEqual(FlipbookClip.Walk, fast.Clip);
        }

        [Test]
        public void Hit_ReturnsToWalk()
        {
            var f = new Flipbook(8, 2, 4, 8f, 0.2f);
            f.Play(FlipbookClip.Hit);
            f.Tick(0.1f, 1f);
            Assert.AreEqual(FlipbookClip.Hit, f.Clip);
            f.Tick(0.15f, 1f);
            Assert.AreEqual(FlipbookClip.Walk, f.Clip);
        }

        [Test]
        public void Death_StopsOnTheLastFrame()
        {
            var f = new Flipbook(8, 1, 4, 8f, 0.2f, 10f);
            f.Play(FlipbookClip.Death);
            for (int i = 0; i < 20; i++) f.Tick(0.1f, 1f);
            Assert.AreEqual(FlipbookClip.Death, f.Clip);
            Assert.AreEqual(3, f.Frame);
            Assert.IsTrue(f.Finished);
        }

        [Test]
        public void ClipsWithoutFrames_NeverGoOutOfRange()
        {
            var f = new Flipbook(2, 0, 0, 8f);
            f.Play(FlipbookClip.Hit);
            f.Tick(0.05f, 1f);
            Assert.AreEqual(0, f.Frame);
            f.Play(FlipbookClip.Death);
            f.Tick(1f, 1f);
            Assert.AreEqual(0, f.Frame);
        }
    }
}
