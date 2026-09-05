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
                State.StunRemaining = Mathf.Max(0f, State.StunRemaining - dt);

            bool hadFuel = State.Fuel > 0f;
            State.Mode = intent.Mode;

            PlayerSpeed = ComputePlayerSpeed();
            BurnFuel(dt);
            if (hadFuel && State.Fuel <= 0f)
                events |= RunEvent.RanOutOfFuel;

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
            if (delta == 0)
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

            switch (State.Mode)
            {
                case DriveMode.Turbo:
                    return config.normalSpeed * config.turboSpeedMultiplier;
                case DriveMode.Reverse:
                    return config.normalSpeed * config.reverseSpeedMultiplier;
                default:
                    return config.normalSpeed;
            }
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
    }
}
