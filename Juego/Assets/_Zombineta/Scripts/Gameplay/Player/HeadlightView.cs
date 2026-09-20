using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;

namespace Zombineta.Player
{
    /// <summary>
    /// El faro, como luz 2D de verdad. No es decoracion: es lo que hace que la
    /// mecanica se entienda sin tutorial. Prender la luz y ver que la horda
    /// afloja tiene que leerse de una, y para eso la calle esta a oscuras.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class HeadlightView : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Tooltip("Intensidad con la bateria llena.")]
        [SerializeField] float fullIntensity = 1.6f;

        [Tooltip("Fraccion de bateria por debajo de la cual la luz empieza a titilar.")]
        [Range(0f, 1f)] [SerializeField] float flickerBelow = 0.25f;

        [SerializeField] float flickerSpeed = 18f;

        Light2D light2d;

        void Awake()
        {
            light2d = GetComponent<Light2D>();

            // El faro tiene que alcanzar la calle (el piso que se ve) y el juego (moto, zombies,
            // items): las mismas dos capas que los destellos. Por targetSortingLayers, la API
            // publica de Light2D, no por SerializedObject (ver Task 3: eso se compila fuera del
            // build y deja las luces sin capas asignadas).
            light2d.targetSortingLayers = new[]
            {
                SortingLayer.NameToID("Calle"),
                SortingLayer.NameToID("Juego"),
            };
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null || light2d == null)
                return;

            var state = run.Sim.State;
            if (!state.HeadlightOn)
            {
                light2d.enabled = false;
                return;
            }

            light2d.enabled = true;

            float charge = run.Config.batteryMax <= 0f
                ? 0f
                : state.Battery / run.Config.batteryMax;

            // Con la bateria baja la luz titila: avisa que se te acaba el recurso
            // antes de que se apague sola, sin necesidad de mirar el HUD.
            float intensity = fullIntensity;
            if (charge < flickerBelow)
            {
                float t = Mathf.PingPong(Time.time * flickerSpeed, 1f);
                intensity *= Mathf.Lerp(0.35f, 1f, t);
            }

            light2d.intensity = intensity;
        }
    }
}
