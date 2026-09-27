using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;
using Zombineta.Level;

namespace Zombineta.UI
{
    /// <summary>
    /// Muestra un icono parpadeante por carril cuando hay un obstaculo (Obstacle o ZombieFront)
    /// entre hideDistance y warnDistance metros por delante del jugador.
    /// Los iconos viven en el Canvas del HUD, posicionados a mano en el Inspector sobre el borde
    /// derecho de la pantalla, uno por carril (lane 0 = abajo, 1 = centro, 2 = arriba).
    /// </summary>
    public sealed class LaneWarningView : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Tooltip("Icono por carril: index 0 = carril inferior, 1 = central, 2 = superior.")]
        [SerializeField] Image[] laneIcons = new Image[3];

        [Tooltip("Metros de anticipacion: el icono aparece cuando el obstaculo esta a esta distancia.")]
        [SerializeField] float warnDistance = 30f;

        [Tooltip("Metros minimos: el icono desaparece cuando el obstaculo ya esta a esta distancia o menos.")]
        [SerializeField] float hideDistance = 10f;

        [Tooltip("Velocidad de parpadeo en Hz.")]
        [SerializeField] float blinkHz = 3f;

        void LateUpdate()
        {
            if (run == null || run.Level == null || run.Sim == null)
                return;

            float playerX = run.Sim.State.PlayerX;
            float near = playerX + hideDistance;
            float far = playerX + warnDistance;

            bool[] active = new bool[RunSimulation.LaneCount];

            foreach (var item in run.Level.Items)
            {
                if (item.Consumed) continue;

                var entry = item.Entry;
                if (entry.kind != LevelEntryKind.Obstacle && entry.kind != LevelEntryKind.ZombieFront)
                    continue;

                if (entry.distance <= near || entry.distance > far)
                    continue;

                int lane = entry.lane;
                if ((uint)lane < (uint)RunSimulation.LaneCount)
                    active[lane] = true;
            }

            float t = (Mathf.Sin(Time.time * blinkHz * Mathf.PI * 2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.15f, 1f, t);

            for (int i = 0; i < laneIcons.Length && i < RunSimulation.LaneCount; i++)
            {
                var icon = laneIcons[i];
                if (icon == null) continue;

                bool show = active[i];
                if (icon.gameObject.activeSelf != show)
                    icon.gameObject.SetActive(show);

                if (show)
                {
                    var c = icon.color;
                    c.a = alpha;
                    icon.color = c;
                }
            }
        }
    }
}
