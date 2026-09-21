# Guía de iluminación para arte

Esta guía es para quien abre Unity a laburar, no a leer código. Todo lo que describe se hace
arrastrando cosas en el Inspector y tocando menús — ningún paso pide escribir una línea de C#.

Está escrita contra lo que **ya existe** en el proyecto hoy (no contra lo que se planeó al
arrancar el sub-proyecto: algunas cosas cambiaron en el camino). Si algo de acá no coincide con lo
que ves en el Editor, avisale a programación antes de seguir.

---

## 1. Abrir el proyecto

- Abrí **`Juego/`** desde Unity Hub — nunca la carpeta raíz del repo, ni `Prototipo/` (ese
  proyecto está congelado).
- Unity **6000.6.0f1** exacto. Si Hub te ofrece bajar o actualizar el editor, no lo hagas sin
  preguntar.
- Qué escena abrir según lo que vas a hacer:
  - **`Assets/_Zombineta/Scenes/TallerLuz.unity`** — para laburar la luz de **un asset suelto**
    (un edificio, un farol, un barril) sin distraerte con el resto del nivel. Es la escena de la
    sección 7.
  - **`Assets/_Zombineta/Scenes/Level_01.unity`** (o `Level_02.unity`) — para ver tu asset **puesto
    en un nivel real**, con el resto del escenario alrededor y la moto andando.

---

## 2. Cómo está armado el escenario

El fondo tiene cinco capas de dibujo (de atrás para adelante): **Cielo, Fondo, Calle, Juego,
Frente**. Cada tile del escenario (un edificio, un árbol, un cartel) vive en una de esas capas
según dónde tiene que aparecer:

| Capa | Qué va ahí |
|---|---|
| Cielo | El fondo del cielo |
| Fondo | Edificios, árboles |
| Calle | Cordón, asfalto, vereda |
| Juego | La moto, la horda, los items del recorrido, sombras de piso, efectos |
| Frente | Postes, carteles y árboles en primer plano, delante de todo |

Ojo con **`Default`**: es una capa vieja de Unity que **no recibe luz de ambiente a propósito**.
Si un sprite tuyo queda en `Default` se va a ver completamente negro de noche. Todo lo que arte
toca tiene que ir en una de las cinco capas de arriba.

**El escenario se arma solo, tile por tile, mientras la cámara avanza** (no es una imagen fija ni
un tilemap pintado a mano). Por eso **un tile tiene que empalmar consigo mismo en los dos bordes**
(izquierdo y derecho): el sistema pone copias de tu tile una al lado de la otra, y cualquier línea
que no llegue justo al borde se nota como una costura cuando se repite.

Quién decide a qué capa y con qué probabilidad aparece cada tile es el asset
**`Assets/_Zombineta/Settings/Escenario.asset`** — ahí es donde se agrega un tile nuevo (ver
sección 3).

---

## 3. Hacer un asset nuevo, paso a paso

Esto vale tanto para un tile de escenario (edificio, árbol, farol) como para un objeto del
recorrido (bidón, barril, rampa, obstáculo).

### 3.1. Preparar el PNG

- Fondo **transparente** (alfa real, no blanco ni cuadriculado horneado).
- **Pivot en la base**: el punto (0,0) del sprite tiene que quedar en los "pies" del objeto — es
  la convención que usa todo el proyecto para apoyar las cosas en el piso o en el carril.
- Si el PNG es una hoja ancha (más de 2048 px de lado), subile `Max Size` al importador antes de
  recortar nada: por default Unity la achica en silencio a 2048 sin avisar.

### 3.2. Importar y armar el material

- Arrastralo a `Assets/_Zombineta/Art/Tileset/` (si es escenario) o
  `Assets/_Zombineta/Art/Level/` (si es un objeto del recorrido).
- Sprite Mode: **Single** (o **Multiple** si es una hoja con varias piezas — recortalo con
  `Assets > Zombineta > Recortar por transparencia`, ver el HANDOFF si tenés dudas de esa parte).
