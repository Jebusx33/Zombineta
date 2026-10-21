using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;
using Zombineta.Luz;
using Zombineta.Player;

namespace Zombineta.Auxilio
{
    /// <summary>
    /// Lo que se ve de la pelea a pie en el mundo: la protagonista caminando y pegando, y lo que
    /// queda en el piso (botiquines, balas y el bidon del ultimo zombie). Solo lee el estado de la
    /// simulacion. Mientras pelea a pie la moto (con la protagonista arriba) se esconde: el arte
    /// todavia no tiene la moto sola.
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

        [Header("Lo que sueltan los zombies")]
        [Tooltip("Dibujo del botiquin. Vacio: se usa uno de prueba (cruz verde).")]
        [SerializeField] Sprite botiquin;
        [Tooltip("Dibujo de las balas. Vacio: se usa uno de prueba (tres balas).")]
        [SerializeField] Sprite balas;
        [Tooltip("Dibujo del bidon que suelta el ultimo zombie. Vacio: Resources/Arsenal/Bidon, o uno de prueba (bidon rojo).")]
        [SerializeField] Sprite bidon;
        [Tooltip("El bidon es mas grande que lo demas: es lo que hay que ir a buscar.")]
        [SerializeField] float altoBidon = 1.1f;
        [Tooltip("Alto de lo que queda en el piso, en unidades de mundo.")]
        [SerializeField] float altoDrop = 0.7f;
        [Tooltip("Segundos antes de irse en que empieza a titilar.")]
        [SerializeField] float segundosTitileo = 3f;
        [SerializeField] float rebote = 0.08f;

        SpriteRenderer cuerpo, golpe;
        readonly System.Collections.Generic.List<SpriteRenderer> drops = new System.Collections.Generic.List<SpriteRenderer>();
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

        // El ultimo golpe: si dio en alguien se ve pleno; si fue al aire, mas chico y apagado.
        bool golpeConecto;

        void AlPasar(RunEvent ev)
        {
            if ((ev & RunEvent.Golpe) != 0)
                golpeConecto = true;
            else if ((ev & RunEvent.GolpeAlAire) != 0)
                golpeConecto = false;

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
            DibujarDrops(s);
        }

        /// <summary>Botiquines y balas en el piso: titilan cuando estan por irse.</summary>
        void DibujarDrops(RunState s)
        {
            var lista = run.Sim.Pelea.Drops;
            int n = s.Phase == RunPhase.Pelea ? lista.Count : 0;

            for (int i = 0; i < n; i++)
            {
                while (drops.Count <= i)
                    drops.Add(NuevoDrop(drops.Count));
                var d = lista[i];
                var sr = drops[i];
                bool esBidon = d.Kind == DropKind.Bidon;
                sr.sprite = esBidon ? SpriteBidon() : d.Kind == DropKind.Botiquin ? SpriteBotiquin() : SpriteBalas();
                sr.enabled = Mathf.Repeat(Time.time * 10f, 1f) < 0.5f || d.Left > segundosTitileo;
                float alto = esBidon ? altoBidon * (1f + 0.08f * Mathf.Sin(Time.time * 6f)) : altoDrop;
                float escala = alto / Mathf.Max(0.01f, sr.sprite.bounds.size.y);
                sr.transform.localScale = Vector3.one * escala;
                float salto = Mathf.Abs(Mathf.Sin(Time.time * 4f + i)) * rebote * (esBidon ? 2.5f : 1f);
                float y = run.LaneToWorldY(d.Lane) + salto - sr.sprite.bounds.min.y * escala;
                sr.transform.position = new Vector3(run.ToWorldX(d.X), y, 0f);
                sr.sortingOrder = LaneSorting.Order(d.Lane, SortSlot.Item);
            }
            for (int i = n; i < drops.Count; i++)
                drops[i].enabled = false;
        }

