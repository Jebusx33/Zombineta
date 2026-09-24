# Tutorial de controles — Diseño

Sub-proyecto 8 del juego definitivo (`Juego/`).

## Objetivo

Un nivel corto que enseña los controles y la gestión de recursos antes de jugar. Se ejecuta solo
la primera vez que se juega, y se puede abrir cuando se quiera desde el menú principal. Se puede
saltear. No se puede perder.

Queda fuera de alcance: voces, animaciones de UI elaboradas, un tutorial por nivel, y cambiar
reglas de la simulación.

## Decisiones tomadas

- **Momento:** la primera vez va después de elegir personaje: Jugar → Personaje → **Tutorial** →
  Cinemática → Nivel 1. Las siguientes veces, Personaje → Cinemática, como hoy.
- **A pedido:** un botón "Tutorial" en el menú principal. Usa el último personaje elegido y al
  terminar vuelve al menú.
- **Ritmo:** hay que hacer cada cosa para avanzar. Cada paso muestra un cartel y espera la
  acción. La moto sigue avanzando todo el tiempo.
- **Saltear:** desde la pausa, con la opción "Saltear tutorial".
- **Horda:** aparece recién en el tramo final. Si alcanza a la moto no hay Game Over: la horda
  retrocede, sale un aviso y se repite el paso.
- **Recursos:** se muestran con las barras reales. Hay aviso con poco recurso, y en 0 se recargan
  solos para seguir aprendiendo.
- **Enfoque:** un nivel propio (`Tutorial.unity`) con su recorrido y su configuración. Un
  `TutorialDirector` encadena los pasos sin tocar las reglas del juego.

## 1. Flujo

El tutorial corre como una partida más (`GameScreen.Playing`), con la marca
`GameFlow.EnTutorial` en `true`. No se agrega una pantalla nueva: la moto, la cámara, la pausa,
la vibración, la música y el sonido ya tratan `Playing` como "la partida está corriendo", y así
siguen andando sin tocarlos. `GameFlow` es compartido, y todo lo que suma es aditivo:

- `bool TutorialPendiente { get; set; }`: lo pone `GameRoot` desde `GameSettings.TutorialVisto`.
- `bool EnTutorial { get; }`.
- `event Action TutorialTerminado`: `GameRoot` marca `TutorialVisto = true` al recibirlo.
- `ChooseCharacter(i)`: si `TutorialPendiente`, entra al tutorial (`EnTutorial = true`,
  `LevelIndex = 0`, `Attempt++`, pasa a `Playing`). Si no, va a `Cinematic` como hoy.
- `OpenTutorial()`: solo desde `MainMenu`. Entra al tutorial y recuerda que vino del menú.
- `TutorialFinished()`: solo con `EnTutorial`, desde `Playing` o `Paused`. Apaga `EnTutorial`,
  pone `TutorialPendiente = false`, dispara `TutorialTerminado`, y va a `MainMenu` si vino del
  menú; si no, a `Cinematic` del nivel 1.
- `SkipTutorial()`: solo desde `Paused` con `EnTutorial`. Equivale a `TutorialFinished()`.
- Durante el tutorial, `LevelWon()` y `LevelLost()` se ignoran (devuelven false).
- `Retry()` desde la pausa reinicia el tutorial: es el comportamiento que ya tiene.
- `ToMainMenu()` desde la pausa sale del tutorial sin marcarlo como visto.
- `JumpToTutorial()`: para dar Play directo en `Tutorial.unity` desde el editor.

`GameSettings.TutorialVisto`: bool, clave `zombineta.tutorialSeen`, default false.

`GameRoot`:
- Carga la escena `Tutorial` en vez de la del nivel cuando `EnTutorial`.
- `CurrentLevel` devuelve el `LevelInfo` del tutorial, un campo serializado propio de `GameRoot`
  con `sceneName = "Tutorial"` y el `audio` del nivel 1. No forma parte de `Niveles.asset`,
  porque no es un nivel de la campaña.
- Reconoce `Tutorial.unity` como escena de entrada.
- `SceneNames.Tutorial`.
- Build Settings la tiene después de `Cinematic`.

## 2. Escena y recorrido

`Scenes/Tutorial.unity` se arma sobre `Templates/NivelBase`, igual que los niveles. Lleva:

- **Su propia configuración**, `Settings/TutorialConfig.asset`: una copia de `GameConfig` con la
  horda más lenta y arrancando lejos (`startingGap` grande). La horda queda fuera de juego hasta
  el paso final, porque el director la mantiene lejos (ver 5).
- **Su recorrido**, `Settings/Niveles/Tutorial.asset`, de unos 600 a 700 m, armado con el editor
  de niveles. Tiene un tramo por paso, y cada tramo **repite su objeto** cada 30 a 40 m para que
  el paso nunca se trabe si el jugador se pasa uno:
  - rampas;
  - bidones;
  - baterías;
  - munición;
  - un tramo largo sin objetos para el faro;
  - zombies de adelante;
  - el refugio al final.
- **La música del nivel 1**, a través de su `AudioDeNivel`.

## 3. Pasos

`TutorialPasos` es un ScriptableObject en `Settings/TutorialPasos.asset`, con una lista ordenada
de `Paso`:

- `texto`: el cartel. Admite marcadores de acción, por ejemplo `{Turbo}` o `{Reverse}`, que se
  reemplazan por la tecla o el botón del dispositivo actual con el mismo sistema que
  `ControlHints`.
- `condicion`: un enum.
- `cantidad`: segundos o veces, según la condición.
- `resaltar`: opcional; qué barra del HUD señalar (Nafta, Batería, Munición, Amenaza).

