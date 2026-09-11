# Rampas y salto regulable — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rampas que lanzan a la moto, un salto que se regula inclinando en el aire (A/D) y un
aterrizaje que premia (impulso) o castiga (caída) según la inclinación.

**Architecture:** Todo el salto vive en la simulación de C# plano (`RunState`,
`RunSimulation`, `LevelRuntime`), testeado en EditMode. Las vistas (`ScooterView`,
`LevelSpawner`) y la cámara (`CameraDirector` + `CameraFollow`) solo leen estado. Spec:
`docs/superpowers/specs/2026-09-10-rampas-y-salto-design.md`.

**Tech Stack:** Unity 6000.6.0f1, 2D URP, NUnit (Test Runner EditMode), MCP nativo de Unity.

## Global Constraints

- Raíz del proyecto: `D:\Jose\Facu\Taller de proyecto integral\Prototipo\Zombineta\Zombineta`.
  Rama `Jose`. Commits terminan con `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`.
- La simulación no conoce Unity más allá de `Mathf`: nada de MonoBehaviour en `Core/` salvo los
  puentes que ya existen.
- Comentarios en castellano sin tildes en el código (como el resto del proyecto).
- Convención de inclinación: grados, **+ = nariz arriba**. En el aire, `DriveMode.Reverse`
  (A/←) = nariz arriba, `DriveMode.Turbo` (D/→) = nariz abajo.
- **Compilar:** después de editar `.cs`, ejecutar `Unity_ManageMenuItem` Action=Execute
  MenuPath=`Assets/Refresh`, y revisar `Unity_GetConsoleLogs` con logTypes `All` buscando
  `error CS` (los errores de compilación llegan como `Log`).
- **Correr tests:** `Unity_RunCommand` con el lanzador de abajo, y en un comando aparte leer
  `SessionState.GetString("zomb.tests")`:
  ```csharp
  using UnityEngine;
  using UnityEditor;
  using UnityEditor.TestTools.TestRunner.Api;
  internal class CommandScript : IRunCommand
  {
      class Cb : ICallbacks
      {
          public void RunStarted(ITestAdaptor t) {}
          public void RunFinished(ITestResultAdaptor r)
          {
              var sb = new System.Text.StringBuilder();
              sb.Append($"pass={r.PassCount} fail={r.FailCount} skip={r.SkipCount}");
              Collect(r, sb);
              SessionState.SetString("zomb.tests", sb.ToString());
          }
          void Collect(ITestResultAdaptor r, System.Text.StringBuilder sb)
          {
              if (!r.HasChildren && r.TestStatus == TestStatus.Failed)
                  sb.Append($" | FAIL {r.Test.Name}: {r.Message}");
              if (r.HasChildren) foreach (var c in r.Children) Collect(c, sb);
          }
          public void TestStarted(ITestAdaptor t) {}
          public void TestFinished(ITestResultAdaptor r) {}
      }
      public void Execute(ExecutionResult result)
      {
          SessionState.SetString("zomb.tests", "running");
          var api = ScriptableObject.CreateInstance<TestRunnerApi>();
          api.RegisterCallbacks(new Cb());
          api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
          result.Log("launched");
      }
  }
  ```
- No editar scripts con Unity en Play Mode.

## Archivos

| Archivo | Cambio |
|---|---|
| `Assets/_Zombineta/Scripts/Core/GameConfig.cs` | Sección "Salto" + `jumpHeightToWorld` |
| `Assets/_Zombineta/Scripts/Core/RunState.cs` | Campos del salto |
| `Assets/_Zombineta/Scripts/Core/RunSimulation.cs` | Eventos, `Launch`, aire, aterrizaje, impulso, `IsAtHeight` |
| `Assets/_Zombineta/Scripts/Core/RunController.cs` | `HeightToWorld` |
| `Assets/_Zombineta/Scripts/Level/LevelDefinition.cs` | `Ramp`, `height` |
| `Assets/_Zombineta/Scripts/Level/LevelRuntime.cs` | Rampas y filtro piso/aire |
| `Assets/_Zombineta/Scripts/Level/LevelSpawner.cs` | Cuña de rampa, pickups aéreos a su altura |
| `Assets/_Zombineta/Scripts/CameraFx/CameraConfig.cs` | Sección "Salto" |
| `Assets/_Zombineta/Scripts/CameraFx/CameraDirector.cs` | `JumpHeight`, tamaño mínimo por salto |
| `Assets/_Zombineta/Scripts/Core/CameraFollow.cs` | Eventos del salto, input de salto |
| `Assets/_Zombineta/Scripts/Player/ScooterView.cs` | Altura, rotación, caída, sombra |
| `Assets/_Zombineta/Tests/EditMode/JumpTests.cs` | Nuevo: física del salto |
| `Assets/_Zombineta/Tests/EditMode/LevelRuntimeTests.cs` | Tests de rampas y pickups aéreos |
| `Assets/_Zombineta/Tests/EditMode/CameraDirectorTests.cs` | Test de encuadre del salto |
| `Assets/_Zombineta/Art/Level/rampa_placeholder.png`, `sombra.png` | Nuevos, generados |
| `Assets/_Zombineta/Settings/Ruta01.asset`, `Ruta02.asset` | Piezas de rampa |
| `Assets/_Zombineta/Scenes/Prototipo.unity` | Sombra y sprite de rampa cableados |
| `HANDOFF.md` | Sección del salto |

