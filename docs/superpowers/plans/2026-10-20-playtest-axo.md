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

## Fase B — Recolectables (la más importante) ✅ hecha (falta verla en Play)

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

**Lo hecho**
- **Se agarran desde el carril de al lado** (`GameConfig.imanCarriles` = 1; 0 vuelve a "solo en su
  carril"). A dos carriles no se agarran. Vale solo para nafta, batería y munición; los obstáculos
  siguen chocando solo en su carril. La altura sigue igual: lo del piso no se agarra en el aire y lo
  aéreo solo a su altura.
- **Flotan y se acercan** (solo dibujo, en `LevelScene`): suben y bajan despacio
  (`flotarAltura`, `flotarVelocidad`, `flotarElevacion`) y, si la moto los puede agarrar y están a
  menos de `imanMetros` (7 m), se van hacia ella y llegan justo cuando los agarra. Si cambia de
  carril y ya no los alcanza, vuelven a su lugar sin saltos. Se apaga con
  `acercarRecolectables` / `flotarRecolectables`.
- **Telemetría `item_pasado`:** una fila por cada recolectable que la moto pisó sin agarrar, con
  tipo, metros, carril del ítem, `dif_carril` (lo cerca que pasó, en carriles), altura del ítem y si
  iba en el aire; y `items_pasados` en el resumen. Con eso la próxima prueba dice por qué falló
  (carril, altura o dibujo) en vez de adivinar.
- **No se encontró un error de colisión** en la simulación (la zona ya era de ±3,3 m); queda por
  confirmar con la telemetría si el problema era el carril o que el dibujo parecía estar en otro
  lado. El carril de al lado y el acercamiento cubren ambos casos.

Verificado fuera de Unity: 256 tests en verde (5 nuevos: carril de al lado, a dos carriles, solo
recolectables, telemetría de lo pasado y de lo agarrado) y el juego compila.

## Fase C — Disparar despeja la horda ✅ hecha (falta verla en Play)

Hoy `ApplyFire` mata al zombie más cercano del carril y la horda sigue empujando desde atrás,
así que el carril se rellena al instante. Propuesta: **dos disparos que dan en el blanco en
poco tiempo** (ventana de ~3 s) hacen **retroceder la horda unos metros** durante unos
segundos (parámetros: metros, segundos y enfriamiento en `GameConfig`), con aviso visual.
Revisar el comportamiento de las **luces**, que también asustan a la horda (`Scare`), para que
el espacio seguro funcione igual. Tests de simulación: tras 2 aciertos el frente de la horda
queda más lejos y vuelve a avanzar pasado el tiempo.

**Lo hecho**
- **Dos aciertos seguidos** (`retrocesoAciertos` = 2) en una ventana de 3 s (`retrocesoVentana`)
  hacen que la horda **ceda 12 m en 3 s** (`retrocesoMetros`, `retrocesoSegundos`): cada zombie
  vivo avanza menos de lo normal, a razón de 4 m/s, y la distancia ganada se queda (no vuelve a
  achicarse sola). **Enfriamiento de 8 s** (`retrocesoEnfriamiento`) hasta el siguiente. Solo
  cuentan los tiros que dan en un zombie (no los que fallan ni los de barril).
- Cartel **"¡LOS ALEJASTE!"** al activarse (`AvisoDeEspacio`, se crea solo, sin tocar escenas) y
  fila `horda_retrocede` en la telemetría.
- Apagado en el tutorial (`retrocesoAciertos` = 0 en `TutorialConfig`).
- **Luces (lo que pidió revisar):** hoy el faro **frena** a la horda al 50 % mientras está
  prendido (`headlightHordeSlowFactor`), pero no le gana espacio ni la hace retroceder; no
  respawnea nada (los zombies muertos reaparecen al fondo de la masa, 8–14 m atrás del frente,
  tras 1 s de cadáver). Así que con las luces no "se llena el carril al instante": simplemente la
  horda sigue cerca a velocidad reducida. Si se quiere el mismo espacio seguro con las luces
  (por ejemplo, un retroceso al prenderlas), se puede agregar con el mismo mecanismo; queda como
  decisión del equipo.
- Los zombies retroceden con la misma animación de correr (no hay animación de "asustado");
  si molesta, se puede cambiar por la de golpe recibido.

Verificado fuera de Unity: 264 tests en verde (8 nuevos: dispara el retroceso, gana los 12 m,
vuelve a su ritmo, un solo acierto, aciertos muy separados, fallos, enfriamiento, reinicio) y el
juego compila.

## Fase D — Golpes del beat'em up ✅ hecha (falta verla en Play)

- Alinear el momento en que el golpe conecta con el dibujo (hoy conecta al apretar, y el barrido
  llega a su punto más lejano a la mitad de `golpeDuracion`): el impacto pasa al pico del
  barrido, o el alcance se achica a lo visible. Se prueba con tests de tiempo.
- Atacar un poco más rápido: bajar `golpeEnfriamiento` (0,35 en el asset) y/o `golpeDuracion`.
- Combos extra: **al final**, como pidió.

**Lo hecho**
- **La colisión no estaba "más adelante" que el dibujo, el dibujo se quedaba corto:** el golpe
  conecta hasta `golpeAlcance` (1,6 m) medido desde ella, pero el barrido se dibujaba centrado a
  1,1 m (más el avance del cuerpo). Ahora el barrido termina justo donde termina el golpe: el
  borde de adelante queda en `golpeAlcance` y, en el remate, en `remateRadio`. Lo que se ve
  tocado es lo que se golpea. Test nuevo del borde del alcance.
- **Se nota si pegó o no:** el golpe que da en alguien se ve pleno; el que va al aire sale más
  chico (80 %) y apagado (45 %). Antes se veían igual.
- **Un poco más rápido:** `golpeEnfriamiento` 0,35 → 0,27 s y `golpeDuracion` 0,18 → 0,15 s (en
  `GameConfig` y `TutorialConfig`). La cadena de 3 golpes tarda ~0,54 s en vez de ~0,7 s. La pausa
  del remate (0,55 s) y la ventana de la cadena no cambian.
- **El empuje rompía la cadena (lo detectó el equipo):** cada golpe común mandaba al zombie
  2,5 m hacia atrás de un salto (`golpeEmpuje`), y el alcance es 1,6 m: un zombie parado a pegar
  (~1,1 m) quedaba a 3,6 m y el 2.º golpe daba al aire, salvo que ella caminara detrás. Ahora el
  empuje es de **0,35 m** (el pesado, con su resistencia, casi no se mueve): el golpeado se
  tambalea en su lugar (sigue frenado 0,5 s) y los 3 golpes de la cadena conectan sobre el mismo
  zombie. El que lo manda lejos es el remate (`remateEmpuje` 3,5 m). Tests nuevos: la cadena
  entera conecta con el empuje del juego, y con 2,5 m el 2.º golpe da al aire (el caso de antes).
  Si se quiere más "sacudida" en cada golpe, es mejor la animación del golpeado que el empuje.
- Combos nuevos: queda para el final, como se pidió.

Verificado fuera de Unity: 267 tests en verde y el juego compila.

## Fase E — Zombies que saltan ✅ hecha (falta verla en Play)

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

**Lo hecho (con la hoja de 25 cuadros que pasó el equipo)**

> **Corrección del 20/10 (tarde):** la primera versión hacía saltar a zombies que ya venían
> *caminando*. La idea del equipo es otra: el problema es el **corte de la moto al beat'em up**
> (la horda que corría detrás de la moto de golpe camina). El salto es el puente. Quedó así:
>
> - **Los que se ven son los que saltan.** Al bajarse de la moto, los zombies de la horda que
>   estaban más cerca (los 3 de la primera oleada) **conservan su lugar, su carril y su pinta**
>   (no se reubican ni cambian de aspecto) y **siguen corriendo** hacia ella a 11 m/s
>   (`saltoVelocidadCarrera`, la carrera de la horda). Quedan acotados entre 9 y 24 m detrás de
>   ella (`saltoEntradaMin`/`Max`): ni encima, ni tan lejos que tarden. Los que no entran en la
>   primera oleada desaparecen fuera de cuadro. En un reintento entran corriendo por atrás.
> - **Frenando, la horda acompaña a la moto** y no se le acerca a menos de 14 m
>   (`frenadoGapHorda`): al bajarse se la ve venir y hay ~1 s antes del primer salto.
> - **Saltan desde el carril en que vienen** y caen en el carril de ella (se deslizan al
>   despegar); no cambian de carril mientras corren. Los que esperan turno (tope de 2 saltando)
>   se quedan a distancia en vez de pasarle por al lado.
> - Después de saltar (o de ser esquivados) quedan en el piso y **caminan lento / se arrastran**,
>   como las oleadas siguientes.

- **Cuándo salta:** solo los zombies de la **primera oleada** de cada pelea (la transición de
  correr a pelear a pie) y una sola vez cada uno. Si ya no salta (oleadas siguientes) camina como
  siempre. Se apaga con `GameConfig.saltoActivo`.
- **Cómo es:** al acercarse a entre 5,5 y 1 m, el zombie **se agacha** (0,45 s,
  con un aro rojo en el piso donde va a caer, que se achica), **vuela** (0,7 s) y **cae** un poco
  antes de ella, de su lado. El punto de caída la sigue mientras se agacha y queda **fijo al
  despegar**: se esquiva alejándose más de 1,3 m (`saltoRadio`) o cambiando de carril. A lo sumo
  dos saltan a la vez (`saltoMaxSimultaneos`, cuentan dentro de `maxAtacantes`).
- **Si le cae encima:** **1 de vida** (y la empuja un poco, con el respiro de siempre) y el zombie
  se recupera antes. **Si lo esquiva:** queda en el piso 1 s (`saltoAterrizaje`), se levanta y
  camina; no vuelve a saltar.
- **Golpe en el aire:** lo **mata** de una (también el barrido del remate). **Balazo en el aire:**
  le saca **1 golpe** y lo tira donde está, sin dañarla; si le quedaba 1, lo mata. Un golpe o
  tiro mientras se agacha le corta el salto y cuenta como uno común.
- **Dibujo:** hoja `ZombieCorredor_salto.png` (5×5 de 256 px, pivote en los pies, escala igualada
  a la del resto: 181 px por unidad) conectada a los tres tipos en `ZombieLooks.asset`. Cuadros:
  0–6 agacharse, 7–17 vuelo, 18–24 aterrizar y levantarse (`saltoCuadrosAgachar` = 7,
  `saltoCuadrosVuelo` = 11). Las alturas del vuelo ya vienen dibujadas, así que el cuerpo solo se
  mueve en horizontal.
- Telemetría: `saltos_acertados`, `saltos_esquivados` y `saltos_cortados` en la fila
  `pelea_termina`.
- **Para pulir con el arte:** en los cuadros 9 a 14 los brazos tocan el borde derecho de la
  celda (256 px) y quedan cortados; conviene ampliar la celda o acortar el brazo en esos cuadros.
  Sin sonido de salto/aterrizaje todavía (faltan clips).

Verificado fuera de Unity: 289 tests en verde con los del salto (primera oleada, ventana de
distancia, agacharse–vuelo–caída, daño, esquivar, golpe y balazo en el aire, corte al agacharse,
tope de simultáneos) y los de la transición (los más cercanos de la horda siguen con su carril y
corren, acotados a la zona de entrada, saltan desde otro carril, el que espera no pasa de largo,
la horda no se acerca frenando) y el juego compila.

## Fase F — Revisiones menores ✅ hecha (falta verla en Play)

- **Bocacalles juntas (min 5:34):** causa encontrada. La capa "Calle" del nivel 1 tiene la calle
  común (peso 5) y la bocacalle (peso 1) **sin evitar repeticiones**, así que dos bocacalles
  podían salir seguidas y sus calles laterales dejaban un solo hueco sin edificios. Ahora cada
  capa tiene `tilesEntreAcompanantes` (por defecto **1**): entre dos tiles con calle lateral hay
  siempre al menos un tile común (13 unidades), y en ese lugar entra un edificio (7,8). Sigue
  siendo determinista por semilla, pero la calle del nivel 1 se reparte distinto (las bocacalles
  cambian de lugar). Tests: sin la regla salen pegadas, con la regla nunca en 50 semillas.
- **Luces del entorno de la pista:** sigue siendo una idea abierta para hablar con el arte.

## Extra — La arena sigue siendo el lugar donde quedó varada

Pedido del 20/10: al empezar la pelea se quitaban los consumibles **y también los coches y
barriles**; eso quita la sensación de que sigue donde se quedó. Ahora:
- Lo despejado para la pelea (**coches/obstáculos, barriles y rampas**) se **queda de decorado**
  mientras se pelea (`LevelRuntime.Item.Despejado`; no choca ni explota: es solo dibujo).
- Al volver a la moto se **desvanece en 0,5 s** (`segundosDesvanecer`) en vez de desaparecer de
  golpe. Se apaga con `conservarDecoradoEnPelea` en `LevelScene`.
- Siguen escondidos: los **recolectables** (nafta, baterías, balas), los **carteles de aviso** de
  carril y los **zombies de frente** del recorrido (no tiene sentido que haya uno parado).
- Solo cuenta lo despejado en la última pelea; al reiniciar el nivel se borra la marca.

Verificado fuera de Unity: 283 tests en verde y el juego compila.

## Orden y estimación

A → B → C → D → E → F, más el extra de la arena: todas hechas el 20/10 (falta verlas en Play).
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
