# Sistema de sonido — Diseño

Sub-proyecto 7 del juego definitivo (`Juego/`). Suma también la pantalla de Créditos.

## Objetivo

Hoy el juego no suena: no hay clips, ni `AudioSource`, ni mixer. Solo existe un slider de volumen
en Opciones que maneja `AudioListener.volume`. Esta etapa agrega:

- música por pantalla y por nivel, con fundido cruzado entre pantallas;
- efectos de la partida, de UI y de ambiente, algunos posicionales respecto de la moto;
- control de volumen General, Música y Efectos;
- la pantalla de Créditos.

Todo con sonidos provisorios que el equipo reemplaza pisando archivos.

Fuera de alcance: middleware (FMOD/Wwise), voces, mezcla fina y arte sonoro final.

## Decisiones tomadas

- **Motor:** sistema propio sobre el audio de Unity: buses de volumen por código, un director
  en `Boot` y ScriptableObjects editables. El paneo y la atenuación los calculamos nosotros.
- **Volúmenes:** General, Música y Efectos. Se guardan y reemplazan al slider único de hoy.
- **Música:**
  - un tema por pantalla y por nivel, con fundido cruzado;
  - en el nivel, una capa de tensión que sube con la cercanía de la horda.
- **Efectos:**
  - eventos de la partida, motor en loop, horda posicional, UI y ambiente por nivel;
  - variantes aleatorias con rango de volumen y tono;
  - un banco global y otro por nivel.
- **Pantallas con música propia:** Game Over, Ending y Créditos, además de las demás.
- **Créditos:** pantalla nueva. Se entra desde el menú principal o después del Ending al ganar.
- **Material:** placeholders generados por código, con nombre y duración definitivos.

## 1. Mezcla

Unity no permite crear un `AudioMixer` por script con su API pública (solo con una API interna
de editor, frágil, que además el MCP bloquea). La mezcla se hace con **buses por código**:

