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

        // --- Movimiento libre ---------------------------------------------------

        [Test]
        public void SheWalksFreelyBetweenLanes_AndNotOutOfThem()
        {
            var sim = SinNafta(MakeConfig());
            UnoSolo(sim, 50f);
            Assert.AreEqual(1f, sim.State.PeatonY, 0.001f, "se baja en el carril de la moto");

            Avanzar(sim, 0.2f, new PlayerIntent { MoveY = 1f });
            Assert.AreEqual(1.6f, sim.State.PeatonY, 0.02f, "3 carriles por segundo");
            Assert.AreEqual(sim.State.PeatonY, sim.State.LaneVisual, 0.0001f, "la vista la sigue sin escalones");
            Assert.AreEqual(2, sim.State.Lane, "Lane es el carril mas cercano");

            Avanzar(sim, 5f, new PlayerIntent { MoveY = 1f });
            Assert.AreEqual(2f, sim.State.PeatonY, 0.001f, "el carril de arriba es el borde");
            Avanzar(sim, 5f, new PlayerIntent { MoveY = -1f });
            Assert.AreEqual(0f, sim.State.PeatonY, 0.001f, "el de abajo es el otro borde");
        }

        [Test]
        public void TheStickIsAnalog_AndADiagonalMovesBothAxes()
        {
            var sim = SinNafta(MakeConfig());
            UnoSolo(sim, 50f);
            float x0 = sim.State.PeatonX;

            Avanzar(sim, 0.4f, new PlayerIntent { MoveX = 0.5f, MoveY = -0.5f });

            Assert.AreEqual(x0 + 1.2f, sim.State.PeatonX, 0.1f, "la mitad de 6 m/s");
            Assert.AreEqual(1f - 0.6f, sim.State.PeatonY, 0.03f, "la mitad de 3 carriles/s");
        }

        [Test]
        public void WithFreeMovement_TheLaneButtonsDoNotJumpALane()
        {
            var sim = SinNafta(MakeConfig());
            UnoSolo(sim, 50f);

            // Los mismos botones de arriba/abajo cuentan como carril en la moto: a pie no deben saltar.
            sim.Tick(new PlayerIntent { LaneDelta = 1, MovimientoLibre = true }, Dt);

            Assert.AreEqual(1f, sim.State.PeatonY, 0.001f);
        }

        [Test]
        public void WithoutFreeMovement_TheLaneButtonsStillJumpALane()
        {
            var sim = SinNafta(MakeConfig());
            UnoSolo(sim, 50f);

            sim.Tick(new PlayerIntent { LaneDelta = 1 }, Dt);

            Assert.AreEqual(2f, sim.State.PeatonY, 0.001f);
            Assert.AreEqual(2, sim.State.Lane);
        }

        [Test]
        public void APunch_ReachesAZombieInTheNextLane_WhenSheIsBetweenThem()
        {
            var sim = SinNafta(MakeConfig());
            var z = UnoSolo(sim, 1f);
            z.Lane = 0;

            sim.State.PeatonY = 1f;      // carril entero de diferencia: no llega
            var ev = sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            Assert.IsTrue((ev & RunEvent.GolpeAlAire) != 0, "un carril entero de diferencia");

            sim.State.PeatonY = 0.5f;    // entre los dos carriles: llega
            Avanzar(sim, 1f, Quieta);    // se enfria el golpe
            z.Lane = 0; z.X = sim.State.PeatonX + 1f; z.Stagger = 0f; z.Alive = true; z.Preparando = -1f; z.Enfriamiento = 5f;
            ev = sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            Assert.IsTrue((ev & RunEvent.Golpe) != 0, "a medio carril");
        }

        [Test]
        public void AZombieLine_ClosesInOnHerLane_ButStopsWhenAlignedBetweenLanes()
        {
            var sim = SinNafta(MakeConfig());
            var z = UnoSolo(sim, 3f);
            z.Lane = 2;
            z.CambioCarril = 0f;
            sim.State.PeatonY = 0.5f;

            Avanzar(sim, 4f, Quieta);

            Assert.LessOrEqual(Mathf.Abs(z.Lane - 0.5f), 0.6f, "se alineo con ella en vez de ir a un carril entero distinto");
        }

        // --- Que no se encimen ---------------------------------------------------

        static void AssertSeparados(RunSimulation sim, float sep, string cuando)
        {
            var units = sim.Horde.Units;
            for (int i = 0; i < units.Length; i++)
                for (int j = i + 1; j < units.Length; j++)
                {
                    if (!sim.Pelea.Activo(i) || !sim.Pelea.Activo(j) || !units[i].Alive || !units[j].Alive ||
                        units[i].Lane != units[j].Lane)
                        continue;
                    Assert.GreaterOrEqual(Mathf.Abs(units[i].X - units[j].X), sep - 0.001f,
                        cuando + ": los zombies " + i + " y " + j + " estan encimados en el carril " + units[i].Lane);
                }
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(11)]
        [TestCase(42)]
        [TestCase(2026)]
        public void TheZombiesThatStartTheFight_DoNotLandOnEachOther(int seed)
        {
            var c = MakeConfig();
            c.hordeSeed = seed;
            c.zombiesEnArena = 8;
            var sim = SinNafta(c);

            AssertSeparados(sim, c.zombieSeparacion, "al empezar");
        }

        [Test]
        public void ZombiesPiledOnTheSameSpot_AreSeparatedIntoALine()
        {
            var c = MakeConfig();
            var sim = SinNafta(c);
            var units = sim.Horde.Units;
            float px = sim.State.PeatonX;
            int n = 0;
            for (int i = 0; i < units.Length; i++)
            {
                if (!sim.Pelea.Activo(i))
                    continue;
                units[i].Alive = true;
                units[i].Lane = 1;
                units[i].X = px + 6f + 0.05f * n++;
                units[i].Stagger = 0f;
                units[i].Preparando = -1f;
            }

            sim.Tick(Quieta, Dt);
            AssertSeparados(sim, c.zombieSeparacion, "apenas amontonados");

            for (int t = 0; t < 120 && sim.State.Phase == RunPhase.Pelea; t++)
            {
                sim.Tick(Quieta, Dt);
                AssertSeparados(sim, c.zombieSeparacion, "caminando hacia ella");
            }
        }

        [Test]
        public void ThePunchThatPushesOneZombieOverAnother_DoesNotStackThem()
        {
            var c = MakeConfig();
            var sim = SinNafta(c);
            var units = sim.Horde.Units;
            float px = sim.State.PeatonX;
            Zombineta.Enemies.ZombieUnit golpeado = null, detras = null;
            for (int i = 0; i < units.Length; i++)
            {
                if (!sim.Pelea.Activo(i))
                    continue;
                var u = units[i];
                if (golpeado == null)
                {
                    golpeado = u;
                    u.Alive = true; u.Lane = sim.State.Lane; u.X = px + 0.8f; u.Golpes = 3;
                    u.Stagger = 0f; u.Preparando = -1f; u.Enfriamiento = 5f;
                }
                else if (detras == null)
                {
                    detras = u;
                    u.Alive = true; u.Lane = sim.State.Lane; u.X = px + 3f; u.Golpes = 3;
                    u.Stagger = 0f; u.Preparando = -1f; u.Enfriamiento = 5f;
                }
                else
                {
                    u.Alive = false;
                    u.CorpseLeft = 999f;
                }
            }

            sim.Tick(new PlayerIntent { Golpe = true }, Dt);

            Assert.GreaterOrEqual(Mathf.Abs(golpeado.X - detras.X), c.zombieSeparacion - 0.001f);
            Assert.Greater(golpeado.X, detras.X, "el golpeado queda detras del otro, no adentro");
        }

        [Test]
        public void ATurnedOffSeparation_LeavesThemAlone()
        {
            var c = MakeConfig();
            c.zombieSeparacion = 0f;
            var sim = SinNafta(c);
            var units = sim.Horde.Units;
            for (int i = 0; i < units.Length; i++)
                if (sim.Pelea.Activo(i))
                {
                    units[i].Alive = true;
                    units[i].Lane = 0;
                    units[i].X = sim.State.PeatonX + 6f;
                }
            sim.Tick(Quieta, Dt);
            Assert.AreEqual(units[0].X, units[1].X, 0.2f);
        }
    }
}
