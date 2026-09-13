# Reestructura: prototipo, juego definitivo y simulación compartida — Diseño

Fecha: 13 de septiembre de 2026. Aprobado por José en la sesión del 13/09.
Sub-proyecto 1 de 5 del paso al juego definitivo (los otros: escenas por pantalla y nivel,
herramienta de niveles a mano, estética nueva, iluminación 2D real).

## Decisiones

- **El prototipo queda congelado como entregable de E2** (9/10). Solo recibe arreglos de bugs y
  balance para los playtests. Tag `prototipo-v1` = estado al cerrar la etapa.
- **Un repo, dos proyectos Unity y la simulación compartida** como paquete local.
- **Unity 6000.6.0f1** para los dos proyectos.

## Estructura

```
Zombineta/                          raíz del repo
  Prototipo/                        el proyecto de hoy (Assets, Packages, ProjectSettings, Tools)
  Juego/                            proyecto nuevo, 2D URP, con una escena Boot vacía
  Paquetes/com.zombineta.simulacion/
    package.json
    Runtime/                        Zombineta.Simulacion.asmdef
    Tests/Editor/                   Zombineta.Simulacion.Tests.asmdef (los 125 tests)
  docs/  HANDOFF.md  README.md  .gitignore  .gitattributes
```

Los dos proyectos referencian el paquete con
`"com.zombineta.simulacion": "file:../../Paquetes/com.zombineta.simulacion"` y lo declaran en
`testables` para que sus tests aparezcan en el Test Runner.

## Qué va al paquete

Todo lo que no es MonoBehaviour, con sus `.meta` (los assets encuentran los scripts por GUID):

| Carpeta | Archivos |
|---|---|
| Core | `GameConfig`, `GameSettings`, `IBarrelField`, `RunSimulation`, `RunState` |
| Player | `PlayerIntent` |
| Enemies | `HordeSimulation`, `ZombieRoster`, `ZombieType` |
| Level | `LevelDefinition`, `LevelRuntime` |
| Flow | `GameFlow`, `LevelSequence` |
| CameraFx | `CameraConfig`, `CameraDirector` |
| Scenery | `ParallaxMath`, `SceneryLayout`, `SceneryTileset` |

Los namespaces no cambian. Queda en el prototipo todo MonoBehaviour: `RunController`,
`CameraFollow`, vistas, UI, `SceneryManager`, `FxManager`, `WheelDustView`, etc. Su asmdef
(`Zombineta`) pasa a referenciar `Zombineta.Simulacion`.

## Juego/

- Mismos `Packages/manifest.json` y `ProjectSettings/` que el prototipo (URP 2D ya configurado),
  más el paquete de simulación.
- `Assets/Settings/` con `UniversalRP.asset`, `Renderer2D.asset` y la configuración global de URP
  copiados tal cual del prototipo, con sus `.meta`: `ProjectSettings/GraphicsSettings` los
  referencia por GUID, y los dos proyectos no comparten `Assets/`, así que no chocan.
- Una escena `Assets/_Zombineta/Scenes/Boot.unity` vacía, registrada en Build Settings.

## Verificación

- **Prototipo:** compila, 125 tests en verde (ahora desde el paquete), y en Play la partida
  arranca, la horda se mueve y el disparo mata (mismo comportamiento que `prototipo-v1`).
- **Juego:** abre sin errores, compila el paquete y corre los mismos 125 tests.
- `GameConfig.asset`, `Ruta01/02`, `Zombies.asset`, `Escenario.asset`, `CameraConfig.asset` y
  `Niveles.asset` siguen resolviendo su script (sin "missing script").

## Riesgos

- **Referencias rotas** en assets o escena si un `.cs` se mueve sin su `.meta`. Mitigación:
  `git mv` de pares `.cs` + `.meta` y verificación de "missing script" en Play.
- **El MCP puede pedir aprobar de nuevo la conexión** al abrir el proyecto desde otra ruta
  (Project Settings > AI > Unity MCP). Es un clic del usuario.
- **Ramas del equipo:** después del movimiento conviene recrearlas desde `master`.
