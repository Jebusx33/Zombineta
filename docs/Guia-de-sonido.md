# Guía de sonido

Esta guía es para quien hace sonido, no para programar. Todo lo que describe se hace
reemplazando archivos de audio y tocando campos en el Inspector — ningún paso pide escribir una
línea de C#.

Está escrita contra lo que **ya existe** en el proyecto hoy (los sonidos actuales son
placeholders generados por código, con el nombre y la duración definitivos: lo único que falta
es el audio final). Si algo de acá no coincide con lo que ves en el Editor, avisale a
programación antes de seguir.

---

## 1. Abrir el proyecto

- Abrí **`Juego/`** desde Unity Hub — nunca la carpeta raíz del repo, ni `Prototipo/` (ese
  proyecto está congelado).
- Unity **6000.6.0f1** exacto. Si Hub te ofrece bajar o actualizar el editor, no lo hagas sin
  preguntar.
- Para escuchar algo hace falta darle Play. Podés arrancar desde cualquier escena de pantalla
  (`MainMenu`, `Level_01`, `GameOver`, etc.): `Boot` se carga sola al lado y el flujo arranca en
  esa pantalla. No hace falta recorrer el juego entero para probar un sonido de una pantalla
  puntual — ver la tabla de la sección 7.

---

## 2. Cómo está armado el sonido (lo mínimo para ubicarse)

- No hay un `AudioMixer` de Unity: no se puede crear uno por código, y la herramienta que sí
  podría (una API interna del editor) es frágil y además el MCP la bloquea. En su lugar hay
  **cuatro "canillas" de volumen manejadas por código** (los buses): **General**, **Música**,
  **Efectos** y **UI** (UI cuelga de Efectos, pero no se le va la mano en la pausa — ver más
  abajo). Los tres sliders de Opciones son General, Música y Efectos.
- **`AudioDirector`** (vive en `Boot`, nunca se descarga) es quien reproduce los efectos: recibe
  una clave (por ejemplo "Disparo"), elige una variante al azar, un volumen y un tono dentro del
  rango que vos definiste, y lo reproduce. Nunca lo llamás vos a mano: lo dispara el código del
  juego (un disparo, un choque, un botón de menú, etc.).
- **`MusicDirector`** (también en `Boot`) maneja el tema de cada pantalla y la capa de tensión
  del nivel, con fundido cruzado. Tampoco se toca a mano.
- Lo que vos tocás son los **assets de datos**: un archivo de audio (el `.wav`) y un
  ScriptableObject `Sonido` que dice qué volumen, qué tono y qué tan seguido puede sonar. Todo
  eso está en `Settings/Audio/` (sección 3).

---

## 3. Dónde está cada cosa

- **Archivos de audio:** `Juego/Assets/_Zombineta/Audio/`, en tres carpetas: `Musica/`, `Sfx/` y
  `Ambiente/`.
- **Qué volumen/tono/variantes tiene cada efecto:** `Juego/Assets/_Zombineta/Settings/Audio/Sonidos/`
  — un asset `Sonido` por efecto (por ejemplo `Disparo.asset`).
- **Qué tema le toca a cada pantalla:** `Juego/Assets/_Zombineta/Settings/Audio/Musica.asset`.
- **Qué suena en cada nivel** (tema, tensión, ambiente y sus efectos propios):
  `Settings/Audio/Nivel01.asset` y `Nivel02.asset`.
- **El banco de efectos global** (todas las claves de la partida y del menú):
  `Settings/Audio/SonidosGlobales.asset`.
- **Los tres números del paneo/atenuación por distancia:** `Settings/Audio/Espacial.asset`.
- **El generador de placeholders** (por si hace falta un archivo de prueba nuevo mientras arte
  todavía no entregó el definitivo): menú `Zombineta > Audio > Generar placeholders`. **Nunca
  pisa un archivo que ya exista**, así que correrlo de nuevo no borra nada de lo que ya
  reemplazaste.

---

## 4. La tabla completa de archivos

Todo lo de abajo es lo que hay hoy en disco (58 archivos), con su duración real, si hace loop, sus
variantes y qué lo dispara en el juego. Las duraciones son las definitivas: el archivo final tiene
que medir lo mismo (ver la sección 6, por qué importa sobre todo en tema/tensión).

### Música (todas en loop, 16 s)

