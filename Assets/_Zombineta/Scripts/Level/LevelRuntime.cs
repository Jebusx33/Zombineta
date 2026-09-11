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
    public sealed class LevelRuntime
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
    }
}
