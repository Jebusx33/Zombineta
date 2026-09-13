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

        void LateUpdate()
        {
            if (run == null || run.Sim == null || dust == null)
                return;

            var state = run.Sim.State;
            float speed = run.Sim.PlayerSpeed;
            bool rolling = state.Phase == RunPhase.Running && !state.Airborne && speed > 0.5f;

            float rate = rolling ? speed * particlesPerSpeed : 0f;
            if (rolling && state.Mode == DriveMode.Turbo)
                rate *= turboMultiplier;

            var emission = dust.emission;
            emission.rateOverTime = rate;
        }
    }
}
