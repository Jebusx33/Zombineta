using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Player;

namespace Zombineta.Tests
{
    /// <summary>
    /// La pelea a pie mientras llega el auxilio: cuando arranca, que hace la pina, que hacen los
    /// zombies y como vuelve todo al runner. Config propia con numeros redondos.
    /// </summary>
    public class PeleaTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig(int ayudas = 2)
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 2000f;
            c.startingGap = 40f;
            c.normalSpeed = 10f;
            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 1f;
            c.hordeBaseSpeed = 11f;
            c.hordeCount = 12;
            c.hordeSeed = 5;
            c.rubberBandMaxBonus = 0f;
            c.ammoMax = 6;
            c.ammoAtStart = 3;
            c.shotRangeMeters = 30f;

            c.ayudas = ayudas;
            c.peleaSegundos = 25f;
            c.vidaPelea = 5;
            c.gapTrasAuxilio = 45f;
            c.arenaMedioAncho = 12f;
            c.peatonVelocidad = 6f;
            c.golpeAlcance = 1.6f;
            c.golpeEnfriamiento = 0.35f;
            c.golpeEmpuje = 2.5f;
            c.golpeFreno = 0.5f;
            c.zombiesEnArena = 6;
            c.zombieVelocidadPelea = 2f;
            c.zombieAlcance = 1.1f;
            c.zombiePreparacion = 0.5f;
            c.zombieEnfriamiento = 1f;
            c.invulnerableTrasGolpe = 0.8f;
            c.furgonetaVelocidad = 28f;
            c.furgonetaDesde = 24f;
            return c;
        }

        static PlayerIntent Quieta => PlayerIntent.Idle;

        static RunSimulation SinNafta(GameConfig c)
        {
            var sim = new RunSimulation(c);
            sim.State.PlayerX = 500f;
            sim.Horde.Reset(460f);
            sim.State.Fuel = 0f;
            sim.Tick(Quieta, Dt);
            return sim;
        }

        static void Avanzar(RunSimulation sim, float segundos, PlayerIntent intent)
        {
            int pasos = Mathf.RoundToInt(segundos / Dt);
            for (int i = 0; i < pasos && sim.State.Phase == RunPhase.Pelea; i++)
                sim.Tick(intent, Dt);
        }

        /// <summary>Deja un solo zombie peleando, pegado a ella, y saca a los demas de la arena.</summary>
        static Zombineta.Enemies.ZombieUnit UnoSolo(RunSimulation sim, float distancia)
        {
            var units = sim.Horde.Units;
            Zombineta.Enemies.ZombieUnit elegido = null;
            for (int i = 0; i < units.Length; i++)
            {
                if (!sim.Pelea.Activo(i))
                    continue;
                if (elegido == null)
                {
                    elegido = units[i];
                    elegido.Alive = true;
                    elegido.X = sim.State.PeatonX + distancia;
                    elegido.Lane = sim.State.Lane;
                    elegido.Stagger = 0f;
                    elegido.Preparando = -1f;
                    elegido.Enfriamiento = 5f;
                }
                else
                {
                    units[i].Alive = false;
                    units[i].CorpseLeft = 999f;
                }
            }
            return elegido;
        }

        // --- Entrada ----------------------------------------------------------

        [Test]
        public void OutOfFuel_WithHelpLeft_StartsTheFight_InsteadOfLosing()
        {
            var sim = SinNafta(MakeConfig(ayudas: 2));

            Assert.AreEqual(RunPhase.Pelea, sim.State.Phase);
            Assert.AreEqual(1, sim.State.Ayudas, "se gasto una ayuda");
            Assert.AreEqual(5, sim.State.Vida);
            Assert.AreEqual(500f, sim.State.PeatonX, 0.001f, "se baja donde quedo la moto");
        }

        [Test]
        public void OutOfFuel_WithoutHelp_IsTheSameDefeatAsAlways()
        {
            var sim = SinNafta(MakeConfig(ayudas: 0));
            Avanzar(sim, 0f, Quieta);

            Assert.AreNotEqual(RunPhase.Pelea, sim.State.Phase);
            // Sin auxilio la moto queda parada y la horda la alcanza.
            for (int i = 0; i < 600 && sim.State.Phase == RunPhase.Running; i++)
                sim.Tick(Quieta, Dt);
            Assert.AreEqual(RunPhase.Lost, sim.State.Phase);
            Assert.AreEqual(LossReason.OutOfFuel, sim.State.Loss);
        }

        // --- A pie ------------------------------------------------------------

        [Test]
        public void SheWalks_AndTurns_ButNotOutOfTheArena()
        {
            var sim = SinNafta(MakeConfig());
            UnoSolo(sim, 50f);

            Avanzar(sim, 1f, new PlayerIntent { MoveX = -1f });
            Assert.AreEqual(-1, sim.State.Mira);
            Assert.AreEqual(494f, sim.State.PeatonX, 0.1f, "6 m/s hacia la izquierda");

            Avanzar(sim, 10f, new PlayerIntent { MoveX = -1f });
            Assert.AreEqual(488f, sim.State.PeatonX, 0.01f, "el borde de la arena: centro - 12");
        }

        [Test]
        public void APunch_PushesAndStaggers_AndEnoughPunchesKill()
        {
            var sim = SinNafta(MakeConfig());
            var z = UnoSolo(sim, 1f);
            float antes = z.X;

            sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            Assert.IsTrue(z.Alive);
            Assert.AreEqual(antes + 2.5f, z.X, 0.01f, "lo empuja hacia donde mira");
            Assert.Greater(z.Stagger, 0f);

            for (int golpe = 0; golpe < 2; golpe++)
            {
                z.X = sim.State.PeatonX + 1f;
                Avanzar(sim, 0.4f, Quieta); // espera el enfriamiento
                sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            }
            Assert.IsFalse(z.Alive, "un comun aguanta 3 pinas");
            Assert.AreEqual(Zombineta.Enemies.DeathCause.Golpe, z.Cause);
        }

        [Test]
        public void APunch_ToTheOtherSide_Misses()
        {
            var sim = SinNafta(MakeConfig());
            UnoSolo(sim, -1f); // atras de ella

            var ev = sim.Tick(new PlayerIntent { Golpe = true }, Dt);

            Assert.IsTrue((ev & RunEvent.GolpeAlAire) != 0);
        }

        [Test]
        public void TheGun_ShootsWhereSheLooks_AndSpendsAmmo()
        {
            var sim = SinNafta(MakeConfig());
            var z = UnoSolo(sim, 8f);
            int balas = sim.State.Ammo;

            var ev = sim.Tick(new PlayerIntent { Fire = true }, Dt);

            Assert.IsTrue((ev & RunEvent.Shot) != 0);
            Assert.AreEqual(balas - 1, sim.State.Ammo);
            Assert.IsTrue(!z.Alive || z.Stagger > 0f, "le pego al de adelante");
        }

        // --- Los zombies ------------------------------------------------------

        [Test]
        public void AZombie_WindsUpBeforeHitting_AndTheHitCostsLife()
        {
            var sim = SinNafta(MakeConfig());
            var z = UnoSolo(sim, 0.8f);
            z.Enfriamiento = 0f;

            Avanzar(sim, 0.3f, Quieta);
            Assert.AreEqual(5, sim.State.Vida, "todavia se esta preparando");
            Assert.GreaterOrEqual(z.Preparando, 0f);

            Avanzar(sim, 0.3f, Quieta);
            Assert.AreEqual(4, sim.State.Vida);
            Assert.Greater(sim.State.Invulnerable, 0f);
        }

        [Test]
        public void PunchingDuringTheWindUp_CancelsTheHit()
        {
            var sim = SinNafta(MakeConfig());
            var z = UnoSolo(sim, 0.8f);
            z.Enfriamiento = 0f;

            Avanzar(sim, 0.2f, Quieta);
            sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            Avanzar(sim, 0.5f, Quieta);

            Assert.AreEqual(5, sim.State.Vida);
        }

        [Test]
        public void WithoutLife_SheLoses()
        {
            var sim = SinNafta(MakeConfig());
            var z = UnoSolo(sim, 0.8f);
            sim.State.Vida = 1;
            z.Enfriamiento = 0f;

            Avanzar(sim, 1f, Quieta);

            Assert.AreEqual(RunPhase.Lost, sim.State.Phase);
            Assert.AreEqual(LossReason.Derribada, sim.State.Loss);
        }

        // --- El auxilio -------------------------------------------------------

        [Test]
        public void WhenTimeIsUp_TheVanRunsThemOver_FillsTheTank_AndTheRunGoesOn()
        {
            var sim = SinNafta(MakeConfig());
            var z = UnoSolo(sim, 4f);
            z.Enfriamiento = 999f;
            sim.State.PeleaRestante = 0.05f;

            Avanzar(sim, 0.1f, Quieta);
            Assert.IsTrue(sim.State.FurgonetaEnCamino);

            // Hasta que la furgoneta pasa por donde estaba el zombie.
            float donde = z.X;
            for (int i = 0; i < 600 && sim.State.FurgonetaX < donde + 3f; i++)
                sim.Tick(Quieta, Dt);
            Assert.IsFalse(z.Alive, "la furgoneta lo arrollo");
            Assert.AreEqual(Zombineta.Enemies.DeathCause.RunOver, z.Cause);

            Avanzar(sim, 3f, Quieta);
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);
            Assert.AreEqual(100f, sim.State.Fuel, 0.001f, "tanque lleno");
            Assert.AreEqual(45f, sim.State.Gap, 0.5f, "la horda queda atras");
            Assert.AreEqual(sim.State.PeatonX, sim.State.PlayerX, 0.001f, "sigue desde donde estaba a pie");
        }

        [Test]
        public void AfterUsingAllTheHelp_TheNextEmptyTankIsADefeat()
        {
            var c = MakeConfig(ayudas: 1);
            var sim = SinNafta(c);
            sim.State.PeleaRestante = 0f;
            Avanzar(sim, 5f, Quieta);
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);

            sim.State.Fuel = 0f;
            sim.Tick(Quieta, Dt);

            Assert.AreEqual(RunPhase.Running, sim.State.Phase, "sin ayudas no hay pelea");
            Assert.AreEqual(0, sim.State.Ayudas);
        }
    }
}
