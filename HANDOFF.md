# Zombineta — Handoff

Última actualización: 14 de septiembre de 2026, tras el sub-proyecto 4: la escala y el encuadre
del juego definitivo calzan con la referencia de arte (carriles abajo, más fondo, personajes
grandes), con zombies por arquetipo y sombras por carril.

**Estado:** el prototipo se recorre de punta a punta con el flujo del GDD (menú, opciones,
personaje, cinemática, dos niveles, victoria, game over, final), sobre un escenario de
placeholders con parallax en varias capas, una cámara que reacciona a la persecución y rampas
con un salto que se regula inclinando en el aire, y una horda de zombies individuales
con tipos y arte propio por arquetipo. 186 tests EditMode en verde.

**Ojo:** con el balance vigente la meta **no se alcanza jugando** (ver sección 4). Para
recorrer el flujo completo existen F2 (ganar) y F3 (perder), solo en editor y builds de
desarrollo.

---

## 0. Estructura del repo (desde el 13/09)

```
Zombineta/
  Prototipo/                          proyecto Unity del prototipo (congelado para E2)
  Juego/                              proyecto Unity del juego definitivo (en construcción)
    Assets/_Zombineta/Simulacion/     la simulación + sus 125 tests, compartida
  docs/superpowers/specs|plans/       diseño y plan de cada etapa
```

- **El prototipo está congelado** como entregable de E2 (9/10): solo arreglos de bugs y de
  balance para los playtests. El estado al cerrar la etapa es el tag **`prototipo-v1`**.
- **La simulación vive dentro de `Juego/`** y el prototipo la importa como paquete local
  (`Prototipo/Packages/manifest.json`: `file:../../Juego/Assets/_Zombineta/Simulacion`). Un
  arreglo de reglas o de balance se hace una vez y llega a los dos. Por qué adentro de
  `Juego/Assets` y no en una carpeta propia: ver la trampa #20.
- **Salvo que se diga otra cosa, las rutas `Assets/...` de este documento son de `Prototipo/`.**
  Los archivos de la simulación (`RunSimulation`, `HordeSimulation`, `GameConfig`, `GameFlow`,
  `CameraDirector`, `SceneryLayout`, etc.) ahora están en `Juego/Assets/_Zombineta/Simulacion/Runtime/`.