---

### Task 1: Física del salto en la simulación

**Files:**
- Modify: `Assets/_Zombineta/Scripts/Core/GameConfig.cs`
- Modify: `Assets/_Zombineta/Scripts/Core/RunState.cs`
- Modify: `Assets/_Zombineta/Scripts/Core/RunSimulation.cs`
- Create: `Assets/_Zombineta/Tests/EditMode/JumpTests.cs`

**Interfaces:**
- Produces: `RunSimulation.Launch() : RunEvent`, `RunSimulation.IsAtHeight(float meters) : bool`,
  `RunEvent.Launched | Landed | LandedPerfect | Fell`, campos `RunState.Airborne, Height,
  VerticalSpeed, AirSpeed, Pitch, LaunchSpin, BoostRemaining, Fallen`, campos de `GameConfig`
  listados abajo (incluido `jumpHeightToWorld`).

- [ ] **Step 1: Escribir los tests que fallan** — crear `JumpTests.cs`:

```csharp
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
```

- [ ] **Step 2: Compilar y ver que falla** — `Assets/Refresh`, consola con `error CS`.
  Expected: errores de compilación (`Launch`, `Airborne`, `rampLaunchSlope`... no existen).

- [ ] **Step 3: `GameConfig`** — agregar antes de `[Header("Presentacion")]`:

```csharp
        [Header("Salto")]
        [Tooltip("Velocidad vertical de salida por cada m/s con que se pisa la rampa. " +
                 "Mas rapido: mas alto y mas lejos.")]
        public float rampLaunchSlope = 0.75f;
        [Tooltip("Gravedad del salto en m/s2. De juego, no realista.")]
        public float jumpGravity = 20f;
        [Tooltip("Cuanto planea con la nariz arriba y cuanto cae con la nariz abajo. " +
                 "0 = inclinar no cambia el largo del salto.")]
        [Range(0f, 0.9f)] public float leanLift = 0.35f;
        [Tooltip("Grados de nariz arriba al salir de la rampa. Un salto normal sin tocar nada " +
                 "aterriza con esta inclinacion.")]
        public float launchPitch = 20f;
        [Tooltip("Giro hacia atras al salir, en grados/s por cada m/s por encima de la " +
                 "velocidad normal. A velocidad normal no gira; en turbo hay que corregir.")]
        public float launchSpinPerExcessSpeed = 2.6f;
        [Tooltip("Grados por segundo que inclina la jugadora con A/D en el aire.")]
        public float leanRate = 60f;
        public float maxPitch = 80f;
        [Tooltip("Aterrizar con menos inclinacion que esta da impulso.")]
        public float perfectLandingAngle = 8f;
        [Tooltip("Aterrizar con mas inclinacion que esta tira la moto al piso.")]
        public float safeLandingAngle = 25f;
        public float landingBoostMultiplier = 1.25f;
        public float landingBoostDuration = 1.5f;
        [Tooltip("Segundos tirada en el piso tras aterrizar mal. Mas que un choque.")]
        public float fallStunDuration = 1.6f;
        public float fallFuelPenalty = 10f;
        [Tooltip("Metros de diferencia de altura con que se agarra un pickup en el aire.")]
        public float aerialPickupTolerance = 1.5f;
```

  y al final de "Presentacion":

```csharp
        [Tooltip("Unidades de mundo por metro de altura del salto. Mas que en X (0,25): " +
                 "exagerado a proposito para que el salto se lea.")]
        public float jumpHeightToWorld = 0.5f;
```

- [ ] **Step 4: `RunState`** — agregar después de `public float Elapsed;`:

```csharp
        // --- Salto ---
        public bool Airborne;
        /// <summary>Metros sobre el carril.</summary>
        public float Height;
        /// <summary>m/s, positivo hacia arriba.</summary>
        public float VerticalSpeed;
        /// <summary>Velocidad horizontal durante el salto: la de la rampa, fija hasta aterrizar.</summary>
        public float AirSpeed;
        /// <summary>Inclinacion en grados. Positivo = nariz arriba.</summary>
        public float Pitch;
        /// <summary>Giro en grados/s que trae de la rampa (mas rapido, mas gira).</summary>
        public float LaunchSpin;
        /// <summary>Segundos que quedan del impulso por aterrizaje perfecto.</summary>
        public float BoostRemaining;
        /// <summary>Tirada en el piso tras aterrizar mal. Dura lo que el aturdimiento.</summary>
        public bool Fallen;
```

- [ ] **Step 5: `RunSimulation`** — cambios:

  5a. En `RunEvent`, después de `Lost = 1 << 10,`:

```csharp
        Launched = 1 << 11,
        Landed = 1 << 12,
        LandedPerfect = 1 << 13,
        Fell = 1 << 14,
```

  5b. En `Reset()`, después de `State.Loss = LossReason.None;`:

