using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Auxilio
{
    /// <summary>
    /// Lo que se ve de las armas nuevas en el mundo: la granada que sale volando hacia atras desde la
    /// moto y la bomba temporizadora que se deja a pie, con su mecha que late cada vez mas rapido. Solo
    /// lee el estado de la simulacion. Se crea sola al dar Play (no hay que ponerla en ninguna escena);
    /// los dibujos salen de Resources/Arsenal.
    /// </summary>
    public sealed class VistaDeArsenal : MonoBehaviour
    {
        const float SegundosBusqueda = 0.5f;

        static VistaDeArsenal instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarStatic() => instancia = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null)
                return;
            var go = new GameObject("VistaDeArsenal");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<VistaDeArsenal>();
        }

        [Tooltip("Alto de la granada en vuelo, en unidades de mundo.")]
        [SerializeField] float altoGranada = 0.55f;
        [Tooltip("Alto de la bomba en el piso, en unidades de mundo.")]
        [SerializeField] float altoBomba = 0.8f;
        [Tooltip("Altura del tiro oblicuo de la granada (a 10 m de distancia; mas cerca, proporcionalmente menos), en unidades de mundo.")]
        [SerializeField] float arcoGranada = 2.4f;
        [Tooltip("Grados que gira la granada en todo el vuelo: un giro lento, sin rulos.")]
        [SerializeField] float giroGranada = 270f;

        RunController run;
        SpriteRenderer granada, bomba;
        float proximaBusqueda;

        void Awake()
        {
            granada = NuevoSprite("Granada en vuelo", Resources.Load<Sprite>("Arsenal/Granada"));
            bomba = NuevoSprite("Bomba temporizadora", Resources.Load<Sprite>("Arsenal/BombaTemporal"));
        }

        SpriteRenderer NuevoSprite(string nombre, Sprite sprite)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = LaneSorting.GameLayerId;
            sr.enabled = false;
            return sr;
        }

        void LateUpdate()
        {
            if (run == null && Time.unscaledTime >= proximaBusqueda)
            {
                proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
                run = FindAnyObjectByType<RunController>();
            }
            if (run == null || run.Sim == null)
            {
                granada.enabled = bomba.enabled = false;
                return;
            }

            var s = run.Sim.State;
            DibujarGranada(s);
            DibujarBomba(s);
        }

        void DibujarGranada(RunState s)
        {
            bool aPie = s.Phase == RunPhase.Pelea;
            bool vuela = (s.Phase == RunPhase.Running || aPie) && s.GranadaVuelo > 0f && granada.sprite != null;
            granada.enabled = vuela;
            if (!vuela)
                return;

            var cfg = run.Config;
            float p = Mathf.Clamp01(1f - s.GranadaVuelo / Mathf.Max(0.01f, cfg.granadaVuelo));
            // Un tiro oblicuo: avanza parejo hacia donde cae (granadaDistancia detras de la moto, o
            // granadaDistanciaAPie por delante de ella a pie) y sube y baja en una parabola, de la mano al piso.
            float desde = run.ToWorldX(aPie ? s.PeatonX : s.PlayerX);
            float hasta = run.ToWorldX(s.GranadaX);
            float x = Mathf.Lerp(desde, hasta, p);
            float altoMoto = !aPie && s.Airborne ? run.HeightToWorld(s.Height) : 0f;
            float altoMano = aPie ? 1.2f : 0.7f;   // de la mano a pie; de la moto, un poco mas abajo
            // Un tiro corto sube menos que uno largo.
            float arco = arcoGranada * Mathf.Clamp(Mathf.Abs(hasta - desde) / 10f, 0.55f, 1f);
            float y = run.LaneToWorldY(s.LaneVisual) + altoMoto + altoMano * (1f - p) + 4f * arco * p * (1f - p);
            Poner(granada, x, y, altoGranada, centrada: true);
            // Gira lento sobre su centro, para el lado del tiro (el pivote esta abajo): se compensa moviendo el dibujo.
            granada.transform.rotation = Quaternion.Euler(0f, 0f, -Mathf.Sign(s.GranadaDir) * p * giroGranada);
            granada.transform.position += granada.transform.rotation * new Vector3(0f, altoGranada * 0.5f, 0f) - new Vector3(0f, altoGranada * 0.5f, 0f);
            granada.sortingOrder = LaneSorting.Order(s.LaneVisual, SortSlot.Effect);
        }

        void DibujarBomba(RunState s)
        {
            bool puesta = s.Phase == RunPhase.Pelea && s.BombaMecha > 0f && bomba.sprite != null;
            bomba.enabled = puesta;
            if (!puesta)
                return;

            var cfg = run.Config;
            float restante = Mathf.Clamp01(s.BombaMecha / Mathf.Max(0.1f, cfg.bombaMecha));
            // La mecha late cada vez mas rapido y la bomba se pone roja.
            float frecuencia = 3f + (1f - restante) * 14f;
            float latido = 0.5f + 0.5f * Mathf.Sin(Time.time * frecuencia * Mathf.PI);
            float x = run.ToWorldX(s.BombaX);
            float y = run.LaneToWorldY(s.BombaY);
            Poner(bomba, x, y, altoBomba * (1f + 0.12f * latido));
            bomba.transform.rotation = Quaternion.identity;
            bomba.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.3f), latido * (1f - restante));
            bomba.sortingOrder = LaneSorting.Order(s.BombaY, SortSlot.Item);
        }

        /// <summary>El pivote de los dibujos esta abajo al centro: la bomba se apoya en el piso; la granada, centrada en el punto.</summary>
        static void Poner(SpriteRenderer sr, float x, float y, float alto, bool centrada = false)
        {
            float escala = alto / Mathf.Max(0.01f, sr.sprite.bounds.size.y);
            sr.transform.localScale = Vector3.one * escala;
            sr.transform.position = new Vector3(x, centrada ? y - alto * 0.5f : y, 0f);
        }

        void OnDestroy()
        {
            if (instancia == this)
                instancia = null;
        }
    }
}
