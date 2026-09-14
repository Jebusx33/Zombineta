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

## Tests

```bash
python -m pytest test_remove_checker_background.py -v
```