```csharp
            State.Airborne = false;
            State.Height = 0f;
            State.VerticalSpeed = 0f;
            State.AirSpeed = 0f;
            State.Pitch = 0f;
            State.LaunchSpin = 0f;
            State.BoostRemaining = 0f;
            State.Fallen = false;
```

  5c. En `Tick`, reemplazar desde `// El aturdimiento por choque...` hasta
  `events |= DrainBattery(dt);` (exclusive) por:

```csharp
            // El aturdimiento por choque corre aunque la moto este frenada.
            if (State.StunRemaining > 0f)
            {
                State.StunRemaining = Mathf.Max(0f, State.StunRemaining - dt);
                if (State.StunRemaining <= 0f)
                    State.Fallen = false;
            }

            if (State.BoostRemaining > 0f)
                State.BoostRemaining = Mathf.Max(0f, State.BoostRemaining - dt);

            bool hadFuel = State.Fuel > 0f;
            State.Mode = intent.Mode;

            if (State.Airborne)
            {
                // Sin traccion: la velocidad es la de la rampa y el motor no gasta.
                PlayerSpeed = State.AirSpeed;
                events |= StepAir(intent.Mode, dt);
            }
            else
            {
                PlayerSpeed = ComputePlayerSpeed();
                BurnFuel(dt);
                if (hadFuel && State.Fuel <= 0f)
                    events |= RanOutOfFuel;
            }

```

  (Queda `events |= DrainBattery(dt);` a continuación, como antes. Ojo: `RanOutOfFuel` es
  `RunEvent.RanOutOfFuel`.)

  5d. En `ApplyLaneChange`, primera línea del cuerpo:

```csharp
            // En el aire no hay de donde agarrarse para cambiar de carril.
            if (delta == 0 || State.Airborne)
                return RunEvent.None;
```

  (reemplaza el `if (delta == 0) return RunEvent.None;` existente).

  5e. Reemplazar `ComputePlayerSpeed()` entero por:

```csharp
        float ComputePlayerSpeed()
        {
            if (State.StunRemaining > 0f)
                return 0f;

            // Sin nafta la moto se para: la horda hace el resto.
            if (State.Fuel <= 0f)
                return 0f;

            float speed;
            switch (State.Mode)
            {
                case DriveMode.Turbo:
                    speed = config.normalSpeed * config.turboSpeedMultiplier;
                    break;
                case DriveMode.Reverse:
                    speed = config.normalSpeed * config.reverseSpeedMultiplier;
                    break;
                default:
                    speed = config.normalSpeed;
                    break;
            }

            // El impulso del aterrizaje perfecto solo empuja hacia adelante.
            if (State.BoostRemaining > 0f && speed > 0f)
                speed *= config.landingBoostMultiplier;

            return speed;
        }
```

  5f. Agregar antes de `RunEvent CheckEndConditions()`:

```csharp
        RunEvent StepAir(DriveMode lean, float dt)
        {
            // En el aire A/D inclinan: retroceso levanta la nariz, turbo la baja.
            float input = lean == DriveMode.Reverse ? 1f : lean == DriveMode.Turbo ? -1f : 0f;
            State.Pitch = Mathf.Clamp(
                State.Pitch + (State.LaunchSpin + input * config.leanRate) * dt,
                -config.maxPitch, config.maxPitch);

            // Nariz arriba planea, nariz abajo cae antes: asi se regula el largo del salto.
            float lift = config.leanLift * Mathf.Sin(State.Pitch * Mathf.Deg2Rad);
            State.VerticalSpeed -= config.jumpGravity * (1f - lift) * dt;
            State.Height += State.VerticalSpeed * dt;

            return State.Height > 0f ? RunEvent.None : Land();
        }

        RunEvent Land()
        {
            float angle = Mathf.Abs(State.Pitch);

            State.Airborne = false;
            State.Height = 0f;
            State.VerticalSpeed = 0f;
            State.Pitch = 0f;
            State.LaunchSpin = 0f;

            if (angle <= config.perfectLandingAngle)
            {
                State.BoostRemaining = config.landingBoostDuration;
                return RunEvent.Landed | RunEvent.LandedPerfect;
            }

            if (angle <= config.safeLandingAngle)
                return RunEvent.Landed;

            // Aterrizo torcida: al piso. La horda hace el resto.
            State.StunRemaining = config.fallStunDuration;
            State.Fuel = Mathf.Max(0f, State.Fuel - config.fallFuelPenalty);
            State.Fallen = true;
            return RunEvent.Fell;
        }
```

  5g. Agregar al final de la clase, después de `ApplyCrash()`:

```csharp
        /// <summary>
        /// La moto piso una rampa: sale volando con la velocidad que traia. De reversa o
        /// frenada no pasa nada. Lo llama LevelRuntime al resolver el tramo recorrido.
        /// </summary>
        public RunEvent Launch()
        {
            if (State.Airborne || State.Phase != RunPhase.Running || PlayerSpeed <= 0f)
                return RunEvent.None;

            State.Airborne = true;
            State.Height = 0f;
            State.AirSpeed = PlayerSpeed;
            State.VerticalSpeed = config.rampLaunchSlope * PlayerSpeed;
            State.Pitch = config.launchPitch;
            State.LaunchSpin = config.launchSpinPerExcessSpeed *
                               Mathf.Max(0f, PlayerSpeed - config.normalSpeed);
            return RunEvent.Launched;
        }

        /// <summary>Si la moto esta volando a esa altura (con la tolerancia de los pickups aereos).</summary>
        public bool IsAtHeight(float meters) =>
            State.Airborne && Mathf.Abs(State.Height - meters) <= config.aerialPickupTolerance;
```

