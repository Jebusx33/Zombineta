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
        /// <summary>Sin nafta y esperando el auxilio: la protagonista pelea a pie.</summary>
        Pelea,
    }

    /// <summary>La nota de una pelea, de peor a mejor: segun los golpes que se llevo la protagonista.</summary>
    public enum NotaPelea { C, B, A, S }

    /// <summary>Por que se perdio. Solo informativo, para la pantalla de Game Over.</summary>
    public enum LossReason
    {
        None,
        CaughtByHorde,
        OutOfFuel,
        /// <summary>Se quedo sin vida peleando a pie mientras esperaba el auxilio.</summary>
        Derribada,
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

        // --- Auxilio y pelea a pie ---
        /// <summary>Pedidos de auxilio que le quedan en el nivel.</summary>
        public int Ayudas;
        /// <summary>Auxilios que ya uso: los bidones de algunos niveles aparecen recien despues del primero.</summary>
        public int AyudasUsadas;
        /// <summary>
        /// Segundos que le quedan a la moto frenando antes de bajarse a pelear (sin nafta, con ayuda).
        /// Negativo = no esta frenando.
        /// </summary>
        public float FrenadoRestante = -1f;
        /// <summary>Velocidad que llevaba al quedarse sin nafta: de ahi baja hasta parar.</summary>
        public float VelocidadAlFrenar;
        /// <summary>Segundos que le quedan al arranque suave despues del auxilio (0 = ya a velocidad normal).</summary>
        public float ArranqueRestante;
        /// <summary>Oleada de la pelea en curso (0 = la primera).</summary>
        public int Oleada;
        /// <summary>Veces que todavia puede reintentar esta pelea si la derriban.</summary>
        public int ReintentosPelea;
        /// <summary>Corazones con los que empezo la pelea: el tope al que llegan los botiquines.</summary>
        public int VidaMax;

        // --- Numeros de la pelea (para la nota, el premio y la telemetria) ---
        /// <summary>Zombies que mato a golpes o a tiros en la pelea (los que arrolla la furgoneta no cuentan).</summary>
        public int PeleaBajas;
        public int PeleaGolpesRecibidos;
        public int PeleaRemates;
        public int PeleaBotiquines;
        /// <summary>Nota de la ultima pelea terminada, y lo que dejo la furgoneta por ella.</summary>
        public NotaPelea Nota;
        public int PremioBalas;
        public float PremioBateria;
        /// <summary>Golpes que aguanta en la pelea. Solo cuenta mientras pelea.</summary>
        public int Vida;
        /// <summary>Segundos que faltan para que llegue la furgoneta.</summary>
        public float PeleaRestante;
        /// <summary>Metros: donde esta la protagonista a pie. La moto queda en PlayerX.</summary>
        public float PeatonX;
        /// <summary>
        /// A pie, en carriles y continuo (0 = abajo, 2 = arriba): camina libre entre los tres.
        /// Lane es el mas cercano y LaneVisual lo sigue de cerca para las vistas.
        /// </summary>
        public float PeatonY;
        /// <summary>Hacia donde mira a pie: 1 derecha, -1 izquierda.</summary>
        public int Mira = 1;
        /// <summary>Segundos en que no la pueden lastimar (recien golpeada).</summary>
        public float Invulnerable;
        /// <summary>Segundos hasta poder volver a pegar.</summary>
        public float GolpeEnfriamiento;
        /// <summary>Segundos que dura el golpe en curso (para la vista).</summary>
        public float Golpeando;
        /// <summary>Paso de la cadena de golpes en curso: 1, 2 o 3 (el remate). 0 si no hay cadena.</summary>
        public int ComboPaso;
        /// <summary>Segundos que quedan para seguir la cadena con otro golpe; si llega a 0 vuelve a empezar.</summary>
        public float ComboVentana;
        public bool FurgonetaEnCamino;
        /// <summary>Metros: donde va la furgoneta mientras cruza.</summary>
        public float FurgonetaX;

        public float Gap => PlayerX - HordeX;
    }
}
