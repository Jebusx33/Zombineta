# Horda de individuos: tipos, disparo, muertes y partículas — Diseño

Fecha: 11 de septiembre de 2026. Aprobado por José en la sesión del 11/09.
Backlog: cubre parte de T13 (trampas ambientales, vía barril explosivo) y suma detalle a T08.

## Qué es

Hoy la horda es un número (`HordeX`) y los doce zombies que se ven son decoración. Pasa a ser
**una masa de individuos simulados**: cada zombie tiene tipo, posición, carril y estado. El
frente de la horda —lo que te puede alcanzar— es simplemente el zombie vivo más adelantado.

Sobre eso se apoya todo lo demás: la bala traza una línea hacia atrás por tu carril y mata (o
no) a quien encuentre, hay barriles que explotan de un tiro, hay zombies que vienen de frente y
se arrollan, y cada muerte tira las partículas que le corresponden.

## Reglas

### La horda

- **24 zombies simulados** (`GameConfig.hordeCount`), repartidos en los tres carriles dentro de
  una franja de `hordeDepthMeters` (14 m) detrás del frente.
- `State.HordeX` deja de ser un valor que se mueve solo: cada tick se calcula como **la X del
  zombie vivo más adelantado**. La derrota sigue siendo `HordeX >= PlayerX`, así que **te puede
  alcanzar un solo corredor** aunque el grueso esté lejos.
- Cada zombie avanza a `hordeBaseSpeed × (multiplicador de su tipo) × factor`, donde el factor
  es el de siempre: goma elástica según la distancia del frente, y ×0,5 con el faro prendido.
- **La horda es infinita.** Un zombie que muere queda de cadáver `corpseSeconds` (1,2 s) y
  después **reaparece al fondo de la masa** con un tipo nuevo; lo mismo el que queda más atrás
  de la franja. La población es constante: matar no vacía la horda, **compra espacio**.
- **Semilla fija** (`GameConfig.hordeSeed`): los tipos que aparecen y la aleatoriedad de las
  muertes son reproducibles, así dos playtests son comparables.

### Tipos de zombie

Viven en `Settings/Zombies.asset` (`ZombieRoster`), una lista de `ZombieType`. Balancear es
editar ese asset.

| Campo | Común | Corredor | Pesado |
|---|---|---|---|
| `speedMultiplier` | 1,0 | 1,35 | 0,75 |
| `deathChance` (por bala) | 0,85 | 1,0 | 0,35 |
| `staggerOnHit` (si aguanta) | 0,6 s | — | 0,4 s |
| `ramStun` (al arrollarlo) | 0,1 s | 0,1 s | 0,8 s |
| `ramFuelPenalty` | 0 | 0 | 8 |
| `spawnWeight` | 60 | 25 | 15 |
| `tint` / `scale` | referencia / 1,0 | claro / 0,85 | oscuro / 1,25 |

"Recibe el impacto y sigue" es literal: **cada bala mata según `deathChance`**. Si no mata,
el zombie reproduce la animación `Hit` (ya existe en el prefab) y queda frenado
`staggerOnHit` segundos.

Si el roster está vacío o no está asignado, la simulación usa **un tipo implícito** (×1,0,
muere siempre): así los tests que no hablan de tipos siguen valiendo.

### El disparo

1. Sin munición: `ShotDenied`, como hoy.
2. Con munición: gasta una bala **siempre**, y traza una línea horizontal hacia atrás por el
   carril de la jugadora, hasta `shotRangeMeters` (60 m).
3. Pega en **lo primero que encuentre**: un zombie vivo de ese carril o un barril.
   - **Zombie:** muere según su `deathChance`, o encaja el impacto y se frena.
   - **Barril:** explota (ver abajo).
   - **Nada:** la bala se pierde (evento `ShotMissed`).
4. Cada muerte **asusta**: los zombies vivos a menos de `deathScareRadius` (4 m) se frenan
   `deathScareSeconds` (0,5 s).

### Explosiones

- El barril es una entrada del recorrido (`LevelEntryKind.Barrel`) y **queda en la calle hasta
  que explota**: se lo pasa de largo y se le dispara después, cuando la horda lo tiene encima.
- Al explotar mata a **todos los zombies vivos** a menos de `explosionRadius` (8 m), sin
  importar el carril, y asusta a los que estén dentro de `explosionScareRadius` (16 m).
- **Encadena:** la explosión detona otros barriles dentro de su radio, hasta 8 en cadena.
- Chocar un barril con la moto no hace nada: está al costado del carril.

### Zombie de frente

- Otra entrada del recorrido (`LevelEntryKind.ZombieFront`), con su **tipo** en el campo
  `variant`. Camina en el lugar, mirando hacia la jugadora.
