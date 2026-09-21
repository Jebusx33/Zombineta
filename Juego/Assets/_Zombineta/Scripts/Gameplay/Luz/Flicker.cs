using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Zombineta.Luz
{
    /// <summary>
    /// El parpadeo de un neon o un farol viejo, aplicado a un Light2D. Tiempo sin escalar: sigue
    /// titilando aunque el juego este en pausa (timeScale 0), como pasaria con un cartel real.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class Flicker : MonoBehaviour
    {
        [Min(0f)] [SerializeField] float frequency = 1.5f;
        [Min(0f)] [SerializeField] float min = 0.4f;
        [Min(0f)] [SerializeField] float max = 1f;
        [SerializeField] int seed;

        Light2D light2d;

        void Awake()
        {
            light2d = GetComponent<Light2D>();
        }

        void Update()
        {
            light2d.intensity = FlickerMath.Intensity(Time.unscaledTime, seed, frequency, min, max);
        }
    }
}
