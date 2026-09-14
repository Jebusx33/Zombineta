using UnityEngine;

namespace Zombineta.Core
{
    public enum SortSlot { Shadow = 0, Item = 10, Zombie = 20, Player = 30, Effect = 40 }

    /// <summary>
    /// Orden de dibujo por carril: los personajes miden mas de dos carriles de alto, asi que lo
    /// del carril de abajo tiene que tapar a lo de arriba. El carril es continuo (el visual de un
    /// cambio de carril) para que el orden no salte a mitad de camino.
    /// </summary>
    public static class LaneSorting
    {
        public const int Base = 1000;
        public const int PerLane = 100;

        public static int Order(float visualLane, SortSlot slot) =>
            Base - Mathf.RoundToInt(visualLane * PerLane) + (int)slot;
    }
}
