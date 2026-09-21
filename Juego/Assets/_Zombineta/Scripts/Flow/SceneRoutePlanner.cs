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
        public const string Credits = "Credits";
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
                case GameScreen.Credits: return SceneNames.Credits;
                default: return null;
            }
        }
    }
}
