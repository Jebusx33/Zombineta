using NUnit.Framework;
using Zombineta.Enemies;

namespace Zombineta.Juego.Tests
{
    public class ZombieLookPickerTests
    {
        [Test]
        public void TheSameInput_GivesTheSameLook()
        {
            Assert.AreEqual(ZombieLookPicker.Pick(7, 3, 2, 3, -1), ZombieLookPicker.Pick(7, 3, 2, 3, -1));
        }

        [Test]
        public void TheLookIsAlwaysInRange()
        {
            for (int u = 0; u < 50; u++)
                for (int g = 0; g < 10; g++)
                {
                    int i = ZombieLookPicker.Pick(1, u, g, 3, -1);
                    Assert.GreaterOrEqual(i, 0);
                    Assert.Less(i, 3);
                }
        }

        [Test]
        public void ItAvoidsRepeatingThePreviousLook()
        {
            for (int u = 0; u < 50; u++)
                Assert.AreNotEqual(1, ZombieLookPicker.Pick(1, u, 0, 2, 1));
        }

        [Test]
        public void WithOneLook_ItIsThatOne_AndWithNoneMinusOne()
        {
            Assert.AreEqual(0, ZombieLookPicker.Pick(1, 5, 5, 1, 0));
            Assert.AreEqual(-1, ZombieLookPicker.Pick(1, 5, 5, 0, -1));
        }

        [Test]
        public void AllLooksGetUsed()
        {
            var seen = new bool[3];
            for (int u = 0; u < 60; u++)
                seen[ZombieLookPicker.Pick(4, u, 0, 3, -1)] = true;
            CollectionAssert.AreEqual(new[] { true, true, true }, seen);
        }
    }
}
