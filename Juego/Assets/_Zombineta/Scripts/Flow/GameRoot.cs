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
