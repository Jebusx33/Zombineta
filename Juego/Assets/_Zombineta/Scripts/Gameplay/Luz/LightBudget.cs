using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>Una luz de adorno candidata a quedar prendida.</summary>
    public readonly struct LightCandidate
    {
        public readonly float x;
        public readonly int priority;

        public LightCandidate(float x, int priority)
        {
            this.x = x;
            this.priority = priority;
        }
    }

    /// <summary>
    /// Cuales de las luces de adorno quedan prendidas cuando hay mas de las que la maquina banca.
    /// Manda la prioridad y despues la cercania a la camara: lo que esta encima del jugador se ve,
    /// lo lejano se apaga y nadie lo nota. C# plano para poder testearlo.
    /// </summary>
    public static class LightBudget
    {
        static readonly List<int> order = new List<int>();

        public static void Choose(IReadOnlyList<LightCandidate> candidates, float cameraX, int budget, List<bool> result)
        {
            result.Clear();
            for (int i = 0; i < candidates.Count; i++)
                result.Add(false);

            if (budget <= 0 || candidates.Count == 0)
                return;

            order.Clear();
            for (int i = 0; i < candidates.Count; i++)
                order.Add(i);

            order.Sort((a, b) =>
            {
                int byPriority = candidates[b].priority.CompareTo(candidates[a].priority);
                if (byPriority != 0)
                    return byPriority;
                float da = Mathf.Abs(candidates[a].x - cameraX);
                float db = Mathf.Abs(candidates[b].x - cameraX);
                int byDistance = da.CompareTo(db);
                return byDistance != 0 ? byDistance : a.CompareTo(b);
            });

            int n = Mathf.Min(budget, order.Count);
            for (int i = 0; i < n; i++)
                result[order[i]] = true;
        }
    }
}
