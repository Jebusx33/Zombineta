using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>Cuanto brilla cada destello: color, intensidad y radio de arranque, y cuanto tarda en apagarse.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Destellos", fileName = "Destellos")]
    public sealed class FlashConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public FlashKind kind;
            public Color color = Color.white;
            [Min(0f)] public float intensity = 1f;
            [Min(0f)] public float radius = 2f;
            [Min(0.01f)] public float seconds = 0.1f;
        }

        public List<Entry> entries = new List<Entry>();

        public Entry Get(FlashKind kind)
        {
            foreach (var e in entries)
                if (e.kind == kind)
                    return e;
            return null;
        }

        public void Set(FlashKind kind, Color color, float intensity, float radius, float seconds)
        {
            var e = Get(kind);
            if (e == null)
                entries.Add(e = new Entry { kind = kind });
            e.color = color;
            e.intensity = intensity;
            e.radius = radius;
            e.seconds = seconds;
        }
    }
}
