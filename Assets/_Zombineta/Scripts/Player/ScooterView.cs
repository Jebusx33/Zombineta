using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Player
{
    /// <summary>
    /// Presentacion pura: lee el estado y coloca el sprite. En greybox es un
    /// cuadrado de color; en la Fase 5 se cambia el SpriteRenderer y nada mas.
    /// </summary>
    public sealed class ScooterView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] SpriteRenderer body;

        [Header("Colores de greybox")]
        [SerializeField] Color normalColor = new Color(1f, 0.35f, 0.6f);
        [SerializeField] Color turboColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] Color reverseColor = new Color(0.4f, 0.7f, 1f);
        [SerializeField] Color stunnedColor = new Color(0.6f, 0.6f, 0.6f);

        /// <summary>
        /// Color base del personaje elegido. Placeholder de la seleccion de personaje hasta
        /// que haya sprites: cuando lleguen, esto pasa a elegir el set de animaciones.
        /// </summary>
        public void SetCharacterColor(Color color) => normalColor = color;

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            var state = run.Sim.State;
            transform.position = new Vector3(
                run.ToWorldX(state.PlayerX), run.LaneToWorldY(state.LaneVisual), 0f);

            if (body == null)
                return;

            if (state.StunRemaining > 0f)
                body.color = stunnedColor;
            else if (state.Fuel <= 0f)
                body.color = stunnedColor;
            else if (state.Mode == DriveMode.Turbo)
                body.color = turboColor;
            else if (state.Mode == DriveMode.Reverse)
                body.color = reverseColor;
            else
                body.color = normalColor;
        }
    }
}
