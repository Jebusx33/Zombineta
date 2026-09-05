# Zombineta — Handoff

Última actualización: 5 de septiembre de 2026, tras cerrar la Fase 2.

**Estado:** el prototipo se juega de punta a punta en greybox. Se puede ganar llegando al
refugio y perder por nafta o por la horda. 31 tests EditMode en verde.

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
- El mundo es estático y la jugadora avanza en X real. La cámara la sigue **por detrás**
  (18 m), dejándola a la derecha de la pantalla con aire a la izquierda para ver venir a la
  horda. Si la cámara va adelante, la amenaza queda fuera de cuadro justo cuando importa.
- Nivel de **distancia fija**: 4000 m ≈ 5 minutos. El "refugio" es la meta al final.

### Verbos

| Acción | Input | Efecto |
|---|---|---|
| Cambiar carril | ↑/↓ o W/S | Tween de 0,15 s |
| Turbo | → o D (mantener) | ×1,8 velocidad, ×3 consumo |
| Retroceso | ← o A (mantener) | Marcha atrás real (−0,5×), ×0,5 consumo |
| Faro | ESPACIO (toggle) | La horda avanza a **×0,5**. Drena batería |
| Disparar | X o click izq. | Empuja la horda **−15 m**. Munición limitada |
| Reiniciar | R | En las pantallas de fin |
| Números de debug | F1 | HUD crudo para balancear |

### La decisión de diseño que sostiene todo

**El faro y la pistola hacen lo mismo (frenar a la horda) pero de forma distinta:**

- **Faro** = efecto *sostenido de tasa*. Compra tiempo. Cuesta batería, no nafta.
- **Pistola** = *impulso instantáneo*. Compra espacio. Munición limitada.

Como el faro no gasta nafta, es la herramienta eficiente y la pistola es el botón de pánico.
Eso es lo que crea la brecha de habilidad: en la simulación, quien usa el faro llega y quien
solo usa turbo muere a mitad de camino. **Si tocás el balance, verificá que esa brecha siga
existiendo** — es la razón de ser del prototipo.

### Fuera del alcance (deliberado)

Rampas y salto (T12), puertas y trampas ambientales (T13), selección de personaje,
cinemáticas, menú de opciones, múltiples niveles, victoria alternativa por tiempo.

---

## 2. Arranque rápido

- **Unity 6000.6.0f1** (en `D:\Dev\Unity`). Todo el equipo tiene que usar exactamente esta.
  Ojo: **6000.6 no es LTS.** Sigue pendiente decidir si se quedan acá o bajan a 6000.0 LTS.
- Abrí `Assets/_Zombineta/Scenes/Prototipo.unity` y dale Play. ENTER arranca.
- El proyecto es 2D URP con Renderer2D. El faro es un `Light2D` real, así que la escena
  depende de la iluminación 2D: si sacás el `Global Light 2D`, se ve todo negro.

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
    CameraFollow.cs
  Player/
    PlayerIntent.cs    Lo que la jugadora QUIERE hacer. Desacopla input de reglas.
    PlayerInputReader.cs   Único lugar que sabe qué teclas existen.
    ScooterView.cs
    HeadlightView.cs   Light2D + titileo con batería baja.
  Enemies/
    HordeView.cs       La sim conoce un número (HordeX); esto lo dibuja como masa.
  Level/
    LevelDefinition.cs El recorrido como datos (ScriptableObject).
    LevelRuntime.cs    Resuelve encuentros. C# plano.
    LevelSpawner.cs    Recicla sprites sobre la ventana visible.
    GoalView.cs        El refugio.
    LaneMarkersView.cs Rayas de la calzada (dan la sensación de velocidad).
  UI/
    HudView.cs         Barras, sin números.
    DebugHudView.cs    Números crudos, detrás de F1.
    ScreenFlow.cs      Menú -> Level -> Victoria / Game Over.