- [ ] **Step 6: Compilar y correr la suite** — Expected: `pass=91 fail=0` (73 anteriores + 18
  nuevos). Si algún test de largo o tiempo falla por poco, revisar la cuenta antes de tocar la
  tolerancia: con los números del spec el salto normal sin input dura ≈1,02 s y ≈12,3 m.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Zombineta/Scripts/Core Assets/_Zombineta/Tests/EditMode/JumpTests.cs Assets/_Zombineta/Tests/EditMode/JumpTests.cs.meta
git commit -m "feat: fisica del salto con inclinacion, aterrizaje e impulso"
```

---

### Task 2: Rampas y pickups aéreos en el recorrido

**Files:**
- Modify: `Assets/_Zombineta/Scripts/Level/LevelDefinition.cs`
- Modify: `Assets/_Zombineta/Scripts/Level/LevelRuntime.cs`
- Modify: `Assets/_Zombineta/Tests/EditMode/LevelRuntimeTests.cs`

**Interfaces:**
- Consumes: `RunSimulation.Launch()`, `RunSimulation.IsAtHeight(float)`, `RunState.Airborne`.
- Produces: `LevelEntryKind.Ramp` (valor 4), `LevelEntry.height`,
  `LevelEntry(float distance, int lane, LevelEntryKind kind, float height = 0f)`.

- [ ] **Step 1: Tests que fallan** — agregar al final de la clase `LevelRuntimeTests`:

```csharp
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
```

  Notas para quien implemente: `MakeConfig()` de esta clase no toca los campos del salto, así
  que usa los defaults de `GameConfig` (los del spec). Con `normalSpeed = 10` y turbo ×2
  (20 m/s): salto normal ≈ 9 m de largo (de 20 a ≈29 m), turbo con D mantenido ≈ 30 m con
  pico ≈ 5,6 m cerca de los 15 m. El test del pickup aéreo usa esos números: si falla por
  poco, recalcular y mover `high`, no la tolerancia.

- [ ] **Step 2: Compilar y ver que falla** — Expected: `error CS` por `LevelEntryKind.Ramp` y
  el constructor de 4 argumentos.

- [ ] **Step 3: `LevelDefinition.cs`** — el enum y el struct quedan:

```csharp
    public enum LevelEntryKind
    {
        Obstacle,
        Fuel,
        Battery,
        Ammo,
        Ramp,
    }

    /// <summary>Una cosa colocada en el recorrido: a tantos metros, en tal carril.</summary>
    [Serializable]
    public struct LevelEntry
    {
        [Tooltip("Metros desde la largada.")]
        public float distance;

        [Range(0, 2)]
        [Tooltip("0 = carril de abajo, 1 = medio, 2 = arriba.")]
        public int lane;

        public LevelEntryKind kind;

        [Tooltip("Metros sobre el carril. 0 = en el piso. Mayor a 0: solo se agarra saltando.")]
        public float height;

        public LevelEntry(float distance, int lane, LevelEntryKind kind, float height = 0f)
        {
            this.distance = distance;
            this.lane = lane;
            this.kind = kind;
            this.height = height;
        }
    }
```

- [ ] **Step 4: `LevelRuntime.Collect`** — reemplazar el cuerpo del `for` y el cálculo previo:

```csharp
            float min = Mathf.Min(fromX, toX);
            float max = Mathf.Max(fromX, toX);
            float lane = sim.State.LaneVisual;
            bool forward = toX > fromX;
            var events = RunEvent.None;

            for (int i = 0; i < Items.Length; i++)
            {
                var item = Items[i];
                float d = item.Entry.distance;

                // Ordenadas por distancia: pasado el tramo, no hay mas candidatas.
                if (d > max)
                    break;
                if (d < min || item.Consumed)
                    continue;
                if (Mathf.Abs(lane - item.Entry.lane) > LaneTolerance)
                    continue;

                // El estado se consulta en cada entrada: una rampa lanza, y lo que sigue en
                // el mismo tramo ya se sobrevuela.
                if (item.Entry.kind == LevelEntryKind.Ramp)
                {
                    // Solo hacia adelante y desde el piso. No se gasta: se puede volver y saltar de nuevo.
                    if (forward && !sim.State.Airborne)
                        events |= sim.Launch();
                    continue;
                }

                // Lo del piso no se toca volando; lo del aire solo volando a su altura.
                bool aerial = item.Entry.height > 0f;
                if (aerial ? !sim.IsAtHeight(item.Entry.height) : sim.State.Airborne)
                    continue;

                item.Consumed = true;
                events |= Apply(sim, item.Entry.kind);
            }

            return events;