- `AudioBuses` (C# plano, testeado) guarda la ganancia de cada bus: `General`, `Musica`,
  `Efectos` (con `Ambiente` adentro) y `UI` (cuelga de Efectos). La ganancia final de una fuente
  es `volumenPropio × bus × General`, y la de Efectos/Ambiente además por el "ducking" de pausa.
- `GameSettings` suma `MusicVolume` y `SfxVolume` (0–1, claves `zombineta.musicVolume` y
  `zombineta.sfxVolume`, default 0,8); `Volume` pasa a ser el General.
- Los sliders son lineales pero la ganancia usa una curva perceptual (`v²`), testeada.
- `GameRoot.ApplySettings` deja de tocar `AudioListener.volume` (queda en 1) y escribe los
  buses. Opciones muestra tres sliders.
- **Pausa:** Efectos y Ambiente bajan a 0 en 0,2 s (tiempo real) y la música a ×0,4; al
  reanudar vuelve todo. UI no se toca.
- Toda fuente del juego pasa por un componente que aplica su bus cada cuadro; ningún script
  asigna `AudioSource.volume` directo.

## 2. Música

`MusicDirector` es un MonoBehaviour en `Boot` que nunca se descarga. Escucha `GameFlow.Changed`.

- **`MusicaDelJuego`** (ScriptableObject, `Settings/Audio/Musica.asset`) asigna un clip a cada
  pantalla: menú, opciones, personaje, cinemática, nivel completo, Game Over, Ending y Créditos.
  - Opciones y personaje pueden quedar vacías: en ese caso siguen con el tema que venía sonando.
  - La pausa no cambia el tema; solo lo atenúa (ver 1).
- **Nivel:** `LevelInfo` suma `AudioDeNivel audio` (ver 3), con `musica` (el tema del nivel) y
  `tension` (la capa de tensión, de la misma duración que el tema).
- **Fundido cruzado:** dos `AudioSource` que alternan, con 1,5 s por defecto (campo del asset).
  - Si la pantalla nueva tiene el mismo clip que el que suena, no pasa nada: no se corta ni se
    reinicia.
  - Si no tiene clip, el tema que venía sigue sonando.
- **Tensión:** una tercera fuente que arranca en el mismo `dspTime` que el tema del nivel y en
  loop, con volumen 0. Durante la partida, el volumen sigue a la amenaza de la horda: 0 a 45 m o
  más, 1 encima de la moto. Se usan los mismos metros que la barra del HUD (`dangerGapMeters`), con
  un suavizado de 0,5 s. Al salir del nivel, la tensión funde junto con el tema.
- **Regla de transición** (qué hacer ante cada cambio: seguir, cruzar o cortar la tensión): es
  C# plano y está testeada.

## 3. Efectos

**`Sonido`** (ScriptableObject) representa un efecto. Campos:

- `clips` (variantes);
- `volumen` (rango min–max);
- `tono` (rango min–max);
- `grupo` (Efectos, Ambiente o UI);
- `posicional` (bool);
- `loop` (bool);
- `cooldown` (s, para que diez disparos en un frame no suenen diez veces).

Elige una variante al azar sin repetir la última. La elección es una función pura, testeada, con
semilla inyectable.

**Bancos:**

- **`BancoDeSonidos` global** (`Settings/Audio/SonidosGlobales.asset`): mapea una clave
  (`enum SonidoClave`) a un `Sonido`.
- **`AudioDeNivel`** (por nivel, en `LevelInfo.audio`) trae su propio `BancoDeSonidos`. Lo que
  define pisa al global y lo que no define cae al global. Suma también `musica`, `tension` y
  `ambiente` (un `Sonido` en loop).

**Claves iniciales:**

| Grupo | Claves |
|---|---|
| Partida | `Disparo`, `SinBalas`, `CambioCarril`, `Choque`, `Atropello`, `ExplosionBarril`, `PickupNafta`, `PickupBateria`, `PickupMunicion`, `FaroOn`, `FaroOff`, `SinNafta`, `Salto`, `Aterrizaje`, `AterrizajePerfecto`, `Victoria`, `Derrota` |
| Continuos | `Motor`, `Horda`, `Ambiente` (este último sale de `AudioDeNivel`) |
| UI | `UiMover`, `UiConfirmar`, `UiVolver` |

Cada clave sale de un `RunEvent` (`Shot`, `ShotDenied`, `LaneChanged`, `Crashed`, `RanOver`,
`Explosion`, `PickedUp`, `HeadlightOn/Off`, `RanOutOfFuel`, `Launched`, `Landed`,
`LandedPerfect`). `PickedUp` es una sola bandera; el tipo de pickup se deduce del estado de la
simulación en ese paso (qué recurso subió). Si no alcanza, se agrega un dato aditivo a la
simulación sin cambiar reglas.

**Disparo de efectos:**

- `SfxDirector` (en la escena del nivel, igual que `FxManager`) escucha `RunController.Stepped`,
  el mismo flujo que usan los efectos visuales, la vibración y los destellos. Traduce cada
  `RunEvent` a una clave y lo reproduce con un pool de `AudioSource` (16 por defecto; si se llena,
  pisa el más viejo que no sea loop).
- Victoria y Derrota se disparan al pasar el flujo a `LevelComplete` o `GameOver`.
- **UI:** un componente `UiSonidos` en cada pantalla reproduce `UiMover` cuando cambia la
  selección del `EventSystem`, `UiConfirmar` en `Submit` y `UiVolver` en `Cancel`. Suena por el
  bus UI, que no se silencia en la pausa.
- **Aleatorios de pantalla:** las pantallas Game Over, Ending y Créditos pueden tener una lista de
  `Sonido` que suena de vez en cuando (intervalo min–max en segundos). Por ejemplo, un gemido
  lejano en el Game Over. Lo maneja un componente `SonidosAleatorios` en la escena de la pantalla.

## 4. Posicional

Para un efecto con `posicional = true` y posición en el mundo, `SonidoEspacial` (C# plano,
testeado) calcula el paneo y el volumen a partir de `dx = x_sonido − x_moto`:

- **paneo:** `clamp(dx / anchoPaneo, −1, 1)`, con `anchoPaneo` = 12 m. Lo de atrás suena a la
  izquierda y lo de adelante, a la derecha;
- **volumen:** 1 hasta `distanciaPlena` (4 m) y después cae lineal hasta 0 en `distanciaMaxima`
  (30 m);
- los tres números están en `Settings/Audio/Espacial.asset`.

Las fuentes quedan en 2D (`spatialBlend = 0`) y el paneo y el volumen los pone el director. No se
usa el audio 3D de Unity.

Efectos posicionales: explosión de barril, choque, atropello, zombies sueltos adelante (el gemido
de `ZombieFront`) y la horda.

## 5. Continuos

- **Motor** (`MotorSonido`, en la moto): loop por el grupo Efectos.
  - El tono va de 0,8 (quieta) a 1,2 (velocidad normal) y a 1,45 con turbo, con un suavizado de
    0,15 s.
  - Sin nafta, el volumen cae a 0 en 1 s. Al perder, se corta con un fundido de 0,3 s. En la
    pausa, lo silencia el bus de Efectos.
  - El mapeo velocidad → tono es una función pura, testeada.
- **Horda** (`HordaSonido`): loop posicional en el frente de la horda (`HordeX`), con el paneo y
  la atenuación de 4. Está en un objeto propio; nunca en `Nivel` (ver la trampa de `HordeGlow`).
- **Ambiente:** loop del `AudioDeNivel` por el grupo Ambiente. Entra con un fundido de 1 s y sale
  con el tema.

## 6. Pantalla de Créditos

- `GameScreen` suma `Credits`, al final del enum para no romper lo serializado. Es un cambio
  aditivo en la simulación compartida; el prototipo no la usa.
- **Flujo:**
  - `MainMenu` va a `Credits` con `OpenCredits()`.
  - `Ending` va a `Credits` con `ShowCredits()`, que es a lo que lleva ahora el botón Continuar del
    Ending.
  - `Credits` va a `MainMenu` con `ToMainMenu()`. Pasa al terminar el rodado, con Cancel, o con el
    botón Volver.
  - Todo eso está testeado en `GameFlowTests`.
- **Escena** `Scenes/Credits.unity` (está en Build Settings y `SceneRoutePlanner` la conoce):
  - Muestra un texto que sube solo, leído de `Settings/Creditos.asset`: secciones con título y
    líneas, que el equipo edita.
  - Submit acelera el rodado ×3 mientras se mantiene apretado.
  - Cancel vuelve al menú.
  - Tiene su música y sus aleatorios.
- **Menú principal:** suma el botón "Créditos" entre Opciones y Salir.

## 7. Archivos y placeholders

Nombres según la guía de estilo (`tipo_nombre_variante`):

- `Audio/Musica/mus_menu_loop.ogg`, `mus_level1_loop.ogg`, `mus_level1_tension.ogg`, …
- `Audio/Sfx/sfx_player_shot_01.wav`, `_02`, …, `sfx_ui_move_01.wav`, …
- `Audio/Ambiente/amb_level1_city.ogg`, …

Todos van bajo `Juego/Assets/_Zombineta/Audio/`.

- **Generador de placeholders:** herramienta de editor `Zombineta > Audio > Generar
  placeholders`.
  - Sintetiza WAV: tonos, ruido filtrado y barridos, uno distinto por clave.
  - Cada archivo tiene la duración pensada para el final: loops de música de 16 s, ambiente de
    10 s, efectos de 0,1 a 1,5 s.
  - No pisa un archivo que ya exista, así que nunca borra el trabajo de sonido.
- **Reemplazo:** se pisa el archivo con el mismo nombre. El `.meta` se conserva, así que no hay
  que tocar assets ni escenas. Para sumar una variante, se agrega el archivo `_03` y se suma a
  `clips` del `Sonido`.
- **Importación:**
  - Música y ambiente: Streaming, Vorbis.
  - Efectos: Decompress On Load, ADPCM.
  - Todo sin "Load In Background" para los efectos cortos.
- **Guía corta para sonido:** `docs/Guia-de-sonido.md`. Explica:
  - la lista de archivos con su duración y si es loop;
  - formatos (WAV 44,1 kHz 16 bit para efectos, OGG para música y ambiente);
  - cómo hacer un loop sin clic;
  - cómo probar cada cosa (en qué escena, con qué acción);
  - cómo ajustar volúmenes y rangos en los assets.

## 8. Pruebas

**EditMode:**

- buses y curva de volumen;
- elección de variante sin repetición;
- `SonidoEspacial` (paneo y volumen en los bordes, detrás y adelante);
- regla de transición de la música;
- tono del motor;
- resolución del banco (nivel sobre global);
- flujo de Créditos en `GameFlowTests`.

Los 243 tests actuales siguen en verde.

**Play por MCP, entrando por el menú:**

- el tema del menú suena;
- al pasar a personaje sigue sin cortarse;
- al ir a la cinemática hay fundido cruzado;
- el nivel tiene tema y tensión sincronizados, y la tensión sube al acercar la horda;
- un disparo y una explosión reproducen su clave, con paneo del lado correcto;
- en la pausa los efectos callan;
- los sliders cambian la ganancia de los buses;
- Game Over, Ending y Créditos tienen su tema;
- Créditos se abre desde el menú y desde el Ending.

Se verifica leyendo el estado de las fuentes (clip, volumen, `panStereo`, `isPlaying`) desde el
callback, no "escuchando".

**A mano:** que José lo juegue con auriculares y confirme que el paneo y los fundidos se sienten
bien.

## Riesgos

- **Placeholders molestos:** los sonidos sintetizados pueden cansar. Por eso los volúmenes por
  defecto son moderados y se ajustan en los assets.
- **Sincronía de la tensión:** si el tema y la capa no miden lo mismo, se desfasan en el loop. El
  generador los crea iguales y la guía lo exige; el director avisa por consola si difieren.
- **`GameScreen` compartido:** se agrega al final del enum; un `switch` del prototipo sin `default`
  no rompe la compilación.
