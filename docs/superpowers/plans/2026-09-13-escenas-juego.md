# Escenas del juego definitivo — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** El flujo completo del juego corre en escenas separadas (`Boot` persistente, pantallas,
niveles de prueba y capas de pausa y opciones), con fundidos y Play desde cualquier escena.

**Architecture:** `GameFlow` (compartido) decide la pantalla; `SceneRoutePlanner` (C# plano) la
traduce en escenas a cargar y descargar; `GameRoot` (MonoBehaviour en `Boot`) ejecuta los planes.
Las escenas se generan con una herramienta de editor idempotente.

**Tech Stack:** Unity 6000.6.0f1, URP 2D, uGUI, Input System, NUnit, MCP de Unity.

Spec: `docs/superpowers/specs/2026-09-13-escenas-juego-design.md`.

## Global Constraints

- Proyecto: `Juego/`. Unity abierto en `Juego/` para compilar y probar (la simulación compartida
  se testea ahí).
- Cambios en la simulación compartida **solo agregan**: los 125 tests existentes siguen en verde.
- La recarga de dominio está desactivada: todo estático se reinicia en
  `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- Comentarios en castellano sin tildes. Commits con
  `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`.
- Compilar con `Assets/Refresh` por MCP y revisar `error CS` con `Types: ["All"]`.

---

### Task 1: El flujo suma pausa, opciones con retorno e intento

**Files:**
- Modify: `Juego/Assets/_Zombineta/Simulacion/Runtime/Flow/GameFlow.cs` (reemplazo completo)
- Modify: `Juego/Assets/_Zombineta/Simulacion/Runtime/Flow/LevelSequence.cs`
- Modify: `Juego/Assets/_Zombineta/Simulacion/Runtime/Core/GameSettings.cs`
- Modify: `Juego/Assets/_Zombineta/Simulacion/Tests/Editor/GameFlowTests.cs`

- [ ] **Step 1: Tests nuevos** — agregar al final de `GameFlowTests`:

```csharp
        // --- Pausa y opciones con retorno -------------------------------------------

        [Test]
        public void PauseAndResume_OnlyInsideALevel()
        {
            var f = new GameFlow(2);
            Assert.IsFalse(f.Pause(), "en el menu no hay nada que pausar");

            f = InLevel(levels: 2, level: 0);
            Assert.IsTrue(f.Pause());
            Assert.AreEqual(GameScreen.Paused, f.Current);
            Assert.IsTrue(f.Resume());
            Assert.AreEqual(GameScreen.Playing, f.Current);
        }

        [Test]
        public void OptionsOpenedFromPause_GoBackToPause()
        {
            var f = InLevel(levels: 2, level: 0);
            f.Pause();

            Assert.IsTrue(f.OpenOptions());
            Assert.AreEqual(GameScreen.Options, f.Current);
            Assert.IsTrue(f.Back());
            Assert.AreEqual(GameScreen.Paused, f.Current);
        }

        [Test]
        public void OptionsOpenedFromTheMenu_StillGoBackToTheMenu_AfterAPause()
        {
            var f = InLevel(levels: 2, level: 0);
            f.Pause();
            f.OpenOptions();
            f.Back();
            f.ToMainMenu();

            f.OpenOptions();
            f.Back();
            Assert.AreEqual(GameScreen.MainMenu, f.Current, "el retorno no queda pegado en la pausa");
        }

        [Test]
        public void Attempt_GrowsWhenALevelStartsFresh_NotWhenResuming()
        {
            var f = InLevel(levels: 2, level: 0);
            int a = f.Attempt;

            f.Pause();
            f.Resume();
            Assert.AreEqual(a, f.Attempt, "seguir no es empezar de nuevo");

            f.Pause();
            Assert.IsTrue(f.Retry(), "desde la pausa se puede reintentar");
            Assert.AreEqual(GameScreen.Playing, f.Current);
            Assert.AreEqual(a + 1, f.Attempt);

            f.LevelLost();
            f.Retry();
            Assert.AreEqual(a + 2, f.Attempt);
        }

        [Test]
        public void FromPause_YouCanQuitToTheMenu()
        {
            var f = InLevel(levels: 2, level: 0);
            f.Pause();

            Assert.IsTrue(f.ToMainMenu());
            Assert.AreEqual(GameScreen.MainMenu, f.Current);
        }

        [Test]
        public void JumpTo_PlacesTheFlowWithoutRaisingChanged()
        {
            var f = new GameFlow(3);
            int changes = 0;
            f.Changed += (a, b) => changes++;

            f.JumpTo(GameScreen.Playing, 2);

            Assert.AreEqual(GameScreen.Playing, f.Current);
            Assert.AreEqual(2, f.LevelIndex);
            Assert.AreEqual(0, changes);
            Assert.IsTrue(f.Pause(), "desde ahi el flujo sigue normal");
        }
```

- [ ] **Step 2:** Compilar y ver que falla (`Pause`, `Resume`, `Attempt`, `JumpTo` no existen).

- [ ] **Step 3: `GameFlow.cs` completo:**

```csharp
using System;

namespace Zombineta.Flow
{
    /// <summary>Las pantallas del juego, segun el diagrama de flujo del GDD (mas la pausa).</summary>
    public enum GameScreen
    {
        MainMenu,
        Options,
        CharacterSelect,
        Cinematic,
        Playing,
        LevelComplete,
        GameOver,
        Ending,
        Paused,
    }

    /// <summary>
    /// El flujo de pantallas como maquina de estados. C# plano: no sabe de escenas, ni de
    /// teclas, ni de Unity. La vista le avisa que paso ("apretaron Jugar", "se gano el
    /// nivel") y el flujo decide a donde se va.
    ///
    ///   Menu <-> Opciones <-> Pausa
    ///   Menu -> Personaje -> Cinematica -> Nivel <-> Pausa
    ///   Nivel gano   -> Nivel completo -> Cinematica del siguiente (o Final si era el ultimo)
    ///   Nivel perdio -> Game Over -> Reintentar (mismo nivel) o Menu
    ///
    /// Una accion que no corresponde a la pantalla actual se ignora y devuelve false:
    /// un ENTER que llega tarde no puede saltear pantallas.
    /// </summary>
    public sealed class GameFlow
    {
        readonly int levelCount;
        GameScreen optionsReturn = GameScreen.MainMenu;

        public GameFlow(int levelCount)
        {
            if (levelCount < 1)
                throw new ArgumentException("El juego necesita al menos un nivel", nameof(levelCount));
            this.levelCount = levelCount;
        }

        public GameScreen Current { get; private set; } = GameScreen.MainMenu;

        /// <summary>Nivel en curso (o por empezar), base 0.</summary>
        public int LevelIndex { get; private set; }

        public int CharacterIndex { get; private set; }

        /// <summary>
        /// Sube cada vez que un nivel arranca de cero: al terminar la cinematica o al
        /// reintentar. Volver de la pausa no lo cambia, asi quien carga escenas sabe si tiene
        /// que recargar el nivel o solo sacar la pausa.
        /// </summary>
        public int Attempt { get; private set; }

        public int LevelCount => levelCount;

        public bool IsLastLevel => LevelIndex >= levelCount - 1;

        /// <summary>Se dispara en cada cambio de pantalla: (desde, hacia).</summary>
        public event Action<GameScreen, GameScreen> Changed;

        // --- Menu principal --------------------------------------------------

        public bool Play() => Go(GameScreen.MainMenu, GameScreen.CharacterSelect);

        /// <summary>Opciones se abre desde el menu o desde la pausa, y vuelve a donde se abrio.</summary>
        public bool OpenOptions()
        {
            if (Current != GameScreen.MainMenu && Current != GameScreen.Paused)
                return false;
            optionsReturn = Current;
            return Switch(GameScreen.Options);
        }

        /// <summary>Volver: desde Opciones a donde se abrio; desde el personaje, al menu.</summary>
        public bool Back()
        {
            if (Current == GameScreen.Options)
                return Switch(optionsReturn);
            if (Current == GameScreen.CharacterSelect)
                return Switch(GameScreen.MainMenu);
            return false;
        }

        // --- Partida ---------------------------------------------------------

        /// <summary>Elegir personaje arranca siempre desde el primer nivel.</summary>
        public bool ChooseCharacter(int index)
        {
            if (Current != GameScreen.CharacterSelect || index < 0)
                return false;

            CharacterIndex = index;
            LevelIndex = 0;
            return Switch(GameScreen.Cinematic);
        }

        public bool CinematicFinished()
        {
            if (Current != GameScreen.Cinematic)
                return false;
            Attempt++;
            return Switch(GameScreen.Playing);
        }

        public bool LevelWon() => Go(GameScreen.Playing, GameScreen.LevelComplete);

        public bool LevelLost() => Go(GameScreen.Playing, GameScreen.GameOver);

        public bool Pause() => Go(GameScreen.Playing, GameScreen.Paused);

        public bool Resume() => Go(GameScreen.Paused, GameScreen.Playing);

        /// <summary>Despues de ganar: la cinematica del siguiente nivel, o el final.</summary>
        public bool Continue()
        {
            if (Current != GameScreen.LevelComplete)
                return false;

            if (IsLastLevel)
                return Switch(GameScreen.Ending);

            LevelIndex++;
            return Switch(GameScreen.Cinematic);
        }

        /// <summary>
        /// Reintentar arranca el nivel de cero, desde el Game Over o desde la pausa, sin
        /// repetir la cinematica: despues de morir nadie quiere volver a ver la introduccion.
        /// </summary>
        public bool Retry()
        {
            if (Current != GameScreen.GameOver && Current != GameScreen.Paused)
                return false;
            Attempt++;
            return Switch(GameScreen.Playing);
        }

        public bool ToMainMenu()
        {
            if (Current == GameScreen.GameOver || Current == GameScreen.Ending || Current == GameScreen.Paused)
                return Switch(GameScreen.MainMenu);
            return false;
        }

        /// <summary>
        /// Pone el flujo en una pantalla sin pasar por las anteriores y sin avisar. Solo para
        /// arrancar en Play desde cualquier escena en el editor: esa escena ya esta cargada.
        /// </summary>
        public void JumpTo(GameScreen screen, int levelIndex = 0)
        {
            LevelIndex = Math.Max(0, Math.Min(levelIndex, levelCount - 1));
            if (screen == GameScreen.Playing)
                Attempt++;
            optionsReturn = GameScreen.MainMenu;
            Current = screen;
        }

        // --- Interno ---------------------------------------------------------

        bool Go(GameScreen from, GameScreen to) => Current == from && Switch(to);

        bool Switch(GameScreen to)
        {
            var from = Current;
            Current = to;
            Changed?.Invoke(from, to);
            return true;
        }
    }
}
```

- [ ] **Step 4: `LevelSequence.cs`** — agregar la clase `ComicPanel` antes de `LevelInfo` y dos
  campos al final de `LevelInfo`:

```csharp
    /// <summary>Una vineta de la cinematica: la imagen y cuanto dura en pantalla.</summary>
    [Serializable]
    public sealed class ComicPanel
    {
        public Sprite image;

        [Min(0.5f)]
        [Tooltip("Segundos en pantalla si nadie la pasa antes.")]
        public float seconds = 3f;
    }
```

```csharp
        [Tooltip("Escena del nivel en el juego definitivo. Tiene que estar en Build Settings.")]
        public string sceneName = "";

        [Tooltip("Vinetas de la cinematica de entrada, en orden. El titulo es displayName.")]
        public List<ComicPanel> comicPanels = new List<ComicPanel>();
```

- [ ] **Step 5: `GameSettings.cs`** — agregar al final de la clase:

```csharp
        const string VolumeKey = "zombineta.volume";
        static float? volume;

        /// <summary>Volumen general, de 0 a 1.</summary>
        public static float Volume
        {
            get => volume ??= PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set
            {
                volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, volume.Value);
                PlayerPrefs.Save();
            }
        }

        const string FullscreenKey = "zombineta.fullscreen";
        static bool? fullscreen;

        public static bool Fullscreen
        {
            get => fullscreen ??= PlayerPrefs.GetInt(FullscreenKey, 1) != 0;
            set
            {
                fullscreen = value;
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
```

- [ ] **Step 6:** Compilar y correr la suite. Expected: `pass=131`.
- [ ] **Step 7:** Commit `feat: el flujo suma pausa, opciones con retorno e intento`.

---

### Task 2: Planificador de escenas

**Files:**
- Create: `Juego/Assets/_Zombineta/Scripts/Zombineta.Juego.asmdef`
- Create: `Juego/Assets/_Zombineta/Scripts/Flow/SceneRoutePlanner.cs`
- Create: `Juego/Assets/_Zombineta/Tests/Editor/Zombineta.Juego.Tests.asmdef`
- Create: `Juego/Assets/_Zombineta/Tests/Editor/SceneRoutePlannerTests.cs`

- [ ] **Step 1: asmdefs.** `Zombineta.Juego.asmdef`:

```json
{
    "name": "Zombineta.Juego",
    "rootNamespace": "Zombineta.Juego",
    "references": [
        "Zombineta.Simulacion",
        "Unity.InputSystem",
        "UnityEngine.UI"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Zombineta.Juego.Tests.asmdef`:

```json
{
    "name": "Zombineta.Juego.Tests",
    "rootNamespace": "Zombineta.Juego.Tests",
    "references": [
        "Zombineta.Juego",
        "Zombineta.Simulacion",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Tests que fallan** — `SceneRoutePlannerTests.cs`:

```csharp
using NUnit.Framework;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Tests
{
    public class SceneRoutePlannerTests
    {
        const string L1 = "Level_01";
        const string L2 = "Level_02";

        static SceneRoutePlanner At(string baseScene, params string[] overlays)
        {
            var p = new SceneRoutePlanner();
            p.Start(baseScene, overlays);
            return p;
        }

        [Test]
        public void FromNothing_TheMenuLoadsWithFade()
        {
            var plan = At(null).Go(GameScreen.MainMenu, null, false);

            CollectionAssert.AreEqual(new[] { SceneNames.MainMenu }, plan.Load);
            CollectionAssert.IsEmpty(plan.Unload);
            Assert.AreEqual(SceneNames.MainMenu, plan.Active);
            Assert.IsTrue(plan.Fade);
        }

        [Test]
        public void ChangingScreen_SwapsTheBaseWithFade()
        {
            var plan = At(SceneNames.MainMenu).Go(GameScreen.CharacterSelect, null, false);

            CollectionAssert.AreEqual(new[] { SceneNames.MainMenu }, plan.Unload);
            CollectionAssert.AreEqual(new[] { SceneNames.CharacterSelect }, plan.Load);
            Assert.IsTrue(plan.Fade);
        }

        [Test]
        public void OptionsFromTheMenu_IsAnOverlay_AndBackOnlyRemovesIt()
        {
            var p = At(SceneNames.MainMenu);

            var open = p.Go(GameScreen.Options, null, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Options }, open.Load);
            CollectionAssert.IsEmpty(open.Unload);
            Assert.IsFalse(open.Fade);

            var back = p.Go(GameScreen.MainMenu, null, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Options }, back.Unload);
            CollectionAssert.IsEmpty(back.Load);
            Assert.IsFalse(back.Fade, "el menu no se recarga");
        }

        [Test]
        public void TheCinematicLeadsToTheLevelScene()
        {
            var plan = At(SceneNames.Cinematic).Go(GameScreen.Playing, L1, true);

            CollectionAssert.AreEqual(new[] { SceneNames.Cinematic }, plan.Unload);
            CollectionAssert.AreEqual(new[] { L1 }, plan.Load);
            Assert.AreEqual(L1, plan.Active);
        }

        [Test]
        public void PauseIsAnOverlay_AndResumeDoesNotReloadTheLevel()
        {
            var p = At(L1);

            var pause = p.Go(GameScreen.Paused, L1, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Pause }, pause.Load);
            CollectionAssert.IsEmpty(pause.Unload);
            Assert.AreEqual(L1, pause.Active, "el nivel sigue siendo la escena activa");

            var resume = p.Go(GameScreen.Playing, L1, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Pause }, resume.Unload);
            CollectionAssert.IsEmpty(resume.Load);
            Assert.IsFalse(resume.Fade);
        }

        [Test]
        public void OptionsFromPause_StackOnTop_AndBackLeavesThePause()
        {
            var p = At(L1, SceneNames.Pause);

            var open = p.Go(GameScreen.Options, L1, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Options }, open.Load);
            CollectionAssert.IsEmpty(open.Unload);
            CollectionAssert.AreEqual(new[] { SceneNames.Pause, SceneNames.Options }, p.Overlays);

            var back = p.Go(GameScreen.Paused, L1, false);
            CollectionAssert.AreEqual(new[] { SceneNames.Options }, back.Unload);
            CollectionAssert.IsEmpty(back.Load);
            Assert.AreEqual(SceneNames.Pause, p.TopScene);
        }

        [Test]
        public void RetryFromPause_ReloadsTheLevel()
        {
            var plan = At(L1, SceneNames.Pause).Go(GameScreen.Playing, L1, true);

            CollectionAssert.AreEqual(new[] { SceneNames.Pause, L1 }, plan.Unload);
            CollectionAssert.AreEqual(new[] { L1 }, plan.Load);
            Assert.IsTrue(plan.Fade);
        }

        [Test]
        public void QuittingFromPause_UnloadsTheOverlaysAndTheLevel()
        {
            var plan = At(L1, SceneNames.Pause).Go(GameScreen.MainMenu, L1, false);

            CollectionAssert.AreEqual(new[] { SceneNames.Pause, L1 }, plan.Unload);
            CollectionAssert.AreEqual(new[] { SceneNames.MainMenu }, plan.Load);
        }

        [Test]
        public void NextLevel_ComesFromTheCinematic_WithItsOwnScene()
        {
            var p = At(SceneNames.LevelComplete);
            p.Go(GameScreen.Cinematic, L2, false);
            var plan = p.Go(GameScreen.Playing, L2, true);

            CollectionAssert.AreEqual(new[] { L2 }, plan.Load);
            Assert.AreEqual(L2, p.BaseScene);
        }

        [Test]
        public void StartingFromAnOverlayAlone_TheMenuReplacesIt()
        {
            var plan = At(null, SceneNames.Options).Go(GameScreen.MainMenu, null, false);

            CollectionAssert.AreEqual(new[] { SceneNames.Options }, plan.Unload);
            CollectionAssert.AreEqual(new[] { SceneNames.MainMenu }, plan.Load);
        }
    }
}
```

- [ ] **Step 3: `SceneRoutePlanner.cs`:**

```csharp
using System.Collections.Generic;
using Zombineta.Flow;

namespace Zombineta.Juego.Flow
{
    /// <summary>Los nombres de las escenas fijas. Los niveles vienen de Niveles.asset.</summary>
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string Options = "Options";
        public const string CharacterSelect = "CharacterSelect";
        public const string Cinematic = "Cinematic";
        public const string LevelComplete = "LevelComplete";
        public const string GameOver = "GameOver";
        public const string Ending = "Ending";
        public const string Pause = "Pause";
    }

    /// <summary>Lo que hay que hacer para pasar de una pantalla a otra.</summary>
    public sealed class ScenePlan
    {
        /// <summary>Escenas a descargar, en este orden (primero las capas de arriba).</summary>
        public readonly List<string> Unload = new List<string>();

        /// <summary>Escenas a cargar encima, en este orden (primero la base).</summary>
        public readonly List<string> Load = new List<string>();

        /// <summary>La escena que queda activa: la base (define luz y objetos nuevos).</summary>
        public string Active;

        /// <summary>Si cambia la base hay fundido; las capas entran y salen en seco.</summary>
        public bool Fade;

        public bool IsEmpty => Unload.Count == 0 && Load.Count == 0;
    }

    /// <summary>
    /// Traduce cambios de pantalla en escenas a cargar y descargar. C# plano: no toca el
    /// SceneManager, solo lleva la cuenta de que hay cargado (una escena base y una pila de
    /// capas) y devuelve el plan. Asi las reglas se prueban sin abrir Unity.
    ///
    /// Base: menu, personaje, cinematica, nivel, nivel completo, game over, final.
    /// Capas: opciones y pausa, encima de la base, sin descargarla.
    /// </summary>
    public sealed class SceneRoutePlanner
    {
        string baseScene;
        readonly List<string> overlays = new List<string>();

        public string BaseScene => baseScene;

        public IReadOnlyList<string> Overlays => overlays;

        /// <summary>La escena de mas arriba: la que recibe el foco del teclado.</summary>
        public string TopScene => overlays.Count > 0 ? overlays[overlays.Count - 1] : baseScene;

        /// <summary>Lo que ya esta cargado: nada al arrancar desde Boot, o la escena desde la que se dio Play.</summary>
        public void Start(string loadedBase, params string[] loadedOverlays)
        {
            baseScene = loadedBase;
            overlays.Clear();
            if (loadedOverlays != null)
                overlays.AddRange(loadedOverlays);
        }

        /// <param name="levelScene">Escena del nivel en curso (se usa al ir a Playing).</param>
        /// <param name="restartLevel">El nivel arranca de cero: recargarlo aunque ya este cargado.</param>
        public ScenePlan Go(GameScreen to, string levelScene, bool restartLevel)
        {
            string nextBase = baseScene;
            var nextOverlays = new List<string>(overlays);

            switch (to)
            {
                case GameScreen.Options:
                    nextOverlays.Remove(SceneNames.Options);
                    nextOverlays.Add(SceneNames.Options);
                    break;

                case GameScreen.Paused:
                    nextOverlays.Remove(SceneNames.Options);
                    if (!nextOverlays.Contains(SceneNames.Pause))
                        nextOverlays.Add(SceneNames.Pause);
                    break;

                case GameScreen.Playing:
                    nextBase = levelScene;
                    nextOverlays.Clear();
                    break;

                default:
                    nextBase = ScreenScene(to);
                    nextOverlays.Clear();
                    break;
            }

            bool reloadBase = nextBase != baseScene || (to == GameScreen.Playing && restartLevel);
            var plan = new ScenePlan { Active = nextBase, Fade = reloadBase };

            for (int i = overlays.Count - 1; i >= 0; i--)
                if (reloadBase || !nextOverlays.Contains(overlays[i]))
                    plan.Unload.Add(overlays[i]);
            if (reloadBase && !string.IsNullOrEmpty(baseScene))
                plan.Unload.Add(baseScene);

            if (reloadBase && !string.IsNullOrEmpty(nextBase))
                plan.Load.Add(nextBase);
            foreach (var overlay in nextOverlays)
                if (reloadBase || !overlays.Contains(overlay))
                    plan.Load.Add(overlay);

            baseScene = nextBase;
            overlays.Clear();
            overlays.AddRange(nextOverlays);
            return plan;
        }

        /// <summary>La escena de una pantalla fija, o null si es un nivel.</summary>
        public static string ScreenScene(GameScreen screen)
        {
            switch (screen)
            {
                case GameScreen.MainMenu: return SceneNames.MainMenu;
                case GameScreen.Options: return SceneNames.Options;
                case GameScreen.CharacterSelect: return SceneNames.CharacterSelect;
                case GameScreen.Cinematic: return SceneNames.Cinematic;
                case GameScreen.LevelComplete: return SceneNames.LevelComplete;
                case GameScreen.GameOver: return SceneNames.GameOver;
                case GameScreen.Ending: return SceneNames.Ending;
                case GameScreen.Paused: return SceneNames.Pause;
                default: return null;
            }
        }
    }
}
```

- [ ] **Step 4:** Compilar y correr. Expected: `pass=141`.
- [ ] **Step 5:** Commit `feat: planificador de escenas del juego`.

---

### Task 3: Boot, director y pantallas

**Files (todos en `Juego/Assets/_Zombineta/Scripts/`):**
`Flow/GameRoot.cs`, `Flow/Bootstrapper.cs`, `Flow/ScreenFader.cs`, `Screens/ScreenBase.cs`,
`Screens/MainMenuScreen.cs`, `Screens/OptionsScreen.cs`, `Screens/CharacterSelectScreen.cs`,
`Screens/CinematicPlayer.cs`, `Screens/LevelStub.cs`, `Screens/LevelCompleteScreen.cs`,
`Screens/GameOverScreen.cs`, `Screens/EndingScreen.cs`, `Screens/PauseScreen.cs`.

- [ ] **Step 1: `Flow/ScreenFader.cs`:**

```csharp
using System.Collections;
using UnityEngine;

namespace Zombineta.Juego.Flow
{
    /// <summary>Negro que cubre la pantalla durante los cambios de escena. Tiempo sin escalar.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        CanvasGroup group;

        CanvasGroup Group => group != null ? group : (group = GetComponent<CanvasGroup>());

        public void SetOpacity(float alpha)
        {
            Group.alpha = alpha;
            // Mientras cubre, frena los clics: nadie aprieta un boton de una escena que se va.
            Group.blocksRaycasts = alpha > 0.01f;
        }

        public IEnumerator FadeTo(float target, float seconds)
        {
            float start = Group.alpha;
            if (seconds <= 0f)
            {
                SetOpacity(target);
                yield break;
            }

            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetOpacity(Mathf.Lerp(start, target, t / seconds));
                yield return null;
            }
            SetOpacity(target);
        }
    }
}
```

- [ ] **Step 2: `Flow/Bootstrapper.cs`:**

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zombineta.Juego.Flow
{
    /// <summary>
    /// Play desde cualquier escena: si Boot no esta cargada, la carga al lado sin descargar la
    /// que se estaba mirando. Asi arte prueba su pantalla sin recorrer todo el juego.
    /// </summary>
    public static class Bootstrapper
    {
        /// <summary>La escena desde la que se dio Play.</summary>
        public static string EntryScene { get; private set; }

        // La recarga de dominio esta desactivada: los estaticos sobreviven entre sesiones de Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => EntryScene = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureBoot()
        {
            EntryScene = SceneManager.GetActiveScene().name;
            if (SceneManager.GetSceneByName(SceneNames.Boot).isLoaded)
                return;
            SceneManager.LoadScene(SceneNames.Boot, LoadSceneMode.Additive);
        }
    }
}
```

- [ ] **Step 3: `Flow/GameRoot.cs`:**

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombineta.Core;
using Zombineta.Flow;

namespace Zombineta.Juego.Flow
{
    /// <summary>
    /// Lo que vive toda la partida. Esta en Boot, que nunca se descarga: el flujo de
    /// pantallas, el director de escenas y el fundido. Las pantallas le hablan al flujo por
    /// GameRoot.Flow; el flujo decide y este componente carga y descarga escenas.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] LevelSequence levels;
        [SerializeField] ScreenFader fader;
        [SerializeField] float fadeSeconds = 0.25f;

        public static GameRoot Instance { get; private set; }

        /// <summary>El flujo de pantallas. Null hasta que Boot termina de cargar.</summary>
        public static GameFlow Flow => Instance != null ? Instance.flow : null;

        public LevelSequence Levels => levels;

        /// <summary>La escena de mas arriba (capa o base): la unica que toma el foco del teclado.</summary>
        public string TopScene => planner.TopScene;

        /// <summary>Frame del ultimo cambio de pantalla. Una tecla de ese frame ya se uso.</summary>
        public int LastChangeFrame { get; private set; } = -1;

        /// <summary>True mientras se cargan o descargan escenas.</summary>
        public bool Busy => busy;

        GameFlow flow;
        readonly SceneRoutePlanner planner = new SceneRoutePlanner();
        readonly Queue<ScenePlan> pending = new Queue<ScenePlan>();
        int lastAttempt;
        bool busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            int count = levels != null ? Mathf.Max(1, levels.levels.Count) : 1;
            flow = new GameFlow(count);
            flow.Changed += OnChanged;
            ApplySettings();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }

        void Start()
        {
            string entry = Bootstrapper.EntryScene;

            // Play desde un nivel: el flujo arranca jugando ese nivel, sin recargarlo.
            int level = LevelIndexOf(entry);
            if (level >= 0)
            {
                flow.JumpTo(GameScreen.Playing, level);
                lastAttempt = flow.Attempt;
                planner.Start(entry);
                fader?.SetOpacity(0f);
                return;
            }

            // Play desde una pantalla base: el flujo arranca en esa pantalla.
            if (TryScreenOf(entry, out var screen))
            {
                flow.JumpTo(screen);
                planner.Start(entry);
                fader?.SetOpacity(0f);
                return;
            }

            // Arranque normal desde Boot (o desde una capa suelta, que se descarga): al menu.
            if (entry == SceneNames.Options || entry == SceneNames.Pause)
                planner.Start(null, entry);
            else
                planner.Start(null);

            fader?.SetOpacity(1f);
            Enqueue(planner.Go(GameScreen.MainMenu, null, false));
        }

        public void ApplySettings()
        {
            AudioListener.volume = GameSettings.Volume;
            if (!Application.isEditor)
                Screen.fullScreen = GameSettings.Fullscreen;
        }

        public LevelInfo CurrentLevel =>
            levels != null && flow.LevelIndex < levels.levels.Count ? levels.levels[flow.LevelIndex] : null;

        // --- Director ------------------------------------------------------------

        void OnChanged(GameScreen from, GameScreen to)
        {
            LastChangeFrame = Time.frameCount;

            bool restart = flow.Attempt != lastAttempt;
            lastAttempt = flow.Attempt;

            Enqueue(planner.Go(to, LevelScene(flow.LevelIndex), restart));

            // Con la pausa encima (sola o con opciones), el tiempo del nivel se congela.
            Time.timeScale = ContainsOverlay(SceneNames.Pause) ? 0f : 1f;
        }

        bool ContainsOverlay(string scene)
        {
            foreach (var o in planner.Overlays)
                if (o == scene)
                    return true;
            return false;
        }

        void Enqueue(ScenePlan plan)
        {
            if (plan == null || plan.IsEmpty)
                return;
            pending.Enqueue(plan);
            if (!busy)
                StartCoroutine(RunPlans());
        }

        IEnumerator RunPlans()
        {
            busy = true;
            while (pending.Count > 0)
            {
                var plan = pending.Dequeue();

                if (plan.Fade && fader != null)
                    yield return fader.FadeTo(1f, fadeSeconds);

                foreach (var scene in plan.Unload)
                {
                    if (!SceneManager.GetSceneByName(scene).isLoaded)
                        continue;
                    var op = SceneManager.UnloadSceneAsync(scene);
                    if (op != null)
                        yield return op;
                }

                foreach (var scene in plan.Load)
                {
                    var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                    if (op == null)
                    {
                        Debug.LogError("No se pudo cargar la escena '" + scene + "'. Esta en Build Settings?", this);
                        continue;
                    }
                    yield return op;
                }

                var active = SceneManager.GetSceneByName(plan.Active);
                if (active.IsValid() && active.isLoaded)
                    SceneManager.SetActiveScene(active);

                if (plan.Fade && fader != null)
                    yield return fader.FadeTo(0f, fadeSeconds);
            }
            busy = false;
        }

        // --- Niveles ---------------------------------------------------------------

        string LevelScene(int index)
        {
            if (levels != null && index >= 0 && index < levels.levels.Count &&
                !string.IsNullOrEmpty(levels.levels[index].sceneName))
                return levels.levels[index].sceneName;
            return "Level_" + (index + 1).ToString("00");
        }

        int LevelIndexOf(string scene)
        {
            if (levels == null || string.IsNullOrEmpty(scene))
                return -1;
            for (int i = 0; i < levels.levels.Count; i++)
                if (levels.levels[i].sceneName == scene)
                    return i;
            return -1;
        }

        static bool TryScreenOf(string scene, out GameScreen screen)
        {
            switch (scene)
            {
                case SceneNames.MainMenu: screen = GameScreen.MainMenu; return true;
                case SceneNames.CharacterSelect: screen = GameScreen.CharacterSelect; return true;
                case SceneNames.Cinematic: screen = GameScreen.Cinematic; return true;
                case SceneNames.LevelComplete: screen = GameScreen.LevelComplete; return true;
                case SceneNames.GameOver: screen = GameScreen.GameOver; return true;
                case SceneNames.Ending: screen = GameScreen.Ending; return true;
                default: screen = GameScreen.MainMenu; return false;
            }
        }
    }
}
```

- [ ] **Step 4: `Screens/ScreenBase.cs`:**

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Base de las pantallas con menu. Selecciona el primer boton al abrirse, y lo recupera si
    /// la seleccion se pierde, solo si esta es la escena de arriba: una pausa tapada por las
    /// opciones no le roba el foco.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        [Tooltip("Boton que queda seleccionado al abrir: con teclado o joystick se navega desde ahi.")]
        [SerializeField] protected Selectable firstSelected;

        protected static GameFlow Flow => GameRoot.Flow;

        /// <summary>Una tecla del mismo frame en que cambio la pantalla ya la uso otro.</summary>
        protected static bool InputConsumed =>
            GameRoot.Instance == null || Time.frameCount == GameRoot.Instance.LastChangeFrame;

        protected virtual void Update()
        {
            var es = EventSystem.current;
            var root = GameRoot.Instance;
            if (es == null || root == null || firstSelected == null)
                return;
            if (root.TopScene != gameObject.scene.name)
                return;

            var current = es.currentSelectedGameObject;
            if (current == null || current.scene != gameObject.scene || !current.activeInHierarchy)
                es.SetSelectedGameObject(firstSelected.gameObject);
        }
    }
}
```

- [ ] **Step 5: pantallas simples.** `Screens/MainMenuScreen.cs`:

```csharp
using UnityEngine;

namespace Zombineta.Juego.Screens
{
    public sealed class MainMenuScreen : ScreenBase
    {
        public void Play() => Flow?.Play();

        public void OpenOptions() => Flow?.OpenOptions();

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
```

`Screens/CharacterSelectScreen.cs`:

```csharp
namespace Zombineta.Juego.Screens
{
    public sealed class CharacterSelectScreen : ScreenBase
    {
        public void Choose(int index) => Flow?.ChooseCharacter(index);

        public void Back() => Flow?.Back();
    }
}
```

`Screens/LevelCompleteScreen.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    public sealed class LevelCompleteScreen : ScreenBase
    {
        [SerializeField] Text detail;

        void Start()
        {
            var level = GameRoot.Instance != null ? GameRoot.Instance.CurrentLevel : null;
            if (detail != null && level != null)
                detail.text = level.displayName + " superado";
        }

        public void Continue() => Flow?.Continue();
    }
}
```

`Screens/GameOverScreen.cs`:

```csharp
namespace Zombineta.Juego.Screens
{
    public sealed class GameOverScreen : ScreenBase
    {
        public void Retry() => Flow?.Retry();

        public void ToMenu() => Flow?.ToMainMenu();
    }
}
```

`Screens/EndingScreen.cs`:

```csharp
namespace Zombineta.Juego.Screens
{
    public sealed class EndingScreen : ScreenBase
    {
        public void ToMenu() => Flow?.ToMainMenu();
    }
}
```

- [ ] **Step 6: `Screens/PauseScreen.cs`:**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Zombineta.Juego.Screens
{
    /// <summary>Capa encima del nivel. La accion Pausa tambien la cierra.</summary>
    public sealed class PauseScreen : ScreenBase
    {
        [SerializeField] InputActionReference pauseAction;

        void OnEnable() => pauseAction?.action.Enable();

        protected override void Update()
        {
            base.Update();

            // Solo si la pausa esta arriba: con las opciones abiertas, Esc es de las opciones.
            var root = Flow != null ? Juego.Flow.GameRoot.Instance : null;
            if (root == null || root.TopScene != gameObject.scene.name || InputConsumed)
                return;
            if (pauseAction != null && pauseAction.action.WasPressedThisFrame())
                Flow.Resume();
        }

        public void Resume() => Flow?.Resume();

        public void Retry() => Flow?.Retry();

        public void OpenOptions() => Flow?.OpenOptions();

        public void ToMenu() => Flow?.ToMainMenu();
    }
}
```

- [ ] **Step 7: `Screens/OptionsScreen.cs`:**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Core;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>Capa de opciones. Escribe GameSettings apenas cambia algo: no hay "aplicar".</summary>
    public sealed class OptionsScreen : ScreenBase
    {
        [SerializeField] Slider volume;
        [SerializeField] Toggle fullscreen;
        [SerializeField] Toggle cameraEffects;
        [SerializeField] Toggle cameraShake;
        [SerializeField] Toggle gore;
        [SerializeField] InputActionReference cancelAction;

        void Awake()
        {
            volume?.onValueChanged.AddListener(v => { GameSettings.Volume = v; GameRoot.Instance?.ApplySettings(); });
            fullscreen?.onValueChanged.AddListener(v => { GameSettings.Fullscreen = v; GameRoot.Instance?.ApplySettings(); });
            cameraEffects?.onValueChanged.AddListener(v => GameSettings.CameraEffects = v);
            cameraShake?.onValueChanged.AddListener(v => GameSettings.CameraShake = v);
            gore?.onValueChanged.AddListener(v => GameSettings.Gore = v);
        }

        void OnEnable()
        {
            cancelAction?.action.Enable();
            volume?.SetValueWithoutNotify(GameSettings.Volume);
            fullscreen?.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            cameraEffects?.SetIsOnWithoutNotify(GameSettings.CameraEffects);
            cameraShake?.SetIsOnWithoutNotify(GameSettings.CameraShake);
            gore?.SetIsOnWithoutNotify(GameSettings.Gore);
        }

        protected override void Update()
        {
            base.Update();
            if (InputConsumed || cancelAction == null)
                return;
            if (GameRoot.Instance.TopScene == gameObject.scene.name && cancelAction.action.WasPressedThisFrame())
                Back();
        }

        public void Back() => Flow?.Back();
    }
}
```

- [ ] **Step 8: `Screens/CinematicPlayer.cs`:**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// La cinematica de entrada de un nivel: el titulo y despues las vinetas de comic, en
    /// secuencia y con fundido. Submit pasa a la siguiente vineta; Cancel saltea todo.
    /// Lee que nivel toca del flujo: una sola escena sirve para todos.
    /// </summary>
    public sealed class CinematicPlayer : MonoBehaviour
    {
        [SerializeField] Text title;
        [SerializeField] CanvasGroup titleGroup;
        [SerializeField] Image panel;
        [SerializeField] CanvasGroup panelGroup;
        [SerializeField] float titleSeconds = 2f;
        [SerializeField] float fadeSeconds = 0.35f;
        [SerializeField] InputActionReference submitAction;
        [SerializeField] InputActionReference cancelAction;

        bool skip;
        bool advance;

        void OnEnable()
        {
            submitAction?.action.Enable();
            cancelAction?.action.Enable();
        }

        IEnumerator Start()
        {
            // Si se dio Play directo en esta escena, Boot tarda un frame en aparecer.
            while (GameRoot.Instance == null || GameRoot.Flow == null)
                yield return null;

            var level = GameRoot.Instance.CurrentLevel;
            SetAlpha(titleGroup, 0f);
            SetAlpha(panelGroup, 0f);

            if (title != null)
                title.text = level != null ? level.displayName : "";

            yield return Show(titleGroup, titleSeconds);

            if (level != null)
            {
                foreach (var p in level.comicPanels)
                {
                    if (skip)
                        break;
                    if (panel != null)
                        panel.sprite = p.image;
                    yield return Show(panelGroup, p.seconds);
                }
            }

            Finish();
        }

        void Update()
        {
            if (Time.frameCount == (GameRoot.Instance != null ? GameRoot.Instance.LastChangeFrame : -1))
                return;
            if (cancelAction != null && cancelAction.action.WasPressedThisFrame())
                skip = true;
            if (submitAction != null && submitAction.action.WasPressedThisFrame())
                advance = true;
        }

        IEnumerator Show(CanvasGroup group, float seconds)
        {
            advance = false;
            yield return Fade(group, 1f);
            for (float t = 0f; t < seconds && !advance && !skip; t += Time.unscaledDeltaTime)
                yield return null;
            yield return Fade(group, 0f);
        }

        IEnumerator Fade(CanvasGroup group, float target)
        {
            if (group == null)
                yield break;
            float start = group.alpha;
            for (float t = 0f; t < fadeSeconds && !skip; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / fadeSeconds);
                yield return null;
            }
            group.alpha = target;
        }

        void Finish()
        {
            var flow = GameRoot.Flow;
            if (flow != null && flow.Current == GameScreen.Cinematic)
                flow.CinematicFinished();
        }

        static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
                group.alpha = alpha;
        }
    }
}
```

- [ ] **Step 9: `Screens/LevelStub.cs`:**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Nivel de prueba del esqueleto: se gana con G, se pierde con P y se pausa con la accion
    /// Pausa. Lo reemplaza el gameplay real en los proximos tramos.
    /// </summary>
    public sealed class LevelStub : MonoBehaviour
    {
        [SerializeField] Text label;
        [SerializeField] InputActionReference pauseAction;

        void OnEnable() => pauseAction?.action.Enable();

        void Start()
        {
            if (label != null)
                label.text = gameObject.scene.name.Replace('_', ' ') +
                             "\n\nG: ganar     P: perder     Esc / Start: pausa";
        }

        void Update()
        {
            var root = GameRoot.Instance;
            var flow = GameRoot.Flow;
            if (root == null || flow == null || flow.Current != GameScreen.Playing)
                return;
            if (root.Busy || Time.frameCount == root.LastChangeFrame)
                return;

            var kb = Keyboard.current;
            if (kb != null && kb.gKey.wasPressedThisFrame)
                flow.LevelWon();
            else if (kb != null && kb.pKey.wasPressedThisFrame)
                flow.LevelLost();
            else if (pauseAction != null && pauseAction.action.WasPressedThisFrame())
                flow.Pause();
        }
    }
}
```

- [ ] **Step 10:** Compilar (sin errores). Commit `feat: Boot, director de escenas y pantallas del esqueleto`.

---

### Task 4: Herramienta que construye las escenas

**Files:**
- Create: `Juego/Assets/_Zombineta/Editor/Zombineta.Juego.Editor.asmdef`
- Create: `Juego/Assets/_Zombineta/Editor/SkeletonSceneBuilder.cs`

- [ ] **Step 1:** asmdef de editor: `"name": "Zombineta.Juego.Editor"`, `"includePlatforms": ["Editor"]`,
  referencias `Zombineta.Juego`, `Zombineta.Simulacion`, `Unity.InputSystem`, `UnityEngine.UI`.

- [ ] **Step 2:** `SkeletonSceneBuilder.cs` con el menú `Zombineta/Esqueleto/Construir escenas` que:
  1. Agrega la acción `Pause` (Esc, `<Gamepad>/start`) al mapa `Player` de
     `Assets/Settings/InputSystem_Actions.inputactions` si no existe, y reimporta.
  2. Genera 6 viñetas placeholder (`Assets/_Zombineta/Art/Placeholder/vineta_0N.png`, 960×540,
     color distinto con marco y número en bloques) como Sprites.
  3. Crea/actualiza `Assets/_Zombineta/Settings/Niveles.asset` con dos niveles
     (`Level_01`/"Nivel 1: La salida", `Level_02`/"Nivel 2: El puente"), tres viñetas cada uno.
  4. Construye las escenas: `Boot` (GameRoot, EventSystem con `InputSystemUIInputModule`, canvas
     del fundido), `MainMenu`, `Options`, `CharacterSelect`, `Cinematic`, `Level_01`, `Level_02`,
     `LevelComplete`, `GameOver`, `Ending`, `Pause`, con cámara (solo bases), canvas 1920×1080,
     fondo, título y botones con listeners persistentes (`UnityEventTools`).
  5. Registra todas en Build Settings con `Boot` primero.

  El código completo está en el archivo del commit de esta task (es una herramienta de editor
  larga; se revisa por su resultado en el paso 3).

- [ ] **Step 3:** Ejecutar el menú por MCP. Verificar: 11 escenas en Build Settings, cada una
  abre sin scripts faltantes, `Niveles.asset` con 2 niveles y sus viñetas.
- [ ] **Step 4:** Commit `feat: herramienta que construye las escenas del esqueleto`.

---

### Task 5: Recorrido completo en Play

- [ ] Abrir `Boot`, entrar en Play con `runInBackground`, y en un solo `RunCommand` con callbacks de
  `EditorApplication.update` recorrer el flujo llamando a `GameRoot.Flow` y registrando, después
  de que `GameRoot.Busy` quede en false, qué escenas están cargadas y cuál es la activa:
  menú → personaje → cinemática (esperar a que termine sola) → nivel 1 → pausa → opciones →
  volver → seguir → ganar → nivel completo → continuar → cinemática → nivel 2 → perder →
  reintentar → ganar → continuar → final → menú. Expected: en cada paso, exactamente `Boot` + la
  base + las capas esperadas; `Time.timeScale` 0 solo con la pausa encima.
- [ ] Salir de Play, abrir `Level_02`, dar Play: se carga `Boot` sola, el flujo queda en `Playing`
  con `LevelIndex` 1 y Esc abre la pausa.
- [ ] Capturas de menú, cinemática, nivel con pausa y opciones sobre la pausa (canvas a
  ScreenSpaceCamera para capturar, trampa #8).
- [ ] Sin errores en consola.

### Task 6: Documentación y subida

- [ ] HANDOFF: sección de escenas en `Juego/` (cómo agregar una pantalla o un nivel, Play desde
  cualquier escena, la herramienta de esqueleto) y tabla de sub-proyectos con el 2 hecho.
- [ ] Commit, push a `Jose`, merge a `master`.
