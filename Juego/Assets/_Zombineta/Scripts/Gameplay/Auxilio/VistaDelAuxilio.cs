using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;
using Zombineta.Luz;
using Zombineta.Player;

namespace Zombineta.Auxilio
{
    /// <summary>
    /// Lo que se ve de la pelea a pie en el mundo: la protagonista caminando y pegando, y la
    /// furgoneta del auxilio cruzando. Solo lee el estado de la simulacion. Mientras pelea a pie
    /// la moto (con la protagonista arriba) se esconde: el arte todavia no tiene la moto sola.
    /// </summary>
    public sealed class VistaDelAuxilio : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Header("Protagonista a pie")]
        [SerializeField] Sprite peaton;
        [Tooltip("Alto de la protagonista a pie, en unidades de mundo.")]
        [SerializeField] float altoPeaton = 2.3f;
        [SerializeField] Sprite swoosh;
        [Tooltip("Cuanto se adelanta el cuerpo al pegar, en unidades.")]
        [SerializeField] float estocada = 0.35f;
        [SerializeField] Color colorLastimada = new Color(1f, 0.35f, 0.35f);
        [Tooltip("Metros por segundo con que la vista alcanza a la posicion de la simulacion: mas que lo que camina, " +
                 "asi el empujon de un golpe recibido se desliza en vez de teletransportarla.")]
        [SerializeField] float velocidadVisual = 9f;
        [Tooltip("Segundos que dura el salto con que se baja de la moto, y su altura en unidades.")]
        [SerializeField] float segundosBajada = 0.4f;
        [SerializeField] float alturaBajada = 0.55f;
        [Tooltip("Cuanto se echa hacia atras al disparar, en unidades. Sin pose de disparo, es lo que marca el tiro.")]
        [SerializeField] float retrocesoDisparo = 0.22f;

        [Header("Furgoneta")]
        [SerializeField] Sprite furgoneta;
        [Tooltip("Alto de la furgoneta, en unidades de mundo: cruza los tres carriles.")]
        [SerializeField] float altoFurgoneta = 3.6f;
        [SerializeField] Color colorFaros = new Color(1f, 0.92f, 0.7f);

        SpriteRenderer cuerpo, golpe, van;
        Light2D faros;
        Renderer[] moto;
        bool motoEscondida;
        float caminata;
        Vector3 golpeEscalaBase = Vector3.one;
        float xVisual;
        bool xListo;
        float disparoRestante;
        const float DuracionDisparo = 0.12f;

        void OnEnable()
        {
            if (run != null)
                run.Stepped += AlPasar;
        }

        void OnDisable()
        {
            if (run != null)
                run.Stepped -= AlPasar;
        }

        void AlPasar(RunEvent ev)
        {
            if ((ev & RunEvent.Shot) != 0 && run.Sim != null && run.Sim.State.Phase == RunPhase.Pelea)
                disparoRestante = DuracionDisparo;
        }

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();

            cuerpo = Crear("Protagonista a pie", peaton, altoPeaton, out _);
            SombraProyectada.Poner(cuerpo.gameObject, cuerpo, "", 0, true);
            golpe = Crear("Golpe", swoosh, 1.1f, out _);
            golpe.transform.SetParent(cuerpo.transform, true);
            golpeEscalaBase = golpe.transform.localScale;
            van = Crear("Furgoneta del auxilio", furgoneta, altoFurgoneta, out _);

            var faroGo = new GameObject("Faros");
            faroGo.transform.SetParent(van.transform, false);
            faros = faroGo.AddComponent<Light2D>();
            faros.lightType = Light2D.LightType.Point;
            faros.blendStyleIndex = 1;
            faros.color = colorFaros;
            faros.intensity = 0.45f;
            faros.pointLightOuterRadius = 6f;
            faros.pointLightInnerRadius = 0.5f;
            // Solo la calle: si alumbra la capa del juego, la propia furgoneta queda blanca.
            faros.targetSortingLayers = new[] { SortingLayer.NameToID("Calle") };
            faros.shadowsEnabled = false;
            faros.enabled = false;
        }

        SpriteRenderer Crear(string nombre, Sprite sprite, float alto, out GameObject go)
        {
            go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = LaneSorting.GameLayerId;
            sr.enabled = false;
            if (sprite != null && sprite.bounds.size.y > 0f)
                go.transform.localScale = Vector3.one * (alto / sprite.bounds.size.y);
            return sr;
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            var s = run.Sim.State;
            bool aPie = s.Phase == RunPhase.Pelea || (s.Phase == RunPhase.Lost && s.Loss == LossReason.Derribada);
            EsconderMoto(aPie);
            DibujarProtagonista(s, aPie);
            DibujarFurgoneta(s);
        }

        void EsconderMoto(bool esconder)
        {
            if (moto == null)
            {
                var vista = FindAnyObjectByType<ScooterView>();
                if (vista == null)
                    return;
                moto = vista.GetComponentsInChildren<Renderer>(true);
            }
            if (!esconder && !motoEscondida)
                return;
            foreach (var r in moto)
                if (r != null && !(r is ParticleSystemRenderer))
                    r.enabled = !esconder;
            motoEscondida = esconder;
        }

        void DibujarProtagonista(RunState s, bool aPie)
        {
            cuerpo.enabled = aPie;
            golpe.enabled = aPie && s.Golpeando > 0f;
            if (!aPie)
            {
                xListo = false;
                return;
            }

            var c = run.Config;
            float t = c.golpeDuracion > 0f ? s.Golpeando / c.golpeDuracion : 0f;
            // La pina: el cuerpo se adelanta y vuelve.
            float adelanto = Mathf.Sin(t * Mathf.PI) * estocada * s.Mira;

            // El tiro la echa un poco hacia atras: la direccion en que dispara se nota en el cuerpo.
            disparoRestante = Mathf.Max(0f, disparoRestante - Time.deltaTime);
            adelanto -= (disparoRestante / DuracionDisparo) * retrocesoDisparo * s.Mira;

            bool camina = run.Sim.State.Phase == RunPhase.Pelea && Mathf.Abs(VelocidadAPie()) > 0.1f;
            caminata = camina ? caminata + Time.deltaTime * 11f : 0f;
            float paso = camina ? Mathf.Abs(Mathf.Sin(caminata)) * 0.08f : 0f;

            // Al empezar la pelea (o al reintentarla) se baja de un salto, en vez de aparecer de golpe.
            float segundosPelea = run.Config.peleaSegundos - s.PeleaRestante;
            float bajada = s.Phase == RunPhase.Pelea && !s.FurgonetaEnCamino && segundosBajada > 0f &&
                           segundosPelea >= 0f && segundosPelea < segundosBajada
                ? Mathf.Sin(segundosPelea / segundosBajada * Mathf.PI) * alturaBajada
                : 0f;

            float xMundo = run.ToWorldX(s.PeatonX);
            xVisual = xListo ? Mathf.MoveTowards(xVisual, xMundo, velocidadVisual * Time.deltaTime) : xMundo;
            xListo = true;
            float x = xVisual + adelanto;
            float y = run.LaneToWorldY(s.LaneVisual) + paso + bajada;
            cuerpo.transform.position = new Vector3(x, y - cuerpo.sprite.bounds.min.y * cuerpo.transform.localScale.y, 0f);
            cuerpo.flipX = s.Mira < 0;
            cuerpo.sortingOrder = LaneSorting.Order(s.LaneVisual, SortSlot.Player);

            // Recien golpeada: titila y se pone roja.
            bool titila = s.Invulnerable > 0f && s.Invulnerable < 100f && Mathf.Repeat(Time.time * 14f, 1f) < 0.5f;
            cuerpo.color = s.Invulnerable > 0f && s.Invulnerable < 100f ? (titila ? colorLastimada : Color.white) : Color.white;
            if (s.Phase == RunPhase.Lost)
                cuerpo.transform.rotation = Quaternion.Euler(0f, 0f, s.Mira * 80f); // derribada
            else
                cuerpo.transform.rotation = Quaternion.identity;

            if (golpe.enabled)
            {
                golpe.flipX = s.Mira < 0;
                golpe.sortingOrder = cuerpo.sortingOrder + 1;
                // El tercer golpe de la cadena (el remate) es un barrido mas grande y anaranjado.
                bool remate = s.ComboPaso == 3;
                golpe.transform.localScale = golpeEscalaBase * (remate ? 1.9f : 1f);
                golpe.transform.position = new Vector3(x + s.Mira * (remate ? 1.7f : 1.1f), y + altoPeaton * 0.62f, 0f);
                var col = remate ? new Color(1f, 0.6f, 0.25f) : Color.white;
                col.a = t;
                golpe.color = col;
            }
        }

        float ultimaX, ultimaY;

        /// <summary>Metros por segundo que camina, sumando lo que avanza de lado y lo que sube o baja de carril.</summary>
        float VelocidadAPie()
        {
            var s = run.Sim.State;
            float dt = Time.deltaTime;
            float vx = dt > 0f ? (s.PeatonX - ultimaX) / dt : 0f;
            float vy = dt > 0f ? (s.PeatonY - ultimaY) / dt * run.Config.laneSpacing : 0f;
            ultimaX = s.PeatonX;
            ultimaY = s.PeatonY;
            return Mathf.Sqrt(vx * vx + vy * vy);
        }

        void DibujarFurgoneta(RunState s)
        {
            bool cruza = s.Phase == RunPhase.Pelea && s.FurgonetaEnCamino;
            van.enabled = cruza;
            faros.enabled = cruza;
            if (!cruza)
                return;

            float x = run.ToWorldX(s.FurgonetaX);
            float y = run.LaneToWorldY(0f) - 0.3f;
            van.transform.position = new Vector3(x - van.sprite.bounds.center.x * van.transform.localScale.x,
                y - van.sprite.bounds.min.y * van.transform.localScale.y, 0f);
            // Delante de todo lo de los carriles: pasa por encima de los zombies.
            van.sortingOrder = LaneSorting.Order(-1f, SortSlot.Effect);
            faros.transform.position = new Vector3(x + altoFurgoneta * 0.9f, y + altoFurgoneta * 0.35f, 0f);
        }
    }
}
