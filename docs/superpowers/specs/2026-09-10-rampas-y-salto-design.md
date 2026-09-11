# Rampas y salto regulable — Diseño

Fecha: 10 de septiembre de 2026. Backlog: T12 ("Implementar rampas y salto de la motoneta").
Aprobado por José en la sesión del 10/09.

## Qué es

Rampas en el recorrido que lanzan a la moto. En el aire, la jugadora **inclina** la moto como
en Excitebike: eso regula el largo del salto y decide cómo aterriza. Aterrizar nivelada da
impulso; aterrizar torcida la tira al piso y la horda recorta distancia. Saltar en turbo rinde
más (vuela más lejos, llega a premios altos) pero exige estabilizar.

No hay botón de salto: solo se salta desde rampas, y saltar es opcional (se esquiva la rampa
cambiando de carril).

## Reglas

### Rampa
- Nueva entrada del recorrido: `LevelEntryKind.Ramp`, en un carril, como un obstáculo.
- Pasarla **hacia adelante, en el piso y en su carril** lanza a la moto. De reversa o
  aturdida (velocidad 0) no pasa nada.
- No se consume: si se vuelve en retroceso y se la pasa otra vez hacia adelante, lanza de nuevo.

### Lanzamiento
- Velocidad horizontal en el aire = velocidad al pisar la rampa (incluye turbo e impulso). Queda
  fija hasta aterrizar.
- Velocidad vertical de salida = `rampLaunchSlope × velocidad horizontal`.
- Inclinación de salida = `launchPitch` (nariz arriba).
- Giro de salida = `launchSpinPerExcessSpeed × max(0, velocidad − normalSpeed)`, hacia atrás.
  A velocidad normal no hay giro; en turbo, sí.

### En el aire
- Gravedad efectiva = `jumpGravity × (1 − leanLift × sen(inclinación))`. Nariz arriba planea
  (salto más largo), nariz abajo cae antes. Así se regula el largo.
- Inclinación: `dθ/dt = giro de salida + input × leanRate`. En el aire, el modo de manejo se
  lee como inclinación: **Retroceso (A/←) = nariz arriba, Turbo (D/→) = nariz abajo**. No
  cambia `PlayerIntent` ni las teclas.
- Inclinación limitada a ±`maxPitch`.
- No consume nafta, no cambia de carril, no choca obstáculos, no agarra lo que está en el piso.
  Faro y pistola funcionan normal. La horda sigue igual.

### Aterrizaje (cuando la altura vuelve a 0 bajando)

| \|inclinación\| | Resultado | Evento |
|---|---|---|
| ≤ `perfectLandingAngle` | Impulso: velocidad × `landingBoostMultiplier` por `landingBoostDuration`, sin consumo extra | `Landed \| LandedPerfect` |
| ≤ `safeLandingAngle` | Nada | `Landed` |
| mayor | Caída: aturdida `fallStunDuration`, −`fallFuelPenalty` de nafta, `Fallen = true` hasta levantarse | `Fell` |

### Pickups en el aire
- `LevelEntry` suma `height` (metros; 0 = piso, el default de todas las entradas existentes).
- Una entrada con `height > 0` solo se agarra **en el aire**, en su carril, con
  `|altura − height| ≤ aerialPickupTolerance`. Una con `height = 0` solo en el piso.
- Se ubican a la altura del pico de un salto en turbo: premian el salto riesgoso.

## Valores iniciales (GameConfig, sección "Salto")

