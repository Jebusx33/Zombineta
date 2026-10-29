using System;
using System.Collections.Generic;
using UnityEngine;
using Zombineta.Level;

namespace Zombineta.Juego.Levels
{
    /// <summary>Los numeros del armado automatico de un nivel. Viven en el propio nivel.</summary>
    [Serializable]
    public sealed class LevelGeneratorSettings
    {
        [Tooltip("Misma semilla, mismo nivel.")]
        public int seed = 1;

        [Tooltip("Metros libres al principio.")]
        public float startClear = 60f;

        [Tooltip("Metros libres antes del refugio.")]
        public float endClear = 150f;

        [Header("Obstaculos")]
        [Tooltip("Obstaculos sueltos cada 100 m (ademas de los de las rampas).")]
        public float obstaclesPer100m = 1.6f;

        [Tooltip("Separacion minima entre los centros de dos cosas del mismo carril, en metros. " +
                 "Ademas se respeta el ancho del dibujo de cada una (ver LevelFootprint).")]
        public float minGapSameLane = 2f;

        [Tooltip("Metros minimos entre obstaculos de carriles vecinos: los dibujos son mas altos " +
                 "que un carril y, pegados, no se sabe cual esta en cual.")]
        public float separacionEntreCarriles = 8f;

        [Header("Recursos (cada cuantos metros)")]
        public float fuelEvery = 120f;
        public float batteryEvery = 450f;
        public float ammoEvery = 380f;
        [Tooltip("Granadas (arma secundaria de la moto). 0 = ninguna.")]
        public float granadaEvery = 650f;
        [Tooltip("Bombas temporizadoras (para la pelea a pie). 0 = ninguna.")]
        public float bombaEvery = 1000f;

        [Header("Piezas (cada cuantos metros)")]
        public float rampEvery = 250f;
        public float barrelEvery = 200f;
        public float frontZombieEvery = 150f;

        [Range(0f, 1f)]
        [Tooltip("Probabilidad de que un zombie de frente sea pesado.")]
        public float heavyFrontZombieChance = 0.3f;
    }

    /// <summary>
    /// Arma un recorrido automatico que respeta las reglas del juego. C# plano y deterministico:
    /// misma semilla, mismo nivel. Recibe lo que ya esta fijado en la escena y no ubica nada
    /// encima; devuelve solo lo nuevo.
    ///
    /// Reglas: piezas de rampa con zona de aterrizaje libre; barriles fuera de las rampas;
    /// zombies de frente lejos de obstaculos de su carril; nunca tres carriles tapados; nada
    /// encimado en un mismo carril (segun el ancho de cada dibujo) ni escondido detras de un
    /// obstaculo del carril de abajo.
    ///
    /// Con tramos, cada uno multiplica las densidades en su pedazo del recorrido (0 = nada de
    /// eso ahi). Sin tramos el nivel sale parejo, como siempre.
    /// </summary>
    public static class LevelGenerator
    {
        const int Lanes = 3;

        // Ventana para "tres carriles tapados": mas ancha que la que se valida (2 m), asi dos
        // obstaculos lejanos entre si no pueden encerrar a un tercero.
        const float BlockWindow = 4f;

        // Zona de una pieza de rampa en su carril: desde un poco antes hasta pasado el aterrizaje.
        const float RampZoneBefore = 5f;
        const float RampZoneAfter = 50f;

        // La pieza de rampa, en metros desde la rampa. El coche tiene que quedar (1) lejos de la rampa para que
        // no se encimen los dibujos (la rampa mide casi 6 m y el coche 4,65: sus centros, a mas de 5,8 m) y
        // (2) dentro del vuelo: con el impulso de la rampa (rampBoostMultiplier 1,6) y la subida de
        // GameConfig.asset el salto sale de unos 10,7 m a velocidad normal, y el coche se sobrevuela hasta
        // unos 6,9 m (RampBoostTests lo mide). Es uno solo. Si cambia el salto o el impulso, revisar este numero,
        // LevelValidator.LandingFrom (el coche tiene que quedar antes) y RampBoostTests.CocheDeLaPieza.
        public const float RampObstacleAt = 6.1f;
        const float RampFuelAt = 3f;
        const float RampFuelHeight = 1f;

