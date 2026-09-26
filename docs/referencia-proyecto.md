# Zombineta — Referencia del proyecto

_Última actualización: 2026-09-25_

---

## Concepto

Juego 2D de acción lateral (estilo Excitebike) para Unity 6. Una repartidora en moto huye de
una horda de zombies. El jugador maneja carriles, velocidad, nafta, batería y balas para llegar
al refugio antes de ser alcanzado. Plataforma objetivo: PC y gamepad.

---

## Repositorio

```
Zombineta/
  Prototipo/          proyecto Unity congelado (entregable E2, tag prototipo-v1)
  Juego/              proyecto activo, Unity 6000.6.0f1, 2D URP
    Assets/_Zombineta/
      Art/            sprites, HUD, iluminación
      Audio/          clips, buses, directors
      Editor/         herramientas de editor (SliceSpriteSheet, LevelEditor, etc.)
      Prefabs/
      Scenes/         Boot, MainMenu, CharacterSelect, Cinematic, Tutorial,
                      Level_01, Level_02, Pause, Options, LevelComplete,
                      GameOver, Ending, Credits, TallerLuz
      Scripts/        MonoBehaviours (ver secciones abajo)
      Simulacion/     lógica pura sin MonoBehaviour (tests EditMode)
      Settings/       ScriptableObjects (GameConfig, TutorialConfig, etc.)
  docs/               diseños, guías, planes
```

La simulación vive en `Juego/Assets/_Zombineta/Simulacion/` (no en un paquete externo;
decisión tomada para que el MCP pueda verificar en Play).

---

## Arquitectura

### Simulación (`Simulacion/Runtime/`, namespace `Zombineta.Core`)

C# puro, sin MonoBehaviour. Se instancia, se le llama `Tick(intent, dt)` y se lee el resultado.
Testeada con ~280+ tests EditMode.

| Clase | Rol |
|---|---|
| `RunSimulation` | Toda la lógica del juego: nafta, batería, balas, carriles, salto, horda |
| `RunState` | Estado mutable de la partida (Fuel, Battery, Ammo, Lane, Phase…) |
| `RunEvent` | Flags de lo que pasó en el tick (Shot, Landed, Lost, Won…) |
| `GameConfig` | ScriptableObject con todos los parámetros numéricos del juego |
| `HordeSimulation` | Horda como colección de individuos con posición y carril |
| `LevelRuntime` | Recorre el `LevelDefinition` y detecta colisiones con pickups/barriles/rampas |
| `LevelDefinition` | ScriptableObject que describe el recorrido (generado o a mano) |

### Capa Unity (`Scripts/`, namespace `Zombineta.Core` / `Zombineta.UI` / etc.)

| Clase | Rol |
|---|---|
| `RunController` | **Puente principal**. Lee input, llama `Tick`, publica `RunEvent`. `[DefaultExecutionOrder(-100)]`. Expone `Sim`, `Level`, `Config`. |
| `GameRoot` | Singleton en `Boot`. Crea `GameFlow`, ejecuta planes de escena, gestiona fundidos. |
| `Bootstrapper` | Carga `Boot` si no está cargada al entrar en Play desde cualquier escena. |
| `SceneRoutePlanner` | C# puro: calcula qué escenas cargar/descargar para cada transición. |
| `HudView` | HUD con barras filled (`fuelFill`, `batteryFill`, `progressFill`, `threatFill`) y pips de balas. Lee de `RunController.Sim.State` en `LateUpdate`. |
| `DebugHudView` | HUD de texto (`OnGUI`) para balanceo. Toggle con F1. |
| `CameraFollow` | Sigue al jugador con lerp. |
| `HordeView` | Dibuja los zombies de la horda. |
| `SceneryManager` | Parallax y tiles de fondo. |
| `FxManager` | Efectos visuales reactivos a `RunEvent`. |
| `TutorialDirector` | Encadena los pasos del tutorial, controla la horda y los recursos. |

### Flujo de pantallas

```
Boot (siempre cargada)
  └─ MainMenu ─► CharacterSelect ─► Tutorial? ─► Cinematic ─► Level_01 ─► LevelComplete ─►
     ─► Cinematic ─► Level_02 ─► Ending ─► MainMenu
         (capas: Pause [100], Options [200], Fader [1000])
```

`GameRoot` ejecuta los `ScenePlan` que devuelve `SceneRoutePlanner` basándose en el estado de
`GameFlow`.

---

## HUD (`HudView`)

Barras en modo `Image.Filled`. Todas las referencias son `[SerializeField]`:

| Campo | Qué muestra |
|---|---|
| `fuelFill` | Nafta. Color: naranja → rojo al bajar del 30 % |
| `batteryFill` | Batería |
| `progressFill` | Avance hacia la meta |
| `threatFill` | Amenaza de la horda (invertida: lleno = encima) |
| `ammoPips` | Array de `Image`; se deshabilitan a medida que se gastan las balas |

**Requisito crítico:** el campo `run` (tipo `RunController`) debe estar asignado en el
Inspector. Si es null, `LateUpdate` sale inmediatamente y las barras no se actualizan.

El `TutorialDirector` usa las propiedades de solo lectura `FuelBarRect`, `BatteryBarRect`,
`ThreatBarRect` y `AmmoPipRects` para resaltar la barra del paso activo.

---

## Niveles

`Level_01` y `Level_02` se arman a mano con `LevelItem` bajo el GameObject `Nivel` en la
escena. `LevelScene` (componente en la escena) construye el `LevelDefinition` y lo pasa a
`RunController` en `Awake`.

El editor `Zombineta > Nivel` provee paleta de objetos y acciones de nivel.

---

## Audio

Sistema propio con buses (`AudioBuses`), director de música (`MusicDirector`), director de SFX
(`SfxDirector`) y tono del motor (`MotorTono`). Los sonidos están en
`Assets/_Zombineta/Audio/`. Las reglas de importación se aplican automáticamente con
`AudioImportRules`.

---

## Iluminación

2D URP con perfil de noche. Luces decorativas con flicker (`DecorLight`, `Flicker`). Un sistema
de presupuesto de luces (`LightBudget`) limita la cantidad activa. `TallerLuz.unity` es una
escena de prueba de iluminación.

---

## Tests

~280+ tests EditMode en `Simulacion/Tests/Editor/` y `Tests/Editor/`. Cubren simulación,
niveles, audio, input, cámara, enemigos y tutorial. Se corren con el Test Runner de Unity.

---

## Patrones recurrentes

- **Toda lógica fuera de MonoBehaviour**: la simulación es C# puro para poder testearla sin
  escena y sin el editor abierto.
- **Vistas solo leen**: `HudView`, `HordeView`, `ScooterView`, etc. no escriben estado; solo
  consumen `RunState` y `RunEvent`.
- **RunController como único puente**: es el único MonoBehaviour que llama a `Sim.Tick`. Las
  vistas reciben el `RunController` por referencia serializada y leen de `run.Sim.State`.
- **ScriptableObjects para configuración**: `GameConfig`, `TutorialConfig`, `TutorialPasos`,
  `BancoDeSonidos`, `MezclaAudio`, etc.
- **Wiring en escena**: las referencias entre componentes se asignan en el Inspector, no se
  buscan con `FindObjectOfType`. Un campo `{fileID: 0}` en el YAML de la escena indica una
  referencia rota.

---

## Historial de issues conocidos

| Escena | Componente | Campo | Síntoma | Fix |
|---|---|---|---|---|
| `Level_01` | `HudView` | `run` | Barras del HUD no se actualizan | Asignar `RunController` (fileID 1170739137) |
