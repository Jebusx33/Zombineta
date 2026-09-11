using UnityEngine;
using Zombineta.Core;

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
        [Tooltip("Sombra en el carril mientras vuela: marca donde va a caer.")]
        [SerializeField] SpriteRenderer shadow;
        [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.45f);
        [Tooltip("La sombra toma este color si la inclinacion daria aterrizaje perfecto.")]
        [SerializeField] Color shadowPerfectColor = new Color(0.3f, 1f, 0.4f, 0.65f);
        [Tooltip("Grados de la moto tirada en el piso tras una caida.")]
        [SerializeField] float fallenAngle = 70f;

        // Solo presentacion: la simulacion termina en la meta, la moto entra rodando.
        float coast;

        Vector3 shadowBaseScale = Vector3.one;

        void Awake()
        {
            if (shadow != null)
                shadowBaseScale = shadow.transform.localScale;
        }

        /// <summary>
        /// Color del personaje elegido. Placeholder de la seleccion de personaje hasta que
        /// haya un set de animaciones por personaje.
        /// </summary>
        public void SetCharacterColor(Color color) => characterColor = color;

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            var state = run.Sim.State;
            if (state.Phase == RunPhase.Won)
                coast = Mathf.Min(arrivalCoastMeters, coast + run.Config.normalSpeed * 0.6f * Time.deltaTime);
            else if (state.Phase == RunPhase.Running)
                coast = 0f;

            // El pivot del sprite esta en el contacto de las ruedas: la moto se apoya en la
            // linea del carril, igual que los pies de los zombies. En el aire, sube.
            float laneY = run.LaneToWorldY(state.LaneVisual);
            float lift = state.Airborne ? run.HeightToWorld(state.Height) : 0f;
            transform.position = new Vector3(run.ToWorldX(state.PlayerX + coast), laneY + lift, 0f);

            // Rota sobre las ruedas: la inclinacion del salto, o tirada tras una caida.
            float angle = state.Airborne ? state.Pitch : state.Fallen ? fallenAngle : 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            UpdateShadow(state, laneY);

            if (body == null)
                return;

            Color tint = Color.white;
            if (state.StunRemaining > 0f || state.Fuel <= 0f)
                tint = stunnedTint;
            else if (state.Mode == DriveMode.Turbo)
                tint = turboTint;
            else if (state.Mode == DriveMode.Reverse)
                tint = reverseTint;

            body.color = characterColor * tint;
        }

        void UpdateShadow(RunState state, float laneY)
        {
            if (shadow == null)
                return;

            if (shadow.enabled != state.Airborne)
                shadow.enabled = state.Airborne;
            if (!state.Airborne)
                return;

            // Queda en el carril, derecha aunque la moto este inclinada.
            shadow.transform.SetPositionAndRotation(
                new Vector3(transform.position.x, laneY, 0f), Quaternion.identity);

            // Mas alto, mas chica: se lee la altura sin mirar la moto.
            float k = Mathf.Clamp01(run.HeightToWorld(state.Height) / 4f);
            shadow.transform.localScale = shadowBaseScale * Mathf.Lerp(1f, 0.55f, k);

            bool perfect = Mathf.Abs(state.Pitch) <= run.Config.perfectLandingAngle;
            shadow.color = perfect ? shadowPerfectColor : shadowColor;
        }
    }
}
