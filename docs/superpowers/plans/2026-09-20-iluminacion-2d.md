# Iluminación 2D con URP — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el juego se ilumine con luces 2D de URP y que arte pueda trabajar la luz de cada
asset abriendo un prefab, con una escena taller, un perfil de luz por nivel, un presupuesto de
luces y una guía propia.

**Architecture:** El escenario deja de crear `SpriteRenderer` sueltos y pasa a instanciar y
reciclar **prefabs** por variante; cinco Sorting Layers con nombre permiten que cada `Light2D`
elija a qué alcanza; un `PerfilDeLuz` por nivel maneja el ambiente; la lógica con números
(presupuesto de luces, parpadeo, destellos) es C# plano con tests, y las vistas solo aplican.

**Tech Stack:** Unity 6000.6.0f1, URP 2D (Renderer2D con los cuatro blend styles ya configurados),
`Light2D`/`ShadowCaster2D`, uGUI, NUnit (EditMode), MCP de Unity.

Spec: `docs/superpowers/specs/2026-09-20-iluminacion-2d-design.md`.

## Modelos recomendados por tarea

Para el que ejecuta el plan con subagentes. El criterio: **transcribir código que ya está escrito
en el plan → modelo barato; integrar varios archivos o escenas → medio; criterio visual o de
arquitectura → el más capaz.**

| Tarea | Qué es | Implementador | Revisor |
|---|---|---|---|
| 1 Capas de dibujo | Datos + cableado mecánico, mucho MCP | sonnet | sonnet |
| 2 Tiles a prefabs | Refactor del pool + migración de assets | sonnet | **opus** (es el cambio más riesgoso) |
| 3 Perfil de luz | Asset + componente + tests dados | sonnet | sonnet |
| 4 Presupuesto y parpadeo | C# puro, con código y tests en el plan | **haiku** | sonnet |
| 5 Luz de gameplay | Faro, destellos y horda; integra con FX | sonnet | sonnet |
| 6 Sombras y prefabs de items | Escenas y prefabs, criterio visual | sonnet | sonnet |
| 7 Calidad Alta/Baja | Opciones + aplicar el presupuesto | sonnet | sonnet |
| 8 Taller de luz | Herramienta de editor que genera escena | sonnet | sonnet |
| 9 Medición y capturas | Verificación, comparar imágenes y FPS | sonnet | — |
| 10 Guía para arte y HANDOFF | Documentación con capturas | sonnet | sonnet |
| Revisión final de la rama | Arquitectura y riesgos cruzados | — | **opus** |

Cada dispatch debe declarar el modelo explícitamente; omitirlo hereda el de la sesión (el más caro).

## Global Constraints

- Solo `Juego/`. No tocar `Prototipo/` ni la lógica de reglas de
  `Juego/Assets/_Zombineta/Simulacion/Runtime` (sí se puede agregar una preferencia en
  `GameSettings`, como se hizo con `Vibration`).
- Sorting Layers, de atrás hacia adelante: `Cielo`, `Fondo`, `Calle`, `Juego`, `Frente`.
  Asignación de capas del escenario: Cielo → `Cielo`; Edificios y Árboles → `Fondo`; Cordón, Calle
  y Vereda → `Calle`; Frontal y Primer plano → `Frente`. Todo lo del juego (moto, horda, items,
  sombras de piso, efectos) va a `Juego` conservando el orden de `LaneSorting`.
- Normal maps: solo escenario. Personajes y objetos se iluminan planos.
- Sombras (`ShadowCaster2D`): faroles, carteles, vallas, obstáculos, barriles, la moto y los
  zombies. El fondo lejano no proyecta.
- `GameSettings.LightQuality`: `Alta` / `Baja`, default `Alta`, clave `zombineta.lightQuality`.
  En `Baja`: sin sombras, sin luces de adorno y sin destellos; quedan ambiente y faro.
- Las luces de gameplay (faro, destellos, horda) nunca entran al presupuesto de luces.
- Cada `SceneryVariant` lleva un `grupo` (texto libre: ciudad, autopista, puente, transición). No
  se implementa ninguna lógica de tramos: es solo dato, para el sub-proyecto 6.
- Estáticos reiniciados en `SubsystemRegistration` (recarga de dominio desactivada).
- Nunca commitear `Juego/ProjectSettings/ProjectSettings.asset` (Unity le mete `runInBackground`
  durante las pruebas). Las Sorting Layers sí viven en `ProjectSettings/TagManager.asset`: ese sí
  se commitea.
- Pruebas en Play por MCP: registrar el callback **antes** de entrar en Play; no enfocar ventanas
  ni leer píxeles de pantalla.
- Commits en español, con el `Co-Authored-By` del modelo que los escribe. Push/merge solo si el
  usuario lo pide.

Rutas: `Z/` = `Juego/Assets/_Zombineta/`.

## Estructura de archivos

| Archivo | Responsabilidad |
|---|---|
| `Z/Scripts/Gameplay/Scenery/SceneryManager.cs` (modificar) | Instanciar y reciclar prefabs por variante; capa y orden |
| `Z/Simulacion/Runtime/Scenery/SceneryTileset.cs` (modificar) | `prefab` y `grupo` por variante; `sortingLayer` por capa |
| `Z/Editor/Luz/TilePrefabMigrator.cs` (nuevo) | Convertir los sprites actuales en prefabs y reescribir el asset |
| `Z/Scripts/Gameplay/Luz/PerfilDeLuz.cs` (nuevo) | Asset de ambiente: color e intensidad por capa |
| `Z/Scripts/Gameplay/Luz/LightingDirector.cs` (nuevo) | Crear y mantener las luces globales del perfil |
| `Z/Scripts/Gameplay/Luz/LightBudget.cs` (nuevo) | C# puro: qué luces de adorno quedan prendidas |
| `Z/Scripts/Gameplay/Luz/FlickerMath.cs` (nuevo) | C# puro: intensidad del parpadeo |
| `Z/Scripts/Gameplay/Luz/Flicker.cs` (nuevo) | Componente que aplica el parpadeo a una `Light2D` |
| `Z/Scripts/Gameplay/Luz/DecorLight.cs` (nuevo) | Marca una luz como "de adorno" y la registra |
| `Z/Scripts/Gameplay/Luz/LightBudgetRunner.cs` (nuevo) | Aplica el presupuesto cada cierto tiempo |
| `Z/Scripts/Gameplay/Luz/FlashDirector.cs` (nuevo) | C# puro: destellos vivos y su intensidad |
| `Z/Scripts/Gameplay/Luz/FlashConfig.cs` (nuevo) | Asset: color, intensidad, radio y duración por evento |
| `Z/Scripts/Gameplay/Luz/FlashLights.cs` (nuevo) | Pool de `Light2D` que aplica el `FlashDirector` |
| `Z/Scripts/Gameplay/Luz/HordeGlow.cs` (nuevo) | Luz tenue que sigue al frente de la horda |
| `Z/Editor/Luz/TallerBuilder.cs` (nuevo) | Genera `Scenes/TallerLuz.unity` |
| `docs/Guia-de-iluminacion-para-arte.md` (nuevo) | Paso a paso para arte |

