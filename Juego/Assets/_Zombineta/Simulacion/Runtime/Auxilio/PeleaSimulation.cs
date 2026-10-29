using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;
using Zombineta.Player;

namespace Zombineta.Auxilio
{
    /// <summary>
    /// La pelea a pie cuando se acaba la nafta. C# plano, como el runner: se le hace Step con
    /// un dt fijo y se verifica. Mueve a los mismos zombies de la horda (asi la vista de la
    /// horda los dibuja tal cual) y a la protagonista a pie. La pelea es por oleadas, sin reloj: se
    /// sigue hasta vencer a todos, el ultimo suelta un bidon y al recogerlo vuelve a la moto.
    ///
    /// La arena es lo que se ve en pantalla alrededor de donde quedo parada la moto: de un
    /// lado llega la horda, del otro algunos que vienen de frente. Solo pelean unos pocos a la
    /// vez; el resto de la horda queda estacionada fuera de cuadro.
    /// </summary>
    public enum DropKind { Botiquin, Balas, Bidon }

    /// <summary>Algo que solto un zombie al morir: se recoge caminando encima y se va solo si nadie lo agarra.</summary>
    public sealed class Drop
    {
        public DropKind Kind;
        public float X;
        public int Lane;
        public float Left;
        public float Total;
        /// <summary>Ya aviso que no lo necesita: no repite hasta que ella se aparte y vuelva.</summary>
        public bool Avisado;
    }

    public sealed class PeleaSimulation
    {
        // Distancia a la que un zombie de otro carril se decide a cambiarse al de ella.
        const float DistanciaParaCambiarCarril = 6f;
        // Donde esperan, fuera de cuadro, los zombies que no estan peleando.
        const float Estacionamiento = 80f;
        // Metros hacia atras que todavia cuentan como "delante" en los golpes: pegado a ella, aunque este apenas detras.
        const float AtrasTolerado = 0.3f;
        // Metros de vuelo que le quedan como minimo al zombie que salta, aunque se deslice agachado.
        const float MinimoDeVuelo = 2f;

        /// <summary>Cuanto se multiplica la velocidad base a pie: la de la pelea de su tipo, o la de la horda si no la tiene.</summary>
        static float MultiplicadorAPie(Zombineta.Enemies.ZombieType t) =>
            t.speedMultiplierAPie > 0f ? t.speedMultiplierAPie : t.speedMultiplier;

        readonly GameConfig c;
        readonly RunState s;
        readonly HordeSimulation h;
        readonly System.Random rng;
        // Aparte del azar de la pelea: soltar o no un drop no corre las demas tiradas (y las pruebas siguen siendo las mismas).
        readonly System.Random dropRng;
        readonly System.Collections.Generic.List<Drop> drops = new System.Collections.Generic.List<Drop>();

        const int MaxDrops = 4;

        /// <summary>Lo que hay en el piso ahora: la vista lo dibuja.</summary>
        public System.Collections.Generic.IReadOnlyList<Drop> Drops => drops;

        readonly System.Collections.Generic.List<int> fila = new System.Collections.Generic.List<int>();
        bool[] activo;
        float centro;
        int lado = 1;
        // Zombies de la oleada en curso que todavia no entraron a la arena.
        int pendientes;

        /// <summary>Los barriles del recorrido: a pie explotan los que quedaron en la arena. Lo setea RunSimulation.</summary>
        public IBarrelField Barrels { get; set; }

        /// <summary>La partida a la que pertenece: la explosion de un barril la resuelve ella.</summary>
        public RunSimulation Run { get; set; }

        public PeleaSimulation(GameConfig config, RunState state, HordeSimulation horde, int seed)
        {
            c = config;
            s = state;
            h = horde;
            rng = new System.Random(seed);
            dropRng = new System.Random(seed + 7919);
        }

        /// <summary>Metros: el centro de la arena (donde quedo la moto).</summary>
        public float Centro => centro;

        /// <summary>Si el zombie de ese indice esta peleando (los demas esperan fuera de cuadro).</summary>
        public bool Activo(int indice) => activo != null && indice < activo.Length && activo[indice];

        /// <summary>Si quedarse sin nafta empieza una pelea (siempre, salvo en el tutorial y las escenas sin pelea).</summary>
        public bool PuedeEmpezar => c.peleaSinNafta;

        /// <summary>La protagonista se baja a pelear: la horda la alcanza y la rodea.</summary>
        public RunEvent Empezar()
        {
            s.Peleas++;
            s.FrenadoRestante = -1f;
            s.RampBoostRemaining = 0f;
            s.Phase = RunPhase.Pelea;
            s.Vida = c.vidaPelea + (s.Peleas == 1 ? c.vidaExtraPrimeraPelea : 0);
            s.Oleada = 0;
            s.ReintentosPelea = c.reintentosPelea;
            s.VidaMax = s.Vida;
            s.PeleaBajas = 0;
            s.PeleaGolpesRecibidos = 0;
            s.PeleaRemates = 0;
            s.PeleaBotiquines = 0;
            s.PeleaSaltosAcertados = 0;
            s.PeleaSaltosEsquivados = 0;
            s.PeleaSaltosCortados = 0;
            s.PremioBalas = 0;
            s.PremioBateria = 0f;
            s.PremioBalasLleno = false;
            s.PremioBateriaLleno = false;
            drops.Clear();
            s.PeleaTiempo = 0f;
            s.PeleaBombas = 0;
            s.BombaMecha = 0f;
            // En la primera pelea se baja con al menos una granada y una bomba: ahi se aprende a usarlas.
            if (s.Peleas == 1)
            {
                s.Granadas = Mathf.Clamp(Mathf.Max(s.Granadas, c.granadasPrimeraPelea), 0, c.granadaMax);
                s.Bombas = Mathf.Clamp(Mathf.Max(s.Bombas, c.bombasPrimeraPelea), 0, c.bombaMax);
            }
            s.GranadaVuelo = 0f;
            s.BidonEnElPiso = false;
            s.OleadaRespiro = 0f;
            s.Invulnerable = 0f;
            s.GolpeEnfriamiento = 0f;
            s.Golpeando = 0f;
            s.ComboPaso = 0;
            s.ComboVentana = 0f;
            s.HeadlightOn = false;
            s.StunRemaining = 0f;
            s.Mode = DriveMode.Normal;
            s.Mira = 1;
            // Se baja en el carril (aunque sea a medio cambio) y de ahi camina libre.
            s.PeatonY = Mathf.Clamp(s.LaneVisual, 0f, RunSimulation.LaneCount - 1);
            s.Lane = Mathf.Clamp(Mathf.RoundToInt(s.PeatonY), 0, RunSimulation.LaneCount - 1);
            s.LaneVisual = s.PeatonY;

            centro = s.PlayerX;
            s.PeatonX = centro;

            ArmarArena(desdeLaHorda: true);
            return RunEvent.PeleaEmpieza;
        }

