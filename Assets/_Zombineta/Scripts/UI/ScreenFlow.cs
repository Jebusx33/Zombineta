using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Core;
using Zombineta.Flow;
using Zombineta.Player;

namespace Zombineta.UI
{
    /// <summary>Un personaje elegible. Placeholder: por ahora solo cambia el color de la moto.</summary>
    [Serializable]
    public sealed class CharacterOption
    {
        public string name = "Personaje";
        [TextArea(1, 3)] public string blurb = "";
        public Color color = Color.white;
    }

    /// <summary>
    /// Conecta el flujo de pantallas (GameFlow, C# plano) con Unity: lee el teclado, le
    /// avisa al flujo que paso, y cuando el flujo cambia de pantalla muestra el panel que
    /// corresponde y pausa o larga la partida.
    /// </summary>
    public sealed class ScreenFlow : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] ScooterView scooter;
        [SerializeField] LevelSequence levels;
        [SerializeField] List<CharacterOption> characters = new List<CharacterOption>();

        [Header("Paneles")]
        [SerializeField] GameObject mainMenuPanel;
        [SerializeField] GameObject optionsPanel;
        [SerializeField] GameObject characterPanel;
        [SerializeField] GameObject cinematicPanel;
        [SerializeField] GameObject hudPanel;
        [SerializeField] GameObject levelCompletePanel;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] GameObject endingPanel;

        [Header("Menus")]
        [SerializeField] MenuList mainMenu;
        [SerializeField] MenuList optionsMenu;
        [SerializeField] MenuList characterMenu;
        [SerializeField] MenuList gameOverMenu;

        [Header("Textos variables")]
        [SerializeField] Text characterBlurb;
        [SerializeField] Text cinematicTitle;
        [SerializeField] Text cinematicLine;
        [SerializeField] Text levelCompleteDetail;
        [SerializeField] Text gameOverReason;

        [Header("Cinematica")]
        [Tooltip("Segundos que queda cada placa de la cinematica placeholder.")]
        [SerializeField] float secondsPerLine = 2.4f;

        const string VolumeKey = "zombineta.volume";
        const string FullscreenKey = "zombineta.fullscreen";

        GameFlow flow;
        bool fullscreen;   // Screen.fullScreen recien cambia al final del frame
        int cinematicIndex;
        float cinematicTimer;

        /// <summary>El flujo en curso. Expuesto para inspeccionarlo y para herramientas.</summary>
        public GameFlow Flow => flow;

        void Awake()
        {
            int levelCount = levels != null ? Mathf.Max(1, levels.levels.Count) : 1;
            flow = new GameFlow(levelCount);
            flow.Changed += OnScreenChanged;

            if (run != null)
                run.QuickRestartEnabled = false;

            AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            fullscreen = Screen.fullScreen;
        }

        void Start()
        {
            // El menu se muestra sobre la calle del primer nivel, quieta.
            LoadCurrentLevel();
            ShowOnly(mainMenuPanel);
            if (mainMenu != null) mainMenu.Select(0);
            RefreshOptionLabels();
        }

        void Update()
        {
            if (run == null || run.Sim == null)
                return;

            var kb = Keyboard.current;

            // Se procesa solo la pantalla con la que empezo el frame: una misma tecla no
            // puede atravesar dos pantallas de un saque.
            switch (flow.Current)
            {
                case GameScreen.MainMenu: UpdateMainMenu(kb); break;
                case GameScreen.Options: UpdateOptions(kb); break;
                case GameScreen.CharacterSelect: UpdateCharacterSelect(kb); break;
                case GameScreen.Cinematic: UpdateCinematic(kb); break;
                case GameScreen.Playing: UpdatePlaying(kb); break;
                case GameScreen.LevelComplete: if (Confirm(kb)) flow.Continue(); break;
                case GameScreen.GameOver: UpdateGameOver(kb); break;
                case GameScreen.Ending: if (Confirm(kb)) flow.ToMainMenu(); break;
            }
        }

        // --- Pantallas ---------------------------------------------------------

        void UpdateMainMenu(Keyboard kb)
        {
            Navigate(kb, mainMenu);
            if (!Confirm(kb)) return;

            switch (mainMenu.Selected)
            {
                case 0: flow.Play(); break;
                case 1: flow.OpenOptions(); break;
                default: Quit(); break;
            }
        }

        void UpdateOptions(Keyboard kb)
        {
            Navigate(kb, optionsMenu);

            int horizontal = Horizontal(kb);
            bool confirm = Confirm(kb);
            bool change = horizontal != 0 || confirm;

            if (optionsMenu.Selected == 0 && change)
            {
                // Izquierda/derecha de a 10%. ENTER sube y al pasar de 100% vuelve a 0.
                float v = AudioListener.volume + (horizontal != 0 ? horizontal : 1) * 0.1f;
                if (horizontal == 0 && v > 1.001f) v = 0f;
                v = Mathf.Clamp01(Mathf.Round(v * 10f) / 10f);
                AudioListener.volume = v;
                PlayerPrefs.SetFloat(VolumeKey, v);
                RefreshOptionLabels();
            }
            else if (optionsMenu.Selected == 1 && change)
            {
                fullscreen = !fullscreen;
                Screen.fullScreen = fullscreen;
                PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
                RefreshOptionLabels();
            }
            else if (Back(kb) || (confirm && optionsMenu.Selected == 2))
            {
                flow.Back();
            }
        }

        void UpdateCharacterSelect(Keyboard kb)
        {
            int before = characterMenu.Selected;
            Navigate(kb, characterMenu);
            if (characterMenu.Selected != before)
                PreviewCharacter(characterMenu.Selected);

            if (Confirm(kb))
                flow.ChooseCharacter(characterMenu.Selected);
            else if (Back(kb))
                flow.Back();
        }

        void UpdateCinematic(Keyboard kb)
        {
            if (Confirm(kb))
            {
                flow.CinematicFinished();
                return;
            }

            cinematicTimer += Time.deltaTime;
            if (cinematicTimer < secondsPerLine)
                return;

            cinematicTimer = 0f;
            cinematicIndex++;
            var lines = CurrentLevel?.introLines;
            if (lines == null || cinematicIndex >= lines.Length)
                flow.CinematicFinished();
            else
                cinematicLine.text = lines[cinematicIndex];
        }

        void UpdatePlaying(Keyboard kb)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Atajos para recorrer el flujo sin tener que ganar o perder de verdad.
            if (kb != null && kb.f2Key.wasPressedThisFrame)
                run.Sim.State.Phase = RunPhase.Won;
            if (kb != null && kb.f3Key.wasPressedThisFrame)
            {
                run.Sim.State.Phase = RunPhase.Lost;
                run.Sim.State.Loss = LossReason.CaughtByHorde;
            }
