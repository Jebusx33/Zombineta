using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Player
{
    /// <summary>
    /// Polvo que levanta la rueda trasera, humo del escape y, en turbo, llamaradas del escape.
    /// Solo presentacion: cuanto mas rapido va la moto, mas polvo; en turbo, bastante mas. En el
    /// aire, frenada o fuera de partida, nada de polvo; el escape humea poco aun en ralenti.
    /// Los sistemas simulan en mundo, asi el humo queda atras y se ve pasar.
    /// </summary>
    public sealed class WheelDustView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] ParticleSystem dust;

        [Tooltip("Particulas por segundo por cada m/s de velocidad.")]
        [SerializeField] float particlesPerSpeed = 1.5f;

        [Tooltip("Multiplica la cantidad en turbo.")]
        [SerializeField] float turboMultiplier = 2f;

        [Header("Escape")]
        [Tooltip("Opcional: humo del caño de escape.")]
        [SerializeField] ParticleSystem exhaust;
        [Tooltip("Bocanadas por segundo con la moto en marcha, aun parada.")]
        [SerializeField] float exhaustIdle = 4f;
        [Tooltip("Bocanadas por segundo extra por cada m/s de velocidad.")]
        [SerializeField] float exhaustPerSpeed = 0.4f;

        [Header("Turbo")]
        [Tooltip("Opcional: llamaradas y chispas del escape, solo en turbo.")]
        [SerializeField] ParticleSystem turboFlames;
        [SerializeField] float turboFlamesRate = 30f;

        ParticleSystemRenderer[] renderers;
        int[] baseOrders;

        void LateUpdate()
        {
            if (run == null || run.Sim == null || dust == null)
                return;

            if (renderers == null)
                CacheRenderers();

            var state = run.Sim.State;
            float speed = run.Sim.PlayerSpeed;
            bool rolling = state.Phase == RunPhase.Running && !state.Airborne && speed > 0.5f;

            float rate = rolling ? speed * particlesPerSpeed : 0f;
            if (rolling && state.Mode == DriveMode.Turbo)
                rate *= turboMultiplier;

            var emission = dust.emission;
            emission.rateOverTime = rate;

            bool running = state.Phase == RunPhase.Running;
            if (exhaust != null)
            {
                var e = exhaust.emission;
                e.rateOverTime = running ? exhaustIdle + speed * exhaustPerSpeed : 0f;
            }
            if (turboFlames != null)
            {
                var e = turboFlames.emission;
                e.rateOverTime = running && state.Mode == DriveMode.Turbo ? turboFlamesRate : 0f;
            }

            // Justo debajo de la moto: se ve pasar el polvo detras de la rueda, pero sin taparlo,
            // y por encima de la sombra propia de la moto (que usa SortSlot.Shadow). Cada capa
            // conserva su orden relativo (el humo detras de las llamas).
            int order = LaneSorting.Order(state.LaneVisual, SortSlot.Player) - 1;
            for (int i = 0; i < renderers.Length; i++)
            {
                int o = order + baseOrders[i];
                if (renderers[i].sortingOrder != o) renderers[i].sortingOrder = o;
                if (renderers[i].sortingLayerID != LaneSorting.GameLayerId) renderers[i].sortingLayerID = LaneSorting.GameLayerId;
            }
        }

        void CacheRenderers()
        {
            var list = new System.Collections.Generic.List<ParticleSystemRenderer>();
            foreach (var ps in new[] { dust, exhaust, turboFlames })
                if (ps != null)
                {
                    var r = ps.GetComponent<ParticleSystemRenderer>();
                    if (r != null && !list.Contains(r))
                        list.Add(r);
                }
            renderers = list.ToArray();
            baseOrders = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseOrders[i] = renderers[i].sortingOrder;
        }
    }
}
