namespace Zombineta.Audio
{
    /// <summary>Elige una variante al azar sin repetir la ultima. Semilla inyectable para tests.</summary>
    public sealed class VariantPicker
    {
        readonly System.Random rng;
        int last = -1;

        public VariantPicker(int seed) => rng = new System.Random(seed);

        public int Pick(int count)
        {
            if (count <= 0)
                return -1;
            if (count == 1)
                return last = 0;
            int pick = rng.Next(count - 1);
            if (last >= 0 && pick >= last)
                pick++;
            return last = pick;
        }
    }
}
