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
            // El pivot del sprite esta en el contacto de las ruedas: la moto se apoya en la
            // linea del carril, igual que los pies de los zombies.
            transform.position = new Vector3(
                run.ToWorldX(state.PlayerX), run.LaneToWorldY(state.LaneVisual), 0f);

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
    }
}