---

### Task 1: Capas de dibujo con nombre

**Modelo:** implementador sonnet · revisor sonnet

**Files:**
- Modify: `Juego/ProjectSettings/TagManager.asset` (Sorting Layers)
- Modify: `Z/Simulacion/Runtime/Scenery/SceneryTileset.cs` (campo `sortingLayer` en `SceneryLayer`)
- Modify: `Z/Scripts/Gameplay/Scenery/SceneryManager.cs` (aplicar la capa al crear el renderer)
- Modify: `Z/Scripts/Gameplay/Core/LaneSorting.cs` (constante con el nombre de la capa del juego)
- Modify (MCP): `Z/Settings/Escenario.asset` (capa de cada layer), `Z/Scenes/Templates/NivelBase.unity`,
  `Z/Scenes/Level_01.unity`, `Z/Scenes/Level_02.unity` (moto, horda, items, HUD y efectos a `Juego`)
- Test: `Z/Tests/Editor/LaneSortingTests.cs` (sumar un caso)

**Interfaces:**
- Produces:
  - Sorting Layers en este orden: `Cielo`, `Fondo`, `Calle`, `Juego`, `Frente` (además de `Default`).
  - `SceneryLayer.sortingLayer` (string, default `"Fondo"`).
  - `LaneSorting.GameLayer` (`const string = "Juego"`).

- [ ] **Step 0: Línea de base** — antes de tocar nada, en Play sobre `Level_01` con la moto en
  300 m y la horda a 20 m: guardar el render de la cámara en `.git/sdd/luz/base_antes.png` y el
  tiempo de cuadro promedio de 300 cuadros (`Time.unscaledDeltaTime` acumulado en el callback) en
  `.git/sdd/luz/medicion.md`, con la máquina donde se corrió.

- [ ] **Step 1: Crear las Sorting Layers** por MCP (`TagManager.asset` vía `SerializedObject` sobre
  `UnityEditor.EditorSettings`/`TagManager`), en orden: `Default`, `Cielo`, `Fondo`, `Calle`,
  `Juego`, `Frente`. Verificar con `SortingLayer.layers` que quedan los seis y en ese orden.

- [ ] **Step 2: Test que falla** — agregar en `LaneSortingTests.cs`:

```csharp
        [Test]
        public void TheGameLayer_IsNamedAndExists()
        {
            Assert.AreEqual("Juego", LaneSorting.GameLayer);
            Assert.IsTrue(System.Array.Exists(UnityEngine.SortingLayer.layers,
                l => l.name == LaneSorting.GameLayer), "falta la Sorting Layer del juego");
        }
```

- [ ] **Step 3: Correr y ver que falla** (Test Runner EditMode por MCP, filtro `LaneSortingTests`):
  error de compilación, `LaneSorting.GameLayer` no existe.

- [ ] **Step 4: Implementar** — en `LaneSorting`: `public const string GameLayer = "Juego";`. En
  `SceneryLayer`: `[Tooltip("Sorting Layer de esta capa: Cielo, Fondo, Calle, Juego o Frente.")] public string sortingLayer = "Fondo";`.
  En `SceneryManager.CreateRenderer`: `sr.sortingLayerName = cfg.sortingLayer;` (además del
  `sortingOrder` actual).

- [ ] **Step 5: Correr: PASS** y la suite completa en verde (209 + 1 = 210).

- [ ] **Step 6: Asignar capas** por MCP en `Escenario.asset` según Global Constraints, y poner en
  `Juego` los renderers de las escenas: `Scooter` y su sombra, el prefab `Zombie`, los items del
  recorrido (`LevelItemPalette` no guarda capa: se pone en `LevelScene.ApplyVisual` y en
  `HordeView`/`ScooterView`/`FxManager` con `sortingLayerName = LaneSorting.GameLayer`), y los
  prefabs de efectos. Guardar escenas.

- [ ] **Step 7: Verificar por MCP en Play** en `Level_01`: capturar el render de la cámara y
  compararlo con `.git/sdd/luz/base_antes.png` (el del Step 0 con el
  mismo tramo, misma posición): la imagen tiene que ser prácticamente igual (el cambio es de
  capas, no de aspecto). Consola sin errores.

- [ ] **Step 8: Commit**

```bash
git add Juego/ProjectSettings/TagManager.asset Juego/Assets/_Zombineta/Simulacion/Runtime/Scenery/SceneryTileset.cs Juego/Assets/_Zombineta/Scripts Juego/Assets/_Zombineta/Settings/Escenario.asset Juego/Assets/_Zombineta/Scenes Juego/Assets/_Zombineta/Tests/Editor/LaneSortingTests.cs
git commit -m "Capas de dibujo con nombre para poder iluminar por capa"
```

---

### Task 2: Tiles como prefabs

**Modelo:** implementador sonnet · revisor **opus** (es el cambio más riesgoso: toca el escenario entero)

