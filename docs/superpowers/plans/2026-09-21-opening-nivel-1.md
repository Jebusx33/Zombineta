# Opening del nivel 1 — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que la cinemática del nivel 1 arme una página de cómic sumando las nueve viñetas de
`scene_opening` una encima de otra, con fundido, en el 0,0 y a pantalla completa.

**Architecture:** `ComicPanel` suma `nuevaPagina`; una clase pura `ComicPages` decide qué viñeta
abre página (testeada). `CinematicPlayer` apila una `Image` por viñeta dentro del objeto `Vineta`
(que ya tiene el `CanvasGroup` de la página) y funde cada una por su color; al abrir página nueva
o al terminar, funde el grupo entero.

**Tech Stack:** Unity 6000.6, uGUI (`Image`, `CanvasGroup`), Input System, NUnit EditMode.

Spec: `docs/superpowers/specs/2026-09-21-opening-nivel-1-design.md`.

## Global Constraints

- Proyecto Unity: `Juego/` (nunca la raíz). Rama `Jose`.
- `Juego/Assets/_Zombineta/Simulacion` es compartida con `Prototipo/`: solo cambios aditivos.
- Nunca commitear `Juego/ProjectSettings/ProjectSettings.asset`.
- Material: solo `Arte/Bocetos/scene_opening/art_opn_page_2k_frame_1.png` … `frame_9.png`; se
  ignoran `old/` y `[position_references].png`.
- Destino de las imágenes: `Juego/Assets/_Zombineta/Art/Cinematicas/Nivel01/`, con el mismo nombre
  de archivo (el reemplazo del arte final es pisar el PNG).
- Importación: Sprite (Single), sin mipmaps, `maxTextureSize` 2048, `alphaIsTransparency`.
- Fundido por viñeta 0,35 s (`fadeSeconds` actual). Título 2 s como hoy.
- `nuevaPagina` default `true`; la primera viñeta siempre abre página.
- Verificación en Play: registrar el callback antes de entrar en Play, loguear a `SessionState`,
  capturar con render de cámara a RenderTexture. No enfocar ventanas ni leer píxeles de pantalla.
- Commits terminan con `Co-Authored-By:` del modelo que los escribió.

## Modelos por tarea

| Tarea | Implementa | Revisa |
|---|---|---|
| 1. Datos y regla de páginas | haiku | sonnet |
| 2. Reproductor, imágenes y verificación | sonnet | sonnet |

---

### Task 1: `nuevaPagina` y `ComicPages`

**Files:**
- Modify: `Juego/Assets/_Zombineta/Simulacion/Runtime/Flow/LevelSequence.cs` (clase `ComicPanel`)
- Create: `Juego/Assets/_Zombineta/Simulacion/Runtime/Flow/ComicPages.cs`
- Test: `Juego/Assets/_Zombineta/Simulacion/Tests/Editor/ComicPagesTests.cs`

**Interfaces:**
- Produces: `ComicPanel.nuevaPagina` (`bool`, default `true`);
  `static bool ComicPages.OpensPage(IReadOnlyList<ComicPanel> panels, int index)`.

- [ ] **Step 1: Write the failing test** — `ComicPagesTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Flow;

namespace Zombineta.Tests
{
    public class ComicPagesTests
    {
        static List<ComicPanel> Panels(params bool[] nuevaPagina)
        {
            var list = new List<ComicPanel>();
            foreach (var n in nuevaPagina)
                list.Add(new ComicPanel { nuevaPagina = n });
            return list;
        }

        [Test]
        public void PorDefectoCadaVinetaAbrePagina()
        {
            Assert.IsTrue(new ComicPanel().nuevaPagina);
        }

        [Test]
        public void LaPrimeraSiempreAbrePagina()
        {
            Assert.IsTrue(ComicPages.OpensPage(Panels(false, false), 0));
        }

        [Test]
        public void UnaPaginaAcumulada()
        {
            var p = Panels(true, false, false);
            Assert.IsTrue(ComicPages.OpensPage(p, 0));
            Assert.IsFalse(ComicPages.OpensPage(p, 1));
            Assert.IsFalse(ComicPages.OpensPage(p, 2));
        }

        [Test]
        public void DosPaginasSeguidas()
        {
            var p = Panels(true, false, true, false);
            Assert.IsFalse(ComicPages.OpensPage(p, 1));
            Assert.IsTrue(ComicPages.OpensPage(p, 2));
            Assert.IsFalse(ComicPages.OpensPage(p, 3));
        }

        [Test]
        public void FueraDeRangoNoAbre()
        {
            Assert.IsFalse(ComicPages.OpensPage(Panels(true), 5));
            Assert.IsFalse(ComicPages.OpensPage(null, 0));
        }
    }
}
```

