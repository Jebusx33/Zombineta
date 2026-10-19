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
                    if (Blocker(e) && e.lane == r.lane && e.distance > r.distance + LevelValidator.LandingFrom && e.distance <= r.distance + 50f)
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
        public void ObstaclesInALane_DoNotOverlapEachOther()
        {
            // Un coche mide casi 4 m de ancho: con 2 m de distancia se encimaban.
            var entries = LevelGenerator.Generate(Settings(), Length);
            float min = 2f * LevelFootprint.Obstacle + LevelFootprint.Margin;
            for (int lane = 0; lane < 3; lane++)
            {
                var obstacles = entries.FindAll(e => e.lane == lane && e.kind == LevelEntryKind.Obstacle);
                obstacles.Sort((a, b) => a.distance.CompareTo(b.distance));
                for (int i = 1; i < obstacles.Count; i++)
                    Assert.GreaterOrEqual(obstacles[i].distance - obstacles[i - 1].distance, min - 0.001f,
                        "obstaculos encimados en el carril " + lane + " a " + obstacles[i].distance + " m");
            }
        }

        [TestCase(7)]
        [TestCase(21)]
        [TestCase(2026)]
        public void NothingGroundLevelIsHiddenBehindAnObstacle(int seed)
        {
            var entries = LevelGenerator.Generate(Settings(seed), Length);
            foreach (var hider in entries)
                foreach (var e in entries)
                    Assert.IsFalse(LevelFootprint.Hides(hider, e),
                        e.kind + " a " + e.distance + " m (carril " + e.lane + ") queda detras del obstaculo de " + hider.distance + " m");
        }

        [Test]
        public void ARampPiece_HasASingleObstacle()
        {
            var entries = LevelGenerator.Generate(Settings(), Length);
            int ramps = 0;
            foreach (var r in entries)
            {
                if (r.kind != LevelEntryKind.Ramp) continue;
                ramps++;
                int obstacles = entries.FindAll(e => e.kind == LevelEntryKind.Obstacle && e.lane == r.lane &&
                                                     e.distance > r.distance && e.distance <= r.distance + LevelValidator.LandingFrom).Count;
                Assert.AreEqual(1, obstacles, "rampa de " + r.distance + " m");
            }
            Assert.Greater(ramps, 5);
        }

        [Test]
        public void Footprints_AreSymmetricAndRespectTheFloor()
        {
            Assert.AreEqual(LevelFootprint.MinDistance(LevelEntryKind.Obstacle, LevelEntryKind.Fuel, 2f),
                LevelFootprint.MinDistance(LevelEntryKind.Fuel, LevelEntryKind.Obstacle, 2f), 0.0001f);
            Assert.GreaterOrEqual(LevelFootprint.MinDistance(LevelEntryKind.Fuel, LevelEntryKind.Battery, 2f), 2f);
        }

        [Test]
        public void AnObstacle_HidesWhatIsInTheLanesAboveButNotInTheAir()
        {
            var car = new LevelEntry(100f, 0, LevelEntryKind.Obstacle);
            Assert.IsTrue(LevelFootprint.Hides(car, new LevelEntry(100f, 1, LevelEntryKind.Fuel)));
            Assert.IsFalse(LevelFootprint.Hides(car, new LevelEntry(100f, 1, LevelEntryKind.Fuel, 1f)), "en el aire");
            Assert.IsFalse(LevelFootprint.Hides(car, new LevelEntry(100f, 0, LevelEntryKind.Fuel)), "mismo carril");
            Assert.IsTrue(LevelFootprint.Hides(car, new LevelEntry(100f, 2, LevelEntryKind.Fuel)), "el coche tapa dos carriles");
            Assert.IsFalse(LevelFootprint.Hides(new LevelEntry(100f, 2, LevelEntryKind.Obstacle), new LevelEntry(100f, 2, LevelEntryKind.Fuel)), "nada arriba del ultimo carril");
            Assert.IsFalse(LevelFootprint.Hides(car, new LevelEntry(110f, 1, LevelEntryKind.Fuel)), "lejos");
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

        // --- Tramos ------------------------------------------------------------------

        static int Count(List<LevelEntry> entries, LevelEntryKind kind, float from, float to) =>
            entries.FindAll(e => e.kind == kind && e.height <= 0f && e.distance >= from && e.distance < to).Count;

        [Test]
        public void WithoutTramos_TheLevelIsTheSameAsBefore()
        {
            var a = LevelGenerator.Generate(Settings(3), Length);
            var b = LevelGenerator.Generate(Settings(3), Length, null, new List<Tramo>());

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
                Assert.AreEqual(a[i], b[i]);
        }

        [Test]
        public void AnEmptyTramo_HasNothingButWhatItAsksFor()
        {
            var tramos = new List<Tramo>
            {
                new Tramo { hasta = 1000f },
                new Tramo { hasta = 2000f, obstaculos = 0f, piezas = 0f, recursos = 2f },
                new Tramo { hasta = Length },
            };

            var entries = LevelGenerator.Generate(Settings(), Length, null, tramos);

            // Margen de 60 m: una pieza de rampa del tramo anterior puede entrar un poco.
            Assert.AreEqual(0, Count(entries, LevelEntryKind.Obstacle, 1060f, 2000f));
            Assert.AreEqual(0, Count(entries, LevelEntryKind.Ramp, 1000f, 2000f));
            Assert.AreEqual(0, Count(entries, LevelEntryKind.Barrel, 1000f, 2000f));
            Assert.AreEqual(0, Count(entries, LevelEntryKind.ZombieFront, 1000f, 2000f));
            Assert.Greater(Count(entries, LevelEntryKind.Fuel, 1000f, 2000f),
                Count(entries, LevelEntryKind.Fuel, 2000f, 3000f), "el doble de recursos");
        }

        [Test]
        public void ADenserTramo_HasMoreObstacles()
        {
            var tramos = new List<Tramo>
            {
                new Tramo { hasta = 2000f, obstaculos = 0.3f, piezas = 0f },
                new Tramo { hasta = Length, obstaculos = 2f, piezas = 0f },
            };

            var entries = LevelGenerator.Generate(Settings(), Length, null, tramos);

            int calm = Count(entries, LevelEntryKind.Obstacle, 0f, 2000f);
            int dense = Count(entries, LevelEntryKind.Obstacle, 2000f, Length);
            Assert.Greater(dense, calm * 3, calm + " en el tranquilo, " + dense + " en el denso");
        }

        [TestCase(1)] [TestCase(2)] [TestCase(99)]
        public void WithTramos_ThreeLanesAreStillNeverBlocked(int seed)
        {
            var tramos = new List<Tramo>
            {
                new Tramo { hasta = 2000f, obstaculos = 3f, piezas = 2f },
                new Tramo { hasta = Length, obstaculos = 0.5f },
            };

            var entries = LevelGenerator.Generate(Settings(seed), Length, null, tramos);
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
        [TestCase(1)] [TestCase(5)] [TestCase(42)]
        public void LooseObstacles_AreNeverSideBySideInNeighbourLanes(int seed)
        {
            var s = Settings(seed);
            var entries = LevelGenerator.Generate(s, Length);
            var sueltos = entries.FindAll(e => e.kind == LevelEntryKind.Obstacle);
            foreach (var a in sueltos)
                foreach (var b in sueltos)
                    if (Mathf.Abs(a.lane - b.lane) == 1)
                    {
                        // Los de una pieza de rampa van en su carril; los sueltos respetan la distancia.
                        bool deRampa = entries.Exists(r => r.kind == LevelEntryKind.Ramp &&
                            (r.lane == a.lane || r.lane == b.lane) &&
                            (Mathf.Abs(r.distance - a.distance) < 6f || Mathf.Abs(r.distance - b.distance) < 6f));
                        if (!deRampa)
                            Assert.GreaterOrEqual(Mathf.Abs(a.distance - b.distance), s.separacionEntreCarriles,
                                "obstaculos pegados en carriles vecinos a " + a.distance + " y " + b.distance);
                    }
        }

        [Test]
        public void ADroughtTramo_HasNoFuel_ButStillHasTheOtherResources()
        {
            var tramos = new List<Tramo>
            {
                new Tramo { nombre = "Con nafta", hasta = 1000f },
                new Tramo { nombre = "Sequia", hasta = 3000f, nafta = 0f },
                new Tramo { nombre = "Final", hasta = 4000f },
            };

            var entries = LevelGenerator.Generate(Settings(), Length, null, tramos);

            Assert.AreEqual(0, entries.FindAll(e => e.kind == LevelEntryKind.Fuel && e.distance >= 1000f && e.distance < 3000f &&
                                                    e.height <= 0f).Count, "ni un bidon en la sequia");
            Assert.Greater(entries.FindAll(e => e.kind == LevelEntryKind.Fuel && e.distance < 1000f).Count, 0, "antes si hay");
            Assert.Greater(entries.FindAll(e => e.kind == LevelEntryKind.Fuel && e.distance >= 3000f).Count, 0, "despues vuelven");
            Assert.Greater(entries.FindAll(e => e.kind == LevelEntryKind.Ammo && e.distance >= 1000f && e.distance < 3000f).Count, 0,
                "las balas y la bateria siguen");
        }

        [Test]
        public void TheDefaultTramo_HasTheNormalFuel()
        {
            Assert.AreEqual(1f, new Tramo().nafta, 0.0001f);
        }
    }
}
