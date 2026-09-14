using System;
using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Player;

namespace Zombineta.Tests
{
    /// <summary>
    /// El salto con numeros propios del test (los del spec), no los del asset: el balance se
    /// puede reajustar sin romper la verificacion de las reglas.
    /// </summary>
    public class JumpTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 5000f;
            c.startingGap = 300f;      // horda lejos: aca se mide el salto, no la persecucion
            c.laneChangeDuration = 0.1f;
            c.normalSpeed = 12f;
            c.turboSpeedMultiplier = 1.8f;
            c.reverseSpeedMultiplier = -0.5f;
            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 1f;
            c.turboBurnMultiplier = 3f;
            c.reverseBurnMultiplier = 0.5f;
            c.hordeBaseSpeed = 5f;
            c.rubberBandMaxBonus = 0f;
            c.crashStunDuration = 0.8f;
            c.crashFuelPenalty = 8f;

            c.rampLaunchSlope = 0.75f;
            c.jumpGravity = 20f;
            c.leanLift = 0.35f;
            c.launchPitch = 20f;
            c.launchSpinPerExcessSpeed = 2.6f;
            c.leanRate = 60f;
            c.maxPitch = 80f;
            c.perfectLandingAngle = 8f;
            c.safeLandingAngle = 25f;
            c.landingBoostMultiplier = 1.25f;
            c.landingBoostDuration = 1.5f;
            c.fallStunDuration = 1.6f;
            c.fallFuelPenalty = 10f;
            c.aerialPickupTolerance = 1.5f;
            return c;
        }

        struct Flight
        {
            public float Distance;
            public float Airtime;
            public float PeakHeight;
            public RunEvent Landing;
        }

        static PlayerIntent Drive(DriveMode mode) => new PlayerIntent { Mode = mode };

        // Pilotos para el aire: que tecla aprieta la jugadora en cada cuadro.
        static DriveMode NoInput(RunState s) => DriveMode.Normal;
        static DriveMode LeanBack(RunState s) => DriveMode.Reverse;
        static DriveMode LeanForward(RunState s) => DriveMode.Turbo;

        // Corrige como lo haria alguien atento: nariz arriba, adelante; nariz abajo, atras.
        static DriveMode Stabilize(RunState s) =>
            s.Pitch > 2f ? DriveMode.Turbo : s.Pitch < -2f ? DriveMode.Reverse : DriveMode.Normal;

        /// <summary>Toma velocidad en el modo pedido, pisa la rampa y vuela hasta tocar el piso.</summary>
        static Flight Jump(RunSimulation sim, DriveMode approach, Func<RunState, DriveMode> pilot, float dt = Dt)
        {
            sim.Tick(Drive(approach), dt);
            Assert.AreEqual(RunEvent.Launched, sim.Launch(), "la rampa tendria que lanzar");

            var f = new Flight();
            float startX = sim.State.PlayerX;
            int guard = 0;
            while (sim.State.Airborne && guard++ < 100000)
            {
                var ev = sim.Tick(Drive(pilot(sim.State)), dt);
                f.Airtime += dt;
                f.PeakHeight = Mathf.Max(f.PeakHeight, sim.State.Height);
                if (!sim.State.Airborne)
                    f.Landing = ev;
            }
            f.Distance = sim.State.PlayerX - startX;
            return f;
        }

        static bool Has(RunEvent events, RunEvent flag) => (events & flag) != 0;

        // --- Lanzamiento ------------------------------------------------------

        [Test]
        public void Launch_FromNormalSpeed_GoesAirborneWithRampPitch()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.Tick(Drive(DriveMode.Normal), Dt);

            Assert.AreEqual(RunEvent.Launched, sim.Launch());
            Assert.IsTrue(sim.State.Airborne);
            Assert.AreEqual(12f, sim.State.AirSpeed, 1e-4f);
            Assert.AreEqual(9f, sim.State.VerticalSpeed, 1e-4f, "0,75 x 12 m/s");
            Assert.AreEqual(20f, sim.State.Pitch, 1e-4f);
            Assert.AreEqual(0f, sim.State.LaunchSpin, 1e-4f, "a velocidad normal no gira");
        }

        [Test]
        public void Launch_InTurbo_SpinsBackwards()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.Tick(Drive(DriveMode.Turbo), Dt);
            sim.Launch();

            // 21,6 m/s: 9,6 por encima de la normal, a 2,6 grados/s cada uno.
            Assert.AreEqual(24.96f, sim.State.LaunchSpin, 1e-3f);
        }

        [Test]
        public void Launch_InReverse_DoesNothing()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.State.PlayerX = 50f;
            sim.Tick(Drive(DriveMode.Reverse), Dt);

            Assert.AreEqual(RunEvent.None, sim.Launch());
            Assert.IsFalse(sim.State.Airborne);
        }

        [Test]
        public void Launch_WhileStunned_DoesNothing()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.ApplyCrash();
            sim.Tick(Drive(DriveMode.Normal), Dt);

            Assert.AreEqual(RunEvent.None, sim.Launch());
        }

        // --- Largo del salto ----------------------------------------------------

        [Test]
        public void NormalJump_WithoutInput_LandsSafelyButNotPerfect()
        {
            var sim = new RunSimulation(MakeConfig());
            var f = Jump(sim, DriveMode.Normal, NoInput);

            Assert.IsTrue(Has(f.Landing, RunEvent.Landed), "20 grados es aterrizaje sano");
            Assert.IsFalse(Has(f.Landing, RunEvent.LandedPerfect));
            Assert.IsFalse(Has(f.Landing, RunEvent.Fell));
            Assert.AreEqual(12.3f, f.Distance, 0.6f, "salto normal de unos 12 m");
            Assert.AreEqual(1.02f, f.Airtime, 0.05f);
        }

        [Test]
        public void FasterLaunch_JumpsFartherAndHigher()
        {
            var normal = Jump(new RunSimulation(MakeConfig()), DriveMode.Normal, Stabilize);
            var turbo = Jump(new RunSimulation(MakeConfig()), DriveMode.Turbo, Stabilize);

            Assert.Greater(turbo.Distance, normal.Distance * 2.5f);
            Assert.Greater(turbo.PeakHeight, normal.PeakHeight * 2.5f);
        }

        [Test]
        public void LeaningBack_Stretches_LeaningForward_Shortens()
        {
            var back = Jump(new RunSimulation(MakeConfig()), DriveMode.Normal, LeanBack);
            var neutral = Jump(new RunSimulation(MakeConfig()), DriveMode.Normal, NoInput);
            var forward = Jump(new RunSimulation(MakeConfig()), DriveMode.Normal, LeanForward);

            Assert.Greater(back.Distance, neutral.Distance + 1f, "nariz arriba planea");
            Assert.Less(forward.Distance, neutral.Distance - 1f, "nariz abajo cae antes");
        }

        // --- En el aire -----------------------------------------------------------

        [Test]
        public void InTheAir_NoFuelIsBurned()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.Tick(Drive(DriveMode.Normal), Dt);
            float fuel = sim.State.Fuel;
            sim.Launch();

            for (int i = 0; i < 20; i++)
                sim.Tick(Drive(DriveMode.Normal), Dt);

            Assert.IsTrue(sim.State.Airborne);
            Assert.AreEqual(fuel, sim.State.Fuel, 1e-5f);
        }

        [Test]
        public void InTheAir_LaneCannotChange()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.Tick(Drive(DriveMode.Normal), Dt);
            sim.Launch();

            var ev = sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = 1 }, Dt);

            Assert.AreEqual(1, sim.State.Lane);
            Assert.IsFalse(Has(ev, RunEvent.LaneChanged));
        }

        [Test]
        public void InTheAir_SpeedStaysAtLaunchSpeed()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.Tick(Drive(DriveMode.Turbo), Dt);
            sim.Launch();

            // Soltar el turbo en el aire no frena: sin traccion, la moto sigue a la velocidad de la rampa.
            sim.Tick(Drive(DriveMode.Normal), Dt);
            Assert.AreEqual(21.6f, sim.PlayerSpeed, 1e-3f);
        }

        // --- Aterrizaje -------------------------------------------------------------

        [Test]
        public void TurboJump_WithoutCorrecting_Falls()
        {
            var sim = new RunSimulation(MakeConfig());
            var f = Jump(sim, DriveMode.Turbo, NoInput);

            Assert.IsTrue(Has(f.Landing, RunEvent.Fell), "el giro de la rampa la deja de espaldas");
        }

        [Test]
        public void TurboJump_HoldingForwardAllTheWay_AlsoFalls()
        {
            var sim = new RunSimulation(MakeConfig());
            var f = Jump(sim, DriveMode.Turbo, LeanForward);

            Assert.IsTrue(Has(f.Landing, RunEvent.Fell), "corregir de mas la clava de trompa");
        }

        [Test]
        public void TurboJump_Stabilized_LandsPerfectAndBoosts()
        {
            var sim = new RunSimulation(MakeConfig());
            var f = Jump(sim, DriveMode.Turbo, Stabilize);

            Assert.IsTrue(Has(f.Landing, RunEvent.LandedPerfect));
            Assert.IsTrue(Has(f.Landing, RunEvent.Landed));
            Assert.AreEqual(1.5f, sim.State.BoostRemaining, 0.02f);
        }

        [Test]
        public void PerfectLanding_BoostsSpeedForItsDuration()
        {
            var sim = new RunSimulation(MakeConfig());
            Jump(sim, DriveMode.Normal, Stabilize);

            sim.Tick(Drive(DriveMode.Normal), Dt);
            Assert.AreEqual(15f, sim.PlayerSpeed, 1e-3f, "12 x 1,25");

            for (int i = 0; i < 100; i++)
                sim.Tick(Drive(DriveMode.Normal), Dt);
            Assert.AreEqual(12f, sim.PlayerSpeed, 1e-3f, "pasado 1,5 s vuelve a la normal");
        }

        [Test]
        public void Fall_StunsLongerThanACrash_AndCostsFuel()
        {
            var sim = new RunSimulation(MakeConfig());
            Jump(sim, DriveMode.Turbo, NoInput);
            float fuelAtLanding = sim.State.Fuel;

            Assert.IsTrue(sim.State.Fallen);
            Assert.AreEqual(1.6f, sim.State.StunRemaining, 0.02f);

            sim.Tick(Drive(DriveMode.Normal), Dt);
            Assert.AreEqual(0f, sim.PlayerSpeed, "tirada no avanza");

            for (int i = 0; i < 100; i++)
                sim.Tick(Drive(DriveMode.Normal), Dt);
            Assert.IsFalse(sim.State.Fallen, "a los 1,6 s se levanta");
            Assert.AreEqual(12f, sim.PlayerSpeed, 1e-3f);
            Assert.Less(fuelAtLanding, 100f - 9.9f, "la caida cuesta 10 de nafta");
        }

        // --- Pickups en el aire --------------------------------------------------------

        [Test]
        public void IsAtHeight_OnlyInTheAirAndWithinTolerance()
        {
            var sim = new RunSimulation(MakeConfig());
            Assert.IsFalse(sim.IsAtHeight(0.5f), "en el piso no se esta a ninguna altura");

            sim.Tick(Drive(DriveMode.Normal), Dt);
            sim.Launch();
            sim.State.Height = 5f;
            Assert.IsTrue(sim.IsAtHeight(6f));
            Assert.IsTrue(sim.IsAtHeight(3.6f));
            Assert.IsFalse(sim.IsAtHeight(7f));
        }

        // --- Robustez --------------------------------------------------------------------

        [Test]
        public void Jump_IsFrameRateIndependent()
        {
            var slow = Jump(new RunSimulation(MakeConfig()), DriveMode.Normal, NoInput, 1f / 30f);
            var fast = Jump(new RunSimulation(MakeConfig()), DriveMode.Normal, NoInput, 1f / 120f);

            Assert.AreEqual(fast.Distance, slow.Distance, 0.6f);
            Assert.AreEqual(fast.Landing, slow.Landing);
        }

        [Test]
        public void Reset_ClearsTheJump()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.Tick(Drive(DriveMode.Normal), Dt);
            sim.Launch();
            sim.Reset();

            Assert.IsFalse(sim.State.Airborne);
            Assert.AreEqual(0f, sim.State.Height);
            Assert.AreEqual(0f, sim.State.Pitch);
            Assert.AreEqual(0f, sim.State.BoostRemaining);
            Assert.IsFalse(sim.State.Fallen);
        }
    }
}
