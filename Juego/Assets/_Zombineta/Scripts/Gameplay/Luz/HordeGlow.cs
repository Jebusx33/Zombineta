using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;

namespace Zombineta.Luz
{
    /// <summary>
    /// El resplandor frio que trae la horda: una luz puntual grande que la sigue y se enciende a
    /// medida que se acerca. El mismo umbral que usa la barra de amenaza del HUD (45 m): cuando la
    /// horda esta a esa distancia o mas lejos no se nota, y llega a maxima intensidad encima tuyo.
    ///
    /// Vive en su propio objeto (el prefab Art/Luz/ResplandorHorda), nunca en "Nivel": mueve su
    /// transform cada cuadro, y si compartiera objeto con el recorrido lo arrastraria entero.
    /// Arte ajusta color, radio, intensidad y distancia en el prefab; valen para todos los niveles.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class HordeGlow : MonoBehaviour
    {
        [SerializeField] RunController run;

        [SerializeField] Color color = new Color(0.35f, 0.85f, 0.65f);
        [Min(0f)] [SerializeField] float radius = 12f;
        [Min(0f)] [SerializeField] float maxIntensity = 1.1f;
        [Min(0.01f)] [SerializeField] float dangerGapMeters = 45f;

        Light2D light2d;

        void Awake()
        {
            // El prefab no puede guardar la referencia a la escena: se busca sola si falta.
            if (run == null)
                run = FindAnyObjectByType<RunController>();
            if (transform.childCount > 0)
                Debug.LogWarning("HordeGlow mueve su objeto cada cuadro: no debe tener hijos.", this);

            light2d = GetComponent<Light2D>();
            light2d.lightType = Light2D.LightType.Point;
            light2d.blendStyleIndex = 1; // Additive, igual que los destellos: no lava el ambiente.
            light2d.targetSortingLayers = new[] { SortingLayer.NameToID("Juego") };
            light2d.color = color;
            light2d.pointLightInnerRadius = 0f;
            light2d.pointLightOuterRadius = radius;
        }

        void Update()
        {
            if (run == null || run.Sim == null || light2d == null)
                return;

            var state = run.Sim.State;
            transform.position = new Vector3(run.ToWorldX(state.HordeX), run.LaneToWorldY(1f), 0f);

            float acercandose = 1f - Mathf.Clamp01(state.Gap / dangerGapMeters);
            light2d.intensity = Mathf.Lerp(0f, maxIntensity, acercandose);
        }
    }
}
