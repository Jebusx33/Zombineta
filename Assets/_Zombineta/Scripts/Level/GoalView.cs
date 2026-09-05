using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Level
{
    /// <summary>
    /// El refugio: un porton que cruza los tres carriles al final del recorrido.
    /// La condicion de victoria la resuelve la simulacion; esto solo lo dibuja,
    /// para que la meta se vea llegar y no aparezca de la nada.
    /// </summary>
    public sealed class GoalView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Transform gate;

        void Start()
        {
            if (run == null || gate == null || run.Config == null)
                return;

            gate.position = new Vector3(run.ToWorldX(run.Config.goalDistance), 0f, 0f);

            // Alto suficiente para tapar los tres carriles y leerse como pared.
            float height = run.Config.laneSpacing * 3.4f;
            gate.localScale = new Vector3(0.6f, height, 1f);
        }
    }
}
