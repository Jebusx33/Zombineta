using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Luz
{
    /// <summary>
    /// Destellos de gameplay (disparo, choque, atropello, explosion): cada evento prende una luz
    /// que arranca a maxima intensidad y se apaga sola. Cola de a lo sumo <c>capacity</c> destellos
    /// vivos: si se dispara uno de mas, se descarta el mas viejo para hacer lugar. Tick descuenta el
    /// tiempo primero y mide despues, asi Tick(0) justo despues de Trigger da la intensidad entera.
    /// </summary>
    public sealed class FlashDirector
    {
        struct Flash
        {
            public Vector2 at;
            public Color color;
            public float intensity;
            public float radius;
            public float seconds;
            public float remaining;
        }

        readonly FlashConfig config;
        readonly int capacity;
        readonly List<Flash> flashes;

        public FlashDirector(FlashConfig config, int capacity = 8)
        {
            this.config = config;
            this.capacity = Mathf.Max(1, capacity);
            flashes = new List<Flash>(this.capacity);
        }

        /// <summary>Cuantos destellos siguen vivos.</summary>
        public int Count => flashes.Count;

        public void Trigger(FlashKind kind, Vector2 at)
        {
            var e = config != null ? config.Get(kind) : null;
            if (e == null)
                return;

            // Cola llena: se descarta el mas viejo (el de indice 0) para hacer lugar al nuevo.
            if (flashes.Count >= capacity)
                flashes.RemoveAt(0);

            flashes.Add(new Flash
            {
                at = at,
                color = e.color,
                intensity = e.intensity,
                radius = e.radius,
                seconds = e.seconds,
                remaining = e.seconds,
            });
        }

        public void Trigger(RunEvent events, Vector2 at)
        {
            if ((events & RunEvent.Shot) != 0) Trigger(FlashKind.Shot, at);
            if ((events & RunEvent.Crashed) != 0) Trigger(FlashKind.Crash, at);
            if ((events & RunEvent.RanOver) != 0) Trigger(FlashKind.RanOver, at);
            if ((events & RunEvent.Explosion) != 0) Trigger(FlashKind.Explosion, at);
        }

        public void Tick(float dt)
        {
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                var f = flashes[i];
                f.remaining = Mathf.Max(0f, f.remaining - dt);
                if (f.remaining <= 0f)
                {
                    flashes.RemoveAt(i);
                    continue;
                }
                flashes[i] = f;
            }
        }

        /// <summary>Destello i-esimo en orden de creacion (0 = el mas viejo que sigue vivo).</summary>
        public bool TryGet(int i, out Vector2 at, out Color color, out float intensity, out float radius)
        {
            if (i < 0 || i >= flashes.Count)
            {
                at = default;
                color = default;
                intensity = 0f;
                radius = 0f;
                return false;
            }

            var f = flashes[i];
            at = f.at;
            color = f.color;
            intensity = f.intensity * f.remaining / f.seconds;
            radius = f.radius;
            return true;
        }
    }
}
