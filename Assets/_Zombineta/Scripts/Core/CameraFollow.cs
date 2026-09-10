using UnityEngine;
using Zombineta.CameraFx;

namespace Zombineta.Core
{
    /// <summary>
    /// Puente entre el juego y el lenguaje de camara (CameraDirector, C# plano): le pasa el
    /// estado de la partida, le avisa de choques y disparos, y aplica la pose a la camara.
    /// Tambien maneja el tiempo: el congelado del impacto y la camara lenta al ser atrapada.
    ///
    /// Corre despues de RunController y antes de SceneryManager: el escenario tiene que
    /// leer la camara ya movida en este frame.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] CameraConfig config;

        Camera cam;
        CameraDirector director;
        bool needsSnap = true;
        bool inFinale;
        float hitstopLeft;

        public CameraDirector Director => director;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (config != null)
                director = new CameraDirector(config);
            else
                Debug.LogError("CameraFollow no tiene CameraConfig asignado.", this);
        }

        void OnEnable()
        {
            if (run == null) return;
            run.Stepped += OnStepped;
            run.Restarted += OnRestarted;
        }

        void OnDisable()
        {
            if (run != null)
            {
                run.Stepped -= OnStepped;
                run.Restarted -= OnRestarted;
            }
            // Nunca dejar el tiempo congelado si la camara se apaga a mitad de un efecto.
            Time.timeScale = 1f;
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null || director == null)
                return;

            director.EffectsEnabled = GameSettings.CameraEffects;
            director.ShakeEnabled = GameSettings.CameraShake;
            director.Aspect = cam.aspect;

            // Todo en tiempo real: la camara sigue viva durante el congelado y la camara lenta.
            float dt = Time.unscaledDeltaTime;

            if (hitstopLeft > 0f)
            {
                hitstopLeft -= dt;
                if (hitstopLeft <= 0f && !inFinale)
                    Time.timeScale = 1f;
            }

            var input = BuildInput();
            if (needsSnap)
            {
                director.Snap(input);
                needsSnap = false;
            }

            var pose = director.Step(input, dt);
            transform.position = new Vector3(pose.X, pose.Y, transform.position.z);
            transform.rotation = Quaternion.Euler(0f, 0f, pose.Roll);
            cam.orthographicSize = pose.Size;
        }

        CameraInput BuildInput()
        {
            var s = run.Sim.State;
            return new CameraInput
            {
                PlayerX = run.ToWorldX(s.PlayerX),
                PlayerY = run.LaneToWorldY(s.LaneVisual),
                GapMeters = s.Gap,
                // Turbo "de verdad": apretar D sin nafta o aturdida no acelera.
                Turbo = s.Mode == DriveMode.Turbo && s.Fuel > 0f && s.StunRemaining <= 0f,
                GoalX = run.ToWorldX(run.Config.goalDistance),
            };
        }

        void OnStepped(RunEvent events)
        {
            if (!GameSettings.CameraEffects || director == null)
                return;

            if ((events & RunEvent.Crashed) != 0)
            {
                director.AddTrauma(config.crashTrauma);
                director.Punch(config.crashPunchForward, config.crashPunchZoom);
                Hitstop(config.hitstopSeconds);
            }

            if ((events & RunEvent.Shot) != 0)
            {
                director.AddTrauma(config.shotTrauma);
                director.Punch(config.shotPunchForward, 0f);
            }
        }

        void OnRestarted()
        {
            inFinale = false;
            hitstopLeft = 0f;
            Time.timeScale = 1f;
            needsSnap = true;
        }

        void Hitstop(float seconds)
        {
            if (seconds <= 0f || inFinale)
                return;
            hitstopLeft = seconds;
            Time.timeScale = 0f;
        }

        // --- Finales: los llama ScreenFlow y espera lo que devuelven -------------

        /// <summary>
        /// La horda te alcanzo: plano cerrado sobre la moto y camara lenta. Devuelve los
        /// segundos reales a esperar antes del Game Over (0 si los efectos estan apagados).
        /// </summary>
        public float PlayCatch()
        {
            if (!GameSettings.CameraEffects || director == null)
                return 0f;
            inFinale = true;
            director.BeginCatch();
            director.AddTrauma(config.catchTrauma);
            Time.timeScale = config.catchTimeScale;
            return config.catchHoldSeconds;
        }

        /// <summary>Llegaste al refugio: el plano se abre sobre la meta.</summary>
        public float PlayVictory()
        {
            if (!GameSettings.CameraEffects || director == null)
                return 0f;
            inFinale = true;
            // Si la moto llego de golpe (F2 de debug), la camara salta en vez de cruzar el nivel.
            var input = BuildInput();
            if (Mathf.Abs(transform.position.x - input.GoalX) > 30f)
                director.Snap(input);
            director.BeginVictory();
            return config.victoryHoldSeconds;
        }

        /// <summary>Fin del final: el tiempo vuelve a la normalidad antes de la pantalla siguiente.</summary>
        public void EndFinale()
        {
            inFinale = false;
            hitstopLeft = 0f;
            Time.timeScale = 1f;
        }
    }
}