```

- [ ] **Step 5: Compilar y correr la suite** — Expected: `pass=99 fail=0`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Zombineta/Scripts/Level Assets/_Zombineta/Tests/EditMode/LevelRuntimeTests.cs
git commit -m "feat: rampas y pickups aereos en el recorrido"
```

---

### Task 3: La cámara acompaña el salto

**Files:**
- Modify: `Assets/_Zombineta/Scripts/CameraFx/CameraConfig.cs`
- Modify: `Assets/_Zombineta/Scripts/CameraFx/CameraDirector.cs`
- Modify: `Assets/_Zombineta/Scripts/Core/CameraFollow.cs`
- Modify: `Assets/_Zombineta/Scripts/Core/RunController.cs`
- Modify: `Assets/_Zombineta/Tests/EditMode/CameraDirectorTests.cs`

**Interfaces:**
- Consumes: `RunEvent.Fell`, `RunEvent.LandedPerfect`, `RunState.Airborne`, `RunState.Height`,
  `GameConfig.jumpHeightToWorld`.
- Produces: `CameraInput.JumpHeight` (mundo), `CameraConfig.jumpHeadroom`, `jumpZoomTime`,
  `perfectLandingTrauma`, `perfectLandingPunchForward`, `RunController.HeightToWorld(float)`.

- [ ] **Step 1: Test que falla** — en `CameraDirectorTests`, agregar a `Cfg()` antes de
  `return c;`:

```csharp
            c.jumpHeadroom = 2f;
            c.jumpZoomTime = 0.15f;
```

  y agregar los tests:

```csharp
        [Test]
        public void Jump_OpensTheShotSoTheScooterStaysInFrame()
        {
            var d = Make(gap: 6f);              // horda encima: plano cerrado (5)
            var input = In(6f);
            input.PlayerY = 1.6f;               // carril de arriba
            input.JumpHeight = 3.3f;            // pico de un salto en turbo

            var pose = Run(d, input, 1f);

            float top = pose.Y + pose.Size;     // borde de arriba de la pantalla
            Assert.GreaterOrEqual(top, 1.6f + 3.3f + 2f - 0.05f);
        }

        [Test]
        public void NoJump_KeepsTheTensionFraming()
        {
            var d = Make(gap: 6f);
            var pose = Run(d, In(6f), 3f);

            Assert.AreEqual(5f, pose.Size, 0.02f);
        }
```

- [ ] **Step 2: Compilar y ver que falla** — Expected: `error CS` (`jumpHeadroom`,
  `JumpHeight`).

- [ ] **Step 3: `CameraConfig`** — agregar al final de la clase:

```csharp
        [Header("Salto")]
        [Tooltip("Unidades que se dejan libres por encima del carril mas la altura del salto. " +
                 "Incluye el alto de la moto: si es menor, la moto se sale por arriba.")]
        public float jumpHeadroom = 2f;

        [Tooltip("Segundos para abrirse cuando lo que manda es el salto. Mas rapido que el " +
                 "zoom normal: la moto sube rapido.")]
        public float jumpZoomTime = 0.15f;

        [Tooltip("Aterrizaje perfecto: golpecito de satisfaccion.")]
        public float perfectLandingTrauma = 0.12f;
        public float perfectLandingPunchForward = 0.25f;
```

- [ ] **Step 4: `CameraDirector`** — cambios:

  4a. En `CameraInput`, agregar:

```csharp
        public float JumpHeight; // mundo: cuanto se elevo la moto sobre su carril
```

  4b. En `Snap`, reemplazar `size = FollowSize();` por `size = FollowSize(input);`.

  4c. En `Step`, rama `default`, reemplazar las dos primeras líneas por:

```csharp
                    float target = FollowSize(input);
                    // Asimetrico: cerrarse lento, abrirse rapido. Si manda el salto, mas rapido todavia.
                    float tau = target < size ? cfg.zoomInTime
                              : JumpSize(input) >= target ? cfg.jumpZoomTime
                              : cfg.zoomOutTime;
```

  4d. Reemplazar `FollowSize()` por:

```csharp
        float FollowSize(CameraInput input)
        {
            if (!EffectsEnabled)
                return cfg.baseSize;
            float tension = Mathf.Lerp(cfg.wideSize, cfg.tightSize, Threat) + turbo * cfg.turboExtraSize;
            return Mathf.Max(tension, JumpSize(input));
        }

        /// <summary>
        /// Tamano minimo para que la moto en el aire no se salga por arriba. El borde de abajo
        /// es fijo, asi que el de arriba esta a dos tamanos de el.
        /// </summary>
        float JumpSize(CameraInput input) =>
            input.JumpHeight <= 0f
                ? 0f
                : (input.PlayerY + input.JumpHeight + cfg.jumpHeadroom - cfg.viewBottomY) * 0.5f;
```

- [ ] **Step 5: `RunController`** — agregar junto a `ToWorldX`:

```csharp
        /// <summary>Convierte metros de altura del salto a unidades de mundo.</summary>
        public float HeightToWorld(float meters) => meters * config.jumpHeightToWorld;
```

- [ ] **Step 6: `CameraFollow`** — cambios:

  6a. En `BuildInput`, la línea `Turbo = ...` y una nueva:

