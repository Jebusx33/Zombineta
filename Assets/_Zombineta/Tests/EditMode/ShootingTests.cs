using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;
using Zombineta.Player;

namespace Zombineta.Tests
{
    public class ShootingTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 5000f;
            c.startingGap = 30f;
            c.normalSpeed = 10f;
            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 1f;
            c.batteryMax = 100f;
            c.ammoMax = 6;
            c.ammoAtStart = 3;
            c.hordeBaseSpeed = 10f;
            c.rubberBandMaxBonus = 0f;
            c.hordeCount = 8;
            c.hordeDepthMeters = 12f;
            c.corpseSeconds = 1f;
            c.hordeSeed = 11;
            c.shotRangeMeters = 60f;
            c.deathScareRadius = 4f;
            c.deathScareSeconds = 0.5f;
            return c;
        }

        static ZombieRoster Roster(float deathChance, float stagger = 0.6f)
        {
            var r = ScriptableObject.CreateInstance<ZombieRoster>();
            r.types.Add(new ZombieType
            {
                name = "Test", speedMultiplier = 1f, deathChance = deathChance,
                staggerOnHit = stagger, spawnWeight = 1f,
            });
            return r;
        }

        static RunSimulation Make(float deathChance = 1f)
        {
            var cfg = MakeConfig();
            cfg.zombies = Roster(deathChance);
            return new RunSimulation(cfg);
        }

        /// <summary>Deja vivo un solo zombie, en la posicion y el carril pedidos.</summary>
        static ZombieUnit Solo(RunSimulation sim, float x, int lane)
        {
            var h = sim.Horde;
            for (int i = 1; i < h.Units.Length; i++)
                h.Units[i].Alive = false;
            var u = h.Units[0];
            u.Alive = true; u.X = x; u.Lane = lane; u.Stagger = 0f; u.CorpseLeft = 0f; u.Type = 0;
            h.RecomputeFront();
            sim.State.HordeX = h.FrontX;
            return u;
        }

        static PlayerIntent Fire() => new PlayerIntent { Mode = DriveMode.Normal, Fire = true };

        static bool Has(RunEvent events, RunEvent flag) => (events & flag) != 0;

        [Test]
        public void Shot_HitsTheNearestZombieInTheLane()
        {
            var sim = Make();
            var far = Solo(sim, -30f, 1);
            var near = sim.Horde.Units[1];
            near.Alive = true; near.X = -10f; near.Lane = 1; near.Type = 0;

            sim.Tick(Fire(), Dt);

            Assert.IsFalse(near.Alive, "el mas cercano se lleva la bala");
            Assert.IsTrue(far.Alive);
        }

        [Test]
        public void Shot_IgnoresZombiesInOtherLanes()
        {
            var sim = Make();
            var other = Solo(sim, -10f, 0);   // la jugadora arranca en el carril 1

            var events = sim.Tick(Fire(), Dt);

            Assert.IsTrue(other.Alive, "no esta en tu carril");
            Assert.IsTrue(Has(events, RunEvent.ShotMissed));
        }

        [Test]
        public void Shot_WithNobodyInTheLane_MissesAndStillSpendsAmmo()
        {
            var sim = Make();
            foreach (var u in sim.Horde.Units) u.Alive = false;

            var events = sim.Tick(Fire(), Dt);

            Assert.IsTrue(Has(events, RunEvent.Shot));
            Assert.IsTrue(Has(events, RunEvent.ShotMissed));
            Assert.AreEqual(2, sim.State.Ammo, "la bala se gasta igual");
        }

        [Test]
        public void DeathChanceOne_AlwaysKills()
        {
            var sim = Make(deathChance: 1f);
            var u = Solo(sim, -10f, 1);

            sim.Tick(Fire(), Dt);

            Assert.IsFalse(u.Alive);
        }

        [Test]
        public void DeathChanceZero_NeverKills_ButStaggers()
        {
            var sim = Make(deathChance: 0f);
            var u = Solo(sim, -10f, 1);

            sim.Tick(Fire(), Dt);

            Assert.IsTrue(u.Alive, "encajo el impacto y sigue");
            Assert.Greater(u.Stagger, 0f, "pero queda frenado");
        }

        [Test]
        public void KillingTheLeader_OpensTheGap()
        {
            var sim = Make();
            Solo(sim, -10f, 1);
            var second = sim.Horde.Units[1];
            second.Alive = true; second.X = -25f; second.Lane = 1; second.Type = 0;
            float gapBefore = sim.State.Gap;

            sim.Tick(Fire(), Dt);

            Assert.Greater(sim.State.Gap, gapBefore + 10f, "el frente pasa a ser el de atras");
        }

        [Test]
        public void ADeath_ScaresTheNeighbours()
        {
            var sim = Make();
            Solo(sim, -10f, 1);
            var neighbour = sim.Horde.Units[1];
            neighbour.Alive = true; neighbour.X = -12f; neighbour.Lane = 2; neighbour.Type = 0;

            sim.Tick(Fire(), Dt);

            Assert.Greater(neighbour.Stagger, 0f, "a 2 m de la muerte, se frena");
        }

        [Test]
        public void Shot_EmitsTracerAndDeathEventsWithTheirPositions()
        {
            var sim = Make();
            var u = Solo(sim, -10f, 1);
            float playerX = sim.State.PlayerX;

            sim.Tick(Fire(), Dt);

            bool tracer = false, death = false;
            foreach (var e in sim.Horde.Events)
            {
                if (e.Kind == HordeEventKind.Tracer)
                {
                    tracer = true;
                    Assert.AreEqual(playerX, e.FromX, 0.5f);
                    Assert.AreEqual(-10f, e.X, 0.5f);
                }
                if (e.Kind == HordeEventKind.Death)
                {
                    death = true;
                    Assert.AreEqual(DeathCause.Bullet, e.Cause);
                    Assert.AreEqual(1, e.Lane);
                }
            }
            Assert.IsTrue(tracer, "falta la traza");
            Assert.IsTrue(death, "falta la muerte");
        }
    }
}
