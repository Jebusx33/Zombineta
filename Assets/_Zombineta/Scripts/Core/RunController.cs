using System;
using UnityEngine;
using Zombineta.Level;
using Zombineta.Player;

namespace Zombineta.Core
{
    /// <summary>
    /// Unico puente entre Unity y la simulacion: lee input, hace Tick, resuelve
    /// los encuentros con el recorrido y publica lo que paso. Todo lo demas en la
    /// escena solo lee estado y dibuja.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RunController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] LevelDefinition level;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Si esta activo, la partida arranca sola. Si no, la largan las pantallas.")]
        [SerializeField] bool autoStart = true;

        /// <summary>Se dispara cada frame en que ocurrio algo. Para SFX, VFX y UI.</summary>
        public event Action<RunEvent> Stepped;

        public RunSimulation Sim { get; private set; }
        public LevelRuntime Level { get; private set; }
        public GameConfig Config => config;

        /// <summary>Mientras sea false la simulacion no corre, aunque la partida este viva.</summary>
        public bool Paused { get; set; }

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
            Level = new LevelRuntime(level);
            Paused = !autoStart;
        }

        void Update()
        {
            if (Sim == null)
                return;

            if (Sim.State.Phase != RunPhase.Running)
            {
                if (input != null && input.RestartPressed)
                    Restart();
                return;
            }

            if (Paused)
                return;

            var intent = input != null ? input.Read() : PlayerIntent.Idle;

            float before = Sim.State.PlayerX;
            var events = Sim.Tick(intent, Time.deltaTime);
            float after = Sim.State.PlayerX;

            // Los encuentros se resuelven despues del movimiento, sobre el tramo
            // efectivamente recorrido: asi nada se saltea por ir rapido.
            if (Sim.State.Phase == RunPhase.Running && !Mathf.Approximately(before, after))
                events |= Level.Collect(Sim, before, after);

            if (events != RunEvent.None)
                Stepped?.Invoke(events);
        }

        public void Restart()
        {
            Sim.Reset();
            Level.Reset();
            Paused = false;
        }

        /// <summary>Altura en mundo del carril indicado (0 abajo, 1 medio, 2 arriba).</summary>
        public float LaneToWorldY(float lane) => (lane - 1f) * config.laneSpacing;

        /// <summary>Convierte metros de la simulacion a unidades de mundo.</summary>
        public float ToWorldX(float meters) => meters * config.worldUnitsPerMeter;
    }
}
