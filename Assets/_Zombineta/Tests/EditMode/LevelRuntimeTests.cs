using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;
using Zombineta.Player;

namespace Zombineta.Tests
{
    public class LevelRuntimeTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 1000f;
            c.startingGap = 50f;
            c.laneChangeDuration = 0.1f;
            c.normalSpeed = 10f;
            c.turboSpeedMultiplier = 2f;
            c.reverseSpeedMultiplier = -0.5f;
            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 1f;
            c.turboBurnMultiplier = 3f;
            c.reverseBurnMultiplier = 0.5f;
            c.fuelPickupAmount = 25f;
            c.batteryMax = 100f;
            c.batteryDrainPerSecond = 10f;
            c.batteryPickupAmount = 40f;
            c.ammoMax = 6;
            c.ammoAtStart = 3;
            c.ammoPickupAmount = 2;
            c.hordeBaseSpeed = 11f;
            c.rubberBandMaxBonus = 0f;
            c.crashStunDuration = 1f;
            c.crashFuelPenalty = 10f;
            return c;
        }

        static LevelDefinition MakeLevel(params LevelEntry[] entries)
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.entries.AddRange(entries);
            return def;
        }

        /// <summary>Empuja la simulacion y resuelve el recorrido igual que RunController.</summary>
        static void Advance(RunSimulation sim, LevelRuntime level, float seconds, PlayerIntent intent)
        {
            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps; i++)
            {
                float before = sim.State.PlayerX;
                sim.Tick(intent, Dt);
                float after = sim.State.PlayerX;
                if (!Mathf.Approximately(before, after))
                    level.Collect(sim, before, after);
            }
        }

        static PlayerIntent Driving(DriveMode mode) => new PlayerIntent { Mode = mode };

        [Test]
        public void PickupInThePlayerLane_IsCollected()
        {
            var cfg = MakeConfig();
            var sim = new RunSimulation(cfg);
            // Arranca en el carril 1 con el tanque lleno: primero gastar un poco.
            var level = new LevelRuntime(MakeLevel(new LevelEntry(50f, 1, LevelEntryKind.Fuel)));

            Advance(sim, level, 4f, Driving(DriveMode.Normal)); // 40 m, nafta 96
            float beforePickup = sim.State.Fuel;
            Advance(sim, level, 2f, Driving(DriveMode.Normal)); // cruza los 50 m

            Assert.Greater(sim.State.Fuel, beforePickup, "tendria que haber sumado nafta");
            Assert.IsTrue(level.Items[0].Consumed);
        }

        [Test]
        public void PickupInAnotherLane_IsMissed()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(50f, 0, LevelEntryKind.Fuel)));

            Advance(sim, level, 8f, Driving(DriveMode.Normal)); // pasa los 50 m por el carril 1

            Assert.IsFalse(level.Items[0].Consumed, "estaba en otro carril");
        }

        [Test]
        public void ReversingBackOverAMissedCan_PicksItUp()
        {
            var cfg = MakeConfig();
            // Ventaja grande a proposito: este test es sobre el retroceso, no sobre
            // la persecucion. Con el gap normal la horda te alcanza antes de volver,
            // que es justamente el costo que hace interesante a la maniobra.
            cfg.startingGap = 300f;

            var sim = new RunSimulation(cfg);
            var level = new LevelRuntime(MakeLevel(new LevelEntry(50f, 0, LevelEntryKind.Fuel)));

            // Pasarlo de largo por el carril del medio.
            Advance(sim, level, 8f, Driving(DriveMode.Normal));
            Assert.IsFalse(level.Items[0].Consumed);

            // Bajar al carril 0 y volver a buscarlo: ese es el sentido del retroceso.
            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = -1 }, Dt);
            Advance(sim, level, 0.2f, Driving(DriveMode.Normal)); // completar el tween
            float beforePickup = sim.State.Fuel;
            Advance(sim, level, 8f, Driving(DriveMode.Reverse));

            Assert.AreEqual(RunPhase.Running, sim.State.Phase, "no tendria que haber perdido");
            Assert.IsTrue(level.Items[0].Consumed, "retrocediendo por el carril 0 tenia que agarrarlo");
            Assert.Greater(sim.State.Fuel, beforePickup);
        }

        [Test]
        public void ReversingTooFar_GetsYouCaught()
        {
            // La contracara del test anterior, fijada como regla: con la ventaja
            // normal, volver 40 m por un bidon cuesta la partida. El retroceso
            // sirve para correcciones cortas, no para desandar el camino.
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(50f, 0, LevelEntryKind.Fuel)));

            Advance(sim, level, 8f, Driving(DriveMode.Normal));
            Advance(sim, level, 8f, Driving(DriveMode.Reverse));

            Assert.AreEqual(RunPhase.Lost, sim.State.Phase);
            Assert.AreEqual(LossReason.CaughtByHorde, sim.State.Loss);
        }

        [Test]
        public void APickupIsCollectedOnlyOnce()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(30f, 1, LevelEntryKind.Ammo)));

            Advance(sim, level, 4f, Driving(DriveMode.Normal));
            Assert.AreEqual(5, sim.State.Ammo);

            // Ir y volver por encima varias veces no tiene que dar mas balas.
            Advance(sim, level, 2f, Driving(DriveMode.Reverse));
            Advance(sim, level, 3f, Driving(DriveMode.Normal));

            Assert.AreEqual(5, sim.State.Ammo);
        }

        [Test]
        public void HittingAnObstacle_StunsAndCostsFuel()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(30f, 1, LevelEntryKind.Obstacle)));

            Advance(sim, level, 2.9f, Driving(DriveMode.Normal));
            float fuelBefore = sim.State.Fuel;
            Advance(sim, level, 0.2f, Driving(DriveMode.Normal)); // cruza los 30 m

            Assert.Greater(sim.State.StunRemaining, 0f, "chocar tiene que aturdir");
            Assert.AreEqual(fuelBefore - 10f, sim.State.Fuel, 0.5f);
        }

        [Test]
        public void FastMovementDoesNotSkipEntries()
        {
            var sim = new RunSimulation(MakeConfig());
            // A 20 m/s el turbo avanza 0,33 m por frame; las entradas estan mas juntas.
            var level = new LevelRuntime(MakeLevel(
                new LevelEntry(20.0f, 1, LevelEntryKind.Ammo),
                new LevelEntry(20.1f, 1, LevelEntryKind.Ammo),
                new LevelEntry(20.2f, 1, LevelEntryKind.Ammo)));

            Advance(sim, level, 3f, Driving(DriveMode.Turbo));

            Assert.IsTrue(level.Items[0].Consumed && level.Items[1].Consumed
                          && level.Items[2].Consumed,
                "ningun encuentro se puede saltear por ir rapido");
        }

        [Test]
        public void EntriesAreSortedByDistanceOnLoad()
        {
            var level = new LevelRuntime(MakeLevel(
                new LevelEntry(300f, 1, LevelEntryKind.Fuel),
                new LevelEntry(100f, 0, LevelEntryKind.Ammo),
                new LevelEntry(200f, 2, LevelEntryKind.Battery)));

            Assert.AreEqual(100f, level.Items[0].Entry.distance);
            Assert.AreEqual(200f, level.Items[1].Entry.distance);
            Assert.AreEqual(300f, level.Items[2].Entry.distance);
        }

        [Test]
        public void Reset_MakesEverythingCollectableAgain()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(30f, 1, LevelEntryKind.Ammo)));

            Advance(sim, level, 4f, Driving(DriveMode.Normal));
            Assert.IsTrue(level.Items[0].Consumed);

            level.Reset();
            Assert.IsFalse(level.Items[0].Consumed);
        }

        [Test]
        public void AnEmptyLevelIsHarmless()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(null);

            Assert.AreEqual(0, level.Items.Length);
            Assert.DoesNotThrow(() => Advance(sim, level, 2f, Driving(DriveMode.Normal)));
        }

        // --- Rampas ------------------------------------------------------------

        static RunEvent AdvanceCollecting(RunSimulation sim, LevelRuntime level, float seconds, PlayerIntent intent)
        {
            var all = RunEvent.None;
            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps; i++)
            {
                float before = sim.State.PlayerX;
                all |= sim.Tick(intent, Dt);
                float after = sim.State.PlayerX;
                if (!Mathf.Approximately(before, after))
                    all |= level.Collect(sim, before, after);
            }
            return all;
        }

        [Test]
        public void RampInThePlayerLane_LaunchesWhenPassedForward()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(20f, 1, LevelEntryKind.Ramp)));

            var ev = AdvanceCollecting(sim, level, 2.1f, Driving(DriveMode.Normal));

            Assert.IsTrue((ev & RunEvent.Launched) != 0);
            Assert.IsFalse(level.Items[0].Consumed, "la rampa no se gasta");
        }

        [Test]
        public void RampInAnotherLane_DoesNotLaunch()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(20f, 0, LevelEntryKind.Ramp)));

            var ev = AdvanceCollecting(sim, level, 2.5f, Driving(DriveMode.Normal));

            Assert.IsFalse((ev & RunEvent.Launched) != 0);
        }

        [Test]
        public void RampPassedInReverse_DoesNotLaunch()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.State.PlayerX = 25f;
            var level = new LevelRuntime(MakeLevel(new LevelEntry(20f, 1, LevelEntryKind.Ramp)));

            var ev = AdvanceCollecting(sim, level, 2f, Driving(DriveMode.Reverse));

            Assert.Less(sim.State.PlayerX, 20f, "paso la rampa para atras");
            Assert.IsFalse((ev & RunEvent.Launched) != 0);
        }

        [Test]
        public void ObstaclesRightAfterTheRamp_AreFlownOver()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(
                new LevelEntry(20f, 1, LevelEntryKind.Ramp),
                new LevelEntry(24f, 1, LevelEntryKind.Obstacle),
                new LevelEntry(27f, 1, LevelEntryKind.Obstacle)));

            var ev = AdvanceCollecting(sim, level, 4f, Driving(DriveMode.Normal));

            Assert.IsFalse((ev & RunEvent.Crashed) != 0, "volando no se choca");
            Assert.IsFalse(level.Items[1].Consumed);
            Assert.IsFalse(level.Items[2].Consumed);
        }

        [Test]
        public void ObstacleBeyondTheLanding_IsHit()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(
                new LevelEntry(20f, 1, LevelEntryKind.Ramp),
                new LevelEntry(45f, 1, LevelEntryKind.Obstacle)));

            var ev = AdvanceCollecting(sim, level, 5f, Driving(DriveMode.Normal));

            Assert.IsTrue((ev & RunEvent.Landed) != 0);
            Assert.IsTrue((ev & RunEvent.Crashed) != 0, "despues de aterrizar vuelve a chocar");
        }

        [Test]
        public void GroundPickupUnderTheJump_IsNotCollected()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(
                new LevelEntry(20f, 1, LevelEntryKind.Ramp),
                new LevelEntry(25f, 1, LevelEntryKind.Fuel)));

            AdvanceCollecting(sim, level, 3f, Driving(DriveMode.Normal));

            Assert.IsFalse(level.Items[1].Consumed);
        }

        [Test]
        public void AerialPickup_IsCollectedOnlyByAJumpThatReachesIt()
        {
            // A 17 m de la rampa y 6 m de alto: el pico de un salto en turbo.
            var high = new LevelEntry(37f, 1, LevelEntryKind.Ammo, 6f);

            var turboSim = new RunSimulation(MakeConfig());
            var turboLevel = new LevelRuntime(MakeLevel(new LevelEntry(20f, 1, LevelEntryKind.Ramp), high));
            AdvanceCollecting(turboSim, turboLevel, 2.5f, Driving(DriveMode.Turbo));
            Assert.IsTrue(turboLevel.Items[1].Consumed, "el salto en turbo llega");

            var normalSim = new RunSimulation(MakeConfig());
            var normalLevel = new LevelRuntime(MakeLevel(new LevelEntry(20f, 1, LevelEntryKind.Ramp), high));
            AdvanceCollecting(normalSim, normalLevel, 4f, Driving(DriveMode.Normal));
            Assert.IsFalse(normalLevel.Items[1].Consumed, "el salto normal pasa por abajo");
        }

        [Test]
        public void AerialPickup_IsNotCollectedFromTheGround()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(10f, 1, LevelEntryKind.Fuel, 1f)));

            AdvanceCollecting(sim, level, 2f, Driving(DriveMode.Normal));

            Assert.IsFalse(level.Items[0].Consumed);
        }
    }
}
