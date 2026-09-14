using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;
using Zombineta.Level;
using Zombineta.Player;

namespace Zombineta.Tests
{
    public class BarrelsAndRamTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 5000f;
            c.startingGap = 40f;
            c.normalSpeed = 10f;
            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 1f;
            c.ammoAtStart = 3;
            c.ammoMax = 6;
            c.hordeBaseSpeed = 10f;
            c.rubberBandMaxBonus = 0f;
            c.hordeCount = 8;
            c.hordeDepthMeters = 12f;
            c.corpseSeconds = 1f;
            c.hordeSeed = 3;
            c.shotRangeMeters = 60f;
            c.explosionRadius = 8f;
            c.explosionScareRadius = 16f;
            c.explosionScareSeconds = 1f;
            c.crashStunDuration = 0.8f;
            return c;
        }

        static ZombieRoster RosterWithHeavy()
        {
            var r = ScriptableObject.CreateInstance<ZombieRoster>();
            r.types.Add(new ZombieType { name = "Comun", deathChance = 1f, ramStun = 0.1f, ramFuelPenalty = 0f, spawnWeight = 1f });
            r.types.Add(new ZombieType { name = "Pesado", deathChance = 0f, ramStun = 0.8f, ramFuelPenalty = 8f, spawnWeight = 1f });
            return r;
        }

        static LevelDefinition MakeLevel(params LevelEntry[] entries)
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.entries.AddRange(entries);
            return def;
        }

        static RunSimulation Make(LevelDefinition def, out LevelRuntime level)
        {
            var cfg = MakeConfig();
            cfg.zombies = RosterWithHeavy();
            var sim = new RunSimulation(cfg);
            level = new LevelRuntime(def);
            sim.Barrels = level;
            return sim;
        }

        /// <summary>Pone los zombies donde uno quiera y mata a los demas.</summary>
        static ZombieUnit Place(RunSimulation sim, int index, float x, int lane)
        {
            var u = sim.Horde.Units[index];
            u.Alive = true; u.X = x; u.Lane = lane; u.Type = 0; u.Stagger = 0f; u.CorpseLeft = 0f;
            return u;
        }

        static void ClearHorde(RunSimulation sim)
        {
            foreach (var u in sim.Horde.Units) u.Alive = false;
        }

        static RunEvent Advance(RunSimulation sim, LevelRuntime level, float seconds, PlayerIntent intent)
        {
            var all = RunEvent.None;
            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps; i++)
            {
                float before = sim.State.PlayerX;
                all |= sim.Tick(intent, Dt);
                if (!Mathf.Approximately(before, sim.State.PlayerX))
                    all |= level.Collect(sim, before, sim.State.PlayerX);
            }
            return all;
        }

        static PlayerIntent Fire() => new PlayerIntent { Mode = DriveMode.Normal, Fire = true };

        [Test]
        public void ShootingABarrel_ExplodesItAndKillsAround()
        {
            var sim = Make(MakeLevel(new LevelEntry(-20f, 1, LevelEntryKind.Barrel)), out var level);
            ClearHorde(sim);
            var close = Place(sim, 0, -22f, 1);
            var far = Place(sim, 1, -60f, 1);

            var events = sim.Tick(Fire(), Dt);

            Assert.AreNotEqual(RunEvent.None, events & RunEvent.Explosion);
            Assert.IsFalse(close.Alive, "estaba a 2 m del barril");
            Assert.IsTrue(far.Alive, "estaba a 40 m");
            Assert.IsTrue(level.Items[0].Consumed, "el barril se gasto");
        }

        [Test]
        public void AnExplosion_KillsInEveryLane()
        {
            var sim = Make(MakeLevel(new LevelEntry(-20f, 1, LevelEntryKind.Barrel)), out _);
            ClearHorde(sim);
            var a = Place(sim, 0, -21f, 0);
            var b = Place(sim, 1, -22f, 2);

            sim.Tick(Fire(), Dt);

            Assert.IsFalse(a.Alive);
            Assert.IsFalse(b.Alive, "la explosion no respeta carriles");
        }

        [Test]
        public void AnExplosion_ScaresBeyondItsKillRadius()
        {
            var sim = Make(MakeLevel(new LevelEntry(-20f, 1, LevelEntryKind.Barrel)), out _);
            ClearHorde(sim);
            var outside = Place(sim, 0, -32f, 0);   // a 12 m: fuera del radio de muerte (8), dentro del susto (16)

            sim.Tick(Fire(), Dt);

            Assert.IsTrue(outside.Alive);
            Assert.Greater(outside.Stagger, 0f);
        }

        [Test]
        public void AnExplosion_ChainsNearbyBarrels()
        {
            var sim = Make(MakeLevel(
                new LevelEntry(-20f, 1, LevelEntryKind.Barrel),
                new LevelEntry(-26f, 0, LevelEntryKind.Barrel)), out var level);
            ClearHorde(sim);
            var farFromFirst = Place(sim, 0, -31f, 2);   // a 11 m del primero, a 5 del segundo

            sim.Tick(Fire(), Dt);

            Assert.IsTrue(level.Items[0].Consumed);
            Assert.IsTrue(level.Items[1].Consumed, "el segundo barril explota por cadena");
            Assert.IsFalse(farFromFirst.Alive, "lo mata la explosion encadenada");
        }

        [Test]
        public void DrivingPastABarrel_DoesNothing()
        {
            var sim = Make(MakeLevel(new LevelEntry(20f, 1, LevelEntryKind.Barrel)), out var level);

            var events = Advance(sim, level, 3f, new PlayerIntent { Mode = DriveMode.Normal });

            Assert.AreEqual(RunEvent.None, events & RunEvent.Explosion);
            Assert.IsFalse(level.Items[0].Consumed, "el barril sigue ahi para dispararle despues");
        }

        [Test]
        public void RunningOverAHeavy_CostsItsStunAndFuel()
        {
            var sim = Make(MakeLevel(new LevelEntry(20f, 1, LevelEntryKind.ZombieFront, 0f, 1)), out var level);

            var events = Advance(sim, level, 2.1f, new PlayerIntent { Mode = DriveMode.Normal });

            Assert.AreNotEqual(RunEvent.None, events & RunEvent.RanOver);
            Assert.AreEqual(0.8f, sim.State.StunRemaining, 0.1f);
            Assert.Less(sim.State.Fuel, 100f - 7.9f, "el pesado cuesta 8 de nafta");
            Assert.IsTrue(level.Items[0].Consumed);
        }

        [Test]
        public void RunningOverACommon_BarelySlowsYouDown()
        {
            var sim = Make(MakeLevel(new LevelEntry(20f, 1, LevelEntryKind.ZombieFront, 0f, 0)), out var level);
            float fuelStart = sim.State.Fuel;

            Advance(sim, level, 2.1f, new PlayerIntent { Mode = DriveMode.Normal });

            Assert.Greater(sim.State.Fuel, fuelStart - 5f, "el comun no cuesta nafta");
        }

        [Test]
        public void AFrontZombie_IsFlownOverWhileAirborne()
        {
            var sim = Make(MakeLevel(
                new LevelEntry(20f, 1, LevelEntryKind.Ramp),
                new LevelEntry(24f, 1, LevelEntryKind.ZombieFront, 0f, 1)), out var level);

            var events = Advance(sim, level, 3f, new PlayerIntent { Mode = DriveMode.Normal });

            Assert.AreEqual(RunEvent.None, events & RunEvent.RanOver, "paso volando por encima");
            Assert.IsFalse(level.Items[1].Consumed);
        }
    }
}
