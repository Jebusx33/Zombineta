using System.Collections.Generic;
using UnityEngine;
using Zombineta.Level;

namespace Zombineta.Juego.Levels
{
    public enum LevelIssueKind
    {
        AllLanesBlocked,
        BlockedLanding,
        Overlapping,
    }

    /// <summary>Un problema del recorrido, en metros y carril (-1 = los tres carriles).</summary>
    public readonly struct LevelIssue
    {
        public readonly LevelIssueKind kind;
        public readonly float distance;
        public readonly int lane;

        public LevelIssue(LevelIssueKind kind, float distance, int lane)
        {
            this.kind = kind;
            this.distance = distance;
            this.lane = lane;
        }

        public string Message
        {
            get
            {
                switch (kind)
                {
                    case LevelIssueKind.AllLanesBlocked: return "Tres carriles tapados: no hay por donde pasar";
                    case LevelIssueKind.BlockedLanding: return "Algo en el aterrizaje de la rampa";
                    default: return "Dos objetos encimados";
                }
            }
        }
    }

    /// <summary>
    /// Revisa un recorrido armado a mano con las mismas reglas que respeta el generador. C# plano:
    /// lo usan las guias de la escena y los tests.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>Distancia a la que tres obstaculos en carriles distintos cierran el paso.</summary>
        public const float BlockWindow = 2f;

        /// <summary>Tramo de aterrizaje de una rampa: despues de los obstaculos que se sobrevuelan.</summary>
        public const float LandingFrom = 8.5f;
        public const float LandingTo = 50f;

        /// <summary>Mas cerca que esto, dos objetos del mismo carril y altura se pisan.</summary>
        public const float OverlapDistance = 1f;

        public static bool IsBlocker(LevelEntry e) =>
            e.height <= 0f && (e.kind == LevelEntryKind.Obstacle || e.kind == LevelEntryKind.ZombieFront);

        public static List<LevelIssue> Check(IReadOnlyList<LevelEntry> entries)
        {
            var issues = new List<LevelIssue>();
            var sorted = new List<LevelEntry>(entries);
            sorted.Sort((a, b) => a.distance.CompareTo(b.distance));

            // Tres carriles tapados: se avisa una vez por tramo.
            float lastBlocked = float.NegativeInfinity;
            for (int i = 0; i < sorted.Count; i++)
            {
                var b = sorted[i];
                if (!IsBlocker(b) || b.distance - lastBlocked <= BlockWindow * 2f)
                    continue;
                int mask = 0;
                for (int k = i; k < sorted.Count && sorted[k].distance - b.distance <= BlockWindow; k++)
                    if (IsBlocker(sorted[k]))
                        mask |= 1 << sorted[k].lane;
                if (mask == 0b111)
                {
                    issues.Add(new LevelIssue(LevelIssueKind.AllLanesBlocked, b.distance, -1));
                    lastBlocked = b.distance;
                }
            }

            // Aterrizajes de rampa.
            foreach (var ramp in sorted)
            {
                if (ramp.kind != LevelEntryKind.Ramp)
                    continue;
                foreach (var e in sorted)
                    if (IsBlocker(e) && e.lane == ramp.lane &&
                        e.distance > ramp.distance + LandingFrom && e.distance <= ramp.distance + LandingTo)
                        issues.Add(new LevelIssue(LevelIssueKind.BlockedLanding, e.distance, e.lane));
            }

            // Encimados: mismo carril, misma altura, casi la misma distancia.
            for (int i = 0; i < sorted.Count; i++)
                for (int k = i + 1; k < sorted.Count && sorted[k].distance - sorted[i].distance < OverlapDistance; k++)
                    if (sorted[k].lane == sorted[i].lane && Mathf.Abs(sorted[k].height - sorted[i].height) < 0.5f)
                        issues.Add(new LevelIssue(LevelIssueKind.Overlapping, sorted[k].distance, sorted[k].lane));

            issues.Sort((a, b) => a.distance.CompareTo(b.distance));
            return issues;
        }
    }
}
