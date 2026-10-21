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

        /// <summary>A pie (en la pelea): -1 izquierda, 1 derecha, 0 quieta. Analogico con el stick.</summary>
        public float MoveX;

        /// <summary>A pie: -1 hacia el carril de abajo, 1 hacia el de arriba. Analogico con el stick.</summary>
        public float MoveY;

        /// <summary>
        /// A pie: el movimiento viene de MoveX/MoveY (libre). Con esto prendido, LaneDelta se
        /// ignora en la pelea: arriba y abajo son los mismos botones y no deben saltar un carril
        /// ademas de caminar.
        /// </summary>
        public bool MovimientoLibre;

        /// <summary>A pie: pina hacia donde mira.</summary>
        public bool Golpe;

        /// <summary>En la moto: lanza una granada hacia atras.</summary>
        public bool Granada;

        /// <summary>A pie: pone una bomba temporizadora.</summary>
        public bool Bomba;

        public static PlayerIntent Idle => new PlayerIntent { Mode = DriveMode.Normal };
    }
}