        /// <summary>
        /// Los zombies de la primera oleada (o "zombiesEnArena" si no hay oleadas) entran a la
        /// arena y el resto queda estacionado fuera de cuadro. Sirve para empezar y para reintentar.
        /// </summary>
        void ArmarArena(bool desdeLaHorda)
        {
            var units = h.Units;
            activo = new bool[units.Length];
            IniciarOleada(0);
            int enArena = Mathf.Min(Simultaneos(0), units.Length);
            pendientes = Mathf.Max(0, s.OleadaTotal - enArena);
            s.ZombiesRestantes = s.OleadaTotal;

            if (c.saltoActivo)
            {
                ArmarArenaCorriendo(enArena, desdeLaHorda);
                return;
            }

            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (i < enArena)
                {
                    activo[i] = true;
                    // La mayoria llega con la horda, por atras; uno de cada tres viene de frente.
                    int desde = i % 3 == 2 ? 1 : -1;
                    float x = desde < 0
                        ? centro - 5f - (float)rng.NextDouble() * 9f
                        : centro + c.arenaMedioAncho + 1f + (float)rng.NextDouble() * 4f;
                    Preparar(i, x, rng.Next(RunSimulation.LaneCount), nuevo: !u.Alive);
                }
                else
                {
                    u.X = centro - Estacionamiento;
                    u.Stagger = 0f;
                    u.Derribado = 0f;
                    u.Preparando = -1f;
                    u.PuedeSaltar = false;
                    u.SaltoFase = 0;
                }
            }
            h.RecomputeFront();
            s.HordeX = h.FrontX;
        }

        /// <summary>
        /// La primera oleada llega CORRIENDO por atras y salta sobre ella: sigue la carrera de la horda
        /// de la moto, asi al bajarse los zombies no pasan de correr a caminar. Al bajarse de la moto, los
        /// de la horda que estaban mas cerca (los que se ven) siguen donde estaban, con su carril y su
        /// pinta, acotados a [saltoEntradaMin, saltoEntradaMax] metros detras de ella. En un reintento,
        /// o si no alcanzan, entran por atras fuera de pantalla.
        /// </summary>
        void ArmarArenaCorriendo(int enArena, bool desdeLaHorda)
        {
            var units = h.Units;
            var elegidos = new System.Collections.Generic.List<int>();
            if (desdeLaHorda)
            {
                for (int i = 0; i < units.Length; i++)
                    if (units[i].Alive)
                        elegidos.Add(i);
                elegidos.Sort((a, b) =>
                {
                    int cmp = Mathf.Abs(centro - units[a].X).CompareTo(Mathf.Abs(centro - units[b].X));
                    return cmp != 0 ? cmp : a.CompareTo(b);
                });
                if (elegidos.Count > enArena)
                    elegidos.RemoveRange(enArena, elegidos.Count - enArena);
            }

            float minimo = Mathf.Min(c.saltoEntradaMin, c.saltoEntradaMax);
            float maximo = Mathf.Max(c.saltoEntradaMin, c.saltoEntradaMax);

            // Los que ya venian en la horda: donde estaban, acotados a la zona de entrada.
            foreach (int i in elegidos)
            {
                var u = units[i];
                float detras = Mathf.Clamp(centro - u.X, minimo, maximo);
                activo[i] = true;
                Preparar(i, centro - detras, u.Lane, nuevo: false, conservarTipo: true);
            }

            // Lo que falte para llegar a la cantidad: entran por atras, fuera de pantalla.
            int puestos = elegidos.Count;
            for (int i = 0; i < units.Length && puestos < enArena; i++)
            {
                if (activo[i])
                    continue;
                activo[i] = true;
                puestos++;
                float detras = minimo + (float)rng.NextDouble() * (maximo - minimo);
                Preparar(i, centro - detras, rng.Next(RunSimulation.LaneCount), nuevo: !units[i].Alive);
            }

            for (int i = 0; i < units.Length; i++)
            {
                if (activo[i])
                    continue;
                var u = units[i];
                u.X = centro - Estacionamiento;
                u.Stagger = 0f;
                u.Derribado = 0f;
                u.Preparando = -1f;
                u.PuedeSaltar = false;
                u.SaltoFase = 0;
            }
            h.RecomputeFront();
            s.HordeX = h.FrontX;
        }

        // --- Oleadas -----------------------------------------------------------

        bool HayOleadas => c.oleadas != null && c.oleadas.Count > 0;

        /// <summary>Que tan rapido atacan los zombies en la oleada en curso (1 = los tiempos del GameConfig).</summary>
        float RitmoDeAtaque => OleadaActual != null ? Mathf.Max(0.1f, OleadaActual.ataque) : 1f;

        /// <summary>Segundos que tarda un zombie en preparar el golpe en la oleada en curso (la vista reparte sus cuadros en ese lapso).</summary>
        public float PreparacionActual => c.zombiePreparacion / RitmoDeAtaque;

        OleadaPelea OleadaActual => HayOleadas ? c.oleadas[Mathf.Clamp(s.Oleada, 0, c.oleadas.Count - 1)] : null;

        int TotalOleadas => HayOleadas ? c.oleadas.Count : 1;

        /// <summary>Zombies que trae esa oleada en total. Sin oleadas configuradas, una sola con 'zombiesEnArena'.</summary>
        int ZombiesDe(int oleada) =>
            HayOleadas ? c.oleadas[Mathf.Clamp(oleada, 0, c.oleadas.Count - 1)].zombies : c.zombiesEnArena;

        /// <summary>Cuantos pelean a la vez en esa oleada (nunca mas de los que trae).</summary>
        int Simultaneos(int oleada)
        {
            int n = HayOleadas ? c.oleadas[Mathf.Clamp(oleada, 0, c.oleadas.Count - 1)].simultaneos : 0;
            if (n <= 0)
                n = c.maxSimultaneos;
            return Mathf.Max(1, Mathf.Min(n, ZombiesDe(oleada)));
        }

        /// <summary>Empieza esa oleada: todos sus zombies quedan pendientes (los que entran los pone el que llama).</summary>
        void IniciarOleada(int oleada)
        {
            s.Oleada = oleada;
            s.Oleadas = TotalOleadas;
            s.OleadaTotal = ZombiesDe(oleada);
            pendientes = s.OleadaTotal;
            s.ZombiesRestantes = s.OleadaTotal;
            s.OleadaRespiro = 0f;
        }

        int VivosEnArena()
        {
            int n = 0;
            var units = h.Units;
            for (int i = 0; i < units.Length; i++)
                if (activo[i] && units[i].Alive)
                    n++;
            return n;
        }

