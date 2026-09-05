# Animación del zombie viejo — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar el `Zombie.prefab` de greybox (cuadrado verde estático) por un
personaje animado (Walk / Hit / Death) usando el arte real `ZombieViejo.png`.

**Architecture:** Un script Python de un solo uso limpia el fondo cuadriculado horneado en
la hoja de arte (no tiene alfa real) generando una copia con transparencia real dentro del
proyecto de Unity. Desde ahí, todo el trabajo es scripting de Editor de Unity vía
`Unity_RunCommand` (MCP): slicing por grilla, 3 `AnimationClip` (sprite-swap), un
`AnimatorController` con 2 triggers, y la actualización del prefab.

**Tech Stack:** Python 3 (Pillow, NumPy, SciPy, pytest) para la limpieza de la hoja. C#
Editor scripting de Unity (`UnityEditor`, `UnityEditor.Animations`, `UnityEditor.U2D.Sprite`)
vía MCP para todo lo demás. Unity 6000.6.0f1, `com.unity.2d.sprite` 1.0.0 (ya instalado).

## Global Constraints

- El archivo original `Arte/Referencias/ZombieViejo.png` no se modifica ni se mueve.
- No se toca `HordeView.cs`, `RunSimulation`, `RunState` ni ningún dato de gameplay.
- Los triggers `Hit`/`Die` del Animator quedan sin conectar a eventos de gameplay reales
  (no existe hoy el concepto de "este zombie individual recibió un disparo").
- Grilla de la hoja: 2752×1536 px, 8 columnas × 3 filas, celda 344×512 px (división exacta).
- Pixels Per Unit de importación: **413** (calculado en el diseño: la figura parada mide
  454px de alto dentro de la celda; 454/413 ≈ 1.1 unidades, igual a la altura del placeholder
  actual).
- Pivot de cada sprite: **Bottom-Center** `(0.5, 0)` normalizado sobre la celda completa (no
  sobre el contenido recortado) — necesario para que el personaje no salte verticalmente
  entre poses de distinta altura (parado vs. tirado en el piso).
- Nombres de sub-sprites tras el slice: `ZombieViejo_r{fila}_c{columna}`, 0-indexado,
  fila 0 = fila superior de la hoja (caminata), fila 2 = fila inferior (muerte).
- Mapeo de clips (de la spec, ya corregido):
  - **Walk**: `r0_c0..r0_c7` (8 frames), 10 fps, loop.
  - **Hit**: `r1_c1, r1_c2` (2 frames), 8 fps, no-loop.
  - **Death**: `r1_c3, r1_c4, r1_c5, r2_c1, r2_c2, r2_c3, r2_c4, r2_c5, r2_c6, r2_c7`
    (10 frames), 10 fps, no-loop (se congela en el último frame).
  - Sin usar: `r1_c0`, `r2_c0` (poses de caminata redundantes), `r1_c6`, `r1_c7` (celdas
    vacías).

---

### Task 1: Script de limpieza de fondo (Python) + tests

**Files:**
- Create: `Tools/SpritePrep/remove_checker_background.py`
- Create: `Tools/SpritePrep/test_remove_checker_background.py`
- Create: `Tools/SpritePrep/README.md`

**Interfaces:**
- Produces: `remove_checker_background(cell: PIL.Image.Image) -> PIL.Image.Image` (RGBA,
  fondo cuadriculado conectado al borde vuelto transparente). `process_sheet(src_path: str,
  out_path: str, cols: int, rows: int) -> None` (aplica la función celda por celda sobre
  toda la hoja y guarda el resultado). Task 2 consume ambas.

- [ ] **Step 1: Preparar el entorno Python**

```bash
pip install pillow numpy scipy pytest
```

Expected: instala sin errores (son dependencias de una herramienta de un solo uso, no del
juego — no van a Packages/manifest.json).

- [ ] **Step 2: Escribir el test que falla**

