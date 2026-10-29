# Plan: controles de beat'em up, granadas y bombas, y pelea por oleadas (sin timer ni ambulancia)

Origen: reunión del equipo después del playtest de Jesús (ver `2026-10-22-playtest-jesus.md`) y las
respuestas del equipo a las preguntas abiertas. Estado: **implementado en código (fases 1 a 4) y
verificado fuera de Unity; falta verlo en Play** y regenerar los niveles para que salgan los items nuevos.

## Lo que se decidió

### 1. Controles: un solo esquema, como en los beat'em up

| | Teclado | Mando |
|---|---|---|
| **A pie** | Flechas: mover · **X**: atacar (golpe y combo) · **Z**: disparo · **C**: granada · **Espacio**: bomba | Stick: mover · **A** golpe · **X** o **RB** disparo · **LB**: granada · **Y**: bomba |
| **Moto** | Flechas: **↑/↓** carril, **→/←** acelerar y frenar · **Espacio**: luz · **X**: disparo · **Z**: granada | **RT/LT** acelerar y frenar · **X** o **RB** disparo · **LB**: granada · **Y**: luz |

- Los botones **cambian de función según el modo** (X es disparo en la moto y ataque a pie; Z es granada
  en la moto y disparo a pie; Espacio es la luz en la moto y la bomba a pie).
- **Mouse fuera.** **WASD** sigue andando como alternativa, pero no se anuncia en ningún cartel.
- **Mando:** como estaba, más **LB** para la granada en los dos modos. **Y** es la luz en la moto y la
  bomba a pie.
- Se actualizaron todas las instrucciones: ayuda de la pelea (con las teclas clave en rojo), ayuda del HUD,
  pausa, tutorial (los pasos usan marcadores `{Turbo}`, `{Fire}`, etc.) y el HUD de depuración.

### 2. Granadas (la moto) y bombas temporizadoras (a pie): dos items distintos
- **Granada** (recolectable): arma secundaria de la pistola, **se usa en la moto y también a pie**.
  - **El tiro es oblicuo y largo:** sube y baja en una parábola (0,85 s de vuelo, `granadaVuelo`) con un giro
    lento, sin rulos.
  - **En la moto:** se lanza **hacia atrás** y **explota a unos 10 m de la moto** sobre la horda, con el
    **alcance del tanque que explotaba** (`explosionRadius`, 8 m, todos los carriles). Los zombies que caen
    **no se reponen durante 5 s** (`hordaSinRegenerar`).
  - **A pie:** se lanza hacia donde mira y **cae a 5 m** (`granadaDistanciaAPie`); explota con **4,5 m de radio**
    (`granadaRadioAPie`, todos los carriles) y se lleva a los zombies cercanos al punto: es el arma táctica, a
    diferencia de la bomba que se los lleva a todos. No se gasta si no hay a quién llevarse.
  - Se llevan **hasta 3**, y es el mismo contador en los dos modos.
- **La primera pelea arranca con 1 granada y 1 bomba** (si no traía, se completan: `granadasPrimeraPelea` /
  `bombasPrimeraPelea`) para que se aprendan ahí: la
  ayuda de abajo resalta esas dos teclas en amarillo (el aviso "¡USÁ LA GRANADA…!" se quitó en la Fase 11: ahora lo enseñan las oleadas didácticas). A pie la granada es **C** (la Z es el disparo).
- **Bomba temporizadora** (recolectable): se usa **a pie**. Se pone donde está parada, tiene una **mecha
  de 2 s** (`bombaMecha`) y al explotar **elimina a todo zombie que esté peleando** (también los que saltan).
  **No frena la oleada**: los que todavía no entraron siguen entrando. Se llevan hasta 2.
- Los dos se recogen en el camino como la nafta, la batería y las balas (con el imán de carril al lado), y
  se esconden durante la pelea.
- **HUD (`HudArsenal`):** dos filas chicas de **íconos con casilleros** (granadas y bombas) a la **derecha del
  marco de las balas** (así no tapan el sensor de la horda ni el resto de la columna), **sin teclas** (cual es
  cual lo dice la ayuda de la pelea); el casillero salta al recoger y al usar. En la moto la fila de bombas queda apagada. Se arma solo y sigue al marco de las balas.
