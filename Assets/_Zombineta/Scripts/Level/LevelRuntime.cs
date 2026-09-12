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

        public void Reset()
        {
            for (int i = 0; i < Items.Length; i++)
                Items[i].Consumed = false;
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
            float lane = sim.State.LaneVisual;
            bool forward = toX > fromX;
            var events = RunEvent.None;

            for (int i = 0; i < Items.Length; i++)
            {
                var item = Items[i];
                float d = item.Entry.distance;

                // Ordenadas por distancia: pasado el tramo, no hay mas candidatas.
                if (d > max)
                    break;
                if (d < min || item.Consumed)
                    continue;
                if (Mathf.Abs(lane - item.Entry.lane) > LaneTolerance)
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
