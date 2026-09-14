# SpritePrep

Herramienta de un solo uso (no forma parte del build del juego) para arreglar hojas de
arte que vienen con un fondo "transparente" horneado como cuadriculado gris opaco en vez
de canal alfa real — el caso de `ZombieViejo.png` y, probablemente, de las próximas hojas
de la protagonista (ver HANDOFF.md, sección 7).

## Uso

```bash
pip install pillow numpy scipy
python remove_checker_background.py <hoja_origen.png> <hoja_limpia.png> <columnas> <filas>
```

Solo transparenta el cuadriculado que está conectado al borde de cada celda de la grilla
(flood fill por componentes conexas), así no se come partes del dibujo que caigan en un
gris similar (contornos, sombras). Requiere que la hoja divida exacto en `columnas x filas`
— si no divide exacto, hay que ajustar el recorte a mano o extender el script para
detectar el tamaño de celda real primero.

## Fondo blanco

Para hojas con fondo blanco liso (o casi blanco, ruido de escaneo/JPEG) en vez de cuadriculado:

```bash
python remove_white_background.py <hoja_origen.png> <hoja_limpia.png> [--tolerance N]
```

Igual que el de cuadriculado: solo transparenta el blanco conectado al borde de la imagen
(flood fill por componentes conexas). Los blancos interiores del dibujo (ojos, dientes,
brillos) no tocan el borde, asi que quedan opacos. `--tolerance` (default 24) controla que
tan lejos de blanco puro (255,255,255) entra en la mascara — subirlo agarra mas ruido casi
blanco, bajarlo protege detalles claros cerca del contorno.

**Leccion de `Arte/Referencias/ZombieFlesh.jpeg`:** su cuadriculado horneado usa grises
mucho mas claros (~212,212,212 alternando con blanco 255) que las bandas hardcodeadas en
`remove_checker_background.py` (`CHECKER_BANDS = [(66,66,66), (104,104,104)]`, pensadas
para `ZombieViejo.png`). Por eso `remove_checker_background()` puede dar **0 pixeles
transparentes** sin avisar — no tira error, simplemente no encuentra nada que coincida con
esas bandas. Si eso pasa, conviene revisar a que tono de gris esta el cuadriculado
(muestrear pixeles de borde) y, si es mas parecido a blanco que al cuadriculado esperado,
resolverlo con `remove_white_background.py` subiendo la tolerancia. Este caso se resolvio
asi:

```bash
python -c "
from remove_white_background import remove_white_background
from PIL import Image
remove_white_background(Image.open('Arte/Referencias/ZombieFlesh.jpeg'), tolerance=65) \
    .save('zombie_hombre.png')
"
```

(equivalente a `python remove_white_background.py Arte/Referencias/ZombieFlesh.jpeg
zombie_hombre.png --tolerance 65`). Con tolerancia 45 quedaban tiras de cuadriculado
visibles; con 55-65 el fondo quedo limpio sin evidencia de comerse el dibujo.

## Tests

```bash
python -m pytest test_remove_checker_background.py -v
python -m pytest test_remove_white_background.py -v
```