        /// <summary>Una unidad de la horda que no esta peleando (esperando fuera de cuadro), o -1.</summary>
        int UnidadLibre()
        {
            for (int i = 0; i < activo.Length; i++)
                if (!activo[i])
                    return i;
            return -1;
        }

        /// <summary>
        /// Las oleadas no tienen reloj ni reponen a los caidos: se vencen todos los zombies de la
        /// oleada (entran de a poco, hasta la cantidad que pelea a la vez) y recien entonces, tras un
        /// respiro, entra la que sigue. Al vencer la ultima, el ultimo zombie suelta un bidon.
        /// </summary>
        RunEvent AtenderOleadas(float dt)
        {
            if (s.BidonEnElPiso)
                return RunEvent.None;

            var ev = RunEvent.None;

            // Entre una oleada y otra hay un respiro; despues entra la que sigue.
            if (s.OleadaRespiro > 0f)
            {
                s.OleadaRespiro -= dt;
                if (s.OleadaRespiro > 0f)
                    return ev;
                IniciarOleada(s.Oleada + 1);
                ev |= RunEvent.OleadaNueva;
            }

            // Entran de a uno, por los costados, mientras haya lugar y falten.
            int cupo = Simultaneos(s.Oleada) - VivosEnArena();
            while (cupo > 0 && pendientes > 0)
            {
                int i = UnidadLibre();
                if (i < 0)
                    break;
                activo[i] = true;
                lado = -lado;
                float x = lado < 0
                    ? centro - c.arenaMedioAncho - 2f - (float)rng.NextDouble() * 3f
                    : centro + c.arenaMedioAncho + 2f + (float)rng.NextDouble() * 3f;
                Preparar(i, x, rng.Next(RunSimulation.LaneCount), nuevo: true);
                pendientes--;
                cupo--;
            }

            s.ZombiesRestantes = pendientes + VivosEnArena();
            if (s.ZombiesRestantes > 0)
                return ev;

            // Vencio la oleada.
            if (s.Oleada + 1 < TotalOleadas)
            {
                s.OleadaRespiro = Mathf.Max(0.01f, c.respiroOleada);
                return ev;
            }
            return ev | VencerPelea();
        }

        /// <summary>Vencio la ultima oleada: el ultimo zombie en caer suelta un bidon que hay que ir a buscar.</summary>
        RunEvent VencerPelea()
        {
            s.BidonEnElPiso = true;

            // El que acaba de caer es el cadaver mas fresco.
            float x = s.PeatonX;
            int carril = s.Lane;
            float masFresco = -1f;
            var units = h.Units;
            for (int i = 0; i < units.Length; i++)
            {
                if (!activo[i] || units[i].Alive || units[i].CorpseLeft <= masFresco)
                    continue;
                masFresco = units[i].CorpseLeft;
                x = units[i].X;
                carril = units[i].Lane;
            }
            x = Mathf.Clamp(x, centro - c.arenaMedioAncho, centro + c.arenaMedioAncho);

            drops.Add(new Drop { Kind = DropKind.Bidon, X = x, Lane = carril, Left = float.PositiveInfinity, Total = 0f });
            return RunEvent.PeleaGanada;
        }

        // --- La granada a pie ----------------------------------------------------

        /// <summary>
        /// A pie, la granada se lanza hacia donde mira y explota a granadaDistanciaAPie metros de ella: se lleva
        /// a los zombies que estaban cerca de ahi (la bomba, en cambio, se los lleva a todos). Es la misma
        /// granada que se recoge para la moto. No la lanza si no tiene, ya hay una en el aire o no hay a quien llevarse.
        /// </summary>
        RunEvent LanzarGranada(PlayerIntent intent)
        {
            if (!intent.Granada)
                return RunEvent.None;
            if (s.Granadas <= 0 || s.GranadaVuelo > 0f || s.BidonEnElPiso || VivosEnArena() == 0)
                return RunEvent.GranadaDenegada;

            s.Granadas--;
            s.GranadaVuelo = Mathf.Max(0.01f, c.granadaVuelo);
            s.GranadaDir = s.Mira;
            s.GranadaX = Mathf.Clamp(s.PeatonX + s.Mira * c.granadaDistanciaAPie,
                centro - c.arenaMedioAncho - 2f, centro + c.arenaMedioAncho + 2f);
            return RunEvent.GranadaLanzada;
        }

