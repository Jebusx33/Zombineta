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
  `bombasPrimeraPelea`) para que se aprendan ahí: sale un aviso "¡USÁ LA GRANADA (C) Y LA BOMBA (ESPACIO)!" y la
  ayuda de abajo resalta esas dos teclas en amarillo. A pie la granada es **C** (la Z es el disparo).
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
2. **Nivel 2: sigue sin pelea** (`ayudas = 0`): quedarse sin nafta es Game Over. Es lo que vio Jesús; decidir
   si lleva pelea (Fase 6).
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
- **Fase 6 — Encuentros y nivel 2:** tope por carril, ajuste con datos de telemetría y la configuración de
  nafta/pelea del nivel 2. **Pendiente.**

## Preguntas abiertas

1. **Mando:** LB es la granada en los dos modos y Y la luz (moto) o la bomba (a pie). Si se prefiere otra
   distribución, son bindings del asset de acciones.
2. **Barriles en la moto:** quedan de adorno sin explotar (interpreté "una cosa no altera la otra"). Si se
   prefiere que además exploten en la moto, es volver a activar el disparo sobre ellos.
3. **Oleada 4:** puse 7 zombies con pesados y corredores mezclados (3 · 5 · 6 · 7); si se completa muy rápido
   con las bombas, se sube esa cantidad o se baja el stock.
