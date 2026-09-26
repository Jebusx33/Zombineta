using UnityEngine;

namespace Zombineta.Juego
{
	[ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BuildingColorModifier : MonoBehaviour
    {
		[SerializeField] private Color grayAreaColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color whiteAreaColor = Color.white;
        [Range(0.1f, 0.95f)]
        [SerializeField] private float splitThreshold = 0.75f;

        private SpriteRenderer spriteRenderer;
        private MaterialPropertyBlock propertyBlock;

        private static readonly int GrayColorId = Shader.PropertyToID("_GrayColor");
        private static readonly int WhiteColorId = Shader.PropertyToID("_WhiteColor");
        private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyColors();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            ApplyColors();
        }

        public void ApplyColors()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(GrayColorId, grayAreaColor);
            propertyBlock.SetColor(WhiteColorId, whiteAreaColor);
            propertyBlock.SetFloat(ThresholdId, splitThreshold);
            spriteRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