        SpriteRenderer NuevoDrop(int indice)
        {
            var go = new GameObject("Drop " + indice);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = LaneSorting.GameLayerId;
            sr.enabled = false;
            return sr;
        }

        Sprite SpriteBidon()
        {
            if (bidon == null)
                bidon = Resources.Load<Sprite>("Arsenal/Bidon");
            if (bidon == null)
                bidon = Dibujar(24, (x, y) =>
                {
                    // Un bidon rojo con tapa amarilla y un asa.
                    bool cuerpo = x >= 4 && x < 20 && y >= 2 && y < 17;
                    bool asa = x >= 14 && x < 19 && y >= 17 && y < 21 && (y >= 19 || x >= 17 || x < 16);
                    bool tapa = x >= 6 && x < 11 && y >= 17 && y < 21;
                    bool borde = cuerpo && (x == 4 || x == 19 || y == 2 || y == 16);
                    if (tapa) return new Color(1f, 0.85f, 0.2f, 1f);
                    if (asa) return new Color(0.3f, 0.3f, 0.3f, 1f);
                    if (borde) return new Color(0.4f, 0.05f, 0.05f, 1f);
                    return cuerpo ? new Color(0.85f, 0.15f, 0.12f, 1f) : Color.clear;
                });
            return bidon;
        }

        Sprite SpriteBotiquin()
        {
            if (botiquin == null)
                botiquin = Dibujar(24, (x, y) =>
                {
                    bool borde = x < 2 || y < 2 || x >= 22 || y >= 22;
                    bool cruz = (x >= 9 && x < 15 && y >= 4 && y < 20) || (y >= 9 && y < 15 && x >= 4 && x < 20);
                    if (cruz) return new Color(0.2f, 0.85f, 0.35f, 1f);
                    return borde ? new Color(0.15f, 0.15f, 0.15f, 1f) : new Color(0.96f, 0.96f, 0.96f, 1f);
                });
            return botiquin;
        }

        Sprite SpriteBalas()
        {
            if (balas == null)
                balas = Dibujar(24, (x, y) =>
                {
                    // Tres balas paradas: cuerpo dorado y punta mas clara.
                    for (int k = 0; k < 3; k++)
                    {
                        int x0 = 3 + k * 7;
                        if (x < x0 || x >= x0 + 5 || y < 3 || y >= 21)
                            continue;
                        return y >= 15 ? new Color(0.85f, 0.55f, 0.3f, 1f) : new Color(1f, 0.82f, 0.25f, 1f);
                    }
                    return Color.clear;
                });
            return balas;
        }

        /// <summary>Un sprite de prueba dibujado pixel a pixel (hasta que haya arte).</summary>
        static Sprite Dibujar(int lado, System.Func<int, int, Color> pixel)
        {
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                    tex.SetPixel(x, y, pixel(x, y));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0f), lado);
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
            float segundosPelea = s.PeleaTiempo;
            float bajada = s.Phase == RunPhase.Pelea && segundosBajada > 0f &&
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
                // El barrido termina justo donde termina el golpe (el alcance de la simulacion, medido
                // desde ella): lo que se ve tocado es lo que se golpea, ni mas ni menos.
                bool remate = s.ComboPaso == 3;
                float escala = (remate ? 1.9f : 1f) * (golpeConecto ? 1f : 0.8f);
                golpe.transform.localScale = golpeEscalaBase * escala;
                float alcance = remate ? c.remateRadio : c.golpeAlcance;
                float mitad = golpe.sprite != null ? golpe.sprite.bounds.extents.x * golpe.transform.lossyScale.x : 0.5f;
                float borde = run.ToWorldX(s.PeatonX) + s.Mira * alcance * run.Config.worldUnitsPerMeter;
                golpe.transform.position = new Vector3(borde - s.Mira * mitad, y + altoPeaton * 0.62f, 0f);
                var col = remate ? new Color(1f, 0.6f, 0.25f) : Color.white;
                col.a = golpeConecto ? t : t * 0.45f;
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
    }
}
