using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;

namespace Zombineta.Telemetria
{
    /// <summary>
    /// Lo que deja una partida de prueba, en filas de un CSV con ";" (Excel en espanol lo abre de
    /// una): un evento por fila (se quedo sin nafta, empezo la pelea, una oleada, un golpe recibido,
    /// la nota...) y un resumen al final con cuanto duro cada fase. C# plano y sin archivos: la
    /// escritura es de TelemetriaDePlaytest. Sirve para comparar playtests sin tomar notas a mano.
    /// </summary>
    public sealed class SesionTelemetria
    {
        public const string Cabecera = "t;nivel;intento;fase;metros;nafta;balas;vida;evento;detalle";

        readonly string nivel;
        readonly int intento;

        // Cuanto duro cada fase, en segundos reales.
        public float SegundosCorriendo { get; private set; }
        public float SegundosFrenando { get; private set; }
        public float SegundosPeleando { get; private set; }
        public float MetrosMaximos { get; private set; }

        public int Peleas { get; private set; }
        public int Choques { get; private set; }
        public int Caidas { get; private set; }
        public int Bajas { get; private set; }
        public int Remates { get; private set; }
        public int GolpesRecibidos { get; private set; }
        public int Botiquines { get; private set; }
        public int Reintentos { get; private set; }
        public int ItemsPasados { get; private set; }
        public int GranadasLanzadas { get; private set; }
        public int BombasUsadas { get; private set; }

        // Por cada recolectable del recorrido: lo mas cerca (en carriles) que estuvo la moto mientras lo pisaba.
        readonly Dictionary<int, float> cercania = new Dictionary<int, float>();
        readonly Dictionary<int, bool> volando = new Dictionary<int, bool>();
        readonly HashSet<int> pasados = new HashSet<int>();

        public SesionTelemetria(string nivel, int intento)
        {
            this.nivel = nivel ?? "";
            this.intento = intento;
        }

        public float SegundosTotales => SegundosCorriendo + SegundosFrenando + SegundosPeleando;

        /// <summary>Acumula el tiempo en la fase en que esta la partida.</summary>
        public void Tiempo(RunState s, float dt)
        {
            if (dt <= 0f)
                return;
            if (s.Phase == RunPhase.Pelea)
                SegundosPeleando += dt;
            else if (s.Phase == RunPhase.Running)
            {
                if (s.FrenadoRestante > 0f)
                    SegundosFrenando += dt;
                else
                    SegundosCorriendo += dt;
            }
            if (s.PlayerX > MetrosMaximos)
                MetrosMaximos = s.PlayerX;
        }