        // Cuando un tramo no quiere algo (densidad 0), se lo cruza de a pasos de estos metros.
        const float SkipStep = 20f;

        public static List<LevelEntry> Generate(LevelGeneratorSettings s, float length, IReadOnlyList<LevelEntry> pinned = null,
            IReadOnlyList<Tramo> tramos = null)
        {
            var result = new List<LevelEntry>();
            var all = new List<LevelEntry>();
            if (pinned != null)
                all.AddRange(pinned);

            float start = s.startClear;
            float end = length - s.endClear;
            if (end <= start)
                return result;

            var rng = new System.Random(s.seed);

            void Add(LevelEntry e)
            {
                result.Add(e);
                all.Add(e);
            }

            float Piezas(float d) { var t = Tramos.At(tramos, d); return t != null ? t.piezas : 1f; }
            float Recursos(float d) { var t = Tramos.At(tramos, d); return t != null ? t.recursos : 1f; }
            float Nafta(float d) { var t = Tramos.At(tramos, d); return t != null ? t.nafta : 1f; }

            // Avanza hasta el proximo lugar de algo que va "cada tantos metros": mas seguido donde
            // el tramo lo pide, y de largo donde no lo quiere.
            float Next(float d, float every, System.Func<float, float> density)
            {
                float m = density(d);
                return d + (m > 0f ? every / m : SkipStep);
            }

            // 1) Piezas de rampa: rampa, un obstaculo para sobrevolar y a veces un bidon aereo.
            int piece = 0;
            if (s.rampEvery > 0f)
            {
                for (float d = start + s.rampEvery; d + RampZoneAfter <= end; d = Next(d, s.rampEvery, Piezas))
                {
                    if (Piezas(d) <= 0f)
                        continue;
                    int lane = rng.Next(Lanes);
                    if (AnyInLane(all, lane, d - RampZoneBefore, d + RampZoneAfter))
                        continue;
                    if (WouldBlock(all, d + RampObstacleAt, lane) || HiddenByObstacle(all, d, lane, LevelEntryKind.Ramp) ||
                        WouldHide(all, d + RampObstacleAt, lane))
                        continue;

                    Add(new LevelEntry(d, lane, LevelEntryKind.Ramp));
                    Add(new LevelEntry(d + RampObstacleAt, lane, LevelEntryKind.Obstacle));
                    if (piece % 2 == 0)
                        Add(new LevelEntry(d + RampFuelAt, lane, LevelEntryKind.Fuel, RampFuelHeight));
                    piece++;
                }
            }

            // 2) Barriles: se pasan de largo y se les dispara despues. Fuera de las rampas.
            if (s.barrelEvery > 0f)
            {
                for (float d = start + s.barrelEvery * 0.5f; d <= end; d = Next(d, s.barrelEvery, Piezas))
                {
                    if (Piezas(d) <= 0f)
                        continue;
                    float at = Mathf.Round(d);
                    int lane = rng.Next(Lanes);
                    if (InAnyRampZone(all, at) || Occupied(all, at, lane, LevelEntryKind.Barrel, s.minGapSameLane) ||
                        HiddenByObstacle(all, at, lane, LevelEntryKind.Barrel))
                        continue;
                    Add(new LevelEntry(at, lane, LevelEntryKind.Barrel));
                }
            }

            // 3) Zombies de frente: comun o pesado, lejos de obstaculos de su carril.
            if (s.frontZombieEvery > 0f)
            {
                for (float d = start + s.frontZombieEvery; d <= end; d = Next(d, s.frontZombieEvery, Piezas))
                {
                    if (Piezas(d) <= 0f)
                        continue;
                    float at = Mathf.Round(d);
                    int lane = rng.Next(Lanes);
                    int variant = rng.NextDouble() < s.heavyFrontZombieChance ? 2 : 0;
                    if (InRampZone(all, at, lane) || Occupied(all, at, lane, LevelEntryKind.ZombieFront, s.minGapSameLane) ||
                        NearKind(all, at, lane, LevelEntryKind.Obstacle, 10f) || WouldBlock(all, at, lane) ||
                        HiddenByObstacle(all, at, lane, LevelEntryKind.ZombieFront))
                        continue;
                    Add(new LevelEntry(at, lane, LevelEntryKind.ZombieFront, 0f, variant));
                }
            }

            // 4) Obstaculos sueltos: cada tramo recibe los suyos, segun su largo y su densidad.
            int tramoCount = tramos != null && tramos.Count > 0 ? tramos.Count : 1;
            for (int i = 0; i < tramoCount; i++)
            {
                float from = start, to = end, density = 1f;
                if (tramos != null && tramos.Count > 0)
                {
                    from = Mathf.Max(start, Tramos.Desde(tramos, i));
                    // El ultimo tramo llega hasta el final aunque su "hasta" quede corto.
                    to = i == tramos.Count - 1 ? end : Mathf.Min(end, tramos[i].hasta);
                    density = tramos[i].obstaculos;
                }
                if (to <= from)
                    continue;

                int wanted = Mathf.RoundToInt((to - from) / 100f * s.obstaclesPer100m * density);
                for (int attempt = 0, placed = 0; attempt < wanted * 8 && placed < wanted; attempt++)
                {
                    float at = Mathf.Round(from + (float)rng.NextDouble() * (to - from));
                    int lane = rng.Next(Lanes);
                    if (InRampZone(all, at, lane) || Occupied(all, at, lane, LevelEntryKind.Obstacle, s.minGapSameLane) ||
                        NearKind(all, at, lane, LevelEntryKind.ZombieFront, 10f) || WouldBlock(all, at, lane) ||
                        WouldHide(all, at, lane) || HiddenByObstacle(all, at, lane, LevelEntryKind.Obstacle) ||
                        ObstaculoEnCarrilVecino(all, at, lane, s.separacionEntreCarriles))
                        continue;
                    Add(new LevelEntry(at, lane, LevelEntryKind.Obstacle));
                    placed++;
                }
            }

            // 5) Recursos, repartidos con un poco de azar.
            PlacePickups(LevelEntryKind.Fuel, s.fuelEvery, d => Recursos(d) * Nafta(d));
            PlacePickups(LevelEntryKind.Battery, s.batteryEvery, Recursos);
            PlacePickups(LevelEntryKind.Ammo, s.ammoEvery, Recursos);
            // Al final de los demas recursos: asi no cambian los lugares de lo que ya salia de una semilla.
            PlacePickups(LevelEntryKind.Granada, s.granadaEvery, Recursos);
            PlacePickups(LevelEntryKind.BombaTemporal, s.bombaEvery, Recursos);

            void PlacePickups(LevelEntryKind kind, float every, System.Func<float, float> density)
            {
                if (every <= 0f)
                    return;
                for (float d = start + every * 0.5f; d <= end; d = Next(d, every, density))
                {
                    if (density(d) <= 0f)
                        continue;
                    for (int tries = 0; tries < 4; tries++)
                    {
                        float jitter = ((float)rng.NextDouble() - 0.5f) * (every / density(d)) * 0.5f;
                        float at = Mathf.Round(Mathf.Clamp(d + jitter, start, end));
                        int lane = rng.Next(Lanes);
                        // El azar no lo puede correr hacia un tramo donde ese recurso no va (una sequia).
                        if (density(at) <= 0f)
                            continue;
                        // Debajo de un salto no se llega: fuera del tramo de la rampa hasta el aterrizaje corto.
                        if (UnderAJump(all, at, lane) || Occupied(all, at, lane, kind, s.minGapSameLane) ||
                            HiddenByObstacle(all, at, lane, kind))
                            continue;
                        Add(new LevelEntry(at, lane, kind));
                        break;
                    }
                }
            }

            result.Sort((a, b) => a.distance.CompareTo(b.distance));
            return result;
        }

