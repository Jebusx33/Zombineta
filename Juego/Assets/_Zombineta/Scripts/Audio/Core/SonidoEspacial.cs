using System;
using UnityEngine;

namespace Zombineta.Audio
{
    [Serializable]
    public struct EspacialConfig
    {
        public float anchoPaneo;
        public float distanciaPlena;
        public float distanciaMaxima;
    }

    /// <summary>Paneo y atenuacion de un sonido segun su distancia en X a la moto (dx = sonido - moto).</summary>
    public static class SonidoEspacial
    {
        public static float Pan(float dx, EspacialConfig c) =>
            c.anchoPaneo <= 0f ? 0f : Mathf.Clamp(dx / c.anchoPaneo, -1f, 1f);

        public static float Volumen(float dx, EspacialConfig c)
        {
            float d = Mathf.Abs(dx);
            if (d <= c.distanciaPlena)
                return 1f;
            if (d >= c.distanciaMaxima || c.distanciaMaxima <= c.distanciaPlena)
                return 0f;
            return 1f - (d - c.distanciaPlena) / (c.distanciaMaxima - c.distanciaPlena);
        }
    }
}