**Files:**
- Modify: `Z/Simulacion/Runtime/Scenery/SceneryTileset.cs` (`SceneryVariant`: `prefab`, `grupo`)
- Modify: `Z/Scripts/Gameplay/Scenery/SceneryManager.cs` (pool por variante)
- Create: `Z/Editor/Luz/TilePrefabMigrator.cs`
- Create: `Z/Art/Tileset/Prefabs/*.prefab` (uno por sprite actual, generados)
- Modify: `Z/Settings/Escenario.asset` (referencias a prefabs y grupos)

**Interfaces:**
- Consumes: `SceneryLayer.sortingLayer` (Task 1).
- Produces:
  - `SceneryVariant.prefab` (`GameObject`), `SceneryVariant.grupo` (string, default `"ciudad"`).
    El campo `sprite` se conserva pero pasa a `[HideInInspector]` y solo lo usa la migración.
  - Menú `Zombineta/Luz/Migrar tiles a prefabs`.
  - Contrato del prefab: raíz con `SpriteRenderer`; el sistema le pisa `sortingLayerName`,
    `sortingOrder`, `color` y la escala; los hijos (`Light2D`, `ShadowCaster2D`, `Flicker`) son de
    arte y no se tocan.

- [ ] **Step 1: Captura de referencia** — antes de tocar nada, por MCP en Play sobre `Level_01`,
  posicionar la moto en 300 m y guardar el render de la cámara en
  `.git/sdd/luz/escenario_antes.png`. Es contra lo que se compara al final.

- [ ] **Step 2: Migrador** — `TilePrefabMigrator`: por cada capa y variante con `sprite != null`,
  crea `Z/Art/Tileset/Prefabs/<NombreSprite>.prefab` (si no existe) con un `GameObject` cuyo
  `SpriteRenderer` tiene ese sprite y el material de la capa (o `Sprite-Lit-Default` si la capa no
  tiene material), asigna `variant.prefab` y deja `grupo = "ciudad"`. No borra `sprite`. Guarda el
  asset. Informa en consola cuántos prefabs creó y cuántas variantes quedaron sin prefab.

- [ ] **Step 3: Correr el migrador** por MCP y verificar: hay un prefab por cada sprite del
  tileset, `Escenario.asset` no tiene ninguna variante con `prefab == null`, y los `.meta` nuevos
  están en el repo.

- [ ] **Step 4: Pool por variante en `SceneryManager`** — reemplazar el pool único de
  `SpriteRenderer` por uno por variante:
  - `scales[i]` y `widths[i]` salen del `SpriteRenderer` del prefab
    (`prefab.GetComponent<SpriteRenderer>().sprite.bounds`); si el prefab no tiene sprite, ancho 0 y
    un `Debug.LogWarning` con el nombre de la variante y la capa.
  - `LayerRuntime` guarda `List<Transform>[] poolPorVariante` y un índice de uso por frame.
  - `Refresh`: por cada tile visible pide una instancia de su variante (reusa una apagada o
    instancia el prefab bajo `root`), la coloca con la misma cuenta de pivot que hoy
    (`tile.X - b.min.x * s`, `cfg.baselineY + v.yOffset - b.min.y * s`) y la prende; al final apaga
    las instancias no usadas de cada variante (`gameObject.SetActive(false)`).
  - Al instanciar: `sortingLayerName`, `sortingOrder`, `color` y escala uniforme `s` sobre la raíz.
  - `Dispose` destruye `root` como hoy.

- [ ] **Step 5: Verificar que no cambió nada visualmente** — misma captura que el Step 1 y
  comparación: diferencia media por píxel < 2 % (calcularla en el mismo RunCommand leyendo los dos
  PNG). Si hay corrimiento vertical u horizontal, es el pivot: revisar la cuenta antes de seguir.

- [ ] **Step 6: Suite completa en verde** (210) y consola sin errores.

- [ ] **Step 7: Commit**

```bash
git add Juego/Assets/_Zombineta/Simulacion/Runtime/Scenery/SceneryTileset.cs Juego/Assets/_Zombineta/Scripts/Gameplay/Scenery/SceneryManager.cs Juego/Assets/_Zombineta/Editor/Luz Juego/Assets/_Zombineta/Art/Tileset/Prefabs Juego/Assets/_Zombineta/Settings/Escenario.asset
git commit -m "El escenario se arma con prefabs: arte puede meterles luz"
```

---

### Task 3: Perfil de luz por nivel

**Modelo:** implementador sonnet · revisor sonnet

**Files:**
- Create: `Z/Scripts/Gameplay/Luz/PerfilDeLuz.cs`, `Z/Scripts/Gameplay/Luz/LightingDirector.cs`
- Create: `Z/Settings/Luz/Noche.asset`
- Modify: `Z/Scripts/Levels/LevelScene.cs` (campo `perfil`)
- Modify (MCP): `NivelBase.unity`, `Level_01.unity`, `Level_02.unity`
- Test: `Z/Tests/Editor/PerfilDeLuzTests.cs`

**Interfaces:**
- Consumes: nombres de capas de Task 1.
- Produces:
  - `PerfilDeLuz : ScriptableObject` con `[Serializable] class Ambiente { public string capa; public Color color = Color.white; [Min(0f)] public float intensidad = 1f; }`,
    `List<Ambiente> capas`, `Color colorGeneral`, `float intensidadGeneral`,
    `bool TryGet(string capa, out Color color, out float intensidad)`.
  - `LightingDirector : MonoBehaviour` (`[ExecuteAlways]`) con `[SerializeField] PerfilDeLuz perfil`,
    `public PerfilDeLuz Perfil { get; set; }`, que crea un hijo `Luz Global <capa>` con una
    `Light2D` global por entrada del perfil, más una general.
  - `LevelScene.Perfil` (propiedad) y campo serializado `perfil`.

- [ ] **Step 1: Test que falla**

