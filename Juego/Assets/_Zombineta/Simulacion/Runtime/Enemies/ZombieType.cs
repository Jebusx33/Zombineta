using System;
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
}
