# Animación del zombie viejo — diseño

Fecha: 2026-09-05

## Contexto

`Zombie.prefab` hoy es un `SpriteRenderer` estático usando `GreyboxSquare.png` teñido de
verde, instanciado 12 veces por `HordeView.cs` (que solo mueve la posición X/carril de cada
instancia; no hay animación ni estado individual por zombie). La simulación (`RunSimulation`,
`RunState`) solo conoce la horda como un número (`HordeX`) — no hay zombies individuales con
vida o muerte propia.

El arte real ya existe: `Arte/Referencias/ZombieViejo.png` (2752×1536), una hoja de 8
columnas × 3 filas (344×512 por celda, división exacta) con un ciclo de caminata, una
reacción de impacto y una secuencia de caída/muerte, dibujados a mano. Esta es la primera
hoja de zombie que se integra al prototipo (Fase 5 del HANDOFF).

## Problema encontrado: sin canal alfa real

`ZombieViejo.png` es RGB puro. El cuadriculado que visualmente parece "fondo transparente"
está horneado como píxeles opacos (dos grises, ~(66,66,66) y ~(104,104,104), alternados en
un patrón de ~30px). Importado tal cual, cada zombie se vería como un cuadrado gris en
juego. Coincide con lo que el HANDOFF ya señalaba como pendiente para el arte.

**Solución:** recortar la hoja en sus 24 celdas y, en cada celda, aplicar flood-fill desde el
borde para transparentar solo el gris del cuadriculado que está conectado al borde de la
celda (no un umbral global). Esto evita comerse partes del dibujo que caigan en ese rango de
gris (contornos oscuros, sombras) porque solo afecta región conectada al fondo, no cualquier
píxel gris aislado dentro del personaje. El resultado se reensambla en una hoja limpia con el
mismo layout 8×3, y esa es la que se copia al proyecto de Unity. El archivo original en
`Arte/Referencias/` no se modifica.

## Mapeo de frames (celdas fila,columna — 0-indexado)

| Clip | Celdas | Frames | Comportamiento |
|---|---|---|---|
| **Walk** | fila 0: c0→c7 | 8 | Loop, ~10 fps (~0.8s/ciclo) |
| **Hit** | fila 1: c1, c2 (impacto con sangre) | 2 | No-loop, ~8 fps, vuelve a Walk al terminar (Exit Time) |
| **Death** | fila 1: c3,c4,c5 (tambaleo) + fila 2: c0→c7 (encorvarse → caer → tirado) | 11 | No-loop, se congela en el último frame |

Sin usar: fila1-c0 y fila2-c0 (poses de caminata redundantes, dejadas de lado en esta pasada).

## Implementación en Unity

1. **Import:** hoja limpia en `Assets/_Zombineta/Art/Zombies/ZombieViejo.png`. Sprite Mode =
   Multiple, Filter Mode = Bilinear (arte pintado, no pixel art).
2. **Slicing:** Sprite Editor (`com.unity.2d.sprite`, ya instalado) → Slice → Grid By Cell
   Count → 8×3 → Apply. Grid (no Automatic) para que el rect de cada sprite sea la celda
   completa sin recorte por alfa — así el pivot queda en el mismo punto relativo en cada
   frame y el personaje no salta verticalmente entre poses de distinta altura (parado vs.
   tirado).
3. **Pixels Per Unit:** se calcula midiendo la altura real del personaje dentro de la celda
   (con el alfa ya limpio) para acercarse a la altura actual del placeholder (~1.1 unidades),
   pero respetando el aspect ratio real del dibujo en vez del estiramiento no uniforme actual
   del prefab (escala 0.5×1.1).
4. **Animator Controller** nuevo (`ZombieAnimator`), 3 estados (Walk / Hit / Death), 2
   triggers (`Hit`, `Die`). Walk es default y loop. Hit → Walk por Exit Time. Death es
   terminal. Los triggers **no se conectan a ningún evento de gameplay** en esta pasada — no
   existe hoy un concepto de "este zombie individual recibió un disparo" en la simulación;
   conectar eso es trabajo aparte, fuera de este alcance.
5. **Zombie.prefab:** agrega `Animator` con el controller nuevo, saca el tinte verde
   (`m_Color`) para mostrar los colores reales del arte, ajusta la escala según el PPU
   elegido en el paso 3.

## Fuera de alcance

- No se toca `HordeView.cs`, `RunSimulation`, ni ningún dato de gameplay.
- No se agrega lógica de "vida por zombie" ni disparo de Hit/Die por eventos reales.
- No se procesan las otras hojas de arte (protagonista, otros zombies) — HANDOFF ya nota que
  esas tienen grillas no exactas (3168/5 no es entero) y van a necesitar un enfoque distinto
  (probablemente slicing automático por alfa en vez de grilla fija).

## Verificación

Abrir el proyecto vía MCP de Unity, entrar a Play Mode y capturar la escena para confirmar
visualmente que la horda camina con sprites reales, sin fondo gris ni parpadeo de pivot. Los
triggers Hit/Die se prueban manualmente desde el Animator Controller en el editor (no hay
forma de dispararlos por gameplay todavía).
