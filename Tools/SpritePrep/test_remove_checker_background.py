import numpy as np
from PIL import Image

from remove_checker_background import remove_checker_background


def _make_checker(w, h, tile=10):
    arr = np.zeros((h, w, 3), dtype=np.uint8)
    for y in range(h):
        for x in range(w):
            band = ((x // tile) + (y // tile)) % 2
            arr[y, x] = (66, 66, 66) if band == 0 else (104, 104, 104)
    return arr


def test_border_checker_becomes_transparent():
    im = Image.fromarray(_make_checker(40, 40), mode="RGB")
    out = remove_checker_background(im)
    assert out.mode == "RGBA"
    assert out.getpixel((0, 0))[3] == 0
    assert out.getpixel((39, 39))[3] == 0


def test_solid_foreground_stays_opaque():
    arr = _make_checker(40, 40)
    arr[10:30, 10:30] = (200, 60, 60)  # cuadrado rojo solido, no es color de cuadricula
    im = Image.fromarray(arr, mode="RGB")
    out = remove_checker_background(im)
    assert out.getpixel((20, 20))[3] == 255


def test_enclosed_gray_island_is_preserved():
    arr = _make_checker(40, 40)
    arr[10:30, 10:30] = (200, 60, 60)
    arr[18:22, 18:22] = (66, 66, 66)  # gris de cuadricula, pero encerrado por el dibujo
    im = Image.fromarray(arr, mode="RGB")
    out = remove_checker_background(im)
    assert out.getpixel((20, 20))[3] == 255  # no conectado al borde -> no se toca
