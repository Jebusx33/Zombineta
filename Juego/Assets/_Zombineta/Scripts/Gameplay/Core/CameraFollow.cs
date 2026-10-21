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

        [Header("Anclaje al piso")]
        [Tooltip("El director fija el borde de abajo de la pantalla; con la calle en el 85 % de la altura, " +
                 "el zoom movia las lineas de carril. Esto fija en pantalla una linea de carril en su lugar.")]
        [SerializeField] bool anchorLaneLine = true;

        [Tooltip("Carril (en coordenadas de carril) cuya linea queda fija. -0,5 = el borde de abajo del carril 0.")]
        [SerializeField] float anchorLane = -0.5f;

        [Tooltip("Segundos reales en que el anclaje se apaga al entrar en atrapada o victoria (y vuelve al salir), " +
                 "para que la camara no pegue un salto.")]
        [SerializeField] float anchorBlendSeconds = 0.25f;

        // Peso del anclaje: 1 en juego normal, 0 en los finales (ahi manda el director, como siempre).
        float anchorWeight = 1f;

        Camera cam;
        CameraDirector director;
        bool needsSnap = true;
        bool inFinale;
        float hitstopLeft;
        float lentaLeft;
        float lentaEscala = 1f;

        public CameraDirector Director => director;

        /// <summary>
        /// Hay una pausa encima del nivel: el tiempo lo tiene congelado otro. Al terminar el
        /// congelado de un impacto, el tiempo se queda en 0 en vez de volver a 1.
        /// </summary>
        public bool TimeHeld { get; set; }

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
                    Time.timeScale = TimeHeld ? 0f : (lentaLeft > 0f ? lentaEscala : 1f);
            }

            if (lentaLeft > 0f)
            {
                lentaLeft -= dt;
                if (hitstopLeft <= 0f && !inFinale)
                    Time.timeScale = TimeHeld ? 0f : (lentaLeft > 0f ? lentaEscala : 1f);
            }

            var input = BuildInput();
            if (needsSnap)
            {
                director.Snap(input);
                needsSnap = false;
                anchorWeight = 1f;   // la largada salta al encuadre normal: el anclaje tambien
            }

            var pose = director.Step(input, dt);
            pose.Y += AnchorOffset(pose.Size, dt);
            transform.position = new Vector3(pose.X, pose.Y, transform.position.z);
            transform.rotation = Quaternion.Euler(0f, 0f, pose.Roll);
            cam.orthographicSize = pose.Size;
        }

        /// <summary>
        /// Corrimiento vertical que deja la linea anclada a la misma altura de pantalla con cualquier
        /// zoom. Con baseSize (el encuadre calibrado contra la referencia) no corre nada; al abrirse baja
        /// la camara y al cerrarse la sube. En atrapada y victoria el director manda como siempre (centra
        /// la moto / fija el borde de abajo): el peso baja a 0 en anchorBlendSeconds, sin salto.
        /// </summary>
        float AnchorOffset(float size, float dt)
        {
            float referenceSize = config.baseSize;
            if (!anchorLaneLine || referenceSize <= 0f)
                return 0f;

            bool finale = director.EffectsEnabled && director.Mode != CameraMode.Follow;
            float target = finale ? 0f : 1f;
            anchorWeight = anchorBlendSeconds > 0f
                ? Mathf.MoveTowards(anchorWeight, target, dt / anchorBlendSeconds)
                : target;
            if (anchorWeight <= 0f)
                return 0f;

            float lineY = run.LaneToWorldY(anchorLane);
            return anchorWeight * (lineY - config.viewBottomY) * (1f - size / referenceSize);
        }

        CameraInput BuildInput()
        {
            var s = run.Sim.State;
            // A pie la camara se planta: la arena queda centrada en pantalla, con tension media.
            if (s.Phase == RunPhase.Pelea)
            {
                float mitad = cam.orthographicSize * cam.aspect;
                return new CameraInput
                {
                    PlayerX = run.ToWorldX(run.Sim.Pelea.Centro) - config.lookAhead + mitad,
                    PlayerY = run.LaneToWorldY(1f),
                    GapMeters = 15f,
                    GoalX = run.ToWorldX(run.Config.goalDistance),
                };
            }

            return new CameraInput
            {
                PlayerX = run.ToWorldX(s.PlayerX),
                PlayerY = run.LaneToWorldY(s.LaneVisual),
                GapMeters = s.Gap,
                // Turbo "de verdad": apretar D sin nafta, aturdida o en el aire no acelera.
                Turbo = s.Mode == DriveMode.Turbo && s.Fuel > 0f && s.StunRemaining <= 0f && !s.Airborne,
                JumpHeight = s.Airborne ? run.HeightToWorld(s.Height) : 0f,
                GoalX = run.ToWorldX(run.Config.goalDistance),
            };
        }

        void OnStepped(RunEvent events)
        {
            if (!GameSettings.CameraEffects || director == null)
                return;

            // Caerse de un salto pega como un choque.
            if ((events & (RunEvent.Crashed | RunEvent.Fell)) != 0)
            {
                director.AddTrauma(config.crashTrauma);
                director.Punch(config.crashPunchForward, config.crashPunchZoom);
                Hitstop(config.hitstopSeconds);
            }

            if ((events & RunEvent.LandedPerfect) != 0)
            {
                director.AddTrauma(config.perfectLandingTrauma);
                director.Punch(config.perfectLandingPunchForward, 0f);
            }

            if ((events & RunEvent.Explosion) != 0)
            {
                director.AddTrauma(config.explosionTrauma);
                director.Punch(0f, 0.2f);
            }

            if ((events & RunEvent.RanOver) != 0)
                director.AddTrauma(config.ramTrauma);

            // --- Pelea a pie ---
            // La moto empieza a frenar sin nafta: un tiron de camara hacia adentro marca el momento.
            if ((events & RunEvent.Frenando) != 0)
            {
                director.AddTrauma(0.18f);
                director.Punch(0f, -0.2f);
            }
            if ((events & RunEvent.PeleaEmpieza) != 0)
            {
                director.AddTrauma(0.3f);
                CamaraLenta(0.6f, 0.3f);
            }
            if ((events & RunEvent.Golpe) != 0)
            {
                director.AddTrauma(0.14f);
                director.Punch(0.12f, 0f);
                Hitstop(0.05f);
            }
            if ((events & RunEvent.Remate) != 0)
            {
                // El remate pisa al golpe comun: mas sacudida, un empujon hacia donde pego y un corte mas largo.
                director.AddTrauma(0.32f);
                director.Punch(0.3f, -0.12f);
                Hitstop(0.1f);
            }
            if ((events & RunEvent.Lastimada) != 0)
            {
                director.AddTrauma(0.4f);
                director.Punch(0f, -0.2f);
                Hitstop(0.08f);
            }
            // Ganar la pelea: el ultimo zombie suelta el bidon.
            if ((events & RunEvent.PeleaGanada) != 0)
                director.AddTrauma(0.25f);
            // Granadas y bombas: la sacudida se siente; la bomba, que se lleva a todos, ademas frena el tiempo un instante.
            if ((events & RunEvent.GranadaLanzada) != 0)
                director.AddTrauma(0.08f);
            if ((events & RunEvent.GranadaExplota) != 0)
                director.AddTrauma(0.3f);
            if ((events & RunEvent.BombaExplota) != 0)
            {
                director.AddTrauma(0.6f);
                CamaraLenta(0.5f, 0.35f);
            }
            if ((events & RunEvent.AuxilioTermina) != 0)
            {
                director.AddTrauma(0.2f);
                director.Punch(0.4f, 0f);
                needsSnap = false;
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
            lentaLeft = 0f;
            Time.timeScale = 1f;
            needsSnap = true;
        }

        /// <summary>Camara lenta por unos segundos reales (el congelado de golpe la respeta).</summary>
        void CamaraLenta(float seconds, float scale)
        {
            if (seconds <= 0f || inFinale)
                return;
            lentaLeft = seconds;
            lentaEscala = Mathf.Clamp(scale, 0.05f, 1f);
            if (hitstopLeft <= 0f && !TimeHeld)
                Time.timeScale = lentaEscala;
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

        /// <summary>
        /// La proxima vez que se actualice la camara, salta directo a la pose (sin interpolar).
        /// La usa el TutorialDirector tras un rebobinado: sin esto la camara cruzaria en un
        /// cuadro todo el tramo que se salteo el jugador.
        /// </summary>
        public void SnapNextFrame() => needsSnap = true;

        /// <summary>Fin del final: el tiempo vuelve a la normalidad antes de la pantalla siguiente.</summary>
        public void EndFinale()
        {
            inFinale = false;
            hitstopLeft = 0f;
            Time.timeScale = 1f;
        }
    }
}
