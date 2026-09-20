using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Player
{
    /// <summary>
    /// Polvo que levanta la rueda trasera. Solo presentacion: cuanto mas rapido va la moto,
    /// mas polvo; en turbo, bastante mas. En el aire, frenada o fuera de partida, nada.
    /// El sistema de particulas simula en mundo, asi el polvo queda atras y se ve pasar.
    /// </summary>
    public sealed class WheelDustView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] ParticleSystem dust;

        [Tooltip("Particulas por segundo por cada m/s de velocidad.")]
        [SerializeField] float particlesPerSpeed = 1.5f;

        [Tooltip("Multiplica la cantidad en turbo.")]
        [SerializeField] float turboMultiplier = 2f;

        ParticleSystemRenderer dustRenderer;

        void LateUpdate()
        {
            if (run == null || run.Sim == null || dust == null)
                return;

            if (dustRenderer == null)
                dustRenderer = dust.GetComponent<ParticleSystemRenderer>();

            var state = run.Sim.State;
            float speed = run.Sim.PlayerSpeed;
            bool rolling = state.Phase == RunPhase.Running && !state.Airborne && speed > 0.5f;

            float rate = rolling ? speed * particlesPerSpeed : 0f;
            if (rolling && state.Mode == DriveMode.Turbo)
                rate *= turboMultiplier;

            var emission = dust.emission;
            emission.rateOverTime = rate;

            // Justo debajo de la moto: se ve pasar el polvo detras de la rueda, pero sin taparlo,
            // y por encima de la sombra propia de la moto (que usa SortSlot.Shadow).
            if (dustRenderer != null)
            {
                int order = LaneSorting.Order(state.LaneVisual, SortSlot.Player) - 1;
                if (dustRenderer.sortingOrder != order) dustRenderer.sortingOrder = order;
                if (dustRenderer.sortingLayerName != LaneSorting.GameLayer) dustRenderer.sortingLayerName = LaneSorting.GameLayer;
            }
        }
    }
}
