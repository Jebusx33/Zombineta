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

            body.sortingOrder = LaneSorting.Order(state.LaneVisual, SortSlot.Player);

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

            // Permanente: marca donde esta parada y, si vuela, donde va a caer.
            bool perfect = state.Airborne && Mathf.Abs(state.Pitch) <= run.Config.perfectLandingAngle;
            shadow.SetColor(perfect ? shadowPerfectColor : shadowColor);

            float heightWorld = state.Airborne ? run.HeightToWorld(state.Height) : 0f;
            shadow.Place(transform.position.x, laneY, heightWorld, state.LaneVisual);
        }
    }
}
