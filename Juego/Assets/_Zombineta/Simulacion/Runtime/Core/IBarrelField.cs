namespace Zombineta.Core
{
    /// <summary>
    /// Lo que la simulacion necesita saber de los barriles, que en realidad son parte del
    /// recorrido. Lo implementa LevelRuntime; en los tests se reemplaza por un doble.
    /// </summary>
    public interface IBarrelField
    {
        /// <summary>El barril mas cercano hacia atras en ese carril, si hay alguno sin explotar.</summary>
        bool TryNearestBarrel(float fromX, int lane, float range, out float x, out int index);

        /// <summary>Lo hace explotar (y encadena los que tenga cerca).</summary>
        void Detonate(int index, RunSimulation sim);
    }
}
