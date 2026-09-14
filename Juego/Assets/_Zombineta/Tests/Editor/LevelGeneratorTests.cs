using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zombineta.Juego.Levels;
using Zombineta.Level;

namespace Zombineta.Juego.Tests
{
    public class LevelGeneratorTests
    {
        const float Length = 4000f;

        static LevelGeneratorSettings Settings(int seed = 7) => new LevelGeneratorSettings { seed = seed };

        static bool Blocker(LevelEntry e) =>
            e.height <= 0f && (e.kind == LevelEntryKind.Obstacle || e.kind == LevelEntryKind.ZombieFront);

        [Test]
        public void TheSameSeed_BuildsTheSameLevel()
        {
            var a = LevelGenerator.Generate(Settings(3), Length);
            var b = LevelGenerator.Generate(Settings(3), Length);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].distance, b[i].distance);
                Assert.AreEqual(a[i].lane, b[i].lane);
                Assert.AreEqual(a[i].kind, b[i].kind);
            }
        }

        [Test]
        public void ADifferentSeed_BuildsADifferentLevel()
        {
            var a = LevelGenerator.Generate(Settings(3), Length);
            var b = LevelGenerator.Generate(Settings(4), Length);

            bool differs = a.Count != b.Count;
            for (int i = 0; !differs && i < a.Count; i++)
                differs = a[i].lane != b[i].lane || a[i].distance != b[i].distance || a[i].kind != b[i].kind;
            Assert.IsTrue(differs);
        }

        [Test]
        public void ItHasSomethingOfEveryKind()
        {
            var entries = LevelGenerator.Generate(Settings(), Length);
            foreach (LevelEntryKind kind in System.Enum.GetValues(typeof(LevelEntryKind)))
                Assert.IsTrue(entries.Exists(e => e.kind == kind), "falta " + kind);
        }

        [Test]
        public void EverythingIsInsideTheLevel()
        {
            var s = Settings();
            foreach (var e in LevelGenerator.Generate(s, Length))
            {
                Assert.GreaterOrEqual(e.distance, s.startClear);
                Assert.LessOrEqual(e.distance, Length - s.endClear + 17f, e.kind + " fuera del nivel");
            }
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(99)]
        public void ThreeLanesAreNeverBlockedAtOnce(int seed)
        {
            var entries = LevelGenerator.Generate(Settings(seed), Length);
            foreach (var b in entries)
            {
                if (!Blocker(b)) continue;
                var lanes = new HashSet<int>();
                foreach (var e in entries)
                    if (Blocker(e) && Mathf.Abs(e.distance - b.distance) <= 2f)
                        lanes.Add(e.lane);
                Assert.Less(lanes.Count, 3, "tres carriles tapados cerca de " + b.distance + " m");
            }
        }

        [Test]
        public void EveryRampHasAClearLanding()
        {
            var entries = LevelGenerator.Generate(Settings(), Length);
            int ramps = 0;
            foreach (var r in entries)
            {
                if (r.kind != LevelEntryKind.Ramp) continue;
                ramps++;
                foreach (var e in entries)
                    if (Blocker(e) && e.lane == r.lane && e.distance > r.distance + 8.5f && e.distance <= r.distance + 50f)
                        Assert.Fail("algo en el aterrizaje de la rampa de " + r.distance + " m: " + e.kind + " a " + e.distance);
            }
            Assert.Greater(ramps, 5);
        }

        [Test]
        public void BarrelsStayOutOfRampPieces()
        {
            var entries = LevelGenerator.Generate(Settings(), Length);
            foreach (var barrel in entries)
            {
                if (barrel.kind != LevelEntryKind.Barrel) continue;
                foreach (var r in entries)
                    if (r.kind == LevelEntryKind.Ramp)
                        Assert.IsFalse(barrel.distance >= r.distance - 5f && barrel.distance <= r.distance + 50f,
                            "barril a " + barrel.distance + " m dentro de la rampa de " + r.distance);
            }
        }

        [Test]
        public void GroundThingsInALaneKeepTheirDistance()
        {
            var s = Settings();
            var entries = LevelGenerator.Generate(s, Length);
            for (int lane = 0; lane < 3; lane++)
            {
                var inLane = entries.FindAll(e => e.lane == lane && e.height <= 0f);
                inLane.Sort((a, b) => a.distance.CompareTo(b.distance));
                for (int i = 1; i < inLane.Count; i++)
                    Assert.GreaterOrEqual(inLane[i].distance - inLane[i - 1].distance, s.minGapSameLane - 0.001f,
                        "carril " + lane + " a " + inLane[i].distance + " m");
            }
        }

        [Test]
        public void NothingIsPlacedOnTopOfAPinnedItem()
        {
            var s = Settings();
            var pinned = new List<LevelEntry>();
            for (float d = 300f; d < 3800f; d += 97f)
                pinned.Add(new LevelEntry(d, 1, LevelEntryKind.Obstacle));

            var entries = LevelGenerator.Generate(s, Length, pinned);

            foreach (var e in entries)
                foreach (var p in pinned)
                    if (e.lane == p.lane && e.height <= 0f)
                        Assert.GreaterOrEqual(Mathf.Abs(e.distance - p.distance), s.minGapSameLane,
                            e.kind + " a " + e.distance + " m pisa el fijado de " + p.distance);
            CollectionAssert.IsNotEmpty(entries);
            Assert.IsFalse(entries.Exists(e => pinned.Contains(e)), "devuelve solo lo nuevo");
        }

        [Test]
        public void PinnedObstacles_CountForTheThreeLaneRule()
        {
            // Dos carriles tapados a mano cada 50 m: el generador no puede tapar el tercero ahi.
            var pinned = new List<LevelEntry>();
            for (float d = 200f; d < 3800f; d += 50f)
            {
                pinned.Add(new LevelEntry(d, 0, LevelEntryKind.Obstacle));
                pinned.Add(new LevelEntry(d, 2, LevelEntryKind.Obstacle));
            }

            var entries = LevelGenerator.Generate(Settings(), Length, pinned);

            foreach (var e in entries)
                if (Blocker(e) && e.lane == 1)
                    foreach (var p in pinned)
                        Assert.Greater(Mathf.Abs(e.distance - p.distance), 2f,
                            "tapo el carril libre a " + e.distance + " m");
        }
    }
}