Crear `Tools/SpritePrep/test_remove_checker_background.py`:

```python
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
```

- [ ] **Step 3: Correr el test y verificar que falla**

Run: `cd "Tools/SpritePrep" && python -m pytest test_remove_checker_background.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'remove_checker_background'`

- [ ] **Step 4: Implementar `remove_checker_background.py`**

```python
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
```

- [ ] **Step 5: Correr el test y verificar que pasa**

Run: `cd "Tools/SpritePrep" && python -m pytest test_remove_checker_background.py -v`
Expected: PASS — 3 passed

- [ ] **Step 6: Escribir el README**

Crear `Tools/SpritePrep/README.md`:

```markdown
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
```

- [ ] **Step 7: Commit**

```bash
git add Tools/SpritePrep/remove_checker_background.py Tools/SpritePrep/test_remove_checker_background.py Tools/SpritePrep/README.md
git commit -m "$(cat <<'EOF'
feat: add SpritePrep tool to strip baked checker backgrounds

Herramienta de preparacion de arte reutilizable: varias hojas del
equipo (ver HANDOFF) tienen fondo "transparente" horneado como grises
opacos en vez de alfa real. Usa flood-fill por componentes conexas
desde el borde para no comerse grises que esten dentro del dibujo.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Generar la hoja limpia de ZombieViejo dentro del proyecto de Unity

**Files:**
- Create (generado, no se edita a mano): `Assets/_Zombineta/Art/Zombies/ZombieViejo.png`

**Interfaces:**
- Consumes: `process_sheet` de Task 1 (`Tools/SpritePrep/remove_checker_background.py`).
- Produces: `Assets/_Zombineta/Art/Zombies/ZombieViejo.png` (RGBA, 2752×1536, mismo layout
  8×3), que Task 3 importa y slicea.

- [ ] **Step 1: Crear la carpeta de destino**

```bash
mkdir -p "Assets/_Zombineta/Art/Zombies"
```

- [ ] **Step 2: Correr el script sobre el archivo real**

```bash
python Tools/SpritePrep/remove_checker_background.py \
  "../../../Arte/Referencias/ZombieViejo.png" \
  "Assets/_Zombineta/Art/Zombies/ZombieViejo.png" \
  8 3
```

Expected: termina sin errores (puede tardar hasta ~1 minuto, la hoja es de 2752×1536).
Correr desde la raíz del repo (`Prototipo/Zombineta/Zombineta`).

- [ ] **Step 3: Verificar el resultado**

```bash
python - <<'EOF'
from PIL import Image
im = Image.open("Assets/_Zombineta/Art/Zombies/ZombieViejo.png")
assert im.mode == "RGBA", im.mode
assert im.size == (2752, 1536), im.size

# esquina superior izquierda: fondo, debe quedar transparente
assert im.getpixel((5, 5))[3] == 0

# celda r0c0 (caminata, parado): entre 65% y 80% de sus pixeles deben
# haberse vuelto transparentes (fondo). Referencia medida en el diseño: 71.9%.
cell = im.crop((0, 0, 344, 512))
alphas = [cell.getpixel((x, y))[3] for y in range(0, 512, 4) for x in range(0, 344, 4)]
transparent_pct = 100 * sum(1 for a in alphas if a == 0) / len(alphas)
assert 65 <= transparent_pct <= 80, transparent_pct

