using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Level
{
    /// <summary>
    /// Resuelve los encuentros con el recorrido: que agarraste, contra que
    /// chocaste. Vive del lado de la simulacion y no del de la fisica, porque la
    /// moto se mueve por transform y no por Rigidbody: un trigger seria fragil y,
    /// sobre todo, no se podria testear sin abrir una escena.
    /// </summary>
    public sealed class LevelRuntime : IBarrelField
    {
        /// <summary>Una entrada del recorrido mas su estado en esta partida.</summary>
        public sealed class Item
        {
            public LevelEntry Entry;
            public bool Consumed;
            /// <summary>
            /// Lo despejo la arena de la ultima pelea (ClearZone): no cuenta para nada, pero la vista lo
            /// deja de decorado mientras dura la pelea, como si la protagonista siguiera varada ahi.
            /// </summary>
            public bool Despejado;
        }

        /// <summary>Cuanto puede desviarse el carril visual y aun asi contar como encuentro.</summary>
        const float LaneTolerance = 0.5f;

        public Item[] Items { get; }

        public LevelRuntime(LevelDefinition definition)
        {
            if (definition == null || definition.entries == null)
            {
                Items = new Item[0];
                return;
            }

            definition.SortByDistance();

            Items = new Item[definition.entries.Count];
            for (int i = 0; i < Items.Length; i++)
                Items[i] = new Item { Entry = definition.entries[i], Consumed = false };
        }

        /// <summary>Lleva un item a otros metros y lo deja sin agarrar (el tutorial repone por delante).</summary>
        public void Mover(int indice, float metros)
        {
            var item = Items[indice];
            var entry = item.Entry;
            entry.distance = metros;
            item.Entry = entry;
            item.Consumed = false;
            item.Despejado = false;
        }

        public void Reset()
        {
            for (int i = 0; i < Items.Length; i++)
            {
                Items[i].Consumed = false;
                Items[i].Despejado = false;
            }
        }

        /// <summary>
        /// Si los bidones todavia no cuentan: algunos niveles (el primero) no tienen nafta hasta
        /// que se usa el primer auxilio. Mientras tanto no se agarran ni se ven.
        /// </summary>
        public static bool BidonesBloqueados(GameConfig config, RunState state) =>
            config != null && state != null && config.bidonesDesdeAuxilio > state.AyudasUsadas;

        /// <summary>
        /// Despeja un tramo del recorrido (la arena de la pelea a pie): los obstaculos, rampas,
        /// barriles y zombies de frente desaparecen, como si ya se hubieran pasado, y no vuelven
        /// cuando la moto retoma. Los recursos se dejan (la vista los esconde mientras se pelea).
        /// Lo despejado queda marcado: la vista lo conserva de decorado durante la pelea, salvo los
        /// zombies de frente. Devuelve cuantos despejo.
        /// </summary>
        public int ClearZone(float from, float to)
        {
            int cleared = 0;
            // Solo cuenta lo despejado en esta pelea.
            for (int i = 0; i < Items.Length; i++)
                Items[i].Despejado = false;

            for (int i = 0; i < Items.Length; i++)
            {
                var item = Items[i];
                float d = item.Entry.distance;
                if (item.Consumed || d < from || d > to)
                    continue;
                var kind = item.Entry.kind;
                if (kind == LevelEntryKind.Obstacle || kind == LevelEntryKind.Ramp ||
                    kind == LevelEntryKind.Barrel || kind == LevelEntryKind.ZombieFront)
                {
                    item.Consumed = true;
                    item.Despejado = kind != LevelEntryKind.ZombieFront;
                    cleared++;
                }
            }
            return cleared;
        }

        /// <summary>
        /// Resuelve el tramo recorrido entre dos posiciones. Funciona igual hacia
        /// adelante que en retroceso, que es justamente el punto: volver por un
        /// bidon que dejaste pasar tiene que agarrarlo.
        /// </summary>
        public RunEvent Collect(RunSimulation sim, float fromX, float toX)
        {
            if (Items.Length == 0)
                return RunEvent.None;

            float min = Mathf.Min(fromX, toX);
            float max = Mathf.Max(fromX, toX);
            var cfg = sim.Config;
            // Lo que ocupo la moto en este cuadro, de la cola en el punto de partida a la punta
            // en el de llegada: lo que toque eso se encontro.
            float ocupaDesde = min - cfg.motoCola;
            float ocupaHasta = max + cfg.motoNariz;
            float lane = sim.State.LaneVisual;
            bool forward = toX > fromX;
            var events = RunEvent.None;

            for (int i = 0; i < Items.Length; i++)
            {
                var item = Items[i];
                float d = item.Entry.distance;

                if (item.Consumed)
                    continue;

                // La rampa sigue siendo un punto bajo las ruedas (el salto esta calibrado asi);
                // lo demas se toca cuando la moto y su largo se pisan.
                // Sin cortar al primer item pasado el tramo: el tutorial mueve items hacia
                // adelante y el orden por distancia deja de valer. Son pocos: recorrerlos todos sale gratis.
                if (item.Entry.kind == LevelEntryKind.Ramp)
                {
                    if (d > max || d < min)
                        continue;
                }
                else
                {
                    float h = MedioLargo(item.Entry, cfg);
                    if (d - h > ocupaHasta || d + h < ocupaDesde)
                        continue;
                }
                // Los recolectables se agarran tambien desde el carril de al lado (imanCarriles); lo demas, solo en el suyo.
                if (Mathf.Abs(lane - item.Entry.lane) > ToleranciaDeCarril(item.Entry.kind, cfg))
                    continue;

                // Sin nafta en el tramo inicial de algunos niveles: el bidon todavia no existe.
                if (item.Entry.kind == LevelEntryKind.Fuel && BidonesBloqueados(sim.Config, sim.State))
                    continue;

                // El estado se consulta en cada entrada: una rampa lanza, y lo que sigue en
                // el mismo tramo ya se sobrevuela.
                if (item.Entry.kind == LevelEntryKind.Ramp)
                {
                    // Solo hacia adelante y desde el piso. No se gasta: se puede volver y saltar de nuevo.
                    if (forward && !sim.State.Airborne)
                        events |= sim.Launch();
                    continue;
                }

                // El barril no se toca al pasarlo: esta al costado y solo explota de un tiro.
                if (item.Entry.kind == LevelEntryKind.Barrel)
                    continue;

                if (item.Entry.kind == LevelEntryKind.ZombieFront)
                {
                    // Volando se le pasa por encima.
                    if (sim.State.Airborne)
                        continue;
                    item.Consumed = true;
                    events |= sim.RunOver(item.Entry.variant, d, item.Entry.lane);
                    continue;
                }

                // Lo del piso no se toca volando; lo del aire solo volando a su altura.
                bool aerial = item.Entry.height > 0f;
                if (aerial ? !sim.IsAtHeight(item.Entry.height) : sim.State.Airborne)
                    continue;

                item.Consumed = true;
                events |= Apply(sim, item.Entry.kind);
            }

            return events;
        }

        public static bool EsRecurso(LevelEntryKind kind) =>
            kind == LevelEntryKind.Fuel || kind == LevelEntryKind.Battery || kind == LevelEntryKind.Ammo;

        /// <summary>Diferencia de carril con la que todavia hay encuentro: la de siempre, o mas amplia en los recolectables.</summary>
        public static float ToleranciaDeCarril(LevelEntryKind kind, GameConfig cfg) =>
            EsRecurso(kind) ? Mathf.Max(LaneTolerance, cfg.imanCarriles + 0.01f) : LaneTolerance;

        /// <summary>Lo que ocupa a lo largo un item, desde su centro: el suyo o el de su tipo.</summary>
        public static float MedioLargo(LevelEntry e, GameConfig cfg)
        {
            if (e.medioLargo > 0f)
                return e.medioLargo;
            switch (e.kind)
            {
                case LevelEntryKind.Obstacle: return cfg.medioLargoObstaculo;
                case LevelEntryKind.ZombieFront: return cfg.medioLargoZombie;
                case LevelEntryKind.Fuel:
                case LevelEntryKind.Battery:
                case LevelEntryKind.Ammo: return cfg.medioLargoRecurso;
                default: return 0f;
            }
        }

        static RunEvent Apply(RunSimulation sim, LevelEntryKind kind)
        {
            switch (kind)
            {
                case LevelEntryKind.Fuel:
                    return sim.ApplyPickup(PickupKind.Fuel);
                case LevelEntryKind.Battery:
                    return sim.ApplyPickup(PickupKind.Battery);
                case LevelEntryKind.Ammo:
                    return sim.ApplyPickup(PickupKind.Ammo);
                default:
                    return sim.ApplyCrash();
            }
        }
        // --- IBarrelField: los barriles son del recorrido, la explosion es de la horda ------

        public bool TryNearestBarrel(float fromX, int lane, float range, out float x, out int index)
        {
            x = 0f;
            index = -1;
            for (int i = 0; i < Items.Length; i++)
            {
                var item = Items[i];
                if (item.Consumed || item.Entry.kind != LevelEntryKind.Barrel)
                    continue;
                if (item.Entry.lane != lane)
                    continue;

                float d = item.Entry.distance;
                if (d > fromX || d < fromX - range)
                    continue;
                if (index < 0 || d > x)
                {
                    x = d;
                    index = i;
                }
            }
            return index >= 0;
        }

        public void Detonate(int index, RunSimulation sim) => Detonate(index, sim, 0);

        void Detonate(int index, RunSimulation sim, int depth)
        {
            if (index < 0 || index >= Items.Length || depth > 8)
                return;

            var item = Items[index];
            if (item.Consumed || item.Entry.kind != LevelEntryKind.Barrel)
                return;

            item.Consumed = true;
            float x = item.Entry.distance;
            sim.Explode(x, item.Entry.lane);

            // Un barril prende a los que tenga cerca: la cadena es la mejor bala del juego.
            float radius = sim.Config.explosionRadius;
            for (int i = 0; i < Items.Length; i++)
            {
                var other = Items[i];
                if (other.Consumed || other.Entry.kind != LevelEntryKind.Barrel)
                    continue;
                if (Mathf.Abs(other.Entry.distance - x) <= radius)
                    Detonate(i, sim, depth + 1);
            }
        }
    }
}
