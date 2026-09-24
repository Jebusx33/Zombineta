using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>
    /// Tono objetivo del loop del motor. Por defecto 0,8 quieta, 1,2 a velocidad normal y 1,45
    /// con turbo; los valores reales salen de MezclaAudio.
    /// </summary>
    public static class MotorTono
    {
        public const float Quieta = 0.8f;
        public const float Normal = 1.2f;
        public const float Turbo = 1.45f;

        public static float Objetivo(float velocidad, float velocidadNormal, bool turbo) =>
            Objetivo(velocidad, velocidadNormal, turbo, Quieta, Normal, Turbo);

        public static float Objetivo(float velocidad, float velocidadNormal, bool turbo,
                                     float tonoQuieta, float tonoNormal, float tonoTurbo)
        {
            if (turbo)
                return tonoTurbo;
            float t = velocidadNormal <= 0f ? 0f : Mathf.Clamp01(velocidad / velocidadNormal);
            return Mathf.Lerp(tonoQuieta, tonoNormal, t);
        }
    }
}