| Archivo | Pantalla / uso |
|---|---|
| `mus_menu_loop.wav` | Menú principal |
| `mus_character_loop.wav` | Selección de personaje |
| `mus_cinematic_loop.wav` | Cinemática (apertura de cada nivel) |
| `mus_level1_loop.wav` | Nivel 1 — tema de partida |
| `mus_level1_tension.wav` | Nivel 1 — capa de tensión (mide exactamente lo mismo que el tema) |
| `mus_level2_loop.wav` | Nivel 2 — tema de partida |
| `mus_level2_tension.wav` | Nivel 2 — capa de tensión (ídem) |
| `mus_levelcomplete_loop.wav` | Nivel completo |
| `mus_gameover_loop.wav` | Game Over |
| `mus_ending_loop.wav` | Final (Ending) |
| `mus_credits_loop.wav` | Créditos |

### Ambiente (loop, 10 s)

| Archivo | Uso |
|---|---|
| `amb_level1_city.wav` | Ambiente de fondo del Nivel 1 (ciudad) |
| `amb_level2_bridge.wav` | Ambiente de fondo del Nivel 2 (puente) |

### Efectos

Duración real de cada variante (los efectos no miden un número redondo exacto por el
redondeo de la compresión ADPCM al importar — la diferencia es de a lo sumo ~1,5 ms, no importa).

| Clave (`Sonido`) | Archivo(s) y duración | Loop / posicional | Qué lo dispara |
|---|---|---|---|
| Disparo | `sfx_player_shot_01` (0,151 s) / `_02` (0,131 s) | — | Disparar con munición |
| SinBalas | `sfx_player_noammo_01` (0,121 s) / `_02` (0,100 s) | — | Disparar sin munición |
| CambioCarril | `sfx_player_lane_01` (0,151 s) / `_02` (0,131 s) | — | Cambiar de carril |
| Choque | `sfx_player_crash_01` (0,601 s) / `_02` (0,550 s) | posicional | Chocar contra un obstáculo |
| Atropello | `sfx_zombie_runover_01` (0,300 s) / `_02` (0,280 s) | posicional | Arrollar un zombie |
| ExplosionBarril | `sfx_barrel_explosion_01` (1,200 s) / `_02` (1,100 s) | posicional | Explota un barril (bala o cadena) |
| ExplosionBarril (solo Nivel 2, pisa a la de arriba) | `sfx_barrel_explosion_03` (1,300 s) | posicional | Igual que arriba, pero solo en el Nivel 2 (banco propio del nivel) |
| PickupNafta | `sfx_pickup_fuel_01` (0,441 s) / `_02` (0,421 s) | — | Agarrar un bidón de nafta |
| PickupBateria | `sfx_pickup_battery_01` (0,401 s) / `_02` (0,380 s) | — | Agarrar una batería |
| PickupMunicion | `sfx_pickup_ammo_01` (0,401 s) / `_02` (0,380 s) | — | Agarrar una caja de balas |
| FaroOn | `sfx_headlight_on_01` (0,121 s) / `_02` (0,110 s) | — | Prender el faro (Espacio) |
| FaroOff | `sfx_headlight_off_01` (0,121 s) / `_02` (0,110 s) | — | Apagar el faro |
| SinNafta | `sfx_player_nofuel_01` (0,501 s) / `_02` (0,451 s) | — | Quedarse sin nafta |
| Salto | `sfx_player_jump_01` (0,200 s) / `_02` (0,181 s) | — | Pasar por una rampa |
| Aterrizaje | `sfx_player_land_01` (0,151 s) / `_02` (0,131 s) | — | Aterrizar sin ángulo perfecto |
| AterrizajePerfecto | `sfx_player_landperfect_01` (0,251 s) / `_02` (0,221 s) | — | Aterrizar con ángulo perfecto (pisa a "Aterrizaje" si coinciden) |
| Victoria | `sfx_stinger_win_01` (0,920 s) / `_02` (0,840 s) | — | Pasar a "Nivel completo" |
| Derrota | `sfx_stinger_lose_01` (1,200 s) / `_02` (1,120 s) | — | Pasar a "Game Over" |
| Motor | `sfx_engine_loop_01` (4,001 s) | **loop** | Siempre sonando en la partida; el tono sube con la velocidad y el turbo |
| Horda | `sfx_horde_loop_01` (4,001 s) | **loop**, posicional | Siempre sonando en la partida; panea y atenúa según la distancia al frente de la horda |
| ZombieAdelante | `sfx_zombie_groan_01` (1,001 s) / `_02` (0,901 s) | posicional | Un zombie suelto de frente entra en rango |
| UiMover | `sfx_ui_move_01` (0,100 s) | bus UI | Mover la selección en un menú |
| UiConfirmar | `sfx_ui_confirm_01` (0,321 s) | bus UI | Confirmar (Enter / botón Sur) |
| UiVolver | `sfx_ui_back_01` (0,341 s) | bus UI | Cancelar / volver (Esc / botón Este) |
| (aleatorio de pantalla) Game Over | `sfx_screen_groan_01` (1,401 s) / `_02` (1,501 s) | — | Cada tanto (6 a 14 s), un gemido lejano en Game Over |
| (aleatorio de pantalla) Ending y Créditos | `sfx_screen_wind_01` (1,501 s) | — | Cada tanto (6 a 14 s), viento en el Final y en Créditos |