        /// <summary>Las filas de lo que paso en este paso. Lo que no se registra (cada golpe, cada disparo) no genera fila.</summary>
        public List<string> Eventos(float t, RunState s, RunEvent ev)
        {
            var filas = new List<string>();

            if ((ev & RunEvent.Crashed) != 0) { Choques++; filas.Add(Fila(t, s, "choque", "")); }
            if ((ev & RunEvent.Fell) != 0) { Caidas++; filas.Add(Fila(t, s, "caida", "")); }
            if ((ev & RunEvent.RanOutOfFuel) != 0) filas.Add(Fila(t, s, "sin_nafta", "peleas=" + s.Peleas));
            if ((ev & RunEvent.Frenando) != 0) filas.Add(Fila(t, s, "frenando", "velocidad=" + N(s.VelocidadAlFrenar)));
            if ((ev & RunEvent.PeleaEmpieza) != 0)
            {
                Peleas++;
                filas.Add(Fila(t, s, "pelea_empieza", "pelea=" + s.Peleas + ";vida=" + s.Vida));
            }
            if ((ev & RunEvent.OleadaNueva) != 0)
                filas.Add(Fila(t, s, "oleada", "n=" + (s.Oleada + 1) + ";de=" + s.Oleadas + ";zombies=" + s.OleadaTotal));
            if ((ev & RunEvent.Remate) != 0) Remates++;
            if ((ev & RunEvent.Lastimada) != 0)
            {
                GolpesRecibidos++;
                filas.Add(Fila(t, s, "golpe_recibido", "vida=" + s.Vida));
            }
            if ((ev & RunEvent.BotiquinRecogido) != 0)
            {
                Botiquines++;
                filas.Add(Fila(t, s, "botiquin", "vida=" + s.Vida));
            }
            if ((ev & RunEvent.BalasRecogidas) != 0) filas.Add(Fila(t, s, "balas_recogidas", "balas=" + s.Ammo));
            if ((ev & RunEvent.HordaRetrocede) != 0) filas.Add(Fila(t, s, "horda_retrocede", "metros=" + N(s.Gap)));
            if ((ev & RunEvent.DropLleno) != 0) filas.Add(Fila(t, s, "drop_lleno", s.DropLlenoKind == 0 ? "vida" : "balas"));
            if ((ev & RunEvent.PeleaReintenta) != 0)
            {
                Reintentos++;
                filas.Add(Fila(t, s, "pelea_reintenta", ""));
            }
            if ((ev & RunEvent.GranadaLanzada) != 0)
            {
                GranadasLanzadas++;
                filas.Add(Fila(t, s, "granada_lanzada", "quedan=" + s.Granadas));
            }
            if ((ev & RunEvent.GranadaExplota) != 0) filas.Add(Fila(t, s, "granada_explota", "muertos=" + s.UltimaExplosionMuertos));
            if ((ev & RunEvent.GranadaDenegada) != 0) filas.Add(Fila(t, s, "granada_denegada", ""));
            if ((ev & RunEvent.BombaPuesta) != 0)
            {
                BombasUsadas++;
                filas.Add(Fila(t, s, "bomba_puesta", "quedan=" + s.Bombas));
            }
            if ((ev & RunEvent.BombaExplota) != 0) filas.Add(Fila(t, s, "bomba_explota", "muertos=" + s.UltimaExplosionMuertos));
            if ((ev & RunEvent.PeleaGanada) != 0) filas.Add(Fila(t, s, "pelea_ganada", "duracion=" + N(s.PeleaTiempo)));
            if ((ev & RunEvent.PeleaTermina) != 0)
            {
                Bajas += s.PeleaBajas;
                filas.Add(Fila(t, s, "pelea_termina",
                    "nota=" + s.Nota + ";bajas=" + s.PeleaBajas + ";remates=" + s.PeleaRemates +
                    ";golpes_recibidos=" + s.PeleaGolpesRecibidos + ";botiquines=" + s.PeleaBotiquines +
                    ";saltos_acertados=" + s.PeleaSaltosAcertados + ";saltos_esquivados=" + s.PeleaSaltosEsquivados +
                    ";saltos_cortados=" + s.PeleaSaltosCortados + ";bombas=" + s.PeleaBombas +
                    ";duracion_pelea=" + N(s.PeleaTiempo) +
                    ";premio_balas=" + s.PremioBalas + ";premio_bateria=" + N(s.PremioBateria)));
            }
            if ((ev & RunEvent.Won) != 0) filas.Add(Fila(t, s, "victoria", ""));
            if ((ev & RunEvent.Lost) != 0) filas.Add(Fila(t, s, "derrota", "causa=" + s.Loss));
            return filas;
        }

