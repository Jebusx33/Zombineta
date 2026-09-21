# Opening del nivel 1 — Diseño

## Objetivo

La cinemática de entrada del nivel 1 arma una página de cómic viñeta por viñeta: cada imagen se
suma encima de las anteriores hasta completar la página. Hoy la cinemática muestra una viñeta por
vez (aparece, se va, viene la siguiente).

Fuera: sonido, animación dentro de cada viñeta, el opening de otros niveles.

## Material

- Origen: `Arte/Bocetos/scene_opening/art_opn_page_2k_frame_1.png` … `frame_9.png`. El orden de
  aparición es el número del nombre.
- Se ignoran la carpeta `old/` (bocetos viejos) y `[position_references].png` (la página armada de
  referencia).
- Todas miden 3840×2160 (16:9) y van apiladas en el 0,0, a pantalla completa. `frame_1` es opaco
  (el fondo de la página); las otras ocho tienen fondo transparente.
- Son bocetos. El arte final llega después, con el mismo tamaño y la misma composición, y se
  reemplaza pisando cada PNG en `Juego/Assets/_Zombineta/Art/Cinematicas/Nivel01/` con el mismo
  nombre. Como el `.meta` no cambia, las referencias siguen andando: el reemplazo no toca ni
  escenas, ni assets, ni código.
- Importación: Sprite, sin mipmaps, tamaño máximo 2048 (queda 2048×1152). En pantalla se estira a
  la pantalla completa; a 1080p no se nota la diferencia y evita cargar nueve texturas 4K.

## Comportamiento

1. El título del nivel aparece 2 s y funde, como hoy.
2. Se arma la página: cada viñeta entra con un fundido de 0,35 s sobre las que ya están.
3. Cada viñeta espera sus segundos (`seconds`, editable en `Niveles.asset`). Submit (Enter / A)
   pasa a la siguiente; Cancel (Esc / B) saltea toda la cinemática.
4. Con la última viñeta, la página completa se sostiene sus segundos y todo funde a negro antes de
   pasar al nivel.

## Datos

`ComicPanel` (en la simulación compartida, cambio aditivo) suma `bool nuevaPagina`:

- `true`: la página que está en pantalla funde y la viñeta arranca una limpia.
- `false`: la viñeta se suma encima de las que ya están.
- La primera viñeta de un nivel siempre abre página, tenga el valor que tenga.

Default `true`: las viñetas que ya existen en otros niveles (y las que se agreguen sin tocar nada)
siguen viéndose de a una, como hoy. `Level_01` carga las nueve viñetas con `nuevaPagina` en
`false` salvo la primera.

La regla de páginas (qué viñetas quedan en pantalla en cada paso) es una función pura en C#
plano, testeada, para que `CinematicPlayer` solo dibuje.

## Escena

En `Cinematic.unity`, el `Image` único de las viñetas pasa a ser una capa con una `Image` por
viñeta de la página, todas estiradas a pantalla completa y con `preserveAspect` (si la pantalla
no es 16:9, la página queda con bandas en vez de deformarse). `CinematicPlayer` crea las `Image`
que falten al vuelo y las recicla entre páginas.

## Pruebas

- EditMode: la regla de páginas (una página con todas acumuladas, todas en `nuevaPagina`, la
  primera ignorando su valor, dos páginas seguidas).
- Los tests actuales (237) siguen en verde.
- Play por MCP entrando por el menú: capturas del paso 1, del paso 5 y de la página completa,
  comparadas contra `[position_references].png`; Submit adelanta y Cancel saltea; el nivel 2
  sigue como antes.
