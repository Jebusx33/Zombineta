using UnityEngine;

namespace Zombineta.Audio
{
    public enum AudioBus { Musica, Efectos, Ambiente, UI }

    /// <summary>
    /// La mezcla del juego sin AudioMixer (Unity no deja crearlo por script). Los sliders
    /// guardan valores lineales 0-1; la ganancia usa una curva perceptual (valor ^ exponente).
    /// UI y Ambiente cuelgan de Efectos. La pausa (PausaDuck 0) calla Efectos y Ambiente y deja
    /// la musica en MusicaEnPausa; UI no se toca. MusicaEnPausa y ExponenteCurva los copia el
    /// AudioDirector desde MezclaAudio.
    /// </summary>
    public sealed class AudioBuses
    {
        public float General = 1f;
        public float Musica = 1f;
        public float Efectos = 1f;
        public float PausaDuck = 1f;

        public float MusicaEnPausa = 0.4f;
        public float ExponenteCurva = 2f;

        public static float Curva(float v, float exponente = 2f)
        {
            v = Mathf.Clamp01(v);
            return Mathf.Pow(v, Mathf.Max(1f, exponente));
        }

        public float Gain(AudioBus bus)
        {
            float general = Curva(General, ExponenteCurva);
            float duck = Mathf.Clamp01(PausaDuck);
            switch (bus)
            {
                case AudioBus.Musica:
                    return general * Curva(Musica, ExponenteCurva) * Mathf.Lerp(Mathf.Clamp01(MusicaEnPausa), 1f, duck);
                case AudioBus.UI:
                    return general * Curva(Efectos, ExponenteCurva);
                default: // Efectos, Ambiente
                    return general * Curva(Efectos, ExponenteCurva) * duck;
            }
        }
    }
}
