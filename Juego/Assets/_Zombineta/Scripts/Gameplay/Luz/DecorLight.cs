using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Zombineta.Luz
{
    /// <summary>
    /// Una luz de adorno: cartel, farol de vidriera, lo que decora sin hacer falta para jugar.
    /// Se anota sola en una lista estatica compartida al habilitarse, para que LightBudgetRunner
    /// decida, entre todas las que hay en pantalla, cuales se banca la maquina.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class DecorLight : MonoBehaviour
    {
        [SerializeField] int priority;

        static readonly List<DecorLight> registradas = new List<DecorLight>();

        /// <summary>Todas las DecorLight habilitadas ahora mismo, en cualquier escena cargada.</summary>
        public static IReadOnlyList<DecorLight> Registradas => registradas;

        public int Priority => priority;
        public Light2D Light { get; private set; }

        // Sin recarga de dominio en este proyecto: sin este reinicio, una lista vieja de la sesion
        // de Play anterior seguiria viva con objetos ya destruidos.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarEstatico() => registradas.Clear();

        void Awake()
        {
            Light = GetComponent<Light2D>();
        }

        void OnEnable()
        {
            registradas.Add(this);
        }

        void OnDisable()
        {
            registradas.Remove(this);
        }
    }
}