- Se abre **cada proyecto por separado** desde Unity Hub (`Prototipo/` o `Juego/`), nunca la raíz.
- **Clonar en una ruta corta.** La ruta de trabajo actual es muy larga y un paquete de Unity ya
  supera los 260 caracteres de Windows (trampa #21).
- **Ramas del equipo:** las de Germán, Jesús, Juana y Seba no tienen trabajo propio; conviene
  borrarlas y recrearlas desde `master` en lugar de actualizarlas.

### El camino al juego definitivo

Cinco sub-proyectos, cada uno con su diseño, plan e implementación:

| # | Sub-proyecto | Estado |
|---|---|---|
| 1 | Reestructura: `Prototipo/`, `Juego/` y simulación compartida | **Hecho** (13/09) |
| 2 | Escenas: `Boot` persistente + una escena por pantalla y por nivel | **Hecho** (13/09) |
| 3 | Herramienta para armar niveles a mano (el nivel es la escena) | **Hecho** (14/09) |
| 4 | Estética nueva: carriles más abajo, más fondo, zombies más grandes | **Hecho** (14/09) |
| 5 | Iluminación 2D real con URP | Próximo; necesita normal maps de arte |

### Escenas del juego definitivo (`Juego/`)

Diseño: `docs/superpowers/specs/2026-09-13-escenas-juego-design.md`.

**Cómo está armado:**

| Escena | Qué es |
|---|---|
| `Boot` | Nunca se descarga. `GameRoot` (flujo + director de escenas), `EventSystem` y el fundido |
| `MainMenu`, `CharacterSelect`, `Cinematic`, `LevelComplete`, `GameOver`, `Ending` | Pantallas base: reemplazan a la anterior con fundido |
| `Level_01`, `Level_02` | Niveles jugables, armados sobre `Scenes/Templates/NivelBase` (ver "Niveles armados a mano"). F2 gana, F3 pierde (editor), Esc/Start pausa |
| `Options`, `Pause` | Capas: se cargan encima sin descargar lo de abajo. Con la pausa encima el tiempo se congela |

- `GameFlow` (compartido) decide a qué pantalla se va. `SceneRoutePlanner` (C# plano, con tests)
  traduce cada cambio en escenas a cargar y descargar. `GameRoot` ejecuta ese plan.
- **Play desde cualquier escena.** Abrís `Level_02` (o `MainMenu`, o la que sea) y le das Play:
  `Boot` se carga sola al lado y el flujo arranca en esa pantalla. No hace falta recorrer el juego
  para probar una pantalla.
- **La cinemática es una sola escena** que muestra el título del nivel y sus viñetas. El
  contenido está en `Assets/_Zombineta/Settings/Niveles.asset`: por cada nivel, `displayName`
  (título), `sceneName` (su escena) y `comicPanels` (imagen y segundos de cada viñeta). Hoy las
  viñetas son placeholders generados en `Art/Placeholder/`.
- **Los menús son botones de uGUI.** Teclado y joystick navegan con el `EventSystem`; cada
  pantalla elige su primer botón en `firstSelected`. Para cambiar un menú alcanza con mover o
  agregar botones en la escena y enganchar su `OnClick`.
- **Input:** la acción `Pause` (Esc, Start) está en el mapa `Player` de
  `Assets/Settings/InputSystem_Actions`. La cinemática usa `UI/Submit` y `UI/Cancel`.

**Cómo agregar cosas:**
- *Un nivel:* agregar una entrada en `Niveles.asset` con su `sceneName`, y correr
  `Zombineta > Esqueleto > Construir escenas que falten`: crea una escena de prueba y la registra
  en Build Settings. Después se reemplaza por una copia de `Scenes/Templates/NivelBase.unity` con
  el mismo nombre (guardar encima) y se arma con la paleta.
- *Una pantalla nueva:* hoy requiere tocar `GameFlow` (el estado), `SceneRoutePlanner` (su escena)
  y la herramienta. Es a propósito: el flujo es la fuente de verdad y tiene tests.
- *Regenerar el esqueleto:* `Construir escenas que falten` nunca pisa una escena existente.
  `Reconstruir todas` sí (pide confirmación): no usarla una vez que arte tocó las pantallas.

**Verificado:** 141 tests en verde en `Juego/` (125 de la simulación + 6 del flujo nuevo + 10 del
planificador). En Play, un recorrido de 18 pasos (menú, personaje, cinemática completa, nivel 1,
pausa, opciones sobre la pausa, volver, seguir, ganar, nivel 2, perder, reintentar, reintentar
desde la pausa, final y menú) dejó en cada paso exactamente las escenas esperadas. Y Play directo
desde `Level_02` arranca jugando el nivel 2.

**Verificado a mano (José, 13/09):** los menús se navegan bien con teclado. **No verificado:**
joystick. Los textos de los interruptores de Opciones se ven algo borrosos porque el control
está escalado: es un placeholder para que arte rehaga.

### Niveles armados a mano (`Juego/`)

Diseño: `docs/superpowers/specs/2026-09-13-niveles-a-mano-design.md`.

**La escena es el nivel.** Cada obstáculo, bidón, rampa, barril o zombie de frente es un objeto
con `LevelItem` bajo `Nivel/Recorrido`. Al dar Play, `LevelScene` junta esos objetos y arma el
recorrido que usa la simulación: lo que se ve en la escena es lo que se juega. No hay un asset
de ruta en el medio (`Ruta01`/`Ruta02` quedaron solo como origen de los dos niveles actuales).

- **La X es la distancia** (metros × `worldUnitsPerMeter`) y **la Y es el carril**. El largo
  del nivel es `goalDistance` en `LevelScene` (pisa el de `GameConfig` solo para ese nivel).
- **Cómo se ve cada tipo** en el editor sale de `Settings/LevelItemPalette.asset` (sprite, color,
  escala, orden). Es provisorio: el arte final de objetos va en el sub-proyecto 4.
- `LevelFlowBridge` une la partida con el flujo: final de cámara → nivel completo / game over,
  pausa, color del personaje elegido.

**La paleta** (overlay "Nivel Zombineta" de la Scene view; aparece sola en una escena con
`LevelScene`, fuera de Play):
- **Colocar:** un botón por tipo; después, click en la escena pone el item en el carril y los
  metros bajo el mouse. Shift sigue colocando, Esc cancela. "Altura" es para pickups aéreos
  (se agarran saltando); "Tipo de zombie" usa los nombres de `Zombies.asset`.
- **Mover:** con la herramienta de mover de Unity. Al soltar, la X queda en la grilla de 1 m y
  la Y en el carril más cercano. Si en cambio se edita `lane` en el inspector, manda el carril.
- **Generar / Regenerar:** arma el nivel con `LevelGenerator` usando la semilla y las densidades
  de `LevelScene > Generator`. **Respeta lo tocado:** solo reemplaza items generados que nadie
  movió ni editó; lo colocado a mano, lo movido y lo fijado se conserva, y el generador no pone
  nada encima. Todo se deshace con Ctrl+Z.
- **Fijar / Desfijar:** para conservar un item generado sin moverlo. Desfijar uno movido lo deja
  como "generado sin tocar" donde está.
- **Ir a (m)** mueve la vista. **Probar desde acá** entra en Play con la moto en los metros del
  centro de la vista y la horda con la ventaja inicial detrás (solo en el editor).
- **Guías** (gizmos): los tres carriles, regla cada 50 m, largada, refugio, un círculo amarillo
  sobre lo fijado o tocado, y **en rojo los problemas** que detecta `LevelValidator`: tres
  carriles tapados a menos de 2 m, algo en el aterrizaje de una rampa (8,5 a 50 m después), dos
  objetos encimados (mismo carril y altura, a menos de 1 m). El botón "N problemas" lleva al
  siguiente.

**Estado de los niveles:** `Level_01` (191 items, de `Ruta01`) y `Level_02` (206, de `Ruta02`),
todos marcados como generados. `Level_01` tiene 4 objetos encimados heredados de la ruta vieja
(1200, 2400, 2450 y 3500 m); se ven en rojo y se arreglan moviendo uno o regenerando.

**Verificado (14/09):** 166 tests en verde en `Juego/` (141 anteriores, +18 del generador y el
layout, +7 del validador). En Play desde `Level_01`: la partida corre, un bidón agarrado se apaga en la escena,
ganar llega a `LevelComplete`, y el recorrido menú → personaje → cinemática → `Level_01` →
pausa → seguir deja las escenas esperadas. En el editor, por MCP: colocar a mano engancha a
carril y grilla; mover un generado lo cambia de carril, lo redondea y deja de ser reemplazable;
regenerar conservó colocado, movido y fijado (189 quitados, 192 nuevos, 0 problemas); probar
desde 1500 m arrancó la moto en 1500,2 m. **No verificado a mano:** arrastrar items con el mouse
y colocar con clicks reales (se probó por la API de la herramienta).

**Arreglado en el camino:** si la moto chocaba justo antes de pausar, el fin del congelado del
impacto le devolvía el tiempo al nivel con la pausa abierta (`CameraFollow.TimeHeld`). Y en el
prototipo, los barriles nunca explotaban jugando: `RunController` no le pasaba el recorrido a la
simulación (`Sim.Barrels = Level`).

### Estética y escala (`Juego/`)

Diseño: `docs/superpowers/specs/2026-09-14-estetica-escala-design.md`.

Carriles más juntos y más abajo, casi todo el cuadro de fondo, personajes al doble de alto y
zombies con arte propio por arquetipo en vez de un sprite genérico teñido. Calzado a ojo contra
`Arte/Bocetos/Concept Art/Mapa_Escala.png` — la referencia es el encuadre **en calma**, con la
horda lejos, no un momento de tensión.

**Guía de referencia y captura de comparación** (editor, menú `Zombineta/Encuadre`):
- `Mostrar referencia` / `Ocultar referencia` superpone el boceto en el Game view (Canvas
  `ScreenSpaceOverlay`, `HideFlags.DontSave`: no llega a builds ni ensucia la escena). `Elegir
  imagen…` cambia el PNG; `Opacidad +`/`Opacidad -` ajustan la mezcla (default 0,4). Sigue viva en
  Play y al cambiar de escena.
- `Capturar comparación` (`FramingCapture`) renderiza `Camera.main` a 1920×1080, le mezcla la
  referencia encima al mismo opacity, y guarda el PNG en `Juego/Temp/Encuadre/`. Es la forma de
  comparar sin depender del tamaño ni el foco de la ventana del Game view (trampa #29).

**Dónde vive cada número:**

| Qué | Dónde |
|---|---|
| Separación entre carriles | `GameConfig.laneSpacing` (0,99) |
| Tamaño de cámara en calma / tensión / atrapada / victoria | `CameraConfig.wideSize` (6,3 = la referencia) / `tightSize` (5,67, −10 %) / `catchSize` (3,6) / `victorySize` (7,5) |
| Borde de abajo de la calle | `CameraConfig.viewBottomY` |
| Anclaje del piso al hacer zoom | `CameraFollow` (carril ancla; mezcla en `anchorBlendSeconds` = 0,25 s reales; se apaga en atrapada y victoria) |
| Altura del salto en pantalla | `GameConfig.jumpHeightToWorld` |
| Escala de la moto | `Scooter.localScale` en `NivelBase` y en cada nivel |
| Escala de los zombies | `HordeView.bodyScale` × la escala del tipo en `Zombies.asset` × `ZombieLook.scale` |
| Escala de objetos del recorrido | `LevelItemPalette.asset` (por tipo) |
| Tamaño de efectos y manchas | `FxManager` (alturas ya serializadas) y la escala de cada prefab `Fx*` |
| Capas de fondo | `Escenario.asset` (parallax, alto y base de cada capa) |

**Orden por carril y sus slots:** `LaneSorting.Order(visualLane, slot)`
(`Scripts/Gameplay/Core/LaneSorting.cs`, namespace `Zombineta.Core`) =
`Base(1000) − round(visualLane×100) + slot`, con `visualLane` continuo (no salta durante el tween
de carril). Los slots, de atrás hacia adelante dentro de un mismo carril:
`Shadow(0) < Item(10) < Zombie(20) < Player(30) < Effect(40)`. Lo usan `ScooterView`, `HordeView`,
`LevelScene.ApplyVisual` y `FxManager`.

**Sombras:** `GroundShadow` (`Scripts/Gameplay/Fx/GroundShadow.cs`) es la elipse
(`Art/Fx/sombra.png`) que sigue la X del dueño, se queda en el piso de su carril y se achica y
aclara con la altura. La crean por código `ScooterView`, `HordeView` (una por zombie, se oculta al
morir) y `LevelScene.ApplyVisual` (una por item que no sea rampa; el ancho sale de
`LevelItemPalette.Look.shadowWidth`).

**`ZombieLooks` — cómo sumar un arquetipo nuevo:**
1. **Preparar la hoja:** fondo transparente con `Tools/SpritePrep/remove_white_background.py
   <entrada> <salida> [--tolerance N]` (fondo blanco liso; el default 24 alcanza salvo un
   cuadriculado disfrazado de blanco — trampa #31). Para cuadriculado gris está
   `remove_checker_background.py`.
2. **Recortar:** seleccionar el PNG en el Project y `Assets > Zombineta > Recortar por
   transparencia` (`SheetSlicer`, `Editor/Art/SheetSlicer.cs`). Encuentra cada figura por
   componentes conexas de alfa (`AlphaIslands`, testeado), agrupa filas por superposición de los
   bordes verticales (no por distancia entre centros: una pose caída/tirada tiene un centro muy
   lejos del de sus vecinas de pie aunque comparta la misma línea de base) y nombra
   `<hoja>_<fila>_<columna>` con pivot en los pies. Si la hoja ya tenía sprites recortados y el
   conteo de islas da distinto, no sobreescribe sin confirmar.
3. **Agregar el look:** en `Settings/ZombieLooks.asset`, un `ZombieLook` nuevo dentro del
   `TypeLooks` del tipo que corresponda (mismo orden que `Zombies.asset`: Común/Corredor/Pesado)
   con sus sprites de `walk`/`hit`/`death`, `walkFps` y `scale` (compensa la resolución de la hoja
   de origen, no el tamaño en juego: las hojas completas van en 1, las poses de `Zombies_poses` en
   2,5 porque salen ~2,3 veces más chicas). `ZombieLookPicker` elige entre los looks del tipo por
   semilla y no repite el look anterior del mismo tipo cuando hay más de uno.

**Estado de los niveles:** `Level_01` y `Level_02` se reabrieron y guardaron con los carriles y
sombras nuevos: 0 items fuera de su carril, mismos problemas preexistentes en `LevelValidator`
que antes (4 y 5, los objetos encimados heredados de la ruta vieja).

**Verificado (14/09):** 186 tests EditMode en verde. Capturas de plano en calma, cerrado
(tensión), atrapada y victoria contra `Mapa_Escala.png`: líneas de carril y de calle a ≤2-3 px de
la referencia. En Play: el carril de abajo tapa al de arriba sin parpadeo durante el tween, la
horda muestra la mezcla de looks por arquetipo con impacto y muerte animados por código, y el
recorrido menú → nivel → pausa no tira errores de consola. `Prototipo/` sin tocar (`git diff`
contra `5b66236` en `Prototipo/Assets|Packages|ProjectSettings` vacío).

**NO verificado — pendiente de jugarlo:** cómo se siente el encuadre nuevo con las manos (las
siluetas del primer plano tapando un carril un instante, el aire para saltar con el ancla del
piso puesto, si sigue leyéndose bien qué carril ocupa cada cosa con personajes tan grandes).

**Límites conocidos de los placeholders:** fachadas de edificio estiradas, troncos de árbol
rellenos (no silueta fina), cordón y vereda como franjas grises lisas, los looks de una sola pose
(oficinista, vagabundo, mujer, adolescente) mueren en un solo cuadro por falta de arte de caída, y
la capa `Frontal` (árboles a parallax 2,5) sigue cruzando los carriles por delante.

---

## 1. Qué es el juego

El equipo tiene el arte avanzado pero **los documentos de diseño (01, 03, 06, 07) siguen
siendo plantillas vacías**. Las decisiones de abajo se tomaron durante el desarrollo del
prototipo y este archivo es, por ahora, el único lugar donde están escritas. Pasarlas a los
.docx es trabajo pendiente y cuenta para la rúbrica de E2.

Persecución 2D lateral. Una repartidora en motoneta escapa de una horda de zombies por una
calle nocturna de tres carriles, administrando tres recursos, hasta llegar a un refugio.

### Forma

- Vista de perfil pura, scroll horizontal. **3 carriles apilados verticalmente** (0 abajo,
  1 medio, 2 arriba). Cambiar de carril es un tween de 0,15 s, no un salto físico.
- El mundo es estático y la jugadora avanza en X real. La cámara la sigue **por detrás**,
  dejándola a la derecha de la pantalla con aire a la izquierda para ver venir a la horda.
  Si la cámara va adelante, la amenaza queda fuera de cuadro justo cuando importa. Cuánto
  cierra y abre el plano según la tensión está en la sección 3, "Lenguaje de cámara".
- Nivel de **distancia fija**: 4000 m ≈ 5 minutos. El "refugio" es la meta al final.

### Verbos

| Acción | Input | Efecto |
|---|---|---|
| Cambiar carril | ↑/↓ o W/S | Tween de 0,15 s |
| Turbo | → o D (mantener) | ×1,8 velocidad, ×3 consumo |
| Retroceso | ← o A (mantener) | Marcha atrás real (−0,5×), ×0,5 consumo |
| Faro | ESPACIO (toggle) | La horda avanza a **×0,5**. Drena batería |
| Disparar | X o click izq. | Bala por **tu carril**: mata al primero que encuentre (o detona un barril). Munición limitada |
| Saltar | Pasar por una rampa | Lanza según la velocidad. No hay botón de salto |
| Inclinar en el aire | ← o A (nariz arriba) / → o D (nariz abajo) | Regula el largo; hay que aterrizar nivelada |
| Menús | W/S elegir, ENTER confirmar, ESC volver | A/D cambia valores en Opciones |
| Reintentar | R | En el Game Over |
| Números de debug | F1 | HUD crudo para balancear |
| Ganar / perder el nivel | F2 / F3 | Solo editor y builds de desarrollo. F2 teletransporta a la meta |

### La decisión de diseño que sostiene todo

**El faro y la pistola hacen lo mismo (frenar a la horda) pero de forma distinta:**

- **Faro** = efecto *sostenido de tasa*. Compra tiempo. Cuesta batería, no nafta.
- **Pistola** = *impulso instantáneo*. Compra espacio. Munición limitada.

Como el faro no gasta nafta, es la herramienta eficiente y la pistola es el botón de pánico.
Eso es lo que crea la brecha de habilidad: en la simulación, quien usa el faro llega y quien
solo usa turbo muere a mitad de camino. **Si tocás el balance, verificá que esa brecha siga
existiendo** — es la razón de ser del prototipo.

### Fuera del alcance (deliberado)

Puertas y trampas ambientales (T13), victoria alternativa por tiempo. Del salto: botón de
salto libre, cambio de carril en el aire y obstáculos aéreos.

Existen **como placeholder**, para mostrar el flujo: la selección de personaje (dos
repartidoras que por ahora solo cambian el color de la moto, una por cada dirección de arte
pendiente), las cinemáticas (placas de texto), Opciones (volumen, pantalla completa,
efectos de cámara y sacudidas) y un segundo nivel (`Ruta02`, generado igual que el primero).

---

## 2. Arranque rápido

- **Unity 6000.6.0f1** (en `D:\Dev\Unity`). Todo el equipo tiene que usar exactamente esta.
  Ojo: **6000.6 no es LTS.** Sigue pendiente decidir si se quedan acá o bajan a 6000.0 LTS.
- Abrí el proyecto `Prototipo/` desde Unity Hub, cargá `Assets/_Zombineta/Scenes/Prototipo.unity`
  y dale Play. Arranca en el menú.
- El proyecto es 2D URP con Renderer2D. El faro es un `Light2D` real, así que la escena
  depende de la iluminación 2D: si sacás el `Global Light 2D`, se ve todo negro.
- Cámara ortográfica de tamaño 6 en reposo, con el borde de abajo clavado en y = −4,6
  (`CameraConfig.viewBottomY`): el arte de la pista es alto y así quedan la calle en la mitad
  de abajo y edificios y cielo arriba. Al hacer zoom se mueve el borde de arriba, nunca la
  calle.

### MCP de Unity

Viene nativo con `com.unity.ai.assistant` (ya instalado). El relay está en
`C:\Users\kabub\.unity\relay\relay_win.exe` y la config vive en el `.mcp.json` de la carpeta
padre (`Taller de proyecto integral/`). Con Unity abierto: *Edit > Project Settings > AI >
Unity MCP*, Bridge en **Running**, y las 54 tools habilitadas.

---

## 3. Arquitectura

**La regla que gobierna todo: la simulación no sabe que existe Unity.**

`RunState` + `RunSimulation` + `LevelRuntime` son C# plano. Se instancian, se les hace `Tick`
y se verifica el resultado sin abrir una escena, sin MCP y sin el editor. Las Views solo leen
estado y dibujan.

Eso no es purismo: es lo que permite simular partidas enteras en segundos para balancear, y
lo que hizo que se detectara que la ruta era imposible **antes** de que nadie la jugara.

```
Assets/_Zombineta/Scripts/
  Core/
    GameConfig.cs      ScriptableObject. TODOS los números de balance.
    RunState.cs        Estado de una partida. C# plano.
    RunSimulation.cs   Todas las reglas del juego. C# plano, sin MonoBehaviour.
    RunController.cs   Único puente con Unity: input -> Tick -> eventos.
    CameraFollow.cs    Puente de la cámara: estado y eventos -> CameraDirector -> transform.
                       También maneja el tiempo (congelado del choque, cámara lenta).
    GameSettings.cs    Preferencias del jugador (PlayerPrefs): efectos de cámara, sacudidas.
  CameraFx/
    CameraConfig.cs    ScriptableObject: todos los números de la cámara.
    CameraDirector.cs  El lenguaje de cámara. C# plano: entra estado, sale una pose.
  Player/
    PlayerIntent.cs    Lo que la jugadora QUIERE hacer. Desacopla input de reglas.
    PlayerInputReader.cs   Único lugar que sabe qué teclas existen.
    ScooterView.cs
    HeadlightView.cs   Light2D + titileo con batería baja.
  Enemies/
    ZombieType.cs      Los numeros de una clase de zombie.
    ZombieRoster.cs    ScriptableObject: la lista de tipos (Settings/Zombies.asset).
    HordeSimulation.cs La masa como individuos: avance, disparo, muertes, reciclado. C# plano.
    HordeView.cs       Un objeto por zombie simulado; dispara sus animaciones.
  Fx/
    FxManager.cs       Eventos de la horda -> particulas, traza del disparo y manchas.
  Level/
    LevelDefinition.cs El recorrido como datos (ScriptableObject).
    LevelRuntime.cs    Resuelve encuentros. C# plano.
    LevelSpawner.cs    Recicla sprites sobre la ventana visible.
    GoalView.cs        El refugio.
    LaneMarkersView.cs Rayas de la calzada (dan la sensación de velocidad).
  Scenery/
    ParallaxMath.cs    La cuenta del parallax. C# plano.
    SceneryLayout.cs   Qué tile va en cada lugar de una capa. C# plano, determinístico.
    SceneryTileset.cs  ScriptableObject: las capas, sus tiles, pesos, huecos y semillas.
    SceneryManager.cs  Arma el escenario a medida que avanza la cámara y lo rearma en vivo.
  Flow/
    GameFlow.cs        Máquina de estados de las pantallas. C# plano.
    LevelSequence.cs   ScriptableObject: los niveles en orden, con su recorrido y cinemática.
  UI/
    HudView.cs         Barras, sin números.
    DebugHudView.cs    Números crudos, detrás de F1.
    MenuList.cs        Lista de opciones con una seleccionada. Solo presentación.
    ScreenFlow.cs      Teclado -> eventos de GameFlow; GameFlow -> paneles visibles.
```

Assets de datos en `Assets/_Zombineta/Settings/`: `GameConfig` (balance), `Ruta01` y `Ruta02`
(recorridos), `Niveles` (orden de los niveles y texto de las cinemáticas), `Escenario`
(capas del fondo) y `CameraConfig` (la cámara). Diseñar es editar esos assets, no código.

### Dos decisiones que conviene no revertir sin pensarlo

**Los encuentros se resuelven por distancia, no con triggers de física.** La moto se mueve
por transform, no por Rigidbody, así que un collider sería frágil. `LevelRuntime.Collect`
resuelve el *tramo* recorrido entre dos posiciones, lo que garantiza que nada se saltee yendo
en turbo y que el retroceso recoja lo que dejaste pasar.

**`worldUnitsPerMeter = 0.75`: la velocidad que se ve es un número de presentación.** La
simulación piensa en metros; las Views los pasan a unidades con este factor, y el balance no
se entera. Empezó en 0,25 y todo se veía lento (la moto en Normal cruzaba la pantalla en 7 s);
el 13/09 se llevó a 0,5 y después a 0,75, tomando como referencia la persecución en moto de
*Terminator 2D: NO FATE*. Medido en Play con la moto a 12 m/s:

| Capa | Parallax | Antes (0,25) | Ahora (0,75) |
|---|---|---|---|
| Calle y moto | 1 | 3 u/s | 9 u/s |
| Edificios | 0,5 -> **0,8** | 1,5 u/s | 7,6 u/s |
| Postes (nueva) | 1,4 | — | 13 u/s |
| Frontal | 2,5 | 7,5 u/s | 24 u/s |

El costo: en pantalla entran unos 28 m de ancho. Para no perder tiempo de reacción,
`CameraConfig.lookAhead` pasó de 6,2 a 9,3 u (y el extra de turbo de 1,5 a 2,25): hacia adelante
se ven los mismos metros que antes, y la moto quedó más a la izquierda. Lo que se perdió es vista
hacia atrás: la horda entra en cuadro recién a unos 17 m.

**Si se toca la escala, acompañarla** con `jumpHeightToWorld` (hoy 1,125: 1,5 veces la escala,
para que el arco del salto no cambie de forma), `lookAhead` y `turboExtraLookAhead` (en
unidades: escalan con el mundo), la separación de rayas de `LaneMarkersView` (hoy 2 m) y el
`headlightRangeMeters` de `HordeView` (hoy 19 m, lo que cubre el cono del faro de 14 u).

**La calle tiene parallax 1: es el mundo, no una imagen que se mueve.** El sincronismo entre
la moto y el fondo sale de ahí, por construcción. No hay una "velocidad de scroll" que ajustar
para que coincida con la de la moto. Medido en Play Mode sobre 263 m: la moto se movió
65,8984 u y la simulación predice 65,8984 u. Si alguien propone mover la calle con un script
de scroll aparte, esa garantía se pierde.

**El escenario es determinístico por semilla.** `SceneryLayout` arma los tiles de a pedazos,
frame a frame, y da exactamente lo mismo que armarlo de una (hay un test para eso). Por eso el
retroceso vuelve sobre los mismos edificios y dos playtests ven la misma calle.

**El flujo de pantallas es una máquina de estados sin Unity.** Una acción que no corresponde
a la pantalla actual se ignora: una victoria reportada en pleno Game Over no saltea nada. Se
comprobó en vivo durante la verificación, sin querer.

### Lenguaje de cámara

La cámara cuenta la persecución sin HUD. Todo sale de **una sola señal de tensión**: la
distancia a la horda, pasada por un smoothstep entre `safeGap` (45 m, plano abierto, tamaño
6,3) y `dangerGap` (8 m, plano cerrado, tamaño 5).

| Qué pasa | Qué hace la cámara |
|---|---|
| La horda se acerca | Cierra el plano **despacio** (0,9 s): la amenaza se siente venir |
| Faro, disparo o turbo sacan ventaja | Abre **rápido** (0,3 s): el alivio se nota en el acto |
| Turbo | Abre un poco más y adelanta la mirada 1,5 u |
| Choque | Sacudida, golpe hacia adelante con zoom, y **60 ms de congelado** |
| Disparo | Sacudida leve y retroceso |
| Te atrapan | Plano cerrado sobre la moto, **cámara lenta ×0,25** y la horda le pasa por encima; 1,2 s y Game Over |
| Llegás al refugio | El plano se abre sobre la meta (tamaño 7,5) y la moto entra rodando; 1,6 s y Nivel completo |

Tres reglas que los tests sostienen y conviene no romper:
- **La mirada hacia adelante no cambia con el zoom.** La cámara se ubica como
  `X = playerX + lookAhead − medioAncho(tamaño)`: cerrar el plano no le quita a la jugadora
  la distancia que ve hacia adelante, solo el aire de atrás.
- **El borde de abajo no se mueve.** `Y = viewBottomY + tamaño`: la calle nunca sale de cuadro.
- **Todo corre en tiempo real** (`unscaledDeltaTime`): la cámara sigue viva durante el
  congelado y la cámara lenta, y los resortes son independientes del framerate.

Los finales (atrapada y refugio) son **solo presentación**: la simulación ya terminó, y lo que
se ve (la horda que avanza de más, la moto que rueda dentro del refugio) lo agregan
`HordeView` y `ScooterView`. `ScreenFlow` espera lo que devuelve `CameraFollow.PlayCatch()` /
`PlayVictory()` antes de cambiar de pantalla, y siempre devuelve `Time.timeScale` a 1.

En Opciones hay dos interruptores (guardados en PlayerPrefs): **Efectos de cámara** apaga
todo y deja el encuadre clásico fijo, y **Sacudidas** apaga solo sacudidas y golpes
(para quien se marea) pero conserva el zoom y los finales.

Durante un salto, la cámara se abre lo justo para que la moto no se salga por arriba
(`CameraConfig.jumpHeadroom`).

### Rampas y salto

Diseño completo en `docs/superpowers/specs/2026-09-10-rampas-y-salto-design.md`; plan de
implementación en `docs/superpowers/plans/`. Lo esencial:

- **La rampa es una entrada del recorrido** (`LevelEntryKind.Ramp`). Pasarla hacia adelante,
  en el piso y en su carril, lanza. No se gasta. Saltar es opcional: se esquiva cambiando de
  carril.
- **El largo depende de la velocidad**: normal ≈ 1 s y 12 m; turbo ≈ 2 s y 40 m.
- **En el aire, A/D inclinan** (el modo de manejo se reinterpreta: retroceso = nariz arriba,
  turbo = nariz abajo). **Nariz arriba planea**, nariz abajo cae antes: así se regula dónde
  caer. No gasta nafta, no cambia de carril, no choca, no agarra lo del piso.
- **En turbo la rampa imprime un giro hacia atrás.** Sin corregir, aterriza a más de 60° y se
  cae; con D apretado todo el salto se pasa de largo y cae de trompa. Hay que soltar a tiempo.
  A velocidad normal no gira: un salto sin tocar nada aterriza a 20°, sano pero sin premio.
- **Aterrizaje:** ≤ 8° da impulso (×1,25 por 1,5 s); ≤ 25° nada; más, **caída**: 1,6 s tirada
  y −10 de nafta, y la horda recorta ≈ 28 m.
- **Pickups aéreos:** `LevelEntry.height > 0`. Solo se agarran volando a esa altura (±1,5 m).
  Los de las rutas están a 6 m: solo los alcanza un salto en turbo.
- **Se ve así:** la moto sube y rota sobre las ruedas; una sombra en el carril marca dónde va
  a caer y **se pone verde cuando el ángulo daría aterrizaje perfecto** (la pista para
  aprenderlo sin tutorial). Tirada, queda rotada 70° con el tinte de aturdida.
- Todos los números en la sección "Salto" de `GameConfig`; la altura en pantalla está
  exagerada respecto del eje X para que el salto se lea (`jumpHeightToWorld = 1,125 u/m`
  contra 0,75 en X).

Las rutas tienen **15 rampas cada una**, cada 250 m desde los 300: rampa, dos obstáculos
para sobrevolar a +4 y +8 m y, una sí y otra no, un bidón aéreo a +17 m. Se verificaron con
la simulación real: todas se saltan sin chocar en normal y en turbo estabilizando, y en
ningún punto quedan los tres carriles bloqueados.

---

### La horda

Diseño completo en `docs/superpowers/specs/2026-09-11-horda-de-individuos-design.md`.

- **La horda son 24 zombies simulados**, no un número. `State.HordeX` se calcula cada tick como
  **la X del vivo más adelantado**: te puede alcanzar un solo corredor aunque el grueso esté
  lejos. Vive en `HordeSimulation` (C# plano, `Scripts/Enemies/`).
- **Es infinita.** El que muere queda de cadáver 1,2 s y reaparece al fondo de la masa con un
  tipo nuevo; el que se descuelga también. La población es constante: **matar compra espacio,
  no vacía la horda**.
- **`shotHordePushback` ya no existe.** El empujón del disparo es consecuencia: si bajás al
  puntero, el frente pasa a ser el siguiente. Matar a uno en el medio del bulto casi no mueve la
  aguja; matar al corredor que se despegó, sí.
- **Tipos en `Settings/Zombies.asset`** (común, corredor, pesado), con velocidad, probabilidad
  de morir por bala, segundos que se frena si aguanta el tiro, costo de arrollarlo, peso de
  aparición, tinte y escala. Sin asset, todos son comunes y mueren de un tiro.
- **El disparo** sale por el carril de la jugadora hasta 60 m y pega en lo primero que
  encuentra: un zombie (muere o encaja el impacto y se frena) o un **barril** (explota, mata en
  8 m en los tres carriles, asusta en 16 y **encadena** otros barriles). Si el carril está
  vacío, la bala se pierde y la munición también.
- **Zombie de frente** (`LevelEntryKind.ZombieFront`, con su tipo en `variant`): se esquiva, se
  sobrevuela con una rampa o se arrolla al costo de su tipo (al común te lo llevás puesto; el
  pesado frena y cuesta nafta).
- **Semilla fija** (`GameConfig.hordeSeed`): tipos y muertes reproducibles entre playtests.
- **Los eventos son la interfaz con las vistas.** `HordeSimulation` publica una lista por tick
  (traza, impacto, muerte con su causa, explosión, bala perdida) y `FxManager` la traduce en
  partículas, la traza del disparo y las manchas del asfalto. `BeginTick()` limpia esa lista al
  **empezar** el tick, no en el paso de la horda: el disparo se resuelve antes que el
  movimiento y sus eventos tienen que sobrevivir.
- **Opciones suma "Sangre: Alta / Baja"** (`GameSettings.Gore`). En baja, polvo gris y sin
  manchas.

---

## 4. Balance

Todo vive en `Assets/_Zombineta/Settings/GameConfig.asset`. Balancear es editar ese asset,
nunca tocar código.

El recorrido vive en `Ruta01.asset`: 111 entradas (31 bidones, 7 baterías, 9 cajas de balas,
64 obstáculos), generado con semilla fija para que dos playtests sean comparables. Los
obstáculos empiezan ralos y se van cerrando, y **nunca bloquean los tres carriles a la vez**.

### El ajuste del 11/09: `hordeBaseSpeed` 17,5 -> 14

Al convertir la horda en individuos, el corredor (×1,35) quedaba en 23,6 m/s contra los 21,6
del turbo: **nada lo despegaba** y la partida se perdía a los 42 m. Se bajó `hordeBaseSpeed` a
**14**, que deja el reparto así:

| | m/s |
|---|---|
| Moto en Normal | 12 |
| Pesado (×0,75) | 10,5 |
| Común (×1) | 14 |
| Corredor (×1,35) | 18,9 |
| Moto en turbo | 21,6 |

O sea: en Normal se pierde terreno contra el grueso de la horda, y el turbo le gana incluso al
corredor. Medido con la simulación real sobre `Ruta01`, sin cambiar de carril (o sea, **sin
juntar un solo bidón**):

| Estrategia | Resultado |
|---|---|
| Siempre Normal | la alcanzan a los 63 m (5 s) |
| Turbo + faro + tiros cuando aprieta | 648 m (16%), se queda sin nafta a los 38 s |

La segunda muere de nafta porque la sonda no esquiva ni recolecta: jugando de verdad se juntan
bidones. Falta la pasada de balance completa con alguien jugando.

### Estado actual: la meta no se alcanza

El 6 de septiembre se subió a mano `fuelBurnPerSecond` de 1,6 a 2,6 y `hordeBaseSpeed` de
13,5 a 17,5, porque jugando la persecución se sentía más tensa y más divertida. El simulador
**no respalda todavía** esos valores:

| Balance | Estrategia hábil | Tiempo |
|---|---|---|
| Actual (2,6 / 17,5) | muere sin nafta al **28%** | 79 s |
| Anterior (1,6 / 13,5) | llega al 100% | 303 s |
| Actual + bidones cada 80 m | tampoco llega (72%) | — |

Las dos lecturas conviven: el feel mejoró y la meta quedó fuera de alcance. La salida más
probable **no es volver atrás el balance sino acortar `goalDistance`** (de 4000 a ~1000 m),
para que el recorrido dure lo que la nafta aguanta. Se decide jugando, no acá. Mientras
tanto, F2 permite ver "Nivel completo" y el Final.

Con el balance anterior, la tabla de referencia era:

| Estilo de juego | Resultado | Tiempo | Margen |
|---|---|---|---|
| Rutea hacia los recursos, usa el faro | **llega** | 303 s | 56 de nafta |
| Solo usa turbo | muere sin nafta | 50 s | 21% del recorrido |

### El error que ya cometimos una vez

La primera sonda de balance suponía que se agarraban **todos** los bidones. Pero están en
carriles al azar y hay que ir a buscarlos: la tasa real de recolección ronda el **50%**. Con
esa suposición la ruta quedó imposible — ninguna estrategia pasaba del 28%. Si volvés a
calcular densidad de recursos, calculá sobre la tasa efectiva, no sobre la nominal.

Otro dato contraintuitivo: **subir el valor del bidón casi no sirve.** Se llega con el tanque
en cero igual y el excedente se pierde contra `fuelMax`. Lo que mueve la aguja es la densidad.

### Cómo re-balancear

Hay dos caminos y conviene usar los dos:

1. **`Tools/BalanceProbe/`** (ver su README): corre la simulación real y los tests reales
   fuera de Unity con shims mínimos. Tres segundos por iteración en vez de cinco minutos de
   partida.
   ```bash
   cd Tools/BalanceProbe && dotnet run
   ```
2. **Vía MCP dentro de Unity**, cargando `GameConfig.asset` y `Ruta01.asset` reales y
   corriendo estrategias contra ellos. Es lo que se usó para el ajuste final. Ojo: la sonda
   externa **copia** los .cs del juego, así que si tocás la simulación hay que volver a
   copiarlos. **Todavía no se copiaron los del salto**: la sonda no conoce rampas, así que
   sus resultados ignoran saltos, impulsos y caídas.

En cualquier caso, medí siempre con **dos** estrategias: una hábil y una torpe. Si las dos
llegan, el juego no premia entender nada; si ninguna llega, es injusto.

---

## 5. Trampas de Unity y del MCP (leer antes de perder una hora)

Estas costaron tiempo real en esta sesión:

1. **Los errores de compilación llegan a la consola con tipo `Log`, no `Error`.** Filtrar
   `Unity_ReadConsole` por `Error` los oculta. Usá `Types: ["All"]` con
   `FilterText: "error CS"`. Por esto se persiguió durante un rato un "bug de caché" que en
   realidad era un nombre de assembly mal puesto en el asmdef.

2. **El código que se manda por `Unity_RunCommand` se envuelve en el namespace
   `Unity.AI.Assistant.Agent.Dynamic.Extension.Editor`.** Eso hace que `Image` resuelva a
   `Unity.AI.Image` y `CompilationPipeline` a `Unity.CompilationPipeline`. Calificá con
   `global::` o usá alias:
   ```csharp
   using UImage = global::UnityEngine.UI.Image;
   ```

3. **`AssetDatabase.Refresh()` y `CompilationPipeline.RequestScriptCompilation()` desde MCP
   no hacen nada.** Después de editar un `.cs` en disco hay que ejecutar el menú:
   ```
   Unity_ManageMenuItem: Action=Execute, MenuPath="Assets/Refresh"
   ```

4. **Un asset recién creado por MCP puede no cargar en el comando siguiente.** Si
   `LoadAssetAtPath` devuelve null aunque el archivo esté bien, hacé un `Assets/Refresh` y
   volvé a intentar en un comando aparte. No uses `ImportAsset` con `ForceUpdate` justo antes
   de cargarlo: empeora las cosas.

5. **`result.Log` solo sustituye `{0}` simple**, no formatos como `{0:0.0}`. Concatená.

6. **Los comentarios `///` dentro del código de `RunCommand` rompen el parser.** Usá `//`.

7. **Reflection está bloqueada** por seguridad en `RunCommand`.

8. `Unity_Camera_Capture` no muestra canvases Screen Space Overlay. **Truco para verificar la
   UI:** en Play Mode, pasar el canvas a `ScreenSpaceCamera` con `worldCamera = Camera.main`,
   `planeDistance = 1` y `sortingOrder = 1000` (sin el orden alto, los sprites de la escena se
   dibujan encima del texto). Es solo de runtime y se revierte al salir de Play. Teclas no se
   pueden enviar: el flujo se maneja por la API de `ScreenFlow.Flow`.

9. **`AssetDatabase.LoadAssetAtPath<Sprite>("hoja.png[NombreSprite]")` devuelve `null` en
   silencio** para un sub-sprite generado por slicing, sin excepción ni error, en esta versión
   de Unity. La alternativa que funciona es `LoadAllAssetsAtPath` o
   `LoadAllAssetRepresentationsAtPath` filtrando por `.name`. Esto es **distinto** de la trampa
   #4 (asset recién creado que no carga): esa se arregla con un `Assets/Refresh`; esta es un
   `null` permanente del indexer con corchetes que un Refresh **no** arregla.

10. **`TextureImporter.maxTextureSize` viene en 2048 por default en todos los overrides de
    plataforma.** Si la textura fuente es más ancha que eso (p. ej. una hoja de 2752 px), Unity
    la reescala para abajo al importar, sin warning ni error en consola. Antes de slicear o
    usar una hoja grande, subí `maxTextureSize` (el default y todos los overrides de
    plataforma) a un valor ≥ la dimensión más grande de la fuente.

11. **Con Unity en segundo plano, el Play Mode no avanza.** `Time.frameCount` se queda en 1.
    Poné `Application.runInBackground = true` desde un `RunCommand` ya en Play: es de runtime,
    no toca la configuración del proyecto (que conviene dejar en false para el build).

12. **No edites scripts con el juego en Play.** Con `runInBackground` activo, Unity detecta
    el cambio, recompila y recarga el dominio en medio de la sesión: los campos no
    serializados (como el `GameFlow` de `ScreenFlow`) quedan en null. Salí de Play, editá,
    `Assets/Refresh`, y volvé a entrar.

13. **El tiempo corre de verdad entre comando y comando.** Cada llamada al MCP tarda
    segundos: en ese lapso una cinemática termina sola o la horda alcanza a la moto. Si una
    prueba necesita varias transiciones seguidas, hacelas todas en un mismo `RunCommand`.

14. **`AssetDatabase.DeleteAsset` está bloqueado:** el MCP lo rechaza entero con "User
    interactions are not supported", sin ejecutar nada. Antes de crear un asset, chequeá si
    existe y no lo pises. Destruir objetos de escena con `DestroyImmediate` sí funciona.

15. **`Object.GetInstanceID()` es error de compilación en 6000.6** (obsoleto, "usá
    `GetEntityId`"). Para el id de un objeto usá `Unity_ManageGameObject` con `find`.

16. **Lo que se edita en un ScriptableObject durante Play Mode queda guardado**, a diferencia
    de lo que se edita en la escena. Es lo que hace útil la edición en vivo del escenario,
    pero también quiere decir que un experimento en Play no se deshace solo al salir.

17. **`System.Text.RegularExpressions` no está disponible en `RunCommand`.** Parseá a mano.

18. **Para medir algo instantáneo (un choque, un frame congelado) no alcanza con consultar
    después:** para cuando llega el siguiente comando ya pasó. Suscribite a `run.Stepped`
    desde un `RunCommand`, o colgá un callback de `EditorApplication.update` que tome
    muestras a tiempos fijos, y guardá lo medido en `SessionState`. Otro comando lo lee.
    Así se verificaron el congelado del choque y la secuencia de F2 a mitad de nivel.

19. **El cono de un `Light2D` apunta hacia +Y local.** Para que el faro ilumine hacia atrás
    (hacia la horda) la rotación Z es 90, no 180. Con 180 apuntaba al piso.


20. **El `RunCommand` del MCP solo compila contra assemblies cuyo asmdef está dentro de
    `Assets/`** (más algunas curadas de Unity). Una assembly que viene de un paquete —aunque sea
    local y esté cargada— no se puede referenciar: da `CS0012 ... is defined in an assembly that
    is not referenced`. Por eso la simulación vive en `Juego/Assets/` y no en una carpeta
    `Paquetes/`. Consecuencia: **en el prototipo, `RunCommand` ya no puede tocar tipos de la
    simulación** (`RunSimulation`, `GameFlow`, `GameConfig`...). Para balancear alcanza con
    `SerializedObject` sobre el asset cargado como `Object`; para verificar lógica, usar `Juego/`.
    (Está en `com.unity.ai.assistant`, `DynamicAssemblyBuilder.GetAssetsAssemblyNames`.)

21. **Límite de 260 caracteres de ruta en Windows.** Con el repo en
    `D:\Jose\Facu\Taller de proyecto integral\Prototipo\Zombineta\Zombineta\Juego`, un archivo
    de `com.unity.2d.tooling` en `Library/PackageCache` ya no se puede leer
    (`DirectoryNotFoundException` al importar un `.uxml`). No rompe el juego, pero va a empeorar.
    Clonar el repo en una ruta corta.

22. **Cerrar Unity desde el MCP:** `EditorApplication.Exit(0)` funciona, pero si se agenda con
    `delayCall` puede tardar más de un minuto en ejecutarse. Antes de mover carpetas, confirmar
    que no queda ningún `Unity.exe` del proyecto (los `AssetImportWorker` también cuentan).


23. **En una herramienta de editor, `EditorSceneManager.NewScene` descarga los assets que nadie
    usa.** Si la herramienta cargó un asset (un `LevelSequence`, una `InputActionReference`) antes
    de crear la escena y lo asigna después, la referencia queda vacía sin error. Cargar cada asset
    justo antes de asignarlo. Las referencias a objetos de la propia escena no tienen el problema.

24. **En 6000.6, `Scene.handle` ya no se convierte a `int`** (error CS0619, obsoleto). Para
    comparar escenas usar `handle.ToString()` o `GetRawData()`.

25. **Un `Text` de uGUI cuya línea no entra en el alto de su rect no muestra nada.** Pasa con los
    controles de `DefaultControls` (su label mide 20 px): poner `verticalOverflow = Overflow`.

26. **`Juego/` tiene desactivada la recarga de dominio al entrar a Play.** Las variables estáticas
    sobreviven entre sesiones de Play: todo estático se reinicia en
    `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`
    (ver `GameRoot` y `Bootstrapper`). Lo mismo con los callbacks: un `EditorApplication.update`
    registrado desde el MCP sigue vivo después de salir de Play.

27. **Una prueba en Play por MCP no llega a tiempo al primer frame.** Entre `Play` y el primer
    `RunCommand` pasan segundos: sin input, la horda ya alcanzó a la moto y el nivel terminó. Para
    probar un nivel, registrar el callback **antes** de entrar en Play: en un mismo `RunCommand`,
    `EditorApplication.playModeStateChanged` (en `EnteredPlayMode` suma el `update`) y después
    `EditorApplication.EnterPlaymode()`. Escribir el avance en `SessionState` paso a paso, no
    todo al final.

28. **Capturar la Scene view con overlays y gizmos:** `Unity_SceneView_Capture2DScene` renderiza
    sin gizmos. Enfocar la ventana en un comando (`SceneView.lastActiveSceneView.Focus()`) y en
    el siguiente leer sus píxeles con `InternalEditorUtility.ReadScreenPixel` sobre
    `position × pixelsPerPoint`. En el mismo comando todavía se lee la pestaña que estaba delante.

29. **No enfocar ventanas del editor ni leer píxeles de la pantalla física por MCP: la máquina es
    el escritorio en uso del usuario.** `EditorWindow.Focus()` + `ReadScreenPixel` capturan lo que
    esté realmente al frente en el sistema operativo, no la ventana de Unity — en esta sesión eso
    llegó a mostrar por accidente el Gmail y el Calendar del usuario con una reunión en curso. Para
    ver el Game view con overlays (Canvas `ScreenSpaceOverlay`) en Play, usar
    `ScreenCapture.CaptureScreenshotAsTexture()` dentro del callback de update; para comparar
    encuadre sin overlays alcanza con renderizar la cámara a una `RenderTexture`
    (`Camera.Render()` + `ReadPixels`), como hace `FramingCapture`.

30. **`TextureImporter.spritesheet` compila pero ya no tiene efecto en 6000.6** (solo tira el
    warning CS0618, no un error: es fácil no notarlo). Para escribir rects de sprite desde código
    hay que usar `UnityEditor.U2D.Sprites.SpriteDataProviderFactories` +
    `ISpriteEditorDataProvider.SetSpriteRects()` + `Apply()` (lo mismo que usa el Sprite Editor por
    dentro), y agregar la referencia `Unity.2D.Sprite.Editor` al asmdef del editor.

31. **`remove_checker_background.py` puede devolver 0 píxeles transparentes en silencio**, sin
    error, si el tono real del cuadriculado no coincide con las bandas hardcodeadas
    (`CHECKER_BANDS`, pensadas para grises ~66/104). `ZombieFlesh.jpeg` tenía un cuadriculado mucho
    más claro (~212): no limpió nada y no avisó. Se resolvió con
    `remove_white_background.py --tolerance 65` en vez de tocar las bandas. Antes de asumir qué
    script usar, inspeccionar a mano el tono real del fondo.

32. **Escribir `localPosition` en un sprite "hijo" manda todo al origen del padre si ese hijo en
    realidad está en la raíz de la jerarquía.** El balanceo (`poseBob`) de `HordeView` escribía
    `sprites[i].transform.localPosition`, pero en `Zombie.prefab` el `SpriteRenderer` está en la
    raíz del prefab: los zombies con look quedaban invisibles en (0,0) del mundo (solo se veían sus
    sombras, que sí se posicionan en coordenadas de mundo). Antes de mover algo por
    `localPosition`, confirmar en qué nivel de la jerarquía vive el componente que se quiere mover.

33. **Los sprites de `ZombieFront` guardados en una escena de nivel solo se recalculan con un tick
    real del editor**, porque `LevelScene.ApplyVisual` corre desde `[ExecuteAlways] Update()`.
    Cargar la escena y tocar el GameObject por MCP no alcanza para que ese `Update()` corra: el
    sprite guardado se queda con el look viejo (o el genérico de la paleta) hasta que el editor
    tiene un frame real o se llama `ApplyVisual` a mano. Antes de dar por buena una escena con
    `ZombieFront`, abrirla, forzar `ApplyVisual` (o esperar un tick real) y guardar.

---

## 6. Qué está verificado y qué no

**Verificado:**
- 73 tests EditMode corriendo dentro de Unity (Test Runner real, no solo los shims).
- Cámara en Play Mode: con 50 m de ventaja tamaño 6,30 y con 6 m tamaño 5,00, con la mirada
  hacia adelante (6,2 u) y el borde de abajo (−4,6) iguales en los dos. Choque contra un
  obstáculo real: congelado en el impacto y vuelta a tiempo normal. Una captura real de la
  horda con el final en cámara lenta. Victoria con plano abierto. F2 a mitad de nivel: la
  cámara salta a la meta en vez de cruzar el nivel. Con efectos apagados, encuadre fijo y
  Game Over inmediato.
- El faro apunta hacia atrás (capturado en Play).
- Horda: la física de la masa, el reciclado, el disparo por carril, las muertes, las explosiones
  con cadena y el atropello tienen tests (24 nuevos). En Play se vieron los 24 zombies con la
  mezcla de tipos esperada (16/5/3), un disparo matando con su traza y su salpicadura, y la
  mancha en el asfalto. Los 32 barriles de las dos rutas detonan de un tiro y limpian la zona.
- Salto: la física, las rampas, los pickups aéreos y la cámara tienen tests. En Play, un salto
  real sobre la primera rampa de `Ruta01` despegó en la rampa, sobrevoló los dos obstáculos y
  aterrizó a 12,3 m, igual que en la simulación; capturado en el aire con la sombra en el
  carril.
- Consumo de nafta exacto contra la config, el faro frenando a la horda a la mitad,
  `Reset` restableciendo el estado, cero errores de consola en Play Mode.
- Escenario: sincronismo moto/calle con diferencia 0 sobre 263 m, parallax medido igual al
  configurado en las cinco capas, sin costuras entre tiles de la pista, y reconstrucción en
  vivo al editar `Escenario.asset` en Play.
- Flujo: las ocho pantallas recorridas en Play Mode y capturadas, incluida una derrota real
  (la horda alcanzó a la moto sola) detectada por el flujo, y el cambio efectivo de `Ruta01` a
  `Ruta02` entre niveles. El HUD de barras ya se vio funcionando.
- Estética y escala (14/09): 186 tests EditMode. Capturas de plano en calma, cerrado, atrapada y
  victoria contra `Mapa_Escala.png` con las líneas de carril y de calle a ≤2-3 px. En Play, el
  orden por carril tapa correctamente sin parpadeo durante el tween de cambio de carril, y la
  horda muestra la mezcla de looks por arquetipo con impacto y muerte animados por código.
  `Prototipo/` sin tocar.

**NO verificado — pendiente de que alguien lo juegue:**
- **El teclado en los menús.** El flujo se manejó por su API; nunca se apretó una tecla real.
  Es lo primero a probar: W/S, ENTER, ESC, A/D en Opciones, R en Game Over.
- Si la capa frontal molesta al jugar: árboles y farolas pasan en silueta por delante de los
  carriles y pueden tapar un obstáculo o la moto un instante, **más con el plano cerrado**
  (cuando la horda está encima). Se ajusta en la capa "Frontal" de `Escenario.asset` (tinte,
  alfa, huecos) sin tocar código.
- Cómo se sienten los números de la cámara con las manos en el teclado: intensidad de las
  sacudidas, velocidad del zoom, duración del congelado. Se tocan en `CameraConfig.asset`
  en pleno Play y quedan guardados.
- **Que la partida sea jugable con la horda nueva.** Ver arriba: hoy el corredor es más
  rápido que el turbo y la partida se pierde enseguida. Es lo primero.
- **El disparo con teclado**: si alinear el carril con el puntero se siente bien o molesto, y si
  se entiende que la bala se pierde cuando el carril está vacío.
- **El salto con teclado.** Ningún salto se piloteó con teclas reales: la inclinación se probó
  con pilotos automáticos en los tests. Lo primero a mirar: si 60 °/s de inclinación y el
  giro del turbo (2,6 °/s por m/s) se sienten controlables o frustrantes, y si mantener D
  (turbo) al pisar la rampa y tener que soltarlo en el aire se entiende solo.
- Que el juego sea **divertido**. Eso es lo que responde el playtest, no el simulador.
- **Cómo se siente el encuadre y la escala nuevos del sub-proyecto 4.** Nadie lo jugó con las
  manos: si las siluetas del primer plano tapan un carril un instante, si el aire para saltar
  alcanza con el ancla del piso puesto, y si se sigue leyendo bien el carril de cada cosa con
  personajes al doble de alto.

---

## 7. Qué sigue

### Dónde quedó la sesión

**13/09:** se cerró la etapa de prototipo (tag `prototipo-v1`) y se hizo el sub-proyecto 1 de
la sección 0: el repo quedó en `Prototipo/` + `Juego/` con la simulación compartida, 125 tests
en verde en los dos proyectos, y el prototipo verificado sin referencias rotas. Lo próximo es
el sub-proyecto 2 (escenas en `Juego/`), que quedó hecho el mismo día.

**14/09:** sub-proyecto 3 hecho (niveles jugables armados a mano, con paleta, generador que
respeta lo tocado y validaciones) y sub-proyecto 4 hecho (estética y escala: guía de referencia
y captura de comparación, carriles y cámara calzados con `Mapa_Escala.png`, orden por carril y
sombras, zombies por arquetipo con animación por código, escenario reacomodado — todo detallado
en la sección 0). **Lo próximo es el sub-proyecto 5 (iluminación 2D con URP), que necesita
normal maps de arte.** Mientras tanto: jugarlo con teclado para sentir el encuadre nuevo
(siluetas del primer plano, aire de salto, lectura de carril — ver sección 6), limpiar los 4
encimados de `Level_01` y decidir `goalDistance`. Lo que sigue abajo es de antes de la
reestructura.

Lo último que se hizo fueron las rampas y el salto (T12). Lo que el usuario ya anunció como
próximo paso son **las animaciones de spritesheet de la protagonista** (ver Fase 5: las hojas `hf_*.png` necesitan limpiar el fondo blanco con flood
fill y un slicing que escanee el alfa, porque no tienen grilla exacta). Las ramas de Germán,
Jesús y Juana siguen en el commit inicial y la de Seba quedó unos commits atrás de `master`
(ninguna tiene trabajo propio); conviene que las actualicen antes de tocar la escena, para
no pelearse con conflictos de `Prototipo.unity`.

### Inmediato
1. Jugarlo con teclado y anotar qué se siente mal. Es lo único que el simulador no contesta.
   Incluye la cámara: sacudidas, velocidad del zoom y congelado se ajustan en
   `CameraConfig.asset`.
2. Decidir `goalDistance` con el balance nuevo, para que la meta vuelva a ser alcanzable.
3. Rebalancear la horda (ver sección 4) y volver a jugarla.
4. Animaciones de la protagonista a partir de las hojas `hf_*.png`.

### Horda
- Arte por tipo: hoy los tres usan el mismo sprite con tinte y escala. `Personajes.png` tiene
  seis arquetipos para elegir.
- El zombie de frente **camina en el lugar**: la entrada del recorrido está a una distancia
  fija. Si se quiere que avance de verdad, hay que moverlo en la simulación.
- Las partículas y la mancha son placeholders generados (`Art/Fx/particula.png`).

### Salto
- Arte de rampa: hoy es una cuña naranja generada (`Art/Level/rampa_placeholder.png`, pivot
  abajo a la derecha: el borde alto es donde lanza). Al subirla la moto la atraviesa, porque la
  simulación lanza desde el piso en el borde alto; con arte final conviene que la moto suba la
  cuña visualmente (solo en `ScooterView`, sin tocar la simulación).
- Animación de caída: hoy la moto solo queda rotada 70°.
- Si la moto llega a la meta o la atrapan en pleno salto, queda congelada en el aire (la
  simulación deja de correr). Las rutas no ponen rampas en los últimos 150 m, así que no pasa
  en la meta.

### Escenario
Dos capas nuevas para la sensación de velocidad, con placeholders generados:
- **Vallas** (`tile_placeholder_valla.png`): la vereda de enfrente, pegada a la calle
  (parallax 1), detrás del carril de arriba.
- **Postes** (`tile_placeholder_poste.png`): bolardos en la vereda de este lado, abajo de
  todo, a parallax 1,4. Muchos objetos chicos por segundo es lo que más vende velocidad.

Y **polvo en la rueda trasera** (`WheelDustView` + `Scooter/Dust`): más cantidad cuanto más
rápido, el doble en turbo, nada en el aire.

Los tiles son placeholders en `Assets/_Zombineta/Art/Tileset/` (copiados de
`Arte/Bocetos/Tileset`). Para reemplazarlos por arte final alcanza con cambiar el sprite de
cada variante en `Escenario.asset`; el ancho se calcula solo a partir de la proporción. Tres
cosas a saber:
- A la copia de `pista` se le recortaron las primeras 74 columnas porque las líneas del
  boceto no llegaban al borde y dejaban costura. **El tile final tiene que empalmar consigo
  mismo en los dos bordes.**
- `vias` venía con fondo blanco horneado; se pasó a alfa. `senalizacion` perdió la ñ del
  nombre (se rompe entre Windows y macOS en git).
- `balcon` quedó sin usar: es un adorno para poner *sobre* las fachadas y el sistema coloca
  tiles uno al lado del otro, no superpuestos. Si hace falta, se agrega como una capa aparte
  con el mismo parallax que los edificios.

### Flujo de pantallas
Los paneles son objetos de escena bajo el Canvas, a propósito: arte puede poner el key art
(`Arte/Referencias/sketch_menu_color.png`) de fondo en `MainMenuPanel` desde el editor. Los
textos de las cinemáticas se editan en `Niveles.asset`. Agregar un nivel es agregar una
entrada ahí.

### Fase 3 — Contenido y balance (T17, T23)
La ruta ya existe y es superable, pero está generada por algoritmo. Falta pasarle la mano:
picos de tensión, un respiro después de cada tramo duro, y que los últimos 500 m se sientan
como un final. Editar `Ruta01.asset` a mano en el Inspector.

### Fase 4 — Playtest e iteración (T18, T19)
Es el 45% de la rúbrica de E2 (25% evidencia de testeo + 20% iteración). Seguir el protocolo
del doc 08 con 3 a 5 testers externos. La regla número 1: **mientras alguien testea, no lo
ayudes** — cada vez que tenés que explicar algo, es un problema de diseño para anotar.

### Fase 5 — Arte, audio y build (T20-T22, T24)
**La protagonista ya usa arte:** `Art/Player/repartidora_moto_boceto.png`, copia recortada de
`Arte/Assets/sprite_sketch.png`. Pivot en el contacto de las ruedas, 1,35 u de alto (el zombie
mide 1,24), apoyada en la línea del carril igual que los pies de la horda. El sprite vive en
el hijo `Scooter/Body`: ahí va el Animator cuando se sumen las hojas. `ScooterView` ya no
reemplaza el color del sprite sino que lo **multiplica** por un tinte leve según el modo
(turbo, retroceso, aturdida), así el arte se conserva.

Los assets están en `../../Arte/` (fuera del repo del juego):
- Spritesheets de la protagonista en moto (`hf_*.png`, 3168×1344), incluida una animación
  de disparo. Son **6 hojas distintas, no 7**: `…8e8f3dff….png` y `…8e8f3dff… (1).png` son
  el mismo archivo byte a byte. Vienen **sin alfa, con fondo blanco liso** (no cuadriculado
  como la de zombies). Para limpiarlas conviene un flood fill desde el borde y no un recorte
  global del blanco: la moto tiene partes crema y reflejos casi blancos que se comería.
- Zombies con ciclo de caminata de 8 frames, impacto y muerte. **Ojo:** la hoja
  (`ZombieViejo.png`) no tenía alfa real — el fondo "transparente" venía horneado como
  cuadriculado gris opaco. Ya se limpió con `Tools/SpritePrep/` (ver su README para la
  técnica).
- Key art del menú ya compuesto y un test de paleta que define la dirección nocturna.

Las Views ya están separadas de la lógica, así que cambiar greybox por sprites reales es
tocar `SpriteRenderer` y nada más.

**Dos temas de arte a resolver con Germán y Juana:**
1. Conviven **dos direcciones de personaje**: el test de paleta tiene casco y caja de delivery
   "Ra π" con zombies verdes; los spritesheets terminados tienen a la protagonista sin casco
   con traje rosa/negro y zombies con piel humana. Hay que elegir una.
2. Varios sheets no tienen alfa real. **No asumas que el fondo es blanco:** el de zombies
   (`ZombieViejo.png`) resultó ser un cuadriculado gris de dos tonos (~RGB 66,66,66 y
   ~RGB 104,104,104, mosaico de ~30px), no blanco opaco. Cada sheet puede tener un patrón
   distinto — conviene inspeccionar antes de asumir cuál. Necesitan pasada de transparencia
   (ver `Tools/SpritePrep/` para el caso cuadriculado).

Los sheets son de 3168×1344 sin grilla exacta (3168/5 = 633,6), así que el slicing por grilla
va a fallar. Conviene una herramienta de editor que calcule los recortes escaneando el alfa.

`Tools/SpritePrep/` resuelve el problema del cuadriculado/sin-alfa, pero se construyó para
`ZombieViejo.png` y requiere que la hoja divida exacto en columnas x filas. Sirve tal cual
para el caso zombie; la grilla no exacta de los sheets de la protagonista es un problema
aparte que `Tools/SpritePrep` todavía no maneja.

### Documentación (cuenta para la rúbrica)
Los docs 01, 03, 06 y 07 siguen vacíos, y el **07 (Plan de Prototipo) es literalmente el
entregable de E2**. Con este archivo ya hay contenido para completarlos describiendo lo que
existe, no lo que se imaginó.

---

## 8. Cosas sueltas

- El proyecto está anidado en `Prototipo/Zombineta/Zombineta`. Es feo pero moverlo con Unity
  abierto es riesgoso. El repo git está en la carpeta interna, que es lo correcto.
- La carpeta se llama `Scripts/Enemies` y no `Scripts/Horde` porque Unity dejó el archivo
  fuera de la lista de fuentes de la assembly pese a reconocerlo como `MonoScript`, y mover
  la carpeta fue lo que destrabó ese caché.
- Git LFS está activo para PNG, audio y video. Si alguien clona sin LFS instalado, va a bajar
  punteros de texto en vez de imágenes.
- El equipo: Sebastián Capurro (programación, producción), Jesús Arias (programación, QA),
  José Luis Ressia (programación, sonido), Germán Heuer y Juana Urzúa (arte).
- **E2 vence el 9 de octubre de 2026.**
