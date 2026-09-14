namespace Zombineta.Enemies
{
    /// <summary>Que aspecto toma un zombie al reciclarse. Deterministico y sin repetir el anterior.</summary>
    public static class ZombieLookPicker
    {
        public static int Pick(int seed, int unit, int generation, int lookCount, int previous)
        {
            if (lookCount <= 0)
                return -1;
            if (lookCount == 1)
                return 0;

            uint h = 2166136261;
            unchecked
            {
                h = (h ^ (uint)seed) * 16777619;
                h = (h ^ (uint)unit) * 16777619;
                h = (h ^ (uint)generation) * 16777619;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
            }

            if (previous < 0 || previous >= lookCount)
                return (int)(h % (uint)lookCount);

            // Elige entre los demas: nunca el anterior.
            int i = (int)(h % (uint)(lookCount - 1));
            return i >= previous ? i + 1 : i;
        }
    }
}
