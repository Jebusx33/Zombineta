# Auxilio y pelea a pie — plan del prototipo

**Objetivo:** probar en una copia del nivel 1 un core loop nuevo: quedarse sin nafta con
ayudas disponibles no es derrota; la protagonista se baja y pelea (beat'em up) hasta que
llega una furgoneta de auxilio que arrolla a los zombies y le llena el tanque.

**Motivo:** el feedback de playtest fue "aburrido": el nivel no tiene momentos distintos
ni picos de emocion. Este loop agrega un cambio de ritmo y un premio memorable.

## Reglas acordadas

| Regla | Valor inicial (GameConfig, seccion Auxilio) |
|---|---|
| Ayudas por nivel | 2 en `Level_01_Auxilio`; 0 en el resto (todo igual que hoy) |
| Sin nafta y con ayudas | se gasta una y arranca la pelea |
| Sin nafta y sin ayudas | derrota, como hoy |
| Vida en la pelea | 5 golpes |
| Duracion | 25 s fijos; despues llega la furgoneta |
| Al terminar | tanque lleno, horda a 45 m, vuelve el runner |
| Golpe | alcance 1,6 m hacia donde mira, empuja 2,5 m y frena 0,5 s; comun muere con 3 |
| Disparo | el de siempre, hacia donde mira, gasta balas |
| Zombies | hasta 8 a la vez, de los dos lados; se preparan 0,5 s antes de pegar |

## Arquitectura

- **Simulacion (C# plano, testeable):** `PeleaSimulation` en `Simulacion/Runtime/Auxilio`.
  Mueve a los mismos `ZombieUnit` de la horda (asi la vista de la horda los dibuja sin
  cambios), la posicion a pie de la protagonista, la vida y la furgoneta.
- **RunSimulation:** fase nueva `RunPhase.Pelea`. Al quedarse sin nafta, si hay ayudas,
  entra a la pelea en vez de esperar a la horda. En esa fase el Tick delega en la pelea.
- **Intent:** `PlayerIntent` suma `MoveX` (izquierda/derecha a pie) y `Golpe` (accion
  nueva "Golpe" en el mapa Moto: Z, clic derecho, boton Sur del joystick).
- **Vistas:** protagonista a pie (placeholder), furgoneta (placeholder), HUD de pelea
  (vida, cuenta del auxilio, ayudas), carteles, y el "feel": cámara lenta al quedarse sin
  nafta y en el impacto de la furgoneta, congelado de cuadros en cada golpe, sacudida.
- **Camara:** fija en la arena durante la pelea.
- **Nivel de prueba:** `Level_01_Auxilio`, copia del nivel 1 con `ayudas = 2` en LevelScene.
  F4 vacia el tanque para probar la pelea sin esperar.

## Tareas

1. Simulacion de la pelea + tests (entrada, golpe, disparo, ataque de zombies, vida,
   furgoneta, salida al runner, sin ayudas = derrota).
2. Input (accion Golpe, MoveX) y RunController (paso de la pelea, F4).
3. Vistas: protagonista a pie, furgoneta, zombies mirando hacia la protagonista.
4. HUD de pelea, carteles y feel (hitstop, camara lenta, sacudida, musica de tension).
5. Camara fija en la arena.
6. Escena `Level_01_Auxilio` y prueba en Play.

## Fuera del prototipo

Combos, agarres, armas en el piso, animaciones definitivas, pedir el auxilio a mano.