```csharp
using NUnit.Framework;
using UnityEngine;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    public class PerfilDeLuzTests
    {
        PerfilDeLuz perfil;

        [SetUp]
        public void SetUp()
        {
            perfil = ScriptableObject.CreateInstance<PerfilDeLuz>();
            perfil.capas.Add(new PerfilDeLuz.Ambiente { capa = "Fondo", color = new Color(0.2f, 0.25f, 0.6f), intensidad = 0.35f });
            perfil.capas.Add(new PerfilDeLuz.Ambiente { capa = "Calle", color = new Color(0.3f, 0.3f, 0.5f), intensidad = 0.2f });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(perfil);

        [Test]
        public void ItFindsTheAmbientOfALayer()
        {
            Assert.IsTrue(perfil.TryGet("Calle", out var color, out float intensidad));
            Assert.AreEqual(0.2f, intensidad, 1e-4f);
            Assert.AreEqual(new Color(0.3f, 0.3f, 0.5f), color);
        }

        [Test]
        public void ALayerWithoutAmbient_IsNotLit()
        {
            Assert.IsFalse(perfil.TryGet("Frente", out _, out _));
        }

        [Test]
        public void TheLookupIsCaseSensitive_LikeSortingLayers()
        {
            Assert.IsFalse(perfil.TryGet("calle", out _, out _));
        }
    }
}
```

- [ ] **Step 2: Correr y ver que falla** (compilación: no existe `PerfilDeLuz`).

- [ ] **Step 3: Implementar `PerfilDeLuz`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>
    /// El ambiente de un nivel: cuanta luz general recibe cada capa de dibujo. Es lo que define si
    /// el nivel es noche cerrada o atardecer, y arte lo edita sin tocar la escena.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Perfil de luz", fileName = "PerfilDeLuz")]
    public sealed class PerfilDeLuz : ScriptableObject
    {
        [Serializable]
        public sealed class Ambiente
        {
            [Tooltip("Nombre de la Sorting Layer: Cielo, Fondo, Calle, Juego o Frente.")]
            public string capa = "Fondo";

            public Color color = Color.white;

            [Min(0f)] public float intensidad = 1f;
        }

        public List<Ambiente> capas = new List<Ambiente>();

        [Header("Luz general (todas las capas)")]
        public Color colorGeneral = Color.white;

        [Min(0f)] public float intensidadGeneral;

        public bool TryGet(string capa, out Color color, out float intensidad)
        {
            foreach (var a in capas)
                if (a != null && a.capa == capa)
                {
                    color = a.color;
                    intensidad = a.intensidad;
                    return true;
                }

            color = Color.black;
            intensidad = 0f;
            return false;
        }
    }
}
```

- [ ] **Step 4: Correr: PASS** (3 tests).

- [ ] **Step 5: `LightingDirector`** — `[ExecuteAlways]`, `[DefaultExecutionOrder(-150)]`:
  - `Rebuild()`: borra los hijos que creó (marcados con `HideFlags.DontSave` en edición) y crea uno
    por entrada del perfil: `GameObject("Luz Global " + capa)` con `Light2D` de tipo `Global`,
    `blendStyleIndex = 0` (Multiply), `color`, `intensity`, y solo esa capa en
    `Light2D.targetSortingLayers` (API pública, funciona igual en editor y en build; **no** usar
    `SerializedObject`, que solo existe en el editor y deja las luces sin capa en la build).
  - `Update`: si cambió el perfil o alguno de sus valores (comparar con un hash guardado:
    cantidad de capas, nombres, colores e intensidades), `Rebuild()`. Así arte ve el cambio en vivo.
  - Si `perfil == null`: ninguna luz global, y un `Debug.LogWarning` una sola vez.
- [ ] **Step 6: `Noche.asset`** con: `Cielo` (0.35, 0.35, 0.55) × 0.8; `Fondo` (0.22, 0.24, 0.5) ×
  0.35; `Calle` (0.28, 0.3, 0.5) × 0.25; `Juego` (0.3, 0.32, 0.5) × 0.3; `Frente` (0.1, 0.1, 0.2) ×
  0.15; general negro × 0.
- [ ] **Step 7: Escenas por MCP** — `LightingDirector` en el objeto `Nivel` de las tres escenas,
  `LevelScene.perfil = Noche.asset`, `LightingDirector.perfil` tomado de `LevelScene` en `Awake` si
  está vacío. Guardar. Captura en Play de `Level_01`: la escena se ve de noche, la moto y el faro
  se leen, el HUD no se oscurece (el Canvas es Overlay).

- [ ] **Step 8: Suite completa** (213) y commit

```bash
git add Juego/Assets/_Zombineta/Scripts Juego/Assets/_Zombineta/Settings/Luz Juego/Assets/_Zombineta/Scenes Juego/Assets/_Zombineta/Tests/Editor/PerfilDeLuzTests.cs
git commit -m "Perfil de luz por nivel, editable en vivo"
```

---

### Task 4: Presupuesto de luces y parpadeo (C# puro)

**Modelo:** implementador **haiku** (el código y los tests están completos acá) · revisor sonnet

**Files:**
- Create: `Z/Scripts/Gameplay/Luz/LightBudget.cs`, `Z/Scripts/Gameplay/Luz/FlickerMath.cs`
- Test: `Z/Tests/Editor/LightBudgetTests.cs`, `Z/Tests/Editor/FlickerMathTests.cs`

**Interfaces:**
- Produces:
  - `readonly struct LightCandidate { public readonly float x; public readonly int priority; public LightCandidate(float x, int priority); }`
  - `static void LightBudget.Choose(IReadOnlyList<LightCandidate> candidates, float cameraX, int budget, List<bool> result)`
    — `result` queda con un `bool` por candidato: `true` = prendida. Elige por prioridad más alta
    primero y, a igual prioridad, por cercanía a `cameraX`. `budget <= 0` apaga todo.
  - `static float FlickerMath.Intensity(float time, int seed, float frequency, float min, float max)`
    — valor entre `min` y `max`, continuo, distinto por semilla.

- [ ] **Step 1: Tests que fallan**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    public class LightBudgetTests
    {
        static List<bool> Choose(int budget, float cameraX, params LightCandidate[] candidates)
        {
            var result = new List<bool>();
            LightBudget.Choose(candidates, cameraX, budget, result);
            return result;
        }

        [Test]
        public void WithRoomForEveryone_TheyAllStayOn()
        {
            var r = Choose(5, 0f, new LightCandidate(1f, 0), new LightCandidate(-3f, 0));
            CollectionAssert.AreEqual(new[] { true, true }, r);
        }

        [Test]
        public void TheClosestOnesWin()
        {
            var r = Choose(2, 10f, new LightCandidate(40f, 0), new LightCandidate(11f, 0), new LightCandidate(9f, 0));
            CollectionAssert.AreEqual(new[] { false, true, true }, r);
        }

        [Test]
        public void PriorityBeatsDistance()
        {
            var r = Choose(1, 0f, new LightCandidate(50f, 3), new LightCandidate(1f, 0));
            CollectionAssert.AreEqual(new[] { true, false }, r);
        }

        [Test]
        public void ABudgetOfZero_TurnsEverythingOff()
        {
            var r = Choose(0, 0f, new LightCandidate(1f, 9), new LightCandidate(2f, 0));
            CollectionAssert.AreEqual(new[] { false, false }, r);
        }

        [Test]
        public void TheSameInput_GivesTheSameAnswer()
        {
            var a = Choose(2, 5f, new LightCandidate(4f, 0), new LightCandidate(6f, 0), new LightCandidate(5f, 0));
            var b = Choose(2, 5f, new LightCandidate(4f, 0), new LightCandidate(6f, 0), new LightCandidate(5f, 0));
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void ItReusesTheResultList()
        {
            var result = new List<bool> { true, true, true, true };
            LightBudget.Choose(new[] { new LightCandidate(0f, 0) }, 0f, 1, result);
            Assert.AreEqual(1, result.Count);
        }
    }

    public class FlickerMathTests
    {
        [Test]
        public void ItStaysBetweenMinAndMax()
        {
            for (float t = 0f; t < 5f; t += 0.05f)
            {
                float v = FlickerMath.Intensity(t, 7, 9f, 0.4f, 1f);
                Assert.GreaterOrEqual(v, 0.4f);
                Assert.LessOrEqual(v, 1f);
            }
        }

        [Test]
        public void TwoSeeds_DoNotBlinkTogether()
        {
            bool different = false;
            for (float t = 0f; t < 2f && !different; t += 0.05f)
                different = Mathf.Abs(FlickerMath.Intensity(t, 1, 9f, 0f, 1f) -
                                      FlickerMath.Intensity(t, 2, 9f, 0f, 1f)) > 0.05f;
            Assert.IsTrue(different);
        }

        [Test]
        public void FrequencyZero_IsASteadyLight()
        {
            Assert.AreEqual(FlickerMath.Intensity(0f, 3, 0f, 0.2f, 1f),
                            FlickerMath.Intensity(4f, 3, 0f, 0.2f, 1f), 1e-5f);
        }
    }
}
```

  (Agregar `using UnityEngine;` en el archivo de `FlickerMathTests` para `Mathf`.)

