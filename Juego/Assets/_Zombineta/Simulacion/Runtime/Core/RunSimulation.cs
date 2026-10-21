using System;
using UnityEngine;
using Zombineta.Enemies;
using Zombineta.Player;

namespace Zombineta.Core
{
    /// <summary>Que paso durante un tick. Las Views leen esto para SFX y VFX.</summary>
    [Flags]
    public enum RunEvent : long
    {
        None = 0,
        LaneChanged = 1 << 0,
        Shot = 1 << 1,
        ShotDenied = 1 << 2,
        HeadlightOn = 1 << 3,
        HeadlightOff = 1 << 4,
        RanOutOfFuel = 1 << 5,
        RanOutOfBattery = 1 << 6,
        Crashed = 1 << 7,
        PickedUp = 1 << 8,
        Won = 1 << 9,
        Lost = 1 << 10,
        Launched = 1 << 11,
        Landed = 1 << 12,
        LandedPerfect = 1 << 13,
        Fell = 1 << 14,
        ShotMissed = 1 << 15,
        RanOver = 1 << 16,
        Explosion = 1 << 17,
        /// <summary>Sin nafta, con ayudas: empieza la pelea a pie.</summary>
        PeleaEmpieza = 1 << 18,
        Golpe = 1 << 19,
        GolpeAlAire = 1 << 20,
        Lastimada = 1 << 21,
        // (los bits 22 y 23 eran la llegada y la partida de la furgoneta del auxilio: ya no existe)
        /// <summary>
        /// Recogio el bidon del ultimo zombie y vuelve a la moto con el tanque lleno. (Antes: la furgoneta paso.)
        /// </summary>
        AuxilioTermina = 1 << 23,
        /// <summary>El tercer golpe de la cadena: derriba a los que la rodean.</summary>
        Remate = 1 << 24,
        /// <summary>Empieza una oleada nueva de la pelea (el numero esta en State.Oleada).</summary>
        OleadaNueva = 1 << 25,
        /// <summary>La derribaron pero tenia un reintento: la pelea vuelve a empezar.</summary>
        PeleaReintenta = 1 << 26,
        /// <summary>Sin nafta y con ayuda: la moto empieza a frenar antes de que la protagonista se baje.</summary>
        Frenando = 1 << 27,
        /// <summary>Recogio un botiquin que solto un zombie: un corazon mas.</summary>
        BotiquinRecogido = 1 << 28,
        /// <summary>Recogio las balas que solto un zombie.</summary>
        BalasRecogidas = 1 << 29,
        /// <summary>Pisa un botiquin o balas que no necesita (vida o cargador llenos): el tipo esta en State.DropLlenoKind.</summary>
        DropLleno = 1L << 30,
        /// <summary>Dos aciertos seguidos: la horda retrocede un rato y le deja espacio.</summary>
        HordaRetrocede = 1L << 31,
        /// <summary>Lanzo una granada hacia atras desde la moto (el vuelo esta en State.GranadaVuelo).</summary>
        GranadaLanzada = 1L << 32,
        /// <summary>La granada cayo y exploto sobre la horda.</summary>
        GranadaExplota = 1L << 33,
        /// <summary>Quiso lanzar una granada y no tenia (o ya habia una en el aire).</summary>
        GranadaDenegada = 1L << 34,
        /// <summary>A pie, puso una bomba temporizadora: la mecha esta en State.BombaMecha.</summary>
        BombaPuesta = 1L << 35,
        /// <summary>La bomba exploto: cayeron todos los zombies que estaban peleando.</summary>
        BombaExplota = 1L << 36,
        /// <summary>Quiso poner una bomba y no tenia, ya habia una puesta o no habia a quien explotar.</summary>
        BombaDenegada = 1L << 37,
        /// <summary>Vencio la ultima oleada: el ultimo zombie suelta un bidon y hay que ir a buscarlo.</summary>
        PeleaGanada = 1L << 38,
    }

    /// <summary>
    /// Todas las reglas del juego. Deliberadamente sin MonoBehaviour: se
    /// instancia, se le hace Tick con un dt fijo y se verifica el resultado.
    /// Nada aca depende de una escena, de MCP ni del editor abierto.
    /// </summary>
    public sealed class RunSimulation
    {
        public const int LaneCount = 3;

