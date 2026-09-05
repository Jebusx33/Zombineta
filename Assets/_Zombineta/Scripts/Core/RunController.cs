using System;
using UnityEngine;
using Zombineta.Player;

namespace Zombineta.Core
{
    /// <summary>
    /// Unico puente entre Unity y la simulacion: lee input, hace Tick y publica
    /// lo que paso. Todo lo demas en la escena solo lee estado y dibuja.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RunController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] PlayerInputReader input;

        /// <summary>Se dispara cada frame en que ocurrio algo. Para SFX, VFX y UI.</summary>
        public event Action<RunEvent> Stepped;

        public RunSimulation Sim { get; private set; }
        public GameConfig Config => config;

        void Awake()
        {
            if (config == null)
            {
                Debug.LogError(
                    "RunController no tiene GameConfig asignado. " +
                    "Asignalo en el Inspector: Assets/_Zombineta/Settings/GameConfig.asset",
                    this);
                enabled = false;
                return;
            }

            if (input == null)
                input = GetComponent<PlayerInputReader>();

            Sim = new RunSimulation(config);
        }

        void Update()
        {
            if (Sim == null)
                return;

            if (Sim.State.Phase != RunPhase.Running)
            {
                if (input != null && input.RestartPressed)
                    Sim.Reset();
                return;
            }

            var intent = input != null ? input.Read() : PlayerIntent.Idle;
            var events = Sim.Tick(intent, Time.deltaTime);

            if (events != RunEvent.None)
                Stepped?.Invoke(events);
        }

        /// <summary>Altura en mundo del carril indicado (0 abajo, 1 medio, 2 arriba).</summary>
        public float LaneToWorldY(float lane) => (lane - 1f) * config.laneSpacing;
    }
}
