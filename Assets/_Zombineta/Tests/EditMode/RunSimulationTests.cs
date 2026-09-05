using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Player;

namespace Zombineta.Tests
{
    /// <summary>
    /// Los tests usan su propia config con numeros redondos, no los valores de
    /// balance del juego. Asi el balance (T23) se puede reajustar libremente sin
    /// romper la verificacion de las reglas.
    /// </summary>
    public class RunSimulationTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();

            c.goalDistance = 1000f;
            c.startingGap = 50f;
            c.laneSpacing = 1.6f;
            c.laneChangeDuration = 0.1f;

            c.normalSpeed = 10f;
            c.turboSpeedMultiplier = 2f;      // 20 m/s
            c.reverseSpeedMultiplier = -0.5f; // -5 m/s

            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 1f;
            c.turboBurnMultiplier = 3f;
            c.reverseBurnMultiplier = 0.5f;
            c.fuelPickupAmount = 25f;

            c.batteryMax = 100f;
            c.batteryDrainPerSecond = 10f;
            c.batteryPickupAmount = 40f;
            c.headlightHordeSlowFactor = 0.5f;

            c.ammoMax = 6;
            c.ammoAtStart = 3;
            c.ammoPickupAmount = 2;
            c.shotHordePushback = 20f;

            c.hordeBaseSpeed = 11f;
            c.rubberBandStartGap = 80f;
            c.rubberBandRange = 80f;
            c.rubberBandMaxBonus = 0f; // desactivada salvo en su propio test

            c.crashStunDuration = 1f;
            c.crashFuelPenalty = 10f;