- Íconos provisorios (granada, bomba, bidón) hechos para esto en `Resources/Arsenal`; arte definitivo más adelante.

### 3. Barriles
- **En la moto ya no explotan** (no se les dispara): quedan de adorno en el recorrido.
- **A pie sí:** los que **quedaron en la arena** (donde se acabó la nafta) se pueden explotar con el
  disparo (Z) y se llevan a los zombies cercanos; la explosión encadena barriles cercanos. Solo vale
  la zona de la pelea (arena + 6 m); los de lejos siguen de adorno. Una cosa no altera la otra.

### 4. La ambulancia se va; la pelea se gana venciendo a todos
- **Sin timer, sin furgoneta, sin cartel de auxilio.**
- **Oleadas por cantidad:** se vence a todos los zombies de la oleada (entran de a varios, hasta los
  "simultáneos" de la oleada) y, tras **1,5 s de respiro**, entra la que sigue. **No hay reposición**: cada
  oleada trae su cantidad. **Ahora son 4:** 3 · 5 · 6 · **7** zombies (3, 4, 5 y 6 a la vez).
- **Al vencer la última, el último zombie suelta un bidón** (`DropKind.Bidon`, no desaparece): hay que ir a
  buscarlo y al agarrarlo **se llena el tanque de una**, sale el premio de la nota y se vuelve a la moto.
- La nota **B/A/S se queda** (por golpes recibidos). HUD: **"OLEADA 2/4 · QUEDAN 3"** con una barra de lo
  vencido de la oleada, y "¡AGARRÁ EL BIDÓN!".
- **Lo que no cambia:** el salto de los zombies en la primera oleada, el frenado y la transición a la
  pelea, el combo con remate, los drops y los avisos de "lleno".

## Qué hay que hacer para verlo (importante)

1. **Regenerar los niveles** (botón "Regenerar" de la paleta del nivel) en `Level_01_Auxilio_v2` y
   `Level_02`: así salen las **granadas** (cada ~650 m) y las **bombas** (cada ~1000 m). Sin eso los niveles
   no tienen esos items (en el tutorial sí: arranca con 2 granadas).
2. **Todos los niveles se pelean** (Fase 7): `Level_02` ya trae la pelea armada. Si se quiere abrirlo en
   Unity con la escena cargada, hay que recargarla (el archivo cambió en disco).
3. Los **íconos** y los sonidos son provisorios. **Sin clips todavía** para: lanzar y explotar la granada,
   mecha y explosión de la bomba, y agarrar el bidón.

## Valores para ajustar (`GameConfig`)

| Qué | Campo | Hoy |
|---|---|---|
| Granadas que se llevan / al empezar | `granadaMax` / `granadaInicial` | 3 / 0 |
| Distancia y vuelo de la granada | `granadaDistancia` / `granadaVuelo` | 10 m / 0,85 s |
| Con qué empieza la primera pelea | `granadasPrimeraPelea` / `bombasPrimeraPelea` | 1 / 1 |
| Horda sin reponer tras la granada | `hordaSinRegenerar` | 5 s |
| Bombas que se llevan / al empezar | `bombaMax` / `bombaInicial` | 2 / 0 |
| Mecha de la bomba | `bombaMecha` | 2 s |
| Oleadas | `oleadas` (zombies, simultáneos, tipos, velocidad) | 3 · 5 · 6 · 7 |
| Respiro entre oleadas / tope a la vez | `respiroOleada` / `maxSimultaneos` | 1,5 s / 6 |
| Con qué frecuencia salen en el nivel | `granadaEvery` / `bombaEvery` (generador) | 650 m / 1000 m |

## Fases

- **Fase 1 — Controles ✅.** Esquema nuevo con lector por modo y textos actualizados.
- **Fase 2 — Granada y bomba como recursos ✅.** Items, contadores, HUD, lanzamiento y explosión de la granada,
  la bomba a pie, telemetría (`granada_lanzada`, `granada_explota`, `bomba_puesta`, `bomba_explota`).
