using UnityEngine;

namespace Zombineta.Core
{
    public enum SortSlot { Shadow = 0, Item = 10, Zombie = 20, Player = 30, Effect = 40 }

    /// <summary>
    /// Orden de dibujo por carril: los personajes miden mas de dos carriles de alto, asi que lo
    /// del carril de abajo tiene que tapar a lo de arriba. El carril es continuo (el visual de un
    /// cambio de carril) para que el orden no salte a mitad de camino.
    /// </summary>
    public static class LaneSorting
    {
        public const int Base = 1000;
        public const int PerLane = 100;

        /// <summary>Sorting Layer donde vive todo lo del juego (moto, horda, items, sombras,
        /// efectos): entre "Calle" y "Frente" del escenario, para que una Light2D pueda
        /// alcanzarlo sin lavar el fondo ni la calle.</summary>
        public const string GameLayer = "Juego";

        static int gameLayerId = int.MinValue;

        /// <summary>
        /// Id de GameLayer, resuelto una sola vez (no en cada cuadro): leer
        /// SpriteRenderer.sortingLayerName aloca un string por llamada, y compararlo/asignarlo
        /// por nombre cada cuadro (WheelDustView, ScooterView, HordeView, GroundShadow, FxManager)
        /// era justamente eso. Comparar y asignar por sortingLayerID no aloca nada.
        /// </summary>
        public static int GameLayerId
        {
            get
            {
                if (gameLayerId == int.MinValue)
                    gameLayerId = SortingLayer.NameToID(GameLayer);
                return gameLayerId;
            }
        }

        public static int Order(float visualLane, SortSlot slot) =>
            Base - Mathf.RoundToInt(visualLane * PerLane) + (int)slot;
    }
}
