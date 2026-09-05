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

        public LevelEntry(float distance, int lane, LevelEntryKind kind)
        {
            this.distance = distance;
            this.lane = lane;
            this.kind = kind;
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
