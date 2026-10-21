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
        /// <summary>Sin nafta: la protagonista pelea a pie hasta vencer las oleadas.</summary>
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
        /// <summary>Se quedo sin vida peleando a pie.</summary>
        Derribada,
    }

    public enum PickupKind
    {
        Fuel,
        Battery,
        Ammo,
        Grenade,
        Bomb,
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
        /// <summary>Granadas que lleva para lanzar desde la moto (arma secundaria de la pistola).</summary>
        public int Granadas;
        /// <summary>Bombas temporizadoras que lleva para la pelea a pie.</summary>
        public int Bombas;
        /// <summary>Segundos que le quedan en el aire a la granada lanzada (0 = ninguna), y los metros donde va a caer.</summary>
        public float GranadaVuelo;
        public float GranadaX;
        /// <summary>Hacia donde va la granada: -1 hacia atras (moto), o hacia donde miraba a pie (1 o -1).</summary>
        public int GranadaDir = -1;
        /// <summary>A pie: segundos que le quedan a la mecha de la bomba puesta (0 = ninguna) y los metros donde esta.</summary>
        public float BombaMecha;
        public float BombaX;
        /// <summary>Carril (continuo) donde se puso la bomba.</summary>
        public float BombaY;
        /// <summary>Zombies que se llevo la ultima explosion (granada, barril o bomba): para la telemetria.</summary>
        public int UltimaExplosionMuertos;

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

        // --- Pelea a pie ---
        /// <summary>
        /// Peleas que empezo en el nivel (la primera trae un corazon de mas y el arsenal; los bidones de algunos
        /// niveles aparecen recien despues de ella). El reintento de una pelea no cuenta como otra.
        /// </summary>
        public int Peleas;
        /// <summary>
        /// Segundos que le quedan a la moto frenando antes de bajarse a pelear (sin nafta).
        /// Negativo = no esta frenando.
        /// </summary>
        public float FrenadoRestante = -1f;
        /// <summary>Velocidad que llevaba al quedarse sin nafta: de ahi baja hasta parar.</summary>
        public float VelocidadAlFrenar;
        /// <summary>Segundos que le quedan al arranque suave despues de la pelea (0 = ya a velocidad normal).</summary>
        public float ArranqueRestante;
        /// <summary>Oleada de la pelea en curso (0 = la primera).</summary>
        public int Oleada;
        /// <summary>Veces que todavia puede reintentar esta pelea si la derriban.</summary>
        public int ReintentosPelea;
        /// <summary>Corazones con los que empezo la pelea: el tope al que llegan los botiquines.</summary>
        public int VidaMax;

        // --- Numeros de la pelea (para la nota, el premio y la telemetria) ---
        /// <summary>Zombies que vencio en la pelea (a golpes, a tiros, con bombas o con barriles).</summary>
        public int PeleaBajas;
        public int PeleaGolpesRecibidos;
        public int PeleaRemates;
        public int PeleaBotiquines;
        /// <summary>Saltos de zombies: los que le cayeron encima, los que esquivo y los que corto en el aire (golpe o balazo).</summary>
        public int PeleaSaltosAcertados;
        public int PeleaSaltosEsquivados;
        public int PeleaSaltosCortados;
        /// <summary>Nota de la ultima pelea terminada, y lo que se llevo por ella.</summary>
        public NotaPelea Nota;
        public int PremioBalas;
        /// <summary>Aciertos de disparo seguidos y los segundos que le quedan a la ventana para el retroceso de la horda.</summary>
        public int AciertosRecientes;
        public float VentanaAciertos;
        /// <summary>Segundos que le quedan al retroceso de la horda, y hasta cuando no se puede repetir.</summary>
        public float RetrocesoRestante;
        public float RetrocesoEnfriamiento;
        public float PremioBateria;
        /// <summary>El premio tenia balas (o bateria) pero ya estaba todo lleno: no entro nada.</summary>
        public bool PremioBalasLleno;
        public bool PremioBateriaLleno;
        /// <summary>Del ultimo aviso de "lleno": 0 = botiquin (vida llena), 1 = balas (cargador lleno).</summary>
        public int DropLlenoKind;
        /// <summary>Golpes que aguanta en la pelea. Solo cuenta mientras pelea.</summary>
        public int Vida;
        /// <summary>Segundos que lleva la pelea.</summary>
        public float PeleaTiempo;
        /// <summary>Bombas temporizadoras que uso en la pelea.</summary>
        public int PeleaBombas;
        /// <summary>Cuantas oleadas tiene la pelea, cuantos zombies tiene la que esta en curso y cuantos faltan vencer.</summary>
        public int Oleadas;
        public int OleadaTotal;
        public int ZombiesRestantes;
        /// <summary>Segundos hasta que entre la oleada que sigue (0 = la oleada esta en curso).</summary>
        public float OleadaRespiro;
        /// <summary>Se vencio la ultima oleada: falta ir a buscar el bidon que solto el ultimo zombie.</summary>
        public bool BidonEnElPiso;
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

        public float Gap => PlayerX - HordeX;
    }
}
