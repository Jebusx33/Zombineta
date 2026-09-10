using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Enemies
{
    /// <summary>
    /// Dibuja la horda como un frente que avanza. La simulacion solo conoce un
    /// numero (HordeX); aca se reparten varios zombies entre los tres carriles
    /// para que se lea como una masa y no como un unico enemigo.
    /// </summary>
    public sealed class HordeView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Transform zombiePrefab;
        [SerializeField] int zombieCount = 12;

        [Tooltip("Cuantos metros hacia atras se extiende la masa desde el frente.")]
        [SerializeField] float depth = 8f;

        [Tooltip("Metros que la horda sigue avanzando por encima de la moto al atraparla.")]
        [SerializeField] float overrunMeters = 12f;

        Transform[] zombies;
        float[] offsets;
        float[] lanes;

        // Solo presentacion: la simulacion ya termino al atraparte, pero la horda sigue
        // avanzando por encima de la moto (en camara lenta, con el tiempo escalado).
        float overrun;

        void Start()
        {
            if (run == null || zombiePrefab == null)
                return;

            zombies = new Transform[zombieCount];
            offsets = new float[zombieCount];
            lanes = new float[zombieCount];

            // Semilla fija: la horda se ve igual en cada partida, asi un playtest
            // es comparable con el siguiente.
            var rng = new System.Random(1234);

            for (int i = 0; i < zombieCount; i++)
            {
                zombies[i] = Instantiate(zombiePrefab, transform);
                offsets[i] = -(float)rng.NextDouble() * depth;
                lanes[i] = (float)rng.NextDouble() * (RunSimulation.LaneCount - 1);
            }
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null || zombies == null)
                return;

            var state = run.Sim.State;
            if (state.Phase == RunPhase.Lost)
                overrun = Mathf.Min(overrunMeters, overrun + run.Config.hordeBaseSpeed * Time.deltaTime);
            else if (state.Phase == RunPhase.Running)
                overrun = 0f;

            float frontX = state.HordeX + overrun;

            for (int i = 0; i < zombies.Length; i++)
            {
                zombies[i].position = new Vector3(
                    run.ToWorldX(frontX + offsets[i]),
                    run.LaneToWorldY(lanes[i]),
                    0f);
            }
        }
    }
}
