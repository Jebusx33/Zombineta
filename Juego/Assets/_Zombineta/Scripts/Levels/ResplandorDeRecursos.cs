using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;
using Zombineta.Level;

namespace Zombineta.Juego.Levels
{
    /// <summary>
    /// El resplandor de lo que se puede agarrar en la moto (nafta, bateria, balas, granadas y bombas): una luz
    /// puntual Multiply (apenas tenida de su color, como las de los prefabs) sobre cada uno de los mas cercanos
    /// por delante, que se hace mas fuerte con la oscuridad: realza la pieza sin pintarla encima. Asi se ven bien
    /// aun en un apagon, sin depender de las luces de adorno de los prefabs (que
    /// compiten por un presupuesto y en calidad Baja se apagan). Solo lee el recorrido y la posicion de la moto.
    /// Se crea sola al dar Play (no hay que ponerla en ninguna escena).
    /// </summary>
    public sealed class ResplandorDeRecursos : MonoBehaviour
    {
        const float SegundosBusqueda = 0.5f;
        const int MaxLuces = 8;
        const int MaxLucesBaja = 3;

        static ResplandorDeRecursos instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarStatic() => instancia = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null)
                return;
            var go = new GameObject("ResplandorDeRecursos");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<ResplandorDeRecursos>();
        }

        [Tooltip("Desde que distancia (en metros) por delante de la moto empieza a brillar un recurso.")]
        [SerializeField] float visibleDesde = 42f;
        [Tooltip("Cuanto se pasa de la moto (metros hacia atras) antes de apagarse.")]
        [SerializeField] float visibleHasta = 3f;
        [Tooltip("Radio de la luz en unidades de mundo, con luz de dia y a oscuras.")]
        [SerializeField] Vector2 radio = new Vector2(2.2f, 3f);
        [Tooltip("Intensidad de la luz con luz de dia y a oscuras.")]
        [SerializeField] Vector2 intensidad = new Vector2(0.5f, 1.1f);
        [Tooltip("Cuanto se mezcla el color de la luz con blanco (1 = blanca): menos tinte, mas color propio del recurso.")]
        [SerializeField, Range(0f, 1f)] float blanqueo = 0.6f;
        [Tooltip("Altura sobre el carril a la que se centra la luz, en unidades de mundo.")]
        [SerializeField] float elevacion = 0.45f;
        [Tooltip("Latidos por segundo de la luz: llama la atencion sin molestar.")]
        [SerializeField] float latido = 0.9f;

        RunController run;
        float proximaBusqueda;
        readonly Light2D[] luces = new Light2D[MaxLuces];
        readonly float[] distancias = new float[MaxLuces];
        readonly int[] elegidos = new int[MaxLuces];

        void LateUpdate()
        {
            if ((run == null || run.Sim == null) && Time.unscaledTime >= proximaBusqueda)
            {
                proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
                run = FindAnyObjectByType<RunController>();
                for (int i = 0; i < luces.Length; i++)
                    if (luces[i] != null)
                        luces[i].enabled = false;
            }

            bool activo = run != null && run.Sim != null && run.Level != null && run.Sim.State.Phase == RunPhase.Running;
            if (!activo)
            {
                Apagar(0);
                return;
            }

            var estado = run.Sim.State;
            bool bidonesBloqueados = LevelRuntime.BidonesBloqueados(run.Config, estado);
            int maximo = GameSettings.LightQuality == LightQuality.Baja ? MaxLucesBaja : MaxLuces;

            // Los mas cercanos por delante (o recien pasados), ordenados por cercania.
            int n = 0;
            var items = run.Level.Items;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.Consumed || !LevelRuntime.EsRecurso(item.Entry.kind))
                    continue;
                if (bidonesBloqueados && item.Entry.kind == LevelEntryKind.Fuel)
                    continue;
                float adelante = item.Entry.distance - estado.PlayerX;
                if (adelante < -visibleHasta || adelante > visibleDesde)
                    continue;

                int pos = n < maximo ? n : maximo - 1;
                if (n >= maximo && adelante >= distancias[pos])
                    continue;
                while (pos > 0 && distancias[pos - 1] > adelante)
                {
                    distancias[pos] = distancias[pos - 1];
                    elegidos[pos] = elegidos[pos - 1];
                    pos--;
                }
                distancias[pos] = adelante;
                elegidos[pos] = i;
                if (n < maximo)
                    n++;
            }

            float oscuridad = Mathf.Clamp01(run.Sim.Oscuridad);
            float pulso = 0.85f + 0.15f * Mathf.Sin(Time.time * latido * Mathf.PI * 2f);
            for (int k = 0; k < n; k++)
            {
                var entry = items[elegidos[k]].Entry;
                var luz = Luz(k);
                // Aparece de a poco al acercarse y se apaga al pasarla.
                float aparicion = Mathf.Clamp01((visibleDesde - distancias[k]) / 8f) * Mathf.Clamp01((distancias[k] + visibleHasta) / 1.5f);
                luz.enabled = aparicion > 0.01f;
                luz.color = Color.Lerp(ColorDe(entry.kind), Color.white, blanqueo);
                luz.intensity = Mathf.Lerp(intensidad.x, intensidad.y, oscuridad) * pulso * aparicion;
                luz.pointLightOuterRadius = Mathf.Lerp(radio.x, radio.y, oscuridad);
                float alto = entry.height > 0f ? run.HeightToWorld(entry.height) : 0f;
                luz.transform.position = new Vector3(run.ToWorldX(entry.distance), run.LaneToWorldY(entry.lane) + alto + elevacion, 0f);
            }
            Apagar(n);
        }

        Light2D Luz(int indice)
        {
            if (luces[indice] != null)
                return luces[indice];

            var go = new GameObject("Resplandor " + indice);
            go.transform.SetParent(transform, false);
            var luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            // Multiply, igual que las luces de los prefabs: devuelve a la pieza sus colores reales en la oscuridad,
            // sin sumarle color encima (la aditiva la dejaba toda brillante y lavada).
            luz.blendStyleIndex = 0;
            luz.targetSortingLayers = new[] { LaneSorting.GameLayerId };
            luz.pointLightInnerRadius = 0f;
            luz.falloffIntensity = 0.5f;
            luz.shadowsEnabled = false;
            luces[indice] = luz;
            return luz;
        }

        void Apagar(int desde)
        {
            for (int i = desde; i < luces.Length; i++)
                if (luces[i] != null)
                    luces[i].enabled = false;
        }

        /// <summary>El color de cada recurso: el mismo que lo identifica en el HUD.</summary>
        static Color ColorDe(LevelEntryKind kind)
        {
            switch (kind)
            {
                case LevelEntryKind.Fuel: return new Color(1f, 0.62f, 0.2f);
                case LevelEntryKind.Battery: return new Color(0.35f, 0.8f, 1f);
                case LevelEntryKind.Ammo: return new Color(1f, 0.85f, 0.3f);
                case LevelEntryKind.Granada: return new Color(0.55f, 1f, 0.45f);
                case LevelEntryKind.BombaTemporal: return new Color(1f, 0.3f, 0.25f);
                default: return Color.white;
            }
        }

        void OnDestroy()
        {
            if (instancia == this)
                instancia = null;
        }
    }
}