```

### Dos decisiones que conviene no revertir sin pensarlo

**Los encuentros se resuelven por distancia, no con triggers de física.** La moto se mueve
por transform, no por Rigidbody, así que un collider sería frágil. `LevelRuntime.Collect`
resuelve el *tramo* recorrido entre dos posiciones, lo que garantiza que nada se saltee yendo
en turbo y que el retroceso recoja lo que dejaste pasar.

**`worldUnitsPerMeter = 0.25`.** La simulación piensa en metros, pero 40 m de ventaja no
entran en pantalla a escala 1:1. Las Views comprimen la distancia; el balance no se entera.

---

## 4. Balance

Todo vive en `Assets/_Zombineta/Settings/GameConfig.asset`. Balancear es editar ese asset,
nunca tocar código.

El recorrido vive en `Ruta01.asset`: 111 entradas (31 bidones, 7 baterías, 9 cajas de balas,
64 obstáculos), generado con semilla fija para que dos playtests sean comparables. Los
obstáculos empiezan ralos y se van cerrando, y **nunca bloquean los tres carriles a la vez**.

### Estado verificado

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
   copiarlos.

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

8. `Unity_Camera_Capture` no muestra canvases Screen Space Overlay, así que **el HUD no se
   puede verificar desde acá**. Tampoco se pueden enviar teclas.

---

## 6. Qué está verificado y qué no

**Verificado:**
- 31 tests EditMode corriendo dentro de Unity (Test Runner real, no solo los shims).
- Consumo de nafta exacto contra la config, el faro frenando a la horda a la mitad,
  `Reset` restableciendo el estado, cero errores de consola en Play Mode.
- La ruta completa es superable por una estrategia hábil e insuperable por una torpe.

**NO verificado — pendiente de que alguien lo juegue:**
- El flujo de pantallas end-to-end. El arranque se simuló por código; nunca se apretó ENTER.
- El HUD de barras (no sale en las capturas de cámara).
- El efecto visual del faro. Está configurado pero nadie lo vio funcionando.
- Que el juego sea **divertido**. Eso es lo que responde el playtest, no el simulador.

---

## 7. Qué sigue

### Inmediato
Jugarlo y anotar qué se siente mal. Es lo único que el simulador no puede contestar.

### Fase 3 — Contenido y balance (T17, T23)
La ruta ya existe y es superable, pero está generada por algoritmo. Falta pasarle la mano:
picos de tensión, un respiro después de cada tramo duro, y que los últimos 500 m se sientan
como un final. Editar `Ruta01.asset` a mano en el Inspector.

### Fase 4 — Playtest e iteración (T18, T19)
Es el 45% de la rúbrica de E2 (25% evidencia de testeo + 20% iteración). Seguir el protocolo
del doc 08 con 3 a 5 testers externos. La regla número 1: **mientras alguien testea, no lo
ayudes** — cada vez que tenés que explicar algo, es un problema de diseño para anotar.

### Fase 5 — Arte, audio y build (T20-T22, T24)
Los assets están en `../../Arte/` (fuera del repo del juego):
- 7 spritesheets de la protagonista en moto (3168×1344), incluida una animación de disparo.
- Zombies con ciclo de caminata de 8 frames, impacto y muerte, con alfa.
- Key art del menú ya compuesto y un test de paleta que define la dirección nocturna.

Las Views ya están separadas de la lógica, así que cambiar greybox por sprites reales es
tocar `SpriteRenderer` y nada más.

**Dos temas de arte a resolver con Germán y Juana:**
1. Conviven **dos direcciones de personaje**: el test de paleta tiene casco y caja de delivery
   "Ra π" con zombies verdes; los spritesheets terminados tienen a la protagonista sin casco
   con traje rosa/negro y zombies con piel humana. Hay que elegir una.
2. Varios sheets tienen **fondo blanco opaco**, no alfa. Necesitan pasada de transparencia.

Los sheets son de 3168×1344 sin grilla exacta (3168/5 = 633,6), así que el slicing por grilla
va a fallar. Conviene una herramienta de editor que calcule los recortes escaneando el alfa.

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
