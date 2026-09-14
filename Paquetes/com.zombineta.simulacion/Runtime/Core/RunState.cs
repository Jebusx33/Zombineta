namespace Zombineta.Core
{
    public enum DriveMode
    {
        Reverse = -1,
        Normal = 0,
        Turbo = 1,
    }

    public enum RunPhase
    {
        Running,
        Won,
        Lost,
    }

    /// <summary>Por que se perdio. Solo informativo, para la pantalla de Game Over.</summary>
    public enum LossReason
    {
        None,
        CaughtByHorde,
        OutOfFuel,
    }

    public enum PickupKind
    {
        Fuel,
        Battery,
        Ammo,
    }

    /// <summary>
    /// Estado completo de una partida. C# plano, sin MonoBehaviour: se puede
    /// crear, mutar y verificar en un test sin abrir una escena.
    /// </summary>
    public sealed class RunState
    {
        public float PlayerX;
        public float HordeX;

        /// <summary>Carril logico: 0 = abajo, 1 = medio, 2 = arriba.</summary>
        public int Lane;

        /// <summary>Carril visual (float). Persigue a Lane para dar el tween.</summary>
        public float LaneVisual;

        public float Fuel;
        public float Battery;
        public int Ammo;

        public bool HeadlightOn;
        public DriveMode Mode;

        public float StunRemaining;
        public float Elapsed;

        // --- Salto ---
        public bool Airborne;
        /// <summary>Metros sobre el carril.</summary>
        public float Height;
        /// <summary>m/s, positivo hacia arriba.</summary>
        public float VerticalSpeed;
        /// <summary>Velocidad horizontal durante el salto: la de la rampa, fija hasta aterrizar.</summary>
        public float AirSpeed;
        /// <summary>Inclinacion en grados. Positivo = nariz arriba.</summary>
        public float Pitch;
        /// <summary>Giro en grados/s que trae de la rampa (mas rapido, mas gira).</summary>
        public float LaunchSpin;
        /// <summary>Segundos que quedan del impulso por aterrizaje perfecto.</summary>
        public float BoostRemaining;
        /// <summary>Tirada en el piso tras aterrizar mal. Dura lo que el aturdimiento.</summary>
        public bool Fallen;

        public RunPhase Phase;
        public LossReason Loss;

        public float Gap => PlayerX - HordeX;
    }
}
