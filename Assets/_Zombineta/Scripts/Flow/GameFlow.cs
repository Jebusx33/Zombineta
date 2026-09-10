using System;

namespace Zombineta.Flow
{
    /// <summary>Las pantallas del juego, segun el diagrama de flujo del GDD.</summary>
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
    }

    /// <summary>
    /// El flujo de pantallas como maquina de estados. C# plano: no sabe de paneles, ni de
    /// teclas, ni de Unity. La vista le avisa que paso ("apretaron Jugar", "se gano el
    /// nivel") y el flujo decide a donde se va.
    ///
    ///   Menu <-> Opciones
    ///   Menu -> Personaje -> Cinematica -> Nivel
    ///   Nivel gano   -> Nivel completo -> Cinematica del siguiente (o Final si era el ultimo)
    ///   Nivel perdio -> Game Over -> Reintentar (mismo nivel) o Menu
    ///
    /// Una accion que no corresponde a la pantalla actual se ignora y devuelve false:
    /// un ENTER que llega tarde no puede saltear pantallas.
    /// </summary>
    public sealed class GameFlow
    {
        readonly int levelCount;

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

        public int LevelCount => levelCount;

        public bool IsLastLevel => LevelIndex >= levelCount - 1;

        /// <summary>Se dispara en cada cambio de pantalla: (desde, hacia).</summary>
        public event Action<GameScreen, GameScreen> Changed;

        // --- Menu principal --------------------------------------------------

        public bool Play() => Go(GameScreen.MainMenu, GameScreen.CharacterSelect);

        public bool OpenOptions() => Go(GameScreen.MainMenu, GameScreen.Options);

        /// <summary>Volver: desde Opciones o desde la seleccion de personaje, al menu.</summary>
        public bool Back()
        {
            if (Current == GameScreen.Options || Current == GameScreen.CharacterSelect)
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

        public bool CinematicFinished() => Go(GameScreen.Cinematic, GameScreen.Playing);

        public bool LevelWon() => Go(GameScreen.Playing, GameScreen.LevelComplete);

        public bool LevelLost() => Go(GameScreen.Playing, GameScreen.GameOver);

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
        /// Reintentar va directo al nivel, sin repetir la cinematica: despues de morir
        /// nadie quiere volver a ver la introduccion.
        /// </summary>
        public bool Retry() => Go(GameScreen.GameOver, GameScreen.Playing);

        public bool ToMainMenu()
        {
            if (Current == GameScreen.GameOver || Current == GameScreen.Ending)
                return Switch(GameScreen.MainMenu);
            return false;
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
