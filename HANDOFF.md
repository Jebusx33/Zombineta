# Zombineta — Handoff

Última actualización: 11 de septiembre de 2026, tras convertir la horda en individuos con tipos, disparo con traza, muertes y partículas.

**Estado:** el prototipo se recorre de punta a punta con el flujo del GDD (menú, opciones,
personaje, cinemática, dos niveles, victoria, game over, final), sobre un escenario de
placeholders con parallax en cinco capas, una cámara que reacciona a la persecución y rampas
con un salto que se regula inclinando en el aire, y una horda de zombies individuales
con tipos. 125 tests EditMode en verde.

**Ojo:** con el balance vigente la meta **no se alcanza jugando**, y desde que la horda tiene tipos es peor: **el corredor (×1,35 sobre 17,5 = 23,6 m/s) es más rápido que el turbo (21,6 m/s)**, así que nada lo despega y la partida se pierde a los pocos segundos. Hay que rebalancear antes del próximo playtest (ver sección 4). Para
recorrer el flujo completo existen F2 (ganar) y F3 (perder), solo en editor y builds de
desarrollo.

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
- Abrí `Assets/_Zombineta/Scenes/Prototipo.unity` y dale Play. Arranca en el menú.
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

**`worldUnitsPerMeter = 0.25`.** La simulación piensa en metros, pero 40 m de ventaja no
entran en pantalla a escala 1:1. Las Views comprimen la distancia; el balance no se entera.

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
  exagerada (`jumpHeightToWorld = 0,5 u/m` contra 0,25 en X) para que el salto se lea.

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

### Lo primero a rebalancear: el corredor es imposible de despegar

Con `hordeBaseSpeed = 17,5` y el corredor en ×1,35, el frente de la horda va a **23,6 m/s**
contra los 21,6 del turbo: **ninguna maniobra lo despega** y la partida se pierde a los pocos
segundos (medido en Play: derrota a los 42 m). Antes de los tipos esto no pasaba porque toda la
horda iba a 17,5.

Dos salidas, las dos a decidir jugando:
1. **Bajar `hordeBaseSpeed`** a ~14, que deja al corredor en 18,9: más rápido que el modo Normal
   (12) pero más lento que el turbo (21,6). El turbo vuelve a ser la respuesta.
2. **Bajar el `speedMultiplier` del corredor** en `Zombies.asset` a ~1,15 (20,1), dejando la
   base como está: más tenso, pero el turbo apenas gana.

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

---

## 7. Qué sigue

### Dónde quedó la sesión
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