```csharp
                // Turbo "de verdad": apretar D sin nafta, aturdida o en el aire no acelera.
                Turbo = s.Mode == DriveMode.Turbo && s.Fuel > 0f && s.StunRemaining <= 0f && !s.Airborne,
                JumpHeight = s.Airborne ? run.HeightToWorld(s.Height) : 0f,
```

  6b. En `OnStepped`, reemplazar el bloque de `Crashed` por:

```csharp
            // Caerse de un salto pega como un choque.
            if ((events & (RunEvent.Crashed | RunEvent.Fell)) != 0)
            {
                director.AddTrauma(config.crashTrauma);
                director.Punch(config.crashPunchForward, config.crashPunchZoom);
                Hitstop(config.hitstopSeconds);
            }

            if ((events & RunEvent.LandedPerfect) != 0)
            {
                director.AddTrauma(config.perfectLandingTrauma);
                director.Punch(config.perfectLandingPunchForward, 0f);
            }
```

- [ ] **Step 7: Compilar y correr la suite** — Expected: `pass=101 fail=0`.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Zombineta/Scripts/CameraFx Assets/_Zombineta/Scripts/Core/CameraFollow.cs Assets/_Zombineta/Scripts/Core/RunController.cs Assets/_Zombineta/Tests/EditMode/CameraDirectorTests.cs
git commit -m "feat: la camara se abre para acompanar el salto"
```

---

### Task 4: Se ve el salto (moto, sombra, rampas)

**Files:**
- Modify: `Assets/_Zombineta/Scripts/Player/ScooterView.cs`
- Modify: `Assets/_Zombineta/Scripts/Level/LevelSpawner.cs`
- Create (generados por MCP): `Assets/_Zombineta/Art/Level/rampa_placeholder.png`,
  `Assets/_Zombineta/Art/Level/sombra.png`
- Modify (MCP): `Assets/_Zombineta/Scenes/Prototipo.unity`

**Interfaces:**
- Consumes: `RunController.HeightToWorld`, `RunState.Airborne/Height/Pitch/Fallen`,
  `GameConfig.perfectLandingAngle`, `LevelEntryKind.Ramp`, `LevelEntry.height`.

- [ ] **Step 1: `ScooterView`** — agregar campos después de `arrivalCoastMeters`:

```csharp
        [Header("Salto")]
        [Tooltip("Sombra en el carril mientras vuela: marca donde va a caer.")]
        [SerializeField] SpriteRenderer shadow;
        [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.45f);
        [Tooltip("La sombra toma este color si la inclinacion daria aterrizaje perfecto.")]
        [SerializeField] Color shadowPerfectColor = new Color(0.3f, 1f, 0.4f, 0.65f);
        [Tooltip("Grados de la moto tirada en el piso tras una caida.")]
        [SerializeField] float fallenAngle = 70f;

        Vector3 shadowBaseScale = Vector3.one;

        void Awake()
        {
            if (shadow != null)
                shadowBaseScale = shadow.transform.localScale;
        }
```

  y reemplazar el bloque de posición (`transform.position = ...;`) por:

```csharp
            // El pivot del sprite esta en el contacto de las ruedas: la moto se apoya en la
            // linea del carril, igual que los pies de los zombies. En el aire, sube.
            float laneY = run.LaneToWorldY(state.LaneVisual);
            float lift = state.Airborne ? run.HeightToWorld(state.Height) : 0f;
            transform.position = new Vector3(run.ToWorldX(state.PlayerX + coast), laneY + lift, 0f);

            // Rota sobre las ruedas: la inclinacion del salto, o tirada tras una caida.
            float angle = state.Airborne ? state.Pitch : state.Fallen ? fallenAngle : 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            UpdateShadow(state, laneY);
```

  y agregar el método:

```csharp
        void UpdateShadow(RunState state, float laneY)
        {
            if (shadow == null)
                return;

            if (shadow.enabled != state.Airborne)
                shadow.enabled = state.Airborne;
            if (!state.Airborne)
                return;

            // Queda en el carril, derecha aunque la moto este inclinada.
            shadow.transform.SetPositionAndRotation(
                new Vector3(transform.position.x, laneY, 0f), Quaternion.identity);

            // Mas alto, mas chica: se lee la altura sin mirar la moto.
            float k = Mathf.Clamp01(run.HeightToWorld(state.Height) / 4f);
            shadow.transform.localScale = shadowBaseScale * Mathf.Lerp(1f, 0.55f, k);

            bool perfect = Mathf.Abs(state.Pitch) <= run.Config.perfectLandingAngle;
            shadow.color = perfect ? shadowPerfectColor : shadowColor;
        }
```

- [ ] **Step 2: `LevelSpawner`** — agregar campos:

```csharp
        [Header("Rampa")]
        [Tooltip("Cuna con el pivot abajo a la derecha: el borde alto es donde lanza.")]
        [SerializeField] Sprite rampSprite;
        [SerializeField] Color rampColor = new Color(0.85f, 0.45f, 0.2f);

        Sprite defaultSprite;
