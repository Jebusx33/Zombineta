using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>Tono objetivo del loop del motor: 0,8 quieta, 1,2 a velocidad normal, 1,45 con turbo.</summary>
    public static class MotorTono
    {
        public const float Quieta = 0.8f;
        public const float Normal = 1.2f;
        public const float Turbo = 1.45f;

        public static float Objetivo(float velocidad, float velocidadNormal, bool turbo)
        {
            if (turbo)
                return Turbo;
            float t = velocidadNormal <= 0f ? 0f : Mathf.Clamp01(velocidad / velocidadNormal);
            return Mathf.Lerp(Quieta, Normal, t);
        }
    }
}
