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