| Campo | Valor | Por qué |
|---|---|---|
| `rampLaunchSlope` | 0,75 | Salto normal ≈ 1 s y 12 m, pico 2 m; turbo ≈ 2 s y 40 m, pico ≈ 6,5 m |
| `jumpGravity` | 20 m/s² | Arco de juego, no realista |
| `leanLift` | 0,35 | Inclinar a 30° cambia la gravedad ±17% |
| `launchPitch` | 20° | Un salto normal sin tocar nada aterriza a 20°: sano pero no perfecto |
| `launchSpinPerExcessSpeed` | 2,6 °/s por m/s | En turbo (+9,6 m/s) gira 25 °/s: sin corregir aterriza a más de 60° y se cae |
| `leanRate` | 60 °/s | Mantener D todo un salto en turbo pasa de largo (−35°) y también se cae |
| `maxPitch` | 80° | |
| `perfectLandingAngle` | 8° | |
| `safeLandingAngle` | 25° | |
| `landingBoostMultiplier` | 1,25 | |
| `landingBoostDuration` | 1,5 s | |
| `fallStunDuration` | 1,6 s | El doble del choque; la horda (17,5 m/s) recorta ≈ 28 m |
| `fallFuelPenalty` | 10 | |
| `aerialPickupTolerance` | 1,5 m | |
| `jumpHeightToWorld` | 0,5 u/m | Altura exagerada respecto del eje X (0,25 u/m) para que el salto se lea |

## Arquitectura

Todo el salto vive en la simulación (C# plano), siguiendo la regla del proyecto.

- **`RunState`**: `Airborne`, `Height`, `VerticalSpeed`, `AirSpeed`, `Pitch`, `PitchRate`,
  `BoostRemaining`, `Fallen`.
- **`RunSimulation`**:
  - `Launch()` (lo llama `LevelRuntime`); en `Tick`, rama aire/piso.
  - Aterrizaje, impulso y caída.
  - Eventos nuevos: `Launched`, `Landed`, `LandedPerfect`, `Fell`.
  - Cambio de carril bloqueado en el aire.
- **`LevelRuntime.Collect`**: decide por entrada según el estado **del momento** (una rampa
  lanza y los obstáculos siguientes del mismo tramo ya se sobrevuelan); rampas no se
  consumen; filtra piso/aire por `height`.
- **`RunController`**: `HeightToWorld(m)`.
- **Vistas**:
  - `ScooterView`: sube la moto y la rota según `Pitch`; tirada cuando `Fallen`; sombra en el
    carril mientras vuela, verde cuando la inclinación daría aterrizaje perfecto.
  - `LevelSpawner`: rampa como cuña placeholder (pivot abajo a la derecha: el borde alto es el
    punto de lanzamiento); pickups aéreos dibujados a su altura.
- **Cámara**:
  - `CameraInput.JumpHeight`; el director agranda el plano lo necesario para que el salto no
    salga por arriba (`CameraConfig.jumpHeadroom`).
  - `Fell` usa los efectos del choque y `LandedPerfect` da un golpe corto.
  - En el aire el "turbo" de cámara no cuenta.

## Contenido

`Ruta01` y `Ruta02` suman piezas de rampa cada ~250 m:
- La rampa, en un carril al azar con semilla fija.
- Obstáculos después, en ese carril, dentro del largo de un salto normal.
- A veces un pickup aéreo a la altura del pico en turbo.

Se sigue cumpliendo que **nunca estén bloqueados los tres carriles a la vez**: donde la pieza
agrega un obstáculo, al menos uno de los otros dos carriles queda libre. Se verifica con la
simulación real después de insertar.

## Tests (EditMode)

- Rampa: lanza solo hacia adelante, en el piso y en su carril; de reversa no; no se consume.
- El largo del salto crece con la velocidad.
- Inclinar atrás estira el salto y adelante lo acorta (misma velocidad).
- En el aire: sin consumo, sin cambio de carril, sin choques, sin pickups de piso.
- Pickup aéreo: se agarra volando a su altura, no desde el piso ni pasando muy abajo.
- Aterrizaje perfecto da impulso; normal no; torcido tira al piso con aturdimiento y nafta.
- Salto normal sin input aterriza sano; salto en turbo sin corregir se cae; turbo con D todo el
  salto también se cae; turbo corrigiendo a tiempo aterriza perfecto.
- Independencia del framerate (30 vs 120 fps: mismo resultado de aterrizaje, largo parecido).
- Cámara: con la moto en el aire el plano contiene el techo del salto.

## Fuera de alcance

- Botón de salto libre, cambio de carril en el aire, obstáculos aéreos.
- `Tools/BalanceProbe` no simula saltos (se anota en el handoff).
- Arte final de rampa y animación de caída.
