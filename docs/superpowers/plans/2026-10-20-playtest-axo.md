# Playtest de Axo (20/10) — plan de acción

Fuente: `Juego/Assets/_Zombineta/Notas internas Testing (Axo).pdf` (cuestionario, planilla de
observación, notas con tiempo del video y notas del desarrollador). Puntaje: **divertido 4,8**
(antes 3), **entender qué hacer 1,5**. Le gustó el beat'em up ("me quedé con ganas de hacer
combos"); se cansó por no poder agarrar los recolectables.

## Qué dijo y qué haremos

| # | Hallazgo | Sev. | Qué haremos | Fase |
|---|----------|------|-------------|------|
| 1 | No logra agarrar nafta / batería / munición: "los atravesaba o siempre los esquivaba". Le impidió avanzar. | **Alta** | Diagnóstico + recolectable que "vuela" hacia la moto en el mismo carril o el adyacente (ver abajo) | B |
| 2 | Al disparar, el carril debería quedar libre pero la horda reaparece al instante | **Alta** | Con 2 disparos la horda retrocede unos metros ("espacio seguro"); revisar si pasa igual con las luces | C |
| 3 | Acelerar en la rampa del tutorial: choca con el suelo al caer y pierde la aceleración | Baja | Sin castigo por acelerar en la rampa; sin maniobrar | A |
| 4 | No sabía encender las luces (batería "al palo") | Media | Cartel "¡PRESIONÁ [tecla de luz]!" con la misma animación de "¡POCA NAFTA!" | A |
| 5 | Beat'em up: no sabe si pega; la colisión del golpe se activa más adelante de lo que muestra el dibujo | Media | Alinear la colisión con el dibujo y subir un poco la velocidad de ataque. Combos nuevos: al final | D |
| 6 | Disonancia ludonarrativa: los zombies corren a la velocidad de la moto y de golpe caminan | Media | **Salto**: se te tiran encima; golpe en el aire = muerte; esquivar = caen, cooldown y se arrastran | E |
| 7 | Al pasar al beat'em up quedan en pantalla nafta, baterías, balas y los carteles de aviso | Media | Quitarlos al entrar a la pelea (y que vuelvan al salir) | A |
| 8 | En el beat'em up, con vida o balas llenas pisa un drop y no pasa nada ni se entera | Media | Mensaje "VIDA LLENA" / "MUNICIÓN LLENA" | A |
| 9 | "Sacaba rango S y no me dejaba ningún objeto" | Media | Revisar el premio (ver abajo) | A |
| 10 | Dos bocacalles juntas (min 5:34): debería haber al menos un edificio entre medio | Baja | Revisar el generador / la escena | F |
| 11 | "A la pista le falta un alguito, tal vez mejorando las luces del entorno" | Baja | Idea, a evaluar al final | F |

**Fuera de alcance (lo pidió la nota):** el asset de la bocacalle que aplasta la línea amarilla
y los +200 px de árboles y edificios. Tampoco hace falta "balancear" por separado: lo que vio en
4:40 es un zombie que parecía más rápido pero iba igual; lo cubre la fase E.

## Respuestas del equipo (20/10)

1. **Luces:** el cartel sale con las luces apagadas + batería > 0 + tramo oscuro.
2. **Premio con todo lleno:** por ahora solo avisar "MUNICIÓN LLENA" / "BATERÍA LLENA".
3. **Salto:** solo en la **primera oleada** de la pelea (es la transición de correr a pelear a pie,
   para que no haya ruptura visual). Si cae sobre ella: **1 de daño**; al zombie lo daña 1 un
   balazo en el aire.
4. **Retroceso de la horda:** 12 m durante 3 s, enfriamiento 8 s.

## Fase A — Arreglos chicos ✅ hecha (falta verla en Play)

- **Rampa del tutorial:** `TutorialConfig.launchSpinPerExcessSpeed` pasó de 1 a 0. Sin giro, la
  moto sale y cae con el ángulo de la rampa (20°, dentro del seguro de 25°), así que acelerar y
  saltar ya no la deja de espaldas ni la castiga. El juego normal no cambia. Test nuevo en
  `JumpTests`.
- **Aviso de luces:** en `AlertaDeApagon` (que ya está en el HUD de todos los niveles y del
  tutorial, sin tocar escenas): "¡PRESIONÁ [tecla] PARA LAS LUCES!" abajo al centro, con la
  tecla o el botón del dispositivo en uso y el mismo parpadeo de "¡POCA NAFTA!". Sale con las
  luces apagadas, batería y un tramo con oscuridad desde 0,35 (`oscuridadParaAvisar`, ajustable).