        readonly GameConfig config;

        public RunState State { get; } = new RunState();

        /// <summary>La horda como individuos. El frente sale de aca.</summary>
        public HordeSimulation Horde { get; }

        /// <summary>La pelea a pie mientras llega el auxilio.</summary>
        public Zombineta.Auxilio.PeleaSimulation Pelea { get; }

        public GameConfig Config => config;

        IBarrelField barrels;

        /// <summary>
        /// Los barriles del recorrido. Lo setea RunController; en tests puede ser null. En la moto no se
        /// les dispara: solo sirven en la pelea a pie, donde explotan los que quedaron en la arena.
        /// </summary>
        public IBarrelField Barrels
        {
            get => barrels;
            set
            {
                barrels = value;
                Pelea.Barrels = value;
            }
        }

        /// <summary>Velocidad efectiva de la jugadora en el ultimo tick (m/s).</summary>
        public float PlayerSpeed { get; private set; }

        /// <summary>Velocidad efectiva de la horda en el ultimo tick (m/s).</summary>
        public float HordeSpeed { get; private set; }

        /// <summary>Cuanto aprieta la horda en el tramo donde esta la moto (1 = lo de siempre).</summary>
        public float Presion =>
            Zombineta.Level.Tramos.PresionAt(config.tramos, State.PlayerX, config.tramoPressureBlendMeters);

        /// <summary>Oscuridad del tramo donde esta la moto: 0 luz normal, 1 apagon.</summary>
        public float Oscuridad =>
            Zombineta.Level.Tramos.OscuridadAt(config.tramos, State.PlayerX, config.tramoDarknessFadeMeters);

        public float Progress01 =>
            config.goalDistance <= 0f ? 0f : Mathf.Clamp01(State.PlayerX / config.goalDistance);

        public RunSimulation(GameConfig config)
        {
            this.config = config != null
                ? config
                : throw new ArgumentNullException(nameof(config));
            Horde = new HordeSimulation(this.config, this.config.zombies, this.config.hordeSeed);
            Pelea = new Zombineta.Auxilio.PeleaSimulation(this.config, State, Horde, this.config.hordeSeed + 1);
            Pelea.Run = this;
            Reset();
        }

        public void Reset()
        {
            State.PlayerX = 0f;
            Horde.Reset(-config.startingGap);
            State.HordeX = Horde.FrontX;
            State.Lane = 1;
            State.LaneVisual = 1f;
            State.Fuel = config.fuelInicial > 0f ? Mathf.Min(config.fuelInicial, config.fuelMax) : config.fuelMax;
            State.Battery = config.batteryMax;
            State.Ammo = Mathf.Clamp(config.ammoAtStart, 0, config.ammoMax);
            State.Granadas = Mathf.Clamp(config.granadaInicial, 0, config.granadaMax);
            State.Bombas = Mathf.Clamp(config.bombaInicial, 0, config.bombaMax);
            State.GranadaVuelo = 0f;
            State.GranadaX = 0f;
            State.BombaMecha = 0f;
            State.BombaX = 0f;
            State.UltimaExplosionMuertos = 0;
            Horde.SinRegenerar = 0f;
            State.HeadlightOn = false;
            State.Mode = DriveMode.Normal;
            State.StunRemaining = 0f;
            State.Elapsed = 0f;
            State.Phase = RunPhase.Running;
            State.Loss = LossReason.None;
            State.Airborne = false;
            State.Height = 0f;
            State.VerticalSpeed = 0f;
            State.AirSpeed = 0f;
            State.Pitch = 0f;
            State.LaunchSpin = 0f;
            State.BoostRemaining = 0f;
            State.Fallen = false;
            State.Ayudas = config.ayudas;
            State.AyudasUsadas = 0;
            State.FrenadoRestante = -1f;
            State.VelocidadAlFrenar = 0f;
            State.ArranqueRestante = 0f;
            State.Oleada = 0;
            State.ReintentosPelea = 0;
            State.VidaMax = 0;
            State.PeleaBajas = 0;
            State.PeleaGolpesRecibidos = 0;
            State.PeleaRemates = 0;
            State.PeleaBotiquines = 0;
            State.PeleaSaltosAcertados = 0;
            State.PeleaSaltosEsquivados = 0;
            State.PeleaSaltosCortados = 0;
            State.PeleaBombas = 0;
            State.PeleaTiempo = 0f;
            State.OleadaTotal = 0;
            State.Oleadas = 0;
            State.ZombiesRestantes = 0;
            State.OleadaRespiro = 0f;
            State.BidonEnElPiso = false;
            State.Nota = NotaPelea.C;
            State.PremioBalas = 0;
            State.PremioBateria = 0f;
            State.AciertosRecientes = 0;
            State.VentanaAciertos = 0f;
            State.RetrocesoRestante = 0f;
            State.RetrocesoEnfriamiento = 0f;
            Horde.RetrocesoVelocidad = 0f;
            State.PremioBalasLleno = false;
            State.PremioBateriaLleno = false;
            State.DropLlenoKind = 0;
            State.Vida = 0;
            State.PeatonX = 0f;
            State.Mira = 1;
            State.Invulnerable = 0f;
            State.GolpeEnfriamiento = 0f;
            State.Golpeando = 0f;
            PlayerSpeed = 0f;
            HordeSpeed = 0f;
        }

