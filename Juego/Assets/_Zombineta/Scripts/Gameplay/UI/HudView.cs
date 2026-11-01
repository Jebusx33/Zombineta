using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.UI
{
    /// <summary>
    /// El HUD de la partida. Los paneles de recursos, armamento y avance estan en HudPaneles; aca queda
    /// el sensor de la horda, que no es parte del prefab. Los numeros crudos siguen disponibles en
    /// DebugHudView para balancear.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Tooltip("El prefab HudPartida de la escena.")]
        [SerializeField] HudPaneles paneles;

        [Header("Sensor de la horda")]
        [Tooltip("El equipo decidio no usarlo por ahora: con esto apagado no se ve en ningun momento.")]
        [SerializeField] bool mostrarSensor;

        [Tooltip("Se esconde en la pelea a pie: ahi no hay horda que perseguir.")]
        [SerializeField] GameObject sensor;

        [Tooltip("Se llena a medida que la horda se acerca: lleno = encima tuyo.")]
        [SerializeField] Image threatFill;

        [Tooltip("Texto que muestra la distancia de la horda al jugador en metros (ej. '99m'). Llega a 0 cuando la horda alcanza al jugador.")]
        [SerializeField] TMP_Text threatDistance;

        [Header("Colores")]
        [SerializeField] Color threatFar = new Color(0.4f, 0.8f, 0.4f);
        [SerializeField] Color threatNear = new Color(0.95f, 0.2f, 0.2f);

        [Tooltip("Distancia a la horda, en metros, a partir de la cual la barra grita.")]
        [SerializeField] float dangerGapMeters = 45f;

        [Tooltip("Distancia en metros por debajo de la cual el texto de amenaza se pone rojo.")]
        [SerializeField] float dangerTextMeters = 10f;

        public HudPaneles Paneles => paneles;

        // --- Accesores de solo lectura: el TutorialDirector los usa para el marco que resalta
        // la parte del HUD del paso actual, sin duplicar referencias en la escena. ---

        /// <summary>El indicador de nafta, o null si no hay paneles.</summary>
        public RectTransform FuelBarRect => paneles != null ? paneles.NaftaRect : null;

        /// <summary>El indicador de bateria, o null si no hay paneles.</summary>
        public RectTransform BatteryBarRect => paneles != null ? paneles.BateriaRect : null;

        /// <summary>El RectTransform de la barra de amenaza (horda), o null si el sensor no se muestra.</summary>
        public RectTransform ThreatBarRect => mostrarSensor && threatFill != null ? threatFill.rectTransform : null;

        /// <summary>El contador de balas, para resaltarlo. Nunca null; puede estar vacio.</summary>
        public RectTransform[] AmmoPipRects =>
            paneles != null ? new[] { paneles.BalasRect } : System.Array.Empty<RectTransform>();

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            var s = run.Sim.State;
            var cfg = run.Config;

            if (paneles != null)
                paneles.Dibujar(s, cfg, run.Sim.Progress01);

            if (sensor != null)
            {
                bool verSensor = mostrarSensor && s.Phase != RunPhase.Pelea;
                if (sensor.activeSelf != verSensor)
                    sensor.SetActive(verSensor);
            }

            if (threatFill != null)
            {
                // Invertida a proposito: barra llena = peligro maximo.
                float threat = 1f - Mathf.Clamp01(s.Gap / dangerGapMeters);
                threatFill.fillAmount = threat;
                threatFill.color = Color.Lerp(threatFar, threatNear, threat);
            }

            if (threatDistance != null)
            {
                float gap = Mathf.Max(0f, s.Gap);
                threatDistance.text = Mathf.RoundToInt(gap) + "m";
                threatDistance.color = gap < dangerTextMeters ? threatNear : threatFar;
            }
        }
    }
}
