using UnityEngine;

namespace Zombineta.Audio
{
    public enum AudioBus { Musica, Efectos, Ambiente, UI }

    /// <summary>
    /// La mezcla del juego sin AudioMixer (Unity no deja crearlo por script). Los sliders
    /// guardan valores lineales 0-1; la ganancia usa una curva perceptual. UI y Ambiente
    /// cuelgan de Efectos. La pausa (PausaDuck 0) calla Efectos y Ambiente y deja la musica
    /// al 40 %; UI no se toca.
    /// </summary>
    public sealed class AudioBuses
    {
        public const float MusicaEnPausa = 0.4f;

        public float General = 1f;
        public float Musica = 1f;
        public float Efectos = 1f;
        public float PausaDuck = 1f;

        public static float Curva(float v)
        {
            v = Mathf.Clamp01(v);
            return v * v;
        }

        public float Gain(AudioBus bus)
        {
            float general = Curva(General);
            float duck = Mathf.Clamp01(PausaDuck);
            switch (bus)
            {
                case AudioBus.Musica:
                    return general * Curva(Musica) * Mathf.Lerp(MusicaEnPausa, 1f, duck);
                case AudioBus.UI:
                    return general * Curva(Efectos);
                default: // Efectos, Ambiente
                    return general * Curva(Efectos) * duck;
            }
        }
    }
}