- El material del `Sprite Renderer` tiene que ser uno **Lit** (`Sprite-Lit-Default`, el que ya usan
  todos los tiles y los items): es el que reacciona a las luces 2D. Con el material de siempre
  (`Sprite-Unlit-Default`), tu asset se va a ver siempre igual de brillante, de día o de noche.

### 3.3. Normal map (opcional, todavía no lo usa nadie)

El material Lit puede recibir un normal map para que la luz le dé relieve real a la fachada de un
edificio (grietas, ventanas hundidas) en vez de un dibujo plano. **Hoy ningún asset del proyecto
tiene uno todavía** — es una mejora que queda pendiente para cuando arte tenga tiempo, no algo que
falte para que la luz funcione. Cuando quieras probarlo:

1. Generá el normal map de tu textura (con cualquier herramienta externa; no hay un paso de
   Unity para esto).
2. En el importador del sprite (seleccionalo en el Project, panel Inspector), abrí
   **Secondary Textures** y agregá una entrada con nombre `_NormalMap` apuntando a esa textura.
3. Aplicá. El material Lit la va a usar solo si la sorting layer tiene una luz cerca — en
   ambiente muy tenue casi no se nota.

### 3.4. Crear el prefab

Los prefabs de escenario y de recorrido comparten el mismo contrato: **la raíz tiene el
`Sprite Renderer` con tu sprite y el material Lit**; el pivot de la raíz manda (la base del
sprite tiene que estar en `y = 0` de la raíz). Podés sumarle, como hijos opcionales, una o más
`Light2D`, un `ShadowCaster2D` y/o un `Flicker` (ver secciones 4 y 5).

**No hay una plantilla en blanco para arrastrar**: la forma más rápida es duplicar un prefab
existente que ya cumple el contrato y cambiarle el sprite:

- Para escenario, duplicá alguno de `Assets/_Zombineta/Art/Tileset/Prefabs/` (por ejemplo
  `tile_placeholder_farola.prefab`, que ya tiene su `ShadowCaster2D`).
- Para un objeto del recorrido, duplicá alguno de `Assets/_Zombineta/Art/Level/Prefabs/` (por
  ejemplo `Barril.prefab`, que ya tiene `Light2D` y `ShadowCaster2D`).

**La capa de dibujo, el orden de dibujo y la escala del tile los pone el sistema solo al
instanciarlo — lo que vos le pongas en el prefab a esos tres campos se pisa.** No pierdas tiempo
ajustándolos ahí.

### 3.5. Engancharlo

- **Tile de escenario**: abrí `Assets/_Zombineta/Settings/Escenario.asset`, buscá la capa que
  corresponde (según la tabla de la sección 2) y agregá una entrada nueva en su lista de
  variantes. Completá:
  - **`Prefab`**: el prefab que armaste en 3.4.
  - **`Grupo`**: una palabra libre para agrupar el tile en el taller (hoy casi todo dice
    `"ciudad"` — usá esa u otra si te sirve para organizar).
  - **`Weight`**: qué tan seguido aparece contra los demás tiles de esa capa (0 lo apaga sin
    borrarlo).
  - **`Height Scale`** / **`Y Offset`**: si tu tile necesita verse más alto/bajo o corrido en
    vertical contra el resto de la capa.
  - **No toques el campo `Sprite`**: está escondido a propósito (es un resabio de cuando el
    escenario todavía no usaba prefabs) y no hace nada en el juego.
- **Objeto del recorrido** (bidón, batería, balas, rampa, barril, obstáculo): abrí
  `Assets/_Zombineta/Settings/LevelItemPalette.asset`, buscá la entrada (`Look`) del tipo que
  te interesa y asignale tu prefab en el campo **`Prefab`**. Los campos `Sprite`/`Color`/`Scale`
  de esa misma entrada quedan como respaldo por si algún día se saca el prefab; con `Prefab`
  asignado, son los que manda tu prefab.