        public RunEvent Tick(PlayerIntent intent, float dt)
        {
            if (dt <= 0f)
                return RunEvent.None;

            // A pie, esperando el auxilio: la pelea tiene sus propias reglas.
            if (State.Phase == RunPhase.Pelea)
            {
                Horde.BeginTick();
                PlayerSpeed = 0f;
                // LaneVisual la maneja la pelea: a pie camina libre entre los carriles.
                var pelea = Pelea.Step(intent, dt);
                State.Elapsed += dt;
                HordeSpeed = 0f;
                return pelea;
            }

            if (State.Phase != RunPhase.Running)
                return RunEvent.None;

            var events = RunEvent.None;

            // Lo primero: la horda arranca el tick con la lista de eventos limpia, asi lo que
            // genere el disparo llega entero a las vistas.
            Horde.BeginTick();

            events |= ApplyLaneChange(intent.LaneDelta);
            events |= ApplyHeadlightToggle(intent.ToggleHeadlight);
            events |= ApplyFire(intent.Fire);
            events |= ApplyGranada(intent.Granada);

            // El aturdimiento por choque corre aunque la moto este frenada.
            if (State.StunRemaining > 0f)
            {
                State.StunRemaining = Mathf.Max(0f, State.StunRemaining - dt);
                if (State.StunRemaining <= 0f)
                    State.Fallen = false;
            }

            if (State.BoostRemaining > 0f)
                State.BoostRemaining = Mathf.Max(0f, State.BoostRemaining - dt);

            if (State.ArranqueRestante > 0f)
                State.ArranqueRestante = Mathf.Max(0f, State.ArranqueRestante - dt);
            if (State.FrenadoRestante > 0f)
                State.FrenadoRestante = Mathf.Max(0f, State.FrenadoRestante - dt);

            bool hadFuel = State.Fuel > 0f;
            State.Mode = intent.Mode;

            if (State.Airborne)
            {
                // Sin traccion: la velocidad es la de la rampa y el motor no gasta.
                PlayerSpeed = State.AirSpeed;
                events |= StepAir(intent.Mode, dt);
            }
            else
            {
                PlayerSpeed = ComputePlayerSpeed();
                BurnFuel(dt);
                if (hadFuel && State.Fuel <= 0f)
                    events |= RunEvent.RanOutOfFuel;
            }

            events |= DrainBattery(dt);

            State.PlayerX = Mathf.Max(0f, State.PlayerX + PlayerSpeed * dt);
            events |= AvanzarGranada(dt);

            Horde.RetrocesoVelocidad = State.RetrocesoRestante > 0f && config.retrocesoSegundos > 0f
                ? config.retrocesoMetros / config.retrocesoSegundos
                : 0f;
            float factorHorda = ComputeHordeSpeedFactor();
            // Frenando por falta de nafta, la horda acompana a la moto: no se le acerca a menos de frenadoGapHorda.
            if (State.FrenadoRestante > 0f && config.frenadoGapHorda > 0f && config.hordeBaseSpeed > 0f &&
                State.Gap < config.frenadoGapHorda + 4f)
                factorHorda = Mathf.Min(factorHorda, PlayerSpeed / config.hordeBaseSpeed);
            Horde.Step(dt, factorHorda);
            State.HordeX = Horde.FrontX;
            HordeSpeed = Horde.FrontSpeed;
            AvanzarRetroceso(dt);

            AdvanceLaneVisual(dt);
            State.Elapsed += dt;

            events |= CheckEndConditions();
            return events;
        }

