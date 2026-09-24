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
        // Nuevo al final: cambio aditivo, no rompe lo ya serializado (el prototipo no la usa).
        Credits,
    }

    /// <summary>
    /// El flujo de pantallas como maquina de estados. C# plano: no sabe de escenas, ni de
    /// teclas, ni de Unity. La vista le avisa que paso ("apretaron Jugar", "se gano el
    /// nivel") y el flujo decide a donde se va.
    ///
    ///   Menu <-> Opciones <-> Pausa
    ///   Menu <-> Creditos
    ///   Menu -> Personaje -> Cinematica -> Nivel <-> Pausa
    ///   Nivel gano   -> Nivel completo -> Cinematica del siguiente (o Final si era el ultimo)
    ///   Nivel perdio -> Game Over -> Reintentar (mismo nivel) o Menu
    ///   Final -> Creditos -> Menu
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

        /// <summary>
        /// Lo pone GameRoot desde GameSettings.TutorialVisto al arrancar: si esta en true, la
        /// proxima vez que se elija personaje se entra al tutorial en vez de a la cinematica.
        /// </summary>
        public bool TutorialPendiente { get; set; }

        /// <summary>La partida en curso es el tutorial (sigue siendo GameScreen.Playing).</summary>
        public bool EnTutorial { get; private set; }

        /// <summary>El tutorial actual se abrio con OpenTutorial (a pedido), no desde el flujo normal.</summary>
        public bool TutorialDesdeMenu { get; private set; }

        /// <summary>Se dispara en cada cambio de pantalla: (desde, hacia).</summary>
        public event Action<GameScreen, GameScreen> Changed;

        /// <summary>Se dispara al terminar el tutorial, ya sea completandolo o salteandolo.</summary>
        public event Action TutorialTerminado;

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

        /// <summary>
        /// Elegir personaje arranca siempre desde el primer nivel. Si el tutorial esta
        /// pendiente, la primera partida pasa por el en vez de ir directo a la cinematica.
        /// </summary>
        public bool ChooseCharacter(int index)
        {
            if (Current != GameScreen.CharacterSelect || index < 0)
                return false;

            CharacterIndex = index;
            LevelIndex = 0;

            if (TutorialPendiente)
            {
                EnTutorial = true;
                Attempt++;
                return Switch(GameScreen.Playing);
            }

            return Switch(GameScreen.Cinematic);
        }

        public bool CinematicFinished()
        {
            if (Current != GameScreen.Cinematic)
                return false;
            Attempt++;
            return Switch(GameScreen.Playing);
        }

        /// <summary>Ignorado durante el tutorial: no se puede perder ni ganar como un nivel.</summary>
        public bool LevelWon() => !EnTutorial && Go(GameScreen.Playing, GameScreen.LevelComplete);

        /// <summary>Ignorado durante el tutorial: no se puede perder ni ganar como un nivel.</summary>
        public bool LevelLost() => !EnTutorial && Go(GameScreen.Playing, GameScreen.GameOver);

        // --- Tutorial ----------------------------------------------------------

        /// <summary>Tutorial a pedido desde el menu principal. Usa el ultimo personaje elegido.</summary>
        public bool OpenTutorial()
        {
            if (Current != GameScreen.MainMenu)
                return false;

            EnTutorial = true;
            TutorialDesdeMenu = true;
            Attempt++;
            return Switch(GameScreen.Playing);
        }

        /// <summary>
        /// Termina el tutorial, completado o salteado: apaga EnTutorial, marca que ya no esta
        /// pendiente, avisa por TutorialTerminado y sigue por donde corresponda.
        /// </summary>
        public bool TutorialFinished()
        {
            if (!EnTutorial || (Current != GameScreen.Playing && Current != GameScreen.Paused))
                return false;

            EnTutorial = false;
            TutorialPendiente = false;
            bool desdeMenu = TutorialDesdeMenu;
            TutorialDesdeMenu = false;
            TutorialTerminado?.Invoke();

            return Switch(desdeMenu ? GameScreen.MainMenu : GameScreen.Cinematic);
        }

        /// <summary>Saltear desde la pausa, dentro del tutorial: equivale a terminarlo.</summary>
        public bool SkipTutorial()
        {
            if (Current != GameScreen.Paused || !EnTutorial)
                return false;
            return TutorialFinished();
        }

        /// <summary>
        /// Como JumpTo, pero directo al tutorial: para dar Play en Tutorial.unity desde el
        /// editor, igual que JumpTo hace con un nivel.
        /// </summary>
        public void JumpToTutorial()
        {
            LevelIndex = 0;
            EnTutorial = true;
            Attempt++;
            optionsReturn = GameScreen.MainMenu;
            Current = GameScreen.Playing;
        }

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

        /// <summary>
        /// Sale de la partida al menu. Desde la pausa del tutorial lo saca sin marcarlo como
        /// visto ni disparar TutorialTerminado: no lo termino, solo se fue.
        /// </summary>
        public bool ToMainMenu()
        {
            if (Current == GameScreen.GameOver || Current == GameScreen.Ending || Current == GameScreen.Paused ||
                Current == GameScreen.Credits)
            {
                EnTutorial = false;
                TutorialDesdeMenu = false;
                return Switch(GameScreen.MainMenu);
            }
            return false;
        }

        // --- Creditos ----------------------------------------------------------

        /// <summary>Desde el menu principal.</summary>
        public bool OpenCredits() => Go(GameScreen.MainMenu, GameScreen.Credits);

        /// <summary>A lo que lleva el boton Continuar del final.</summary>
        public bool ShowCredits() => Go(GameScreen.Ending, GameScreen.Credits);

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
