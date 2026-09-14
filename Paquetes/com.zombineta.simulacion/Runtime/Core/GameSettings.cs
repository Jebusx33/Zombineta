using UnityEngine;

namespace Zombineta.Core
{
    /// <summary>
    /// Preferencias de la jugadora que sobreviven entre sesiones (PlayerPrefs). Las lee
    /// quien las necesita; la pantalla de Opciones es la unica que las escribe.
    /// </summary>
    public static class GameSettings
    {
        const string CameraEffectsKey = "zombineta.cameraEffects";
        const string CameraShakeKey = "zombineta.cameraShake";

        static bool? cameraEffects;
        static bool? cameraShake;

        /// <summary>Interruptor general del lenguaje de camara: tension, golpes, finales.</summary>
        public static bool CameraEffects
        {
            get => cameraEffects ??= PlayerPrefs.GetInt(CameraEffectsKey, 1) == 1;
            set { cameraEffects = value; PlayerPrefs.SetInt(CameraEffectsKey, value ? 1 : 0); }
        }

        /// <summary>
        /// Sacudidas y golpes de camara. Pueden marear: es una opcion de accesibilidad.
        /// Apagarlas conserva el encuadre por tension.
        /// </summary>
        public static bool CameraShake
        {
            get => cameraShake ??= PlayerPrefs.GetInt(CameraShakeKey, 1) == 1;
            set { cameraShake = value; PlayerPrefs.SetInt(CameraShakeKey, value ? 1 : 0); }
        }

        const string GoreKey = "zombineta.gore";
        static bool? gore;

        /// <summary>Sangre alta o baja. En baja, las salpicaduras son polvo y no quedan manchas.</summary>
        public static bool Gore
        {
            get => gore ??= PlayerPrefs.GetInt(GoreKey, 1) != 0;
            set
            {
                gore = value;
                PlayerPrefs.SetInt(GoreKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
