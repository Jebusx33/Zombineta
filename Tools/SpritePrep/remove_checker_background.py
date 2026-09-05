"""Quita el fondo a cuadros horneado (sin alfa real) de una hoja de sprites,
sin tocar el contenido del dibujo. Ver README.md."""
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

CHECKER_BANDS = [(66, 66, 66), (104, 104, 104)]
NEUTRAL_TOLERANCE = 10
BAND_TOLERANCE = 20
_CROSS = np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]])


def _checker_like_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[..., 0].astype(int), rgb[..., 1].astype(int), rgb[..., 2].astype(int)
    neutral = (
        (np.abs(r - g) < NEUTRAL_TOLERANCE)
        & (np.abs(g - b) < NEUTRAL_TOLERANCE)
        & (np.abs(r - b) < NEUTRAL_TOLERANCE)
    )
    near_band = np.zeros(r.shape, dtype=bool)
    for br, bg, bb in CHECKER_BANDS:
        near_band |= (
            (np.abs(r - br) < BAND_TOLERANCE)
            & (np.abs(g - bg) < BAND_TOLERANCE)
            & (np.abs(b - bb) < BAND_TOLERANCE)
        )
    return neutral & near_band


def remove_checker_background(cell: Image.Image) -> Image.Image:
    """RGBA de `cell` con el cuadriculado de fondo transparentado.

    Solo se borra el fondo conectado al borde de la imagen (flood fill via
    componentes conexas), asi un gris que caiga dentro del dibujo (contorno,
    sombra) no se convierte en un agujero.
    """
    rgb = np.array(cell.convert("RGB"))
    h, w = rgb.shape[:2]
    mask = _checker_like_mask(rgb)

    labels, _ = ndimage.label(mask, structure=_CROSS)
    border_labels = set(labels[0, :]) | set(labels[-1, :])
    border_labels |= set(labels[:, 0]) | set(labels[:, -1])
    border_labels.discard(0)
    background = np.isin(labels, list(border_labels))

    rgba = np.dstack([rgb, np.full((h, w), 255, dtype=np.uint8)])
    rgba[background, 3] = 0
    return Image.fromarray(rgba, mode="RGBA")


def process_sheet(src_path: str, out_path: str, cols: int, rows: int) -> None:
    sheet = Image.open(src_path).convert("RGB")
    w, h = sheet.size
    if w % cols or h % rows:
        raise ValueError(f"{src_path}: {w}x{h} no divide exacto en {cols}x{rows}")
    cw, ch = w // cols, h // rows

    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for row in range(rows):
        for col in range(cols):
            box = (col * cw, row * ch, (col + 1) * cw, (row + 1) * ch)
            cleaned = remove_checker_background(sheet.crop(box))
            out.paste(cleaned, (box[0], box[1]))
    out.save(out_path)


if __name__ == "__main__":
    if len(sys.argv) != 5:
        print("uso: remove_checker_background.py <src.png> <out.png> <cols> <rows>")
        raise SystemExit(1)
    process_sheet(sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]))