            return c;
        }

        static RunSimulation Advance(RunSimulation sim, float seconds, PlayerIntent intent)
        {
            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps; i++)
                sim.Tick(intent, Dt);
            return sim;
        }

        static PlayerIntent Driving(DriveMode mode) => new PlayerIntent { Mode = mode };

        // --- Estado inicial -------------------------------------------------

        [Test]
        public void Reset_StartsPlayerAheadOfHordeByStartingGap()
        {
            var sim = new RunSimulation(MakeConfig());

            Assert.AreEqual(0f, sim.State.PlayerX);
            Assert.AreEqual(-50f, sim.State.HordeX);
            Assert.AreEqual(50f, sim.State.Gap);
            Assert.AreEqual(1, sim.State.Lane, "arranca en el carril del medio");
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);
        }

        // --- Modos de conduccion --------------------------------------------

        [Test]
        public void NormalMode_LosesGroundSlowly()
        {
            var sim = new RunSimulation(MakeConfig());

            Advance(sim, 10f, Driving(DriveMode.Normal));

            // Jugadora 10 m/s, horda 11 m/s: pierde 1 m por segundo.
            Assert.AreEqual(100f, sim.State.PlayerX, 0.5f);
            Assert.AreEqual(40f, sim.State.Gap, 0.5f);
        }

        [Test]
        public void Turbo_GainsGroundButBurnsThreeTimesTheFuel()
        {
            var normal = new RunSimulation(MakeConfig());
            var turbo = new RunSimulation(MakeConfig());

            Advance(normal, 10f, Driving(DriveMode.Normal));
            Advance(turbo, 10f, Driving(DriveMode.Turbo));

            Assert.Greater(turbo.State.Gap, normal.State.Gap, "el turbo tiene que ganar terreno");
            Assert.AreEqual(90f, normal.State.Fuel, 0.2f);  // 10 s x 1
            Assert.AreEqual(70f, turbo.State.Fuel, 0.2f);   // 10 s x 3
        }

        [Test]
        public void Reverse_MovesPlayerBackwardsTowardsTheHorde()
        {
            var sim = new RunSimulation(MakeConfig());

            Advance(sim, 5f, Driving(DriveMode.Normal)); // avanza 50 m
            float gapBefore = sim.State.Gap;
            Advance(sim, 2f, Driving(DriveMode.Reverse));

            Assert.Less(sim.State.Gap, gapBefore, "retroceder tiene que costar distancia");
            // 2 s a -5 m/s = -10 m, mientras la horda avanza 22 m.
            Assert.AreEqual(40f, sim.State.PlayerX, 0.5f);
        }

        [Test]
        public void PlayerX_NeverGoesNegative()
        {
            var sim = new RunSimulation(MakeConfig());

            Advance(sim, 3f, Driving(DriveMode.Reverse));

            Assert.AreEqual(0f, sim.State.PlayerX);
        }

        // --- Combustible ----------------------------------------------------

        [Test]
        public void OutOfFuel_StopsTheScooter()
        {
            var config = MakeConfig();
            config.fuelMax = 2f; // 2 segundos de nafta
            var sim = new RunSimulation(config);

            Advance(sim, 2f, Driving(DriveMode.Normal));
            Assert.AreEqual(0f, sim.State.Fuel, 0.01f);

            // La ultima gota todavia empuja: el tick que vacia el tanque igual
            // mueve la moto. Recien el siguiente la deja clavada.
            Advance(sim, 0.1f, Driving(DriveMode.Turbo));
            float xWhenDry = sim.State.PlayerX;

            Advance(sim, 1f, Driving(DriveMode.Turbo)); // ni el turbo la mueve

            Assert.AreEqual(xWhenDry, sim.State.PlayerX, 0.001f);
        }

        [Test]
        public void RunningOutOfFuel_RaisesTheEventOnce()
        {
            var config = MakeConfig();
            config.fuelMax = 0.5f;
            var sim = new RunSimulation(config);

            int raised = 0;
            for (int i = 0; i < 120; i++)
            {
                if ((sim.Tick(Driving(DriveMode.Normal), Dt) & RunEvent.RanOutOfFuel) != 0)
                    raised++;
            }

            Assert.AreEqual(1, raised);
        }

        // --- Faro y bateria -------------------------------------------------

        [Test]
        public void Headlight_HalvesHordeSpeed()
        {
            var sim = new RunSimulation(MakeConfig());

            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, ToggleHeadlight = true }, Dt);
            Assert.IsTrue(sim.State.HeadlightOn);

            Advance(sim, 1f, Driving(DriveMode.Normal));

            Assert.AreEqual(5.5f, sim.HordeSpeed, 0.01f, "11 m/s a la mitad");
        }

        [Test]
        public void Headlight_DrainsBatteryAndShutsOffWhenEmpty()
        {
            var sim = new RunSimulation(MakeConfig());

            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, ToggleHeadlight = true }, Dt);
            Advance(sim, 5f, Driving(DriveMode.Normal));

            // 10 por segundo sobre 100 de bateria: a los 5 s quedan ~50.
            Assert.AreEqual(50f, sim.State.Battery, 1f);
            Assert.IsTrue(sim.State.HeadlightOn);

            Advance(sim, 6f, Driving(DriveMode.Normal));

            Assert.AreEqual(0f, sim.State.Battery, 0.01f);
            Assert.IsFalse(sim.State.HeadlightOn, "sin bateria el faro se apaga solo");
        }

        [Test]
        public void Headlight_CannotBeTurnedOnWithoutBattery()
        {
            var config = MakeConfig();
            config.batteryMax = 0f;
            var sim = new RunSimulation(config);

            var events = sim.Tick(
                new PlayerIntent { Mode = DriveMode.Normal, ToggleHeadlight = true }, Dt);

            Assert.IsFalse(sim.State.HeadlightOn);
            Assert.AreEqual(RunEvent.None, events & RunEvent.HeadlightOn);
        }

        // --- Pistola --------------------------------------------------------

        [Test]
        public void Fire_PushesTheHordeBackAndSpendsAmmo()
        {
            var sim = new RunSimulation(MakeConfig());
            float gapBefore = sim.State.Gap;

            var events = sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, Fire = true }, Dt);

            Assert.AreNotEqual(RunEvent.None, events & RunEvent.Shot);
            Assert.AreEqual(2, sim.State.Ammo);
            // +20 de empuje, menos lo poco que avanza la horda en un frame.
            Assert.AreEqual(gapBefore + 20f, sim.State.Gap, 0.5f);
        }

        [Test]
        public void Fire_WithoutAmmo_IsDeniedAndChangesNothing()
        {
            var config = MakeConfig();
            config.ammoAtStart = 0;
            var sim = new RunSimulation(config);
            float hordeBefore = sim.State.HordeX;

            var events = sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, Fire = true }, Dt);

            Assert.AreNotEqual(RunEvent.None, events & RunEvent.ShotDenied);
            Assert.AreEqual(0, sim.State.Ammo);
            Assert.Greater(sim.State.HordeX, hordeBefore, "la horda siguio avanzando igual");
        }

        // --- Horda ----------------------------------------------------------

        [Test]
        public void RubberBand_SpeedsUpTheHordeOnlyBeyondTheStartGap()
        {
            var config = MakeConfig();
            config.rubberBandMaxBonus = 4f;
            config.startingGap = 160f; // muy por encima de start(80) + range(80)
            var sim = new RunSimulation(config);

            sim.Tick(Driving(DriveMode.Normal), Dt);

            Assert.AreEqual(15f, sim.HordeSpeed, 0.1f, "11 base + 4 de bonus al maximo");
        }

        // --- Fin de partida -------------------------------------------------

        [Test]
        public void HordeCatchingThePlayer_LosesTheRun()
        {
            var config = MakeConfig();
            config.startingGap = 5f; // 5 m de ventaja, se pierde 1 m/s
            var sim = new RunSimulation(config);

            Advance(sim, 10f, Driving(DriveMode.Normal));

            Assert.AreEqual(RunPhase.Lost, sim.State.Phase);
            Assert.AreEqual(LossReason.CaughtByHorde, sim.State.Loss);
        }

        [Test]
        public void BeingCaughtWithAnEmptyTank_ReportsOutOfFuel()
        {
            var config = MakeConfig();
            config.fuelMax = 1f;
            config.startingGap = 20f;
            var sim = new RunSimulation(config);

            Advance(sim, 10f, Driving(DriveMode.Normal));

            Assert.AreEqual(RunPhase.Lost, sim.State.Phase);
            Assert.AreEqual(LossReason.OutOfFuel, sim.State.Loss);
        }

        [Test]
        public void ReachingTheGoal_WinsTheRun()
        {
            var config = MakeConfig();
            config.goalDistance = 100f;
            var sim = new RunSimulation(config);

            Advance(sim, 11f, Driving(DriveMode.Normal));

            Assert.AreEqual(RunPhase.Won, sim.State.Phase);
            Assert.AreEqual(1f, sim.Progress01, 0.001f);
        }

        [Test]
        public void AfterTheRunEnds_TickingDoesNothing()
        {
            var config = MakeConfig();
            config.goalDistance = 100f;
            var sim = new RunSimulation(config);

            Advance(sim, 11f, Driving(DriveMode.Normal));
            float xAtGoal = sim.State.PlayerX;

            Advance(sim, 5f, Driving(DriveMode.Turbo));

            Assert.AreEqual(xAtGoal, sim.State.PlayerX, 0.001f);
        }

        // --- Choques y pickups ----------------------------------------------

        [Test]
        public void Crash_StunsThePlayerAndCostsFuel()
        {
            var sim = new RunSimulation(MakeConfig());
            Advance(sim, 1f, Driving(DriveMode.Normal));

            float fuelBefore = sim.State.Fuel;
            sim.ApplyCrash();

            Assert.AreEqual(fuelBefore - 10f, sim.State.Fuel, 0.01f);

            float xAtCrash = sim.State.PlayerX;
            Advance(sim, 0.5f, Driving(DriveMode.Normal));

            Assert.AreEqual(xAtCrash, sim.State.PlayerX, 0.01f, "aturdida no se mueve");

            Advance(sim, 1f, Driving(DriveMode.Normal));
            Assert.Greater(sim.State.PlayerX, xAtCrash, "pasado el aturdimiento, arranca de nuevo");
        }

        [Test]
        public void Pickups_RestoreResourcesWithoutExceedingTheMaximum()
        {
            var sim = new RunSimulation(MakeConfig());

            // Vaciar un poco para que los pickups tengan efecto medible.
            Advance(sim, 30f, Driving(DriveMode.Normal));
            float fuelBefore = sim.State.Fuel;

            sim.ApplyPickup(PickupKind.Fuel);
            Assert.AreEqual(fuelBefore + 25f, sim.State.Fuel, 0.01f);

            // Con el tanque casi lleno, el pickup no lo desborda.
            sim.ApplyPickup(PickupKind.Fuel);
            sim.ApplyPickup(PickupKind.Fuel);
            Assert.AreEqual(100f, sim.State.Fuel, 0.01f);

            sim.ApplyPickup(PickupKind.Ammo);
            Assert.AreEqual(5, sim.State.Ammo);
            sim.ApplyPickup(PickupKind.Ammo);
            sim.ApplyPickup(PickupKind.Ammo);
            Assert.AreEqual(6, sim.State.Ammo, "no puede pasar de ammoMax");
        }

        // --- Carriles -------------------------------------------------------

        [Test]
        public void LaneChanges_ClampToTheThreeLanes()
        {
            var sim = new RunSimulation(MakeConfig());

            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = 1 }, Dt);
            Assert.AreEqual(2, sim.State.Lane);

            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = 1 }, Dt);
            Assert.AreEqual(2, sim.State.Lane, "no hay carril arriba del 2");

            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = -1 }, Dt);
            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = -1 }, Dt);
            Assert.AreEqual(0, sim.State.Lane);

            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = -1 }, Dt);
            Assert.AreEqual(0, sim.State.Lane, "no hay carril abajo del 0");
        }

        [Test]
        public void LaneVisual_CatchesUpToTheLogicalLane()
        {
            var sim = new RunSimulation(MakeConfig());

            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = 1 }, Dt);
            Assert.AreEqual(2, sim.State.Lane);
            Assert.Less(sim.State.LaneVisual, 2f, "el tween todavia no llego");

            Advance(sim, 0.2f, Driving(DriveMode.Normal)); // laneChangeDuration = 0.1

            Assert.AreEqual(2f, sim.State.LaneVisual, 0.001f);
        }
    }
}
