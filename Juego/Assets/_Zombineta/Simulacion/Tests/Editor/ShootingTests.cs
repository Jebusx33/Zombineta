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
            c.retrocesoAciertos = 0;   // el retroceso de la horda tiene sus propios tests
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
    
        // --- Playtest de Axo, fase C: dos aciertos despejan la horda --------------------

        /// <summary>Dos blancos en el carril de la jugadora y el resto de la horda atras, en otro carril.</summary>
        static RunSimulation MakeRetreat(bool enabled)
        {
            var cfg = MakeConfig();
            cfg.zombies = Roster(1f);
            cfg.ammoMax = 20;
            cfg.ammoAtStart = 20;
            cfg.retrocesoAciertos = enabled ? 2 : 0;
            cfg.retrocesoVentana = 3f;
            cfg.retrocesoMetros = 12f;
            cfg.retrocesoSegundos = 3f;
            cfg.retrocesoEnfriamiento = 8f;
            var sim = new RunSimulation(cfg);
            var us = sim.Horde.Units;
            for (int i = 0; i < us.Length; i++)
            {
                us[i].Alive = true; us[i].Stagger = 0f; us[i].CorpseLeft = 0f; us[i].Type = 0;
                us[i].Lane = i < 4 ? 1 : 0;
                us[i].X = -8f - 2f * i;
            }
            sim.Horde.RecomputeFront();
            sim.State.HordeX = sim.Horde.FrontX;
            return sim;
        }

        static RunEvent Disparar(RunSimulation sim, int cuantos, float entreDisparos)
        {
            var ev = RunEvent.None;
            for (int i = 0; i < cuantos; i++)
            {
                ev |= sim.Tick(Fire(), Dt);
                for (int t = 0; t < Mathf.RoundToInt(entreDisparos / Dt); t++)
                    ev |= sim.Tick(PlayerIntent.Idle, Dt);
            }
            return ev;
        }

        static void Pasar(RunSimulation sim, float segundos)
        {
            for (int t = 0; t < Mathf.RoundToInt(segundos / Dt); t++)
                sim.Tick(PlayerIntent.Idle, Dt);
        }

        [Test]
        public void TwoHitsInTheWindow_MakeTheHordeFallBack()
        {
            var sim = MakeRetreat(true);
            var ev = Disparar(sim, 2, 0.2f);

            Assert.IsTrue(Has(ev, RunEvent.HordaRetrocede), "el segundo acierto la hace retroceder");
            Assert.Greater(sim.State.RetrocesoRestante, 0f);
        }

        [Test]
        public void TheRetreat_GainsAboutTheConfiguredMeters_OverItsDuration()
        {
            var con = MakeRetreat(true);
            var sin = MakeRetreat(false);
            Disparar(con, 2, 0.2f);
            Disparar(sin, 2, 0.2f);
            Pasar(con, 4f);
            Pasar(sin, 4f);

            // Una de las que quedaron atras y no recibio tiros.
            float diferencia = sin.Horde.Units[7].X - con.Horde.Units[7].X;
            Assert.AreEqual(12f, diferencia, 0.5f);
        }

        [Test]
        public void TheHorde_GoesBackToItsPaceAfterTheRetreat()
        {
            var sim = MakeRetreat(true);
            Disparar(sim, 2, 0.2f);
            Pasar(sim, 3.5f);
            Assert.AreEqual(0f, sim.State.RetrocesoRestante, 1e-4f);

            float antes = sim.Horde.Units[7].X;
            Pasar(sim, 1f);
            Assert.Greater(sim.Horde.Units[7].X, antes, "vuelve a avanzar");
        }

        [Test]
        public void OneHit_DoesNotMakeTheHordeFallBack()
        {
            var sim = MakeRetreat(true);
            var ev = Disparar(sim, 1, 0.2f);
            Assert.IsFalse(Has(ev, RunEvent.HordaRetrocede));
        }

        [Test]
        public void TwoHitsTooFarApart_DoNotCount()
        {
            var sim = MakeRetreat(true);
            var ev = Disparar(sim, 2, 3.5f);   // mas que la ventana de 3 s
            Assert.IsFalse(Has(ev, RunEvent.HordaRetrocede));
        }

        [Test]
        public void MissedShots_DoNotCountAsHits()
        {
            var sim = MakeRetreat(true);
            foreach (var u in sim.Horde.Units) u.Lane = 0;   // nadie en el carril de la jugadora
            var ev = Disparar(sim, 3, 0.2f);
            Assert.IsFalse(Has(ev, RunEvent.HordaRetrocede));
        }

        [Test]
        public void TheRetreat_HasACooldown_ButWorksAgainAfterIt()
        {
            var sim = MakeRetreat(true);
            Disparar(sim, 2, 0.2f);                       // retrocede
            Pasar(sim, 3.5f);

            foreach (var u in sim.Horde.Units) { u.Alive = true; u.Lane = 1; u.X = sim.State.PlayerX - 8f; u.CorpseLeft = 0f; }
            var ev = Disparar(sim, 2, 0.2f);              // dentro del enfriamiento (8 s)
            Assert.IsFalse(Has(ev, RunEvent.HordaRetrocede), "todavia en enfriamiento");

            Pasar(sim, 9f);
            foreach (var u in sim.Horde.Units) { u.Alive = true; u.Lane = 1; u.X = sim.State.PlayerX - 8f; u.Stagger = 0f; u.CorpseLeft = 0f; }
            ev = Disparar(sim, 2, 0.2f);
            Assert.IsTrue(Has(ev, RunEvent.HordaRetrocede), "pasado el enfriamiento vuelve a funcionar");
        }

        [Test]
        public void Reset_ClearsTheRetreat()
        {
            var sim = MakeRetreat(true);
            Disparar(sim, 2, 0.2f);
            sim.Reset();
            Assert.AreEqual(0f, sim.State.RetrocesoRestante);
            Assert.AreEqual(0f, sim.State.RetrocesoEnfriamiento);
            Assert.AreEqual(0f, sim.Horde.RetrocesoVelocidad);
        }
}
}
