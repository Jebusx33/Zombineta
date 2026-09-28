using UnityEngine;
using Zombineta.Core;
using Zombineta.Fx;

namespace Zombineta.Player
{
    /// <summary>
    /// Presentacion pura: lee el estado, coloca a la jugadora en su carril y tinta el
    /// sprite segun el modo. El sprite vive en un hijo ("Body") para que las animaciones
    /// de spritesheet se le puedan sumar sin tocar esta logica.
    /// </summary>
    public sealed class ScooterView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] SpriteRenderer body;

        [Header("Animacion")]
        [Tooltip("Cuadros parada/andando, en loop.")]
        [SerializeField] Sprite[] idleSprites;
        [Tooltip("Cuadros al bajar un carril: un solo ciclo que vuelve solo a Idle.")]
        [SerializeField] Sprite[] downSprites;
        [Tooltip("Cuadros al subir un carril: un solo ciclo que vuelve solo a Idle.")]
        [SerializeField] Sprite[] upSprites;
        [Tooltip("Cuadros del disparo: un solo ciclo que vuelve solo a Idle.")]
        [SerializeField] Sprite[] shootSprites;
        [Tooltip("Cuadros de la caida (aterrizaje malo de rampa): un solo ciclo que se congela " +
                 "en el ultimo cuadro mientras dure Fallen.")]
        [SerializeField] Sprite[] crashSprites;

        [SerializeField] float idleFps = 4f;
        [SerializeField] float downFps = 16f;
        [SerializeField] float upFps = 16f;
        [SerializeField] float shootFps = 20f;

        PlayerAnim anim;
        int lastLane;

        [Header("Tintes")]
        [Tooltip("Color del personaje elegido. Blanco = el arte tal cual.")]
        [SerializeField] Color characterColor = Color.white;

        // Se multiplican sobre el color del personaje: con arte real, reemplazar el color
        // pintaria todo el dibujo de un tono plano. Un tinte leve alcanza para leer el modo.
        [SerializeField] Color turboTint = new Color(1f, 0.9f, 0.65f);
        [SerializeField] Color reverseTint = new Color(0.75f, 0.88f, 1f);
        [SerializeField] Color stunnedTint = new Color(0.5f, 0.5f, 0.5f);

        [Header("Llegada")]
        [Tooltip("Metros que la moto sigue rodando dentro del refugio al ganar.")]
        [SerializeField] float arrivalCoastMeters = 6f;

        [Header("Salto")]
        [Tooltip("Sombra permanente en el piso del carril: marca donde esta parada o donde va a caer.")]
        [SerializeField] GroundShadow shadow;
        [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.45f);
        [Tooltip("La sombra toma este color si la inclinacion daria aterrizaje perfecto. Solo mientras vuela.")]
        [SerializeField] Color shadowPerfectColor = new Color(0.3f, 1f, 0.4f, 0.65f);
        [Tooltip("Grados de la moto tirada en el piso tras una caida.")]
        [SerializeField] float fallenAngle = 70f;

        // Solo presentacion: la simulacion termina en la meta, la moto entra rodando.
        float coast;

        /// <summary>
        /// Color del personaje elegido. Placeholder de la seleccion de personaje hasta que
        /// haya un set de animaciones por personaje.
        /// </summary>
        public void SetCharacterColor(Color color) => characterColor = color;

        void Awake()
        {
            float crashFps = crashSprites != null && crashSprites.Length > 0 && run != null && run.Config != null
                ? crashSprites.Length / Mathf.Max(0.01f, run.Config.fallStunDuration)
                : 8f;
            anim = new PlayerAnim(
                Largo(idleSprites), Largo(downSprites), Largo(upSprites), Largo(shootSprites), Largo(crashSprites),
                idleFps, downFps, upFps, shootFps, crashFps);
        }

        static int Largo(Sprite[] cuadros) => cuadros != null ? cuadros.Length : 0;

        void OnEnable()
        {
            if (run != null)
            {
                run.Stepped += OnStepped;
                run.Restarted += OnRestarted;
            }
            SincronizarCarril();
        }

        void OnDisable()
        {
            if (run != null)
            {
                run.Stepped -= OnStepped;
                run.Restarted -= OnRestarted;
            }
        }

        void OnRestarted() => SincronizarCarril();

        // Evita un Down/Up fantasma en el primer cuadro tras entrar o reintentar: sin esto,
        // lastLane seguiria en 0 y el primer carril real (normalmente 1) se leeria como un salto.
        void SincronizarCarril()
        {
            if (run != null && run.Sim != null)
                lastLane = run.Sim.State.Lane;
        }

        // Un solo ciclo por evento: Down/Up con el carril de origen, Shoot en cada disparo
        // (pegue o no), Crash en un aterrizaje malo de rampa. Chocar contra un obstaculo solo
        // atonta (tinte gris mas abajo); no tiene cuadros propios todavia.
        void OnStepped(RunEvent events)
        {
            if (run == null || run.Sim == null)
                return;

            var state = run.Sim.State;

            if ((events & RunEvent.LaneChanged) != 0)
            {
                if (state.Lane > lastLane) anim.Play(PlayerClip.Up);
                else if (state.Lane < lastLane) anim.Play(PlayerClip.Down);
            }
            lastLane = state.Lane;

            if ((events & RunEvent.Shot) != 0)
                anim.Play(PlayerClip.Shoot);

            if ((events & RunEvent.Fell) != 0)
                anim.Play(PlayerClip.Crash);
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            var state = run.Sim.State;
            if (state.Phase == RunPhase.Won)
                coast = Mathf.Min(arrivalCoastMeters, coast + run.Config.normalSpeed * 0.6f * Time.deltaTime);
            else if (state.Phase == RunPhase.Running)
                coast = 0f;

            // El clip de Crash lo dispara el evento Fell; lo dejamos cuando el estado deja de
            // estar caido (el struct solo sabe congelarse en el ultimo cuadro, no cuando volver).
            if (!state.Fallen && anim.Clip == PlayerClip.Crash)
                anim.Play(PlayerClip.Idle);
            anim.Tick(Time.deltaTime);

            // El pivot del sprite esta en el contacto de las ruedas: la moto se apoya en la
            // linea del carril, igual que los pies de los zombies. En el aire, sube.
            float laneY = run.LaneToWorldY(state.LaneVisual);
            float lift = state.Airborne ? run.HeightToWorld(state.Height) : 0f;
            transform.position = new Vector3(run.ToWorldX(state.PlayerX + coast), laneY + lift, 0f);

            // Rota sobre las ruedas: la inclinacion del salto. La caida ya no se rota aca si hay
            // arte de Crash (el dibujo ya muestra la moto tirada); sin arte, sigue el placeholder
            // de rotar el sprite entero.
            bool arteDeCaida = crashSprites != null && crashSprites.Length > 0;
            float angle = state.Airborne ? state.Pitch : (state.Fallen && !arteDeCaida) ? fallenAngle : 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            UpdateShadow(state, laneY);

            if (body == null)
                return;

            body.sortingOrder = LaneSorting.Order(state.LaneVisual, SortSlot.Player);
            body.sortingLayerID = LaneSorting.GameLayerId;
            body.sprite = CuadroActual();

            Color tint = Color.white;
            if (state.StunRemaining > 0f || state.Fuel <= 0f)
                tint = stunnedTint;
            else if (state.Mode == DriveMode.Turbo)
                tint = turboTint;
            else if (state.Mode == DriveMode.Reverse)
                tint = reverseTint;

            body.color = characterColor * tint;
        }

        // Sin cuadros asignados para el clip actual, deja el sprite que ya tenia el Renderer (el
        // placeholder de siempre): reemplazo limpio, escena vieja no wireada no rompe.
        Sprite CuadroActual()
        {
            Sprite[] cuadros = anim.Clip switch
            {
                PlayerClip.Down => downSprites,
                PlayerClip.Up => upSprites,
                PlayerClip.Shoot => shootSprites,
                PlayerClip.Crash => crashSprites,
                _ => idleSprites,
            };
            if (cuadros == null || cuadros.Length == 0)
                return body.sprite;
            return cuadros[Mathf.Clamp(anim.Frame, 0, cuadros.Length - 1)];
        }

        void UpdateShadow(RunState state, float laneY)
        {
            if (shadow == null)
                return;

            // Permanente: marca donde esta parada y, si vuela, donde va a caer.
            bool perfect = state.Airborne && Mathf.Abs(state.Pitch) <= run.Config.perfectLandingAngle;
            shadow.SetColor(perfect ? shadowPerfectColor : shadowColor);

            float heightWorld = state.Airborne ? run.HeightToWorld(state.Height) : 0f;
            shadow.Place(transform.position.x, laneY, heightWorld, state.LaneVisual);
        }
    }
}