- **Pelea limpia:** al empezar la pelea `LevelScene` esconde nafta, baterías y balas del
  recorrido y `LaneWarningView` esconde los carteles de aviso de carril. Vuelven al retomar.
- **Drop con todo lleno:** evento nuevo `DropLleno`: al pisar un botiquín con la vida llena o balas
  con el cargador lleno sale "VIDA LLENA" / "MUNICIÓN LLENA" una sola vez (se repite si se
  aparta y vuelve). El drop se queda en el piso. Queda en la telemetría como `drop_lleno`.
- **Premio con todo lleno:** si el premio traía balas o batería y ya estaban llenas, el cartel de
  la nota dice "MUNICIÓN LLENA" / "BATERÍA LLENA" en vez de callar.

- **Ayuda de controles de la pelea (bug aparte, pedido el 20/10):** la letra se achicaba sola a
  los 7 s (de 40 a 24 px y casi transparente). Ahora el tamaño es fijo (`tamanoAyuda` = 38), las
  teclas de **golpe** y **disparo** van en rojo y negrita (`colorTeclasClave`), y el cartel sale
  **solo en la primera pelea** (todo el tiempo que dura, reintento incluido). En las siguientes ya
  no aparece.

- **Caída al seguir acelerando en el aire (bug aparte, 20/10):** en el aire el turbo inclina la
  moto hacia adelante a 60°/s sin tope práctico (`maxPitch` era 80°), así que quien mantenía el
  turbo después de la rampa terminaba clavando la trompa y caía, cortando el ritmo. Ahora
  `maxPitch` es **22°**, por debajo del ángulo seguro (25°): mantener turbo o retroceso en el
  aire nunca la tira. Enderezarla hasta ±8° sigue dando el aterrizaje perfecto con impulso (ese
  es el premio a la habilidad). Vale para el juego y para el tutorial; si se quiere volver a que
  se pueda caer, se sube `maxPitch` en `GameConfig`.

Verificado fuera de Unity: 251 tests en verde (5 nuevos de pelea, 3 de salto) y el juego compila.

1. **Rampa del tutorial.** Al caer torcido hoy se cae (`Land()` con ángulo mayor al seguro);
   acelerar en la rampa gira mucho la moto (`launchSpinPerExcessSpeed` = 1 por m/s de más). En
   `TutorialConfig` el giro pasa a 0 (o el aterrizaje del tutorial siempre es seguro), sin tocar
   el juego normal. Test: acelerar y saltar en el tutorial no cae.
2. **Aviso de luces.** Si las luces están apagadas, hay batería y el tramo es oscuro: texto
   parpadeante "¡PRESIONÁ [tecla]!" con la tecla del dispositivo en uso (`ControlHints`) y el mismo
   parpadeo de "¡POCA NAFTA!". Se apaga solo al prenderlas. **Pregunta abajo: cuándo molesta.**
3. **Pelea limpia.** Al empezar la pelea se esconden los recolectables y los carteles de aviso
   de carril; vuelven al retomar. Hoy se quitan solo obstáculos y zombies de frente de ±18 m, y
   los recursos se quedaban (así se pensó en la fase 4).
4. **Drop con todo lleno.** Cuando pisa un botiquín con la vida llena o balas con el cargador
   lleno, flota "VIDA LLENA" / "MUNICIÓN LLENA" (el drop se queda hasta que se vaya).
5. **Premio que no llega.** Con S da +3 balas y +40 de batería, pero si ya las tenía llenas no
   queda nada. Ver pregunta; mínimo: el cartel dice "MUNICIÓN LLENA" en vez de callar.

## Fase B — Recolectables (la más importante)

**Qué encontré.** En la simulación la zona de recolección es *generosa*: la moto ocupa ±1,4 m
y el recurso ±0,5 m, o sea que lo agarra si pasa a 3,3 m, y vale carril con tolerancia 0,5.
Entonces el problema parece de **dibujo contra colisión** (el sprite se ve en un lugar y se
recoge en otro: carril visual en medio de un cambio, escala de la cámara, pivote del sprite) o
de condiciones (volando no agarra lo del piso; en el tutorial los items se mueven de lugar).
No se puede saber sin verlo, así que:

1. **Diagnóstico con la telemetría:** nueva fila `item_pasado` cuando la moto pasa un recurso
   a menos de 1,5 carriles sin recogerlo (carril de la moto, carril del item, altura, si estaba
   en el aire). Con eso la próxima prueba dice si falla por carril, altura o dibujo.