        // --- Reglas ---------------------------------------------------------

        RunEvent ApplyLaneChange(int delta)
        {
            // En el aire no hay de donde agarrarse para cambiar de carril.
            if (delta == 0 || State.Airborne)
                return RunEvent.None;

            int target = Mathf.Clamp(State.Lane + Math.Sign(delta), 0, LaneCount - 1);
            if (target == State.Lane)
                return RunEvent.None;

            State.Lane = target;
            return RunEvent.LaneChanged;
        }

        RunEvent ApplyHeadlightToggle(bool toggle)
        {
            if (!toggle)
                return RunEvent.None;

            // Sin bateria no se puede prender.
            if (!State.HeadlightOn && State.Battery <= 0f)
                return RunEvent.None;

            State.HeadlightOn = !State.HeadlightOn;
            return State.HeadlightOn ? RunEvent.HeadlightOn : RunEvent.HeadlightOff;
        }

        /// <summary>
        /// Un disparo dio en un zombie. Con los aciertos de la ventana, la horda retrocede unos
        /// metros durante unos segundos: espacio seguro para quien viene disparando. Despues hay que
        /// esperar el enfriamiento para que vuelva a pasar.
        /// </summary>
        RunEvent RegistrarAcierto()
        {
            if (config.retrocesoAciertos <= 0 || config.retrocesoMetros <= 0f)
                return RunEvent.None;

            if (State.AciertosRecientes == 0)
                State.VentanaAciertos = config.retrocesoVentana;
            State.AciertosRecientes++;

            if (State.AciertosRecientes < config.retrocesoAciertos || State.RetrocesoEnfriamiento > 0f)
                return RunEvent.None;

            State.RetrocesoRestante = config.retrocesoSegundos;
            State.RetrocesoEnfriamiento = config.retrocesoEnfriamiento;
            State.AciertosRecientes = 0;
            State.VentanaAciertos = 0f;
            return RunEvent.HordaRetrocede;
        }

        void AvanzarRetroceso(float dt)
        {
            if (State.VentanaAciertos > 0f)
            {
                State.VentanaAciertos -= dt;
                if (State.VentanaAciertos <= 0f)
                {
                    State.VentanaAciertos = 0f;
                    State.AciertosRecientes = 0;
                }
            }
            State.RetrocesoRestante = Mathf.Max(0f, State.RetrocesoRestante - dt);
            State.RetrocesoEnfriamiento = Mathf.Max(0f, State.RetrocesoEnfriamiento - dt);
        }

        /// <summary>
        /// Lanza una granada hacia atras: vuela unos instantes y explota a granadaDistancia metros de la
        /// moto sobre la horda, con el alcance de un barril. Una sola en el aire a la vez.
        /// </summary>
        RunEvent ApplyGranada(bool lanzar)
        {
            if (!lanzar)
                return RunEvent.None;
            if (State.Granadas <= 0 || State.GranadaVuelo > 0f)
                return RunEvent.GranadaDenegada;

            State.Granadas--;
            State.GranadaVuelo = Mathf.Max(0.01f, config.granadaVuelo);
            State.GranadaX = State.PlayerX - config.granadaDistancia;
            State.GranadaDir = -1;
            return RunEvent.GranadaLanzada;
        }

