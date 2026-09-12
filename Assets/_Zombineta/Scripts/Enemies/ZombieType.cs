using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Enemies
{
    /// <summary>
    /// Los numeros de una clase de zombie. Balancear la horda es editar estos valores en
    /// Settings/Zombies.asset: nunca tocar codigo.
    /// </summary>
    [Serializable]
    public sealed class ZombieType
    {
        public string name = "Comun";

        [Tooltip("Multiplica la velocidad base de la horda. Mas de 1 = se despega y va de puntero.")]
        public float speedMultiplier = 1f;

        [Range(0f, 1f)]
        [Tooltip("Probabilidad de morir por cada bala. Menos de 1 = a veces encaja el tiro y sigue.")]
        public float deathChance = 0.85f;

        [Tooltip("Segundos que queda frenado si aguanta el tiro.")]
        public float staggerOnHit = 0.6f;

        [Tooltip("Segundos que frena a la moto al arrollarlo de frente.")]
        public float ramStun = 0.1f;

        [Tooltip("Nafta que cuesta arrollarlo de frente.")]
        public float ramFuelPenalty = 0f;

        [Tooltip("Peso relativo con que aparece en la masa.")]
        public float spawnWeight = 60f;

        [Header("Presentacion")]
        public Color tint = Color.white;
        public float scale = 1f;
    }

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