2. **Imán** (la sugerencia del desarrollador): recurso en el **mismo carril o el adyacente** se
   recoge (hoy solo el mismo); a 2 carriles, no. En la vista, el recurso "flota" (sube y baja
   suave) y, al estar cerca, **se acerca a la moto** hasta recogerse solo. La regla vive en la
   simulación (se prueba con tests) y el movimiento es solo dibujo.
3. **Revisar el dibujo:** alinear el sprite con su posición en la simulación (pivote y escala).
4. Mensaje corto al recoger, si hace falta más feedback.

Factibilidad: alta. El riesgo es el balance: con tolerancia de un carril los recursos son más
fáciles de agarrar y los obstáculos del mismo carril no cambian. Se mide con la telemetría.

## Fase C — Disparar despeja la horda

Hoy `ApplyFire` mata al zombie más cercano del carril y la horda sigue empujando desde atrás,
así que el carril se rellena al instante. Propuesta: **dos disparos que dan en el blanco en
poco tiempo** (ventana de ~3 s) hacen **retroceder la horda unos metros** durante unos
segundos (parámetros: metros, segundos y enfriamiento en `GameConfig`), con aviso visual.
Revisar el comportamiento de las **luces**, que también asustan a la horda (`Scare`), para que
el espacio seguro funcione igual. Tests de simulación: tras 2 aciertos el frente de la horda
queda más lejos y vuelve a avanzar pasado el tiempo.

## Fase D — Golpes del beat'em up

- Alinear el momento en que el golpe conecta con el dibujo (hoy conecta al apretar, y el barrido
  llega a su punto más lejano a la mitad de `golpeDuracion`): el impacto pasa al pico del
  barrido, o el alcance se achica a lo visible. Se prueba con tests de tiempo.
- Atacar un poco más rápido: bajar `golpeEnfriamiento` (0,35 en el asset) y/o `golpeDuracion`.
- Combos extra: **al final**, como pidió.

## Fase E — Zombies que saltan (cuando llegue la animación)

La idea de Axo: en vez de caminar, el zombie **se te tira encima de forma exagerada**; se
puede **golpear en el aire** (muerte de un golpe: riesgoso pero eficaz) o **esquivar** (cae, queda
un rato en el piso y después se levanta o se arrastra: seguro pero más lento). Si la agarra al
caer, **daño**. Resuelve que los zombies corran y de golpe caminen.

Diseño propuesto (a confirmar, ver preguntas):
- Estado nuevo del zombie `Saltando`: sale desde cierta distancia, vuela con arco y cae en
  donde estaba ella al despegar (se puede esquivar cambiando de carril o yendo hacia atrás).
- En el aire: el golpe lo mata; en el punto de caída hay un aviso (anillo, como el del ataque).
- Al caer: si ella sigue en la zona, daño (con la invulnerabilidad de siempre); si no, el zombie
  queda derribado el tiempo de cooldown y después camina lento / se arrastra.
- Cantidad limitada (cuenta dentro de `maxAtacantes`) y solo algunos tipos/oleadas.
- Hace falta la animación (la tiene el usuario) para el salto en el aire y la caída.

## Fase F — Revisiones menores

- **Bocacalles juntas:** comprobar si las genera el generador de escenario o la escena; si es
  el generador, exigir al menos un edificio entre dos bocacalles (con test).
- **Luces del entorno de la pista:** idea para evaluar con el arte; no bloquea nada.

## Orden y estimación

A (medio día) → B (1 día, con diagnóstico) → C → D → E (cuando haya animación) → F.
Cada fase se prueba fuera de Unity (tests de simulación + compilación) y se anota en este
archivo; después hace falta mirarla en Play. La telemetría de cada partida nos da los números.

## Preguntas para el equipo

1. **Luces:** ¿el cartel sale siempre que estén apagadas, o solo en tramos oscuros / con la horda
   cerca? (Propongo: apagadas + batería > 0 + el nivel es de noche.)
2. **Premio con todo lleno:** ¿lo convertimos en otra cosa (p. ej. nafta o un poco de vida del
   próximo tramo), o alcanza con avisar "MUNICIÓN LLENA"?
3. **Salto:** ¿saltan todos los tipos o solo comunes? ¿Después del cooldown se levantan y caminan
   lento o se arrastran? ¿Cuánto daño hace si cae encima (1 corazón)?
4. **Retroceso de la horda:** ¿cuántos metros y cuánto tiempo? (Propongo 12 m durante 3 s y
   enfriamiento de 8 s.)
