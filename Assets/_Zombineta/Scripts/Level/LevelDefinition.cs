using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Level
{
    public enum LevelEntryKind
    {
        Obstacle,
        Fuel,
        Battery,
        Ammo,
        Ramp,
        Barrel,
        ZombieFront,
    }

    /// <summary>Una cosa colocada en el recorrido: a tantos metros, en tal carril.</summary>
    [Serializable]
    public struct LevelEntry
    {
        [Tooltip("Metros desde la largada.")]
        public float distance;

        [Range(0, 2)]
        [Tooltip("0 = carril de abajo, 1 = medio, 2 = arriba.")]
        public int lane;

        public LevelEntryKind kind;

        [Tooltip("Metros sobre el carril. 0 = en el piso. Mayor a 0: solo se agarra saltando.")]
        public float height;

        [Tooltip("Variante. En un zombie de frente, el indice de su tipo en Zombies.asset.")]
        public int variant;

        public LevelEntry(float distance, int lane, LevelEntryKind kind, float height = 0f, int variant = 0)
        {
            this.distance = distance;
            this.lane = lane;
            this.kind = kind;
            this.height = height;
            this.variant = variant;
        }
    }

    /// <summary>
    /// El recorrido completo, como datos. Disenar un nivel es editar esta lista:
    /// no hay que colocar objetos a mano en la escena ni tocar codigo.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public List<LevelEntry> entries = new List<LevelEntry>();

        public void SortByDistance() =>
            entries.Sort((a, b) => a.distance.CompareTo(b.distance));
    }
}