print("OK, transparent_pct =", transparent_pct)
EOF
```

Expected: imprime `OK, transparent_pct = <algo entre 65 y 80>` sin AssertionError. Si
falla el rango de porcentaje, ajustar `BAND_TOLERANCE` en
`Tools/SpritePrep/remove_checker_background.py` (subir si quedó fondo sin limpiar, bajar si
se comió partes del dibujo) y repetir desde el Step 2.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Zombineta/Art/Zombies/ZombieViejo.png
git commit -m "$(cat <<'EOF'
feat: add cleaned ZombieViejo sprite sheet with real alpha

Generado con Tools/SpritePrep a partir de Arte/Referencias/ZombieViejo.png,
que tiene el fondo "transparente" horneado como cuadriculado opaco en vez
de canal alfa real.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

Nota: `.gitattributes` ya trackea `*.png` con Git LFS (ver HANDOFF sección 8), así que este
commit sube un puntero LFS, no el binario directo — es el comportamiento esperado.

---

### Task 3: Importar y slicear la hoja en Unity

**Files:**
- Modify (vía Unity Editor, generado): `Assets/_Zombineta/Art/Zombies/ZombieViejo.png.meta`

**Interfaces:**
- Consumes: `Assets/_Zombineta/Art/Zombies/ZombieViejo.png` de Task 2.
- Produces: 24 sub-sprites nombrados `ZombieViejo_r{fila}_c{columna}` dentro de ese asset,
  cargables como `AssetDatabase.LoadAssetAtPath<Sprite>(path + "[nombre]")`. Task 4 los
  consume por nombre.

- [ ] **Step 1: Hacer que Unity note el archivo nuevo**

Llamar `Unity_ManageMenuItem` con `Action="Execute"`, `MenuPath="Assets/Refresh"`,
`Refresh=true`.
Expected: `success: true`. Esto es necesario porque `AssetDatabase.Refresh()` desde MCP no
alcanza (ver HANDOFF sección 5, trampa #3) — hace falta el menú.

- [ ] **Step 2: Configurar import settings y sliceo por grilla**

Llamar `Unity_RunCommand` con este código:

```csharp
using UnityEditor;
using UnityEditor.U2D.Sprite;
using UnityEngine;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    const string Path = "Assets/_Zombineta/Art/Zombies/ZombieViejo.png";
    const int Cols = 8;
    const int Rows = 3;
    const int CellW = 344;
    const int CellH = 512;
    const float Ppu = 413f;

    public void Execute(ExecutionResult result)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Path);
        if (importer == null)
        {
            result.LogError("No se encontro el TextureImporter en " + Path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = Ppu;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                float yFromBottom = (Rows - 1 - row) * CellH;
                rects.Add(new SpriteRect
                {
                    spriteID = GUID.Generate(),
                    name = "ZombieViejo_r" + row + "_c" + col,
                    rect = new Rect(col * CellW, yFromBottom, CellW, CellH),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                });
            }
        }
        dataProvider.SetSpriteRects(rects.ToArray());
        dataProvider.Apply();

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        result.Log("Sliced {0} sprites from ZombieViejo.png", rects.Count);
    }
}
```

- [ ] **Step 3: Verificar que no hay errores de compilación**

Llamar `Unity_ReadConsole` con `Action="Get"`, `Types=["All"]`, `FilterText="error CS"`.
Expected: `data` vacío (sin resultados). Este filtro exacto es por la trampa #1 del HANDOFF:
los errores de compilación llegan con tipo `Log`, no `Error`.

- [ ] **Step 4: Verificar el conteo y nombres de sprites**

Llamar `Unity_RunCommand` con:

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(
            "Assets/_Zombineta/Art/Zombies/ZombieViejo.png");
        result.Log("Sub-assets encontrados: {0}", reps.Length);
        foreach (var rep in reps)
            result.Log(rep.name);
    }
}
```

Expected: log `Sub-assets encontrados: 24`, seguido de 24 nombres
`ZombieViejo_r0_c0` .. `ZombieViejo_r2_c7`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Zombineta/Art/Zombies/ZombieViejo.png.meta
git commit -m "$(cat <<'EOF'
feat: slice ZombieViejo sheet into 24 grid sprites

Grid By Cell Count 8x3, PPU 413, pivot bottom-center por celda completa
(sin recorte por alfa) para que el personaje no salte verticalmente
entre poses de distinta altura.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Crear los 3 Animation Clips (Walk / Hit / Death)