        /// <summary>
        /// La granada en el aire cae siempre a granadaDistancia metros por detras de la moto. Al llegar
        /// explota, y la horda no repone a los caidos durante hordaSinRegenerar segundos.
        /// </summary>
        RunEvent AvanzarGranada(float dt)
        {
            if (State.GranadaVuelo <= 0f)
                return RunEvent.None;

            State.GranadaVuelo -= dt;
            State.GranadaX = State.PlayerX - config.granadaDistancia;
            if (State.GranadaVuelo > 0f)
                return RunEvent.None;

            State.GranadaVuelo = 0f;
            var ev = Explode(State.GranadaX, State.Lane) | RunEvent.GranadaExplota;
            Horde.SinRegenerar = Mathf.Max(Horde.SinRegenerar, config.hordaSinRegenerar);
            return ev;
        }

        RunEvent ApplyFire(bool fire)
        {
            if (!fire)
                return RunEvent.None;

            if (State.Ammo <= 0)
                return RunEvent.ShotDenied;

            State.Ammo--;

            float from = State.PlayerX;
            int lane = State.Lane;
            float range = config.shotRangeMeters;

            // Pega en el primer zombie que encuentra hacia atras. (Los barriles ya no explotan en la moto:
            // solo en la pelea a pie.)
            var zombie = Horde.NearestBehind(from, lane, range);

            if (zombie != null)
            {
                Horde.HitZombie(zombie, from);
                State.HordeX = Horde.FrontX;
                return RunEvent.Shot | RegistrarAcierto();
            }

            Horde.Miss(from, lane, range);
            return RunEvent.Shot | RunEvent.ShotMissed;
        }

        float ComputePlayerSpeed()
        {
            if (State.StunRemaining > 0f)
                return 0f;

            // Sin nafta la moto se para: la horda hace el resto. Con auxilio disponible no se clava de
            // golpe: sigue avanzando con el motor tosiendo y va frenando hasta que la protagonista se baja.
            if (State.Fuel <= 0f)
            {
                if (State.FrenadoRestante > 0f && config.paradaSegundos > 0f)
                {
                    float k = Mathf.Clamp01(State.FrenadoRestante / config.paradaSegundos);
                    return State.VelocidadAlFrenar * k * k;
                }
                return 0f;
            }

            float speed;
            switch (State.Mode)
            {
                case DriveMode.Turbo:
                    speed = config.normalSpeed * config.turboSpeedMultiplier;
                    break;
                case DriveMode.Reverse:
                    speed = config.normalSpeed * config.reverseSpeedMultiplier;
                    break;
                default:
                    speed = config.normalSpeed;
                    break;
            }

            // El impulso del aterrizaje perfecto solo empuja hacia adelante.
            if (State.BoostRemaining > 0f && speed > 0f)
                speed *= config.landingBoostMultiplier;

            // Despues del auxilio arranca de a poco, no a velocidad completa de un cuadro al otro.
            if (State.ArranqueRestante > 0f && config.arranqueSegundos > 0f && speed > 0f)
            {
                float avance = 1f - Mathf.Clamp01(State.ArranqueRestante / config.arranqueSegundos);
                speed *= avance * avance * (3f - 2f * avance);
            }

            return speed;
        }

        void BurnFuel(float dt)
        {
            if (State.Fuel <= 0f || State.StunRemaining > 0f)
                return;

            float multiplier;
            switch (State.Mode)
            {
                case DriveMode.Turbo:
                    multiplier = config.turboBurnMultiplier;
                    break;
                case DriveMode.Reverse:
                    multiplier = config.reverseBurnMultiplier;
                    break;
                default:
                    multiplier = 1f;
                    break;
            }

            State.Fuel = Mathf.Max(0f, State.Fuel - config.fuelBurnPerSecond * multiplier * dt);
        }

        RunEvent DrainBattery(float dt)
        {
            if (!State.HeadlightOn)
                return RunEvent.None;

            State.Battery = Mathf.Max(0f, State.Battery - config.batteryDrainPerSecond * dt);
            if (State.Battery > 0f)
                return RunEvent.None;

            State.HeadlightOn = false;
            return RunEvent.RanOutOfBattery | RunEvent.HeadlightOff;
        }

