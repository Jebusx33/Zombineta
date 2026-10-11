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

        [Header("Apagon")]
        [Tooltip("Unidades de mundo hacia adelante donde nace el haz delantero.")]
        [SerializeField] float frontOffset = 2f;

        [Tooltip("Intensidad del haz delantero en un apagon total, como fraccion de la del faro.")]
        [SerializeField] float frontIntensity = 1f;

        Light2D light2d;

        // El faro apunta hacia atras, a la horda. En un apagon tambien hace falta ver la calle:
        // este segundo haz, igual al primero pero hacia adelante, solo se prende ahi.
        Light2D front;

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

            front = CrearHazDelantero();
        }

        Light2D CrearHazDelantero()
        {
            var go = new GameObject("Haz delantero");
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

            var haz = go.AddComponent<Light2D>();
            haz.lightType = Light2D.LightType.Point;
            haz.blendStyleIndex = light2d.blendStyleIndex;
            haz.color = light2d.color;
            haz.pointLightInnerRadius = light2d.pointLightInnerRadius;
            haz.pointLightOuterRadius = light2d.pointLightOuterRadius;
            haz.pointLightInnerAngle = light2d.pointLightInnerAngle;
            haz.pointLightOuterAngle = light2d.pointLightOuterAngle;
            haz.falloffIntensity = light2d.falloffIntensity;
            haz.shadowsEnabled = light2d.shadowsEnabled;
            haz.shadowIntensity = light2d.shadowIntensity;
            haz.targetSortingLayers = light2d.targetSortingLayers;
            haz.enabled = false;
            return haz;
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null || light2d == null)
                return;

            var state = run.Sim.State;
            if (!state.HeadlightOn)
            {
                light2d.enabled = false;
                front.enabled = false;
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

            // Fuera de un apagon el haz delantero no existe: la luz del nivel queda como siempre.
            float oscuridad = run.Sim.Oscuridad;
            front.enabled = oscuridad > 0.01f;
            if (front.enabled)
            {
                front.intensity = intensity * frontIntensity * oscuridad;
                front.transform.position = transform.position + Vector3.right * frontOffset;
            }
        }
    }
}
