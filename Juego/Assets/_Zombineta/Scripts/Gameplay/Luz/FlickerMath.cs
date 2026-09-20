using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>
    /// El parpadeo de un neon o un farol viejo: dos ondas que no cierran entre si, para que no se
    /// note el ciclo, y un corrimiento por semilla para que dos carteles no titilen al unisono.
    /// </summary>
    public static class FlickerMath
    {
        public static float Intensity(float time, int seed, float frequency, float min, float max)
        {
            if (frequency <= 0f)
                return max;

            float phase = (seed * 0.6180339f) % 1f * 10f;
            float t = time * frequency + phase;
            float wave = Mathf.Sin(t) * 0.6f + Mathf.Sin(t * 1.7f) * 0.4f;   // en [-1, 1]
            return Mathf.Lerp(min, max, Mathf.InverseLerp(-1f, 1f, wave));
        }
    }
}
