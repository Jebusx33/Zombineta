using Zombineta.Core;

namespace Zombineta.Player
{
    /// <summary>
    /// Lo que la jugadora QUIERE hacer este frame. Desacopla el input de la
    /// simulacion: un test puede fabricar intents sin teclado, y cambiar el
    /// esquema de controles no toca ninguna regla de juego.
    /// </summary>
    public struct PlayerIntent
    {
        /// <summary>-1 baja un carril, +1 sube uno, 0 no cambia.</summary>
        public int LaneDelta;

        public DriveMode Mode;
        public bool ToggleHeadlight;
        public bool Fire;

        /// <summary>A pie (en la pelea): -1 izquierda, 1 derecha, 0 quieta.</summary>
        public float MoveX;

        /// <summary>A pie: pina hacia donde mira.</summary>
        public bool Golpe;

        public static PlayerIntent Idle => new PlayerIntent { Mode = DriveMode.Normal };
    }
}