**Pasos iniciales:**

| # | Cartel (resumen) | Condición |
|---|---|---|
| 1 | Mantené {Turbo} para acelerar | Turbo sostenido 1,5 s |
| 2 | Mantené {Reverse} para frenar y retroceder | Marcha atrás sostenida 1 s |
| 3 | {LaneUp} / {LaneDown} cambian de carril | Un cambio de carril hacia cada lado |
| 4 | Pasá por la rampa. En el aire, {Turbo} / {Reverse} inclinan | `Launched` y después `Landed` |
| 5 | Agarrá un bidón: es tu nafta | `PickedUp` de nafta |
| 6 | La batería alimenta el faro | `PickedUp` de batería |
| 7 | Balas para defenderte | `PickedUp` de munición |
| 8 | {Headlight} prende el faro: gasta batería | Faro prendido 2 s |
| 9 | {Fire} dispara a los zombies de adelante | `Shot` que golpea (`ShotMissed` no cuenta) |
| 10 | ¡La horda! Disparar la empuja y el faro la frena. Llegá al refugio | Llegar a la meta |

La lógica de avance es C# plano y está testeada: `TutorialProgreso`. Recibe los eventos y la
intención de cada paso y dice si el paso actual se cumplió.

## 4. Recursos

`TutorialRecursos` es C# plano y está testeado. Con el estado del paso decide los avisos y las
recargas:

- **Aviso de poco recurso:** nafta o batería por debajo del 20 %. Sale un aviso que apunta a la
  barra, por ejemplo "¡Poca nafta! Agarrá un bidón". Se muestra una vez por cruce del umbral.
- **Recarga en 0:** nafta o batería en 0 se recargan al 60 %, con el cartel "En el juego te
  quedarías sin nafta: te la recargamos para que sigas". También se recarga la munición en 0
  durante los pasos 9 y 10, para que siempre pueda disparar.
- **Quién aplica la recarga:** el `TutorialDirector`, escribiendo el `RunState`. Es lo mismo que
  ya hace "Probar desde acá" con la posición. Las reglas de consumo no cambian.

## 5. Horda y final

- **Pasos 1 a 9:** en cada cuadro, el director mantiene la horda a una distancia fija detrás de
  la moto (60 m) con `Sim.Horde.Reset(...)`. Así no amenaza y la barra de amenaza queda vacía.
- **Paso 10:** la suelta. Si la alcanza (`Phase == Lost` con `LossReason.CaughtByHorde`), el
  director:
  1. devuelve `Phase` a `Running`;
  2. aleja la horda 35 m;
  3. muestra "¡Te alcanzaron! Dispará o usá el faro";
  4. sigue en el paso 10.
- **`LevelFlowBridge` con `Flow.EnTutorial`:** nunca llama a `LevelLost`. Al ganar llama a
  `Flow.TutorialFinished()` en vez de `LevelWon`, así que no pasa por `LevelComplete`.
- **Victoria:** la moto llega al refugio con la animación de siempre. Después sale el cartel
  "¡Listo! Ya sabés jugar" durante 2 s, y el director pasa al flujo.

## 6. UI

- **Cartel del paso:** un panel arriba al centro con el texto del paso, con fundido entre pasos.
  Es legible sobre el escenario gracias a un fondo semitransparente y un contorno.
- **Resaltado de barra:** un marco que titila sobre la barra del HUD indicada.
- **Avisos:** en un panel más chico, abajo del cartel del paso. Duran 2,5 s.
- **Pausa:** la opción "Saltear tutorial" se muestra solo con `EnTutorial`. En el
  tutorial, "Reintentar" reinicia desde el paso 1.
- **Menú principal:** el botón "Tutorial" va entre Jugar y Opciones, con la navegación rehecha.

## 7. Pruebas

**EditMode:**

- Flujo:
  - la primera vez pasa por el tutorial y después no;
  - `OpenTutorial` desde el menú vuelve al menú;
  - `SkipTutorial`;
  - pausa y reanudar desde el tutorial;
  - `LevelWon` y `LevelLost` se ignoran en el tutorial;
  - `ToMainMenu` desde la pausa no marca el tutorial como visto.
- `TutorialProgreso`: cada condición, y que los eventos de otro paso no hagan avanzar.
- `TutorialRecursos`: el umbral, que avise una sola vez, la recarga en 0 y la munición en los
  pasos finales.
- La escena del tutorial tiene un `TutorialDirector` y nada que mueva su transform colgado de
  `Nivel`.
- Los 284 tests actuales siguen en verde.

**Play por la CLI, entrando por el menú con `TutorialVisto` en false:**

- Personaje lleva al tutorial.
- Cada paso se completa cumpliendo su condición. La condición se fuerza con el estado de la
  simulación y los métodos del juego, igual que en las verificaciones anteriores.
- La recarga de recursos en 0.
- La horda alcanza en el paso 10 sin Game Over.
- Al llegar al refugio se pasa a la cinemática del nivel 1.
- Un segundo Jugar ya no pasa por el tutorial.
- "Tutorial" desde el menú vuelve al menú.
- "Saltear" desde la pausa.
- Capturas de dos carteles y de un aviso.

**A mano:** que José lo juegue una vez con teclado y otra con joystick.

## Riesgos

- **El tutorial no puede trabarse:** si un objeto necesario no aparece, el paso nunca avanza. La
  repetición de objetos en el recorrido lo evita. Un test de la escena verifica que cada tramo
  tenga al menos tres objetos de su tipo.
- **Largo:** tiene que ser corto. La meta es de 2 a 3 minutos para quien lo juega bien. El
  recorrido se ajusta a eso en la verificación.
