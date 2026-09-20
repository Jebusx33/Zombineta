using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Fx
{
    /// <summary>
    /// Sombra en el piso del carril: marca donde esta parado o donde va a caer algo que flota.
    /// Cuanto mas alto, mas chica y mas tenue (se lee la altura sin mirar el objeto). El
    /// renderer que mueve vive aparte del padre para poder dejarlo siempre plano y derecho,
    /// aunque el objeto que proyecta la sombra este inclinado.
    /// </summary>
    public sealed class GroundShadow : MonoBehaviour
    {
        [SerializeField] SpriteRenderer shadow;
        [SerializeField] float width = 1f;

        public float Width { get => width; set => width = value; }

        Vector3 baseScale = Vector3.one;
        float baseAlpha = 1f;

        void Awake()
        {
            if (shadow == null)
                return;
            baseScale = shadow.transform.localScale;
            baseAlpha = shadow.color.a;
        }

        /// <summary>
        /// Arma la sombra a mano cuando no se cablea desde el inspector (se crea por codigo,
        /// como una sombra por zombie o por item del nivel).
        /// </summary>
        public void Init(SpriteRenderer renderer, float shadowWidth = 1f)
        {
            shadow = renderer;
            width = shadowWidth;
            baseScale = shadow != null ? shadow.transform.localScale : Vector3.one;
            baseAlpha = shadow != null ? shadow.color.a : 1f;
        }

        /// <summary>Prende o apaga la sombra (item consumido, zombie recien caido, etc).</summary>
        public bool Visible
        {
            get => shadow != null && shadow.enabled;
            set { if (shadow != null) shadow.enabled = value; }
        }

        /// <summary>
        /// Fija el color base de la sombra (RGB y el alfa de referencia): Place sigue atenuando
        /// ese alfa segun la altura. Sirve para tintes puntuales, como el aviso de aterrizaje
        /// perfecto de la moto.
        /// </summary>
        public void SetColor(Color color)
        {
            if (shadow == null)
                return;
            shadow.color = color;
            baseAlpha = color.a;
        }

        /// <summary>
        /// Ubica la sombra en el piso del carril. heightWorld es cuanto flota el objeto, en
        /// unidades de mundo (ya convertido, no metros de salto).
        /// </summary>
        public void Place(float worldX, float groundY, float heightWorld, float visualLane)
        {
            if (shadow == null)
                return;

            shadow.transform.SetPositionAndRotation(new Vector3(worldX, groundY, 0f), Quaternion.identity);

            float k = Mathf.Clamp01(heightWorld / 4f);
            shadow.transform.localScale = baseScale * width * Mathf.Lerp(1f, 0.55f, k);

            var color = shadow.color;
            color.a = baseAlpha * Mathf.Lerp(1f, 0.6f, k);
            shadow.color = color;

            shadow.sortingOrder = LaneSorting.Order(visualLane, SortSlot.Shadow);
            shadow.sortingLayerName = LaneSorting.GameLayer;
        }
    }
}
