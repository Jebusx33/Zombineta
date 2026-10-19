using UnityEngine;
using Zombineta.Level;

namespace Zombineta.Juego.Levels
{
    /// <summary>
    /// Cuanto lugar ocupa cada cosa del recorrido a lo largo del camino. La simulacion trata a
    /// todo como un punto, pero el dibujo no: un coche mide 6 m de ancho, asi que dos a 2 m
    /// de distancia se encimaban. El generador usa estas medidas (mitad del ancho del dibujo, en
    /// metros) para separar las cosas segun lo que se ve, no solo segun donde estan.
    /// Si arte cambia el tamano de un dibujo, se ajusta aca.
    /// </summary>
    public static class LevelFootprint
    {
        /// <summary>
        /// Coche a su escala actual en LevelItemPalette (75 % de 616 px a 100 px/u: 4,65 m) con un
        /// poco de aire.
        /// </summary>
        public const float Obstacle = 2.5f;

        /// <summary>
        /// Carriles de arriba que tapa un obstaculo: el coche mide 2,3 m de alto (al 75 %) y los
        /// carriles estan a 0,99 m. Los dos numeros de obstaculo (ancho y carriles) dependen del tamano del dibujo:
        /// si arte lo cambia, se ajustan juntos.
        /// </summary>
        public const int ObstacleCoversLanes = 2;

        /// <summary>Rampa a escala 0,3 (casi 6 m).</summary>
        public const float Ramp = 3f;

        public const float ZombieFront = 0.7f;

        /// <summary>Barril, bidon, bateria y balas.</summary>
        public const float Small = 0.5f;

        /// <summary>Aire que se deja entre dos dibujos.</summary>
        public const float Margin = 0.3f;

        public static float Half(LevelEntryKind kind)
        {
            switch (kind)
            {
                case LevelEntryKind.Obstacle: return Obstacle;
                case LevelEntryKind.Ramp: return Ramp;
                case LevelEntryKind.ZombieFront: return ZombieFront;
                default: return Small;
            }
        }

        /// <summary>
        /// Distancia minima entre los centros de dos cosas del mismo carril para que no se
        /// toquen. Nunca menos que "floor" (la separacion minima de siempre).
        /// </summary>
        public static float MinDistance(LevelEntryKind a, LevelEntryKind b, float floor) =>
            Mathf.Max(floor, Half(a) + Half(b) + Margin);

        /// <summary>
        /// Si "hider" tapa a "item": un obstaculo en el piso es mas alto que un carril, y lo que
        /// esta en los carriles de arriba, a la altura de su dibujo, queda detras (los carriles
        /// de abajo se dibujan delante). Lo que va en el aire no se tapa.
        /// </summary>
        public static bool Hides(LevelEntry hider, LevelEntry item)
        {
            if (hider.kind != LevelEntryKind.Obstacle || hider.height > 0f || item.height > 0f)
                return false;
            int arriba = item.lane - hider.lane;
            if (arriba < 1 || arriba > ObstacleCoversLanes)
                return false;
            return Mathf.Abs(item.distance - hider.distance) < Half(hider.kind) + Half(item.kind);
        }
    }
}
