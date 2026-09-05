using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Level
{
    /// <summary>
    /// Rayas de la calzada. No es decoracion: sin una referencia de suelo que
    /// pase, la velocidad no se percibe y el turbo no se siente distinto del
    /// modo normal. Se reciclan alrededor de la camara, asi el nivel puede medir
    /// kilometros con un punado de objetos.
    /// </summary>
    public sealed class LaneMarkersView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Transform dashPrefab;
        [SerializeField] Transform followTarget;

        [Tooltip("Separacion entre rayas, en metros.")]
        [SerializeField] float spacing = 6f;

        [Tooltip("Cuantas rayas mantiene vivas cada linea. Tiene que cubrir el ancho de pantalla.")]
        [SerializeField] int dashesPerLine = 20;

        // Bordes de los tres carriles, en coordenadas de carril.
        static readonly float[] LineLanes = { -0.5f, 0.5f, 1.5f, 2.5f };

        Transform[][] dashes;

        void Start()
        {
            if (run == null || dashPrefab == null)
                return;

            dashes = new Transform[LineLanes.Length][];
            for (int line = 0; line < LineLanes.Length; line++)
            {
                dashes[line] = new Transform[dashesPerLine];
                for (int i = 0; i < dashesPerLine; i++)
                    dashes[line][i] = Instantiate(dashPrefab, transform);
            }
        }

        void LateUpdate()
        {
            if (dashes == null || followTarget == null)
                return;

            // spacing esta en metros; en pantalla se mide en unidades de mundo.
            float step = run.ToWorldX(spacing);
            if (step <= 0f)
                return;

            // Anclar la grilla a un multiplo de step: al avanzar la camara las
            // rayas saltan exactamente un espacio, lo cual es invisible.
            float anchor = Mathf.Floor(followTarget.position.x / step) * step;
            int half = dashesPerLine / 2;

            for (int line = 0; line < LineLanes.Length; line++)
            {
                float y = run.LaneToWorldY(LineLanes[line]);
                for (int i = 0; i < dashesPerLine; i++)
                    dashes[line][i].position = new Vector3(anchor + (i - half) * step, y, 0f);
            }
        }
    }
}