```

  en `Start()`, después del chequeo de null: `defaultSprite = itemPrefab.sprite;`

  dentro del loop, reemplazar las tres líneas de posición/escala/color por:

```csharp
                var kind = item.Entry.kind;
                sr.sprite = kind == LevelEntryKind.Ramp && rampSprite != null ? rampSprite : defaultSprite;
                sr.transform.position = new Vector3(
                    run.ToWorldX(d),
                    run.LaneToWorldY(item.Entry.lane) + run.HeightToWorld(item.Entry.height),
                    0f);
                sr.transform.localScale = ScaleFor(kind);
                sr.color = ColorFor(kind);
```

  en `ColorFor`, agregar `case LevelEntryKind.Ramp: return rampColor;` y reemplazar `ScaleFor`
  por:

```csharp
        // Los obstaculos ocupan el carril; los recursos son chicos y flotan; la rampa ya viene a escala.
        static Vector3 ScaleFor(LevelEntryKind kind)
        {
            switch (kind)
            {
                case LevelEntryKind.Obstacle: return new Vector3(1.1f, 1.2f, 1f);
                case LevelEntryKind.Ramp: return Vector3.one;
                default: return new Vector3(0.55f, 0.55f, 1f);
            }
        }
```

- [ ] **Step 3: Compilar** — `Assets/Refresh`, consola sin `error CS`.

- [ ] **Step 4: Generar los dos placeholders** — `Unity_RunCommand` (fuera de Play):

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string dir = "Assets/_Zombineta/Art/Level";
        Directory.CreateDirectory(dir);

        // Cuna: sube de izquierda a derecha. Blanca: el color lo pone LevelSpawner.
        int w = 160, h = 56;
        var ramp = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                ramp.SetPixel(x, y, y <= (float)x * h / w ? Color.white : new Color(1, 1, 1, 0));
        ramp.Apply();
        File.WriteAllBytes(dir + "/rampa_placeholder.png", ramp.EncodeToPNG());

        // Sombra: elipse de borde suave.
        int sw = 128, sh = 40;
        var sh2 = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
        for (int y = 0; y < sh; y++)
            for (int x = 0; x < sw; x++)
            {
                float dx = (x + 0.5f) / sw * 2f - 1f, dy = (y + 0.5f) / sh * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                sh2.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1f - r) * 3f)));
            }
        sh2.Apply();
        File.WriteAllBytes(dir + "/sombra.png", sh2.EncodeToPNG());
        result.Log("ok");
    }
}
```

  Después `Assets/Refresh`, y en otro comando configurar los importers: `textureType = Sprite`,
  `spriteImportMode = Single`, `spritePixelsToUnits = 100`, `alphaIsTransparency = true`, pivot
  de la rampa `(1, 0)` (`spriteAlignment = Custom`, `spritePivot = new Vector2(1, 0)`), pivot de la
  sombra centrado; `SaveAndReimport()`. Queda una cuña de 1,6 × 0,56 u y una sombra de 1,28 × 0,4 u.

- [ ] **Step 5: Cablear la escena** — `Unity_RunCommand` (fuera de Play):
  - Crear `Scooter/Shadow` con `SpriteRenderer` (sprite `sombra`, `sortingOrder` uno menos
    que el de `Scooter/Body`, deshabilitado), escala local `(1, 1, 1)`.
  - `ScooterView.shadow` = ese renderer (vía `SerializedObject`).
  - `LevelSpawner.rampSprite` = el sprite `rampa_placeholder`.
  - Guardar la escena (`EditorSceneManager.SaveScene`).

- [ ] **Step 6: Verificar en Play** — entrar en Play, `Application.runInBackground = true`,
  y **en un mismo comando**: arrancar un nivel por la API de `ScreenFlow.Flow` (Play,
  ChooseCharacter(0), CinematicFinished) y colgar un callback de `EditorApplication.update` que,
  pasado 1 s, llame `run.Sim.Launch()` (las rutas todavía no tienen rampas) y pause el editor
  (`EditorApplication.isPaused = true`) cuando `Height` pase de 1,5 m. Después capturar con
  `Unity_Camera_Capture`. Expected: moto elevada e inclinada, sombra en el carril, plano
  abierto. Salir de Play.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Zombineta/Scripts/Player/ScooterView.cs Assets/_Zombineta/Scripts/Level/LevelSpawner.cs Assets/_Zombineta/Art/Level Assets/_Zombineta/Art/Level.meta Assets/_Zombineta/Scenes/Prototipo.unity
