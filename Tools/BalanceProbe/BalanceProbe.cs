using System;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Player;

/// Corre partidas simuladas con los valores REALES de GameConfig.asset para ver
/// si el balance produce un juego jugable antes de construir un solo prefab.
public static class BalanceProbe
{
    const float Dt = 1f / 60f;

    static GameConfig RealConfig()
    {
        var c = ScriptableObject.CreateInstance<GameConfig>();
        c.goalDistance = 4000f;
        c.startingGap = 60f;
        c.laneSpacing = 1.6f;
        c.laneChangeDuration = 0.15f;
        c.normalSpeed = 12f;
        c.turboSpeedMultiplier = 1.8f;
        c.reverseSpeedMultiplier = -0.5f;
        c.fuelMax = 100f;
        c.fuelBurnPerSecond = 1.6f;
        c.turboBurnMultiplier = 3f;
        c.reverseBurnMultiplier = 0.5f;
        c.fuelPickupAmount = 25f;
        c.batteryMax = 100f;
        c.batteryDrainPerSecond = 8f;
        c.batteryPickupAmount = 40f;
        c.headlightHordeSlowFactor = 0.5f;
        c.ammoMax = 6;
        c.ammoAtStart = 3;
        c.ammoPickupAmount = 2;
        c.shotHordePushback = 15f;
        c.hordeBaseSpeed = 12.5f;
        c.rubberBandStartGap = 80f;
        c.rubberBandRange = 80f;
        c.rubberBandMaxBonus = 6f;
        c.crashStunDuration = 0.8f;
        c.crashFuelPenalty = 8f;
        return c;
    }

    /// <param name="fuelEvery">metros entre bidones; 0 = sin pickups</param>
    static void Run(string label, Func<RunState, PlayerIntent> brain,
                    float fuelEvery = 0f, float batteryEvery = 0f, float ammoEvery = 0f,
                    Action<GameConfig> tweak = null)
    {
        var cfg = RealConfig();
        tweak?.Invoke(cfg);
        var sim = new RunSimulation(cfg);
        float nextFuel = fuelEvery, nextBattery = batteryEvery, nextAmmo = ammoEvery;
        float minGap = float.MaxValue;

        for (int i = 0; i < 60 * 60 * 12 && sim.State.Phase == RunPhase.Running; i++)
        {
            sim.Tick(brain(sim.State), Dt);
            minGap = MathF.Min(minGap, sim.State.Gap);

            if (fuelEvery > 0f && sim.State.PlayerX >= nextFuel)
            {
                sim.ApplyPickup(PickupKind.Fuel);
                nextFuel += fuelEvery;
            }
            if (batteryEvery > 0f && sim.State.PlayerX >= nextBattery)
            {
                sim.ApplyPickup(PickupKind.Battery);
                nextBattery += batteryEvery;
            }
            if (ammoEvery > 0f && sim.State.PlayerX >= nextAmmo)
            {
                sim.ApplyPickup(PickupKind.Ammo);
                nextAmmo += ammoEvery;
            }
        }

        var s = sim.State;
        string outcome = s.Phase == RunPhase.Won
            ? "LLEGO"
            : s.Phase == RunPhase.Lost
                ? (s.Loss == LossReason.OutOfFuel ? "sin nafta" : "alcanzada")
                : "sigue";

        Console.WriteLine(
            $"  {label,-42} {outcome,-10} {s.PlayerX,6:0} m " +
            $"({sim.Progress01 * 100f,3:0}%)  {s.Elapsed,5:0} s  " +
            $"gap min {minGap,5:0} m  nafta {s.Fuel,5:0}");
    }