        // --- Reglas ------------------------------------------------------------------

        static bool IsBlocker(LevelEntry e) =>
            e.height <= 0f && (e.kind == LevelEntryKind.Obstacle || e.kind == LevelEntryKind.ZombieFront);

        /// <summary>Si con algo en este carril quedarian los tres carriles tapados.</summary>
        static bool WouldBlock(List<LevelEntry> all, float at, int lane)
        {
            bool l0 = lane == 0, l1 = lane == 1, l2 = lane == 2;
            foreach (var e in all)
            {
                if (!IsBlocker(e) || Mathf.Abs(e.distance - at) > BlockWindow)
                    continue;
                if (e.lane == 0) l0 = true;
                else if (e.lane == 1) l1 = true;
                else l2 = true;
            }
            return l0 && l1 && l2;
        }

        static bool ObstaculoEnCarrilVecino(List<LevelEntry> all, float at, int lane, float separacion)
        {
            foreach (var e in all)
                if (e.kind == LevelEntryKind.Obstacle && e.height <= 0f && Mathf.Abs(e.lane - lane) == 1 &&
                    Mathf.Abs(e.distance - at) < separacion)
                    return true;
            return false;
        }

        /// <summary>Si algo del mismo carril, en el piso, tocaria a esta cosa segun el ancho de los dos dibujos.</summary>
        static bool Occupied(List<LevelEntry> all, float at, int lane, LevelEntryKind kind, float floor)
        {
            foreach (var e in all)
                if (e.lane == lane && e.height <= 0f &&
                    Mathf.Abs(e.distance - at) < LevelFootprint.MinDistance(e.kind, kind, floor))
                    return true;
            return false;
        }

