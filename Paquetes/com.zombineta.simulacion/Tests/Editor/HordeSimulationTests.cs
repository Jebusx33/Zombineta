using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;

namespace Zombineta.Tests
{
    /// <summary>
    /// La masa con numeros propios del test. El roster tiene tres tipos claros: uno normal,
    /// uno rapido y uno lento, para que las cuentas se lean de un vistazo.
    /// </summary>
    public class HordeSimulationTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.startingGap = 50f;
            c.hordeBaseSpeed = 10f;
            c.hordeCount = 12;
            c.hordeDepthMeters = 10f;
            c.corpseSeconds = 1f;
            c.hordeSeed = 7;
            c.shotRangeMeters = 60f;
            c.deathScareRadius = 4f;
            c.deathScareSeconds = 0.5f;
            c.explosionRadius = 8f;
            c.explosionScareRadius = 16f;
            c.explosionScareSeconds = 1f;
            return c;
        }

        static ZombieRoster MakeRoster()
        {
            var r = ScriptableObject.CreateInstance<ZombieRoster>();
            r.types.Add(new ZombieType { name = "Comun", speedMultiplier = 1f, deathChance = 1f, spawnWeight = 1f });
            r.types.Add(new ZombieType { name = "Corredor", speedMultiplier = 2f, deathChance = 1f, spawnWeight = 1f });
            r.types.Add(new ZombieType { name = "Pesado", speedMultiplier = 0.5f, deathChance = 0f, staggerOnHit = 0.4f, spawnWeight = 1f });
            return r;
        }

        static HordeSimulation Make(out GameConfig cfg)
        {
            cfg = MakeConfig();
            var h = new HordeSimulation(cfg, MakeRoster(), cfg.hordeSeed);
            h.Reset(-cfg.startingGap);
            return h;
        }

        /// <summary>Deja un solo zombie vivo, del tipo pedido, en la posicion y carril pedidos.</summary>
        static ZombieUnit Solo(HordeSimulation h, int type, float x, int lane)
        {
            for (int i = 1; i < h.Units.Length; i++)
                h.Kill(h.Units[i], DeathCause.Bullet);
            var u = h.Units[0];
            u.Type = type; u.X = x; u.Lane = lane; u.Alive = true; u.Stagger = 0f; u.CorpseLeft = 0f;
            h.RecomputeFront();
            return u;
        }

        [Test]
        public void Reset_PutsTheFrontExactlyAtTheStartingGap()
        {
            var h = Make(out var cfg);

            Assert.AreEqual(-cfg.startingGap, h.FrontX, 0.001f);
            Assert.AreEqual(cfg.hordeCount, h.Units.Length);
        }

        [Test]
        public void Front_IsTheFrontmostAliveZombie()
        {
            var h = Make(out _);
            var u = Solo(h, 0, -20f, 1);
            h.Step(Dt, 1f);

            Assert.AreEqual(u.X, h.FrontX, 0.001f, "el unico vivo manda");
        }

        [Test]
        public void EachType_MovesAtItsOwnSpeed()
        {
            var h = Make(out var cfg);
            var runner = Solo(h, 1, -20f, 1);   // x2
            float x0 = runner.X;

            for (int i = 0; i < 60; i++) h.Step(Dt, 1f);

            // 10 m/s base x 2 x 1 s
            Assert.AreEqual(x0 + 20f, runner.X, 0.2f);
        }

        [Test]
        public void SpeedFactor_SlowsEveryone()
        {
            var h = Make(out _);
            var u = Solo(h, 0, -20f, 1);
            float x0 = u.X;

            for (int i = 0; i < 60; i++) h.Step(Dt, 0.5f);

            Assert.AreEqual(x0 + 5f, u.X, 0.2f, "10 m/s a la mitad durante 1 s");
        }

        [Test]
        public void AStaggeredZombie_DoesNotAdvance()
        {
            var h = Make(out _);
            var u = Solo(h, 0, -20f, 1);
            u.Stagger = 0.5f;
            float x0 = u.X;

            for (int i = 0; i < 15; i++) h.Step(Dt, 1f);   // 0,25 s

            Assert.AreEqual(x0, u.X, 0.001f);
        }

        [Test]
        public void ACorpse_ComesBackBehindTheFrontAfterItsTime()
        {
            var h = Make(out var cfg);
            var front = Solo(h, 0, -20f, 1);
            var dead = h.Units[1];
            dead.Alive = true; dead.X = -22f; dead.Lane = 0;
            h.Kill(dead, DeathCause.Bullet);
            int gen = dead.Generation;

            Assert.IsFalse(dead.Alive);
            for (int i = 0; i < 90; i++) h.Step(Dt, 1f);   // 1,5 s > corpseSeconds

            Assert.IsTrue(dead.Alive, "la horda es infinita: vuelve a entrar");
            Assert.AreNotEqual(gen, dead.Generation, "es otro zombie, no el mismo");
            Assert.Less(dead.X, front.X, "reaparece detras del frente");
            Assert.GreaterOrEqual(dead.X, front.X - cfg.hordeDepthMeters * 2f);
        }

        [Test]
        public void AZombieLeftFarBehind_IsRecycled()
        {
            var h = Make(out var cfg);
            var front = Solo(h, 0, 0f, 1);
            var straggler = h.Units[1];
            straggler.Alive = true; straggler.X = -200f; straggler.Lane = 2; straggler.CorpseLeft = 0f;

            h.Step(Dt, 1f);

            Assert.Greater(straggler.X, front.X - cfg.hordeDepthMeters * 3f, "lo reciclaron al fondo de la masa");
        }

        [Test]
        public void Population_StaysConstantAfterManyDeaths()
        {
            var h = Make(out _);
            for (int round = 0; round < 5; round++)
            {
                foreach (var u in h.Units)
                    if (u.Alive) h.Kill(u, DeathCause.Bullet);
                for (int i = 0; i < 120; i++) h.Step(Dt, 1f);
            }

            int alive = 0;
            foreach (var u in h.Units) if (u.Alive) alive++;
            Assert.AreEqual(h.Units.Length, alive, "todos vuelven: la horda no se vacia");
        }
    }
}
