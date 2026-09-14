# Niveles armados a mano — Diseño

Fecha: 13 de septiembre de 2026. Aprobado por José en la sesión del 13/09.
Sub-proyecto 3 de 5 del juego definitivo. Proyecto: `Juego/`.

## Alcance

Armar un nivel a mano **y jugarlo**, con un generador automático como primera pasada que
**respeta lo ajustado a mano** al regenerar. Fuera: estética nueva (tramo 4), luz 2D (tramo 5),
escenario ubicado a mano, arte final de los objetos.

## Decisiones

- **La escena es el nivel.** Cada objeto del recorrido es un GameObject con `LevelItem`. Al
  arrancar, `LevelScene` convierte sus items en el `LevelDefinition` que usa la simulación. No hay
  exportación ni asset intermedio: lo que se ve es lo que se juega.
- **Regenerar respeta lo tocado.** Un item agregado a mano, o uno generado que se movió o editó,
  queda fijado. Regenerar solo reemplaza los items generados sin tocar.
- **Edición con paleta flotante en la Scene view**, enganche a carril y a grilla de 1 m.

## Etapa 3a — Un nivel jugable en `Juego/`

- Se traen del prototipo, **con sus `.meta`**: las vistas (`RunController`, `CameraFollow`,
  `ScooterView`, `HeadlightView`, `WheelDustView`, `PlayerInputReader`, `HordeView`, `FxManager`,
  `GoalView`, `LaneMarkersView`, `SceneryManager`, `HudView`, `DebugHudView`), el arte, los
  prefabs y los assets de configuración (`GameConfig`, `CameraConfig`, `Zombies`, `Escenario`,
  `Ruta01`, `Ruta02`). No se traen `ScreenFlow`, `MenuList` ni `LevelSpawner`.
- La escena del prototipo se copia como plantilla (`Scenes/Templates/NivelBase.unity`) y se le
  quitan los paneles de menú y `ScreenFlow`. De ella salen `Level_01` y `Level_02`.
- **`LevelItem`** (en cada objeto): `kind`, `lane`, `variant`, `height`, `generated`, `pinned`,
  y la posición con la que lo dejó el generador. Su X en el mundo son los metros del recorrido.
- **`LevelScene`** (raíz del nivel): `GameConfig`, `LevelItemPalette`, `goalDistance`, parámetros
  del generador. `BuildDefinition()` junta los items activos en un `LevelDefinition` en memoria.
  Mantiene la vista de cada item (sprite, color, escala y posición según su tipo) y los apaga
  cuando la simulación los consume.
- **`LevelLayout`** (C# plano, testeado): metros ↔ X de mundo, carril ↔ Y, enganche a carril y a
  grilla, altura de pickups aéreos.
- **`RunController`** toma el recorrido de `LevelScene` si lo tiene.
- **`LevelFlowBridge`**: los finales de cámara (atrapada / refugio) y avisar al flujo; la acción
  Pausa; F2 y F3 en editor; el color del personaje elegido.
- **`LevelItemPalette`** (ScriptableObject): sprite, color y escala por tipo (lo que tenía
  `LevelSpawner`).
- Contenido inicial: `Level_01` y `Level_02` con los recorridos de `Ruta01` y `Ruta02` convertidos
  a items (después se regeneran o ajustan).

## Etapa 3b — La herramienta

- **`LevelGenerator`** (C# plano, testeado): semilla, largo y densidades por tipo → lista de
  entradas. Reglas: piezas de rampa con aterrizaje libre, barriles fuera de las rampas, zombies de
  frente lejos de obstáculos del mismo carril, nunca tres carriles tapados a ±3 m, separación
  mínima de 2 m en un carril. Recibe los items fijados y no ubica nada encima de ellos.
- **Paleta** (`Overlay` de la Scene view, solo con un `LevelScene` en la escena): un botón por
  tipo que coloca el item en el carril y la distancia bajo el mouse; Generar/Regenerar; Fijar /
  Desfijar selección; deslizador de distancia; **Probar desde acá**.
- **Enganche:** al mover items con las herramientas de Unity, la X se redondea a 1 m y la Y al
  carril más cercano. Mover o editar un item generado lo fija.
- **Guías** (`OnDrawGizmos` de `LevelScene`): los 3 carriles, regla cada 50 m, largada, refugio,
  y en rojo las validaciones (tres carriles tapados, rampa sin aterrizaje, items pisados).
- **Probar desde acá:** guarda la distancia en `EditorPrefs` y entra en Play; en editor,
  `LevelScene` arranca la moto ahí (y la horda con la ventaja inicial detrás).

## Pruebas

- EditMode: `LevelLayout`, `LevelGenerator` (reglas, determinismo por semilla, no pisa fijados).
- Play por MCP: una partida en `Level_01` dentro del flujo (ganar con F2 → nivel completo);
  items consumidos se apagan; Probar desde acá a mitad de nivel; regenerar con items fijados.
