# Estética nueva: escala y encuadre — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que los niveles de `Juego/` se vean con la escala y la composición de
`Arte/Bocetos/Concept Art/Mapa_Escala.png`: carriles finos y abajo, mucho fondo, personajes al
doble, orden por carril con sombras y zombies con arte por arquetipo.

**Architecture:** La lógica nueva que se puede testear es C# plano (`LaneSorting`,
`ZombieLookPicker`, `Flipbook`) o Python (`remove_white_background`). Lo visual son vistas finas
(`GroundShadow`, cambios en `HordeView`/`ScooterView`/`LevelScene`) y datos en assets. El
encuadre se calza con una guía de referencia de editor y valores en `GameConfig`,
`CameraConfig`, `Escenario`, `LevelItemPalette` y `ZombieLooks`.

**Tech Stack:** Unity 6000.6.0f1, URP 2D, uGUI, NUnit (EditMode), Python 3 + Pillow/numpy/scipy +
pytest, MCP de Unity.

Spec: `docs/superpowers/specs/2026-09-14-estetica-escala-design.md`.

**Nota:** como en los planes anteriores, el código de vistas y herramientas de editor se escribe
directo en los archivos; el plan fija archivos, interfaces, el código de la lógica testeable y
las verificaciones. Los números de encuadre se ajustan mirando la guía, no se inventan.

## Global Constraints

- Unity abierto en `Juego/`. **No tocar `Prototipo/`** ni la lógica de
  `Juego/Assets/_Zombineta/Simulacion/Runtime` (el prototipo la comparte). Assets de datos de
  `Juego/` (`GameConfig.asset`, `CameraConfig.asset`, `Zombies.asset`, `Escenario.asset`) sí.
- Ancho visible de cámara en plano normal: ~30 m (el actual). Zoom de tensión ±10 % del normal,
  anclado al piso.
- Arquetipos: Común = anciano, oficinista, vagabundo. Corredor = mujer, adolescente. Pesado =
  hombre rojo. Índices de tipo en `Zombies.asset`: 0 Común, 1 Corredor, 2 Pesado.