No hace falta tocar código ni escenas para nada de esto.

---

## 4. Ponerle luz

Qué `Light2D` usar según el caso:

| Caso | Tipo de Light2D |
|---|---|
| Un farol, una luz puntual | **Point** |
| Una vidriera, una franja de luz pareja | **Freeform** (dibujás el polígono) |
| Un cartel de neón con forma propia | **Sprite** (usa la silueta del sprite como forma de luz) |

Pasos, sobre el prefab de tu asset (abrilo en modo prefab, doble click):

1. Agregá un hijo (`Create Empty` dentro del prefab) y ponele un `Light2D` del tipo que
   corresponda.
2. **Capas que alcanza**: en el Inspector del `Light2D`, el campo se llama **"Target Sorting
   Layers"**. Ahí marcás a qué capas de dibujo le pega tu luz. Esto es lo que hace que, por
   ejemplo, un farol ilumine la calle y la vereda sin lavarle el color al cielo. Los faroles y
   carteles de calle normalmente van a **Calle** y **Juego** (las mismas dos que usa el faro de
   la moto y los destellos de disparo/choque).
3. Ajustá color, intensidad y radio a ojo, mirándolo en el taller (sección 7) con el ambiente de
   noche real puesto.

**Cuándo usar un sprite "prendido" en vez de una luz real**: si lo que querés es una ventana con
la persiana iluminada o un cartel que se ve "encendido" sin que en verdad ilumine nada alrededor,
alcanza con pintarlo así en el sprite (colores más claros/saturados en esa zona) — no hace falta
gastar una `Light2D` de verdad para eso. Reservá el `Light2D` para cuando la luz tiene que
iluminar objetos de al lado.

**Parpadeo (`Flicker`)**: para un neón o un farol viejo que titila, agregale al mismo GameObject
que tiene el `Light2D` un componente **`Flicker`**. Tiene cuatro campos:

- **`Frequency`**: qué tan rápido titila (0 = luz fija, sin parpadeo).
- **`Min`** / **`Max`**: entre qué intensidades se mueve.
- **`Seed`**: cambiala si tenés dos luces con `Flicker` cerca una de la otra — con la misma
  semilla titilan exactamente igual, sincronizadas, y se nota artificial; con semillas distintas
  cada una tira para su lado.

Así se ve una luz de adorno ya andando (el barril, en el taller), con el resplandor cálido de su
propio `Light2D` marcándose contra el fondo oscuro:

![Un prefab con su Light2D encendido, visto en el taller](img/luz/prefab_luz.png)

---

## 5. Sombras

Agregale un **`ShadowCaster2D`** (hijo del mismo GameObject que tiene el `Sprite Renderer`, o de
la raíz) a lo que esté **cerca de la calle y sea sólido**: faroles, carteles, vallas, obstáculos,
barriles. El fondo lejano (edificios, árboles de fondo, el cielo) **no** proyecta sombra — no le
pongas `ShadowCaster2D` a esas capas, es gasto de rendimiento sin nada que se note.

**La silueta tiene que ser simple, no el contorno del dibujo.** Un rectángulo o una forma de
pocos puntos que cubra el bulto del objeto alcanza y sobra — no dibujes cada detalle del sprite en
la forma de la sombra, eso solo hace más caro el cálculo sin mejorar cómo se ve.

Para dibujar la forma: seleccioná el `ShadowCaster2D` en el Inspector y usá el botón de edición de
forma (el mismo flujo que un `PolygonCollider2D`): vas agregando y moviendo los puntos del
polígono directamente en la Scene View, sobre tu sprite, hasta que la silueta te cierre.

---

## 6. El ambiente

El **`PerfilDeLuz`** es el asset que define si un nivel es de noche cerrada o de atardecer: por
cada una de las cinco capas de dibujo, un color y una intensidad de luz general, más un color e
intensidad "general" que suma sobre todas. Vive en `Assets/_Zombineta/Settings/Luz/` — hoy el
único que existe es **`Noche.asset`**, y lo usan `Level_01` y `Level_02` los dos (todavía no se
separó uno para cada nivel).