- **Fase 3 — Pelea por oleadas ✅.** Sin reloj ni furgoneta, 4 oleadas, bidón del último zombie, HUD de oleadas.
- **Fase 4 — Barriles en la arena ✅** (los que quedaron donde se acabó la nafta).
- **Fase 5 — Respuesta del combate** *(playtest de Jesús)*: cola de entrada para golpes, ritmo según qué tan
  rápido se aprieta, medir golpes perdidos, balazo determinista. **Pendiente.**
- **Fase 6 — Encuentros:** tope por carril y ajuste con datos de telemetría. **Pendiente.**
- **Fase 7 — Sin límite de auxilios ✅ (21/10).** Se sacó la lógica de auxilios: **cada vez que se acaba la nafta
  hay que resolver el beat'em up** para seguir, sin tope de peleas, en todos los niveles. Detalle abajo.

## Fase 7 — Cada nivel se resuelve peleando

**Qué cambió**
- **Fuera:** `GameConfig.ayudas`, `RunState.Ayudas`, el cartel "AUXILIOS: n" del HUD y el límite de pedidos
  (antes el nivel 1 tenía 3 y el nivel 2 ninguno: sin nafta era Game Over).
- **Regla nueva:** sin nafta (y en el piso, no en el aire) la moto frena y la protagonista se baja a pelear; al
  vencer la última oleada el último zombie suelta el bidón, se llena el tanque y se sigue. Pasa **cada vez** que
  se acaba la nafta, las que sean.
- **`GameConfig.peleaSinNafta`** (también en `LevelScene`, por nivel; `true` por defecto) enciende o apaga la
  pelea. Solo el **tutorial** y la plantilla `NivelBase` (que no tienen la pelea armada) la traen apagada: ahí
  quedarse sin nafta sigue siendo perder.
- **Se llevan la cuenta de peleas** (`RunState.Peleas`, antes `AyudasUsadas`): la primera de cada nivel trae un
  corazón de más, una granada y una bomba, y la ayuda de controles. El reintento no cuenta como otra pelea.
- **Renombres** (los valores de las escenas y los assets se migraron): `bidonesDesdeAuxilio` →
  `bidonesDesdePelea`, `gapTrasAuxilio` → `gapTrasPelea`, `RunEvent.AuxilioTermina` → `PeleaTermina`. En la
  telemetría: `sin_nafta` y `pelea_empieza` dicen `peleas=`/`pelea=` y el resumen `peleas=` (antes `auxilios=`).
  La carpeta y el espacio de nombres `Auxilio` (y `VistaDelAuxilio`) se dejaron como estaban para no romper
  referencias de las escenas.
- **El aviso "¡POCA NAFTA!"** ahora sale en todos los niveles con pelea (antes solo con auxilios disponibles).
- **Nivel 2** (`Level_02.unity`): se le agregó lo que le faltaba para pelear, copiado del nivel 1: `HudPelea` (en el
  Canvas), `HudMunicionPelea` (en el HUD, apuntando a su marco de las balas y a los marcadores que se esconden) y el
  objeto `Auxilio` con la `VistaDelAuxilio` (la protagonista a pie). Tiene **1 reintento por pelea**, como el nivel 1.
- **Tests:** uno nuevo que pelea cinco veces seguidas sin tope, los de "sin pelea = derrota" ahora usan
  `peleaSinNafta = false`, y `PeleaEnEscenasTests` (corre en Unity) avisa si un nivel con pelea prendida no
  trae el HUD, la protagonista a pie o el marco de las balas.

**Menos nafta en el nivel 2 (21/10):** se le quitó **1/4 de los bidones** para que haya más peleas: de 39 pasó a
29 (se sacaron 10 del suelo, los que más amontonaban; los 7 aéreos de las rampas se dejaron). El hueco medio entre
bidones quedó en ~133 m y el mayor en ~199 m. Se borraron de la escena y se subió `fuelEvery` del generador de 120 a 170,
así un "Regenerar" futuro deja una cantidad parecida. Con el tanque de 100 (460 m) y +45 por bidón (~207 m), recogiendo
todos alcanzaría de sobra para los 4000 m; las peleas salen por los que se pierden. Si hacen falta más, se
quitan más bidones o se achica `fuelMax`.

