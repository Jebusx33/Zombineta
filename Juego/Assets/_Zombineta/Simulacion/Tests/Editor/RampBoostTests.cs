using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;
using Zombineta.Player;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// El impulso gratis de la rampa: al saltar, la moto gana velocidad un rato sin gastar mas nafta, y el coche que
    /// se sobrevuela queda lejos de la rampa (no se encima con ella) pero sigue sin chocar.
    /// </summary>
    public class RampBoostTests
    {
        const float Dt = 1f / 60f;

        /// <summary>Los numeros del salto del asset (GameConfig.asset), para medir el recorrido real.</summary>
        static GameConfig AssetLike()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 5000f;
            c.startingGap = 300f;
            c.laneChangeDuration = 0.15f;
            c.normalSpeed = 12f;
            c.turboSpeedMultiplier = 1.8f;
            c.reverseSpeedMultiplier = -0.5f;
            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 2.6f;
            c.turboBurnMultiplier = 3f;
            c.reverseBurnMultiplier = 0.5f;
            c.hordeBaseSpeed = 5f;
            c.rubberBandMaxBonus = 0f;
            c.motoNariz = 1.4f;
            c.motoCola = 1.4f;
            c.medioLargoRecurso = 0.5f;
            c.medioLargoObstaculo = 2f;
            c.medioLargoZombie = 0.4f;
            c.crashStunDuration = 0.05f;
            c.rampLaunchSlope = 0.42f;
            c.jumpGravity = 18f;
            c.leanLift = 0f;
            c.launchPitch = 15f;
            c.launchSpinPerExcessSpeed = 1f;
            c.leanRate = 60f;
            c.maxPitch = 22f;
            c.perfectLandingAngle = 8f;
            c.safeLandingAngle = 25f;
            c.landingBoostMultiplier = 1.25f;
            c.landingBoostDuration = 1.5f;
            c.aerialPickupTolerance = 1.5f;
            return c;
        }

        static void Paso(RunSimulation sim, LevelRuntime level, PlayerIntent intent, ref RunEvent acumulado)
        {
            float antes = sim.State.PlayerX;
            acumulado |= sim.Tick(intent, Dt);
            float despues = sim.State.PlayerX;
            if (level != null && !Mathf.Approximately(antes, despues))
                acumulado |= level.Collect(sim, antes, despues);
        }

        /// <summary>Pasa por una rampa en el metro 100 con un coche del mismo carril a "cocheA" metros de ella.</summary>
        static bool ChocaConElCoche(GameConfig cfg, float cocheA)
        {
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.entries.Add(new LevelEntry(100f, 1, LevelEntryKind.Ramp));
            def.entries.Add(new LevelEntry(100f + cocheA, 1, LevelEntryKind.Obstacle));
            var level = new LevelRuntime(def);
            var sim = new RunSimulation(cfg);
            sim.State.PlayerX = 80f;
            var ev = RunEvent.None;
            for (int i = 0; i < 60 * 6; i++)
                Paso(sim, level, PlayerIntent.Idle, ref ev);
            return (ev & RunEvent.Crashed) != 0;
        }

        [Test]
        public void ToTheRamp_Launch_GivesTheBoost_AndTheJumpGoesFaster()
        {
            var c = AssetLike();
            var sim = new RunSimulation(c);
            sim.Tick(PlayerIntent.Idle, Dt);

            Assert.AreEqual(RunEvent.Launched, sim.Launch());

            Assert.AreEqual(12f * c.rampBoostMultiplier, sim.State.AirSpeed, 0.01f);
            Assert.AreEqual(c.rampBoostDuration, sim.State.RampBoostRemaining, 0.001f);
            Assert.AreEqual(0.42f * 12f, sim.State.VerticalSpeed, 0.01f, "la subida no cambia: sale el mismo salto, mas largo");
        }

        [Test]
        public void AfterLanding_TheBoostKeepsPushing_ThenFadesOut()
        {
            var c = AssetLike();
            var sim = new RunSimulation(c);
            sim.Tick(PlayerIntent.Idle, Dt);
            sim.Launch();
            for (int i = 0; i < 60 && sim.State.Airborne; i++)
                sim.Tick(PlayerIntent.Idle, Dt);
            Assert.IsFalse(sim.State.Airborne, "ya aterrizo");
            Assert.Greater(sim.State.RampBoostRemaining, 0f, "todavia empuja");
            sim.Tick(PlayerIntent.Idle, Dt);
            Assert.Greater(sim.PlayerSpeed, 12f * c.rampBoostMultiplier * 0.99f, "mas rapido que el paso normal");

            for (int i = 0; i < 60 * 5; i++)
                sim.Tick(PlayerIntent.Idle, Dt);
            Assert.AreEqual(0f, sim.State.RampBoostRemaining);
            Assert.AreEqual(12f, sim.PlayerSpeed, 0.01f, "y vuelve a la velocidad normal");
        }

        [Test]
        public void TheBoost_IsFree_ItDoesNotBurnMoreFuel()
        {
            var normal = new RunSimulation(AssetLike());
            var conImpulso = new RunSimulation(AssetLike());
            conImpulso.Tick(PlayerIntent.Idle, Dt);
            conImpulso.Launch();
            conImpulso.State.Airborne = false;           // en el piso, con el impulso corriendo
            conImpulso.State.Height = 0f;
            normal.Tick(PlayerIntent.Idle, Dt);

            float f0 = conImpulso.State.Fuel, n0 = normal.State.Fuel;
            for (int i = 0; i < 60; i++)
            {
                conImpulso.Tick(PlayerIntent.Idle, Dt);
                normal.Tick(PlayerIntent.Idle, Dt);
            }

            Assert.Greater(conImpulso.State.RampBoostRemaining, 0f);
            Assert.AreEqual(n0 - normal.State.Fuel, f0 - conImpulso.State.Fuel, 0.001f, "gasta lo mismo que sin impulso");
        }

        [Test]
        public void TwoRampsInARow_DoNotStackTheBoost()
        {
            var c = AssetLike();
            var sim = new RunSimulation(c);
            sim.Tick(PlayerIntent.Idle, Dt);
            sim.Launch();
            sim.State.Airborne = false;
            sim.Tick(PlayerIntent.Idle, Dt);              // en el piso, con el impulso de la primera
            Assert.Greater(sim.PlayerSpeed, 12f * 1.3f);

            sim.Launch();

            Assert.AreEqual(12f * c.rampBoostMultiplier, sim.State.AirSpeed, 0.01f, "no es 1,4 x 1,4");
        }

        [Test]
        public void WithoutBoost_TheJumpIsAsBefore()
        {
            var c = AssetLike();
            c.rampBoostMultiplier = 1f;
            var sim = new RunSimulation(c);
            sim.Tick(PlayerIntent.Idle, Dt);
            sim.Launch();
            Assert.AreEqual(12f, sim.State.AirSpeed, 0.001f);
        }

        [Test]
        public void ReversingOntoARamp_GivesNothing()
        {
            var sim = new RunSimulation(AssetLike());
            sim.Tick(new PlayerIntent { Mode = DriveMode.Reverse }, Dt);
            Assert.AreEqual(RunEvent.None, sim.Launch());
            Assert.AreEqual(0f, sim.State.RampBoostRemaining);
        }

        [Test]
        public void StartingTheFight_CutsTheBoost()
        {
            var c = AssetLike();
            var sim = new RunSimulation(c);
            sim.State.PlayerX = 500f;
            sim.Horde.Reset(460f);
            sim.Tick(PlayerIntent.Idle, Dt);
            sim.Launch();
            sim.State.Airborne = false;
            sim.State.Fuel = 0f;
            for (int i = 0; i < 400 && sim.State.Phase != RunPhase.Pelea; i++)
                sim.Tick(PlayerIntent.Idle, Dt);
            Assert.AreEqual(RunPhase.Pelea, sim.State.Phase);
            Assert.AreEqual(0f, sim.State.RampBoostRemaining);
        }

        /// <summary>Metros a los que el generador pone el coche despues de la rampa (LevelGenerator.RampObstacleAt).</summary>
        const float CocheDeLaPieza = 6.1f;

        [Test]
        public void TheCarOfThePiece_IsFlownOver_AtNormalSpeedAndAtTurbo()
        {
            var c = AssetLike();
            Assert.IsFalse(ChocaConElCoche(c, CocheDeLaPieza), "a velocidad normal, con el impulso, pasa por encima");

            var turbo = new PlayerIntent { Mode = DriveMode.Turbo };
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.entries.Add(new LevelEntry(100f, 1, LevelEntryKind.Ramp));
            def.entries.Add(new LevelEntry(100f + CocheDeLaPieza, 1, LevelEntryKind.Obstacle));
            var level = new LevelRuntime(def);
            var sim = new RunSimulation(c);
            sim.State.PlayerX = 80f;
            var ev = RunEvent.None;
            for (int i = 0; i < 60 * 6; i++)
                Paso(sim, level, turbo, ref ev);
            Assert.IsTrue((ev & RunEvent.Launched) != 0);
            Assert.IsTrue((ev & RunEvent.Crashed) == 0, "y a turbo tambien");
        }

        [Test]
        public void TheCarOfThePiece_IsNotOnTopOfTheRamp_ButStillWithinTheJump()
        {
            // El dibujo de la rampa mide casi 6 m (mitad 3) y el del coche 4,65 m (mitad 2,5): sus centros tienen que
            // estar a mas de 5,5 m para no encimarse.
            Assert.GreaterOrEqual(CocheDeLaPieza, 3f + 2.5f + 0.3f, "no se encima con la rampa");
            // Y el salto con impulso tiene que cubrirlo: un poco mas lejos ya se choca.
            Assert.IsTrue(ChocaConElCoche(AssetLike(), CocheDeLaPieza + 1.5f), "el salto no da para mas");
        }
    }
}
