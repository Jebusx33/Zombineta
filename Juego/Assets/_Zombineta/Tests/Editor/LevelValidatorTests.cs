using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Juego.Levels;
using Zombineta.Level;

namespace Zombineta.Juego.Tests
{
    public class LevelValidatorTests
    {
        static LevelEntry Obstacle(float d, int lane) => new LevelEntry(d, lane, LevelEntryKind.Obstacle);

        [Test]
        public void AGeneratedLevel_HasNoProblems()
        {
            var entries = LevelGenerator.Generate(new LevelGeneratorSettings { seed = 5 }, 4000f);
            CollectionAssert.IsEmpty(LevelValidator.Check(entries));
        }

        [Test]
        public void ThreeLanesBlocked_IsReportedOnce()
        {
            var entries = new List<LevelEntry> { Obstacle(100f, 0), Obstacle(101f, 1), Obstacle(102f, 2) };

            var issues = LevelValidator.Check(entries);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(LevelIssueKind.AllLanesBlocked, issues[0].kind);
            Assert.AreEqual(-1, issues[0].lane);
            Assert.AreEqual(100f, issues[0].distance, 0.01f);
        }

        [Test]
        public void ThreeLanesFarApart_AreFine()
        {
            var entries = new List<LevelEntry> { Obstacle(100f, 0), Obstacle(103f, 1), Obstacle(106f, 2) };
            CollectionAssert.IsEmpty(LevelValidator.Check(entries));
        }

        [Test]
        public void AnAerialPickup_DoesNotBlockALane()
        {
            var entries = new List<LevelEntry>
            {
                Obstacle(100f, 0), Obstacle(100f, 1), new LevelEntry(100f, 2, LevelEntryKind.Obstacle, 6f),
            };
            CollectionAssert.IsEmpty(LevelValidator.Check(entries));
        }

        [Test]
        public void SomethingInARampLanding_IsReported()
        {
            var entries = new List<LevelEntry>
            {
                new LevelEntry(200f, 1, LevelEntryKind.Ramp),
                Obstacle(204f, 1), Obstacle(208f, 1),     // se sobrevuelan: parte de la pieza
                Obstacle(230f, 1),                         // en el aterrizaje
                Obstacle(230f, 0),                         // otro carril: no importa
            };

            var issues = LevelValidator.Check(entries);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(LevelIssueKind.BlockedLanding, issues[0].kind);
            Assert.AreEqual(230f, issues[0].distance, 0.01f);
            Assert.AreEqual(1, issues[0].lane);
        }

        [Test]
        public void TwoItemsOnTopOfEachOther_AreReported()
        {
            var entries = new List<LevelEntry>
            {
                new LevelEntry(300f, 2, LevelEntryKind.Fuel),
                new LevelEntry(300.5f, 2, LevelEntryKind.Battery),
                new LevelEntry(300f, 2, LevelEntryKind.Ammo, 6f),   // arriba: no se pisan
            };

            var issues = LevelValidator.Check(entries);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(LevelIssueKind.Overlapping, issues[0].kind);
            Assert.AreEqual(2, issues[0].lane);
        }

        [Test]
        public void EveryIssue_HasAMessage()
        {
            var entries = new List<LevelEntry> { Obstacle(100f, 0), Obstacle(100f, 1), Obstacle(100f, 2), Obstacle(100.2f, 2) };
            foreach (var issue in LevelValidator.Check(entries))
                Assert.IsFalse(string.IsNullOrEmpty(issue.Message));
        }
    }
}