Todos los efectos son mono, 44,1 kHz. Los "posicional" son los que se escuchan más de un lado
según dónde pasa (ver sección 7 para probarlo).

---

## 5. Formatos

- **Efectos** (todo lo que está en `Audio/Sfx/`): **WAV, 44,1 kHz, 16 bit**. Al importar, Unity
  los deja en modo "Decompress On Load" + compresión ADPCM automáticamente (hay una regla de
  importación que lo hace sola la primera vez que ve el archivo — no hace falta tocar nada en el
  Inspector).
- **Música y ambiente** (`Audio/Musica/` y `Audio/Ambiente/`): pueden ser **WAV u OGG**; hoy están
  como WAV pero Unity los importa en modo streaming con compresión Vorbis igual (misma regla
  automática). Si tu archivo final ya viene en OGG, lo podés pisar directamente con esa
  extensión — no hace falta convertirlo a WAV primero, pero si cambiás la extensión del archivo
  avisale a programación (el asset que lo referencia apunta al archivo por su GUID, y cambiar de
  `.wav` a `.ogg` es un archivo distinto para Unity, no un reemplazo).

---

## 6. Cómo reemplazar un archivo

1. **Mismo nombre, mismo lugar:** para reemplazar `sfx_player_shot_01.wav` por el disparo
   definitivo, simplemente pisá ese archivo en `Audio/Sfx/` con el mismo nombre exacto. El
   `.meta` que ya existe se conserva, así que ningún asset ni escena se rompe — no hace falta
   tocar nada más en Unity.
2. **Sumar una variante nueva** (por ejemplo un tercer disparo): agregá el archivo con el
   siguiente número, `sfx_player_shot_03.wav`, en la misma carpeta. Después abrí
   `Settings/Audio/Sonidos/Disparo.asset` y arrastrá el nuevo clip a la lista `Clips` (le sumás
   un elemento). El juego elige entre todas las variantes de la lista al azar, sin repetir la
   última que sonó.
3. **Sacar una variante:** simplemente quitala de la lista `Clips` del `Sonido`. No hace falta
   borrar el archivo del disco (aunque podés, si ya no la usa nadie).

---

## 7. Cómo hacer un loop sin clic (y por qué tema y tensión tienen que medir lo mismo)

- **El loop sin clic** es responsabilidad de cómo editás el archivo antes de exportarlo: el
  final del clip tiene que empalmar con el principio sin un salto brusco de volumen. La forma
  más simple es que el inicio y el final del archivo estén los dos en (o muy cerca de) el
  silencio — un cruce por cero — y no cortar en la mitad de una nota o de un golpe. Si tu editor
  de audio tiene una herramienta de "loop" o de crossfade en el propio archivo, es el momento de
  usarla antes de exportar. Unity no hace nada para disimular un loop mal cortado: reproduce el
  archivo tal cual se lo diste, una y otra vez.
- **Por qué el tema y la tensión de un nivel tienen que medir exactamente lo mismo:** el
  `MusicDirector` arranca las dos fuentes (el tema y la capa de tensión) en el mismo instante
  exacto y las dos hacen loop por separado. Si una dura, aunque sea, una décima de segundo más
  que la otra, con cada vuelta del loop se van a ir desfasando cada vez más — al principio no se
  nota, pero después de unos minutos de partida la tensión y el tema quedan sonando "corridos"
  uno contra el otro. El juego avisa por consola si detecta que difieren (a programación, no a
  vos), pero **la única forma de arreglarlo de raíz es que los dos archivos midan lo mismo,
  sample por sample**. Si tu editor exporta en segundos redondeados, exportá los dos con el
  mismo ajuste y confirmá la duración final de los dos archivos antes de subirlos.
- Mismo criterio para los dos loops de ambiente (`amb_level1_city.wav`, `amb_level2_bridge.wav`)
  y para el loop del motor y de la horda: no tienen pareja que sincronizar, pero igual necesitan
  un empalme limpio para no chasquear cada 4 o 10 segundos.

