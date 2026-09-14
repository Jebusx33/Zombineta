import numpy as np
from PIL import Image

from remove_white_background import remove_white_background


def _figure_with_white_eye():
    # Fondo blanco, un cuadrado marron con un "ojo" blanco adentro.
    a = np.full((40, 40, 3), 255, np.uint8)
    a[10:30, 10:30] = (120, 70, 40)
    a[18:22, 18:22] = (255, 255, 255)
    return Image.fromarray(a, "RGB")


def test_the_white_around_the_figure_becomes_transparent():
    out = np.array(remove_white_background(_figure_with_white_eye()))
    assert out[0, 0, 3] == 0
    assert out[39, 39, 3] == 0
    assert out[5, 20, 3] == 0


def test_the_figure_stays_opaque():
    out = np.array(remove_white_background(_figure_with_white_eye()))
    assert out[12, 12, 3] == 255
    assert tuple(out[12, 12, :3]) == (120, 70, 40)


def test_a_white_detail_inside_the_figure_is_kept():
    out = np.array(remove_white_background(_figure_with_white_eye()))
    assert out[20, 20, 3] == 255


def test_near_white_paper_noise_is_removed_too():
    a = np.full((20, 20, 3), 250, np.uint8)
    a[0, 0] = (238, 240, 236)
    a[8:12, 8:12] = (0, 0, 0)
    out = np.array(remove_white_background(Image.fromarray(a, "RGB")))
    assert out[0, 0, 3] == 0
    assert out[10, 10, 3] == 255