        /// <summary>
        /// Los recolectables que la moto dejo atras sin agarrar: una fila por cada uno, con el carril de
        /// cada uno, lo cerca que estuvo y si iba en el aire. Con eso se ve si fallo por carril, por
        /// altura o por dibujo.
        /// </summary>
        public List<string> RecolectablesPasados(float t, RunState s, GameConfig cfg, LevelRuntime level)
        {
            var filas = new List<string>();
            if (level == null || cfg == null || s.Phase != RunPhase.Running)
                return filas;

            for (int i = 0; i < level.Items.Length; i++)
            {
                var item = level.Items[i];
                var e = item.Entry;
                if (item.Consumed || pasados.Contains(i) || !LevelRuntime.EsRecurso(e.kind) ||
                    (e.kind == LevelEntryKind.Fuel && LevelRuntime.BidonInexistente(cfg, s, e.distance)))
                    continue;

                float medio = LevelRuntime.MedioLargo(e, cfg);
                float adelante = e.distance - s.PlayerX;

                // Mientras la moto lo pisa (de punta a cola) se anota lo cerca que paso.
                if (adelante - medio <= cfg.motoNariz && adelante + medio >= -cfg.motoCola)
                {
                    float dif = Mathf.Abs(s.LaneVisual - e.lane);
                    cercania[i] = cercania.TryGetValue(i, out var antes) ? Mathf.Min(antes, dif) : dif;
                    volando[i] = (volando.TryGetValue(i, out var v) && v) || s.Airborne;
                    continue;
                }

                // Ya quedo todo atras sin que se lo lleve.
                if (adelante + medio < -cfg.motoCola && cercania.ContainsKey(i))
                {
                    pasados.Add(i);
                    ItemsPasados++;
                    filas.Add(Fila(t, s, "item_pasado",
                        "tipo=" + e.kind + ";metros=" + N(e.distance) + ";carril_item=" + e.lane +
                        ";dif_carril=" + N(cercania[i]) + ";altura_item=" + N(e.height) + ";en_aire=" + (volando[i] ? "si" : "no")));
                }
            }
            return filas;
        }

        public string Inicio(float t, RunState s) => Fila(t, s, "inicio", "");

        /// <summary>La fila final: como termino y cuanto duro cada fase.</summary>
        public string Resumen(float t, RunState s, string resultado) =>
            Fila(t, s, "resumen",
                "resultado=" + resultado + ";causa=" + s.Loss + ";duracion=" + N(SegundosTotales) +
                ";corriendo=" + N(SegundosCorriendo) + ";frenando=" + N(SegundosFrenando) + ";peleando=" + N(SegundosPeleando) +
                ";metros_max=" + N(MetrosMaximos) + ";peleas=" + Peleas + ";reintentos=" + Reintentos +
                ";choques=" + Choques + ";caidas=" + Caidas + ";bajas=" + Bajas + ";remates=" + Remates +
                ";golpes_recibidos=" + GolpesRecibidos + ";botiquines=" + Botiquines +
                ";items_pasados=" + ItemsPasados + ";granadas_lanzadas=" + GranadasLanzadas + ";bombas_usadas=" + BombasUsadas);

        /// <summary>Una fila del CSV. El detalle puede traer ";" propios (clave=valor;clave=valor): va entre comillas.</summary>
        public string Fila(float t, RunState s, string evento, string detalle) =>
            Fila(t, nivel, intento, FaseDe(s), s.PlayerX, s.Fuel, s.Ammo, s.Vida, evento, detalle);

        public static string Fila(float t, string nivel, int intento, string fase, float metros, float nafta,
                                  int balas, int vida, string evento, string detalle)
        {
            var sb = new StringBuilder(128);
            sb.Append(N(t)).Append(';')
              .Append(Limpiar(nivel)).Append(';')
              .Append(intento.ToString(CultureInfo.InvariantCulture)).Append(';')
              .Append(fase).Append(';')
              .Append(N(metros)).Append(';')
              .Append(N(nafta)).Append(';')
              .Append(balas.ToString(CultureInfo.InvariantCulture)).Append(';')
              .Append(vida.ToString(CultureInfo.InvariantCulture)).Append(';')
              .Append(Limpiar(evento)).Append(';')
              .Append('"').Append((detalle ?? "").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ")).Append('"');
            return sb.ToString();
        }

        static string FaseDe(RunState s)
        {
            switch (s.Phase)
            {
                case RunPhase.Pelea: return "pelea";
                case RunPhase.Won: return "ganada";
                case RunPhase.Lost: return "perdida";
                default: return s.FrenadoRestante > 0f ? "frenando" : "corriendo";
            }
        }

        static string N(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        static string Limpiar(string texto) =>
            (texto ?? "").Replace(';', ',').Replace("\r", " ").Replace("\n", " ");

        /// <summary>Nombre del archivo de una sesion: fecha y hora, para que no se pisen.</summary>
        public static string NombreDeArchivo(System.DateTime ahora) =>
            "sesion_" + ahora.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv";
    }
}