    public static void Report()
    {
        Console.WriteLine();
        Console.WriteLine("SONDA DE BALANCE (config real, meta 4000 m)");
        Console.WriteLine(new string('-', 100));

        Run("A. siempre Normal, sin pickups",
            _ => new PlayerIntent { Mode = DriveMode.Normal });

        Run("B. siempre Turbo, sin pickups",
            _ => new PlayerIntent { Mode = DriveMode.Turbo });

        Run("C. adaptativa, sin pickups", Adaptive);

        Run("D. adaptativa + nafta c/200 m", Adaptive, fuelEvery: 200f);

        Run("E. adaptativa + nafta 200 / bat 600 / balas 500", Adaptive,
            fuelEvery: 200f, batteryEvery: 600f, ammoEvery: 500f);

        Run("F. como E pero nafta c/250 m", Adaptive,
            fuelEvery: 250f, batteryEvery: 600f, ammoEvery: 500f);

        Console.WriteLine();
        Console.WriteLine("  -- misma dieta, jugadora que SI usa el faro --");

        Run("G. faro-first, nafta 200 / bat 600 / balas 500", LightFirst,
            fuelEvery: 200f, batteryEvery: 600f, ammoEvery: 500f);

        Run("H. faro-first, nafta c/250 m", LightFirst,
            fuelEvery: 250f, batteryEvery: 600f, ammoEvery: 500f);

        Run("I. faro-first, nafta c/300 m", LightFirst,
            fuelEvery: 300f, batteryEvery: 600f, ammoEvery: 500f);

        Console.WriteLine();
        Console.WriteLine("  -- AJUSTE: horda 13,5 m/s, gap inicial 40 m, goma elastica desde 25 m --");

        Action<GameConfig> tighter = null;
        tighter = c =>
        {
            c.hordeBaseSpeed = 13.5f;
            c.startingGap = 40f;
            c.rubberBandStartGap = 25f;
            c.rubberBandRange = 35f;
            c.rubberBandMaxBonus = 9f;
        };

        Run("J. ajustada, adaptativa, nafta 200", Adaptive,
            fuelEvery: 200f, batteryEvery: 600f, ammoEvery: 500f, tweak: tighter);

        Run("K. ajustada, faro-first, nafta 200", LightFirst,
            fuelEvery: 200f, batteryEvery: 600f, ammoEvery: 500f, tweak: tighter);

        Run("L. ajustada, faro-first, nafta 220", LightFirst,
            fuelEvery: 220f, batteryEvery: 500f, ammoEvery: 400f, tweak: tighter);

        Console.WriteLine();
        Console.WriteLine("  -- horda apretada + nafta mas generosa --");

        Action<GameConfig> tightAndFed = c =>
        {
            tighter(c);
            c.fuelPickupAmount = 35f;
        };
        Action<GameConfig> tightAndCheap = c =>
        {
            tighter(c);
            c.fuelBurnPerSecond = 1.25f;
        };

        Run("M. horda apretada + bidones de 35, adaptativa", Adaptive,
            fuelEvery: 200f, batteryEvery: 500f, ammoEvery: 400f, tweak: tightAndFed);
        Run("N. horda apretada + bidones de 35, faro-first", LightFirst,
            fuelEvery: 200f, batteryEvery: 500f, ammoEvery: 400f, tweak: tightAndFed);
        Run("O. horda apretada + consumo 1,25, adaptativa", Adaptive,
            fuelEvery: 200f, batteryEvery: 500f, ammoEvery: 400f, tweak: tightAndCheap);
        Run("P. horda apretada + consumo 1,25, faro-first", LightFirst,
            fuelEvery: 200f, batteryEvery: 500f, ammoEvery: 400f, tweak: tightAndCheap);

        Console.WriteLine(new string('-', 100));
        Console.WriteLine("Rendimiento por unidad de nafta:  Normal 7,5 m   Turbo 4,5 m");
        Console.WriteLine("El turbo compra distancia sobre la horda, pero cuesta autonomia.");
        Console.WriteLine();
    }

    /// Heuristica de una jugadora razonable: turbo cuando la horda aprieta,
    /// faro cuando aprieta mas, balas como ultimo recurso.
    static bool lightWasOn;

    static PlayerIntent Adaptive(RunState s)
    {
        var intent = new PlayerIntent { Mode = DriveMode.Normal };

        if (s.Gap < 45f)
            intent.Mode = DriveMode.Turbo;

        bool wantLight = s.Gap < 30f && s.Battery > 0f;
        if (wantLight != lightWasOn)
        {
            intent.ToggleHeadlight = true;
            lightWasOn = wantLight;
        }
        if (s.Battery <= 0f) lightWasOn = false;

        if (s.Gap < 12f && s.Ammo > 0)
            intent.Fire = true;

        return intent;
    }

    static bool lightWasOn2;

    /// Jugadora que entendio la economia: el faro frena a la horda sin gastar
    /// una gota de nafta, asi que se usa siempre que haya bateria. El turbo
    /// queda para las emergencias, porque es lo unico que cuesta autonomia.
    static PlayerIntent LightFirst(RunState s)
    {
        var intent = new PlayerIntent { Mode = DriveMode.Normal };

        bool wantLight = s.Battery > 0f && s.Gap < 70f;
        if (wantLight != lightWasOn2)
        {
            intent.ToggleHeadlight = true;
            lightWasOn2 = wantLight;
        }
        if (s.Battery <= 0f) lightWasOn2 = false;

        if (s.Gap < 25f)
            intent.Mode = DriveMode.Turbo;

        if (s.Gap < 12f && s.Ammo > 0)
            intent.Fire = true;

        return intent;
    }
}
