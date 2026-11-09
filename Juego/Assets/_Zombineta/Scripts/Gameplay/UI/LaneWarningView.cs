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

        [Tooltip("Cada icono sigue la altura en pantalla de su carril (la camara hace zoom: un " +
                 "icono fijo se despega de su carril y parece avisar en otro).")]
        [SerializeField] bool anclarAlCarril = true;
        [Tooltip("Unidades de mundo sobre el centro del carril donde va el centro del icono. 0 = centrado en su carril.")]
        [SerializeField] float alturaSobreCarril = 0f;
        [Tooltip("Alto del icono, en carriles: menos de 1 para que dos avisos seguidos no se pisen ni parezcan de otro carril.")]
        [Range(0.3f, 1.2f)] [SerializeField] float altoEnCarriles = 0.85f;

        /// <summary>
        /// Lleva el icono a la altura en pantalla de su carril y lo achica o agranda a la medida del carril
        /// (Canvas Screen Space - Overlay): con el zoom de la camara los carriles cambian de tamano en pantalla.
        /// </summary>
        void Anclar(RectTransform icono, int carril)
        {
            var cam = Camera.main;
            if (!anclarAlCarril || cam == null)
                return;
            float x = cam.transform.position.x;
            float y = cam.WorldToScreenPoint(new Vector3(x, run.LaneToWorldY(carril) + alturaSobreCarril, 0f)).y;
            float y0 = cam.WorldToScreenPoint(new Vector3(x, run.LaneToWorldY(0), 0f)).y;
            float y1 = cam.WorldToScreenPoint(new Vector3(x, run.LaneToWorldY(1), 0f)).y;
            var p = icono.position;
            icono.position = new Vector3(p.x, y, p.z);

            var canvas = icono.GetComponentInParent<Canvas>();
            float escalaCanvas = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            icono.localScale = Vector3.one * EscalaDelIcono(Mathf.Abs(y1 - y0), icono.rect.height * escalaCanvas, altoEnCarriles);
        }

        /// <summary>Regla pura: cuanto escalar un icono de altoIconoPx para que mida altoEnCarriles carriles de separacionPx.</summary>
        public static float EscalaDelIcono(float separacionPx, float altoIconoPx, float altoEnCarriles) =>
            altoIconoPx <= 0f || separacionPx <= 0f ? 1f : separacionPx * altoEnCarriles / altoIconoPx;

        void LateUpdate()
        {
            if (run == null || run.Level == null || run.Sim == null)
                return;

            // A pie no hay carriles que cuidar: los avisos se van mientras dura la pelea.
            bool peleando = run.Sim.State.Phase == RunPhase.Pelea;

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

                if (peleando || entry.distance <= near || entry.distance > far)
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
                    Anclar(icon.rectTransform, i);
                }
            }
        }
    }
}
