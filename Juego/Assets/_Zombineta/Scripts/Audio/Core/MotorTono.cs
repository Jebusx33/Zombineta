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

        /// <summary>Fraccion de la velocidad normal en la que el ralenti ya se apago del todo.</summary>
        public const float FinRalenti = 0.4f;

        /// <summary>
        /// Cuanto suena el ralenti (0-1): todo con la moto quieta y nada desde FinRalenti de la
        /// velocidad normal; la marcha se lleva el resto. Hacia atras tambien cuenta como andar.
        /// </summary>
        public static float Ralenti(float velocidad, float velocidadNormal)
        {
            if (velocidadNormal <= 0f)
                return 1f;
            return 1f - Mathf.Clamp01(Mathf.Abs(velocidad) / (velocidadNormal * FinRalenti));
        }

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