        /// <summary>Al caer explota: cae todo zombie que pelea a granadaRadioAPie metros del punto (en todos los carriles).</summary>
        RunEvent AtenderGranada(float dt)
        {
            if (s.GranadaVuelo <= 0f)
                return RunEvent.None;

            s.GranadaVuelo -= dt;
            if (s.GranadaVuelo > 0f)
                return RunEvent.None;
            s.GranadaVuelo = 0f;

            int muertos = 0;
            var units = h.Units;
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (!activo[i] || !u.Alive || Mathf.Abs(u.X - s.GranadaX) > c.granadaRadioAPie)
                    continue;
                h.Kill(u, DeathCause.Explosion);
                u.SaltoFase = 0;
                u.Preparando = -1f;
                muertos++;
            }
            s.PeleaBajas += muertos;
            s.UltimaExplosionMuertos = muertos;
            h.EventoExplosion(s.GranadaX, s.Lane);
            return RunEvent.GranadaExplota | RunEvent.Explosion;
        }

        // --- La bomba temporizadora ----------------------------------------------

        /// <summary>
        /// A pie, pone una bomba temporizadora donde esta parada: explota a los bombaMecha segundos. No la
        /// pone si no tiene, ya hay una puesta, ya vencio todo o no hay a quien llevarse.
        /// </summary>
        RunEvent PonerBomba(PlayerIntent intent)
        {
            if (!intent.Bomba)
                return RunEvent.None;
            if (s.Bombas <= 0 || s.BombaMecha > 0f || s.BidonEnElPiso || VivosEnArena() == 0)
                return RunEvent.BombaDenegada;

            s.Bombas--;
            s.PeleaBombas++;
            s.BombaMecha = Mathf.Max(0.05f, c.bombaMecha);
            s.BombaX = s.PeatonX;
            s.BombaY = s.PeatonY;
            return RunEvent.BombaPuesta;
        }

        /// <summary>
        /// Cuenta la mecha. Al llegar a cero explota y cae todo zombie que este peleando (tambien los
        /// que saltan). No frena la oleada: los que todavia no entraron siguen entrando.
        /// </summary>
        RunEvent AtenderBomba(float dt)
        {
            if (s.BombaMecha <= 0f)
                return RunEvent.None;

            s.BombaMecha -= dt;
            if (s.BombaMecha > 0f)
                return RunEvent.None;
            s.BombaMecha = 0f;

            int muertos = 0;
            var units = h.Units;
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (!activo[i] || !u.Alive)
                    continue;
                h.Kill(u, DeathCause.Explosion);
                u.SaltoFase = 0;
                u.Preparando = -1f;
                muertos++;
            }
            s.PeleaBajas += muertos;
            s.UltimaExplosionMuertos = muertos;
            // La explosion se ve donde quedo la bomba (no en el carril de la protagonista, que ya se pudo haber movido).
            int carrilBomba = Mathf.Clamp(Mathf.RoundToInt(s.BombaY), 0, RunSimulation.LaneCount - 1);
            h.EventoExplosion(s.BombaX, carrilBomba, s.BombaY);
            var ev = RunEvent.BombaExplota | RunEvent.Explosion;

            // La bomba tambien lastima: a quien se queda cerca le saca 1 de vida (como un golpe, con su respiro).
            float dx = s.PeatonX - s.BombaX;
            bool cerca = Mathf.Abs(dx) <= c.bombaRadioPropio && Mathf.Abs(s.PeatonY - s.BombaY) <= c.bombaCarrilesPropios;
            if (cerca && s.Invulnerable <= 0f)
            {
                int lejos = dx > 0f ? 1 : dx < 0f ? -1 : (s.Mira > 0 ? -1 : 1);
                ev |= Lastimar(lejos);
            }
            return ev;
        }

        // --- Los barriles de la arena ----------------------------------------------

        /// <summary>El barril de la arena mas cercano hacia donde mira, si hay uno al alcance del tiro.</summary>
        bool BarrilAlAlcance(out float x, out int indice)
        {
            x = 0f;
            indice = -1;
            if (Barrels == null || Run == null)
                return false;
            // Solo los de la arena: los que quedaron cerca de donde se vario la moto.
            float zona = c.arenaMedioAncho + 6f;
            return Barrels.TryNearestBarrelAhead(s.PeatonX, s.Mira, s.PeatonY, c.toleranciaCarril, c.shotRangeMeters,
                                                 centro - zona, centro + zona, out x, out indice);
        }

        RunEvent ExplotarBarril(float x, int indice)
        {
            h.Tracer(s.PeatonX, x, s.Lane);
            int antes = VivosEnArena();
            Barrels.Detonate(indice, Run);
            int cayeron = Mathf.Max(0, antes - VivosEnArena());
            s.PeleaBajas += cayeron;
            return RunEvent.Shot | RunEvent.Explosion;
        }

        /// <summary>El tipo de zombie que toca en la oleada actual, segun sus pesos. Sin oleadas, el que ya tenia.</summary>
        int ElegirTipo(int actual)
        {
            var o = OleadaActual;
            if (o == null || o.pesos == null || o.pesos.Length == 0)
                return actual;

            int tipos = c.zombies != null && c.zombies.types != null ? Mathf.Max(1, c.zombies.types.Count) : 1;
            int n = Mathf.Min(o.pesos.Length, tipos);
            float total = 0f;
            for (int i = 0; i < n; i++)
                total += Mathf.Max(0f, o.pesos[i]);
            if (total <= 0f)
                return actual;

            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < n; i++)
            {
                r -= Mathf.Max(0f, o.pesos[i]);
                if (r <= 0f)
                    return i;
            }
            return n - 1;
        }

        public RunEvent Step(PlayerIntent intent, float dt)
        {
            var ev = RunEvent.None;

            s.PeleaTiempo += dt;
            s.Invulnerable = Mathf.Max(0f, s.Invulnerable - dt);
            s.GolpeEnfriamiento = Mathf.Max(0f, s.GolpeEnfriamiento - dt);
            s.Golpeando = Mathf.Max(0f, s.Golpeando - dt);
            s.ComboVentana = Mathf.Max(0f, s.ComboVentana - dt);
            if (s.ComboVentana <= 0f && s.GolpeEnfriamiento <= 0f)
                s.ComboPaso = 0;

            ev |= MoverProtagonista(intent, dt);
            ev |= Pegar(intent);
            ev |= Disparar(intent);
            ev |= PonerBomba(intent);
            ev |= AtenderBomba(dt);
            ev |= LanzarGranada(intent);
            ev |= AtenderGranada(dt);
            ev |= AtenderOleadas(dt);
            ev |= MoverZombies(dt);
            ev |= AtenderDrops(dt);
            if (s.Phase != RunPhase.Pelea)
                return ev; // la derribaron
            Separar();

            return ev;
        }

        // --- La protagonista ---------------------------------------------------

        RunEvent MoverProtagonista(PlayerIntent intent, float dt)
        {
            var ev = RunEvent.None;
            if (Mathf.Abs(intent.MoveX) > 0.01f)
            {
                s.Mira = intent.MoveX > 0f ? 1 : -1;
                s.PeatonX = Mathf.Clamp(s.PeatonX + intent.MoveX * c.peatonVelocidad * dt,
                    centro - c.arenaMedioAncho, centro + c.arenaMedioAncho);
            }

            float maxCarril = RunSimulation.LaneCount - 1;
            if (Mathf.Abs(intent.MoveY) > 0.01f)
            {
                // Libre entre los carriles: el stick a medias camina a medias.
                s.PeatonY = Mathf.Clamp(s.PeatonY + Mathf.Clamp(intent.MoveY, -1f, 1f) * c.peatonVelocidadCarril * dt, 0f, maxCarril);
            }
            else if (intent.LaneDelta != 0 && !intent.MovimientoLibre)
            {
                // Sin accion de movimiento libre (asset de entrada viejo): salta de a un carril.
                int destino = Mathf.Clamp(Mathf.RoundToInt(s.PeatonY) + System.Math.Sign(intent.LaneDelta), 0, RunSimulation.LaneCount - 1);
                if (destino != Mathf.RoundToInt(s.PeatonY))
                {
                    s.PeatonY = destino;
                    ev |= RunEvent.LaneChanged;
                }
            }

            // Lane es el mas cercano (lo usan los disparos y los zombies para perseguirla) y
            // LaneVisual es ella misma: la vista y el orden de dibujo la siguen sin escalones.
            s.Lane = Mathf.Clamp(Mathf.RoundToInt(s.PeatonY), 0, RunSimulation.LaneCount - 1);
            s.LaneVisual = s.PeatonY;
            return ev;
        }

        /// <summary>
        /// Cadena de 3 golpes: rapidos los dos primeros (enfriamiento corto) y el tercero, el
        /// remate, derriba a los que la rodean y marca una pausa antes de poder empezar otra
        /// cadena. Si pasa demasiado entre un golpe y el siguiente, la cadena vuelve al primero.
        /// Los golpes al aire tambien cuentan: la cadena es de ritmo, no de aciertos.
        /// </summary>
        RunEvent Pegar(PlayerIntent intent)
        {
            if (!intent.Golpe || s.GolpeEnfriamiento > 0f)
                return RunEvent.None;

            int paso = s.ComboVentana > 0f ? s.ComboPaso + 1 : 1;
            if (paso > 3)
                paso = 1;
            bool remate = paso == 3;

            s.ComboPaso = paso;
            s.GolpeEnfriamiento = remate ? c.rematePausa : c.golpeEnfriamiento;
            s.Golpeando = c.golpeDuracion;
            // La ventana corre desde ahora e incluye el enfriamiento. Despues del remate no hay
            // ventana: lo que sigue es una cadena nueva (el paso se queda en 3 hasta la pausa).
            s.ComboVentana = remate ? 0f : c.golpeEnfriamiento + c.comboVentana;

            var u = MasCercano(s.PeatonX, s.Mira, remate ? c.golpeAlcance + 0.4f : c.golpeAlcance);
            if (remate)
                return Rematar(u);

            if (u == null)
                return RunEvent.GolpeAlAire;

            if (GolpeEnElAire(u))
                return RunEvent.Golpe;
            if (h.Golpear(u, s.Mira, c.golpeEmpuje, c.golpeFreno))
                Baja(u);
            return RunEvent.Golpe;
        }

        /// <summary>
        /// El tercer golpe: el de enfrente lo recibe como un golpe mas (puede morir) y todos los
        /// que tiene delante, a menos de remateRadio, caen al piso sin perder golpes. Los que
        /// quedaron a su espalda no se tocan: el remate pega hacia donde mira.
        /// </summary>
        RunEvent Rematar(ZombieUnit principal)
        {
            bool conecto = false;
            if (principal != null)
            {
                if (!GolpeEnElAire(principal) && h.Golpear(principal, s.Mira, c.golpeEmpuje, c.golpeFreno))
                    Baja(principal);
                conecto = true;
            }

            var units = h.Units;
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (u == principal || !activo[i] || !u.Alive)
                    continue;
                // Un poco mas generoso con los carriles que un golpe comun: el remate es un barrido.
                if (Mathf.Abs(u.Lane - s.PeatonY) > c.toleranciaCarril + 0.5f)
                    continue;
                float dx = u.X - s.PeatonX;
                // Solo hacia donde mira. Uno pegado a ella y apenas detras cuenta (como en el golpe comun).
                if (dx * s.Mira < -AtrasTolerado || Mathf.Abs(dx) > c.remateRadio)
                    continue;
                if (!GolpeEnElAire(u))
                    h.Derribar(u, dx >= 0f ? 1 : -1, c.remateEmpuje, c.remateDerribo);
                conecto = true;
            }

            if (conecto)
                s.PeleaRemates++;
            return conecto ? RunEvent.Golpe | RunEvent.Remate : RunEvent.GolpeAlAire;
        }

        // --- Bajas y drops -------------------------------------------------------

        /// <summary>Un zombie murio por obra de ella: cuenta para la nota y puede soltar algo.</summary>
        void Baja(ZombieUnit u)
        {
            s.PeleaBajas++;

            double r = dropRng.NextDouble();
            DropKind? kind = null;
            if (r < c.dropBotiquin)
                kind = DropKind.Botiquin;
            else if (r < c.dropBotiquin + c.dropBalas)
                kind = DropKind.Balas;
            if (kind == null)
                return;

            if (drops.Count >= MaxDrops)
                drops.RemoveAt(0);   // el mas viejo se va para que entre el nuevo
            float x = u.X;
            if (kind.Value == DropKind.Botiquin && c.botiquinSeparacion > 0f && Mathf.Abs(x - s.PeatonX) < c.botiquinSeparacion)
            {
                // Que no se levante solo al terminar un combo: cae mas alla, del lado donde estaba el zombie.
                float lado = x > s.PeatonX ? 1f : x < s.PeatonX ? -1f : (s.Mira > 0 ? 1f : -1f);
                float izq = centro - c.arenaMedioAncho, der = centro + c.arenaMedioAncho;
                x = s.PeatonX + lado * c.botiquinSeparacion;
                if (x < izq || x > der)
                    x = Mathf.Clamp(s.PeatonX - lado * c.botiquinSeparacion, izq, der);   // contra la pared: del otro lado
            }
            drops.Add(new Drop { Kind = kind.Value, X = x, Lane = u.Lane, Left = c.dropSegundos, Total = c.dropSegundos });
        }

        /// <summary>Cuenta regresiva de los drops y recogida: ella pasa por encima, en un carril cercano.</summary>
        RunEvent AtenderDrops(float dt)
        {
            var ev = RunEvent.None;
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                d.Left -= dt;
                if (d.Left <= 0f)
                {
                    drops.RemoveAt(i);
                    continue;
                }

                if (Mathf.Abs(d.X - s.PeatonX) > c.dropAlcance || Mathf.Abs(d.Lane - s.PeatonY) > c.toleranciaCarril + 0.3f)
                {
                    d.Avisado = false;
                    continue;
                }

                // El bidon del ultimo zombie: tanque lleno de una, el premio de la nota y de vuelta a la moto.
                if (d.Kind == DropKind.Bidon)
                {
                    s.Fuel = c.fuelMax;
                    Premiar();
                    return ev | RunEvent.PickedUp | Terminar();
                }

                bool lleno = d.Kind == DropKind.Botiquin ? s.Vida >= s.VidaMax : s.Ammo >= c.ammoMax;
                if (lleno)
                {
                    // Lo deja ahi, por si lo necesita despues, pero avisa para que no parezca que no pasa nada.
                    if (!d.Avisado)
                    {
                        d.Avisado = true;
                        s.DropLlenoKind = (int)d.Kind;
                        ev |= RunEvent.DropLleno;
                    }
                    continue;
                }

                if (d.Kind == DropKind.Botiquin)
                {
                    s.Vida++;
                    s.PeleaBotiquines++;
                    ev |= RunEvent.BotiquinRecogido;
                }
                else
                {
                    s.Ammo = Mathf.Min(c.ammoMax, s.Ammo + c.dropBalasCantidad);
                    ev |= RunEvent.BalasRecogidas;
                }
                drops.RemoveAt(i);
            }
            return ev;
        }

        // --- La nota -------------------------------------------------------------

        /// <summary>
        /// La nota de la pelea: segun los golpes que se llevo. Ninguno es S; uno, A; dos o tres, B;
        /// mas, C. Si la derribaron y tuvo que reintentar, no pasa de B.
        /// </summary>
        public NotaPelea CalcularNota()
        {
            int golpes = s.PeleaGolpesRecibidos;
            var nota = golpes <= 0 ? NotaPelea.S : golpes == 1 ? NotaPelea.A : golpes <= 3 ? NotaPelea.B : NotaPelea.C;
            bool reintento = c.reintentosPelea - s.ReintentosPelea > 0;
            if (reintento && nota > NotaPelea.B)
                nota = NotaPelea.B;
            return nota;
        }

        /// <summary>La furgoneta deja balas y bateria segun la nota. Devuelve si dejo algo.</summary>
        void Premiar()
        {
            var nota = CalcularNota();
            s.Nota = nota;

            int i = (int)nota;
            int balas = c.premioBalas != null && i < c.premioBalas.Length ? c.premioBalas[i] : 0;
            float bateria = c.premioBateria != null && i < c.premioBateria.Length ? c.premioBateria[i] : 0f;

            int antes = s.Ammo;
            s.Ammo = Mathf.Min(c.ammoMax, s.Ammo + Mathf.Max(0, balas));
            s.PremioBalas = s.Ammo - antes;
            s.PremioBalasLleno = balas > 0 && s.PremioBalas == 0;

            float bateriaAntes = s.Battery;
            s.Battery = Mathf.Min(c.batteryMax, s.Battery + Mathf.Max(0f, bateria));
            s.PremioBateria = s.Battery - bateriaAntes;
            s.PremioBateriaLleno = bateria > 0f && s.PremioBateria <= 0f;
        }

        RunEvent Disparar(PlayerIntent intent)
        {
            if (!intent.Fire)
                return RunEvent.None;
            if (s.Ammo <= 0)
                return RunEvent.ShotDenied;

            s.Ammo--;
            var u = MasCercano(s.PeatonX, s.Mira, c.shotRangeMeters);

            // Pega en lo primero que encuentra hacia donde mira: un barril de la arena o un zombie.
            if (BarrilAlAlcance(out float barrilX, out int barrilIndice) &&
                (u == null || Mathf.Abs(barrilX - s.PeatonX) <= Mathf.Abs(u.X - s.PeatonX)))
                return ExplotarBarril(barrilX, barrilIndice);

            if (u != null)
            {
                if (u.SaltoFase == 2)
                {
                    BalazoEnElAire(u);
                    return RunEvent.Shot;
                }
                if (u.SaltoFase == 1)
                {
                    u.SaltoFase = 0;   // el tiro le corta el salto
                    u.SaltoT = 0f;
                }
                if (h.HitZombie(u, s.PeatonX))
                    Baja(u);
                return RunEvent.Shot;
            }
            h.MissToward(s.PeatonX, s.PeatonX + s.Mira * c.shotRangeMeters, s.Lane);
            return RunEvent.Shot | RunEvent.ShotMissed;
        }

        /// <summary>
        /// El zombie vivo y peleando mas cercano hacia donde mira, dentro del alcance y alineado
        /// con ella (a lo sumo toleranciaCarril carriles de diferencia: ella camina entre carriles).
        /// </summary>
        ZombieUnit MasCercano(float desde, int mira, float alcance)
        {
            ZombieUnit mejor = null;
            float mejorDist = float.MaxValue;
            var units = h.Units;
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (!activo[i] || !u.Alive || !Alineado(u))
                    continue;
                float d = (u.X - desde) * mira;
                // Un poco hacia atras tambien cuenta: pegado a ella, aunque este apenas detras.
                if (d < -0.3f || d > alcance || d >= mejorDist)
                    continue;
                mejor = u;
                mejorDist = d;
            }
            return mejor;
        }

        // --- Los zombies -------------------------------------------------------

        /// <summary>
        /// Metros por segundo con los que camina este zombie a pie: la base de la pelea por su tipo y
        /// por la oleada. La vista la usa para elegir entre caminar lento o correr.
        /// </summary>
        public float VelocidadDe(ZombieUnit u) =>
            u.PuedeSaltar && u.SaltoFase == 0
                ? c.saltoVelocidadCarrera   // los que todavia tienen que saltar vienen corriendo
                : c.zombieVelocidadPelea * MultiplicadorAPie(h.Type(u.Type)) * (OleadaActual != null ? OleadaActual.velocidad : 1f);


        /// <summary>Si el zombie esta en un carril lo bastante cerca del de ella para pegarse.</summary>
        bool Alineado(ZombieUnit u) => Mathf.Abs(u.Lane - s.PeatonY) <= c.toleranciaCarril;

        /// <summary>El zombie sale de la pelea: queda esperando fuera de cuadro, listo para una oleada nueva.</summary>
        void Estacionar(int i)
        {
            var u = h.Units[i];
            activo[i] = false;
            u.Alive = true;
            u.CorpseLeft = 0f;
            u.X = centro - Estacionamiento;
            u.Stagger = 0f;
            u.Derribado = 0f;
            u.Preparando = -1f;
            u.PuedeSaltar = false;
            u.SaltoFase = 0;
        }

        RunEvent MoverZombies(float dt)
        {
            var ev = RunEvent.None;
            var units = h.Units;

            // Cuantos estan preparando un ataque ahora: hay un tope, los demas esperan su turno.
            int atacando = 0;
            int saltando = 0;
            for (int i = 0; i < units.Length; i++)
            {
                if (!activo[i] || !units[i].Alive)
                    continue;
                if (units[i].Preparando >= 0f)
                    atacando++;
                if (units[i].SaltoFase == 1 || units[i].SaltoFase == 2)
                {
                    atacando++;
                    saltando++;
                }
            }

            for (int i = 0; i < units.Length; i++)
            {
                if (!activo[i])
                    continue;
                var u = units[i];

                if (!u.Alive)
                {
                    // El cadaver queda un rato y despues se va. Nadie lo repone: cada oleada trae su cantidad.
                    u.CorpseLeft -= dt;
                    if (u.CorpseLeft <= 0f)
                        Estacionar(i);
                    continue;
                }

                // Saltando: el salto manda hasta que termina (se agacha, vuela, cae y se levanta).
                if (u.SaltoFase != 0)
                {
                    ev |= AvanzarSalto(u, dt);
                    if (s.Phase != RunPhase.Pelea)
                        return ev;
                    continue;
                }

                u.Derribado = Mathf.Max(0f, u.Derribado - dt);
                if (u.Stagger > 0f)
                {
                    u.Stagger = Mathf.Max(0f, u.Stagger - dt);
                    continue;
                }

                float dx = s.PeatonX - u.X;
                u.Mira = dx >= 0f ? 1 : -1;

                bool alineado = Alineado(u);
                if (!alineado && !u.PuedeSaltar)
                {
                    u.CambioCarril -= dt;
                    if (Mathf.Abs(dx) < DistanciaParaCambiarCarril && u.CambioCarril <= 0f)
                    {
                        u.Lane += System.Math.Sign(s.Lane - u.Lane);
                        u.CambioCarril = 0.6f + (float)rng.NextDouble() * 0.6f;
                    }
                }

                // Los de la primera oleada vienen corriendo y, al acercarse, se agachan y saltan sobre ella desde el
                // carril en que esten (con lugar para pocos a la vez; los demas esperan su turno a distancia).
                if (u.PuedeSaltar && u.Preparando < 0f)
                {
                    float distancia = Mathf.Abs(dx);
                    if (distancia <= c.saltoDistanciaMax && distancia >= c.saltoDistanciaMin)
                    {
                        if (atacando < c.maxAtacantes && saltando < c.saltoMaxSimultaneos)
                        {
                            IniciarSalto(u);
                            atacando++;
                            saltando++;
                        }
                        continue;
                    }
                }

                bool enRango = alineado && Mathf.Abs(dx) <= c.zombieAlcance;

                if (u.Preparando >= 0f)
                {
                    u.Preparando += dt;
                    if (u.Preparando >= PreparacionActual)
                    {
                        u.Preparando = -1f;
                        u.Enfriamiento = c.zombieEnfriamiento / RitmoDeAtaque;
                        if (enRango && s.Invulnerable <= 0f)
                            ev |= Lastimar(System.Math.Sign(dx));
                        if (s.Phase != RunPhase.Pelea)
                            return ev;
                    }
                    continue;
                }

                if (enRango)
                {
                    u.Enfriamiento -= dt;
                    // Listo para pegar, pero solo prepara el ataque si hay un lugar libre.
                    if (u.Enfriamiento <= 0f && atacando < c.maxAtacantes)
                    {
                        u.Preparando = 0f;
                        atacando++;
                    }
                    continue;
                }

                // Se acerca: en su carril hasta quedar a tiro; en otro, hasta ponerse a la par.
                float objetivo = alineado ? c.zombieAlcance * 0.8f : 0.4f;
                if (Mathf.Abs(dx) > objetivo)
                {
                    float v = VelocidadDe(u);
                    u.X += Mathf.Sign(dx) * Mathf.Min(v * dt, Mathf.Abs(dx) - objetivo);
                }
            }
            return ev;
        }

        // --- El salto de los zombies ---------------------------------------------

        /// <summary>Donde va a caer: un poco antes de ella, de su lado, para no subirsele encima.</summary>
        float PuntoDeCaida(ZombieUnit u)
        {
            float dir = s.PeatonX >= u.X ? 1f : -1f;
            return s.PeatonX - dir * c.saltoAlcanceCaida;
        }

        void IniciarSalto(ZombieUnit u)
        {
            u.PuedeSaltar = false;
            u.SaltoFase = 1;
            u.SaltoT = 0f;
            u.Preparando = -1f;
            u.Mira = s.PeatonX >= u.X ? 1 : -1;
            u.SaltoX0 = u.X;
            u.SaltoX1 = PuntoDeCaida(u);
            u.SaltoLane = s.Lane;
        }

        RunEvent AvanzarSalto(ZombieUnit u, float dt)
        {
            u.SaltoT += dt;
            switch (u.SaltoFase)
            {
                case 1:
                {
                    // Agachado sigue deslizandose hacia ella con la inercia de la carrera (se frena de a poco), pero
                    // deja siempre un tramo de vuelo. El punto de caida la sigue; al despegar queda fijo y se puede esquivar.
                    float dir = s.PeatonX >= u.X ? 1f : -1f;
                    float hastaElPunto = Mathf.Abs(PuntoDeCaida(u) - u.X);
                    float inercia = c.saltoVelocidadCarrera * (1f - Mathf.Clamp01(u.SaltoT / Mathf.Max(0.01f, c.saltoAgacharse)));
                    u.X += dir * Mathf.Min(inercia * dt, Mathf.Max(0f, hastaElPunto - MinimoDeVuelo));
                    u.SaltoX1 = PuntoDeCaida(u);
                    u.SaltoLane = s.Lane;
                    if (u.SaltoT >= c.saltoAgacharse)
                    {
                        u.SaltoFase = 2;
                        u.SaltoT = 0f;
                        u.SaltoX0 = u.X;
                        u.Lane = u.SaltoLane;   // salta hacia el carril de ella (la vista lo desliza)
                    }
                    return RunEvent.None;
                }

                case 2:
                {
                    float t = c.saltoVuelo > 0f ? Mathf.Clamp01(u.SaltoT / c.saltoVuelo) : 1f;
                    u.X = Mathf.Lerp(u.SaltoX0, u.SaltoX1, t);
                    return t >= 1f ? Aterrizar(u) : RunEvent.None;
                }

                default:
                    u.Derribado = Mathf.Max(0f, u.Derribado - dt);
                    if (u.Stagger > 0f)
                        u.Stagger = Mathf.Max(0f, u.Stagger - dt);
                    if (u.SaltoT >= c.saltoAterrizaje)
                    {
                        u.SaltoFase = 0;
                        u.SaltoT = 0f;
                        u.Enfriamiento = 0.3f + (float)rng.NextDouble() * 0.5f;
                    }
                    return RunEvent.None;
            }
        }

        /// <summary>Cae en el punto marcado: si ella sigue ahi (y no esta en su respiro) le saca 1 de vida.</summary>
        RunEvent Aterrizar(ZombieUnit u)
        {
            u.SaltoFase = 3;
            u.SaltoT = 0f;
            u.X = u.SaltoX1;

            bool acierta = Alineado(u) && Mathf.Abs(s.PeatonX - u.X) <= c.saltoRadio && s.Invulnerable <= 0f;
            if (!acierta)
            {
                s.PeleaSaltosEsquivados++;
                return RunEvent.None;
            }

            s.PeleaSaltosAcertados++;
            u.SaltoT = Mathf.Max(0f, c.saltoAterrizaje - 0.4f);   // si le cayo encima se recupera antes
            return Lastimar(s.PeatonX >= u.X ? 1 : -1);
        }

        /// <summary>
        /// Un golpe a un zombie que salta. En el aire lo mata de una (devuelve true); agachado, le
        /// corta el salto y el golpe sigue como uno comun (devuelve false).
        /// </summary>
        bool GolpeEnElAire(ZombieUnit u)
        {
            if (u.SaltoFase == 2)
            {
                h.Kill(u, DeathCause.Golpe, s.Mira);
                u.SaltoFase = 0;
                s.PeleaSaltosCortados++;
                Baja(u);
                return true;
            }
            if (u.SaltoFase == 1)
            {
                u.SaltoFase = 0;
                u.SaltoT = 0f;
            }
            return false;
        }

        /// <summary>
        /// Un balazo a un zombie en el aire: le saca 1 golpe. Si no llega a morir, lo tira donde esta
        /// (cae sin hacerle dano a ella).
        /// </summary>
        void BalazoEnElAire(ZombieUnit u)
        {
            h.Tracer(s.PeatonX, u.X, u.Lane);
            u.Golpes--;
            s.PeleaSaltosCortados++;
            if (u.Golpes <= 0)
            {
                h.Kill(u, DeathCause.Bullet, s.Mira);
                u.SaltoFase = 0;
                Baja(u);
                return;
            }
            u.SaltoFase = 3;
            u.SaltoT = 0f;
            u.Stagger = Mathf.Max(u.Stagger, c.golpeFreno);
        }

        RunEvent Lastimar(int desde)
        {
            s.Vida--;
            s.PeleaGolpesRecibidos++;
            s.Invulnerable = c.invulnerableTrasGolpe;
            // El golpe la corre un poco, alejandola del que pego.
            s.PeatonX = Mathf.Clamp(s.PeatonX + desde * 0.8f, centro - c.arenaMedioAncho, centro + c.arenaMedioAncho);
            if (s.Vida > 0)
                return RunEvent.Lastimada;

            // Derribada con un reintento: la pelea vuelve a empezar en vez de perder.
            if (s.ReintentosPelea > 0)
                return Reintentar();

            s.Phase = RunPhase.Lost;
            s.Loss = LossReason.Derribada;
            return RunEvent.Lastimada | RunEvent.Lost;
        }

        /// <summary>
        /// Vuelve al principio de la pelea: vida entera y la arena rearmada desde la primera oleada.
        /// No cuenta como otra pelea. Un respiro de 2 s sin dano.
        /// </summary>
        RunEvent Reintentar()
        {
            s.ReintentosPelea--;
            s.Vida = c.vidaPelea;
            s.Oleada = 0;
            drops.Clear();
            s.PeleaTiempo = 0f;
            s.BombaMecha = 0f;
            s.GranadaVuelo = 0f;
            s.BidonEnElPiso = false;
            s.Invulnerable = 2f;
            s.GolpeEnfriamiento = 0f;
            s.Golpeando = 0f;
            s.ComboPaso = 0;
            s.ComboVentana = 0f;
            s.PeatonX = centro;
            ArmarArena(desdeLaHorda: false);
            return RunEvent.Lastimada | RunEvent.PeleaReintenta;
        }

        void Preparar(int indice, float x, int carril, bool nuevo, bool conservarTipo = false)
        {
            var u = h.Units[indice];
            // Con oleadas, el tipo lo da la oleada; si cambia es otro zombie para la vista (otra pinta).
            // Los que siguen de la horda de la moto conservan el suyo: es el mismo zombie que se veia.
            int tipo = conservarTipo ? u.Type : ElegirTipo(u.Type);
            if (tipo != u.Type)
            {
                u.Type = tipo;
                nuevo = true;
            }
            if (nuevo)
                u.Generation++;
            x = Despejar(indice, x, carril);
            u.Alive = true;
            u.Cause = DeathCause.None;
            u.CorpseLeft = 0f;
            u.Stagger = 0f;
            u.Derribado = 0f;
            u.X = x;
            u.Lane = carril;
            u.Golpes = h.Type(u.Type).golpesParaMorir;
            u.Preparando = -1f;
            // En la primera oleada llegan saltando (la transicion de correr a pelear a pie); en las demas, caminando.
            u.PuedeSaltar = c.saltoActivo && s.Oleada == 0;
            u.SaltoFase = 0;
            u.SaltoT = 0f;
            u.Enfriamiento = 0.3f + (float)rng.NextDouble() * 0.5f;
            u.CambioCarril = (float)rng.NextDouble() * 0.8f;
            u.Mira = x < s.PeatonX ? 1 : -1;
        }

        // --- Que no se encimen -------------------------------------------------

        /// <summary>
        /// Un lugar libre donde aparecer: si hay otro zombie vivo del mismo carril a menos de la
        /// separacion, se corre hacia afuera de la arena hasta quedar a la distancia justa.
        /// </summary>
        float Despejar(int indice, float x, int carril)
        {
            float sep = c.zombieSeparacion;
            if (sep <= 0f)
                return x;
            float afuera = x < centro ? -1f : 1f;
            var units = h.Units;
            for (int vuelta = 0; vuelta < units.Length; vuelta++)
            {
                bool libre = true;
                for (int j = 0; j < units.Length; j++)
                {
                    if (j == indice || !activo[j] || !units[j].Alive || units[j].Lane != carril)
                        continue;
                    if (Mathf.Abs(units[j].X - x) < sep)
                    {
                        x = units[j].X + afuera * sep;
                        libre = false;
                    }
                }
                if (libre)
                    break;
            }
            return x;
        }

        /// <summary>
        /// Dos zombies vivos del mismo carril no pueden estar a menos de la separacion. Cuando se
        /// encimarian, el que esta mas lejos de la protagonista se queda atras del otro: asi hacen
        /// fila, y el golpe que empuja a uno contra otro tampoco los superpone. Solo se mueve al
        /// de atras, para que el que esta pegado a ella no sea empujado hacia adentro.
        /// </summary>
        void Separar()
        {
            float sep = c.zombieSeparacion;
            if (sep <= 0f)
                return;

            var units = h.Units;
            for (int carril = 0; carril < RunSimulation.LaneCount; carril++)
            {
                fila.Clear();
                for (int i = 0; i < units.Length; i++)
                    if (activo[i] && units[i].Alive && units[i].Lane == carril && units[i].SaltoFase != 2)
                        fila.Add(i);
                if (fila.Count < 2)
                    continue;

                // De los mas cercanos a ella a los mas lejanos: cada uno se acomoda contra los de adelante.
                float px = s.PeatonX;
                fila.Sort((a, b) =>
                {
                    int cmp = Mathf.Abs(units[a].X - px).CompareTo(Mathf.Abs(units[b].X - px));
                    return cmp != 0 ? cmp : a.CompareTo(b);
                });

                for (int k = 1; k < fila.Count; k++)
                {
                    var u = units[fila[k]];
                    float afuera = u.X < px ? -1f : 1f;
                    for (int vuelta = 0; vuelta < fila.Count; vuelta++)
                    {
                        bool movido = false;
                        for (int m = 0; m < k; m++)
                        {
                            float x = units[fila[m]].X;
                            if (Mathf.Abs(u.X - x) < sep)
                            {
                                u.X = x + afuera * sep;
                                movido = true;
                            }
                        }
                        if (!movido)
                            break;
                    }
                }
            }
        }

        // --- Volver a la moto ---------------------------------------------------

        RunEvent Terminar()
        {
            s.BidonEnElPiso = false;
            s.BombaMecha = 0f;
            s.GranadaVuelo = 0f;
            s.OleadaRespiro = 0f;
            s.Invulnerable = 0f;
            s.Phase = RunPhase.Running;
            s.PlayerX = s.PeatonX;
            s.Fuel = c.fuelMax;
            s.Vida = 0;
            s.ArranqueRestante = c.arranqueSegundos;
            drops.Clear();
            h.Reset(s.PlayerX - c.gapTrasPelea);
            s.HordeX = h.FrontX;
            activo = null;
            return RunEvent.PeleaTermina;
        }
    }
}