**Files:**
- Create (vía Unity Editor): `Assets/_Zombineta/Art/Zombies/Animations/Zombie_Walk.anim`
- Create (vía Unity Editor): `Assets/_Zombineta/Art/Zombies/Animations/Zombie_Hit.anim`
- Create (vía Unity Editor): `Assets/_Zombineta/Art/Zombies/Animations/Zombie_Death.anim`

**Interfaces:**
- Consumes: sprites `ZombieViejo_r{fila}_c{col}` de Task 3.
- Produces: 3 `.anim` en la ruta de arriba, con curva `SpriteRenderer.m_Sprite`. Task 5 los
  referencia como `Motion` de los 3 estados del Animator Controller.

- [ ] **Step 1: Generar los 3 clips**

Llamar `Unity_RunCommand` con:

```csharp
using UnityEditor;
using UnityEngine;
using System.IO;

internal class CommandScript : IRunCommand
{
    const string Texture = "Assets/_Zombineta/Art/Zombies/ZombieViejo.png";
    const string OutDir = "Assets/_Zombineta/Art/Zombies/Animations";

    public void Execute(ExecutionResult result)
    {
        if (!Directory.Exists(OutDir))
            AssetDatabase.CreateFolder("Assets/_Zombineta/Art/Zombies", "Animations");

        BuildClip("Zombie_Walk", new[] { "r0_c0", "r0_c1", "r0_c2", "r0_c3", "r0_c4", "r0_c5", "r0_c6", "r0_c7" }, 10f, loop: true, result);

        BuildClip("Zombie_Hit", new[] { "r1_c1", "r1_c2" }, 8f, loop: false, result);

        BuildClip("Zombie_Death", new[]
        {
            "r1_c3", "r1_c4", "r1_c5",
            "r2_c1", "r2_c2", "r2_c3", "r2_c4", "r2_c5", "r2_c6", "r2_c7",
        }, 10f, loop: false, result);
    }

    void BuildClip(string clipName, string[] frameSuffixes, float fps, bool loop, ExecutionResult result)
    {
        var sprites = new Sprite[frameSuffixes.Length];
        for (int i = 0; i < frameSuffixes.Length; i++)
        {
            string spritePath = Texture + "[ZombieViejo_" + frameSuffixes[i] + "]";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprites[i] == null)
            {
                result.LogError("No se encontro el sprite " + spritePath);
                return;
            }
        }

        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string savePath = OutDir + "/" + clipName + ".anim";
        AssetDatabase.CreateAsset(clip, savePath);
        result.RegisterObjectCreation(clip);
        result.Log("Created {0} with {1} frames at {2} fps (loop={3})", clip, sprites.Length, fps, loop);
    }
}
```

- [ ] **Step 2: Verificar que no hay errores**

Llamar `Unity_ReadConsole` con `Action="Get"`, `Types=["All"]`, `FilterText="error CS"`.
Expected: sin resultados.

- [ ] **Step 3: Verificar los 3 clips**

Llamar `Unity_RunCommand` con:

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string[] paths =
        {
            "Assets/_Zombineta/Art/Zombies/Animations/Zombie_Walk.anim",
            "Assets/_Zombineta/Art/Zombies/Animations/Zombie_Hit.anim",
            "Assets/_Zombineta/Art/Zombies/Animations/Zombie_Death.anim",
        };
        foreach (var p in paths)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
            result.Log("{0}: length={1:0.00}s frameRate={2}", p, clip.length, clip.frameRate);
        }
    }
}
```

Expected: `Zombie_Walk` length ≈ 0.80s frameRate=10; `Zombie_Hit` length ≈ 0.25s
frameRate=8; `Zombie_Death` length ≈ 1.00s frameRate=10.

- [ ] **Step 4: Commit**

```bash
git add "Assets/_Zombineta/Art/Zombies/Animations"
git commit -m "$(cat <<'EOF'
feat: add Walk/Hit/Death animation clips for the old zombie

