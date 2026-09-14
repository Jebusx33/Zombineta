# Estética nueva: escala y encuadre — Diseño

Sub-proyecto 4 del juego definitivo (`Juego/`). Referencia: `Arte/Bocetos/Concept Art/Mapa_Escala.png`.

## Objetivo

Que un nivel de `Juego/` se vea con la escala y la composición de la referencia: carriles finos y
abajo, mucho fondo, personajes grandes, zombies distintos por arquetipo, sin perder la sensación
de velocidad ni la lectura de en qué carril está cada cosa.

Fuera: iluminación 2D (sub-proyecto 5), arte final de escenario y objetos (se usan los
placeholders), animaciones de la protagonista, cambios de reglas o de balance.

## Medidas de la referencia (1920×1080) contra hoy

| | Hoy | Referencia |
|---|---|---|
| Separación entre carriles | ~137 px | ~90 px |
| Alto de moto y zombies | ~110 px | ~220 px (≈ 2,4 carriles) |
| Borde superior de la calle | 45 % de la altura | 60 % |
| Último carril | ~82 % | ~85 % |
| Fondo | franja 18–40 % | todo el 60 % de arriba |
| Debajo de los carriles | postes | vereda con farol y cartel por delante |

## Decisiones tomadas

- **Cámara:** el encuadre de la referencia es el normal; el zoom de tensión se mantiene pero
  reducido a ±10 % y anclado al piso. Atrapada y victoria no cambian de comportamiento.
- **Velocidad en pantalla:** se mantiene el ancho visible actual (~30 m). Los carriles se juntan
  en el mundo y los personajes crecen, en lugar de alejar la cámara.
- **Enfoque:** ajuste en los assets de configuración existentes, calzado a ojo con una guía de
  referencia. Sin un sistema nuevo de escala.
- **Arquetipos por físico:** Común = anciano, oficinista, vagabundo. Corredor = mujer, adolescente.
  Pesado = hombre rojo.
- **El prototipo no cambia.** Nada de esto toca `Prototipo/` ni la lógica de la simulación
  compartida.

## 1. Guía de referencia (solo editor)

- Menú `Zombineta > Encuadre > Mostrar referencia` / `Ocultar referencia`.
- Un Canvas `ScreenSpaceOverlay` con orden 2000 y un `RawImage` estirado a pantalla completa,
  creado en la escena activa con `HideFlags.DontSave` y tag `EditorOnly`: no se guarda ni llega a
  builds. Funciona en edición y en Play.
- La imagen y la opacidad (default 0,4) se guardan en `EditorPrefs`; `Elegir imagen…` abre un
  selector de archivo. Default: la ruta de `Mapa_Escala.png`.
- Si la escena cambia (Play, cambio de nivel), la guía se vuelve a crear mientras esté activada.

## 2. Encuadre y cámara

Valores iniciales, ajustados después con la guía:

- `GameConfig.laneSpacing`: 1,6 → ~1,05 (el valor que hace coincidir las líneas de carril con la
  referencia).
- `CameraConfig`: tamaño normal igual al actual (~6,3) para conservar el ancho visible;
  `tightSize`/`wideSize` a ±10 % de ese valor; `viewBottomY` tal que el carril 0 quede al ~85 % de
  la altura. `catchSize` y `victorySize` se recalculan con la misma proporción respecto del
  normal que tienen hoy.
- El director de cámara ya ancla el borde de abajo (`viewBottomY`): al hacer zoom el piso no se
  mueve. No hace falta código nuevo si el anclaje se verifica en las capturas.
- Los `LevelItem` de `Level_01`/`Level_02` se reacomodan solos: su Y sale de `laneSpacing` al
  abrir la escena. Se guardan las dos escenas después de abrirlas.

## 3. Tamaños

Todo en proporción al cambio de altura de personaje (≈ ×2), ajustado con la guía:

- Moto: escala del objeto `Scooter` en `NivelBase` y en los niveles.
- Zombies: una escala base de la vista de la horda; los tipos conservan su escala relativa de
  `Zombies.asset` (1 / 0,85 / 1,25).
- Objetos del recorrido: `LevelItemPalette.asset` (escala de cada tipo).
- `GameConfig.jumpHeightToWorld` ×2 (solo visual: altura del arco en pantalla).
- Efectos: partículas de impacto y sangre, manchas en el piso, polvo de la rueda, origen del
  trazo del disparo y alcance/posición del faro. Cada uno toma su tamaño de su config o de su
  prefab; se ajustan ahí.

## 4. Orden por carril y sombras

**Orden de dibujo.** Con cuerpos de más de dos carriles de alto, lo del carril de abajo tiene que
tapar a lo de arriba.

- `LaneSorting` (C# plano, en `Scripts/Levels` o `Scripts/Gameplay`):
  `int Order(float visualLane, int layerOffset)` → `Base − round(visualLane × 100) + layerOffset`.
  El carril visual es continuo, así durante un cambio de carril el orden se interpola sin saltos.
  `layerOffset` separa dentro de un mismo carril: sombra < objeto < zombie < moto < efectos.
- Lo usan `ScooterView` (con `LaneVisual`), la vista de la horda (carril de cada zombie), la
  vista de los objetos del nivel (`LevelScene.ApplyVisual` y en juego) y los efectos que nacen en
  un carril.
- Rango reservado: todo lo del juego queda entre las capas de calle (−20) y las de primer plano
  (≥ 40), que siguen dibujándose encima de todo.

**Sombras.** Una elipse oscura en el piso del carril bajo cada cosa:

- `GroundShadow` (MonoBehaviour): sigue la X del dueño, se queda en la Y del piso de su carril,
  escala con el dueño y se achica y aclara con la altura (misma regla que la sombra actual de la
  moto, que pasa a usar este componente).
- Sprite: una elipse suave generada (`Art/Fx/sombra.png`).
- Moto, zombies de la horda, zombies de frente, obstáculos, barriles y pickups. Los pickups
  aéreos proyectan la sombra en su carril, lo que ayuda a leer dónde se agarran.

## 5. Zombies por arquetipo

**Datos.** Un asset de `Juego/`, `Settings/ZombieLooks.asset` (`ZombieLookSet`), fuera de la
simulación compartida:

- `ZombieLook`: nombre, `walk` (sprites), `hit` (sprite o sprites), `death` (sprites), `walkFps`
  a velocidad de referencia, `pivot` común (pies), `scale` relativa.
- Por tipo de `Zombies.asset` (por índice): la lista de looks que puede usar.

**Elección.** `ZombieLookPicker` (C# plano, testeado): dado semilla, índice de unidad, número de
reciclado y tipo, devuelve un look; misma entrada, mismo look; con más de un look por tipo no
repite el del zombie anterior del mismo tipo cuando se puede.

**Animación por código.** La vista de cada zombie pasa de `Animator` a un flipbook:

- Caminata: avanza cuadros a `walkFps × (velocidad del zombie / velocidad de referencia)`.
- Impacto: al recibir un disparo y sobrevivir, muestra `hit` ~0,2 s y vuelve a caminar.
- Muerte: reproduce `death` una vez y queda en el último cuadro hasta reciclarse.
- Arquetipos de 4 poses: `walk` = las dos poses de pie alternadas, más un balanceo vertical y de
  rotación leve para que no se vea rígido; `death` = la pose en el piso.
- El `ZombieAnimator.controller` y sus clips quedan sin uso en el nivel; no se borran.

**Preparación del arte.**

- `Tools/SpritePrep` suma `remove_white_background.py` (con tests): flood fill del blanco desde
  los bordes con tolerancia, para no comerse blancos interiores (ojos, dientes). El de fondo
  cuadriculado ya existe y se usa para `ZombieFlesh`.
- Recortador de editor (`Zombineta > Arte > Recortar hoja por transparencia`): encuentra cada
  figura por componentes conectados de alfa, ordena por fila y columna, y escribe los sprites de
  la hoja con pivot en los pies (centro abajo). Mismo criterio que el recortador descripto en el
  plan original para las hojas sin grilla exacta.
- Fuentes: `Zombies_poses.png` (6 arquetipos × 4 poses), `ZombieFlesh.jpeg` (hombre rojo, hoja
  completa), `ZombieViejo.png` (anciano, hoja completa, ya importada). Donde un arquetipo tiene
  hoja completa, se usa esa y no las poses.
- Destino: `Juego/Assets/_Zombineta/Art/Zombies/`.

**Zombie de frente.** El objeto del recorrido usa un look del tipo que indica su `variant`, con
la misma elección determinística (índice del item como unidad) y mirando hacia la moto.

## 6. Escenario según la referencia

`Escenario.asset` de `Juego/`, capas de atrás hacia adelante (valores iniciales, ajuste con la
guía):

| Capa | Parallax | Qué es | Placeholder |
|---|---|---|---|
| Cielo | 0 | fondo | el actual |
| Edificios | 0,6 | altos, llegan cerca del borde superior | `edificio_1..3`, `tienda`, `bocacalle` |
| Árboles | 0,9 | troncos sobre la vereda de enfrente | `arbol_1`, `arbol_2` |
| Cordón de enfrente | 1 | línea donde empieza la calle (60 %) | `pista` recortada o una franja |
| Calle | 1 | asfalto y líneas de carril | el actual |
| Vereda de abajo | 1 | debajo del carril 0 | `pista` o franja |
| Primer plano | 1,6 | farol, cartel, cartel de piso | `farola`, `cartel`, `cartel_piso`, `señalizacion` |

- `Vias`, `Vallas` y `Postes` quedan con `enabled = false`.
- Los sprites nuevos se copian de `Arte/Bocetos/Tileset` a `Juego/Assets/_Zombineta/Art/Tileset/`
  (la ñ de `señalizacion` se quita del nombre, como en el prototipo).
- El marcado de carriles (`LaneMarkersView`) toma la nueva separación de `GameConfig`.

## 7. Pruebas

**EditMode:**
- `LaneSorting`: abajo delante de arriba; interpolación continua entre carriles; los offsets
  ordenan sombra < objeto < zombie < moto < efectos dentro de un carril.
- `ZombieLookPicker`: determinismo; solo looks del tipo; sin repetición inmediata cuando hay más
  de uno; tipo sin looks → nulo (la vista cae en el sprite actual).
- `remove_white_background.py`: fondo blanco quitado, blanco interior conservado.
- Los 166 tests existentes siguen en verde.

**Play por MCP:**
- Capturas del Game view en plano normal, cerrado (horda encima), atrapada y victoria, y cada
  una mezclada con la referencia al 40 % para comparar líneas de carril, calle y tamaños.
- Moto y zombies en carriles distintos: el de abajo tapa al de arriba; durante un cambio de
  carril no hay parpadeo de orden.
- Mezcla de looks por tipo visible en la horda; impacto y muerte con su animación.
- Objetos de `Level_01` sobre sus carriles nuevos; consola sin errores; recorrido del flujo hasta
  el nivel y pausa.
- `Prototipo/` sigue compilando (no se tocó código compartido).

## Riesgos

- **Lectura de carril.** Con personajes altos y carriles finos puede costar ver en qué carril está
  un obstáculo. Las sombras y el orden son la respuesta; si no alcanza, se agrega un tinte de piso
  por carril en el marcado (sin tocar reglas).
- **Colisión percibida.** Las reglas siguen siendo por carril y distancia: un zombie de otro
  carril puede "tocar" visualmente a la moto sin que pase nada. Es aceptable y ya pasa en la
  referencia.
- **Recorte de las poses.** Si en `Zombies_poses.png` dos figuras se tocan, el recorte por alfa
  las une; el recortador reporta figuras fuera del tamaño esperado para ajustarlas a mano.
