using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>
    /// Un efecto: sus variantes de clip y el rango de volumen y tono con el que suena.
    /// Reproducirlo (elegir variante, aplicar bus, panear) es trabajo del director.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Audio/Sonido", fileName = "Sonido")]
    public sealed class Sonido : ScriptableObject
    {
        public AudioClip[] clips;

        public Vector2 volumen = new Vector2(0.9f, 1f);
        public Vector2 tono = new Vector2(0.95f, 1.05f);

        public AudioBus bus = AudioBus.Efectos;

        public bool posicional;
        public bool loop;

        [Tooltip("Segundos minimos entre dos reproducciones: evita que diez disparos en un frame suenen diez veces.")]
        public float cooldown = 0.05f;
    }
}
