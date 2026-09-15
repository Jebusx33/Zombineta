using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Fx
{
    /// <summary>Cuanto vibra cada cosa: motor grave (low), agudo (high) y duracion.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Vibracion", fileName = "Rumble")]
    public sealed class RumbleConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public RumbleKind kind;
            [Range(0f, 1f)] public float low;
            [Range(0f, 1f)] public float high;
            [Min(0.01f)] public float seconds = 0.2f;
        }

        public List<Entry> entries = new List<Entry>();

        public Entry Get(RumbleKind kind)
        {
            foreach (var e in entries)
                if (e.kind == kind)
                    return e;
            return null;
        }

        public void Set(RumbleKind kind, float low, float high, float seconds)
        {
            var e = Get(kind);
            if (e == null)
                entries.Add(e = new Entry { kind = kind });
            e.low = low;
            e.high = high;
            e.seconds = seconds;
        }
    }
}