Sprite-swap sobre SpriteRenderer.m_Sprite. Walk en loop (8 frames,
10fps); Hit (2 frames, 8fps) y Death (10 frames, 10fps) sin loop, Death
se congela en el ultimo frame (cuerpo tirado).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Animator Controller (Walk / Hit / Death + triggers)

**Files:**
- Create (vía Unity Editor): `Assets/_Zombineta/Art/Zombies/ZombieAnimator.controller`

**Interfaces:**
- Consumes: los 3 `.anim` de Task 4.
- Produces: `Assets/_Zombineta/Art/Zombies/ZombieAnimator.controller` con parámetros Trigger
  `Hit` y `Die`. Task 6 lo asigna al componente `Animator` del prefab.

- [ ] **Step 1: Crear el controller**

Llamar `Unity_RunCommand` con:

```csharp
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    const string Dir = "Assets/_Zombineta/Art/Zombies";

    public void Execute(ExecutionResult result)
    {
        string controllerPath = Dir + "/ZombieAnimator.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var rootSM = controller.layers[0].stateMachine;

        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir + "/Animations/Zombie_Walk.anim");
        var hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir + "/Animations/Zombie_Hit.anim");
        var deathClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir + "/Animations/Zombie_Death.anim");

        var walkState = rootSM.AddState("Walk");
        walkState.motion = walkClip;
        var hitState = rootSM.AddState("Hit");
        hitState.motion = hitClip;
        var deathState = rootSM.AddState("Death");
        deathState.motion = deathClip;

        rootSM.defaultState = walkState;

        var walkToHit = walkState.AddTransition(hitState);
        walkToHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        walkToHit.hasExitTime = false;
        walkToHit.duration = 0f;

        var hitToWalk = hitState.AddTransition(walkState);
        hitToWalk.hasExitTime = true;
        hitToWalk.exitTime = 1f;
        hitToWalk.hasFixedDuration = true;
        hitToWalk.duration = 0f;

        var walkToDeath = walkState.AddTransition(deathState);
        walkToDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        walkToDeath.hasExitTime = false;
        walkToDeath.duration = 0f;

        var hitToDeath = hitState.AddTransition(deathState);
        hitToDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        hitToDeath.hasExitTime = false;
        hitToDeath.duration = 0f;

        EditorUtility.SetDirty(controller);
        result.RegisterObjectCreation(controller);
        result.Log("Created {0} with {1} states and {2} parameters",
            controller, rootSM.states.Length, controller.parameters.Length);
    }
}
```

- [ ] **Step 2: Verificar que no hay errores**

Llamar `Unity_ReadConsole` con `Action="Get"`, `Types=["All"]`, `FilterText="error CS"`.
Expected: sin resultados.

- [ ] **Step 3: Verificar estados y parámetros**

El log del Step 1 debe decir `3 states and 2 parameters`. Si no, revisar el código antes de
seguir.

- [ ] **Step 4: Commit**

```bash
git add "Assets/_Zombineta/Art/Zombies/ZombieAnimator.controller"
git commit -m "$(cat <<'EOF'
feat: add ZombieAnimator controller (Walk/Hit/Death)

Walk es el estado default y hace loop. Trigger Hit va a Hit y vuelve a
Walk por Exit Time. Trigger Die es terminal (desde Walk o Hit). Los
triggers no estan conectados a ningun evento de gameplay todavia.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Actualizar Zombie.prefab

**Files:**
- Modify: `Assets/_Zombineta/Prefabs/Zombie.prefab`

**Interfaces:**
- Consumes: `Assets/_Zombineta/Art/Zombies/ZombieAnimator.controller` de Task 5, sprite
  `ZombieViejo_r0_c0` de Task 3.

- [ ] **Step 1: Modificar el prefab**

Llamar `Unity_RunCommand` con:

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    const string PrefabPath = "Assets/_Zombineta/Prefabs/Zombie.prefab";

    public void Execute(ExecutionResult result)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);

        var sr = root.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Zombineta/Art/Zombies/ZombieViejo.png[ZombieViejo_r0_c0]");
        sr.color = Color.white;

        var animator = root.GetComponent<Animator>();
        if (animator == null)
            animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/_Zombineta/Art/Zombies/ZombieAnimator.controller");

        root.transform.localScale = Vector3.one;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        result.Log("Updated {0}", PrefabPath);
    }
}
```