        /// <summary>Si un obstaculo del carril de abajo taparia a esta cosa.</summary>
        static bool HiddenByObstacle(List<LevelEntry> all, float at, int lane, LevelEntryKind kind)
        {
            var item = new LevelEntry(at, lane, kind);
            foreach (var e in all)
                if (LevelFootprint.Hides(e, item))
                    return true;
            return false;
        }

        /// <summary>Si un obstaculo puesto aca taparia algo que ya esta colocado en el carril de arriba.</summary>
        static bool WouldHide(List<LevelEntry> all, float at, int lane)
        {
            var obstacle = new LevelEntry(at, lane, LevelEntryKind.Obstacle);
            foreach (var e in all)
                if (LevelFootprint.Hides(obstacle, e))
                    return true;
            return false;
        }

        static bool AnyInLane(List<LevelEntry> all, int lane, float from, float to)
        {
            foreach (var e in all)
                if (e.lane == lane && e.distance >= from && e.distance <= to)
                    return true;
            return false;
        }

        static bool NearKind(List<LevelEntry> all, float at, int lane, LevelEntryKind kind, float radius)
        {
            foreach (var e in all)
                if (e.kind == kind && e.lane == lane && Mathf.Abs(e.distance - at) < radius)
                    return true;
            return false;
        }

        static bool InRampZone(List<LevelEntry> all, float at, int lane)
        {
            foreach (var e in all)
                if (e.kind == LevelEntryKind.Ramp && e.lane == lane &&
                    at >= e.distance - RampZoneBefore && at <= e.distance + RampZoneAfter)
                    return true;
            return false;
        }

        static bool InAnyRampZone(List<LevelEntry> all, float at)
        {
            foreach (var e in all)
                if (e.kind == LevelEntryKind.Ramp && at >= e.distance - RampZoneBefore && at <= e.distance + RampZoneAfter)
                    return true;
            return false;
        }

        static bool UnderAJump(List<LevelEntry> all, float at, int lane)
        {
            foreach (var e in all)
                if (e.kind == LevelEntryKind.Ramp && e.lane == lane && at >= e.distance - 3f && at <= e.distance + 12f)
                    return true;
            return false;
        }
    }
}