#endif
            if (run.Sim.State.Phase == RunPhase.Won)
                flow.LevelWon();
            else if (run.Sim.State.Phase == RunPhase.Lost)
                flow.LevelLost();
        }

        void UpdateGameOver(Keyboard kb)
        {
            Navigate(kb, gameOverMenu);

            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                flow.Retry();
                return;
            }

            if (!Confirm(kb)) return;
            if (gameOverMenu.Selected == 0) flow.Retry();
            else flow.ToMainMenu();
        }

        // --- Entrada a cada pantalla ------------------------------------------

        void OnScreenChanged(GameScreen from, GameScreen to)
        {
            switch (to)
            {
                case GameScreen.MainMenu:
                    run.Paused = true;
                    ShowOnly(mainMenuPanel);
                    mainMenu.Select(0);
                    break;

                case GameScreen.Options:
                    ShowOnly(optionsPanel);
                    optionsMenu.Select(0);
                    RefreshOptionLabels();
                    break;

                case GameScreen.CharacterSelect:
                    ShowOnly(characterPanel);
                    characterMenu.Select(flow.CharacterIndex);
                    PreviewCharacter(flow.CharacterIndex);
                    break;

                case GameScreen.Cinematic:
                    LoadCurrentLevel();
                    ApplyCharacter(flow.CharacterIndex);
                    StartCinematic();
                    ShowOnly(cinematicPanel);
                    break;

                case GameScreen.Playing:
                    // Desde la cinematica o desde Reintentar: siempre partida nueva.
                    run.Restart();
                    ShowOnly(hudPanel);
                    break;

                case GameScreen.LevelComplete:
                    run.Paused = true;
                    levelCompleteDetail.text =
                        CurrentLevel?.displayName + " en " + run.Sim.State.Elapsed.ToString("0.0") + " s" +
                        (flow.IsLastLevel ? "\nEra el último nivel." : "\nSigue: " + NextLevelName());
                    ShowOnly(levelCompletePanel);
                    break;

                case GameScreen.GameOver:
                    run.Paused = true;
                    gameOverReason.text = run.Sim.State.Loss == LossReason.OutOfFuel
                        ? "Te quedaste sin nafta y la horda te alcanzó."
                        : "La horda te alcanzó.";
                    gameOverMenu.Select(0);
                    ShowOnly(gameOverPanel);
                    break;

                case GameScreen.Ending:
                    run.Paused = true;
                    ShowOnly(endingPanel);
                    break;
            }
        }

        // --- Ayudantes ---------------------------------------------------------

        LevelInfo CurrentLevel =>
            levels != null && flow.LevelIndex < levels.levels.Count ? levels.levels[flow.LevelIndex] : null;

        string NextLevelName()
        {
            int next = flow.LevelIndex + 1;
            return levels != null && next < levels.levels.Count ? levels.levels[next].displayName : "";
        }

        void LoadCurrentLevel()
        {
            var info = CurrentLevel;
            if (info != null && info.route != null)
                run.LoadLevel(info.route);
            else
                run.Paused = true;
        }

        void StartCinematic()
        {
            cinematicIndex = 0;
            cinematicTimer = 0f;
            var info = CurrentLevel;
            cinematicTitle.text = info != null ? info.displayName : "";
            cinematicLine.text = info != null && info.introLines.Length > 0 ? info.introLines[0] : "";
        }

        void PreviewCharacter(int index)
        {
            if (index < 0 || index >= characters.Count) return;
            if (characterBlurb != null) characterBlurb.text = characters[index].blurb;
            ApplyCharacter(index);
        }

        void ApplyCharacter(int index)
        {
            if (scooter != null && index >= 0 && index < characters.Count)
                scooter.SetCharacterColor(characters[index].color);
        }

        void RefreshOptionLabels()
        {
            if (optionsMenu == null) return;
            // Sin flechas alrededor del valor: chocarian con los marcadores "> <" de la
            // opcion seleccionada. Como se cambia lo dice la ayuda del pie.
            optionsMenu.SetLabel(0, "Volumen:  " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%");
            optionsMenu.SetLabel(1, "Pantalla completa:  " + (fullscreen ? "Sí" : "No"));
        }

        void ShowOnly(GameObject panel)
        {
            foreach (var p in new[] { mainMenuPanel, optionsPanel, characterPanel, cinematicPanel,
                                      hudPanel, levelCompletePanel, gameOverPanel, endingPanel })
                if (p != null) p.SetActive(p == panel);
        }

        static void Navigate(Keyboard kb, MenuList menu)
        {
            if (kb == null || menu == null) return;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) menu.Move(-1);
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) menu.Move(1);
        }

        static int Horizontal(Keyboard kb)
        {
            if (kb == null) return 0;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) return -1;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return 1;
            return 0;
        }

        static bool Confirm(Keyboard kb) =>
            kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame
                           || kb.spaceKey.wasPressedThisFrame);

        static bool Back(Keyboard kb) =>
            kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame);

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