Para probarlo:

1. Abrí `Noche.asset` (o creá uno nuevo con el menú `Create > Zombineta > Perfil de luz` si
   querés un atardecer aparte) y tocale los colores/intensidades de una capa.
2. Con la escena abierta y el editor con foco (no hace falta estar en Play), el cambio se ve
   solo, casi al instante: hay un componente (`LightingDirector`, en el objeto `Nivel` de la
   escena) que está mirando el asset todo el tiempo y reconstruye la luz apenas nota un cambio.
3. **El perfil de un nivel se asigna en `LevelScene`** (el componente del objeto `Nivel`, en el
   campo **`Perfil`**) — es la única fuente real. No hace falta (ni conviene) asignar también un
   perfil directo en `LightingDirector`: si lo dejás vacío, lo toma solo del `LevelScene` de al
   lado.

Así se ve `Noche.asset` puesto en un nivel real, con el faro apagado (para que se note el color de
ambiente puro, sin el cono de luz de la moto encima):

![El PerfilDeLuz Noche.asset en Level_01, con el faro apagado](img/luz/perfil_noche.png)

---

## 7. Ver el resultado

Tenés tres formas de ver tu trabajo, de la más aislada a la más real:

1. **El taller** (`Assets/_Zombineta/Scenes/TallerLuz.unity`): una copia de **cada** prefab de
   escenario y de cada item del recorrido, agrupada por capa, a su altura y escala reales, con el
   ambiente de `Noche.asset` puesto y la moto (con su faro) al lado para comparar tamaños. No se
   edita a mano: si agregaste un prefab nuevo (sección 3) o cambiaste alguno existente, hay que
   **regenerarlo** con el menú **`Zombineta > Luz > Construir taller`** para que aparezca. Las
   capas de escenario que hoy están apagadas en el juego (`enabled` sin marcar en
   `Escenario.asset`) igual se arman ahí, pero separadas del resto y a mitad de opacidad, con el
   cartel "(apagada en el juego)" — así las ves sin confundirlas con lo que sí sale en una
   partida real.

   ![El taller de luz, con la fila del Cielo y la moto de referencia](img/luz/taller.png)

2. **"Probar desde acá"**: con `Level_01` o `Level_02` abierto, fuera de Play, la paleta de nivel
   (overlay "Nivel Zombineta" en la Scene View) tiene un botón **"Probar desde acá"**: entra en
   Play con la moto arrancando justo en los metros donde tenías la vista centrada. Es la forma más
   rápida de ver tu asset puesto en el contexto real del nivel, con el resto del escenario
   alrededor, sin tener que jugar el nivel entero desde el principio.

3. **El interruptor Alta/Baja**: en Opciones hay un desplegable **"Iluminacion"** con **Alta** y
   **Baja**. Anda tanto en Play como en una build. Tu asset se tiene que seguir viendo bien en las
   dos calidades — en particular, si le pusiste sombra (sección 5), verificá que en Baja
   simplemente desaparezca en vez de verse rota o cortada (en Baja se apagan todas las sombras y
   no queda ninguna luz de adorno prendida, ver sección 8).

---

## 8. Presupuesto

No todas las luces de adorno de la pantalla quedan prendidas al mismo tiempo — sería carísimo
para la máquina. Una luz es "de adorno" cuando **no hace falta para jugar**: el brillo de una
vidriera, un cartel de neón, el farol de una esquina. (El faro de la moto, los destellos de
disparo/choque/explosión y el resplandor de la horda **no** entran en esta cuenta: esos se
prenden siempre.)

