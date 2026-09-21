using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>
    /// El unico lugar que escribe AudioSource.volume. Cada cuadro combina el volumen propio del
    /// sonido (elegido al azar dentro de su rango), la atenuacion espacial (1 si no es posicional)
    /// y la ganancia del bus (que ya trae el General y el ducking de pausa).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class FuenteConBus : MonoBehaviour
    {
        public AudioBus bus;
        public float volumenPropio = 1f;

        [Tooltip("Atenuacion espacial (0-1). La escribe quien maneja la fuente: 1 si no es posicional.")]
        public float espacial = 1f;

        AudioSource source;

        void Awake() => source = GetComponent<AudioSource>();

        void LateUpdate()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            source.volume = volumenPropio * espacial * AudioDirector.Buses.Gain(bus);
        }
    }
}