        /// <summary>
        /// Cuanto se multiplica la velocidad base de cada zombie. Cada tipo le suma lo suyo.
        /// </summary>
        float ComputeHordeSpeedFactor()
        {
            // El tramo del nivel marca el ritmo: respiro al principio, apreton en el giro.
            float speed = config.hordeBaseSpeed * Presion;

            // Goma elastica: si te escapaste mucho, la horda aprieta.
            if (config.rubberBandRange > 0f)
            {
                float t = Mathf.Clamp01(
                    (State.Gap - config.rubberBandStartGap) / config.rubberBandRange);
                speed += t * config.rubberBandMaxBonus;
            }

            // El faro frena a la horda mientras este prendido: efecto sostenido.
            if (State.HeadlightOn)
                speed *= config.headlightHordeSlowFactor;

            // Piedad de ultimo metro: con la horda encima afloja un poco, sin dejar de alcanzar.
            if (config.mercyGap > 0f && State.Gap < config.mercyGap)
                speed *= Mathf.Lerp(config.mercySlowFactor, 1f, Mathf.Clamp01(State.Gap / config.mercyGap));

            return config.hordeBaseSpeed <= 0f ? 0f : speed / config.hordeBaseSpeed;
        }

        void AdvanceLaneVisual(float dt)
        {
            if (config.laneChangeDuration <= 0f)
            {
                State.LaneVisual = State.Lane;
                return;
            }

            float step = dt / config.laneChangeDuration;
            State.LaneVisual = Mathf.MoveTowards(State.LaneVisual, State.Lane, step);
        }

        RunEvent StepAir(DriveMode lean, float dt)
        {
            // En el aire A/D inclinan: retroceso levanta la nariz, turbo la baja.
            float input = lean == DriveMode.Reverse ? 1f : lean == DriveMode.Turbo ? -1f : 0f;
            State.Pitch = Mathf.Clamp(
                State.Pitch + (State.LaunchSpin + input * config.leanRate) * dt,
                -config.maxPitch, config.maxPitch);

            // Nariz arriba planea, nariz abajo cae antes: asi se regula el largo del salto.
            float lift = config.leanLift * Mathf.Sin(State.Pitch * Mathf.Deg2Rad);
            State.VerticalSpeed -= config.jumpGravity * (1f - lift) * dt;
            State.Height += State.VerticalSpeed * dt;

            return State.Height > 0f ? RunEvent.None : Land();
        }

        RunEvent Land()
        {
            float angle = Mathf.Abs(State.Pitch);

            State.Airborne = false;
            State.Height = 0f;
            State.VerticalSpeed = 0f;
            State.Pitch = 0f;
            State.LaunchSpin = 0f;

            if (angle <= config.perfectLandingAngle)
            {
                State.BoostRemaining = config.landingBoostDuration;
                return RunEvent.Landed | RunEvent.LandedPerfect;
            }

            if (angle <= config.safeLandingAngle)
                return RunEvent.Landed;

            // Aterrizo torcida: al piso. La horda hace el resto.
            State.StunRemaining = config.fallStunDuration;
            State.Fuel = Mathf.Max(0f, State.Fuel - config.fallFuelPenalty);
            State.Fallen = true;
            return RunEvent.Fell;
        }

        RunEvent CheckEndConditions()
        {
            // Sin nafta y con ayudas: no se pierde, se pide el auxilio y se pelea a pie. Antes la moto
            // frena de a poco (paradaSegundos): se ve que se queda sin nafta y despues se baja.
            if (State.Fuel <= 0f && !State.Airborne && Pelea.PuedeEmpezar)
            {
                if (config.paradaSegundos <= 0f)
                    return Pelea.Empezar();

                if (State.FrenadoRestante < 0f)
                {
                    State.FrenadoRestante = config.paradaSegundos;
                    State.VelocidadAlFrenar = PlayerSpeed;
                    return RunEvent.Frenando;
                }
                if (State.FrenadoRestante <= 0f)
                    return Pelea.Empezar();

                // Todavia frenando: la horda no la pierde en este rato, pero si la inercia la lleva a la meta, gana.
                if (State.PlayerX >= config.goalDistance)
                {
                    State.FrenadoRestante = -1f;
                    State.Phase = RunPhase.Won;
                    return RunEvent.Won;
                }
                return RunEvent.None;
            }
            if (State.FrenadoRestante >= 0f && State.Fuel > 0f)
                State.FrenadoRestante = -1f;   // llego a un bidon a tiempo

            if (State.HordeX >= State.PlayerX)
            {
                State.Phase = RunPhase.Lost;
                State.Loss = State.Fuel <= 0f ? LossReason.OutOfFuel : LossReason.CaughtByHorde;
                return RunEvent.Lost;
            }

            if (State.PlayerX >= config.goalDistance)
            {
                State.Phase = RunPhase.Won;
                return RunEvent.Won;
            }

            return RunEvent.None;
        }

