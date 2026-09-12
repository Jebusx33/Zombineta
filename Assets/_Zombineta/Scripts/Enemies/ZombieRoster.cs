using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Enemies
{
    /// <summary>La lista de tipos que puede tener la horda.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Zombie Roster", fileName = "Zombies")]
    public sealed class ZombieRoster : ScriptableObject
    {
        public List<ZombieType> types = new List<ZombieType>();

        /// <summary>
        /// El tipo que se usa cuando no hay roster: uno normal que muere siempre. Asi la
        /// simulacion anda sin asset, y los tests que no hablan de tipos siguen valiendo.
        /// </summary>
        public static readonly ZombieType Default =
            new ZombieType { name = "Comun", speedMultiplier = 1f, deathChance = 1f, staggerOnHit = 0f };

        public ZombieType Get(int index) =>
            types != null && index >= 0 && index < types.Count ? types[index] : Default;

        /// <summary>Elige un tipo segun los pesos, con el random de la horda: reproducible.</summary>
        public int Pick(System.Random rng)
        {
            if (types == null || types.Count == 0)
                return 0;

            float total = 0f;
            for (int i = 0; i < types.Count; i++)
                total += Mathf.Max(0f, types[i].spawnWeight);

            if (total <= 0f)
                return rng.Next(types.Count);

            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < types.Count; i++)
            {
                r -= Mathf.Max(0f, types[i].spawnWeight);
                if (r <= 0f)
                    return i;
            }
            return types.Count - 1;
        }
    }
}