- [ ] **Step 2: Correr y ver que falla** (compilación).

- [ ] **Step 3: Implementar**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>Una luz de adorno candidata a quedar prendida.</summary>
    public readonly struct LightCandidate
    {
        public readonly float x;
        public readonly int priority;

        public LightCandidate(float x, int priority)
        {
            this.x = x;
            this.priority = priority;
        }
    }

    /// <summary>
    /// Cuales de las luces de adorno quedan prendidas cuando hay mas de las que la maquina banca.
    /// Manda la prioridad y despues la cercania a la camara: lo que esta encima del jugador se ve,
    /// lo lejano se apaga y nadie lo nota. C# plano para poder testearlo.
    /// </summary>
    public static class LightBudget
    {
        static readonly List<int> order = new List<int>();

        public static void Choose(IReadOnlyList<LightCandidate> candidates, float cameraX, int budget, List<bool> result)
        {
            result.Clear();
            for (int i = 0; i < candidates.Count; i++)
                result.Add(false);

            if (budget <= 0 || candidates.Count == 0)
                return;

            order.Clear();
            for (int i = 0; i < candidates.Count; i++)
                order.Add(i);

            order.Sort((a, b) =>
            {
                int byPriority = candidates[b].priority.CompareTo(candidates[a].priority);
                if (byPriority != 0)
                    return byPriority;
                float da = Mathf.Abs(candidates[a].x - cameraX);
                float db = Mathf.Abs(candidates[b].x - cameraX);
                int byDistance = da.CompareTo(db);
                return byDistance != 0 ? byDistance : a.CompareTo(b);
            });

            int n = Mathf.Min(budget, order.Count);
            for (int i = 0; i < n; i++)
                result[order[i]] = true;
        }
    }
}
```

```csharp
using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>
    /// El parpadeo de un neon o un farol viejo: dos ondas que no cierran entre si, para que no se
    /// note el ciclo, y un corrimiento por semilla para que dos carteles no titilen al unisono.
    /// </summary>
    public static class FlickerMath
    {
        public static float Intensity(float time, int seed, float frequency, float min, float max)
        {
            if (frequency <= 0f)
                return max;

            float phase = (seed * 0.6180339f) % 1f * 10f;
            float t = time * frequency + phase;
            float wave = Mathf.Sin(t) * 0.6f + Mathf.Sin(t * 1.7f) * 0.4f;   // en [-1, 1]
            return Mathf.Lerp(min, max, Mathf.InverseLerp(-1f, 1f, wave));
        }
    }
}
```

- [ ] **Step 4: Correr: PASS** (9 tests) y suite completa (222).

- [ ] **Step 5: Commit**

```bash
git add Juego/Assets/_Zombineta/Scripts/Gameplay/Luz Juego/Assets/_Zombineta/Tests/Editor/LightBudgetTests.cs Juego/Assets/_Zombineta/Tests/Editor/FlickerMathTests.cs
git commit -m "Presupuesto de luces y parpadeo, en C# plano con tests"
```

---

### Task 5: Luz de gameplay (faro, destellos, horda)

**Modelo:** implementador sonnet · revisor sonnet

**Files:**
- Create: `Z/Scripts/Gameplay/Luz/FlashDirector.cs`, `FlashConfig.cs`, `FlashLights.cs`, `HordeGlow.cs`
- Create: `Z/Settings/Luz/Destellos.asset`
- Modify: `Z/Scripts/Gameplay/Player/HeadlightView.cs` (capas a las que alcanza)
- Modify (MCP): las tres escenas de nivel
- Test: `Z/Tests/Editor/FlashDirectorTests.cs`

**Interfaces:**
- Consumes: `RunEvent` (`Shot`, `Crashed`, `RanOver`, `Explosion`), `RunController.Stepped`,
  `LaneSorting.GameLayer`.
- Produces:
  - `enum FlashKind { Shot, Crash, RanOver, Explosion }`
  - `FlashConfig : ScriptableObject` con `Entry { FlashKind kind; Color color; float intensity; float radius; float seconds; }`,
    `Entry Get(FlashKind)`, `void Set(FlashKind, Color, float intensity, float radius, float seconds)`.
  - `FlashDirector(FlashConfig config, int capacity = 8)` con `void Trigger(FlashKind kind, Vector2 at)`,
    `void Trigger(RunEvent events, Vector2 at)`, `void Tick(float dt)`, `int Count`,
    `bool TryGet(int i, out Vector2 at, out Color color, out float intensity, out float radius)`.

- [ ] **Step 1: Test que falla**

```csharp
using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    public class FlashDirectorTests
    {
        FlashConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<FlashConfig>();
            config.Set(FlashKind.Shot, Color.white, 1.5f, 2f, 0.06f);
            config.Set(FlashKind.Explosion, new Color(1f, 0.6f, 0.2f), 3f, 6f, 0.3f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void AFlash_StartsFull_AndFadesOut()
        {
            var d = new FlashDirector(config);
            d.Trigger(FlashKind.Explosion, new Vector2(10f, 1f));
            d.Tick(0f);
            Assert.AreEqual(1, d.Count);
            Assert.IsTrue(d.TryGet(0, out var at, out _, out float intensity, out float radius));
            Assert.AreEqual(new Vector2(10f, 1f), at);
            Assert.AreEqual(3f, intensity, 1e-4f);
            Assert.AreEqual(6f, radius, 1e-4f);

            d.Tick(0.15f);
            Assert.IsTrue(d.TryGet(0, out _, out _, out float half, out _));
            Assert.AreEqual(1.5f, half, 1e-3f);

            d.Tick(0.2f);
            Assert.AreEqual(0, d.Count);
        }

        [Test]
        public void SeveralFlashesLiveAtOnce()
        {
            var d = new FlashDirector(config);
            d.Trigger(FlashKind.Shot, Vector2.zero);
            d.Trigger(FlashKind.Explosion, new Vector2(5f, 0f));
            d.Tick(0f);
            Assert.AreEqual(2, d.Count);
        }

        [Test]
        public void RunEventsBecomeFlashes()
        {
            var d = new FlashDirector(config);
            d.Trigger(RunEvent.Explosion | RunEvent.PickedUp, new Vector2(3f, 0f));
            d.Tick(0f);
            Assert.AreEqual(1, d.Count);
        }

        [Test]
        public void AKindWithoutConfig_IsIgnored()
        {
            var d = new FlashDirector(config);
            d.Trigger(FlashKind.Crash, Vector2.zero);
            d.Tick(0f);
            Assert.AreEqual(0, d.Count);
        }

        [Test]
        public void OverCapacity_TheOldestIsDropped()
        {
            var d = new FlashDirector(config, capacity: 2);
            d.Trigger(FlashKind.Shot, new Vector2(1f, 0f));
            d.Trigger(FlashKind.Shot, new Vector2(2f, 0f));
            d.Trigger(FlashKind.Shot, new Vector2(3f, 0f));
            d.Tick(0f);
            Assert.AreEqual(2, d.Count);
            Assert.IsTrue(d.TryGet(0, out var first, out _, out _, out _));
            Assert.AreNotEqual(1f, first.x);
        }
    }
}
```

- [ ] **Step 2: Correr y ver que falla** (compilación).

- [ ] **Step 3: Implementar `FlashConfig` y `FlashDirector`** — mismo patrón que `RumbleConfig` /
  `RumbleDirector` (`Juego/Assets/_Zombineta/Scripts/Gameplay/Fx/`): anillo de `capacity` slots con
  `remaining`, `Tick` descuenta primero y mide después (`intensity = entry.intensity * remaining / seconds`),
  `Count` son los vivos y `TryGet` los recorre en orden de creación. `Trigger(RunEvent)` mapea
  `Shot → Shot`, `Crashed → Crash`, `RanOver → RanOver`, `Explosion → Explosion`.

- [ ] **Step 4: Correr: PASS** (5 tests) y suite completa (227).

- [ ] **Step 5: Vistas**
  - `FlashLights` (en el objeto `Nivel`): pool de `Light2D` puntuales apagadas (`capacity` del
    director), `blendStyleIndex = 1` (Additive), capas `Calle` y `Juego`. Se suscribe a
    `run.Stepped` y llama `director.Trigger(events, posición)`: la posición es la de la moto
    (`run.ToWorldX(state.PlayerX)`, `run.LaneToWorldY(state.LaneVisual)`) salvo para `Explosion` y
    `RanOver`, que usan los eventos de la horda si están disponibles, y si no la de la moto. Cada
    `Update`: `Tick(Time.unscaledDeltaTime)` y vuelca los vivos al pool.
  - `Destellos.asset`: Shot blanco 1.5 / radio 2 / 0.06 s; Crash (1, 0.85, 0.6) 1.2 / 3 / 0.12 s;
    RanOver (1, 0.3, 0.3) 1.2 / 2.5 / 0.15 s; Explosion (1, 0.6, 0.2) 3 / 6 / 0.3 s.
  - `HeadlightView`: sumar las capas `Calle` y `Juego` a su `Light2D` (por `targetSortingLayers`,
    igual que el director), sin tocar la lógica de intensidad ni de batería.
  - `HordeGlow` (en el objeto `Nivel`): `Light2D` global-free puntual grande que sigue
    `run.ToWorldX(state.HordeX)`; intensidad `Lerp(0, max, 1 - clamp01(gap / dangerGapMeters))`
    con `dangerGapMeters = 45` (el mismo número que el HUD); color verdoso frío.

- [ ] **Step 6: Verificar por MCP en Play** (callback antes de Play): forzar un choque y capturar:
  hay un destello; forzar una explosión de barril: destello naranja; con el faro prendido la calle y
  los zombies se aclaran y con el faro apagado no. Consola sin errores.

- [ ] **Step 7: Commit**

```bash
git add Juego/Assets/_Zombineta/Scripts Juego/Assets/_Zombineta/Settings/Luz Juego/Assets/_Zombineta/Scenes Juego/Assets/_Zombineta/Tests/Editor/FlashDirectorTests.cs
git commit -m "Luz de gameplay: faro real, destellos de eventos y resplandor de la horda"
```

---

### Task 6: Sombras y prefabs de los objetos del recorrido

**Modelo:** implementador sonnet · revisor sonnet

**Files:**
- Modify: `Z/Scripts/Levels/LevelItemPalette.cs` (campo `prefab` en `Look`)
- Modify: `Z/Scripts/Levels/LevelScene.cs` (instanciar el prefab del look)
- Create: `Z/Art/Level/Prefabs/{Obstaculo,Barril,Rampa,Nafta,Bateria,Balas}.prefab`
- Modify (MCP): `Z/Settings/LevelItemPalette.asset`, prefab `Zombie`, `Scooter` en las tres escenas,
  prefabs de tiles de farol/cartel/vallas

**Interfaces:**
- Consumes: contrato de prefab de Task 2; capas de Task 1.
- Produces: `LevelItemPalette.Look.prefab` (`GameObject`, opcional). Si está, `LevelScene` instancia
  el prefab como hijo del `LevelItem` (en editor con `HideFlags.DontSave`, en juego normal) y no
  pinta el sprite; si no está, todo sigue como hoy.

- [ ] **Step 1: `Look.prefab` y `LevelScene`** — en `ApplyVisual(item, index)`: si el look tiene
  prefab y el item no tiene ya una instancia de ese prefab, borrar la anterior e instanciar; ponerle
  capa `Juego` y el `sortingOrder` que hoy se le pone al `SpriteRenderer`; si no hay prefab, la rama
  actual sin cambios. La sombra de piso (`GroundShadow`) sigue igual.

- [ ] **Step 2: Prefabs de items** — uno por tipo, con el sprite actual de la paleta, material lit y
  `ShadowCaster2D` en obstáculo y barril. El barril suma una `Light2D` puntual tenue naranja
  (`DecorLight` con prioridad 1) para que se lea en la oscuridad.

- [ ] **Step 3: Sombras en personajes** — `ShadowCaster2D` con silueta rectangular en el prefab
  `Zombie` y en `Scooter` (las tres escenas). No proyectan sobre sí mismos (`selfShadows = false`).

- [ ] **Step 4: Sombras en el escenario cercano** — agregar `ShadowCaster2D` a los prefabs de
  farola, cartel, cartel de piso, señalización y valla.

- [ ] **Step 5: Verificar por MCP en Play**: con el faro prendido, la moto y los zombies proyectan
  sombra sobre la calle; captura del tramo. Los items del recorrido siguen en su carril y los
  consumidos siguen desapareciendo (el chequeo de `Level_01` que ya existe). Suite en verde (227).

- [ ] **Step 6: Commit**

```bash
git add Juego/Assets/_Zombineta/Scripts/Levels Juego/Assets/_Zombineta/Art/Level/Prefabs Juego/Assets/_Zombineta/Settings/LevelItemPalette.asset Juego/Assets/_Zombineta/Prefabs Juego/Assets/_Zombineta/Scenes Juego/Assets/_Zombineta/Art/Tileset/Prefabs
git commit -m "Sombras 2D y objetos del recorrido como prefabs"
```

---

### Task 7: Calidad Alta/Baja y aplicación del presupuesto

**Modelo:** implementador sonnet · revisor sonnet

**Files:**
- Create: `Z/Scripts/Gameplay/Luz/DecorLight.cs`, `Z/Scripts/Gameplay/Luz/LightBudgetRunner.cs`,
  `Z/Scripts/Gameplay/Luz/Flicker.cs`
- Modify: `Z/Simulacion/Runtime/Core/GameSettings.cs` (`LightQuality`)
- Modify: `Z/Scripts/Screens/OptionsScreen.cs`, `Z/Editor/SkeletonSceneBuilder.cs`
- Modify (MCP): `Z/Scenes/Options.unity`, las tres escenas de nivel

**Interfaces:**
- Consumes: `LightBudget.Choose`, `FlickerMath.Intensity` (Task 4).
- Produces:
  - `enum LightQuality { Alta, Baja }` en `Zombineta.Core`; `GameSettings.LightQuality`
    (default `Alta`, clave `zombineta.lightQuality`).
  - `DecorLight : MonoBehaviour` con `[SerializeField] int priority`, `Light2D Light`, que se
    registra en una lista estática al habilitarse (reiniciada en `SubsystemRegistration`).
  - `LightBudgetRunner : MonoBehaviour` con `[SerializeField] int budgetAlta = 12;`
    `[SerializeField] float intervalSeconds = 0.2f;` que aplica `LightBudget.Choose` sobre las
    `DecorLight` registradas usando la X de la cámara.
  - `Flicker : MonoBehaviour` (frecuencia, min, max, semilla) que aplica `FlickerMath` a su `Light2D`.

- [ ] **Step 1: `LightQuality` en `GameSettings`** siguiendo el patrón de `Vibration` (cacheado,
  `PlayerPrefs.SetInt`, `Save()`), guardando el enum como int.

- [ ] **Step 2: `DecorLight` + `LightBudgetRunner`** — el runner corre cada `intervalSeconds`
  (tiempo sin escalar), arma la lista de candidatos (`new LightCandidate(transform.position.x, priority)`)
  y prende o apaga cada `Light2D`. Con calidad `Baja` el presupuesto es 0. Reusa las listas: sin
  asignaciones por frame.

- [ ] **Step 3: Calidad en el resto** — con `Baja`: `FlashLights` no dispara destellos; los
  `ShadowCaster2D` de la escena se apagan (un componente `ShadowQuality` en el objeto `Nivel` que
  los recorre una vez al arrancar y cuando cambia la opción); el ambiente y el faro no cambian.

- [ ] **Step 4: Opciones** — desplegable "Iluminación" con `Alta` / `Baja`
  (`TMP` no se usa en este proyecto: es un `Dropdown` de uGUI, como los `Toggle` existentes),
  cableado en `OptionsScreen` (`onValueChanged` → `GameSettings.LightQuality`, y
  `SetValueWithoutNotify` en `OnEnable`), sumado a `SkeletonSceneBuilder.BuildOptions()` y a la
  cadena de navegación (arriba/abajo) igual que "Vibración". Al cambiarlo, avisar a los
  componentes vivos (evento estático `GameSettings.LightQualityChanged`).

- [ ] **Step 5: Verificar por MCP en Play**: en `Alta`, con más luces de adorno visibles que el
  tope, quedan prendidas exactamente `budgetAlta` y son las más cercanas; al pasar a `Baja` se
  apagan todas las de adorno y las sombras, y el faro sigue. Capturas de los dos estados.

- [ ] **Step 6: Suite completa** (227) y commit

```bash
git add Juego/Assets/_Zombineta/Scripts Juego/Assets/_Zombineta/Simulacion/Runtime/Core/GameSettings.cs Juego/Assets/_Zombineta/Editor/SkeletonSceneBuilder.cs Juego/Assets/_Zombineta/Scenes
git commit -m "Calidad de iluminacion Alta/Baja y presupuesto de luces aplicado"
```

---

### Task 8: Taller de luz

**Modelo:** implementador sonnet · revisor sonnet

**Files:**
- Create: `Z/Editor/Luz/TallerBuilder.cs`
- Create: `Z/Scenes/TallerLuz.unity` (generada)

**Interfaces:**
- Consumes: `SceneryVariant.prefab`/`grupo`, `LevelItemPalette.Look.prefab`, `PerfilDeLuz`,
  `LightingDirector`.
- Produces: menú `Zombineta/Luz/Construir taller` (regenera pisando la escena, con confirmación).

- [ ] **Step 1: Implementar** — crea la escena, y por cada capa del escenario una fila a la altura
  `baselineY` de esa capa, ordenada por `grupo`, con una instancia de cada prefab separada 2 u,
  cada una con un cartel (`TextMesh` o un `Text` en un Canvas World) con `nombre del prefab` y
  `grupo`. Abajo de todo, una fila con los prefabs de `LevelItemPalette`. Un objeto `Ambiente` con
  `LightingDirector` y `Noche.asset`. Una copia de `Scooter` con su faro a la izquierda para
  comparar escala. Cámara ortográfica encuadrando la primera fila. Guarda la escena y la registra
  en Build Settings **desactivada** (no tiene que ir al build).

- [ ] **Step 2: Verificar por MCP**: construir el taller, abrirlo, capturar la Scene view (render
  de cámara, sin enfocar ventanas): se ven todas las filas con sus carteles, iluminadas por el
  perfil. Cambiar `intensidadGeneral` del perfil y ver que cambia en vivo. Regenerar dos veces
  seguidas no duplica objetos.

- [ ] **Step 3: Commit**

```bash
git add Juego/Assets/_Zombineta/Editor/Luz Juego/Assets/_Zombineta/Scenes/TallerLuz.unity* Juego/ProjectSettings/EditorBuildSettings.asset
git commit -m "Taller de luz: una escena con todos los prefabs para que arte trabaje"
```

---

### Task 9: Medición de rendimiento y capturas

**Modelo:** implementador sonnet · sin revisor (es verificación; su salida la usa la revisión final)

**Files:**
- Create: `.git/sdd/luz/*.png` y `.git/sdd/luz/medicion.md` (fuera del repo del juego)

- [ ] **Step 1: Medir** — en Play sobre `Level_01`, mismo tramo (moto en 300 m, horda a 20 m),
  medir el tiempo de cuadro promedio de 300 cuadros en: (a) `Alta`, (b) `Baja`, (c) con el faro
  prendido y apagado en `Alta`. Usar `Time.unscaledDeltaTime` acumulado en el callback.

- [ ] **Step 2: Línea de base** — la medición del mismo tramo **sin luces** se toma en la Task 1,
  Step 1, antes de tocar nada (mismo procedimiento, 300 cuadros), y se anota ahí mismo en
  `.git/sdd/luz/medicion.md`. Acá se compara contra ese número.

- [ ] **Step 3: Escribir `medicion.md`** con los números, la máquina donde se corrió y las capturas
  de cada caso.

---

### Task 10: Guía para arte y HANDOFF

**Modelo:** implementador sonnet · revisor sonnet

**Files:**
- Create: `docs/Guia-de-iluminacion-para-arte.md` y `docs/img/luz/*.png` (capturas)
- Modify: `HANDOFF.md`

- [ ] **Step 1: Guía** — los 10 puntos de la sección 8 del spec, en castellano rioplatense, con
  capturas del editor generadas por MCP (render de cámara o de la Scene view; no enfocar ventanas):
  el taller, el inspector de un prefab con su `Light2D`, el `PerfilDeLuz`, y el mismo asset en
  `Alta` y en `Baja`. Cada paso con su ruta exacta y el nombre del menú.

- [ ] **Step 2: HANDOFF** — sección "Iluminación (`Juego/`)": capas de dibujo, prefabs de escenario
  y de items, perfil por nivel, luces de gameplay, presupuesto y calidad, taller, y un puntero a la
  guía de arte. Tabla de sub-proyectos: 5 **Hecho**, 6 (tramos por ambiente) próximo. Trampas
  nuevas. Sección 6: verificado y no verificado, con los números de la medición.

- [ ] **Step 3: Commit**

```bash
git add docs/Guia-de-iluminacion-para-arte.md docs/img/luz HANDOFF.md
git commit -m "docs: guia de iluminacion para arte y handoff"
```