---

## 8. Cómo ajustar volumen, tono y cooldown de un efecto

Cada efecto tiene su asset `Sonido` en `Settings/Audio/Sonidos/` (se llaman igual que la clave de
la tabla de la sección 4). Seleccionalo en el Project y vas a ver estos campos en el Inspector:

- **`Clips`**: la lista de variantes (sección 6).
- **`Volumen`** (mínimo y máximo): cada vez que suena, se elige un volumen al azar dentro de ese
  rango. Hoy casi todos están en 0,6–0,8 (moderado, porque son placeholders sintetizados y
  suenan más cansadores que un sonido real a ese volumen — cuando pongas el audio definitivo,
  probablemente puedas subirlos). Los de UI están en 0,6–0,6 (fijo).
- **`Tono`** (mínimo y máximo, 1 = normal): variación de tono al azar en cada reproducción, para
  que diez disparos seguidos no suenen todos clonados. El rango por defecto es 0,95–1,05 (una
  variación chica, casi no se nota como "pitch" pero rompe la monotonía).
- **`Bus`**: a qué canilla de volumen pertenece — Efectos, Ambiente o UI. Salvo que programación
  te diga lo contrario, no lo cambies: UI, por ejemplo, es el único bus que sigue sonando en la
  pausa, así que un efecto de partida puesto ahí por error se escucharía aunque el juego esté
  pausado.
- **`Posicional`**: si está tildado, el sonido panea y se atenúa según a qué distancia pasa
  respecto de la moto (ver los tres números de `Espacial.asset`, sección 9). No lo tildes en un
  efecto que no tiene una posición en el mundo (por ejemplo los de UI).
- **`Loop`**: solo para Motor, Horda y los dos Sonido de ambiente. No lo actives en un efecto
  nuevo salvo que sepas que va a manejarlo un componente propio (los sonidos en loop no pasan
  por el pool de disparos comunes).
- **`Cooldown`** (segundos): el tiempo mínimo entre dos reproducciones del mismo efecto. Sirve
  para que, por ejemplo, diez zombies muriendo en el mismo cuadro no disparen diez sonidos
  superpuestos y saturados — con el cooldown puesto, los que llegan demasiado seguido
  simplemente no suenan. Hoy está en 0,05 s en todos: si notás que un efecto se "traga" copias
  que sí querés escuchar, se puede bajar; si un efecto se siente ametrallado, se puede subir.

**Ejemplo concreto:** para que el disparo suene un poco más fuerte, abrís `Disparo.asset` y
subís el rango de `Volumen` (por ejemplo de 0,6–0,8 a 0,75–0,9). Guardás el asset con Ctrl+S. No
hace falta tocar código ni volver a entrar a Play desde cero: el próximo disparo ya sale con el
volumen nuevo.

---

## 9. El paneo y la distancia (sonidos posicionales)

Los tres números que gobiernan cómo panea y se atenúa un sonido "posicional" (Choque, Atropello,
ExplosionBarril, ZombieAdelante y Horda) están en un solo lugar:
**`Settings/Audio/Espacial.asset`**:

- **`Ancho Paneo`** (hoy 12 m): a esta distancia de la moto (adelante o atrás) el sonido ya suena
  completamente de un solo lado (paneo a fondo). Lo que pasa atrás de la moto suena a la
  izquierda; lo que pasa adelante, a la derecha.
- **`Distancia Plena`** (hoy 4 m): hasta esta distancia el sonido suena a su volumen completo.
- **`Distancia Maxima`** (hoy 30 m): más allá de esta distancia, el sonido no se escucha nada
  (volumen 0). Entre "Distancia Plena" y "Distancia Maxima" el volumen baja de forma pareja.

Si un efecto posicional se siente "pegado" siempre al centro, o si nunca se llega a escuchar
algo que debería sonar lejos, es este asset el que hay que ajustar (no el `Sonido` del efecto en
sí).

---

## 10. Cómo probar cada cosa

No hace falta jugar el nivel entero para escuchar un cambio. Dale Play directamente sobre la
escena que te interesa (`Boot` carga sola al lado):