Cómo funciona, en criollo: el sistema mira todas las luces de adorno que hay cerca de la cámara y
solo deja prendidas las más cercanas, hasta un tope (hoy **12** en calidad Alta; **0** en Baja —
en Baja no queda ninguna luz de adorno prendida). Si tu asset tiene una `Light2D` de adorno,
tenés que agregarle también un componente **`DecorLight`** al mismo GameObject (junto al
`Light2D`), con un campo **`Priority`**: entre dos luces igual de lejos de la cámara, gana la de
mayor prioridad. Sin `DecorLight`, tu luz **no** entra en esta cuenta — queda siempre prendida sin
importar cuántas otras haya alrededor, lo cual puede volverse caro si arte pone muchas.

Qué pasa cuando te pasás del tope: nada se rompe ni tira error — las luces que no entran
simplemente quedan apagadas (no destruidas), y se van prendiendo y apagando solas a medida que la
cámara se mueve y cambia cuál es "la más cercana".

Comparación real de Alta contra Baja, mismo tramo de `Level_01`, con el faro prendido en los dos
casos:

| Alta (sombras + luces de adorno) | Baja (sin sombras, sin luces de adorno) |
|---|---|
| ![Iluminación en calidad Alta](img/luz/calidad_alta.png) | ![Iluminación en calidad Baja](img/luz/calidad_baja.png) |

La diferencia principal se nota en el cono del faro: en Alta corta con sombras duras (cables,
ramas) recortadas encima del asfalto; en Baja es un parche de luz más parejo, sin esas sombras. Es
la diferencia que hay que tener en la cabeza al ajustar una escena: lo que ves en Alta (con todas
las sombras y adornos) es el techo; en Baja tiene que seguir siendo legible sin todo eso.

---

## 9. Qué no tocar

- **La capa de dibujo, el orden de dibujo (sorting order) y la escala** de un tile o de un item:
  las pone el sistema al instanciar el prefab, siempre. Lo que dejes puesto en esos campos dentro
  del prefab se pisa apenas entra en juego — no pierdas tiempo ajustándolos ahí (ver sección 3.4).
- **Los números del recorrido** (dónde va cada bidón, cada rampa, la distancia del nivel) y **los
  números de la cámara** (tamaño, velocidad de zoom, anticipación) son de programación/diseño de
  nivel, no de arte de iluminación. Si algo de eso te haría falta cambiar para que tu luz se vea
  bien, hablalo con programación en vez de tocarlo directo.
- **El campo `Sprite` de una variante de `Escenario.asset`** (está escondido en el Inspector a
  propósito): es un resabio de antes de que el escenario usara prefabs y no hace nada hoy. Lo que
  importa es el campo `Prefab`.
- **`TallerLuz.unity`**: no se edita a mano. Se regenera con
  `Zombineta > Luz > Construir taller` cada vez que hace falta reflejar un prefab nuevo o
  cambiado.

---

## 10. Checklist antes de subir

- [ ] El material del `Sprite Renderer` de tu prefab es uno **Lit** (`Sprite-Lit-Default`), no
  Unlit.
- [ ] El pivot de la raíz del prefab está en la base del sprite (los "pies" en `y = 0`).
- [ ] Si el asset va cerca de la calle y es sólido: tiene su `ShadowCaster2D` con una silueta
  simple (no el contorno del dibujo).
- [ ] Si tiene una `Light2D` de adorno (no de gameplay): tiene también su `DecorLight` con una
  prioridad puesta a criterio.
- [ ] Probaste el asset en el **taller** (regenerado con `Zombineta > Luz > Construir taller`) y
  en un **nivel real** (`Level_01`/`Level_02`, con "Probar desde acá").
- [ ] Lo viste en **Alta y en Baja** (desplegable "Iluminacion" en Opciones) y se sigue leyendo
  bien en las dos.
- [ ] Guardaste la escena/asset con Ctrl+S antes de subir.

**Para subir el trabajo**: Git LFS ya está configurado para PNG (no hace falta hacer nada
especial al agregar una textura nueva, git la va a versionar como puntero automáticamente). Subís
a tu rama de siempre (no a `master` directo) — si tenés dudas de cuál es la tuya, preguntale a
programación antes de pushear.
