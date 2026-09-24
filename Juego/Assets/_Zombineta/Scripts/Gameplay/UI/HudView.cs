using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.UI
{
    /// <summary>
    /// HUD de barras, sin numeros. En un juego de reaccion nadie lee cifras: lo
    /// que importa es cuanto queda y cuan cerca esta la horda, y eso se lee mejor
    /// como longitud y color. Los numeros crudos siguen disponibles en
    /// DebugHudView para balancear.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Header("Barras (Image en modo Filled)")]
        [SerializeField] Image fuelFill;
        [SerializeField] Image batteryFill;
        [SerializeField] Image progressFill;

        [Tooltip("Se llena a medida que la horda se acerca: lleno = encima tuyo.")]
        [SerializeField] Image threatFill;

        [Header("Balas")]
        [Tooltip("Un objeto por bala. Se apagan a medida que se gastan.")]
        [SerializeField] Image[] ammoPips;

        [Header("Colores")]
        [SerializeField] Color fuelOk = new Color(1f, 0.65f, 0.15f);
        [SerializeField] Color fuelLow = new Color(0.95f, 0.25f, 0.2f);
        [SerializeField] Color threatFar = new Color(0.4f, 0.8f, 0.4f);
        [SerializeField] Color threatNear = new Color(0.95f, 0.2f, 0.2f);

        [Tooltip("Distancia a la horda, en metros, a partir de la cual la barra grita.")]
        [SerializeField] float dangerGapMeters = 45f;

        // --- Accesores de solo lectura: el TutorialDirector los usa para el marco que resalta
        // la barra del paso actual, sin duplicar referencias a las mismas Image en la escena. ---

        /// <summary>El fondo de la barra de nafta (el padre de fuelFill), o null si no esta asignada.</summary>
        public RectTransform FuelBarRect => BackgroundOf(fuelFill);

        /// <summary>El fondo de la barra de bateria, o null si no esta asignada.</summary>
        public RectTransform BatteryBarRect => BackgroundOf(batteryFill);

        /// <summary>El fondo de la barra de amenaza (horda), o null si no esta asignada.</summary>
        public RectTransform ThreatBarRect => BackgroundOf(threatFill);

        /// <summary>Las balas (pips), para resaltarlas todas juntas. Nunca null; puede estar vacio.</summary>
        public RectTransform[] AmmoPipRects
        {
            get
            {
                if (ammoPips == null)
                    return System.Array.Empty<RectTransform>();

                var list = new List<RectTransform>(ammoPips.Length);
                foreach (var pip in ammoPips)
                    if (pip != null)
                        list.Add(pip.rectTransform);
                return list.ToArray();
            }
        }

        static RectTransform BackgroundOf(Image fill) =>
            fill != null ? fill.rectTransform.parent as RectTransform : null;

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            var s = run.Sim.State;
            var cfg = run.Config;

            if (fuelFill != null)
            {
                float f = cfg.fuelMax <= 0f ? 0f : s.Fuel / cfg.fuelMax;
                fuelFill.fillAmount = f;
                fuelFill.color = Color.Lerp(fuelLow, fuelOk, Mathf.InverseLerp(0f, 0.3f, f));
            }

            if (batteryFill != null)
                batteryFill.fillAmount =
                    cfg.batteryMax <= 0f ? 0f : s.Battery / cfg.batteryMax;

            if (progressFill != null)
                progressFill.fillAmount = run.Sim.Progress01;

            if (threatFill != null)
            {
                // Invertida a proposito: barra llena = peligro maximo.
                float threat = 1f - Mathf.Clamp01(s.Gap / dangerGapMeters);
                threatFill.fillAmount = threat;
                threatFill.color = Color.Lerp(threatFar, threatNear, threat);
            }

            if (ammoPips != null)
                for (int i = 0; i < ammoPips.Length; i++)
                    if (ammoPips[i] != null)
                        ammoPips[i].enabled = i < s.Ammo;
        }
    }
}
