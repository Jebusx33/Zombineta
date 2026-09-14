using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Juego.Levels
{
    /// <summary>
    /// La cuenta entre el recorrido (metros y carriles) y la escena (unidades de mundo). C# plano:
    /// es lo mismo que hacen RunController y las vistas, pero en un solo lugar y con tests, para
    /// que un objeto ubicado a mano caiga exactamente donde la simulacion lo va a encontrar.
    /// </summary>
    public readonly struct LevelLayout
    {
        public readonly float WorldUnitsPerMeter;
        public readonly float LaneSpacing;
        public readonly float JumpHeightToWorld;

        public LevelLayout(float worldUnitsPerMeter, float laneSpacing, float jumpHeightToWorld)
        {
            WorldUnitsPerMeter = worldUnitsPerMeter;
            LaneSpacing = laneSpacing;
            JumpHeightToWorld = jumpHeightToWorld;
        }

        public static LevelLayout From(GameConfig config) =>
            new LevelLayout(config.worldUnitsPerMeter, config.laneSpacing, config.jumpHeightToWorld);

        public float ToWorldX(float meters) => meters * WorldUnitsPerMeter;

        public float ToMeters(float worldX) => WorldUnitsPerMeter <= 0f ? 0f : worldX / WorldUnitsPerMeter;

        /// <summary>Altura en mundo de un carril: 0 abajo, 1 medio, 2 arriba (igual que RunController).</summary>
        public float LaneY(int lane) => (lane - 1) * LaneSpacing;

        /// <summary>El carril mas cercano a una altura de mundo (la del piso, sin la altura aerea).</summary>
        public int NearestLane(float groundWorldY)
        {
            if (LaneSpacing <= 0f)
                return 1;
            return Mathf.Clamp(Mathf.RoundToInt(groundWorldY / LaneSpacing) + 1, 0, RunSimulation.LaneCount - 1);
        }

        public static float SnapMeters(float meters, float step) =>
            step <= 0f ? meters : Mathf.Round(meters / step) * step;

        /// <summary>Donde se dibuja un item: su carril, y mas arriba si es aereo.</summary>
        public Vector2 ItemPosition(float meters, int lane, float height) =>
            new Vector2(ToWorldX(meters), LaneY(lane) + height * JumpHeightToWorld);

        /// <summary>La altura del piso de un item dibujado en una Y, sabiendo cuanto flota.</summary>
        public float GroundY(float worldY, float height) => worldY - height * JumpHeightToWorld;
    }
}
