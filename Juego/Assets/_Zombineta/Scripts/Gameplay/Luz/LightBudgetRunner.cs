using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Luz
{
    /// <summary>
    /// Cada intervalSeconds (tiempo sin escalar, no depende de pausas ni timescale) arma la lista
    /// de candidatas con las DecorLight vivas y le pide a LightBudget.Choose cuales quedan
    /// prendidas segun la X de la camara. En calidad Baja el presupuesto es 0: ninguna luz de
    /// adorno queda prendida. Reusa las listas de candidatos y resultado: sin asignaciones por
    /// frame (solo puede crecer el backing array la primera vez que hay mas luces que antes).
    /// </summary>
    public sealed class LightBudgetRunner : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] int budgetAlta = 12;
        [SerializeField] float intervalSeconds = 0.2f;

        readonly List<LightCandidate> candidatos = new List<LightCandidate>();
        readonly List<bool> prendidas = new List<bool>();
        float proximaAplicacion;

        void Awake()
        {
            if (cam == null)
                cam = Camera.main;
        }

        void Start()
        {
            Aplicar();
        }

        void Update()
        {
            if (Time.unscaledTime < proximaAplicacion)
                return;
            proximaAplicacion = Time.unscaledTime + intervalSeconds;
            Aplicar();
        }

        void Aplicar()
        {
            var registradas = DecorLight.Registradas;

            candidatos.Clear();
            for (int i = 0; i < registradas.Count; i++)
                candidatos.Add(new LightCandidate(registradas[i].transform.position.x, registradas[i].Priority));

            int budget = GameSettings.LightQuality == LightQuality.Baja ? 0 : budgetAlta;
            float cameraX = cam != null ? cam.transform.position.x : 0f;
            LightBudget.Choose(candidatos, cameraX, budget, prendidas);

            for (int i = 0; i < registradas.Count; i++)
            {
                var luz = registradas[i].Light;
                if (luz != null)
                    luz.enabled = prendidas[i];
            }
        }
    }
}