git commit -m "feat: la moto vuela, rota y proyecta sombra; rampas placeholder"
```

---

### Task 5: Piezas de rampa en Ruta01 y Ruta02

**Files:**
- Modify (MCP): `Assets/_Zombineta/Settings/Ruta01.asset`, `Ruta02.asset`

- [ ] **Step 1: Insertar las piezas** — `Unity_RunCommand` (fuera de Play), una ruta por vez,
  semillas fijas (Ruta01 → 1201, Ruta02 → 1202). Reglas de la pieza, con `d` la distancia de
  la rampa:
  - `d` = 300, 550, 800, … mientras `d ≤ goalDistance − 150` (con `goalDistance` del
    `GameConfig.asset`).
  - Carril `L` al azar (semilla). En `L`: borrar obstáculos en `[d − 3, d + 50]` y pickups en
    `[d − 3, d + 12]` (quedarían bajo el salto).
  - Agregar `Ramp` en `(d, L)`, `Obstacle` en `(d + 4, L)` y `(d + 8, L)`.
  - Una rampa sí y otra no: `Fuel` aéreo en `(d + 17, L, height 6)` (pico del salto en turbo).
  - Para cada obstáculo nuevo, si los otros dos carriles tienen obstáculos a ±3 m, borrar el de
    uno de ellos (semilla).
  - `SortByDistance()`, `EditorUtility.SetDirty`, `AssetDatabase.SaveAssets()`.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LD = global::Zombineta.Level.LevelDefinition;
using LE = global::Zombineta.Level.LevelEntry;
using LK = global::Zombineta.Level.LevelEntryKind;
using GC = global::Zombineta.Core.GameConfig;
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var cfg = AssetDatabase.LoadAssetAtPath<GC>("Assets/_Zombineta/Settings/GameConfig.asset");
        Place("Assets/_Zombineta/Settings/Ruta01.asset", 1201, cfg.goalDistance, result);
        Place("Assets/_Zombineta/Settings/Ruta02.asset", 1202, cfg.goalDistance, result);
        AssetDatabase.SaveAssets();
    }

    static void Place(string path, int seed, float goal, ExecutionResult result)
    {
        var def = AssetDatabase.LoadAssetAtPath<LD>(path);
        var e = def.entries;
        foreach (var x in e)
            if (x.kind == LK.Ramp) { result.LogWarning(path + " ya tiene rampas: no se toca"); return; }

        var rng = new System.Random(seed);
        int ramps = 0;
        for (float d = 300f; d <= goal - 150f; d += 250f)
        {
            int L = rng.Next(0, 3);
            e.RemoveAll(x => x.lane == L &&
                ((x.kind == LK.Obstacle && x.distance >= d - 3f && x.distance <= d + 50f) ||
                 (x.kind != LK.Obstacle && x.distance >= d - 3f && x.distance <= d + 12f)));

            e.Add(new LE(d, L, LK.Ramp));
            foreach (float o in new[] { d + 4f, d + 8f })
            {
                e.Add(new LE(o, L, LK.Obstacle));
                int a = (L + 1) % 3, b = (L + 2) % 3;
                bool blockedA = e.Exists(x => x.kind == LK.Obstacle && x.lane == a && Mathf.Abs(x.distance - o) <= 3f);
                bool blockedB = e.Exists(x => x.kind == LK.Obstacle && x.lane == b && Mathf.Abs(x.distance - o) <= 3f);
                if (blockedA && blockedB)
                {
                    int free = rng.Next(0, 2) == 0 ? a : b;
                    e.RemoveAll(x => x.kind == LK.Obstacle && x.lane == free && Mathf.Abs(x.distance - o) <= 3f);
                }
            }
            if (ramps % 2 == 0)
                e.Add(new LE(d + 17f, L, LK.Fuel, 6f));
            ramps++;
        }

        def.SortByDistance();
        EditorUtility.SetDirty(def);
        result.Log(path + ": " + ramps + " rampas, " + e.Count + " entradas");
    }
}
```

- [ ] **Step 2: Verificar la regla de los tres carriles** — `Unity_RunCommand` que cargue cada
  ruta y, para cada obstáculo, cuente los carriles con obstáculo a ±2 m. Expected: ningún punto
  con 3 carriles bloqueados; reportar cantidad de rampas y de pickups aéreos.

- [ ] **Step 3: Verificar un salto real en Play** — en un solo `RunCommand`: arrancar el nivel,
  poner `run.Sim.State.PlayerX` a 5 m antes de la primera rampa y `Lane`/`LaneVisual` en su
  carril, y colgar un callback de `EditorApplication.update` que registre en `SessionState` los
  eventos de `run.Stepped` (Launched / Landed / Fell / Crashed) durante 3 s. Leerlo en otro
  comando. Expected: `Launched` y después `Landed` (el input real es Normal, sin inclinar: salto
  sano), sin `Crashed` hasta pasar `d + 8`. Salir de Play.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Zombineta/Settings/Ruta01.asset Assets/_Zombineta/Settings/Ruta02.asset
git commit -m "content: piezas de rampa en Ruta01 y Ruta02"
```

---

### Task 6: Handoff

**Files:**
- Modify: `HANDOFF.md`

- [ ] **Step 1:** Actualizar:
  - Estado y conteo de tests.
  - Verbos: rampa, A/D en el aire.
  - "Fuera del alcance": sacar rampas y salto.
  - Arquitectura: sección "Salto" con las reglas, la convención de inclinación y que
    `BalanceProbe` no simula saltos.
  - Qué está verificado y qué no (el *feel* del salto con teclado no está probado).
  - Qué sigue: arte de rampa y animación de caída; la moto se atraviesa con la cuña al
    subirla porque la simulación lanza en el borde alto.

- [ ] **Step 2: Commit**

```bash
git add HANDOFF.md
git commit -m "docs: handoff con rampas y salto"
```
