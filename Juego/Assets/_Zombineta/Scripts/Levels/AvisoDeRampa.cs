using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;

namespace Zombineta.Juego.Levels
{
    /// <summary>
    /// El aviso de la rampa: unas flechas pintadas en el piso, en el carril de la rampa, un poco antes de llegar,
    /// que se encienden una tras otra hacia ella. Anticipan el salto (y el impulso de velocidad que da). Solo lee el
    /// recorrido y la posicion de la moto. Se crea sola al dar Play (no hay que ponerla en ninguna escena); el dibujo
    /// (provisorio) sale de Resources/Rampa/AvisoRampa.
    /// </summary>
    public sealed class AvisoDeRampa : MonoBehaviour
    {
        const float SegundosBusqueda = 0.5f;

        static AvisoDeRampa instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarStatic() => instancia = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null)
                return;
            var go = new GameObject("AvisoDeRampa");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<AvisoDeRampa>();
        }

        [Tooltip("Metros antes de la rampa a los que se pintan las flechas, de la mas lejana a la mas cercana.")]
        [SerializeField] float[] metrosAntes = { 15f, 8.5f };
        [Tooltip("Desde que distancia (en metros) se empiezan a ver, adelante de la moto.")]
        [SerializeField] float visibleDesde = 60f;
        [Tooltip("Ancho de cada flecha en unidades de mundo.")]
        [SerializeField] float ancho = 2.6f;
        [Tooltip("Cuanto sube del piso, en unidades de mundo: el dibujo se apoya en el carril.")]
        [SerializeField] float elevacion = 0.12f;
        [Tooltip("Vueltas por segundo del encendido de las flechas hacia la rampa.")]
        [SerializeField] float velocidad = 2.4f;

        RunController run;
        Sprite sprite;
        float proximaBusqueda;
        readonly Dictionary<int, SpriteRenderer[]> pool = new Dictionary<int, SpriteRenderer[]>();

        void Awake() => sprite = Resources.Load<Sprite>("Rampa/AvisoRampa");

        void LateUpdate()
        {
            if (run == null && Time.unscaledTime >= proximaBusqueda)
            {
                proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
                run = FindAnyObjectByType<RunController>();
                foreach (var par in pool)
                    foreach (var sr in par.Value)
                        if (sr != null)
                            Destroy(sr.gameObject);
                pool.Clear();
            }

            bool activo = run != null && run.Sim != null && run.Level != null && sprite != null &&
                          run.Sim.State.Phase == RunPhase.Running;
            if (!activo)
            {
                OcultarTodo();
                return;
            }

            float moto = run.Sim.State.PlayerX;
            var items = run.Level.Items;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.Entry.kind != LevelEntryKind.Ramp)
                    continue;

                float adelante = item.Entry.distance - moto;
                bool ver = !item.Consumed && adelante > 0f && adelante < visibleDesde;
                if (!ver)
                {
                    Ocultar(i);
                    continue;
                }

                var flechas = Flechas(i);
                // Aparecen de a poco al acercarse, y todas titilan en cadena hacia la rampa.
                float aparicion = Mathf.Clamp01((visibleDesde - adelante) / 12f);
                for (int k = 0; k < flechas.Length; k++)
                {
                    float metros = item.Entry.distance - metrosAntes[k];
                    // La flecha que ya se paso deja de verse.
                    float pasada = Mathf.Clamp01((metros - moto + 1f) / 1f);
                    var sr = flechas[k];
                    sr.enabled = pasada > 0f;
                    float onda = Mathf.Max(0f, Mathf.Sin((Time.time * velocidad - k * 0.45f) * Mathf.PI * 2f));
                    var c = Color.white;
                    c.a = aparicion * pasada * (0.45f + 0.55f * onda);
                    sr.color = c;
                    sr.transform.position = new Vector3(run.ToWorldX(metros), run.LaneToWorldY(item.Entry.lane) + elevacion, 0f);
                    sr.sortingOrder = LaneSorting.Order(item.Entry.lane, SortSlot.Shadow) + 1;
                }
            }
        }

        SpriteRenderer[] Flechas(int indice)
        {
            if (pool.TryGetValue(indice, out var existentes))
                return existentes;

            var flechas = new SpriteRenderer[metrosAntes.Length];
            for (int k = 0; k < flechas.Length; k++)
            {
                var go = new GameObject("Aviso rampa " + indice + "." + k);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerID = LaneSorting.GameLayerId;
                // El dibujo trae tres flechas juntas (">>>"); se achata un poco para que se lea pintado en el piso.
                float escala = ancho / Mathf.Max(0.01f, sprite.bounds.size.x);
                go.transform.localScale = new Vector3(escala, escala * 0.75f, 1f);
                flechas[k] = sr;
            }
            pool[indice] = flechas;
            return flechas;
        }

        void Ocultar(int indice)
        {
            if (!pool.TryGetValue(indice, out var flechas))
                return;
            foreach (var sr in flechas)
                if (sr != null)
                    sr.enabled = false;
        }

        void OcultarTodo()
        {
            foreach (var par in pool)
                foreach (var sr in par.Value)
                    if (sr != null)
                        sr.enabled = false;
        }

        void OnDestroy()
        {
            if (instancia == this)
                instancia = null;
        }
    }
}
