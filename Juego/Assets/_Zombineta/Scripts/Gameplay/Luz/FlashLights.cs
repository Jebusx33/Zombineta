using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;
using Zombineta.Enemies;

namespace Zombineta.Luz
{
    /// <summary>
    /// Vista del FlashDirector: un pool de Light2D puntuales, apagadas hasta que algo las prende.
    /// Cada disparo, choque, atropello o explosion arranca un destello aditivo en la posicion de la
    /// moto, salvo atropello y explosion, que usan la posicion real del evento en la horda cuando
    /// esta disponible (para que el destello quede donde reventaron el particulas, no en la moto).
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class FlashLights : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] FlashConfig config;
        [SerializeField] int capacity = 8;

        FlashDirector director;
        Light2D[] pool;
        int[] targetLayers;

        void Awake()
        {
            director = new FlashDirector(config, capacity);
            targetLayers = new[] { SortingLayer.NameToID("Calle"), SortingLayer.NameToID("Juego") };

            pool = new Light2D[capacity];
            for (int i = 0; i < capacity; i++)
                pool[i] = CrearLuz(i);
        }

        Light2D CrearLuz(int i)
        {
            var go = new GameObject("Destello " + i);
            go.transform.SetParent(transform, false);

            var luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.blendStyleIndex = 1; // Additive: se suma a lo que ya esta iluminado, no lo reemplaza.
            luz.targetSortingLayers = targetLayers;
            luz.pointLightInnerRadius = 0f;
            luz.enabled = false;
            return luz;
        }

        void OnEnable()
        {
            if (run != null)
                run.Stepped += OnStepped;
        }

        void OnDisable()
        {
            if (run != null)
                run.Stepped -= OnStepped;
        }

        void OnStepped(RunEvent events)
        {
            if (run.Sim == null || GameSettings.LightQuality == LightQuality.Baja)
                return;

            var state = run.Sim.State;
            var atMoto = new Vector2(run.ToWorldX(state.PlayerX), run.LaneToWorldY(state.LaneVisual));

            if ((events & RunEvent.Shot) != 0)
                director.Trigger(FlashKind.Shot, atMoto);
            if ((events & RunEvent.Crashed) != 0)
                director.Trigger(FlashKind.Crash, atMoto);
            if ((events & RunEvent.RanOver) != 0)
                director.Trigger(FlashKind.RanOver, HordeEventPosition(HordeEventKind.Death, DeathCause.RunOver, atMoto));
            if ((events & RunEvent.Explosion) != 0)
                director.Trigger(FlashKind.Explosion, HordeEventPosition(HordeEventKind.Explosion, DeathCause.None, atMoto));
        }

        /// <summary>
        /// Busca en los eventos de la horda de este mismo tick el que corresponde (mismo que lee
        /// FxManager para sus particulas): asi el destello queda donde paso la cosa, no en la moto.
        /// Si no lo encuentra (no deberia pasar, pero por las dudas) usa la posicion de la moto.
        /// </summary>
        Vector2 HordeEventPosition(HordeEventKind kind, DeathCause cause, Vector2 fallback)
        {
            var horde = run.Sim.Horde;
            var eventos = horde.Events;
            for (int i = 0; i < eventos.Count; i++)
            {
                var e = eventos[i];
                if (e.Kind != kind)
                    continue;
                if (kind == HordeEventKind.Death && e.Cause != cause)
                    continue;
                return new Vector2(run.ToWorldX(e.X), run.LaneToWorldY(e.Lane));
            }
            return fallback;
        }

        void Update()
        {
            director.Tick(Time.unscaledDeltaTime);

            int vivos = director.Count;
            for (int i = 0; i < pool.Length; i++)
            {
                var luz = pool[i];
                if (i < vivos && director.TryGet(i, out var at, out var color, out var intensity, out var radius))
                {
                    luz.enabled = true;
                    luz.transform.position = at;
                    luz.color = color;
                    luz.intensity = intensity;
                    luz.pointLightOuterRadius = radius;
                }
                else
                {
                    luz.enabled = false;
                }
            }
        }
    }
}