**Qué no cambió:** `bidonesDesdePelea = 1` en el nivel 1 (no hay nafta en el recorrido hasta la primera pelea),
`combustibleInicial` y `reintentosPelea` de cada nivel.

**Cosas para decidir** (no se tocaron)
1. **Dificultad entre peleas:** hoy todas son las mismas 4 oleadas (3 · 5 · 6 · 7). Con varias por nivel
   se puede repetir; una opción es que cada pelea suba un escalón (más zombies o más tipos pesados).
2. **Cuánto pesa la nafta:** como cada pelea llena el tanque, los bidones del recorrido valen menos. Se puede
   bajar la cantidad de bidones o achicar `fuelMax` para que las peleas lleguen más seguido.
3. **Nivel 2 más largo (4000 m):** puede tener muchas peleas; mirar la telemetría (`peleas=`) antes de balancear.
4. **El tutorial no enseña el beat'em up:** se aprende en la primera pelea del nivel 1 (ayuda, granada y bomba).
5. **Escenas viejas:** `Level_01` (el nivel anterior al beat'em up) quedó con la pelea apagada porque no tiene
   las vistas, y `Level_01_Auxilio` (el prototipo) la tiene prendida con el HUD y la protagonista a pie, pero sin el marco de
   las balas de la pelea (`HudMunicionPelea`). Ninguna está en la secuencia
   de niveles del juego (`Niveles.asset`).

## Preguntas abiertas

1. **Mando:** LB es la granada en los dos modos y Y la luz (moto) o la bomba (a pie). Si se prefiere otra
   distribución, son bindings del asset de acciones.
2. **Barriles en la moto:** quedan de adorno sin explotar (interpreté "una cosa no altera la otra"). Si se
   prefiere que además exploten en la moto, es volver a activar el disparo sobre ellos.
3. **Oleada 4:** puse 7 zombies con pesados y corredores mezclados (3 · 5 · 6 · 7); si se completa muy rápido
   con las bombas, se sube esa cantidad o se baja el stock.

## Fase 8 — Rampas, apagón y bomba (28/10)

Cuatro tareas del equipo, todas con tests en la simulación.

1. **Impulso de velocidad al tomar una rampa (sin gastar nafta).**
   - Al saltar, la moto gana `rampBoostMultiplier` (**1,6x**) durante `rampBoostDuration` (**2,2 s**): el vuelo sale
     más rápido y más largo (de ~5,6 m a ~10,7 m a velocidad normal) y, al aterrizar, sigue empujando lo que queda.
   - **Es gratis:** el consumo de nafta sigue el modo (normal, turbo), no la velocidad, así que el impulso no gasta de más.
   - No se suma si se toma otra rampa con el impulso puesto, no pasa de reversa y se corta al bajarse a pelear.
   - `rampLaunchSlope` del asset subió de 0,35 a **0,42** (la subida del salto) para que el vuelo dé para sobrevolar el coche.
   - **Aviso en el piso:** `AvisoDeRampa` (se crea sola al dar Play, como `VistaDeArsenal`) pinta dos tandas de flechas
     amarillas (">>>") en el carril de la rampa, a 15 y 8,5 m antes, que se encienden en cadena hacia ella. El dibujo
     es **provisorio** (`Resources/Rampa/AvisoRampa.png`); arte lo puede reemplazar sin tocar código.
2. **Apagón en la pelea.** Si se acaba la nafta en pleno apagón, la luz vuelve para pelear y **sigue prendida hasta que
   ese apagón termina**; el apagón siguiente se aplica normal (`RunState.ApagonAnulado`, `RunSimulation.Oscuridad`).
   La luz sube en ~0,5 s (`LightingDirector.velocidadDeApagon`).
3. **Bomba (C4).** Al explotar le **saca 1 de vida** a la protagonista si está a `bombaRadioPropio` (3 m) o menos y a
   `bombaCarrilesPropios` (1 carril) o menos; respeta el respiro tras un golpe y cuenta para la nota. **Bug del efecto:**
   la explosión se dibujaba en el carril de la protagonista; ahora sale **donde quedó la bomba** (el evento lleva el
   carril exacto: `HordeEvent.LaneY`).
4. **Coches y rampas.** El coche de cada pieza de rampa estaba a 4,4 m de ella y los dibujos se encimaban (la rampa mide
   casi 6 m y el coche 4,65). Ahora está a **6,1 m** (`LevelGenerator.RampObstacleAt`): fuera de la huella y todavía
   dentro del vuelo con el impulso. `LevelValidator.LandingFrom` pasó de 4,5 a 6,5. Se movieron los coches de las
   **18 piezas** de `Level_01_Auxilio_v2` (4) y `Level_02` (14); no queda ningún solape ni nada nuevo tapado.

**Si se cambia el salto o el impulso** hay que revisar juntos `RampObstacleAt`, `LandingFrom` y
`RampBoostTests.CocheDeLaPieza`: el test falla si el coche deja de poder sobrevolarse.

## Fase 9 — Mapa entre niveles y balance de las oleadas (28/10)

**Mapa.** Nueva pantalla `GameScreen.Map` (escena `Mapa.unity`, en Build Settings) entre elegir personaje y la cinematica, y
entre ganar un nivel y la cinematica del siguiente:
`Nuevo juego → Personaje → (tutorial, la primera vez) → MAPA → Cinematica → Juego → Nivel completo → MAPA → Cinematica…`
(después del último nivel va directo al final, sin mapa).
- Resalta el nivel que sigue (punto dorado que late, con su nombre) y marca con una **cruz verde** los ya jugados; el que se
  acaba de ganar se marca con un sello que cae. El nivel 1 está abajo a la izquierda.
- Leyenda parpadeante **"PRESIONÁ ENTER PARA CONTINUAR"** (en joystick, "A"); Esc o B vuelve al menú.
- La imagen es **provisoria** (`Resources/Mapa/MapaCiudad.jpg`) y los puntos de cada nivel se ajustan en el componente
  `MapScreen` (`nodos`, fracciones del ancho y el alto, en el orden de los niveles; hay 6 cargados sobre el camino).
  Todo se arma por código: para cambiar el mapa alcanza con reemplazar la imagen y acomodar los puntos.
- Flujo: `GameFlow.MapContinue()` (mapa → cinematica), `IsLevelCompleted(i)` (los niveles anteriores al que toca); la música
  sigue con la de elegir personaje.

**Balance de las oleadas.** Las dos primeras quedan como estaban. Las dos últimas aprietan:

| Oleada | Zombies (a la vez) | Tipos (común/corredor/pesado) | Velocidad | Ataque |
|---|---|---|---|---|
| 1 | 3 (3) | 100/0/0 | 0,85 | 1 |
| 2 | 5 (4) | 70/30/0 | 1 | 1 |
| 3 | **7** (5) | 30/35/35 | **1,1** | **1,6x** |
| 4 | **9** (6) | 20/40/40 | **1,2** | **2,2x** |

(Tabla de la Fase 9; la Fase 10 la volvió a subir. Los valores vigentes están en la Fase 10.)

`OleadaPelea.ataque` divide el tiempo que tarda un zombie en preparar el golpe (`zombiePreparacion`) y en repetirlo
(`zombieEnfriamiento`): con 2,2x tardan 0,23 s en preparar y 0,45 s en repetir (antes, 0,5 s y 1 s). La vista sigue el
lapso de la oleada en curso (`PeleaSimulation.PreparacionActual`) para que la animación de ataque no se corte.
Se ajusta en `GameConfig` → `oleadas`, sin tocar código.

**Drops del beat em up con el arte del recorrido.** Las balas y el bidón de nafta que quedan en el piso durante la pelea
usan los mismos dibujos que los del recorrido (`Art/Items/item_bullets.png` e `item_gas.png`), asignados en la
`VistaDelAuxilio` de `Level_01_Auxilio_v2` y `Level_02`. Solo el botiquín sigue con el dibujo de prueba (cruz verde): todavía
no hay arte de botiquín. Los dibujos de prueba y `Resources/Arsenal/Bidon` quedan de respaldo si el campo está vacío.

## Fase 10 — Sonidos de recogida, brillo de recursos y más dificultad (29/10)

**Sonidos de recoger granada y bomba.** `SonidoClave.PickupGranada` y `PickupBomba` (claves 30 y 31 de `SonidosGlobales`):
`SfxDirector` los dispara cuando sube `Granadas` o `Bombas` en un paso con `PickedUp`. Los clips son de prueba
(`sfx_pickup_grenade_01/02.wav`, un tintineo metálico, y `sfx_pickup_bomb_01/02.wav`, dos pitidos electrónicos), con sus assets
`Settings/Audio/Sonidos/PickupGranada` y `PickupBomba`; `PlaceholderAudioGenerator` también los genera. Para reemplazarlos
alcanza con cambiar los clips de esos dos assets.

**Brillo de lo que se agarra en la moto.** Nuevo `ResplandorDeRecursos` (se crea solo al dar Play, como `AvisoDeRampa`): una
luz puntual Multiply (como las de los prefabs; la aditiva lavaba el sprite) del color de cada recurso (nafta naranja, batería celeste, balas amarillas, granada verde, bomba roja)
sobre los 8 más cercanos por delante, que crece y se hace más fuerte con la oscuridad del tramo y late despacio. Antes dependían
de la luz de adorno de los prefabs, que compite por un presupuesto de 12 luces y en calidad Baja se apaga; la granada y la
bomba ni siquiera tenían luz. En calidad Baja quedan 3 luces. Se ajusta en el componente (radio, intensidad, distancia).

**Balance de las oleadas.** Atacar más rápido casi no cambiaba nada: el tope real de golpes era la invulnerabilidad de 1 s
tras recibir uno. Nueva palanca `OleadaPelea.invulnerable` (multiplica `invulnerableTrasGolpe`) y una tabla más dura:

| Oleada | Zombies (a la vez) | Tipos (común/corredor/pesado) | Velocidad | Ataque | Invulnerable |
|---|---|---|---|---|---|
| 1 | 3 (3) | 100/0/0 | 0,85 | 1 | 1 |
| 2 | 5 (4) | 70/30/0 | 1 | 1 | 1 |
| 3 | **9** (6) | 25/35/40 | **1,2** | **2x** | **0,8** |
| 4 | **12** (7) | 15/40/45 | **1,35** | **3x** | **0,6** |

**Nivel 2 más duro.** `Level_02` tenía los tramos vacíos (presión de horda 1 todo el recorrido). Ahora tiene 6 tramos con
`presionHorda` creciente: 1,1 / 1,15 / 1,2 / 1,25 / 1,3 / 1,3 (la horda corre un 10 a 30 % más rápido que en el nivel 1; el
faro la sigue frenando a la mitad). Densidades en 1: el recorrido ya colocado no cambia. Para endurecerlo o aflojarlo se edita
`presionHorda` de cada tramo en el `LevelScene`; `gapTrasPelea` (45 m) es global y también aprieta si se baja.

**Verificado:** 350 tests en verde fuera de Unity y el ensamblado del juego compila. **Sin probar en Unity:** cómo suenan los
clips nuevos, cuánto se ve el brillo en los apagones y si el nivel 2 quedó justo (tramos con presión > 1,3 son casi inalcanzables
sin turbo).

## Fase 11 - La pelea del primer nivel enseña el arsenal

**Pedido:** limpiar el HUD de la pelea y, solo en el primer beat'em up, forzar a usar la granada (oleada 3) y la C4 (oleada 4).

**HUD de la pelea.** Se quitó la barra de progreso de la oleada (no mostraba nada útil), el aviso "¡USÁ LA GRANADA (C) Y LA BOMBA
(Espacio)!" y la aclaración "(3 seguidos = remate)" de la ayuda de abajo. Los corazones y el combo subieron para ocupar el hueco.
El cartel "¡ÚLTIMA OLEADA!" ahora sale de `State.Oleadas` (con oleadas distintas por pelea, `Config.oleadas.Count` ya no sirve).

**Oleadas didácticas.** `GameConfig.peleaDidactica` (lo pone `LevelScene.peleaDidactica`, prendido solo en `Level_01_Auxilio_v2`) hace que
la **primera pelea del nivel** use `oleadasDidacticas` en vez de `oleadas`. Las peleas siguientes y el nivel 2 siguen con las comunes.
`OleadaPelea` tiene dos campos nuevos: `entrada` (Alternada / Izquierda / Derecha) y `ayuda` (Ninguna / Granada / Bomba).

| Oleada | Zombies (a la vez) | Tipos | Velocidad | Entrada | Ayuda |
|---|---|---|---|---|---|
| 1 | 3 (3) | comunes | 0,85 | alternada | - |
| 2 | 5 (4) | 70 % comunes, 30 % corredores | 1 | alternada | - |
| 3 | **20 (20)** | solo comunes | **0,55** (lentos) | **izquierda** | **granada** |
| 4 | **24 (24)** | solo comunes | **0,6** | **los dos lados** | **bomba** |

Entran todos juntos y hacen fila: pegarles de a uno es una tortura (la horda tiene 24 zombies, la cuarta usa casi todos). Una
granada tirada hacia la izquierda se lleva a los que quedan en su radio; la bomba se los lleva a todos.

**Ayuda.** `RunState.Ayuda` dice qué arma pide la oleada en curso mientras no se haya usado. `HudArsenal` hace parpadear esa fila (luz
verde para las granadas, roja para las bombas, detrás de los íconos, con los íconos latiendo). Al empezar la oleada se asegura
que haya al menos `cantidadAsegurada` de esa arma (2 granadas en la oleada 3, 1 bomba en la 4; si se gastaron en las anteriores). La pelea
didáctica además baja con **2 granadas** (`granadasPeleaDidactica`) en vez de 1, para que sea más fácil llevarse a toda la manada. Si se usa y cae casi nadie (menos de 4: se tiró antes de que
llegaran), **se devuelve** y vuelve a parpadear: es para aprender, no para fallar una vez. Al usarla bien deja de parpadear.

**Verificado:** 373 tests de la simulación en verde fuera de Unity (8 nuevos de las oleadas didácticas; la prueba de la cámara
que tiembla falla solo por el ruido Perlin falso del arnés) y el ensamblado del juego compila. **Sin probar en Unity:** cómo se
ve el parpadeo, si 20 y 24 zombies lentos alcanzan para "forzar" la granada y la bomba, y si la manada de la oleada 3 queda
a tiro de una sola granada. Hay que recargar `Level_01_Auxilio_v2` (se agregó `peleaDidactica: 1` a su `LevelScene`).

## Fase 12 - Los zombies parados del camino respiran

Los `ZombieFront` (los que esperan quietos en el carril, en todos los niveles de moto) se dibujaban con un solo cuadro de la
caminata. Ahora tienen una animación de quieto en loop: la hoja `Art/Zombies/ZombieCorredor_quieto.png` (5x5, 25 cuadros de 256 px,
pivote en los pies como la hoja de caminata).

- `ZombieLook` suma `idle` (los cuadros) e `idleFps` (12). `Settings/ZombieLooks.asset` los carga en los tres tipos (Común, Corredor, Pesado),
  así que vale para todas las escenas que usan ese set (Tutorial, Level_01, Level_01_Auxilio, Level_01_Auxilio_v2, Level_02).
- `ZombieQuieto` (nuevo, `Scripts/Gameplay/Enemies`) cambia el sprite del item con el reloj; cada zombie arranca en un cuadro distinto
  (`índice * 7`) para que una fila no respire toda junta. `LevelScene.Awake` lo agrega en Play; en el editor se ve el primer cuadro del quieto.
- Un look sin `idle` se comporta como antes (primer cuadro de `walk`). Es solo dibujo: la simulación no cambia.

**Sin probar en Unity:** compila (0 errores), pero falta mirarlo en Play: que los pies queden en el piso (pivote `0.1055`), que la
sombra proyectada siga bien el sprite que cambia y que el tamaño parezca el mismo que el de antes.