- Estáticos reiniciados en `SubsystemRegistration` (recarga de dominio desactivada en `Juego/`).
- Pruebas en Play por MCP: registrar el callback **antes** de entrar en Play (trampa #27).
- Commits con `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`. Push/merge solo cuando el
  usuario lo pida.

Rutas abreviadas: `Z/` = `Juego/Assets/_Zombineta/`.

---

### Task 1: Guía de referencia

**Files:**
- Create: `Z/Editor/Framing/FramingGuide.cs`

**Interfaces:**
- Produces: menús `Zombineta/Encuadre/Mostrar referencia`, `Ocultar referencia`,
  `Elegir imagen…`, `Opacidad +`, `Opacidad -`; `static bool FramingGuide.Visible`;
  `static void FramingGuide.Show(string imagePath, float opacity)`; `static void FramingGuide.Hide()`.
  EditorPrefs: `zombineta.framing.visible` (bool), `zombineta.framing.path` (string),
  `zombineta.framing.opacity` (float, default 0.4).

- [ ] **Step 1: Implementar `FramingGuide`**

  - `[InitializeOnLoad]`; se suscribe a `EditorSceneManager.activeSceneChangedInEditMode`,
    `SceneManager.activeSceneChanged` y `EditorApplication.playModeStateChanged`; en cada uno, si
    `Visible`, llama a `Ensure()`.
  - `Ensure()`: busca un GameObject `__GuiaEncuadre` (con `HideFlags.DontSave`); si no existe lo
    crea con `Canvas` (`ScreenSpaceOverlay`, `sortingOrder = 2000`), `CanvasScaler` y un hijo con
    `RawImage` estirado (anchors 0..1, offsets 0), `raycastTarget = false`, color
    `(1,1,1,opacity)`. Tag `EditorOnly`. En Play, `DontDestroyOnLoad`.
  - La textura se carga desde disco con `File.ReadAllBytes` + `Texture2D.LoadImage` (la imagen
    está fuera de `Assets/`), con `hideFlags = DontSave`, cacheada por ruta.
  - Default de ruta: `Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../../Arte/Bocetos/Concept Art/Mapa_Escala.png"))`;
    si no existe, `Elegir imagen…` usa `EditorUtility.OpenFilePanel`.
  - `Hide()` destruye `__GuiaEncuadre` y pone `Visible = false`.

- [ ] **Step 2: Verificar por MCP**

  Ejecutar `Zombineta/Encuadre/Mostrar referencia` con `Level_01` abierto, entrar en Play con el
  callback registrado antes, y a los 2 s renderizar la cámara **y** leer la pantalla del Game
  view con `InternalEditorUtility.ReadScreenPixel` (trampa #28): la captura debe mostrar el nivel
  con la referencia encima. Salir de Play: la guía sigue. `Ocultar referencia`: desaparece. La
  escena no queda marcada como modificada (`scene.isDirty == false`).

- [ ] **Step 3: Commit**

```bash
git add Juego/Assets/_Zombineta/Editor/Framing
git commit -m "Guia de referencia de encuadre sobre el Game view"
```

---

### Task 2: Orden por carril y sombras

**Files:**
- Create: `Z/Scripts/Gameplay/Core/LaneSorting.cs`
- Create: `Z/Scripts/Gameplay/Fx/GroundShadow.cs`
- Create: `Z/Art/Fx/sombra.png` (elipse suave 128×48, generada por script de editor o Python)
- Modify: `Z/Scripts/Gameplay/Player/ScooterView.cs` (orden + sombra permanente)
- Modify: `Z/Scripts/Gameplay/Enemies/HordeView.cs` (orden + sombra por zombie)
- Modify: `Z/Scripts/Levels/LevelScene.cs` (orden y sombra de los items)
- Modify: `Z/Scripts/Gameplay/Fx/FxManager.cs` (orden de los efectos por carril)
- Test: `Z/Tests/Editor/LaneSortingTests.cs`

**Interfaces:**
- Produces:
  - `enum SortSlot { Shadow = 0, Item = 10, Zombie = 20, Player = 30, Effect = 40 }`
  - `static int LaneSorting.Order(float visualLane, SortSlot slot)`
  - `const int LaneSorting.Base = 1000`, `const int LaneSorting.PerLane = 100`
  - `GroundShadow`: `[SerializeField] SpriteRenderer shadow; float width = 1f;`
    `void Place(float worldX, float groundY, float heightWorld, float visualLane)`.

- [ ] **Step 1: Test que falla**

```csharp
using NUnit.Framework;
using Zombineta.Core;

namespace Zombineta.Juego.Tests
{
    public class LaneSortingTests
    {
        [Test]
        public void TheLowerLane_IsDrawnInFront()
        {
            Assert.Greater(LaneSorting.Order(0f, SortSlot.Zombie), LaneSorting.Order(1f, SortSlot.Zombie));
            Assert.Greater(LaneSorting.Order(1f, SortSlot.Item), LaneSorting.Order(2f, SortSlot.Player));
        }

        [Test]
        public void ChangingLane_MovesTheOrderGradually()
        {
            int from = LaneSorting.Order(2f, SortSlot.Player);
            int mid = LaneSorting.Order(1.5f, SortSlot.Player);
            int to = LaneSorting.Order(1f, SortSlot.Player);
            Assert.Greater(mid, from);
            Assert.Less(mid, to);
        }

        [Test]
        public void InsideALane_ShadowItemZombiePlayerEffect()
        {
            Assert.Less(LaneSorting.Order(1f, SortSlot.Shadow), LaneSorting.Order(1f, SortSlot.Item));
            Assert.Less(LaneSorting.Order(1f, SortSlot.Item), LaneSorting.Order(1f, SortSlot.Zombie));
            Assert.Less(LaneSorting.Order(1f, SortSlot.Zombie), LaneSorting.Order(1f, SortSlot.Player));
            Assert.Less(LaneSorting.Order(1f, SortSlot.Player), LaneSorting.Order(1f, SortSlot.Effect));
        }

        [Test]
        public void AShadowInTheLaneBelow_IsInFrontOfAnEffectInTheLaneAbove()
        {
            // Un carril entero pesa mas que cualquier slot: la escena se lee de abajo hacia arriba.
            Assert.Greater(LaneSorting.Order(0f, SortSlot.Shadow), LaneSorting.Order(1f, SortSlot.Effect));
        }

        [Test]
        public void EverythingStaysBetweenTheStreetAndTheForeground()
        {
            // Calle en -20 y primer plano en 40 son ordenes de capas de escenario en OTRA sorting
            // layer de valores bajos; el juego vive en su propio rango positivo.
            Assert.Greater(LaneSorting.Order(2f, SortSlot.Shadow), 0);
            Assert.Less(LaneSorting.Order(-0.5f, SortSlot.Effect), 32767);
        }
    }
}
```

- [ ] **Step 2: Correr y ver que falla** (Test Runner EditMode por MCP, filtro
  `LaneSortingTests`). Esperado: error de compilación, `LaneSorting` no existe.

- [ ] **Step 3: Implementar**

```csharp
using UnityEngine;

namespace Zombineta.Core
{
    public enum SortSlot { Shadow = 0, Item = 10, Zombie = 20, Player = 30, Effect = 40 }

    /// <summary>
    /// Orden de dibujo por carril: los personajes miden mas de dos carriles de alto, asi que lo
    /// del carril de abajo tiene que tapar a lo de arriba. El carril es continuo (el visual de un
    /// cambio de carril) para que el orden no salte a mitad de camino.
    /// </summary>
    public static class LaneSorting
    {
        public const int Base = 1000;
        public const int PerLane = 100;

        public static int Order(float visualLane, SortSlot slot) =>
            Base - Mathf.RoundToInt(visualLane * PerLane) + (int)slot;
    }
}
```

  Nota de rango: con `Base = 1000`, todo el juego queda entre ~790 y ~1090. Las capas de
  escenario que deben ir **encima** (primer plano, `sortingOrder` hoy 40/50) pasan a
  `2000` y `2010` en `Escenario.asset` (Task 6); calle y fondo quedan negativos.

- [ ] **Step 4: Correr: PASS** (5 tests) y el total sigue en verde.

- [ ] **Step 5: `GroundShadow` y aplicarlo**

  - `GroundShadow.Place`: posición `(worldX, groundY)`, rotación identidad, escala
    `baseScale * width * Lerp(1, 0.55, Clamp01(heightWorld / 4))`, alfa
    `baseAlpha * Lerp(1, 0.6, mismo k)`, `sortingOrder = LaneSorting.Order(visualLane, Shadow)`.
  - `ScooterView`: `body.sortingOrder = Order(LaneVisual, Player)`; la sombra se muestra siempre
    (no solo en el aire) usando `GroundShadow`; el color "aterrizaje perfecto" se conserva solo
    mientras vuela.
  - `HordeView`: cada cuerpo tiene un `GroundShadow` hijo creado en `Start` (sprite
    `sombra.png`); `sprites[i].sortingOrder = Order(u.Lane, Zombie)`; cadáver en el piso:
    `Order(u.Lane, Item)`.
  - `LevelScene`: en `ApplyVisual` y en juego, `sr.sortingOrder = Order(item.lane, Item)` (la
    rampa `Order(lane, Shadow) + 1`: se pisa) y una sombra hija `GroundShadow` con `width` de la
    paleta (nuevo campo `LevelItemPalette.Look.shadowWidth`, default 1).
  - `FxManager`: cada efecto que nace en un carril usa `Order(lane, Effect)`; las manchas de
    sangre `Order(lane, Shadow) + 1`.

- [ ] **Step 6: Verificar por MCP en Play** (`Level_01`, callback antes de Play): moto en carril
  0 y un zombie en carril 1 a la misma X → `moto.sortingOrder > zombie.sortingOrder`; durante un
  cambio de carril de 2 a 0 el orden de la moto sube de forma monótona (muestrear cada frame);
  una sombra activa bajo la moto con Y = piso del carril. Consola sin errores. Tests en verde.

- [ ] **Step 7: Commit**

```bash
git add Juego/Assets/_Zombineta/Scripts Juego/Assets/_Zombineta/Tests/Editor/LaneSortingTests.cs* Juego/Assets/_Zombineta/Art/Fx/sombra.png*
git commit -m "Orden de dibujo por carril y sombras en el piso"
```

---

### Task 3: Quitar fondo blanco (SpritePrep)

**Files:**
- Create: `Prototipo/Tools/SpritePrep/remove_white_background.py`
- Create: `Prototipo/Tools/SpritePrep/test_remove_white_background.py`
- Modify: `Prototipo/Tools/SpritePrep/README.md`

(`Tools/` es una herramienta de disco, no parte del proyecto Unity del prototipo: no rompe la
restricción de no tocar `Prototipo/`.)

**Interfaces:**
- Produces: `remove_white_background(img: Image.Image, tolerance: int = 24) -> Image.Image`
  (RGBA); CLI `python remove_white_background.py <entrada> <salida> [--tolerance N]`.

- [ ] **Step 1: Test que falla**

```python
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
```

- [ ] **Step 2: Correr y ver que falla**

Run: `cd Prototipo/Tools/SpritePrep && python -m pytest test_remove_white_background.py -v`
Expected: FAIL, `ModuleNotFoundError: remove_white_background`.

- [ ] **Step 3: Implementar**

```python
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
```

- [ ] **Step 4: Correr: PASS** (4 tests) y los tests del script de cuadriculado siguen pasando.

- [ ] **Step 5: README** — agregar la sección "Fondo blanco" con el comando y la aclaración de
  blancos interiores.

- [ ] **Step 6: Procesar las fuentes** (fuera del repo del juego, a una carpeta de trabajo):
  - `Arte/Assets/Zombies_poses.png` → `Z/Art/Zombies/Source/zombies_poses.png`
  - `Arte/Referencias/ZombieFlesh.jpeg` con `remove_checker_background.py` →
    `Z/Art/Zombies/Source/zombie_hombre.png` (primero con el script de cuadriculado; si el
    resultado tiene restos, pasar además `remove_white_background`).
  - Mirar las dos salidas (leer las imágenes) antes de seguir: fondo transparente, figuras
    enteras.

- [ ] **Step 7: Commit**

```bash
git add Prototipo/Tools/SpritePrep Juego/Assets/_Zombineta/Art/Zombies/Source
git commit -m "SpritePrep: fondo blanco; hojas de zombies sin fondo"
```

---

### Task 4: Recortador de hojas por transparencia

**Files:**
- Create: `Z/Scripts/Gameplay/Art/AlphaIslands.cs` (C# plano en el runtime para poder testearlo)
- Create: `Z/Editor/Art/SheetSlicer.cs`
- Test: `Z/Tests/Editor/AlphaIslandsTests.cs`

**Interfaces:**
- Produces:
  - `struct PixelRect { int x, y, width, height; }`
  - `static List<PixelRect> AlphaIslands.Find(bool[] opaque, int width, int height, int minArea, int mergeGap)`
    — componentes conectados (8 vecinos) de píxeles opacos; une cajas separadas por menos de
    `mergeGap` px (un brazo suelto); descarta áreas menores a `minArea`; ordena por fila (centro Y
    agrupado con tolerancia de media altura mediana, de arriba hacia abajo) y dentro de la fila
    por X. `y` en coordenadas de textura de Unity (0 abajo).
  - Menú `Assets/Zombineta/Recortar por transparencia` sobre una textura seleccionada: importer
    `Sprite (Multiple)`, `isReadable` temporal, escribe un `SpriteRect` por isla con nombre
    `<hoja>_<fila>_<columna>` y pivot `(0.5, 0)`; informa en consola cuántas islas por fila.

- [ ] **Step 1: Tests que fallan**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Art;

namespace Zombineta.Juego.Tests
{
    public class AlphaIslandsTests
    {
        static bool[] Canvas(int w, int h, params (int x, int y, int w, int h)[] boxes)
        {
            var a = new bool[w * h];
            foreach (var b in boxes)
                for (int y = b.y; y < b.y + b.h; y++)
                    for (int x = b.x; x < b.x + b.w; x++)
                        a[y * w + x] = true;
            return a;
        }

        [Test]
        public void TwoFigures_AreTwoIslands_LeftToRight()
        {
            var r = AlphaIslands.Find(Canvas(100, 50, (60, 5, 20, 40), (10, 5, 20, 40)), 100, 50, 10, 0);
            Assert.AreEqual(2, r.Count);
            Assert.AreEqual(10, r[0].x);
            Assert.AreEqual(60, r[1].x);
        }

        [Test]
        public void RowsGoTopToBottom()
        {
            // En textura de Unity, y alto = arriba.
            var r = AlphaIslands.Find(Canvas(60, 100, (5, 5, 20, 30), (5, 60, 20, 30)), 60, 100, 10, 0);
            Assert.AreEqual(60, r[0].y);
            Assert.AreEqual(5, r[1].y);
        }

        [Test]
        public void ALooseHand_IsMergedWithItsBody()
        {
            var r = AlphaIslands.Find(Canvas(100, 60, (10, 5, 20, 40), (33, 30, 4, 4)), 100, 60, 10, 5);
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(27, r[0].width);
        }

        [Test]
        public void Specks_AreIgnored()
        {
            var r = AlphaIslands.Find(Canvas(80, 50, (10, 5, 20, 40), (70, 45, 2, 2)), 80, 50, 10, 0);
            Assert.AreEqual(1, r.Count);
        }
    }
}
```

- [ ] **Step 2: Correr y ver que falla** (compilación: `AlphaIslands` no existe).

- [ ] **Step 3: Implementar `AlphaIslands.Find`** (BFS con cola sobre índices, cajas por
  componente, unión repetida de cajas cuya distancia entre bordes sea `< mergeGap` hasta que no
  cambie, filtro por área, agrupación en filas por centro Y con tolerancia = mitad de la altura
  mediana, orden). Namespace `Zombineta.Art`.

- [ ] **Step 4: Correr: PASS** (4 tests).

- [ ] **Step 5: `SheetSlicer`** (editor) y recortar:
  - `zombies_poses.png`: 6 filas × 4 poses (oficinista, anciano, mujer, hombre, adolescente,
    vagabundo; poses: pie, impacto, pie 2, piso). Verificar 24 sprites; si una fila da otra
    cantidad, ajustar `mergeGap` y repetir.
  - `zombie_hombre.png`: filas caminata (8), impacto (5), muerte (8) = 21.
  - `ZombieViejo.png` ya está recortada: no se toca.
  - Leer una captura de la Scene view o del importer para confirmar los recortes.

- [ ] **Step 6: Commit**

```bash
git add Juego/Assets/_Zombineta/Scripts/Gameplay/Art Juego/Assets/_Zombineta/Editor/Art Juego/Assets/_Zombineta/Tests/Editor/AlphaIslandsTests.cs* Juego/Assets/_Zombineta/Art/Zombies
git commit -m "Recortador de hojas por transparencia; hojas de zombies recortadas"
```

---

### Task 5: Looks de zombies y animación por código

**Files:**
- Create: `Z/Scripts/Gameplay/Enemies/ZombieLookSet.cs` (ScriptableObject + `ZombieLook`)
- Create: `Z/Scripts/Gameplay/Enemies/ZombieLookPicker.cs` (C# plano)
- Create: `Z/Scripts/Gameplay/Enemies/Flipbook.cs` (C# plano)
- Create: `Z/Settings/ZombieLooks.asset`
- Modify: `Z/Scripts/Gameplay/Enemies/HordeView.cs` (flipbook en vez de Animator)
- Modify: `Z/Scripts/Levels/LevelScene.cs` (zombie de frente con look)
- Test: `Z/Tests/Editor/ZombieLookPickerTests.cs`, `Z/Tests/Editor/FlipbookTests.cs`

**Interfaces:**
- Consumes: `LaneSorting.Order`, `GroundShadow` (Task 2); sprites de Task 4.
- Produces:
  - `[Serializable] class ZombieLook { string name; Sprite[] walk; Sprite[] hit; Sprite[] death; float walkFps = 8f; float scale = 1f; bool poseBob; }`
  - `class ZombieLookSet : ScriptableObject { List<TypeLooks> types; }` con
    `[Serializable] class TypeLooks { string typeName; List<ZombieLook> looks; }` y
    `IReadOnlyList<ZombieLook> For(int typeIndex)` (lista vacía si no hay).
  - `static int ZombieLookPicker.Pick(int seed, int unit, int generation, int lookCount, int previous)`
    → índice en `[0, lookCount)`, o `-1` si `lookCount == 0`; distinto de `previous` cuando
    `lookCount > 1`.
  - `enum FlipbookClip { Walk, Hit, Death }`
  - `struct Flipbook` con `void Play(FlipbookClip clip)`, `void Tick(float dt, float speedRatio)`,
    `int Frame`, `FlipbookClip Clip`, `bool Finished`; construido con
    `Flipbook(int walkFrames, int hitFrames, int deathFrames, float walkFps, float hitSeconds = 0.2f, float deathFps = 10f)`.
    Walk hace loop y usa `speedRatio`; Hit dura `hitSeconds` y vuelve a Walk; Death no hace loop
    y queda en el último cuadro con `Finished = true`.

- [ ] **Step 1: Tests que fallan**

```csharp
using NUnit.Framework;
using Zombineta.Enemies;

namespace Zombineta.Juego.Tests
{
    public class ZombieLookPickerTests
    {
        [Test]
        public void TheSameInput_GivesTheSameLook()
        {
            Assert.AreEqual(ZombieLookPicker.Pick(7, 3, 2, 3, -1), ZombieLookPicker.Pick(7, 3, 2, 3, -1));
        }

        [Test]
        public void TheLookIsAlwaysInRange()
        {
            for (int u = 0; u < 50; u++)
                for (int g = 0; g < 10; g++)
                {
                    int i = ZombieLookPicker.Pick(1, u, g, 3, -1);
                    Assert.GreaterOrEqual(i, 0);
                    Assert.Less(i, 3);
                }
        }

        [Test]
        public void ItAvoidsRepeatingThePreviousLook()
        {
            for (int u = 0; u < 50; u++)
                Assert.AreNotEqual(1, ZombieLookPicker.Pick(1, u, 0, 2, 1));
        }

        [Test]
        public void WithOneLook_ItIsThatOne_AndWithNoneMinusOne()
        {
            Assert.AreEqual(0, ZombieLookPicker.Pick(1, 5, 5, 1, 0));
            Assert.AreEqual(-1, ZombieLookPicker.Pick(1, 5, 5, 0, -1));
        }

        [Test]
        public void AllLooksGetUsed()
        {
            var seen = new bool[3];
            for (int u = 0; u < 60; u++)
                seen[ZombieLookPicker.Pick(4, u, 0, 3, -1)] = true;
            CollectionAssert.AreEqual(new[] { true, true, true }, seen);
        }
    }

    public class FlipbookTests
    {
        [Test]
        public void WalkLoops_FasterWithSpeed()
        {
            var slow = new Flipbook(8, 1, 4, 8f);
            var fast = new Flipbook(8, 1, 4, 8f);
            for (int i = 0; i < 10; i++) { slow.Tick(0.05f, 1f); fast.Tick(0.05f, 2f); }
            Assert.AreEqual(4, slow.Frame);   // 0,5 s × 8 fps
            Assert.AreEqual(0, fast.Frame);   // 8 cuadros: dio la vuelta justo
            Assert.AreEqual(FlipbookClip.Walk, fast.Clip);
        }

        [Test]
        public void Hit_ReturnsToWalk()
        {
            var f = new Flipbook(8, 2, 4, 8f, 0.2f);
            f.Play(FlipbookClip.Hit);
            f.Tick(0.1f, 1f);
            Assert.AreEqual(FlipbookClip.Hit, f.Clip);
            f.Tick(0.15f, 1f);
            Assert.AreEqual(FlipbookClip.Walk, f.Clip);
        }

        [Test]
        public void Death_StopsOnTheLastFrame()
        {
            var f = new Flipbook(8, 1, 4, 8f, 0.2f, 10f);
            f.Play(FlipbookClip.Death);
            for (int i = 0; i < 20; i++) f.Tick(0.1f, 1f);
            Assert.AreEqual(FlipbookClip.Death, f.Clip);
            Assert.AreEqual(3, f.Frame);
            Assert.IsTrue(f.Finished);
        }

        [Test]
        public void ClipsWithoutFrames_NeverGoOutOfRange()
        {
            var f = new Flipbook(2, 0, 0, 8f);
            f.Play(FlipbookClip.Hit);
            f.Tick(0.05f, 1f);
            Assert.AreEqual(0, f.Frame);
            f.Play(FlipbookClip.Death);
            f.Tick(1f, 1f);
            Assert.AreEqual(0, f.Frame);
        }
    }
}
```

- [ ] **Step 2: Correr y ver que falla** (compilación).

- [ ] **Step 3: Implementar**

```csharp
namespace Zombineta.Enemies
{
    /// <summary>Que aspecto toma un zombie al reciclarse. Deterministico y sin repetir el anterior.</summary>
    public static class ZombieLookPicker
    {
        public static int Pick(int seed, int unit, int generation, int lookCount, int previous)
        {
            if (lookCount <= 0)
                return -1;
            if (lookCount == 1)
                return 0;

            uint h = 2166136261;
            unchecked
            {
                h = (h ^ (uint)seed) * 16777619;
                h = (h ^ (uint)unit) * 16777619;
                h = (h ^ (uint)generation) * 16777619;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
            }

            if (previous < 0 || previous >= lookCount)
                return (int)(h % (uint)lookCount);

            // Elige entre los demas: nunca el anterior.
            int i = (int)(h % (uint)(lookCount - 1));
            return i >= previous ? i + 1 : i;
        }
    }
}
```

```csharp
using UnityEngine;

namespace Zombineta.Enemies
{
    public enum FlipbookClip { Walk, Hit, Death }

    /// <summary>
    /// Cuadro de animacion de un zombie sin Animator: caminata en loop a la velocidad del zombie,
    /// impacto corto que vuelve a caminar, muerte que queda en el ultimo cuadro.
    /// </summary>
    public struct Flipbook
    {
        readonly int walkFrames, hitFrames, deathFrames;
        readonly float walkFps, hitSeconds, deathFps;
        float time;

        public FlipbookClip Clip { get; private set; }
        public int Frame { get; private set; }
        public bool Finished { get; private set; }

        public Flipbook(int walkFrames, int hitFrames, int deathFrames, float walkFps,
                        float hitSeconds = 0.2f, float deathFps = 10f)
        {
            this.walkFrames = walkFrames;
            this.hitFrames = hitFrames;
            this.deathFrames = deathFrames;
            this.walkFps = walkFps;
            this.hitSeconds = hitSeconds;
            this.deathFps = deathFps;
            time = 0f;
            Clip = FlipbookClip.Walk;
            Frame = 0;
            Finished = false;
        }

        public void Play(FlipbookClip clip)
        {
            Clip = clip;
            time = 0f;
            Frame = 0;
            Finished = false;
        }

        public void Tick(float dt, float speedRatio)
        {
            switch (Clip)
            {
                case FlipbookClip.Walk:
                    time += dt * Mathf.Max(0f, speedRatio);
                    Frame = walkFrames > 0 ? Mathf.FloorToInt(time * walkFps + 1e-4f) % walkFrames : 0;
                    break;

                case FlipbookClip.Hit:
                    time += dt;
                    if (time >= hitSeconds)
                    {
                        Play(FlipbookClip.Walk);
                        return;
                    }
                    Frame = hitFrames > 0 ? Mathf.Min(hitFrames - 1, Mathf.FloorToInt(time / hitSeconds * hitFrames)) : 0;
                    break;

                case FlipbookClip.Death:
                    time += dt;
                    int f = Mathf.FloorToInt(time * deathFps + 1e-4f);
                    int last = Mathf.Max(0, deathFrames - 1);
                    Frame = Mathf.Min(f, last);
                    Finished = f >= last;
                    break;
            }
        }
    }
}
```

- [ ] **Step 4: Correr: PASS** (9 tests).

- [ ] **Step 5: `ZombieLookSet`, `ZombieLooks.asset` y vistas**
  - Asset por MCP con los sprites de Task 4 (cargar sprites justo antes de asignar, trampa #23):
    - Común (0): anciano (`ZombieViejo` hoja completa: caminata/impacto/muerte de sus clips
      actuales), oficinista y vagabundo (poses: `walk = [pie, pie 2]`, `hit = [impacto]`,
      `death = [piso]`, `poseBob = true`, `walkFps = 3`).
    - Corredor (1): mujer y adolescente (poses, `poseBob = true`, `walkFps = 4`).
    - Pesado (2): hombre rojo (`zombie_hombre` 8/5/8, `walkFps = 10`).
  - `HordeView`: campo `ZombieLookSet looks` y `int lookSeed`; por unidad guarda look y
    `Flipbook`; al reciclar elige look con `Pick(lookSeed, i, generation, count, previousLook[i])`;
    `Hit` al subir `Stagger`, `Death` al morir; `speedRatio = FrontSpeed / hordeBaseSpeed`; si
    `poseBob`, balanceo `sin(t × 9) × 0,04` u en Y y `± 3°`; si el tipo no tiene looks, cae en el
    `Animator` del prefab como hoy. Tinte del tipo pasa a blanco cuando hay look (el arte ya
    diferencia).
  - `LevelScene`: el `ZombieFront` usa el primer cuadro de `walk` del look
    `Pick(lookSeed, índice del item, 0, count, -1)` del tipo `variant`, mirando hacia la moto;
    en Play, `HordeView`/la vista de items no animan al zombie de frente (se queda en pose, igual
    que hoy).

- [ ] **Step 6: Verificar por MCP en Play**: los 24 zombies con los looks de su tipo (listar
  nombre de look por tipo: Común solo anciano/oficinista/vagabundo, etc.); un disparo que no
  mata muestra `Hit` y vuelve a `Walk`; uno que mata termina en el último cuadro de `Death`;
  captura del Game view. Tests en verde.

- [ ] **Step 7: Commit**

```bash
git add Juego/Assets/_Zombineta/Scripts Juego/Assets/_Zombineta/Settings/ZombieLooks.asset* Juego/Assets/_Zombineta/Tests/Editor/ZombieLookPickerTests.cs* Juego/Assets/_Zombineta/Tests/Editor/FlipbookTests.cs* Juego/Assets/_Zombineta/Scenes
git commit -m "Zombies por arquetipo con animacion por codigo"
```

---

### Task 6: Encuadre, tamaños y escenario

**Files:**
- Modify (datos): `Z/Settings/GameConfig.asset`, `Z/Settings/CameraConfig.asset`,
  `Z/Settings/Escenario.asset`, `Z/Settings/LevelItemPalette.asset`, `Z/Prefabs/Fx*.prefab`,
  `Z/Prefabs/Zombie.prefab`
- Modify (escenas): `Z/Scenes/Templates/NivelBase.unity`, `Z/Scenes/Level_01.unity`,
  `Z/Scenes/Level_02.unity` (escala de `Scooter`, `HordeView` base, faro)
- Create: `Z/Art/Tileset/tile_placeholder_{arbol_1,arbol_2,farola,cartel,cartel_piso,senalizacion,bocacalle,tienda}.png`
  (los que falten, copiados de `Arte/Bocetos/Tileset` con `.meta` generado por Unity)
- Create: `Z/Editor/Framing/FramingCapture.cs` (menú `Zombineta/Encuadre/Capturar comparacion`)

**Interfaces:**
- Consumes: `FramingGuide` (Task 1), `LaneSorting` (Task 2), looks (Task 5).
- Produces: `FramingCapture.Capture(string outPath, float guideOpacity)` → PNG del render de la
  cámara con la referencia mezclada a `guideOpacity` (para comparar sin la ventana del editor).

- [ ] **Step 1: `FramingCapture`**: renderiza `Camera.main` a 1920×1080, mezcla la textura de la
  guía (misma carga que `FramingGuide`) con alfa `guideOpacity`, escribe PNG. Solo editor.

- [ ] **Step 2: Carriles y cámara** (por MCP, con capturas de comparación tras cada cambio):
  1. `laneSpacing` 1,6 → 1,05. Captura: las líneas de carril sobre las de la referencia.
     Ajustar en pasos de 0,05 hasta que coincidan (±5 px). `LaneMarkersView` lee la separación
     de `GameConfig`: confirmar en la captura que sus líneas se movieron; si tiene un espaciado
     propio serializado, igualarlo.
  2. `viewBottomY` hasta que la línea del carril 0 caiga sobre la de la referencia (~85 %).
  3. Tamaño normal = el actual; `tightSize` = normal × 0,9; `wideSize` = normal × 1,1;
     `catchSize` y `victorySize` con la proporción actual respecto de 6,3 (0,571 y 1,19).
  4. Verificar anclaje: capturas con la horda a 6 m y a 50 m; la línea del carril 0 queda en la
     misma Y de pantalla (±3 px).

- [ ] **Step 3: Tamaños**
  1. Escala de `Scooter` hasta que la moto mida lo de la bici de la referencia (~220 px).
  2. `HordeView` escala base (nuevo campo `bodyScale`, multiplicado por `type.scale × look.scale`)
     hasta que un Común mida ~220 px.
  3. `jumpHeightToWorld` × el mismo factor que la moto.
  4. `LevelItemPalette`: escalas × el factor; `shadowWidth` por tipo.
  5. Prefabs de efectos: `startSize` y radio de emisión × factor; `FxMancha` escala × factor.
  6. Faro: posición local del `Light2D` en la moto y alcance × factor; `WheelDustView` offset.
  Capturas: moto y zombies contra la referencia.

- [ ] **Step 4: Escenario** (`Escenario.asset`, capas en orden de la tabla del spec):
  - Cielo parallax 0; Edificios parallax 0,6, `height` hasta el ~10 % superior, `baselineY` en
    el cordón de enfrente; Árboles parallax 0,9 (nueva capa, `arbol_1/2`, `gapChance` 0,4);
    Cordón de enfrente y Vereda de abajo (nuevas, franja de `pista`); Calle a la nueva
    separación; Primer plano parallax 1,6, `sortingOrder` 2010 (farola, cartel, cartel_piso,
    senalizacion, `gapChance` 0,6); Frontal existente → 2000.
  - `Vias`, `Vallas`, `Postes`: `enabled = false`.
  - Captura de comparación: línea de la calle a ~60 %, fondo ocupando el 60 % de arriba, primer
    plano pasando por delante.

- [ ] **Step 5: Niveles**: abrir `Level_01` y `Level_02`, dejar que `LevelScene` reacomode los
  items, guardar. Validar por MCP que todos los items estén en `LaneY(lane) + height × jump`
  (±0,001) y que `LevelValidator` no reporte problemas nuevos.

- [ ] **Step 6: Verificación completa por MCP**
  - Capturas mezcladas en plano normal, cerrado (horda a 6 m), atrapada y victoria; leerlas.
  - Recorrido menú → personaje → cinemática → `Level_01` → pausa → seguir: escenas esperadas.
  - Consola sin errores; todos los tests en verde (166 + 5 + 4 + 9 = 184).
  - `Prototipo/`: `git diff --stat master -- Prototipo/Assets Prototipo/Packages Prototipo/ProjectSettings`
    vacío.

- [ ] **Step 7: Commit**

```bash
git add Juego/Assets/_Zombineta
git commit -m "Encuadre, tamaños y escenario segun Mapa_Escala"
```

---

### Task 7: Documentación

**Files:**
- Modify: `HANDOFF.md`

- [ ] **Step 1**: sección nueva "Estética y escala (`Juego/`)": la guía de referencia y la
  captura de comparación, dónde vive cada número (carriles, cámara, tamaños, escenario), el orden
  por carril y sus slots, las sombras, `ZombieLooks` (cómo sumar un arquetipo: preparar hoja,
  recortar, agregar look), SpritePrep de fondo blanco. Tabla de sub-proyectos: 4 **Hecho**, 5
  próximo. Trampas nuevas si aparecieron. Estado verificado con las capturas.

- [ ] **Step 2: Commit**

```bash
git add HANDOFF.md
git commit -m "docs: handoff con la estetica nueva"
```