Check the namespace used by the other tests in that folder (e.g. `GameFlowTests.cs`) and use the
same one.

- [ ] **Step 2: Run it and see it fail** — compile error: `nuevaPagina` / `ComicPages` don't exist.
  Run the EditMode suite with `TestRunnerApi` (filter `ComicPagesTests`) from a `Unity_RunCommand`.

- [ ] **Step 3: Implement.** In `ComicPanel`, after `seconds`:

```csharp
        [Tooltip("Si esta prendido, la pagina anterior se va y esta vineta arranca una limpia. " +
                 "Apagado, se suma encima de las que ya estan. La primera siempre abre pagina.")]
        public bool nuevaPagina = true;
```

`ComicPages.cs`:

```csharp
using System.Collections.Generic;

namespace Zombineta.Flow
{
    /// <summary>La regla de paginas de la cinematica: que vineta arranca una pagina limpia.</summary>
    public static class ComicPages
    {
        public static bool OpensPage(IReadOnlyList<ComicPanel> panels, int index)
        {
            if (panels == null || index < 0 || index >= panels.Count)
                return false;
            return index == 0 || panels[index] == null || panels[index].nuevaPagina;
        }
    }
}
```

- [ ] **Step 4: Run the full EditMode suite** — expected: all green (237 + 5).

- [ ] **Step 5: Commit** — `git add` the three files (and the new `.meta`), message
  `Cinematica: nuevaPagina por vineta y la regla de paginas`.

---

### Task 2: Reproductor apilado, imágenes del nivel 1 y verificación

**Files:**
- Modify: `Juego/Assets/_Zombineta/Scripts/Screens/CinematicPlayer.cs`
- Create: `Juego/Assets/_Zombineta/Art/Cinematicas/Nivel01/art_opn_page_2k_frame_{1..9}.png`
- Modify: `Juego/Assets/_Zombineta/Settings/Niveles.asset` (comicPanels del nivel 1)
- Modify: `HANDOFF.md` (cómo reemplazar el arte final)

**Interfaces:**
- Consumes: `ComicPanel.nuevaPagina`, `ComicPages.OpensPage(panels, index)` (Task 1).

- [ ] **Step 1: Replace the panel loop in `CinematicPlayer`.** The scene's `Vineta` object has the
  `Image` (`panel`) and the `CanvasGroup` (`panelGroup`). Keep both fields. `panel` becomes the
  template: it is disabled, and each viñeta of the page is an `Image` child of `panel.transform`,
  stretched to fill it, copying `preserveAspect`. Replace the `if (level != null) { foreach ... }`
  block in `Start` and add the helpers:

