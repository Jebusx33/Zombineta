namespace Zombineta.Core
{
    /// <summary>
    /// Lo que la simulacion necesita saber de los barriles, que en realidad son parte del
    /// recorrido. Lo implementa LevelRuntime; en los tests se reemplaza por un doble.
    /// </summary>
    public interface IBarrelField
    {
        /// <summary>
        /// El barril sin explotar mas cercano hacia donde mira (dir: 1 derecha, -1 izquierda), a lo sumo
        /// laneTolerance carriles del de ella (lane: continuo, camina entre carriles), dentro del alcance y
        /// entre minX y maxX (la arena de la pelea). Solo sirve a pie: en la moto no se les dispara.
        /// </summary>
        bool TryNearestBarrelAhead(float fromX, int dir, float lane, float laneTolerance, float range,
                                   float minX, float maxX, out float x, out int index);

        /// <summary>Lo hace explotar (y encadena los que tenga cerca).</summary>
        void Detonate(int index, RunSimulation sim);
    }
}
