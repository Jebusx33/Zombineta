# Horda de individuos Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** La horda pasa de ser un número a ser 24 zombies simulados con tipo propio; la bala
traza una línea por el carril y mata (o no) a quien encuentre; hay barriles que explotan de un
tiro y zombies que se arrollan de frente, cada muerte con sus partículas.

**Architecture:** Toda la horda vive en `HordeSimulation` (C# plano, testeable), contenida por
`RunSimulation`, que sincroniza `State.HordeX` con el frente. Los barriles los conoce el
recorrido y se los pasa a la simulación por una interfaz chica (`IBarrelField`). Las vistas y
los efectos solo leen la lista de eventos que la horda emite por tick.

**Tech Stack:** Unity 6000.6.0f1, 2D URP, NUnit (Test Runner EditMode), MCP nativo de Unity.

Spec: `docs/superpowers/specs/2026-09-11-horda-de-individuos-design.md`.

## Global Constraints

- Raíz: `D:\Jose\Facu\Taller de proyecto integral\Prototipo\Zombineta\Zombineta`. Rama `Jose`.
  Commits terminan con `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`.
- La simulación no conoce Unity más allá de `Mathf` y `ScriptableObject`: nada de MonoBehaviour
  en `Core/` ni en `HordeSimulation`.
- Comentarios en castellano sin tildes, como el resto del proyecto.
- Aleatoriedad **siempre** por `System.Random` con semilla de config: dos playtests iguales.
- **Compilar:** tras editar `.cs`, `Unity_ManageMenuItem` Action=Execute MenuPath=`Assets/Refresh`,
  esperar ~15 s y revisar `Unity_ReadConsole` con `Types: ["All"]` y `FilterText: "error CS"`
  (los errores de compilación llegan como tipo `Log`).
- **Correr tests:** `Unity_RunCommand` con el lanzador del `TestRunnerApi` que guarda el
  resultado en `SessionState["zomb.tests"]`, y leerlo en un comando aparte
  (`SessionState.GetString("zomb.tests")`). El detalle está en el plan anterior
  (`docs/superpowers/plans/2026-09-10-rampas-y-salto.md`, sección "Global Constraints").
- **Suite actual: 101 tests en verde.** Este plan agrega 24 y reescribe 1.
- No editar scripts con Unity en Play Mode.

## Archivos

| Archivo | Cambio |
|---|---|
| `Assets/_Zombineta/Scripts/Enemies/ZombieType.cs` | Nuevo: `ZombieType` + `ZombieRoster` |
| `Assets/_Zombineta/Scripts/Enemies/HordeSimulation.cs` | Nuevo: la masa, C# plano |
| `Assets/_Zombineta/Scripts/Core/GameConfig.cs` | Sección "Horda"; se va `shotHordePushback` |
| `Assets/_Zombineta/Scripts/Core/RunSimulation.cs` | Contiene la horda, disparo, `RunOver`, `Explode` |
| `Assets/_Zombineta/Scripts/Core/IBarrelField.cs` | Nuevo: la interfaz de los barriles |
| `Assets/_Zombineta/Scripts/Level/LevelDefinition.cs` | `Barrel`, `ZombieFront`, campo `variant` |
| `Assets/_Zombineta/Scripts/Level/LevelRuntime.cs` | Implementa `IBarrelField`; arrollar; cadena |
| `Assets/_Zombineta/Scripts/Level/LevelSpawner.cs` | Dibuja barriles y zombies de frente |
| `Assets/_Zombineta/Scripts/Enemies/HordeView.cs` | Reescrita: una unidad por zombie |
| `Assets/_Zombineta/Scripts/Fx/FxManager.cs` | Nuevo: partículas, traza y manchas |
| `Assets/_Zombineta/Scripts/Core/GameSettings.cs` | `Gore` |
| `Assets/_Zombineta/Scripts/UI/ScreenFlow.cs` | Opción "Sangre" |
| `Assets/_Zombineta/Scripts/CameraFx/CameraConfig.cs` + `Core/CameraFollow.cs` | Sacudida de explosión y de atropello |
| `Assets/_Zombineta/Tests/EditMode/HordeSimulationTests.cs` | Nuevo (8) |
| `Assets/_Zombineta/Tests/EditMode/ShootingTests.cs` | Nuevo (8) |
| `Assets/_Zombineta/Tests/EditMode/BarrelsAndRamTests.cs` | Nuevo (8) |
| `Assets/_Zombineta/Tests/EditMode/RunSimulationTests.cs` | Reescribe el test del empujón fijo |
| `Assets/_Zombineta/Settings/Zombies.asset` | Nuevo: los tres tipos |
| `Assets/_Zombineta/Settings/Ruta01.asset`, `Ruta02.asset` | Barriles y zombies de frente |
| `Assets/_Zombineta/Art/Fx/*`, `Assets/_Zombineta/Prefabs/Fx*` | Partícula, materiales, prefabs |
| `Assets/_Zombineta/Scenes/Prototipo.unity` | FxManager, traza, menú de Opciones |
| `HANDOFF.md` | Sección de la horda |

---

### Task 1: Tipos de zombie y la masa simulada

**Files:**
- Create: `Assets/_Zombineta/Scripts/Enemies/ZombieType.cs`
- Create: `Assets/_Zombineta/Scripts/Enemies/HordeSimulation.cs`
- Modify: `Assets/_Zombineta/Scripts/Core/GameConfig.cs`
- Test: `Assets/_Zombineta/Tests/EditMode/HordeSimulationTests.cs`

**Interfaces:**
- Produces: `ZombieType` (campos `name, speedMultiplier, deathChance, staggerOnHit, ramStun,
  ramFuelPenalty, spawnWeight, tint, scale`), `ZombieRoster : ScriptableObject` con
  `List<ZombieType> types`, `Get(int) : ZombieType`, `Pick(System.Random) : int`,
  `ZombieRoster.Default : ZombieType`; `ZombieUnit` (campos `Type, X, Lane, Stagger, Alive,
  CorpseLeft, Cause, Generation`), `DeathCause { None, Bullet, Explosion, RunOver }`,
  `HordeEventKind { Tracer, Impact, Death, Explosion, Miss }`,
  `HordeEvent { Kind, FromX, X, Lane, Type, Cause }`,
  `HordeSimulation(GameConfig, ZombieRoster, int seed)` con `Units : ZombieUnit[]`,
  `Events : IReadOnlyList<HordeEvent>`, `Epoch : int`, `FrontX : float`,
  `FrontSpeed : float`, `Type(int) : ZombieType`, `Reset(float front)`,
  `Step(float dt, float speedFactor)`; campos nuevos de `GameConfig`: `zombies, hordeCount,
  hordeDepthMeters, corpseSeconds, hordeSeed, shotRangeMeters, deathScareRadius,
  deathScareSeconds, explosionRadius, explosionScareRadius, explosionScareSeconds`.

- [ ] **Step 1: Escribir los tests que fallan** — crear `HordeSimulationTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;

namespace Zombineta.Tests
{
    /// <summary>
    /// La masa con numeros propios del test. El roster tiene tres tipos claros: uno normal,
    /// uno rapido y uno lento, para que las cuentas se lean de un vistazo.
    /// </summary>
    public class HordeSimulationTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.startingGap = 50f;
            c.hordeBaseSpeed = 10f;
            c.hordeCount = 12;
            c.hordeDepthMeters = 10f;
            c.corpseSeconds = 1f;
            c.hordeSeed = 7;
            c.shotRangeMeters = 60f;
            c.deathScareRadius = 4f;
            c.deathScareSeconds = 0.5f;
            c.explosionRadius = 8f;
            c.explosionScareRadius = 16f;
            c.explosionScareSeconds = 1f;
            return c;
        }

        static ZombieRoster MakeRoster()
        {
            var r = ScriptableObject.CreateInstance<ZombieRoster>();
            r.types.Add(new ZombieType { name = "Comun", speedMultiplier = 1f, deathChance = 1f, spawnWeight = 1f });
            r.types.Add(new ZombieType { name = "Corredor", speedMultiplier = 2f, deathChance = 1f, spawnWeight = 1f });
            r.types.Add(new ZombieType { name = "Pesado", speedMultiplier = 0.5f, deathChance = 0f, staggerOnHit = 0.4f, spawnWeight = 1f });
            return r;
        }

        static HordeSimulation Make(out GameConfig cfg)
        {
            cfg = MakeConfig();
            var h = new HordeSimulation(cfg, MakeRoster(), cfg.hordeSeed);
            h.Reset(-cfg.startingGap);
            return h;
        }

        /// <summary>Deja un solo zombie vivo, del tipo pedido, en la posicion y carril pedidos.</summary>
        static ZombieUnit Solo(HordeSimulation h, int type, float x, int lane)
        {
            for (int i = 1; i < h.Units.Length; i++)
                h.Kill(h.Units[i], DeathCause.Bullet);
            var u = h.Units[0];
            u.Type = type; u.X = x; u.Lane = lane; u.Alive = true; u.Stagger = 0f; u.CorpseLeft = 0f;
            return u;
        }

        [Test]
        public void Reset_PutsTheFrontExactlyAtTheStartingGap()
        {
            var h = Make(out var cfg);

            Assert.AreEqual(-cfg.startingGap, h.FrontX, 0.001f);
            Assert.AreEqual(cfg.hordeCount, h.Units.Length);
        }

        [Test]
        public void Front_IsTheFrontmostAliveZombie()
        {
            var h = Make(out _);
            var u = Solo(h, 0, -20f, 1);
            h.Step(Dt, 1f);

            Assert.AreEqual(u.X, h.FrontX, 0.001f, "el unico vivo manda");
        }

        [Test]
        public void EachType_MovesAtItsOwnSpeed()
        {
            var h = Make(out var cfg);
            var runner = Solo(h, 1, -20f, 1);   // x2
            float x0 = runner.X;

            for (int i = 0; i < 60; i++) h.Step(Dt, 1f);

            // 10 m/s base x 2 x 1 s
            Assert.AreEqual(x0 + 20f, runner.X, 0.2f);
        }

        [Test]
        public void SpeedFactor_SlowsEveryone()
        {
            var h = Make(out _);
            var u = Solo(h, 0, -20f, 1);
            float x0 = u.X;

            for (int i = 0; i < 60; i++) h.Step(Dt, 0.5f);

            Assert.AreEqual(x0 + 5f, u.X, 0.2f, "10 m/s a la mitad durante 1 s");
        }

        [Test]
        public void AStaggeredZombie_DoesNotAdvance()
        {
            var h = Make(out _);
            var u = Solo(h, 0, -20f, 1);
            u.Stagger = 0.5f;
            float x0 = u.X;

            for (int i = 0; i < 15; i++) h.Step(Dt, 1f);   // 0,25 s

            Assert.AreEqual(x0, u.X, 0.001f);
        }

        [Test]
        public void ACorpse_ComesBackBehindTheFrontAfterItsTime()
        {
            var h = Make(out var cfg);
            var front = Solo(h, 0, -20f, 1);
            var dead = h.Units[1];
            dead.Alive = true; dead.X = -22f; dead.Lane = 0;
            h.Kill(dead, DeathCause.Bullet);
            int gen = dead.Generation;

            Assert.IsFalse(dead.Alive);
            for (int i = 0; i < 90; i++) h.Step(Dt, 1f);   // 1,5 s > corpseSeconds

            Assert.IsTrue(dead.Alive, "la horda es infinita: vuelve a entrar");
            Assert.AreNotEqual(gen, dead.Generation, "es otro zombie, no el mismo");
            Assert.Less(dead.X, front.X, "reaparece detras del frente");
            Assert.GreaterOrEqual(dead.X, front.X - cfg.hordeDepthMeters * 2f);
        }

        [Test]
        public void AZombieLeftFarBehind_IsRecycled()
        {
            var h = Make(out var cfg);
            var front = Solo(h, 0, 0f, 1);
            var straggler = h.Units[1];
            straggler.Alive = true; straggler.X = -200f; straggler.Lane = 2; straggler.CorpseLeft = 0f;

            h.Step(Dt, 1f);

            Assert.Greater(straggler.X, front.X - cfg.hordeDepthMeters * 3f, "lo reciclaron al fondo de la masa");
        }

        [Test]
        public void Population_StaysConstantAfterManyDeaths()
        {
            var h = Make(out _);
            for (int round = 0; round < 5; round++)
            {
                foreach (var u in h.Units)
                    if (u.Alive) h.Kill(u, DeathCause.Bullet);
                for (int i = 0; i < 120; i++) h.Step(Dt, 1f);
            }

            int alive = 0;
            foreach (var u in h.Units) if (u.Alive) alive++;
            Assert.AreEqual(h.Units.Length, alive, "todos vuelven: la horda no se vacia");
        }
    }
}
```

- [ ] **Step 2: Compilar y ver que falla** — Expected: `error CS` por `ZombieRoster`,
  `HordeSimulation`, `c.hordeCount`, etc.

- [ ] **Step 3: Crear `ZombieType.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Enemies
{
    /// <summary>
    /// Los numeros de una clase de zombie. Balancear la horda es editar estos valores en
    /// Settings/Zombies.asset: nunca tocar codigo.
    /// </summary>
    [Serializable]
    public sealed class ZombieType
    {
        public string name = "Comun";

        [Tooltip("Multiplica la velocidad base de la horda. Mas de 1 = se despega y va de puntero.")]
        public float speedMultiplier = 1f;

        [Range(0f, 1f)]
        [Tooltip("Probabilidad de morir por cada bala. Menos de 1 = a veces encaja el tiro y sigue.")]
        public float deathChance = 0.85f;

        [Tooltip("Segundos que queda frenado si aguanta el tiro.")]
        public float staggerOnHit = 0.6f;

        [Tooltip("Segundos que frena a la moto al arrollarlo de frente.")]
        public float ramStun = 0.1f;

        [Tooltip("Nafta que cuesta arrollarlo de frente.")]
        public float ramFuelPenalty = 0f;

        [Tooltip("Peso relativo con que aparece en la masa.")]
        public float spawnWeight = 60f;

        [Header("Presentacion")]
        public Color tint = Color.white;
        public float scale = 1f;
    }

    /// <summary>La lista de tipos que puede tener la horda.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Zombie Roster", fileName = "Zombies")]
    public sealed class ZombieRoster : ScriptableObject
    {
        public List<ZombieType> types = new List<ZombieType>();

        /// <summary>
        /// El tipo que se usa cuando no hay roster: uno normal que muere siempre. Asi la
        /// simulacion anda sin asset, y los tests que no hablan de tipos siguen valiendo.
        /// </summary>
        public static readonly ZombieType Default =
            new ZombieType { name = "Comun", speedMultiplier = 1f, deathChance = 1f, staggerOnHit = 0f };

        public ZombieType Get(int index) =>
            types != null && index >= 0 && index < types.Count ? types[index] : Default;

        /// <summary>Elige un tipo segun los pesos, con el random de la horda: reproducible.</summary>
        public int Pick(System.Random rng)
        {
            if (types == null || types.Count == 0)
                return 0;

            float total = 0f;
            for (int i = 0; i < types.Count; i++)
                total += Mathf.Max(0f, types[i].spawnWeight);

            if (total <= 0f)
                return rng.Next(types.Count);

            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < types.Count; i++)
            {
                r -= Mathf.Max(0f, types[i].spawnWeight);
                if (r <= 0f)
                    return i;
            }
            return types.Count - 1;
        }
    }
}
```

- [ ] **Step 4: Crear `HordeSimulation.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Enemies
{
    public enum DeathCause { None, Bullet, Explosion, RunOver }

    public enum HordeEventKind { Tracer, Impact, Death, Explosion, Miss }

    /// <summary>Algo que paso en la horda este tick. Las vistas leen esto para VFX y SFX.</summary>
    public struct HordeEvent
    {
        public HordeEventKind Kind;
        public float FromX;   // metros: de donde salio el disparo (solo Tracer)
        public float X;       // metros
        public int Lane;
        public int Type;
        public DeathCause Cause;
    }

    /// <summary>Un zombie de la masa. Se recicla: nunca se crea ni se destruye en partida.</summary>
    public sealed class ZombieUnit
    {
        public int Type;
        public float X;
        public int Lane;

        /// <summary>Segundos que queda frenado (impacto que aguanto, o susto).</summary>
        public float Stagger;

        public bool Alive;

        /// <summary>Segundos que le quedan al cadaver antes de volver a entrar.</summary>
        public float CorpseLeft;

        public DeathCause Cause;

        /// <summary>Cambia al reciclarse: la vista sabe que es otro zombie y reinicia su animacion.</summary>
        public int Generation;
    }

    /// <summary>
    /// La horda como individuos. C# plano: se instancia, se le hace Step y se verifica, sin
    /// abrir una escena. El frente (lo que te alcanza) es el vivo mas adelantado, asi que matar
    /// al puntero abre distancia sin ningun empujon artificial.
    /// </summary>
    public sealed class HordeSimulation
    {
        readonly GameConfig config;
        readonly ZombieRoster roster;
        readonly System.Random rng;
        readonly List<HordeEvent> events = new List<HordeEvent>();

        float frontX;

        public HordeSimulation(GameConfig config, ZombieRoster roster, int seed)
        {
            this.config = config;
            this.roster = roster;
            rng = new System.Random(seed);

            int count = Mathf.Max(1, config.hordeCount);
            Units = new ZombieUnit[count];
            for (int i = 0; i < count; i++)
                Units[i] = new ZombieUnit();
        }

        public ZombieUnit[] Units { get; }

        /// <summary>Lo que paso en el ultimo Step. Se limpia al empezar cada uno.</summary>
        public IReadOnlyList<HordeEvent> Events => events;

        /// <summary>Sube en cada Step: la vista sabe si los eventos son nuevos o los de antes.</summary>
        public int Epoch { get; private set; }

        /// <summary>X del vivo mas adelantado: el que te puede alcanzar.</summary>
        public float FrontX => frontX;

        /// <summary>Velocidad efectiva del frente en el ultimo Step (m/s). Para el HUD de debug.</summary>
        public float FrontSpeed { get; private set; }

        public ZombieType Type(int index) =>
            roster != null ? roster.Get(index) : ZombieRoster.Default;

        public void Reset(float front)
        {
            frontX = front;
            FrontSpeed = 0f;
            events.Clear();

            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];
                u.Alive = true;
                u.Stagger = 0f;
                u.CorpseLeft = 0f;
                u.Cause = DeathCause.None;
                u.Type = Pick();
                u.Lane = rng.Next(RunSimulation.LaneCount);
                // El primero justo en el frente: la ventaja inicial es exactamente startingGap.
                u.X = i == 0 ? front : front - (float)rng.NextDouble() * config.hordeDepthMeters;
                u.Generation++;
            }
        }

        public void Step(float dt, float speedFactor)
        {
            events.Clear();
            Epoch++;

            float previousFront = frontX;
            float front = float.NegativeInfinity;
            float frontSpeed = 0f;

            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];

                if (!u.Alive)
                {
                    u.CorpseLeft -= dt;
                    if (u.CorpseLeft > 0f)
                        continue;
                    Recycle(u, previousFront);
                }
                else if (u.X < previousFront - config.hordeDepthMeters * 2f)
                {
                    // Se descolgo de la masa: vuelve a entrar por el fondo.
                    Recycle(u, previousFront);
                }

                float speed = 0f;
                if (u.Stagger > 0f)
                    u.Stagger = Mathf.Max(0f, u.Stagger - dt);
                else
                {
                    speed = config.hordeBaseSpeed * Type(u.Type).speedMultiplier * speedFactor;
                    u.X += speed * dt;
                }

                if (u.X > front)
                {
                    front = u.X;
                    frontSpeed = speed;
                }
            }

            if (front > float.NegativeInfinity)
                frontX = front;
            FrontSpeed = frontSpeed;
        }

        // --- Acciones --------------------------------------------------------

        /// <summary>El vivo mas cercano hacia atras en ese carril, o null si no hay nadie.</summary>
        public ZombieUnit NearestBehind(float fromX, int lane, float range)
        {
            ZombieUnit best = null;
            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];
                if (!u.Alive || u.Lane != lane)
                    continue;
                if (u.X > fromX || u.X < fromX - range)
                    continue;
                if (best == null || u.X > best.X)
                    best = u;
            }
            return best;
        }

        /// <summary>
        /// Le pega a un zombie: muere segun su tipo, o encaja el impacto y queda frenado.
        /// Devuelve true si murio.
        /// </summary>
        public bool HitZombie(ZombieUnit u, float fromX)
        {
            var t = Type(u.Type);
            events.Add(new HordeEvent
            {
                Kind = HordeEventKind.Tracer, FromX = fromX, X = u.X, Lane = u.Lane, Type = u.Type,
            });

            if ((float)rng.NextDouble() <= t.deathChance)
            {
                Kill(u, DeathCause.Bullet);
                return true;
            }

            events.Add(new HordeEvent
            {
                Kind = HordeEventKind.Impact, X = u.X, Lane = u.Lane, Type = u.Type,
            });
            u.Stagger = Mathf.Max(u.Stagger, t.staggerOnHit);
            return false;
        }

        /// <summary>La bala no encontro nada: traza hasta el final del alcance.</summary>
        public void Miss(float fromX, int lane, float range)
        {
            float to = fromX - range;
            events.Add(new HordeEvent { Kind = HordeEventKind.Tracer, FromX = fromX, X = to, Lane = lane });
            events.Add(new HordeEvent { Kind = HordeEventKind.Miss, X = to, Lane = lane });
        }

        /// <summary>Traza sin victima: la bala pego en otra cosa (un barril).</summary>
        public void Tracer(float fromX, float toX, int lane)
        {
            events.Add(new HordeEvent { Kind = HordeEventKind.Tracer, FromX = fromX, X = toX, Lane = lane });
        }

        public void Kill(ZombieUnit u, DeathCause cause)
        {
            if (!u.Alive)
                return;

            u.Alive = false;
            u.Cause = cause;
            u.CorpseLeft = config.corpseSeconds;
            u.Stagger = 0f;
            events.Add(new HordeEvent
            {
                Kind = HordeEventKind.Death, X = u.X, Lane = u.Lane, Type = u.Type, Cause = cause,
            });

            // Los de al lado se asustan: un buen tiro se siente aunque el frente se mueva poco.
            Scare(u.X, config.deathScareRadius, config.deathScareSeconds);
            RecomputeFront();
        }

        /// <summary>Explosion de un barril: mata en radio en los tres carriles. Devuelve cuantos.</summary>
        public int Explode(float x, int lane)
        {
            events.Add(new HordeEvent { Kind = HordeEventKind.Explosion, X = x, Lane = lane });

            int killed = 0;
            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];
                if (u.Alive && Mathf.Abs(u.X - x) <= config.explosionRadius)
                {
                    Kill(u, DeathCause.Explosion);
                    killed++;
                }
            }

            Scare(x, config.explosionScareRadius, config.explosionScareSeconds);
            return killed;
        }

        /// <summary>Frena un momento a los vivos que esten cerca.</summary>
        public void Scare(float x, float radius, float seconds)
        {
            if (seconds <= 0f)
                return;
            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];
                if (u.Alive && Mathf.Abs(u.X - x) <= radius)
                    u.Stagger = Mathf.Max(u.Stagger, seconds);
            }
        }

        /// <summary>
        /// Un zombie de frente arrollado por la moto. No es parte de la masa (vive en el
        /// recorrido): aca solo se registra el evento para que los efectos lo dibujen.
        /// </summary>
        public void ReportRunOver(float x, int lane, int type)
        {
            events.Add(new HordeEvent
            {
                Kind = HordeEventKind.Death, X = x, Lane = lane, Type = type, Cause = DeathCause.RunOver,
            });
        }

        // --- Interno ---------------------------------------------------------

        int Pick() => roster != null ? roster.Pick(rng) : 0;

        void Recycle(ZombieUnit u, float front)
        {
            u.Alive = true;
            u.Cause = DeathCause.None;
            u.CorpseLeft = 0f;
            u.Stagger = 0f;
            u.Type = Pick();
            u.Lane = rng.Next(RunSimulation.LaneCount);
            // Reaparece al fondo de la masa: matar compra espacio, no vacia la horda.
            u.X = front - config.hordeDepthMeters * (0.6f + 0.4f * (float)rng.NextDouble());
            u.Generation++;
        }

        void RecomputeFront()
        {
            float front = float.NegativeInfinity;
            for (int i = 0; i < Units.Length; i++)
                if (Units[i].Alive && Units[i].X > front)
                    front = Units[i].X;

            // Si murieron todos, el frente queda donde estaba: los que vuelven entran detras.
            if (front > float.NegativeInfinity)
                frontX = front;
        }
    }
}
```

- [ ] **Step 5: Agregar la sección "Horda" a `GameConfig`** — reemplazar el bloque
  `[Header("Horda")]` actual por:

```csharp
        [Header("Horda")]
        [Tooltip("Los tipos de zombie que pueden aparecer. Sin asset, todos son comunes.")]
        public Zombineta.Enemies.ZombieRoster zombies;

        [Tooltip("Cuantos zombies se simulan a la vez. La masa se recicla: nunca se agota.")]
        public int hordeCount = 24;

        [Tooltip("Metros que se extiende la masa hacia atras desde el frente.")]
        public float hordeDepthMeters = 14f;

        [Tooltip("Segundos que queda el cadaver antes de volver a entrar por el fondo.")]
        public float corpseSeconds = 1.2f;

        [Tooltip("Semilla de la horda: los tipos y las muertes son reproducibles entre playtests.")]
        public int hordeSeed = 2026;

        [Tooltip("Apenas mas rapida que el modo Normal: mantener Normal pierde terreno de a poco.")]
        public float hordeBaseSpeed = 12.5f;
        [Tooltip("A partir de esta distancia la horda empieza a acelerar para sostener la tension.")]
        public float rubberBandStartGap = 80f;
        [Tooltip("Rango de distancia sobre el cual la aceleracion llega al maximo.")]
        public float rubberBandRange = 80f;
        [Tooltip("Velocidad extra maxima que puede ganar la horda por goma elastica.")]
        public float rubberBandMaxBonus = 6f;

        [Header("Disparo y explosiones")]
        [Tooltip("Alcance de la bala hacia atras, en metros.")]
        public float shotRangeMeters = 60f;
        [Tooltip("Metros alrededor de una muerte que hacen frenar a los vecinos.")]
        public float deathScareRadius = 4f;
        public float deathScareSeconds = 0.5f;
        [Tooltip("Metros en los que una explosion mata.")]
        public float explosionRadius = 8f;
        [Tooltip("Metros en los que una explosion solo asusta.")]
        public float explosionScareRadius = 16f;
        public float explosionScareSeconds = 1f;
```

  y **borrar** la línea `public float shotHordePushback = 15f;` con su `[Tooltip]` de la
  sección "Pistola".

- [ ] **Step 6: No commitear todavía** — `GameConfig` ya no tiene `shotHordePushback`, así que
  `RunSimulation.cs` y `RunSimulationTests.cs` no compilan hasta la Task 2. Las dos tasks
  comparten un único estado compilable: seguir directo con la Task 2 y correr la suite al
  final de ella.

---

### Task 2: El disparo y la horda dentro de la simulación

**Files:**
- Modify: `Assets/_Zombineta/Scripts/Core/RunSimulation.cs`
- Create: `Assets/_Zombineta/Scripts/Core/IBarrelField.cs`
- Modify: `Assets/_Zombineta/Tests/EditMode/RunSimulationTests.cs`
- Test: `Assets/_Zombineta/Tests/EditMode/ShootingTests.cs`

**Interfaces:**
- Consumes: todo lo que produce la Task 1.
- Produces: `RunSimulation.Horde : HordeSimulation`, `RunSimulation.Config : GameConfig`,
  `RunSimulation.Barrels : IBarrelField`, `RunSimulation.Explode(float x, int lane) : RunEvent`,
  `RunSimulation.RunOver(int type, float x, int lane) : RunEvent`, eventos nuevos
  `RunEvent.ShotMissed`, `RunEvent.RanOver`, `RunEvent.Explosion`;
  `IBarrelField { bool TryNearestBarrel(float fromX, int lane, float range, out float x, out int index);
  void Detonate(int index, RunSimulation sim); }`.

- [ ] **Step 1: Escribir los tests que fallan** — crear `ShootingTests.cs`:

```csharp
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
```

- [ ] **Step 2: Reescribir el test del empujón fijo** — en
  `Assets/_Zombineta/Tests/EditMode/RunSimulationTests.cs`, borrar de `MakeConfig()` la línea
  `c.shotHordePushback = 20f;`, agregarle `c.hordeCount = 8;` y `c.hordeSeed = 5;`, y
  reemplazar el test `Fire_PushesTheHordeBackAndSpendsAmmo` por:

```csharp
        [Test]
        public void Fire_KillsTheLeaderAndSpendsAmmo()
        {
            var sim = new RunSimulation(MakeConfig());
            // Un solo zombie, en el carril de la jugadora, bien adelante de la masa.
            foreach (var u in sim.Horde.Units) u.Alive = false;
            var leader = sim.Horde.Units[0];
            leader.Alive = true; leader.X = sim.State.PlayerX - 10f; leader.Lane = sim.State.Lane;
            float gapBefore = sim.State.Gap;

            var events = sim.Tick(new PlayerIntent { Mode = DriveMode.Normal, Fire = true }, Dt);

            Assert.AreNotEqual(RunEvent.None, events & RunEvent.Shot);
            Assert.AreEqual(2, sim.State.Ammo);
            Assert.IsFalse(leader.Alive, "sin roster, un tiro mata");
            Assert.Greater(sim.State.Gap, gapBefore, "el frente quedo mas atras");
        }
```

- [ ] **Step 3: Crear `IBarrelField.cs`**

```csharp
namespace Zombineta.Core
{
    /// <summary>
    /// Lo que la simulacion necesita saber de los barriles, que en realidad son parte del
    /// recorrido. Lo implementa LevelRuntime; en los tests se reemplaza por un doble.
    /// </summary>
    public interface IBarrelField
    {
        /// <summary>El barril mas cercano hacia atras en ese carril, si hay alguno sin explotar.</summary>
        bool TryNearestBarrel(float fromX, int lane, float range, out float x, out int index);

        /// <summary>Lo hace explotar (y encadena los que tenga cerca).</summary>
        void Detonate(int index, RunSimulation sim);
    }
}
```

- [ ] **Step 4: Cambiar `RunSimulation`**

  4a. En `RunEvent`, después de `Fell = 1 << 14,`:

```csharp
        ShotMissed = 1 << 15,
        RanOver = 1 << 16,
        Explosion = 1 << 17,
```

  4b. Agregar, junto a `public RunState State { get; }`:

```csharp
        /// <summary>La horda como individuos. El frente sale de aca.</summary>
        public HordeSimulation Horde { get; }

        public GameConfig Config => config;

        /// <summary>Los barriles del recorrido. Lo setea RunController; en tests puede ser null.</summary>
        public IBarrelField Barrels { get; set; }
```

  y en el constructor, **antes** de `Reset()`:

```csharp
            Horde = new HordeSimulation(config, config.zombies, config.hordeSeed);
```

  (requiere `using Zombineta.Enemies;` arriba del archivo).

  4c. En `Reset()`, reemplazar `State.HordeX = -config.startingGap;` por:

```csharp
            Horde.Reset(-config.startingGap);
            State.HordeX = Horde.FrontX;
```

  4d. En `Tick`, reemplazar las dos líneas

```csharp
            HordeSpeed = ComputeHordeSpeed();
            State.HordeX += HordeSpeed * dt;
```

  por:

```csharp
            Horde.Step(dt, ComputeHordeSpeedFactor());
            State.HordeX = Horde.FrontX;
            HordeSpeed = Horde.FrontSpeed;
```

  4e. Reemplazar `ComputeHordeSpeed()` entera por:

```csharp
        /// <summary>
        /// Cuanto se multiplica la velocidad base de cada zombie. Cada tipo le suma lo suyo.
        /// </summary>
        float ComputeHordeSpeedFactor()
        {
            float speed = config.hordeBaseSpeed;

            // Goma elastica: si te escapaste mucho, la horda aprieta.
            if (config.rubberBandRange > 0f)
            {
                float t = Mathf.Clamp01(
                    (State.Gap - config.rubberBandStartGap) / config.rubberBandRange);
                speed += t * config.rubberBandMaxBonus;
            }

            // El faro frena a la horda mientras este prendido: efecto sostenido.
            if (State.HeadlightOn)
                speed *= config.headlightHordeSlowFactor;

            return config.hordeBaseSpeed <= 0f ? 0f : speed / config.hordeBaseSpeed;
        }
```

  4f. Reemplazar `ApplyFire` entera por:

```csharp
        RunEvent ApplyFire(bool fire)
        {
            if (!fire)
                return RunEvent.None;

            if (State.Ammo <= 0)
                return RunEvent.ShotDenied;

            State.Ammo--;

            float from = State.PlayerX;
            int lane = State.Lane;
            float range = config.shotRangeMeters;

            var zombie = Horde.NearestBehind(from, lane, range);
            bool hasBarrel = Barrels != null &&
                             Barrels.TryNearestBarrel(from, lane, range, out float barrelX, out int barrelIndex);

            // Pega en lo primero que encuentra hacia atras: el barril o el zombie.
            if (hasBarrel && (zombie == null || barrelX > zombie.X))
            {
                Horde.Tracer(from, barrelX, lane);
                Barrels.Detonate(barrelIndex, this);
                return RunEvent.Shot | RunEvent.Explosion;
            }

            if (zombie != null)
            {
                Horde.HitZombie(zombie, from);
                State.HordeX = Horde.FrontX;
                return RunEvent.Shot;
            }

            Horde.Miss(from, lane, range);
            return RunEvent.Shot | RunEvent.ShotMissed;
        }
```

  4g. Agregar al final de la clase, después de `IsAtHeight`:

```csharp
        /// <summary>Un barril explota: mata a la horda en el radio. Lo llama LevelRuntime.</summary>
        public RunEvent Explode(float x, int lane)
        {
            Horde.Explode(x, lane);
            State.HordeX = Horde.FrontX;
            return RunEvent.Explosion;
        }

        /// <summary>
        /// La moto arrollo a un zombie que venia de frente. Cuesta lo que diga su tipo: al
        /// comun te lo llevas puesto, el pesado es como chocar un auto.
        /// </summary>
        public RunEvent RunOver(int type, float x, int lane)
        {
            var t = Horde.Type(type);
            if (t.ramStun > 0f)
                State.StunRemaining = Mathf.Max(State.StunRemaining, t.ramStun);
            if (t.ramFuelPenalty > 0f)
                State.Fuel = Mathf.Max(0f, State.Fuel - t.ramFuelPenalty);

            Horde.ReportRunOver(x, lane, type);
            return RunEvent.RanOver;
        }
```

- [ ] **Step 5: Compilar y correr la suite** — Expected: `pass=117 fail=0` (101 + 8 de la
  Task 1 + 8 de esta). Si falla `Reset_StartsPlayerAheadOfHordeByStartingGap`, revisar que
  `Reset` del `HordeSimulation` ponga la unidad 0 exactamente en el frente.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Zombineta/Scripts/Enemies Assets/_Zombineta/Scripts/Core Assets/_Zombineta/Tests/EditMode
git commit -m "feat: la horda pasa a ser individuos con tipo y el disparo mata de verdad"
```

---

### Task 3: Barriles que explotan y zombies de frente

**Files:**
- Modify: `Assets/_Zombineta/Scripts/Level/LevelDefinition.cs`
- Modify: `Assets/_Zombineta/Scripts/Level/LevelRuntime.cs`
- Test: `Assets/_Zombineta/Tests/EditMode/BarrelsAndRamTests.cs`

**Interfaces:**
- Consumes: `RunSimulation.Explode(float, int)`, `RunSimulation.RunOver(int, float, int)`,
  `IBarrelField`, `RunEvent.RanOver`, `RunEvent.Explosion`.
- Produces: `LevelEntryKind.Barrel` (valor 5), `LevelEntryKind.ZombieFront` (valor 6),
  `LevelEntry.variant : int`, `LevelEntry(float distance, int lane, LevelEntryKind kind,
  float height = 0f, int variant = 0)`, `LevelRuntime : IBarrelField`.

- [ ] **Step 1: Escribir los tests que fallan** — crear `BarrelsAndRamTests.cs`:

```csharp
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
```

- [ ] **Step 2: Compilar y ver que falla** — Expected: `error CS` por `LevelEntryKind.Barrel`,
  `ZombieFront`, el constructor de 5 argumentos y `sim.Barrels = level`.

- [ ] **Step 3: `LevelDefinition.cs`** — el enum y el struct quedan:

```csharp
    public enum LevelEntryKind
    {
        Obstacle,
        Fuel,
        Battery,
        Ammo,
        Ramp,
        Barrel,
        ZombieFront,
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

        [Tooltip("Variante. En un zombie de frente, el indice de su tipo en Zombies.asset.")]
        public int variant;

        public LevelEntry(float distance, int lane, LevelEntryKind kind, float height = 0f, int variant = 0)
        {
            this.distance = distance;
            this.lane = lane;
            this.kind = kind;
            this.height = height;
            this.variant = variant;
        }
    }
```

- [ ] **Step 4: `LevelRuntime.cs`** — declarar que implementa la interfaz:

```csharp
    public sealed class LevelRuntime : IBarrelField
```

  agregar en el `for` de `Collect`, **después** del bloque de la rampa y **antes** del bloque
  de piso/aire:

```csharp
                // El barril no se toca al pasarlo: esta al costado y solo explota de un tiro.
                if (item.Entry.kind == LevelEntryKind.Barrel)
                    continue;

                if (item.Entry.kind == LevelEntryKind.ZombieFront)
                {
                    // Volando se le pasa por encima.
                    if (sim.State.Airborne)
                        continue;
                    item.Consumed = true;
                    events |= sim.RunOver(item.Entry.variant, d, item.Entry.lane);
                    continue;
                }
```

  y agregar al final de la clase:

```csharp
        // --- IBarrelField: los barriles son del recorrido, la explosion es de la horda ------

        public bool TryNearestBarrel(float fromX, int lane, float range, out float x, out int index)
        {
            x = 0f;
            index = -1;
            for (int i = 0; i < Items.Length; i++)
            {
                var item = Items[i];
                if (item.Consumed || item.Entry.kind != LevelEntryKind.Barrel)
                    continue;
                if (item.Entry.lane != lane)
                    continue;

                float d = item.Entry.distance;
                if (d > fromX || d < fromX - range)
                    continue;
                if (index < 0 || d > x)
                {
                    x = d;
                    index = i;
                }
            }
            return index >= 0;
        }

        public void Detonate(int index, RunSimulation sim) => Detonate(index, sim, 0);

        void Detonate(int index, RunSimulation sim, int depth)
        {
            if (index < 0 || index >= Items.Length || depth > 8)
                return;

            var item = Items[index];
            if (item.Consumed || item.Entry.kind != LevelEntryKind.Barrel)
                return;

            item.Consumed = true;
            float x = item.Entry.distance;
            sim.Explode(x, item.Entry.lane);

            // Un barril prende a los que tenga cerca: la cadena es la mejor bala del juego.
            float radius = sim.Config.explosionRadius;
            for (int i = 0; i < Items.Length; i++)
            {
                var other = Items[i];
                if (other.Consumed || other.Entry.kind != LevelEntryKind.Barrel)
                    continue;
                if (Mathf.Abs(other.Entry.distance - x) <= radius)
                    Detonate(i, sim, depth + 1);
            }
        }
```

- [ ] **Step 5: Compilar y correr la suite** — Expected: `pass=125 fail=0`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Zombineta/Scripts/Level Assets/_Zombineta/Tests/EditMode/BarrelsAndRamTests.cs Assets/_Zombineta/Tests/EditMode/BarrelsAndRamTests.cs.meta
git commit -m "feat: barriles que explotan de un tiro y zombies que se arrollan de frente"
```

---

### Task 4: El asset de tipos y la horda que se ve

**Files:**
- Create (MCP): `Assets/_Zombineta/Settings/Zombies.asset`
- Rewrite: `Assets/_Zombineta/Scripts/Enemies/HordeView.cs`
- Modify: `Assets/_Zombineta/Scripts/Level/LevelSpawner.cs`
- Modify (MCP): `Assets/_Zombineta/Scenes/Prototipo.unity`

**Interfaces:**
- Consumes: `RunSimulation.Horde`, `ZombieUnit`, `ZombieRoster`, `LevelEntryKind.Barrel`,
  `LevelEntryKind.ZombieFront`, `LevelEntry.variant`.

- [ ] **Step 1: Crear `Zombies.asset` con los tres tipos** — `Unity_RunCommand` (fuera de Play):

```csharp
using UnityEngine;
using UnityEditor;
using ZR = global::Zombineta.Enemies.ZombieRoster;
using ZT = global::Zombineta.Enemies.ZombieType;
using GC = global::Zombineta.Core.GameConfig;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        const string path = "Assets/_Zombineta/Settings/Zombies.asset";
        var roster = AssetDatabase.LoadAssetAtPath<ZR>(path);
        if (roster == null)
        {
            roster = ScriptableObject.CreateInstance<ZR>();
            AssetDatabase.CreateAsset(roster, path);
        }
        roster.types.Clear();
        roster.types.Add(new ZT { name = "Comun", speedMultiplier = 1f, deathChance = 0.85f,
            staggerOnHit = 0.6f, ramStun = 0.1f, ramFuelPenalty = 0f, spawnWeight = 60f,
            tint = Color.white, scale = 1f });
        roster.types.Add(new ZT { name = "Corredor", speedMultiplier = 1.35f, deathChance = 1f,
            staggerOnHit = 0f, ramStun = 0.1f, ramFuelPenalty = 0f, spawnWeight = 25f,
            tint = new Color(1f, 0.95f, 0.85f), scale = 0.85f });
        roster.types.Add(new ZT { name = "Pesado", speedMultiplier = 0.75f, deathChance = 0.35f,
            staggerOnHit = 0.4f, ramStun = 0.8f, ramFuelPenalty = 8f, spawnWeight = 15f,
            tint = new Color(0.72f, 0.72f, 0.8f), scale = 1.25f });
        EditorUtility.SetDirty(roster);

        var cfg = AssetDatabase.LoadAssetAtPath<GC>("Assets/_Zombineta/Settings/GameConfig.asset");
        var so = new SerializedObject(cfg);
        so.FindProperty("zombies").objectReferenceValue = roster;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        result.Log("roster con {0} tipos", roster.types.Count.ToString());
    }
}
```

  Si `LoadAssetAtPath` devuelve null recién creado, ejecutar `Assets/Refresh` y volver a
  asignar el roster al `GameConfig` en un comando aparte (trampa #4 del handoff).

- [ ] **Step 2: Reescribir `HordeView.cs`**

```csharp
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Enemies
{
    /// <summary>
    /// Dibuja la horda: un objeto por zombie simulado, con el tinte y la escala de su tipo.
    /// No decide nada; la masa vive en HordeSimulation. Dispara los triggers Hit y Die del
    /// Animator que ya trae el prefab, y reinicia la animacion cuando un zombie se recicla.
    /// </summary>
    public sealed class HordeView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Transform zombiePrefab;

        [Tooltip("Metros que la horda sigue avanzando por encima de la moto al atraparla.")]
        [SerializeField] float overrunMeters = 12f;

        [Header("Faro")]
        [Tooltip("Los zombies iluminados se aclaran y se echan para atras: se ve por que frenan.")]
        [SerializeField] Color litTint = new Color(1.25f, 1.2f, 1.1f);
        [SerializeField] float litLeanDegrees = -12f;
        [Tooltip("Metros detras de la moto que alcanza el cono del faro.")]
        [SerializeField] float headlightRangeMeters = 28f;

        Transform[] bodies;
        SpriteRenderer[] sprites;
        Animator[] animators;
        int[] generations;
        bool[] wasAlive;
        float[] lastStagger;

        // Solo presentacion: la simulacion ya termino al atraparte, pero la horda sigue
        // avanzando por encima de la moto (en camara lenta, con el tiempo escalado).
        float overrun;

        void Start()
        {
            if (run == null || zombiePrefab == null || run.Sim == null)
                return;

            int count = run.Sim.Horde.Units.Length;
            bodies = new Transform[count];
            sprites = new SpriteRenderer[count];
            animators = new Animator[count];
            generations = new int[count];
            wasAlive = new bool[count];
            lastStagger = new float[count];

            for (int i = 0; i < count; i++)
            {
                bodies[i] = Instantiate(zombiePrefab, transform);
                sprites[i] = bodies[i].GetComponentInChildren<SpriteRenderer>();
                animators[i] = bodies[i].GetComponentInChildren<Animator>();
                generations[i] = -1;
                wasAlive[i] = true;
            }
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null || bodies == null)
                return;

            var state = run.Sim.State;
            var horde = run.Sim.Horde;

            if (state.Phase == RunPhase.Lost)
                overrun = Mathf.Min(overrunMeters, overrun + run.Config.hordeBaseSpeed * Time.deltaTime);
            else if (state.Phase == RunPhase.Running)
                overrun = 0f;

            bool lightOn = state.HeadlightOn;

            for (int i = 0; i < bodies.Length; i++)
            {
                var u = horde.Units[i];
                var type = horde.Type(u.Type);

                // Se reciclo: es otro zombie, vuelve a caminar desde cero.
                if (generations[i] != u.Generation)
                {
                    generations[i] = u.Generation;
                    wasAlive[i] = true;
                    lastStagger[i] = 0f;
                    if (animators[i] != null)
                    {
                        animators[i].Rebind();
                        animators[i].Update(0f);
                    }
                }

                if (wasAlive[i] && !u.Alive && animators[i] != null)
                    animators[i].SetTrigger("Die");
                else if (u.Alive && u.Stagger > lastStagger[i] + 0.01f && animators[i] != null)
                    animators[i].SetTrigger("Hit");

                wasAlive[i] = u.Alive;
                lastStagger[i] = u.Stagger;

                bodies[i].position = new Vector3(
                    run.ToWorldX(u.X + overrun), run.LaneToWorldY(u.Lane), 0f);
                bodies[i].localScale = Vector3.one * type.scale;

                bool lit = lightOn && u.Alive &&
                           u.X < state.PlayerX && u.X > state.PlayerX - headlightRangeMeters;
                bodies[i].rotation = Quaternion.Euler(0f, 0f, lit ? litLeanDegrees : 0f);
                if (sprites[i] != null)
                    sprites[i].color = lit ? type.tint * litTint : type.tint;
            }
        }
    }
}
```

- [ ] **Step 3: `LevelSpawner.cs`** — agregar campos:

```csharp
        [Header("Barril y zombie de frente")]
        [SerializeField] Sprite barrelSprite;
        [SerializeField] Color barrelColor = new Color(0.9f, 0.55f, 0.15f);
        [Tooltip("Sprite del zombie parado que viene de frente. Se dibuja espejado.")]
        [SerializeField] Sprite frontZombieSprite;
```

  y dentro del loop, después de la línea `var kind = item.Entry.kind;`, reemplazar la
  asignación de sprite por:

```csharp
                sr.sprite = SpriteFor(kind);
                sr.flipX = kind == LevelEntryKind.ZombieFront;   // mira hacia la jugadora
```

  agregar el método:

```csharp
        Sprite SpriteFor(LevelEntryKind kind)
        {
            switch (kind)
            {
                case LevelEntryKind.Ramp: return rampSprite != null ? rampSprite : defaultSprite;
                case LevelEntryKind.Barrel: return barrelSprite != null ? barrelSprite : defaultSprite;
                case LevelEntryKind.ZombieFront:
                    return frontZombieSprite != null ? frontZombieSprite : defaultSprite;
                default: return defaultSprite;
            }
        }
```

  en `ColorFor`, agregar antes del `default`:

```csharp
                case LevelEntryKind.Barrel: return barrelColor;
```

  El color del zombie de frente depende de su **tipo**, que está en la entrada y no en el
  `kind`, así que va aparte: en el loop, después de `sr.color = ColorFor(kind);` agregar:

```csharp
                if (kind == LevelEntryKind.ZombieFront && run.Config.zombies != null)
                    sr.color = run.Config.zombies.Get(item.Entry.variant).tint;
```

  y en `ScaleFor`, agregar antes del `default`:

```csharp
                case LevelEntryKind.Barrel: return new Vector3(0.7f, 0.9f, 1f);
                case LevelEntryKind.ZombieFront: return Vector3.one;
```

- [ ] **Step 4: Generar el sprite del barril y sacar el del zombie parado** —
  `Unity_RunCommand`: escribir `Assets/_Zombineta/Art/Level/barril_placeholder.png` (un
  rectángulo blanco de 96×128 con las esquinas recortadas, igual técnica que la rampa) y
  configurar su importer como Sprite, 100 PPU, pivot abajo al centro `(0.5, 0)`. Para el
  zombie de frente, usar el **primer sprite de `ZombieViejo.png`** ya importado: obtenerlo con
  `AssetDatabase.LoadAllAssetRepresentationsAtPath` (trampa #9 del handoff) y asignarlo a
  `frontZombieSprite`.

- [ ] **Step 5: Cablear la escena** — `Unity_RunCommand`: asignar en `LevelSpawner` los campos
  `barrelSprite` y `frontZombieSprite` por `SerializedObject`, y guardar la escena con
  `EditorSceneManager.SaveScene`.

- [ ] **Step 6: Verificar en Play** — entrar en Play, `Application.runInBackground = true`,
  arrancar un nivel por la API de `ScreenFlow.Flow` y, en el mismo comando, colgar un callback
  de `EditorApplication.update` que a los 2 s registre en `SessionState` cuántos zombies hay
  vivos, cuántos tipos distintos se ven y la posición del frente; capturar con
  `Unity_Camera_Capture`. Expected: la masa repartida en los tres carriles con tamaños
  distintos, sin errores en consola. Salir de Play.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Zombineta/Settings/Zombies.asset Assets/_Zombineta/Settings/Zombies.asset.meta Assets/_Zombineta/Settings/GameConfig.asset Assets/_Zombineta/Scripts/Enemies/HordeView.cs Assets/_Zombineta/Scripts/Level/LevelSpawner.cs Assets/_Zombineta/Art/Level Assets/_Zombineta/Scenes/Prototipo.unity
git commit -m "feat: la horda se dibuja por individuos, con tipos, barriles y zombies de frente"
```

---

### Task 5: Partículas, traza, manchas y la opción de sangre

**Files:**
- Create: `Assets/_Zombineta/Scripts/Fx/FxManager.cs`
- Modify: `Assets/_Zombineta/Scripts/Core/GameSettings.cs`
- Modify: `Assets/_Zombineta/Scripts/UI/ScreenFlow.cs`
- Modify: `Assets/_Zombineta/Scripts/CameraFx/CameraConfig.cs`, `Core/CameraFollow.cs`
- Create (MCP): `Assets/_Zombineta/Art/Fx/particula.png`, material y 4 prefabs de partículas
- Modify (MCP): `Assets/_Zombineta/Scenes/Prototipo.unity`

**Interfaces:**
- Consumes: `HordeEvent`, `HordeEventKind`, `DeathCause`, `HordeSimulation.Events`,
  `HordeSimulation.Epoch`, `RunController.ToWorldX`, `RunController.LaneToWorldY`,
  `RunEvent.Explosion`, `RunEvent.RanOver`.
- Produces: `GameSettings.Gore : bool` (clave `zombineta.gore`), `CameraConfig.explosionTrauma`,
  `CameraConfig.ramTrauma`.

- [ ] **Step 1: `GameSettings`** — agregar, con el mismo patrón de los otros:

```csharp
        const string GoreKey = "zombineta.gore";
        static bool? gore;

        /// <summary>Sangre alta o baja. En baja, las salpicaduras son polvo y no quedan manchas.</summary>
        public static bool Gore
        {
            get => gore ??= PlayerPrefs.GetInt(GoreKey, 1) != 0;
            set
            {
                gore = value;
                PlayerPrefs.SetInt(GoreKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
```

- [ ] **Step 2: Crear la partícula y los prefabs** — `Unity_RunCommand`:
  1. Escribir `Assets/_Zombineta/Art/Fx/particula.png`: 64×64, círculo blanco de borde suave
     (mismo generador que `sombra.png`). Importer: Sprite, 100 PPU, alpha is transparency.
  2. Crear `Assets/_Zombineta/Art/Fx/Particula.mat` con
     `Shader.Find("Universal Render Pipeline/Particles/Unlit")`; si devuelve null, usar
     `Shader.Find("Sprites/Default")`. Textura principal: la partícula.
  3. Crear cuatro prefabs en `Assets/_Zombineta/Prefabs/`: `FxImpacto`, `FxMuerte`,
     `FxAtropello`, `FxExplosion`, cada uno un GameObject con `ParticleSystem` configurado
     por código (`main.duration = 1`, `loop = false`, `playOnAwake = false`,
     `startLifetime`, `startSpeed`, `startSize`, `gravityModifier`, `maxParticles`;
     `emission.SetBursts` con un burst; `shape.angle`/`radius`), el
     `ParticleSystemRenderer` con el material y `sortingOrder = 20`. Valores:
     - `FxImpacto`: burst 10, vida 0,35 s, velocidad 4, tamaño 0,08, gravedad 0,6, cono hacia atrás.
     - `FxMuerte`: burst 24, vida 0,7 s, velocidad 5, tamaño 0,12, gravedad 1.
     - `FxAtropello`: burst 40, vida 0,9 s, velocidad 9, tamaño 0,14, gravedad 1,2, cono hacia adelante.
     - `FxExplosion`: burst 60, vida 1,1 s, velocidad 12, tamaño 0,25, gravedad 0,2, esfera.

- [ ] **Step 3: Crear `FxManager.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;

namespace Zombineta.Fx
{
    /// <summary>
    /// Traduce los eventos de la horda en cosas que se ven: traza del disparo, particulas de
    /// impacto, muerte, atropello y explosion, y manchas en el asfalto. Todo con pool: en
    /// partida no se instancia nada.
    ///
    /// Corre despues de la simulacion y lee la lista de eventos del tick. Si no hubo tick
    /// (pausa, Game Over), el Epoch no cambia y no se repite nada.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class FxManager : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Header("Particulas")]
        [SerializeField] ParticleSystem impactPrefab;
        [SerializeField] ParticleSystem deathPrefab;
        [SerializeField] ParticleSystem runOverPrefab;
        [SerializeField] ParticleSystem explosionPrefab;
        [SerializeField] int poolPerKind = 6;

        [Header("Traza del disparo")]
        [SerializeField] LineRenderer tracer;
        [SerializeField] float tracerSeconds = 0.05f;

        [Header("Manchas")]
        [SerializeField] SpriteRenderer decalPrefab;
        [SerializeField] int decalCount = 30;
        [SerializeField] Color bloodColor = new Color(0.55f, 0.05f, 0.07f, 0.85f);
        [SerializeField] Color dustColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        readonly List<ParticleSystem> impacts = new List<ParticleSystem>();
        readonly List<ParticleSystem> deaths = new List<ParticleSystem>();
        readonly List<ParticleSystem> runOvers = new List<ParticleSystem>();
        readonly List<ParticleSystem> explosions = new List<ParticleSystem>();

        SpriteRenderer[] decals;
        int nextDecal;
        int lastEpoch = -1;
        float tracerLeft;

        void Start()
        {
            Fill(impacts, impactPrefab);
            Fill(deaths, deathPrefab);
            Fill(runOvers, runOverPrefab);
            Fill(explosions, explosionPrefab);

            if (decalPrefab != null)
            {
                decals = new SpriteRenderer[decalCount];
                for (int i = 0; i < decalCount; i++)
                {
                    decals[i] = Instantiate(decalPrefab, transform);
                    decals[i].enabled = false;
                }
            }

            if (tracer != null)
                tracer.enabled = false;
        }

        void Fill(List<ParticleSystem> pool, ParticleSystem prefab)
        {
            if (prefab == null)
                return;
            for (int i = 0; i < poolPerKind; i++)
                pool.Add(Instantiate(prefab, transform));
        }

        void LateUpdate()
        {
            if (tracerLeft > 0f)
            {
                tracerLeft -= Time.unscaledDeltaTime;
                if (tracerLeft <= 0f && tracer != null)
                    tracer.enabled = false;
            }

            if (run == null || run.Sim == null)
                return;

            var horde = run.Sim.Horde;
            if (horde.Epoch == lastEpoch)
                return;
            lastEpoch = horde.Epoch;

            bool gore = GameSettings.Gore;
            for (int i = 0; i < horde.Events.Count; i++)
                Play(horde.Events[i], gore);
        }

        void Play(HordeEvent e, bool gore)
        {
            Vector3 at = new Vector3(run.ToWorldX(e.X), run.LaneToWorldY(e.Lane) + 0.5f, 0f);

            switch (e.Kind)
            {
                case HordeEventKind.Tracer:
                    ShowTracer(e);
                    break;

                case HordeEventKind.Impact:
                    Emit(impacts, at, gore ? bloodColor : dustColor);
                    break;

                case HordeEventKind.Death:
                    Emit(e.Cause == DeathCause.RunOver ? runOvers : deaths, at,
                         gore ? bloodColor : dustColor);
                    if (gore)
                        Stain(new Vector3(at.x, run.LaneToWorldY(e.Lane), 0f));
                    break;

                case HordeEventKind.Explosion:
                    Emit(explosions, at, new Color(1f, 0.75f, 0.3f, 1f));
                    break;

                case HordeEventKind.Miss:
                    // La bala perdida no deja nada: la traza ya conto la historia.
                    break;
            }
        }

        void ShowTracer(HordeEvent e)
        {
            if (tracer == null)
                return;
            float y = run.LaneToWorldY(e.Lane) + 0.55f;
            tracer.SetPosition(0, new Vector3(run.ToWorldX(e.FromX), y, 0f));
            tracer.SetPosition(1, new Vector3(run.ToWorldX(e.X), y, 0f));
            tracer.enabled = true;
            tracerLeft = tracerSeconds;
        }

        void Emit(List<ParticleSystem> pool, Vector3 at, Color color)
        {
            if (pool.Count == 0)
                return;

            // El que este libre; si estan todos ocupados, se reusa el primero.
            ParticleSystem chosen = pool[0];
            for (int i = 0; i < pool.Count; i++)
                if (!pool[i].isPlaying)
                {
                    chosen = pool[i];
                    break;
                }

            chosen.transform.position = at;
            var main = chosen.main;
            main.startColor = color;
            chosen.Clear();
            chosen.Play();
        }

        void Stain(Vector3 at)
        {
            if (decals == null || decals.Length == 0)
                return;
            var d = decals[nextDecal];
            nextDecal = (nextDecal + 1) % decals.Length;
            d.transform.position = at;
            d.transform.localScale = Vector3.one * Random.Range(0.7f, 1.3f);
            d.color = bloodColor;
            d.enabled = true;
        }
    }
}
```

- [ ] **Step 4: Sacudidas de explosión y atropello** — en `CameraConfig`, al final:

```csharp
        [Header("Horda")]
        [Tooltip("Sacudida de una explosion de barril.")]
        public float explosionTrauma = 0.5f;
        [Tooltip("Sacudida al arrollar a un zombie de frente.")]
        public float ramTrauma = 0.25f;
```

  y en `CameraFollow.OnStepped`, después del bloque de `LandedPerfect`:

```csharp
            if ((events & RunEvent.Explosion) != 0)
            {
                director.AddTrauma(config.explosionTrauma);
                director.Punch(0f, 0.2f);
            }

            if ((events & RunEvent.RanOver) != 0)
                director.AddTrauma(config.ramTrauma);
```

- [ ] **Step 5: La opción "Sangre" en el menú** — en `ScreenFlow`, en el manejo de Opciones,
  insertar un ítem nuevo **antes** del de VOLVER, con el mismo patrón que los toggles de
  cámara: etiqueta `"Sangre:  Alta"` / `"Sangre:  Baja"` leyendo y escribiendo
  `GameSettings.Gore` con A/D o ENTER, y correr el índice de VOLVER en uno. Después, por MCP,
  agregar `Item5` al `OptionsPanel/Menu` (fuente 42) y repartir los seis ítems en
  y = 180, 108, 36, −36, −108, −180, dejando `MenuList.items` con las seis referencias.

- [ ] **Step 6: Cablear la escena** — `Unity_RunCommand`: crear un GameObject `Fx` con
  `FxManager`, asignarle `run`, los cuatro prefabs de partículas y el `decalPrefab` (un
  SpriteRenderer con la partícula, `sortingOrder` por debajo de los zombies); crear
  `Scooter/Tracer` con un `LineRenderer` (2 posiciones, ancho 0,06, material del sprite,
  `sortingOrder = 25`) y asignarlo. Guardar la escena.

- [ ] **Step 7: Verificar en Play** — en un solo comando: arrancar el nivel, poner la moto y
  un zombie en el mismo carril, forzar un disparo (`run.Sim.Tick` no: usar
  `run.Sim.State.Ammo = 6` y llamar a `sim.Tick` con `Fire = true` desde un callback de
  `EditorApplication.update`), pausar al frame siguiente y capturar. Expected: la traza
  visible, partículas en el impacto y, si `Gore` está en alto, una mancha en el asfalto.
  Repetir con un barril para ver la explosión. Salir de Play.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Zombineta/Scripts/Fx Assets/_Zombineta/Scripts/Core/GameSettings.cs Assets/_Zombineta/Scripts/UI/ScreenFlow.cs Assets/_Zombineta/Scripts/CameraFx/CameraConfig.cs Assets/_Zombineta/Scripts/Core/CameraFollow.cs Assets/_Zombineta/Art/Fx Assets/_Zombineta/Prefabs Assets/_Zombineta/Scenes/Prototipo.unity
git commit -m "feat: particulas de impacto, muerte, atropello y explosion, con opcion de sangre"
```

---

### Task 6: Barriles y zombies de frente en las rutas

**Files:**
- Modify (MCP): `Assets/_Zombineta/Settings/Ruta01.asset`, `Ruta02.asset`

- [ ] **Step 1: Insertar las entradas nuevas** — `Unity_RunCommand`, semillas fijas
  (Ruta01 → 1301, Ruta02 → 1302). Reglas:
  - **Barriles** cada ~200 m desde los 250 m: uno en un carril al azar, siempre que no caiga
    dentro de una pieza de rampa (`[d − 5, d + 50]` de cualquier rampa).
  - **Zombies de frente** cada ~150 m desde los 200 m: carril al azar, tipo 0 (común) el 70%
    de las veces y tipo 2 (pesado) el 30%, nunca dentro de una pieza de rampa ni a menos de
    10 m de un obstáculo del mismo carril.
  - Un zombie de frente **cuenta como bloqueo** para la regla de los tres carriles: si con él
    quedaran los tres carriles bloqueados a ±3 m, no se coloca.
  - `SortByDistance()`, `EditorUtility.SetDirty`, `AssetDatabase.SaveAssets()`.
  - Si la ruta ya tiene barriles, no tocar nada (idempotente, como en el plan de las rampas).

- [ ] **Step 2: Verificar con la simulación real** — `Unity_RunCommand` que, por cada ruta:
  cuente barriles y zombies de frente; verifique que ningún punto tenga los tres carriles
  bloqueados contando obstáculos y zombies de frente; y para cada barril, simule un disparo
  desde 5 m más adelante con la horda colocada encima del barril y confirme que explota y mata.
  Expected: 0 puntos bloqueados y todos los barriles detonables.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Zombineta/Settings/Ruta01.asset Assets/_Zombineta/Settings/Ruta02.asset
git commit -m "content: barriles explosivos y zombies de frente en las dos rutas"
```

---

### Task 7: Verificación en Play y handoff

**Files:**
- Modify: `HANDOFF.md`

- [ ] **Step 1: Partida real** — en Play, con `runInBackground`, arrancar un nivel y dejarlo
  correr 20 s registrando por `run.Stepped` los eventos `Shot`, `ShotMissed`, `RanOver` y
  `Explosion`, más la cantidad de zombies vivos al final. Disparar por código cada 2 s.
  Expected: la población se mantiene, hay muertes, no hay errores en consola. Capturar.

- [ ] **Step 2: Actualizar `HANDOFF.md`:**
  - Estado y cantidad de tests (125).
  - Sección nueva "La horda" en Arquitectura: individuos, frente = el más adelantado, tipos en
    `Zombies.asset`, reciclado infinito, semilla, disparo por carril, barriles y cadena,
    arrollar de frente, y que **`shotHordePushback` ya no existe**.
  - Verbos: la bala pega en tu carril; los barriles se detonan de un tiro.
  - Opciones: la tercera preferencia (`Sangre`).
  - Verificado / no verificado: el balance cambió (la pistola ya no empuja 15 m fijos) y hay
    que volver a jugarlo; el *feel* del disparo sin teclado sigue sin probarse.
  - Fase 5: los tipos usan el mismo sprite con tinte y escala; falta arte por tipo.

- [ ] **Step 3: Commit**

```bash
git add HANDOFF.md
git commit -m "docs: handoff con la horda de individuos"
```
