using System;
using System.Collections.Generic;

namespace Zombineta.Scenery
{
    /// <summary>Un tile colocado en una capa, en coordenadas locales de esa capa.</summary>
    public struct TilePlacement
    {
        public float X;
        public float Width;
        public int Variant;

        public float End => X + Width;
    }

    /// <summary>
    /// Decide que tile va en cada lugar de una capa. C# plano: no conoce sprites ni
    /// escenas, solo anchos y pesos, asi que se prueba sin abrir Unity.
    ///
    /// El escenario se construye a medida que la camara lo necesita, pero siempre sale
    /// igual para la misma semilla: generar de a pedazos, frame a frame, da exactamente
    /// el mismo resultado que generar todo de una. Eso es lo que permite ir y volver en
    /// retroceso sin que los edificios cambien, y que dos playtests vean la misma calle.
    /// </summary>
    public sealed class SceneryLayout
    {
        // Tope de seguridad por llamada: un ancho mal configurado no cuelga el editor.
        const int MaxTilesPerCall = 10000;

        readonly float[] widths;
        readonly float[] weightPrefix;
        readonly float totalWeight;
        readonly int usableVariants;
        readonly float gapChance;
        readonly float gapMin;
        readonly float gapMax;
        readonly bool noImmediateRepeat;
        readonly Random rng;
        readonly List<TilePlacement> placed = new List<TilePlacement>();

        float cursor;
        int lastVariant = -1;

        /// <param name="widths">Ancho de cada variante. Una variante con ancho no positivo nunca se usa.</param>
        /// <param name="weights">Probabilidad relativa de cada variante.</param>
        /// <param name="gapChance">Probabilidad de dejar un hueco despues de cada tile (0 = contiguo).</param>
        /// <param name="gapMin">Hueco minimo, en unidades de la capa.</param>
        /// <param name="gapMax">Hueco maximo, en unidades de la capa.</param>
        /// <param name="noImmediateRepeat">Evita poner la misma variante dos veces seguidas.</param>
        /// <param name="seed">Semilla: misma semilla, mismo escenario.</param>
        /// <param name="startX">Donde arranca la capa. Tiene que quedar a la izquierda de todo lo visible.</param>
        public SceneryLayout(float[] widths, float[] weights, float gapChance, float gapMin,
                             float gapMax, bool noImmediateRepeat, int seed, float startX)
        {
            if (widths == null || weights == null || widths.Length != weights.Length)
                throw new ArgumentException("widths y weights tienen que tener el mismo largo");

            this.widths = widths;
            weightPrefix = new float[widths.Length];

            float acc = 0f;
            for (int i = 0; i < widths.Length; i++)
            {
                float w = widths[i] > 0f && weights[i] > 0f ? weights[i] : 0f;
                if (w > 0f) usableVariants++;
                acc += w;
                weightPrefix[i] = acc;
            }
            totalWeight = acc;

            this.gapChance = Math.Max(0f, Math.Min(1f, gapChance));
            this.gapMin = Math.Max(0f, Math.Min(gapMin, gapMax));
            this.gapMax = Math.Max(this.gapMin, gapMax);
            this.noImmediateRepeat = noImmediateRepeat;

            rng = new Random(seed);
            cursor = startX;
        }

        /// <summary>No hay ninguna variante utilizable: la capa queda vacia.</summary>
        public bool IsEmpty => usableVariants == 0;

        /// <summary>Hasta donde esta construida la capa.</summary>
        public float CoveredUntil => cursor;

        public IReadOnlyList<TilePlacement> Placed => placed;

        /// <summary>Construye tiles hasta cubrir maxX. Si ya estaba cubierto, no hace nada.</summary>
        public void EnsureCovered(float maxX)
        {
            if (IsEmpty)
                return;

            for (int n = 0; cursor < maxX && n < MaxTilesPerCall; n++)
            {
                int v = Pick();
                placed.Add(new TilePlacement { X = cursor, Width = widths[v], Variant = v });
                cursor += widths[v];
                lastVariant = v;

                if (gapChance > 0f && rng.NextDouble() < gapChance)
                    cursor += gapMin + (float)rng.NextDouble() * (gapMax - gapMin);
            }
        }

        /// <summary>
        /// Llena results con los tiles que se superponen con [min, max). Funciona igual
        /// hacia atras: lo ya construido queda guardado y se busca por posicion.
        /// </summary>
        public void Query(float min, float max, List<TilePlacement> results)
        {
            results.Clear();
            EnsureCovered(max);

            // Busqueda binaria del primer tile que termina despues de min.
            int lo = 0, hi = placed.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (placed[mid].End <= min) lo = mid + 1;
                else hi = mid;
            }

            for (int i = lo; i < placed.Count && placed[i].X < max; i++)
                results.Add(placed[i]);
        }

        int Pick()
        {
            int v = Roll();

            if (noImmediateRepeat && usableVariants > 1 && v == lastVariant)
            {
                // Unos reintentos al azar y, si la suerte insiste, la siguiente utilizable.
                // Todo sale del mismo generador, asi que sigue siendo deterministico.
                for (int tries = 0; tries < 4 && v == lastVariant; tries++)
                    v = Roll();

                if (v == lastVariant)
                    v = NextUsableAfter(v);
            }

            return v;
        }

        int Roll()
        {
            double r = rng.NextDouble() * totalWeight;
            for (int i = 0; i < weightPrefix.Length; i++)
                if (r < weightPrefix[i])
                    return i;

            return NextUsableAfter(-1);
        }

        int NextUsableAfter(int index)
        {
            for (int step = 1; step <= widths.Length; step++)
            {
                int i = (index + step + widths.Length) % widths.Length;
                float before = i == 0 ? 0f : weightPrefix[i - 1];
                if (weightPrefix[i] > before)
                    return i;
            }
            return 0;
        }
    }
}
