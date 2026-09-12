using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Enemies
{
    public enum DeathCause { None, Bullet, Explosion, RunOver }

    public enum HordeEventKind { Tracer, Impact, Death, Explosion, Miss }

    /// <summary>Algo que paso en la horda este tick. Las vistas leen esto para VFX y SFX.</summary>
    public struct HordeEvent
    {
        public HordeEventKind Kind;
        public float FromX;   // metros: de donde salio el disparo (solo Tracer)
        public float X;       // metros
        public int Lane;
        public int Type;
        public DeathCause Cause;
    }

    /// <summary>Un zombie de la masa. Se recicla: nunca se crea ni se destruye en partida.</summary>
    public sealed class ZombieUnit
    {
        public int Type;
        public float X;
        public int Lane;

        /// <summary>Segundos que queda frenado (impacto que aguanto, o susto).</summary>
        public float Stagger;

        public bool Alive;

        /// <summary>Segundos que le quedan al cadaver antes de volver a entrar.</summary>
        public float CorpseLeft;

        public DeathCause Cause;

        /// <summary>Cambia al reciclarse: la vista sabe que es otro zombie y reinicia su animacion.</summary>
        public int Generation;
    }

    /// <summary>
    /// La horda como individuos. C# plano: se instancia, se le hace Step y se verifica, sin
    /// abrir una escena. El frente (lo que te alcanza) es el vivo mas adelantado, asi que matar
    /// al puntero abre distancia sin ningun empujon artificial.
    /// </summary>
    public sealed class HordeSimulation
    {
        readonly GameConfig config;
        readonly ZombieRoster roster;
        readonly System.Random rng;
        readonly List<HordeEvent> events = new List<HordeEvent>();

        float frontX;

        public HordeSimulation(GameConfig config, ZombieRoster roster, int seed)
        {
            this.config = config;
            this.roster = roster;
            rng = new System.Random(seed);

            int count = Mathf.Max(1, config.hordeCount);
            Units = new ZombieUnit[count];
            for (int i = 0; i < count; i++)
                Units[i] = new ZombieUnit();
        }

        public ZombieUnit[] Units { get; }

        /// <summary>Lo que paso en el ultimo Step. Se limpia al empezar cada uno.</summary>
        public IReadOnlyList<HordeEvent> Events => events;

        /// <summary>Sube en cada Step: la vista sabe si los eventos son nuevos o los de antes.</summary>
        public int Epoch { get; private set; }

        /// <summary>X del vivo mas adelantado: el que te puede alcanzar.</summary>
        public float FrontX => frontX;

        /// <summary>Velocidad efectiva del frente en el ultimo Step (m/s). Para el HUD de debug.</summary>
        public float FrontSpeed { get; private set; }

        public ZombieType Type(int index) =>
            roster != null ? roster.Get(index) : ZombieRoster.Default;

        public void Reset(float front)
        {
            frontX = front;
            FrontSpeed = 0f;
            events.Clear();

            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];
                u.Alive = true;
                u.Stagger = 0f;
                u.CorpseLeft = 0f;
                u.Cause = DeathCause.None;
                u.Type = Pick();
                u.Lane = rng.Next(RunSimulation.LaneCount);
                // El primero justo en el frente: la ventaja inicial es exactamente startingGap.
                u.X = i == 0 ? front : front - (float)rng.NextDouble() * config.hordeDepthMeters;
                u.Generation++;
            }
        }

        /// <summary>
        /// Arranca un tick: limpia lo que paso en el anterior. Va antes que nada, porque el
        /// disparo se resuelve al principio del tick y sus eventos tienen que sobrevivir.
        /// </summary>
        public void BeginTick()
        {
            events.Clear();
            Epoch++;
        }

        public void Step(float dt, float speedFactor)
        {
            float previousFront = frontX;
            float front = float.NegativeInfinity;
            float frontSpeed = 0f;

            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];

                if (!u.Alive)
                {
                    u.CorpseLeft -= dt;
                    if (u.CorpseLeft > 0f)
                        continue;
                    Recycle(u, previousFront);
                }
                else if (u.X < previousFront - config.hordeDepthMeters * 2f)
                {
                    // Se descolgo de la masa: vuelve a entrar por el fondo.
                    Recycle(u, previousFront);
                }

                float speed = 0f;
                if (u.Stagger > 0f)
                    u.Stagger = Mathf.Max(0f, u.Stagger - dt);
                else
                {
                    speed = config.hordeBaseSpeed * Type(u.Type).speedMultiplier * speedFactor;
                    u.X += speed * dt;
                }

                if (u.X > front)
                {
                    front = u.X;
                    frontSpeed = speed;
                }
            }

            if (front > float.NegativeInfinity)
                frontX = front;
            FrontSpeed = frontSpeed;
        }

        // --- Acciones --------------------------------------------------------

        /// <summary>El vivo mas cercano hacia atras en ese carril, o null si no hay nadie.</summary>
        public ZombieUnit NearestBehind(float fromX, int lane, float range)
        {
            ZombieUnit best = null;
            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];
                if (!u.Alive || u.Lane != lane)
                    continue;
                if (u.X > fromX || u.X < fromX - range)
                    continue;
                if (best == null || u.X > best.X)
                    best = u;
            }
            return best;
        }

        /// <summary>
        /// Le pega a un zombie: muere segun su tipo, o encaja el impacto y queda frenado.
        /// Devuelve true si murio.
        /// </summary>
        public bool HitZombie(ZombieUnit u, float fromX)
        {
            var t = Type(u.Type);
            events.Add(new HordeEvent
            {
                Kind = HordeEventKind.Tracer, FromX = fromX, X = u.X, Lane = u.Lane, Type = u.Type,
            });

            if ((float)rng.NextDouble() <= t.deathChance)
            {
                Kill(u, DeathCause.Bullet);
                return true;
            }

            events.Add(new HordeEvent
            {
                Kind = HordeEventKind.Impact, X = u.X, Lane = u.Lane, Type = u.Type,
            });
            u.Stagger = Mathf.Max(u.Stagger, t.staggerOnHit);
            return false;
        }

        /// <summary>La bala no encontro nada: traza hasta el final del alcance.</summary>
        public void Miss(float fromX, int lane, float range)
        {
            float to = fromX - range;
            events.Add(new HordeEvent { Kind = HordeEventKind.Tracer, FromX = fromX, X = to, Lane = lane });
            events.Add(new HordeEvent { Kind = HordeEventKind.Miss, X = to, Lane = lane });
        }

        /// <summary>Traza sin victima: la bala pego en otra cosa (un barril).</summary>
        public void Tracer(float fromX, float toX, int lane)
        {
            events.Add(new HordeEvent { Kind = HordeEventKind.Tracer, FromX = fromX, X = toX, Lane = lane });
        }

        public void Kill(ZombieUnit u, DeathCause cause)
        {
            if (!u.Alive)
                return;

            u.Alive = false;
            u.Cause = cause;
            u.CorpseLeft = config.corpseSeconds;
            u.Stagger = 0f;
            events.Add(new HordeEvent
            {
                Kind = HordeEventKind.Death, X = u.X, Lane = u.Lane, Type = u.Type, Cause = cause,
            });

            // Los de al lado se asustan: un buen tiro se siente aunque el frente se mueva poco.
            Scare(u.X, config.deathScareRadius, config.deathScareSeconds);
            RecomputeFront();
        }

        /// <summary>Explosion de un barril: mata en radio en los tres carriles. Devuelve cuantos.</summary>
        public int Explode(float x, int lane)
        {
            events.Add(new HordeEvent { Kind = HordeEventKind.Explosion, X = x, Lane = lane });

            int killed = 0;
            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];
                if (u.Alive && Mathf.Abs(u.X - x) <= config.explosionRadius)
                {
                    Kill(u, DeathCause.Explosion);
                    killed++;
                }
            }

            Scare(x, config.explosionScareRadius, config.explosionScareSeconds);
            return killed;
        }

        /// <summary>Frena un momento a los vivos que esten cerca.</summary>
        public void Scare(float x, float radius, float seconds)
        {
            if (seconds <= 0f)
                return;
            for (int i = 0; i < Units.Length; i++)
            {
                var u = Units[i];
                if (u.Alive && Mathf.Abs(u.X - x) <= radius)
                    u.Stagger = Mathf.Max(u.Stagger, seconds);
            }
        }

        /// <summary>
        /// Un zombie de frente arrollado por la moto. No es parte de la masa (vive en el
        /// recorrido): aca solo se registra el evento para que los efectos lo dibujen.
        /// </summary>
        public void ReportRunOver(float x, int lane, int type)
        {
            events.Add(new HordeEvent
            {
                Kind = HordeEventKind.Death, X = x, Lane = lane, Type = type, Cause = DeathCause.RunOver,
            });
        }

        // --- Interno ---------------------------------------------------------

        int Pick() => roster != null ? roster.Pick(rng) : 0;

        void Recycle(ZombieUnit u, float front)
        {
            u.Alive = true;
            u.Cause = DeathCause.None;
            u.CorpseLeft = 0f;
            u.Stagger = 0f;
            u.Type = Pick();
            u.Lane = rng.Next(RunSimulation.LaneCount);
            // Reaparece al fondo de la masa: matar compra espacio, no vacia la horda.
            u.X = front - config.hordeDepthMeters * (0.6f + 0.4f * (float)rng.NextDouble());
            u.Generation++;
        }

        /// <summary>Recalcula el frente. Publico para tests y para teletransportes de debug.</summary>
        public void RecomputeFront()
        {
            float front = float.NegativeInfinity;
            for (int i = 0; i < Units.Length; i++)
                if (Units[i].Alive && Units[i].X > front)
                    front = Units[i].X;

            // Si murieron todos, el frente queda donde estaba: los que vuelven entran detras.
            if (front > float.NegativeInfinity)
                frontX = front;
        }
    }
}
