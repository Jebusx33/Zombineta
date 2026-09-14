# Niveles armados a mano — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Niveles de `Juego/` jugables, armados a mano sobre la escena con una paleta, y un
generador que da la primera pasada y respeta lo ajustado.

**Architecture:** La escena es el nivel (`LevelItem` + `LevelScene`); la conversión a datos y el
generador son C# plano con tests; la parte visual y la paleta son MonoBehaviour y editor. El
gameplay se trae del prototipo copiando scripts y assets con sus `.meta`.

**Tech Stack:** Unity 6000.6.0f1, URP 2D, uGUI, Input System, `UnityEditor.Overlays`, NUnit, MCP.

Spec: `docs/superpowers/specs/2026-09-13-niveles-a-mano-design.md`.

**Nota:** a diferencia de los planes anteriores, el código de cada task se escribe directo en los
archivos (es mayormente port del prototipo y herramientas de editor); el plan fija archivos,
interfaces y verificaciones.

## Global Constraints

- Unity abierto en `Juego/`. Copias desde `Prototipo/` **siempre con `.meta`**.
- Estáticos reiniciados en `SubsystemRegistration` (recarga de dominio desactivada).
- Assets en editor: cargarlos justo antes de asignarlos si hubo `NewScene`/`OpenScene` (trampa #23).
- Commits con `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`.

---

### Task 1: Traer el gameplay del prototipo

- [ ] Copiar con `.meta` a `Juego/Assets/_Zombineta/Scripts/Gameplay/`: `Core/{RunController,CameraFollow}`,
  `Player/{ScooterView,HeadlightView,WheelDustView,PlayerInputReader}`, `Enemies/HordeView`,
  `Fx/FxManager`, `Level/{GoalView,LaneMarkersView}`, `Scenery/SceneryManager`,
  `UI/{HudView,DebugHudView}`.
- [ ] Copiar con `.meta`: `Art/` (todo), `Prefabs/` (todo), `Settings/{GameConfig,CameraConfig,Zombies,Escenario,Ruta01,Ruta02}`,
  y `Scenes/Prototipo.unity` → `Scenes/Templates/NivelBase.unity`.
- [ ] `Zombineta.Juego.asmdef`: sumar `Unity.RenderPipelines.Universal.Runtime` y
  `Unity.RenderPipelines.Universal.2D.Runtime`.
- [ ] Compilar sin errores; 141 tests en verde.

### Task 2: `LevelLayout`, `LevelItem`, `LevelItemPalette`, `LevelScene`, `LevelFlowBridge`

**Files (en `Juego/Assets/_Zombineta/Scripts/Levels/`):** `LevelLayout.cs`, `LevelItem.cs`,
`LevelItemPalette.cs`, `LevelScene.cs`, `LevelFlowBridge.cs`; test `Tests/Editor/LevelLayoutTests.cs`.

**Interfaces:**
- `LevelLayout(float worldUnitsPerMeter, float laneSpacing, float jumpHeightToWorld)`:
  `ToWorldX(m)`, `ToMeters(x)`, `LaneY(lane)`, `NearestLane(y)`, `SnapMeters(m, step)`,
  `ItemPosition(meters, lane, height) : Vector2`.
- `LevelItem : MonoBehaviour`: `kind`, `lane`, `variant`, `height`, `generated`, `pinned`,
  `generatedMeters`, `generatedLane`; `Meters` (desde la X), `ToEntry(LevelLayout) : LevelEntry`,
  `IsTouchedSinceGeneration`.
- `LevelScene : MonoBehaviour` (`[DefaultExecutionOrder(-200)]`, `[ExecuteAlways]`):
  `Config`, `Palette`, `GoalDistance`, `Items`, `BuildDefinition() : LevelDefinition`,
  `ApplyVisual(LevelItem)`, `Layout`.
- `RunController`: `[SerializeField] LevelScene levelScene`; en `Awake`, si hay escena, usa
  `levelScene.BuildDefinition()`.
- `LevelFlowBridge`: `run`, `cameraRig`, `scooter`, `pauseAction`, `characterColors`.

- [ ] Tests de `LevelLayout` (ida y vuelta metros/mundo, carril más cercano, grilla).
- [ ] Implementar y compilar; tests en verde.

### Task 3: Escenas de nivel

- [ ] Por MCP, abrir `NivelBase`: borrar `ScreenFlow` y paneles de menú, quitar scripts faltantes,
  dejar el HUD activo, crear `LevelItemPalette.asset` con los sprites/colores de `LevelSpawner`,
  agregar `LevelScene` y `LevelFlowBridge` cableados, `RunController.levelScene`,
  `QuickRestartEnabled` apagado. Guardar la plantilla.
- [ ] Generar `Level_01` y `Level_02` desde la plantilla, creando un `LevelItem` por entrada de
  `Ruta01`/`Ruta02` bajo `Recorrido`.
- [ ] Verificar: sin scripts faltantes; Play desde `Level_01` corre la partida; F2 → nivel
  completo; recorrido Boot → menú → personaje → cinemática → Level_01 → pausa → seguir; items
  consumidos se apagan.
- [ ] Commit.

### Task 4: Generador

**Files:** `Scripts/Levels/LevelGenerator.cs` (+ `LevelGeneratorSettings` serializable),
`Tests/Editor/LevelGeneratorTests.cs`.

- [ ] Tests: determinismo por semilla; nunca tres carriles tapados a ±3 m; aterrizaje de rampa
  libre; barriles fuera de piezas de rampa; separación mínima en carril; no ubica nada a menos de
  la separación de un item fijado; respeta el largo.
- [ ] Implementar; tests en verde. Commit.

### Task 5: Herramienta de editor

**Files (en `Juego/Assets/_Zombineta/Editor/Levels/`):** `LevelPaletteOverlay.cs`,
`LevelItemSnapping.cs`, `LevelEditorActions.cs` (generar, fijar, probar desde acá), y
`OnDrawGizmos` en `LevelScene` para guías y validaciones; `LevelScene` aplica la distancia de
"Probar desde acá" al entrar en Play desde el editor.

- [ ] Verificar por MCP: colocar un item por la API de la herramienta y que quede enganchado;
  mover un item generado lo fija; regenerar conserva los fijados; Probar desde acá a 1500 m arranca
  la moto ahí. Captura de la Scene view con guías.
- [ ] Commit.

### Task 6: Documentación y subida

- [ ] HANDOFF (cómo armar un nivel, paleta, generador, fijados, probar desde acá, trampas nuevas),
  tabla de sub-proyectos. Commit, push a `Jose`, merge a `master`.