```csharp
            if (level != null)
                yield return PlayPanels(level.comicPanels);

            Finish();
        }

        readonly List<Image> pageImages = new List<Image>();
        int pageCount;

        IEnumerator PlayPanels(List<ComicPanel> panels)
        {
            if (panel == null)
                yield break;
            panel.enabled = false;

            for (int i = 0; i < panels.Count && !skip; i++)
            {
                if (ComicPages.OpensPage(panels, i))
                {
                    if (i > 0)
                        yield return Fade(panelGroup, 0f);
                    ClearPage();
                    SetAlpha(panelGroup, 1f);
                }

                advance = false;
                var img = NextImage(panels[i].image);
                yield return FadeImage(img, 1f);
                advance = false; // un Submit durante el fundido solo lo completa
                for (float t = 0f; t < panels[i].seconds && !advance && !skip; t += Time.unscaledDeltaTime)
                    yield return null;
            }

            yield return Fade(panelGroup, 0f);
        }

        Image NextImage(Sprite sprite)
        {
            Image img;
            if (pageCount < pageImages.Count)
                img = pageImages[pageCount];
            else
            {
                var go = new GameObject("Vineta " + (pageCount + 1), typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(panel.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                img = go.GetComponent<Image>();
                img.preserveAspect = panel.preserveAspect;
                img.raycastTarget = false;
                pageImages.Add(img);
            }
            pageCount++;
            img.transform.SetAsLastSibling();
            img.sprite = sprite;
            img.gameObject.SetActive(true);
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        void ClearPage()
        {
            foreach (var img in pageImages)
                img.gameObject.SetActive(false);
            pageCount = 0;
        }

        IEnumerator FadeImage(Image img, float target)
        {
            var c = img.color;
            float start = c.a;
            for (float t = 0f; t < fadeSeconds && !skip && !advance; t += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Lerp(start, target, t / fadeSeconds);
                img.color = c;
                yield return null;
            }
            c.a = target;
            img.color = c;
        }
```

  Add `using System.Collections.Generic;`. `FadeImage` stops on `advance`, so Submit during a fade
  completes it at once; the second `advance = false` makes that press not also skip the wait.
  Update the class summary:
  "las vinetas de comic, que se suman en la pagina (ver ComicPanel.nuevaPagina)". Remove the now
  unused `Show` only if nothing else uses it (the title still uses `Show`: keep it).

- [ ] **Step 2: Copy and import the images.** From
  `D:\Jose\Facu\Taller de proyecto integral\Arte\Bocetos\scene_opening\` copy only
  `art_opn_page_2k_frame_1.png` … `frame_9.png` to `Juego/Assets/_Zombineta/Art/Cinematicas/Nivel01/`.
  After `AssetDatabase.Refresh()`, set each `TextureImporter`: `textureType = Sprite`,
  `spriteImportMode = Single`, `mipmapEnabled = false`, `maxTextureSize = 2048`,
  `alphaIsTransparency = true`, then `SaveAndReimport()`.

- [ ] **Step 3: Load the nine panels into level 1.** With a `Unity_RunCommand`: load
  `Assets/_Zombineta/Settings/Niveles.asset` (`LevelSequence`), `levels[0].comicPanels` = nine
  `ComicPanel` in order frame_1 … frame_9, `seconds = 2.5f`, `nuevaPagina = true` only for
  frame_1, `false` for the rest. `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets()`. Level 2
  is untouched (its panels deserialize with `nuevaPagina = true`: one at a time, as today).

- [ ] **Step 4: Compile clean and run the full EditMode suite** — all green.

- [ ] **Step 5: Verify in Play from the menu** (`Boot.unity`, then `GameRoot.Flow.Play()`,
  `ChooseCharacter(0)`, with ~120 frames between steps). While `Current == Cinematic`, render the
  main camera to a 1920×1080 RenderTexture at: first viñeta visible, fifth, and the complete page
  (after the ninth fades in). Compare the last one against `[position_references].png` (same
  layout). Check: pieces never move between steps; Submit (`advance`) jumps to the next one; Cancel
  skips to the level; a second run of level 2 still shows its three panels one at a time.

- [ ] **Step 6: HANDOFF.** Short note under the flow/cinematic section: where the opening lives,
  that the final art replaces each PNG in `Art/Cinematicas/Nivel01/` with the same name (the
  `.meta` keeps the references), and what `nuevaPagina` does.

- [ ] **Step 7: Commit** — the script, the nine PNGs with their `.meta`, the folder `.meta`s,
  `Niveles.asset` and `HANDOFF.md`. Message: `Opening del nivel 1: la pagina se arma vineta por vineta`.
