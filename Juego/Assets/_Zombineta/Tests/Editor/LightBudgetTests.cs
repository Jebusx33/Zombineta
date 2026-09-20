using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    public class LightBudgetTests
    {
        static List<bool> Choose(int budget, float cameraX, params LightCandidate[] candidates)
        {
            var result = new List<bool>();
            LightBudget.Choose(candidates, cameraX, budget, result);
            return result;
        }

        [Test]
        public void WithRoomForEveryone_TheyAllStayOn()
        {
            var r = Choose(5, 0f, new LightCandidate(1f, 0), new LightCandidate(-3f, 0));
            CollectionAssert.AreEqual(new[] { true, true }, r);
        }

        [Test]
        public void TheClosestOnesWin()
        {
            var r = Choose(2, 10f, new LightCandidate(40f, 0), new LightCandidate(11f, 0), new LightCandidate(9f, 0));
            CollectionAssert.AreEqual(new[] { false, true, true }, r);
        }

        [Test]
        public void PriorityBeatsDistance()
        {
            var r = Choose(1, 0f, new LightCandidate(50f, 3), new LightCandidate(1f, 0));
            CollectionAssert.AreEqual(new[] { true, false }, r);
        }

        [Test]
        public void ABudgetOfZero_TurnsEverythingOff()
        {
            var r = Choose(0, 0f, new LightCandidate(1f, 9), new LightCandidate(2f, 0));
            CollectionAssert.AreEqual(new[] { false, false }, r);
        }

        [Test]
        public void TheSameInput_GivesTheSameAnswer()
        {
            var a = Choose(2, 5f, new LightCandidate(4f, 0), new LightCandidate(6f, 0), new LightCandidate(5f, 0));
            var b = Choose(2, 5f, new LightCandidate(4f, 0), new LightCandidate(6f, 0), new LightCandidate(5f, 0));
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void ItReusesTheResultList()
        {
            var result = new List<bool> { true, true, true, true };
            LightBudget.Choose(new[] { new LightCandidate(0f, 0) }, 0f, 1, result);
            Assert.AreEqual(1, result.Count);
        }
    }
}
