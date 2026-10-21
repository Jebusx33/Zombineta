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
            c.imanCarriles = 0f;   // estos tests miden la regla de "solo en su carril"; el imán tiene los suyos
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
        // --- Contacto por largo: la punta de la moto contra el borde de las cosas ------------

        [Test]
        public void AnObstacle_IsHitWhenTheNoseTouchesItsNearEdge_NotItsCenter()
        {
            var c = MakeConfig();
            c.motoNariz = 1.4f;
            var sim = new RunSimulation(c);
            var auto = new LevelEntry(100f, 1, LevelEntryKind.Obstacle) { medioLargo = 3f };
            var level = new LevelRuntime(MakeLevel(auto));

            sim.State.PlayerX = 95f; // la punta en 96,4: todavia no toca el borde (97)
            Advance(sim, level, Dt, Driving(DriveMode.Normal));
            Assert.IsFalse(level.Items[0].Consumed);

            Advance(sim, level, 0.1f, Driving(DriveMode.Normal));
            Assert.IsTrue(level.Items[0].Consumed, "la punta llego al borde del auto");
            Assert.Less(sim.State.PlayerX, 97f, "choco bastante antes del centro del auto");
        }

        [Test]
        public void APickup_IsCollectedWhenTheNoseReachesIt()
        {
            var c = MakeConfig();
            c.motoNariz = 1.4f;
            var sim = new RunSimulation(c);
            var bidon = new LevelEntry(100f, 1, LevelEntryKind.Fuel) { medioLargo = 0.5f };
            var level = new LevelRuntime(MakeLevel(bidon));

            sim.State.PlayerX = 98f; // la punta llega a 99,57: ya toca el borde del bidon (99,5)
            Advance(sim, level, Dt, Driving(DriveMode.Normal));

            Assert.IsTrue(level.Items[0].Consumed, "se agarra apenas lo toca la punta");
        }

        [Test]
        public void ChangingLane_IntoALongObstacleAlongside_Crashes()
        {
            var c = MakeConfig();
            c.motoNariz = 1.4f;
            c.motoCola = 1.4f;
            var sim = new RunSimulation(c);
            var auto = new LevelEntry(100f, 2, LevelEntryKind.Obstacle) { medioLargo = 3f };
            var level = new LevelRuntime(MakeLevel(auto));

            sim.State.PlayerX = 100f; // a la par del auto, en el carril de al lado
            Advance(sim, level, 0.2f, Driving(DriveMode.Normal));
            Assert.IsFalse(level.Items[0].Consumed, "en otro carril no lo toca");

            sim.State.PlayerX = 99f;
            Advance(sim, level, Dt, new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = 1 });
            Advance(sim, level, 0.2f, Driving(DriveMode.Normal));
            Assert.IsTrue(level.Items[0].Consumed, "meterse en su carril estando a la par es chocarlo");
        }

        [Test]
        public void WithoutLengths_ItIsTheOldPointContact()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(100f, 1, LevelEntryKind.Fuel)));

            sim.State.PlayerX = 99.5f;
            Advance(sim, level, Dt, Driving(DriveMode.Normal));
            Assert.IsFalse(level.Items[0].Consumed);

            Advance(sim, level, 0.1f, Driving(DriveMode.Normal));
            Assert.IsTrue(level.Items[0].Consumed);
        }

        // --- Fase 4: bidones bloqueados y arena despejada ---------------------------

        [Test]
        public void TheFuelCannotBePickedUp_UntilTheFirstFight()
        {
            var c = MakeConfig();
            c.bidonesDesdePelea = 1;
            c.fuelInicial = 50f;
            var sim = new RunSimulation(c);
            var level = new LevelRuntime(MakeLevel(new LevelEntry(30f, 1, LevelEntryKind.Fuel)));

            Advance(sim, level, 4f, PlayerIntent.Idle);   // pasa por encima del bidon (a 10 m/s)

            Assert.IsTrue(sim.State.PlayerX > 30f);
            Assert.IsFalse(level.Items[0].Consumed, "sin gastar: todavia no existe");
            Assert.Less(sim.State.Fuel, 50f, "y no recargo");
        }

        [Test]
        public void AfterTheFirstFight_TheFuelIsBackToNormal()
        {
            var c = MakeConfig();
            c.bidonesDesdePelea = 1;
            var sim = new RunSimulation(c);
            sim.State.Peleas = 1;
            sim.State.Fuel = 10f;
            var level = new LevelRuntime(MakeLevel(new LevelEntry(30f, 1, LevelEntryKind.Fuel)));

            Advance(sim, level, 4f, PlayerIntent.Idle);

            Assert.IsTrue(level.Items[0].Consumed);
            Assert.Greater(sim.State.Fuel, 25f, "recargo");
        }

        [Test]
        public void ALevelWithoutTheRule_AlwaysHasFuel()
        {
            var sim = new RunSimulation(MakeConfig());
            sim.State.Fuel = 10f;
            var level = new LevelRuntime(MakeLevel(new LevelEntry(30f, 1, LevelEntryKind.Fuel)));

            Advance(sim, level, 4f, PlayerIntent.Idle);

            Assert.IsTrue(level.Items[0].Consumed);
        }

        [Test]
        public void ClearingTheArena_RemovesTheObstaclesButNotThePickups()
        {
            var level = new LevelRuntime(MakeLevel(
                new LevelEntry(100f, 1, LevelEntryKind.Obstacle),
                new LevelEntry(105f, 0, LevelEntryKind.Ramp),
                new LevelEntry(110f, 2, LevelEntryKind.Barrel),
                new LevelEntry(112f, 1, LevelEntryKind.ZombieFront),
                new LevelEntry(114f, 1, LevelEntryKind.Ammo),
                new LevelEntry(500f, 1, LevelEntryKind.Obstacle)));

            int quitados = level.ClearZone(90f, 130f);

            Assert.AreEqual(3, quitados, "el coche, la rampa y el zombie de frente; el barril se queda (a pie explota)");
            Assert.IsTrue(level.Items[0].Consumed && level.Items[1].Consumed && level.Items[3].Consumed);
            Assert.IsFalse(level.Items[2].Consumed, "el barril se queda para dispararle a pie");
            Assert.IsFalse(level.Items[4].Consumed, "los recursos se quedan");
            Assert.IsFalse(level.Items[5].Consumed, "lo que esta lejos no se toca");
        }

        [Test]
        public void TheClearedArena_StaysClearedWhenTheBikeResumes()
        {
            var sim = new RunSimulation(MakeConfig());
            var level = new LevelRuntime(MakeLevel(new LevelEntry(100f, 1, LevelEntryKind.Obstacle)));
            level.ClearZone(90f, 110f);
            sim.State.PlayerX = 95f;

            Advance(sim, level, 2f, PlayerIntent.Idle);   // pasa sobre donde estaba el coche

            Assert.IsTrue(sim.State.PlayerX > 100f);
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);
            Assert.Greater(sim.State.Fuel, 95f, "ni choque ni castigo de nafta");
        }
    
        // --- Playtest de Axo, fase B: recolectables desde el carril de al lado ---------

        [Test]
        public void WithTheMagnet_APickupInTheNextLane_IsCollected()
        {
            var cfg = MakeConfig();
            cfg.imanCarriles = 1f;
            var sim = new RunSimulation(cfg);
            var level = new LevelRuntime(MakeLevel(new LevelEntry(50f, 0, LevelEntryKind.Fuel)));

            Advance(sim, level, 8f, Driving(DriveMode.Normal));   // pasa por el carril 1

            Assert.IsTrue(level.Items[0].Consumed, "estaba en el carril de al lado");
        }

        [Test]
        public void WithTheMagnet_APickupTwoLanesAway_IsStillMissed()
        {
            var cfg = MakeConfig();
            cfg.imanCarriles = 1f;
            var sim = new RunSimulation(cfg);
            var level = new LevelRuntime(MakeLevel(new LevelEntry(50f, 2, LevelEntryKind.Fuel)));

            sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, LaneDelta = -1 }, Dt);   // baja al carril 0
            Advance(sim, level, 8f, Driving(DriveMode.Normal));

            Assert.IsFalse(level.Items[0].Consumed, "a dos carriles queda lejos");
        }

        [Test]
        public void TheMagnet_OnlyAppliesToPickups_NotToObstacles()
        {
            var cfg = MakeConfig();
            cfg.imanCarriles = 1f;
            var sim = new RunSimulation(cfg);
            float nafta = sim.State.Fuel;
            var level = new LevelRuntime(MakeLevel(new LevelEntry(50f, 0, LevelEntryKind.Obstacle)));

            Advance(sim, level, 8f, Driving(DriveMode.Normal));

            Assert.IsFalse(level.Items[0].Consumed, "el obstaculo del carril de al lado no choca");
            Assert.IsFalse(sim.State.StunRemaining > 0f);
        }

        [Test]
        public void TheTelemetry_ReportsAPickupLeftBehind_WithItsLaneDistance()
        {
            var cfg = MakeConfig();
            cfg.imanCarriles = 0f;
            cfg.motoNariz = 1.4f;
            cfg.motoCola = 1.4f;
            cfg.medioLargoRecurso = 0.5f;
            var sim = new RunSimulation(cfg);
            var level = new LevelRuntime(MakeLevel(new LevelEntry(50f, 0, LevelEntryKind.Ammo)));
            var sesion = new Zombineta.Telemetria.SesionTelemetria("n", 1);

            var filas = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 60 * 8; i++)
            {
                float antes = sim.State.PlayerX;
                sim.Tick(Driving(DriveMode.Normal), Dt);
                level.Collect(sim, antes, sim.State.PlayerX);
                filas.AddRange(sesion.RecolectablesPasados(i * Dt, sim.State, cfg, level));
            }

            Assert.AreEqual(1, filas.Count);
            StringAssert.Contains(";item_pasado;", filas[0]);
            StringAssert.Contains("tipo=Ammo", filas[0]);
            StringAssert.Contains("dif_carril=1.0", filas[0]);
            Assert.AreEqual(1, sesion.ItemsPasados);
        }

        [Test]
        public void TheTelemetry_DoesNotReportWhatWasCollected()
        {
            var cfg = MakeConfig();
            var sim = new RunSimulation(cfg);
            var level = new LevelRuntime(MakeLevel(new LevelEntry(50f, 1, LevelEntryKind.Fuel)));
            var sesion = new Zombineta.Telemetria.SesionTelemetria("n", 1);

            int total = 0;
            for (int i = 0; i < 60 * 8; i++)
            {
                float antes = sim.State.PlayerX;
                sim.Tick(Driving(DriveMode.Normal), Dt);
                level.Collect(sim, antes, sim.State.PlayerX);
                total += sesion.RecolectablesPasados(i * Dt, sim.State, cfg, level).Count;
            }

            Assert.AreEqual(0, total);
        }

        // --- Lo despejado queda de decorado -------------------------------------------

        [Test]
        public void TheClearedArena_IsMarked_ExceptTheZombiesInFront_AndResourcesAreNotCleared()
        {
            var level = new LevelRuntime(MakeLevel(
                new LevelEntry(100f, 1, LevelEntryKind.Obstacle),
                new LevelEntry(102f, 0, LevelEntryKind.Barrel),
                new LevelEntry(104f, 1, LevelEntryKind.Ramp),
                new LevelEntry(106f, 2, LevelEntryKind.ZombieFront),
                new LevelEntry(108f, 1, LevelEntryKind.Fuel)));

            level.ClearZone(90f, 130f);

            Assert.IsTrue(level.Items[0].Despejado, "el coche queda de decorado");
            Assert.IsFalse(level.Items[1].Consumed, "el barril no se despeja: a pie se le dispara");
            Assert.IsFalse(level.Items[1].Despejado);
            Assert.IsTrue(level.Items[2].Despejado, "la rampa si");
            Assert.IsTrue(level.Items[3].Consumed);
            Assert.IsFalse(level.Items[3].Despejado, "los zombies de frente no");
            Assert.IsFalse(level.Items[4].Consumed, "el recurso no se despeja");
        }

        [Test]
        public void OnlyTheLastClearingCounts_AndRestartingWipesTheMark()
        {
            var level = new LevelRuntime(MakeLevel(
                new LevelEntry(100f, 1, LevelEntryKind.Obstacle),
                new LevelEntry(400f, 1, LevelEntryKind.Obstacle)));

            level.ClearZone(90f, 130f);
            level.ClearZone(390f, 430f);
            Assert.IsFalse(level.Items[0].Despejado, "el de la pelea anterior ya no es decorado");
            Assert.IsTrue(level.Items[1].Despejado);

            level.Reset();
            Assert.IsFalse(level.Items[1].Despejado);
            Assert.IsFalse(level.Items[1].Consumed);
        }

        [Test]
        public void ABarrelBlownUpByAShot_IsNotMarkedAsCleared()
        {
            var level = new LevelRuntime(MakeLevel(new LevelEntry(100f, 1, LevelEntryKind.Barrel)));
            level.Items[0].Consumed = true;   // detonado, no despejado
            Assert.IsFalse(level.Items[0].Despejado);
        }
}
}
