using UnityEngine;

// Alias solo para este archivo: GameSettings.LightQuality (la propiedad) y LightQuality (el enum)
// comparten nombre a proposito (lo pide la spec de la tarea). Referenciar "LightQuality.Alta" a
// secas adentro de la clase resolveria contra la propiedad, no el tipo (CS0119): con el alias
// evitamos el choque sin tocar la API publica.
using LQ = Zombineta.Core.LightQuality;

namespace Zombineta.Core
{
    /// <summary>Calidad de iluminacion: Alta prende todo; Baja apaga lo caro para una maquina lenta.</summary>
    public enum LightQuality { Alta, Baja }

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

        const string VolumeKey = "zombineta.volume";
        static float? volume;

        /// <summary>Se dispara cuando cambia cualquiera de los tres volumenes (General, Musica, Efectos).</summary>
        public static event System.Action AudioSettingsChanged;

        /// <summary>Volumen general, de 0 a 1.</summary>
        public static float Volume
        {
            get => volume ??= PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set
            {
                volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, volume.Value);
                PlayerPrefs.Save();
                AudioSettingsChanged?.Invoke();
            }
        }

        const string MusicVolumeKey = "zombineta.musicVolume";
        static float? musicVolume;

        /// <summary>Volumen del bus Musica, de 0 a 1.</summary>
        public static float MusicVolume
        {
            get => musicVolume ??= PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
            set
            {
                musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume.Value);
                PlayerPrefs.Save();
                AudioSettingsChanged?.Invoke();
            }
        }

        const string SfxVolumeKey = "zombineta.sfxVolume";
        static float? sfxVolume;

        /// <summary>Volumen del bus Efectos (Ambiente y UI cuelgan de el), de 0 a 1.</summary>
        public static float SfxVolume
        {
            get => sfxVolume ??= PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume.Value);
                PlayerPrefs.Save();
                AudioSettingsChanged?.Invoke();
            }
        }

        const string FullscreenKey = "zombineta.fullscreen";
        static bool? fullscreen;

        public static bool Fullscreen
        {
            get => fullscreen ??= PlayerPrefs.GetInt(FullscreenKey, 1) != 0;
            set
            {
                fullscreen = value;
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        const string VibrationKey = "zombineta.vibration";
        static bool? vibration;

        /// <summary>Vibracion del joystick ante golpes, disparos y la atrapada.</summary>
        public static bool Vibration
        {
            get => vibration ??= PlayerPrefs.GetInt(VibrationKey, 1) != 0;
            set
            {
                vibration = value;
                PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        const string LightQualityKey = "zombineta.lightQuality";
        static LQ? lightQuality;

        /// <summary>Avisa a los componentes vivos (runners de presupuesto, sombras) que la calidad cambio.</summary>
        public static event System.Action<LQ> LightQualityChanged;

        /// <summary>Alta prende ambiente, faro, destellos, sombras y luces de adorno hasta el tope.
        /// Baja apaga sombras y destellos y no deja ninguna luz de adorno prendida.</summary>
        public static LQ LightQuality
        {
            get => lightQuality ??= (LQ)PlayerPrefs.GetInt(LightQualityKey, (int)LQ.Alta);
            set
            {
                lightQuality = value;
                PlayerPrefs.SetInt(LightQualityKey, (int)value);
                PlayerPrefs.Save();
                LightQualityChanged?.Invoke(value);
            }
        }
    }
}