- Se lo esquiva cambiando de carril, se lo sobrevuela con una rampa (igual que un obstáculo),
  o **se lo arrolla**: muere con causa `RunOver` y le cobra a la moto el `ramStun` y el
  `ramFuelPenalty` de su tipo. Al común te lo llevás puesto; el pesado es como chocar un auto.

### Tres formas de morir

`DeathCause`: `Bullet`, `Explosion`, `RunOver`. Cada una elige las partículas y el empuje del
cadáver. La animación `Death` del prefab es la misma para las tres.

## Presentación

- **`HordeView`** pasa a mapear 1 a 1 los zombies simulados: posición, carril, tinte y escala
  del tipo, y los triggers `Hit` y `Die` del Animator que ya existen.
- **Traza del disparo:** una línea que aparece `tracerSeconds` (0,05 s) desde la pistola hasta
  el punto de impacto (o hasta el final del alcance si se perdió).
- **Partículas** (pool, sin instanciar en pleno juego): impacto de bala, muerte, atropello
  (salpicadura grande hacia adelante) y explosión (fogonazo + humo).
- **Manchas en el asfalto:** cada muerte deja una mancha que queda en el lugar del mundo. Pool
  de 30, se reciclan las más viejas.
- **Reacción al faro:** los zombies iluminados (los que están dentro del cono, detrás de la
  moto) se aclaran y se inclinan hacia atrás, para que se vea por qué la horda frena.
- **Opciones suma "Sangre: Alta / Baja"** (`GameSettings.Gore`, en PlayerPrefs, como los
  toggles de cámara). En baja: las salpicaduras son polvo gris y no quedan manchas.

## Arquitectura

La regla de siempre: la simulación no sabe que existe Unity, y las vistas solo leen estado.

```
Scripts/Enemies/
  ZombieType.cs        ScriptableObject ZombieRoster + clase ZombieType. Los numeros.
  HordeSimulation.cs   La masa: unidades, avance, disparo, muertes, reciclado. C# plano.
  HordeView.cs         Dibuja una unidad por zombie y dispara sus animaciones.
Scripts/Fx/
  FxManager.cs         Escucha los eventos y tira particulas y trazas. Pool.
  BloodDecals.cs       Manchas en el asfalto. Pool.
```

- `HordeSimulation` expone `Units`, `FrontX`, `Step(dt, speedFactor)`, `Shoot(...)`,
  `Explode(x, radius)`, `RunOver(...)` y una **lista de eventos del tick**
  (`HordeEvent { Kind, FromX, X, Lane, Type, Cause }`, con `Kind` ∈ Tracer, Impact, Death,
  Explosion, Miss). Las vistas leen esa lista; nadie adivina nada mirando estado.
- `RunSimulation` la contiene: sincroniza `State.HordeX` con `FrontX`, le pasa el factor de
  velocidad (goma elástica y faro) y le deriva el disparo.
- Los barriles los conoce el recorrido, no la horda: `RunSimulation` pregunta por una interfaz
  chica (`IBarrelField`, con "¿hay un barril en este carril antes que el zombie?" y
  "detoná el barril N"), que implementa `LevelRuntime`. En los tests se reemplaza por un doble.
- `GameConfig` suma una sección "Horda" con los números de arriba y **pierde
  `shotHordePushback`**: el empujón ya no es un número, es consecuencia de a quién mataste.

## Tests (EditMode)

- **Horda:** el frente es el vivo más adelantado; cada tipo avanza a su velocidad; el faro
  frena a todos; el que se frena no avanza; la población se mantiene tras muchas muertes; el
  reciclado reaparece detrás del frente.
- **Disparo:** pega al más cercano del carril; ignora a los de otros carriles; sin nadie en el
  carril es `ShotMissed` y gasta bala igual; `deathChance` 1 mata siempre y 0 nunca; el que
  aguanta queda frenado; matar al puntero agranda la distancia; las muertes asustan a los
  vecinos.
- **Explosión:** mata en radio en los tres carriles, asusta más lejos, y encadena barriles.
- **Atropello:** cobra el `ramStun` y la nafta del tipo; en el aire no se arrolla (se
  sobrevuela); se consume la entrada.
- **Eventos:** cada acción emite el evento con posición y causa correctas.
- Se **reescribe** `Fire_PushesTheHordeBackAndSpendsAmmo` (ya no hay empujón fijo) y se agrega
  el caso de la bala perdida. El resto de la suite (101) queda igual.

## Fuera de alcance

- Zombies que esquiven obstáculos o se traben entre ellos.
- Que el zombie de frente camine de verdad hacia la jugadora: camina en el lugar (la entrada
  del recorrido está a una distancia fija).
- Arte final de zombies por tipo: por ahora el mismo sprite con tinte y escala.
- Que la explosión lastime a la jugadora.
