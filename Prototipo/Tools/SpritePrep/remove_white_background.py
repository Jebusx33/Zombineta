"""Quita el fondo blanco de una hoja de sprites: solo el blanco conectado con los bordes.

Los blancos interiores (ojos, dientes, brillos) quedan, porque no tocan el borde de la imagen.
"""
import argparse

import numpy as np
from PIL import Image
from scipy import ndimage


def remove_white_background(img: Image.Image, tolerance: int = 24) -> Image.Image:
    rgba = np.array(img.convert("RGBA"))
    rgb = rgba[:, :, :3].astype(np.int16)
    near_white = np.all(rgb >= 255 - tolerance, axis=2)

    labels, _ = ndimage.label(near_white)
    border = np.unique(np.concatenate([labels[0, :], labels[-1, :], labels[:, 0], labels[:, -1]]))
    border = border[border != 0]
    background = np.isin(labels, border)

    rgba[background, 3] = 0
    return Image.fromarray(rgba, "RGBA")


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("src")
    p.add_argument("dst")
    p.add_argument("--tolerance", type=int, default=24)
    a = p.parse_args()
    remove_white_background(Image.open(a.src), a.tolerance).save(a.dst)


if __name__ == "__main__":
    main()
