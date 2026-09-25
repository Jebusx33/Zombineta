using System;
using UnityEngine;
using Zombineta.Juego.Levels;
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

        [Tooltip("Nivel armado a mano: si esta, el recorrido y el largo salen de la escena.")]
        [SerializeField] LevelScene levelScene;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Si esta activo, la partida arranca sola. Si no, la largan las pantallas.")]
        [SerializeField] bool autoStart = true;

        /// <summary>Se dispara cada frame en que ocurrio algo. Para SFX, VFX y UI.</summary>
        public event Action<RunEvent> Stepped;

        /// <summary>La partida volvio a la largada (reintento o nivel nuevo).</summary>
        public event Action Restarted;

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

            if (levelScene != null)
            {
                config = levelScene.RuntimeConfig(config);
                level = levelScene.BuildDefinition();
            }

            Sim = new RunSimulation(config);
            Level = new LevelRuntime(level);
            // Sin esto los barriles no existen para el disparo: la bala nunca los encuentra.
            Sim.Barrels = Level;
            Paused = !autoStart;

            if (levelScene != null)
                levelScene.Bind(this);
        }

        void Update()
        {
            if (Sim == null)
                return;

            if (Sim.State.Phase != RunPhase.Running)
            {
                if (QuickRestartEnabled && input != null && input.RestartPressed)
                    Restart();
                return;
            }

            if (Paused)
                return;

            var intent = input != null ? input.Read() : PlayerIntent.Idle;

            var events = StepSimulation(Sim, Level, intent, Time.deltaTime);

            if (events != RunEvent.None)
                Stepped?.Invoke(events);
        }

        /// <summary>
        /// Un cuadro de partida: Tick y despues los encuentros con el recorrido. Es lo que hace
        /// Update; esta publico y estatico para que un test pueda correr la partida igual que el
        /// juego, sin escena (el test de punta a punta del tutorial).
        /// </summary>
        public static RunEvent StepSimulation(RunSimulation sim, LevelRuntime level, PlayerIntent intent, float dt)
        {
            float before = sim.State.PlayerX;
            var events = sim.Tick(intent, dt);
            float after = sim.State.PlayerX;

            // Los encuentros se resuelven despues del movimiento, sobre el tramo
            // efectivamente recorrido: asi nada se saltea por ir rapido.
            if (level != null && sim.State.Phase == RunPhase.Running && !Mathf.Approximately(before, after))
                events |= level.Collect(sim, before, after);

            return events;
        }

        public void Restart()
        {
            Sim.Reset();
            Level.Reset();
            Paused = false;
            Restarted?.Invoke();
        }

        /// <summary>
        /// R reinicia la partida al perder o ganar. Cuando hay un flujo de pantallas, lo
        /// apaga: reiniciar tiene que pasar por el Game Over, no saltearlo.
        /// </summary>
        public bool QuickRestartEnabled { get; set; } = true;

        /// <summary>Cambia de recorrido y deja la partida lista en la largada, en pausa.</summary>
        public void LoadLevel(LevelDefinition definition)
        {
            level = definition;
            Level = new LevelRuntime(definition);
            Sim.Barrels = Level;
            Sim.Reset();
            Paused = true;
            Restarted?.Invoke();
        }

        /// <summary>Altura en mundo del carril indicado (0 abajo, 1 medio, 2 arriba).</summary>
        public float LaneToWorldY(float lane) => (lane - 1f) * config.laneSpacing;

        /// <summary>Convierte metros de la simulacion a unidades de mundo.</summary>
        public float ToWorldX(float meters) => meters * config.worldUnitsPerMeter;

        /// <summary>Convierte metros de altura del salto a unidades de mundo.</summary>
        public float HeightToWorld(float meters) => meters * config.jumpHeightToWorld;
    }
}
