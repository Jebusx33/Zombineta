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
| `JefeSimulation` | El duelo en moto con el jefe: patrón, ataques, vida y corazones |
| `JefeConfig` | ScriptableObject con los números del duelo. El nivel que lo asigna tiene jefe |

### Capa Unity (`Scripts/`, namespace `Zombineta.Core` / `Zombineta.UI` / etc.)

| Clase | Rol |
|---|---|
| `RunController` | **Puente principal**. Lee input, llama `Tick`, publica `RunEvent`. `[DefaultExecutionOrder(-100)]`. Expone `Sim`, `Level`, `Config`. |
| `GameRoot` | Singleton en `Boot`. Crea `GameFlow`, ejecuta planes de escena, gestiona fundidos. |
| `Bootstrapper` | Carga `Boot` si no está cargada al entrar en Play desde cualquier escena. |
| `SceneRoutePlanner` | C# puro: calcula qué escenas cargar/descargar para cada transición. |
| `HudView` | Le pasa el estado a `HudPaneles` (prefab `HudPartida`: recursos, armamento y barra de avance) y maneja el sensor de la horda, oculto por ahora. |
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

El prefab `Prefabs/HudPartida.prefab` se usa en todos los niveles y se edita en un solo lugar:

| Campo de `HudPaneles` | Qué muestra |
|---|---|
| `nafta`, `bateria` | Anillos. Cambian de color desde el 50 % y titilan en el último 20 % |
| `balas`, `balasMaximo` | Balas actuales y máximas, con dígitos en imagen |
| `granadas`, `bombas` | Icono y cantidad. A pie, una oleada didáctica hace latir el arma pedida |
| `progreso`, `nodos`, `marcador` | Barra de avance: se pintan los puntos que se pasan |

`threatFill` y `threatDistance` siguen en `HudView`. El sensor se muestra con `mostrarSensor`.

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

El jefe se activa con el campo `Jefe` de `LevelScene`. Hoy solo lo usa `Level_02`, con
`Settings/JefeNivel2.asset`. Lo que se ve está en `VistaDelJefe` y `HudJefe`, y los dibujos en
`Settings/AnimacionesJefeNivel2.asset`.

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

## Clima, estilo de imagen y transiciones

- **Clima** (`Scripts/Gameplay/Clima/ClimaDirector`): lluvia con salpicaduras, tres bancos de niebla, papeles
  al viento y árboles que se mecen. Cada tramo de `LevelScene` tiene `lluvia`, `niebla` y `viento` (0 a 1) y
  entran con el mismo fundido que el apagón. Es solo dibujo: no cambia reglas. Se crea solo al dar Play; en
  calidad Baja usa menos partículas y el faro no se ve en el aire.
- **Estilo** (`Scripts/Gameplay/Estilo/EstiloDeImagen`): blanco y negro o película vieja, con el campo `Estilo`
  de `LevelScene`. Es un Volume de post-processing armado por código (color, grano, viñeta) más una capa con
  rayas y polvo (`Resources/Estilo/PeliculaVieja.shader`). El HUD no cambia. Al ser atrapada, el color se va.
  F6 recorre los estilos en el editor.
- **Iris** (`ScreenFader` + `Resources/Estilo/Iris.shader`): al entrar y salir de un nivel la pantalla se
  cierra en un círculo sobre la protagonista. Se apaga con `irisEnLosNiveles` de `GameRoot` (escena Boot).

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