        // --- Eventos disparados por triggers de la escena --------------------

        public RunEvent ApplyPickup(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Fuel:
                    State.Fuel = Mathf.Min(config.fuelMax, State.Fuel + config.fuelPickupAmount);
                    break;
                case PickupKind.Battery:
                    State.Battery = Mathf.Min(
                        config.batteryMax, State.Battery + config.batteryPickupAmount);
                    break;
                case PickupKind.Ammo:
                    State.Ammo = Mathf.Min(config.ammoMax, State.Ammo + config.ammoPickupAmount);
                    break;
                case PickupKind.Grenade:
                    State.Granadas = Mathf.Min(config.granadaMax, State.Granadas + 1);
                    break;
                case PickupKind.Bomb:
                    State.Bombas = Mathf.Min(config.bombaMax, State.Bombas + 1);
                    break;
            }

            return RunEvent.PickedUp;
        }

        /// <summary>Chocar no mata: frena y cuesta nafta. La horda hace el trabajo sucio.</summary>
        public RunEvent ApplyCrash()
        {
            State.StunRemaining = config.crashStunDuration;
            State.Fuel = Mathf.Max(0f, State.Fuel - config.crashFuelPenalty);
            return RunEvent.Crashed;
        }

        /// <summary>
        /// La moto piso una rampa: sale volando con la velocidad que traia. De reversa o
        /// frenada no pasa nada. Lo llama LevelRuntime al resolver el tramo recorrido.
        /// </summary>
        public RunEvent Launch()
        {
            if (State.Airborne || State.Phase != RunPhase.Running || PlayerSpeed <= 0f)
                return RunEvent.None;

            State.Airborne = true;
            State.Height = 0f;
            State.AirSpeed = PlayerSpeed;
            State.VerticalSpeed = config.rampLaunchSlope * PlayerSpeed;
            if (config.maxLaunchVerticalSpeed > 0f)
                State.VerticalSpeed = Mathf.Min(State.VerticalSpeed, config.maxLaunchVerticalSpeed);
            State.Pitch = config.launchPitch;
            State.LaunchSpin = config.launchSpinPerExcessSpeed *
                               Mathf.Max(0f, PlayerSpeed - config.normalSpeed);
            return RunEvent.Launched;
        }

        /// <summary>Si la moto esta volando a esa altura (con la tolerancia de los pickups aereos).</summary>
        public bool IsAtHeight(float meters) =>
            State.Airborne && Mathf.Abs(State.Height - meters) <= config.aerialPickupTolerance;

        /// <summary>Un barril explota: mata a la horda en el radio. Lo llama LevelRuntime.</summary>
        public RunEvent Explode(float x, int lane)
        {
            State.UltimaExplosionMuertos = Horde.Explode(x, lane);
            State.HordeX = Horde.FrontX;
            return RunEvent.Explosion;
        }

        /// <summary>
        /// La moto arrollo a un zombie que venia de frente. Cuesta lo que diga su tipo: al
        /// comun te lo llevas puesto, el pesado es como chocar un auto.
        /// </summary>
        public RunEvent RunOver(int type, float x, int lane)
        {
            var t = Horde.Type(type);
            if (t.ramStun > 0f)
                State.StunRemaining = Mathf.Max(State.StunRemaining, t.ramStun);
            if (t.ramFuelPenalty > 0f)
                State.Fuel = Mathf.Max(0f, State.Fuel - t.ramFuelPenalty);

            Horde.ReportRunOver(x, lane, type);
            return RunEvent.RanOver;
        }
    }
}