| Qué querés escuchar | Escena para Play | Qué hacer |
|---|---|---|
| Tema del menú | `MainMenu` | Nada, empieza a sonar solo |
| Tema de selección de personaje | `CharacterSelect` | Nada |
| Tema de la cinemática | `Cinematic` | Nada |
| Tema y tensión del Nivel 1 o 2 | `Level_01` / `Level_02` | Nada para el tema; para escuchar la tensión subir, dejá que la horda se acerque (o esperá, avanza sola) |
| Motor y su tono | `Level_01` / `Level_02` | Acelerá con turbo (mantener D o →) y notá cómo sube el tono |
| Disparo / sin balas | `Level_01` / `Level_02` | Disparar (X o click izquierdo), con y sin munición |
| Cambio de carril | cualquier nivel | W/S o ↑/↓ |
| Choque | cualquier nivel | Chocar contra un obstáculo |
| Atropello / explosión de barril | cualquier nivel | Arrollar un zombie de frente / dispararle a un barril |
| Faro on/off | cualquier nivel | Espacio |
| Salto, aterrizaje, aterrizaje perfecto | cualquier nivel | Pasar por una rampa; para el aterrizaje perfecto hay que caer bien nivelada (ver la sección de salto del GDD) |
| Pickups (nafta, batería, munición) | cualquier nivel | Agarrar el objeto correspondiente |
| Ambiente del nivel | `Level_01` / `Level_02` | Nada, es un loop de fondo |
| Victoria / Nivel completo | cualquier nivel | Llegar a la meta, o F2 en el editor (teletransporta a la meta) |
| Derrota / Game Over | cualquier nivel | Dejarse atrapar por la horda, o F3 en el editor |
| Gemido aleatorio de Game Over | `GameOver` | Esperar entre 6 y 14 segundos |
| Tema y viento del Final | `Ending` | Nada |
| Tema y viento de Créditos | `Credits` | Nada (se llega desde el menú con el botón "Créditos", o desde el Final) |
| Efectos de menú (mover, confirmar, volver) | cualquier pantalla con menú | Navegar con W/S o el stick, confirmar con Enter, volver con Esc |
| Pausa silenciando efectos | cualquier nivel | Esc — los efectos y el ambiente se callan, la música baja pero sigue sonando de fondo |
| Los tres sliders de volumen | `Options` | Mover "General", "Musica" y "Efectos" y confirmar que cada uno afecta lo suyo |

---

## 11. Qué no tocar

- **No hay que crear un `AudioMixer` de Unity.** Se probó y la única forma de hacerlo desde
  código es una API interna y frágil del editor — por eso la mezcla se resuelve con los cuatro
  buses de la sección 2. Si en algún momento el equipo decide sumar un `AudioMixer` de verdad
  (por ejemplo para efectos de reverb o un limitador), es un cambio de arquitectura para
  programación, no algo para armar solo desde los assets.
- **Nunca se asigna el volumen de una fuente de audio directamente** (lo que en el Inspector de
  un `AudioSource` es el campo "Volume"). Absolutamente todas las fuentes del juego lo calculan
  solas, cuadro a cuadro, a partir del volumen que vos pusiste en el `Sonido` y del bus que le
  corresponde. Si alguna vez ves un `AudioSource` en una escena o un prefab con el campo
  "Volume" tocado a mano (distinto de 1), es un bug: avisale a programación en vez de "arreglarlo"
  subiendo o bajando ese número, porque el próximo cuadro el bus se lo va a pisar igual.
- **El campo `Bus` de un `Sonido`**: cambiarlo sin avisar puede hacer que un efecto de partida
  quede sonando en la pausa (si lo pasás a UI) o que un sonido de menú se calle quede afectado
  por los sliders de música o efectos de una forma rara. Si necesitás moverlo, coordinalo con
  programación.
- **La clave (`SonidoClave`) de un efecto**: es un número interno fijo que usa el código para
  pedir "el sonido del disparo", etc. No se puede renombrar ni reordenar desde los assets — eso
  es un cambio de código.
- **`Settings/Audio/SonidosGlobales.asset` y los `BancoNivelXX.asset`**: son los que deciden qué
  `Sonido` corresponde a cada clave. No hace falta tocarlos para reemplazar un archivo de audio
  (eso se hace pisando el `.wav`, sección 6) ni para ajustar volumen/tono (eso se hace en el
  `Sonido`, sección 8). Solo hacen falta si programación agrega una clave nueva.
- **El generador de placeholders** (`Zombineta > Audio > Generar placeholders`) nunca pisa un
  archivo que ya existe, así que correrlo de nuevo no te puede borrar el trabajo de sonido ya
  hecho — pero tampoco sirve para "restaurar" un archivo placeholder si ya lo reemplazaste y
  querés volver atrás: para eso hay que recuperarlo de una versión anterior en git.