- [ ] **Step 2: Verificar que no hay errores**

Llamar `Unity_ReadConsole` con `Action="Get"`, `Types=["All"]`, `FilterText="error CS"`.
Expected: sin resultados.

- [ ] **Step 3: Verificar el prefab actualizado**

Leer el archivo `Assets/_Zombineta/Prefabs/Zombie.prefab` (es texto YAML) y confirmar:
- Hay un bloque `Animator:` con `m_Controller` apuntando al guid de `ZombieAnimator.controller`.
- `SpriteRenderer.m_Color` es `{r: 1, g: 1, b: 1, a: 1}` (ya no el verde `0.45, 0.62, 0.36`).
- `Transform.m_LocalScale` es `{x: 1, y: 1, z: 1}` (ya no `{x: 0.5, y: 1.1, z: 1}`).

- [ ] **Step 4: Commit**

```bash
git add "Assets/_Zombineta/Prefabs/Zombie.prefab"
git commit -m "$(cat <<'EOF'
feat: wire ZombieAnimator into Zombie.prefab, drop greybox tint

Reemplaza el cuadrado gris teñido de verde por el sprite real
(ZombieViejo, primer frame de Walk) mas el Animator. Escala pasa de
0.5x1.1 (estirada a mano para el placeholder) a 1x1 porque el aspect
ratio correcto ya viene del PPU de importacion.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 7: Verificación visual en Play Mode

**Files:** ninguno (solo verificación).

**Interfaces:**
- Consumes: el prefab actualizado de Task 6, ya cableado en la escena `Prototipo.unity` vía
  `HordeView` (sin cambios en `HordeView.cs`).

- [ ] **Step 1: Entrar en Play Mode**

Llamar `Unity_RunCommand` con:

```csharp
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        EditorApplication.isPlaying = true;
        result.Log("Play mode requested");
    }
}
```

- [ ] **Step 2: Confirmar que no hay errores de consola al entrar en Play**

Llamar `Unity_ReadConsole` con `Action="Get"`, `Types=["All"]`, `FilterText="error"`.
Expected: sin resultados nuevos relacionados a `Zombie`, `Animator` o `SpriteRenderer`.

- [ ] **Step 3: Capturar la escena**

Llamar `Unity_SceneView_Capture2DScene` con coordenadas centradas en donde esté la horda en
ese momento (usar `worldX`/`worldY` cerca del origen de la escena, `worldWidth`/`worldHeight`
~20×10, `pixelsPerUnit` ~64) para ver varios zombies a la vez.

Expected (revisar la imagen): los zombies se ven con el arte real de `ZombieViejo` (piel,
overol azul, sangre), **sin fondo gris a cuadros alrededor**, y sin que el personaje
"flote" o se corte de forma rara verticalmente entre instancias.

- [ ] **Step 4: Salir de Play Mode**

Llamar `Unity_RunCommand` con:

```csharp
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        EditorApplication.isPlaying = false;
        result.Log("Play mode stopped");
    }
}
```

Si el Step 3 muestra fondo gris: volver a Task 3 y revisar `alphaIsTransparency` /
`BAND_TOLERANCE`. Si el personaje aparece cortado o con el pivot mal puesto: revisar el
`pivot` en Task 3, Step 2.

No hay commit en esta tarea — es solo verificación de lo ya commiteado en las tareas 1-6.
