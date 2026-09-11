using System;
using UnityEngine;
using Zombineta.Player;

namespace Zombineta.Core
{
    /// <summary>Que paso durante un tick. Las Views leen esto para SFX y VFX.</summary>
    [Flags]
    public enum RunEvent
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

        /// <summary>Velocidad efectiva de la jugadora en el ultimo tick (m/s).</summary>
        public float PlayerSpeed { get; private set; }

        /// <summary>Velocidad efectiva de la horda en el ultimo tick (m/s).</summary>
        public float HordeSpeed { get; private set; }

        public float Progress01 =>
            config.goalDistance <= 0f ? 0f : Mathf.Clamp01(State.PlayerX / config.goalDistance);

        public RunSimulation(GameConfig config)
        {
            this.config = config != null
                ? config
                : throw new ArgumentNullException(nameof(config));
            Reset();
        }

        public void Reset()
        {
            State.PlayerX = 0f;
            State.HordeX = -config.startingGap;
            State.Lane = 1;
            State.LaneVisual = 1f;
            State.Fuel = config.fuelMax;
            State.Battery = config.batteryMax;
            State.Ammo = Mathf.Clamp(config.ammoAtStart, 0, config.ammoMax);
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
            PlayerSpeed = 0f;
            HordeSpeed = 0f;
        }

        public RunEvent Tick(PlayerIntent intent, float dt)
        {
            if (State.Phase != RunPhase.Running || dt <= 0f)
                return RunEvent.None;

            var events = RunEvent.None;

            events |= ApplyLaneChange(intent.LaneDelta);
            events |= ApplyHeadlightToggle(intent.ToggleHeadlight);
            events |= ApplyFire(intent.Fire);

            // El aturdimiento por choque corre aunque la moto este frenada.
            if (State.StunRemaining > 0f)
            {
                State.StunRemaining = Mathf.Max(0f, State.StunRemaining - dt);
                if (State.StunRemaining <= 0f)
                    State.Fallen = false;
            }

            if (State.BoostRemaining > 0f)
                State.BoostRemaining = Mathf.Max(0f, State.BoostRemaining - dt);

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

            HordeSpeed = ComputeHordeSpeed();
            State.HordeX += HordeSpeed * dt;

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

        RunEvent ApplyFire(bool fire)
        {
            if (!fire)
                return RunEvent.None;

            if (State.Ammo <= 0)
                return RunEvent.ShotDenied;

            State.Ammo--;
            // Empuje instantaneo: la pistola compra espacio, el faro compra tiempo.
            State.HordeX -= config.shotHordePushback;
            return RunEvent.Shot;
        }

        float ComputePlayerSpeed()
        {
            if (State.StunRemaining > 0f)
                return 0f;

            // Sin nafta la moto se para: la horda hace el resto.
            if (State.Fuel <= 0f)
                return 0f;

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

        float ComputeHordeSpeed()
        {
            float speed = config.hordeBaseSpeed;

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

            return speed;
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
            State.Pitch = config.launchPitch;
            State.LaunchSpin = config.launchSpinPerExcessSpeed *
                               Mathf.Max(0f, PlayerSpeed - config.normalSpeed);
            return RunEvent.Launched;
        }

        /// <summary>Si la moto esta volando a esa altura (con la tolerancia de los pickups aereos).</summary>
        public bool IsAtHeight(float meters) =>
            State.Airborne && Mathf.Abs(State.Height - meters) <= config.aerialPickupTolerance;
    }
}
