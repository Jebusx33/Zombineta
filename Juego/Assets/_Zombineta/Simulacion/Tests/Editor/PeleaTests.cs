using System.Collections.Generic;
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

            // Lo de la Fase 4 apagado: estos tests prueban la pelea de siempre. Los de la Fase 4 lo prenden.
            c.paradaSegundos = 0f;
            c.arranqueSegundos = 0f;
            c.vidaExtraPrimeraPelea = 0;
            c.reintentosPelea = 0;
            c.oleadas = new List<OleadaPelea>();
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

        [TestCase(1)]
        [TestCase(-1)]
        public void TheShot_GoesWhereSheLooks_AndTheEventsKnowTheDirection(int mira)
        {
            var sim = SinNafta(MakeConfig());
            var z = UnoSolo(sim, 8f * mira);
            sim.State.Mira = mira;

            sim.Tick(new PlayerIntent { Fire = true }, Dt);

            bool traza = false, conDireccion = false;
            foreach (var e in sim.Horde.Events)
            {
                if (e.Kind == Zombineta.Enemies.HordeEventKind.Tracer)
                {
                    traza = true;
                    Assert.AreEqual(mira, Mathf.Sign(e.X - e.FromX), "la traza va hacia donde mira");
                }
                if (e.Kind == Zombineta.Enemies.HordeEventKind.Death || e.Kind == Zombineta.Enemies.HordeEventKind.Impact)
                {
                    conDireccion = true;
                    Assert.AreEqual(mira, e.Dir, "el impacto sabe hacia donde iba la bala");
                }
            }
            Assert.IsTrue(traza, "hubo disparo");
            Assert.IsTrue(conDireccion, "le pego");
        }

        [Test]
        public void ATiltedShotThatMisses_AlsoGoesWhereSheLooks()
        {
            var sim = SinNafta(MakeConfig());
            UnoSolo(sim, 50f);       // fuera del alcance
            sim.State.Mira = -1;

            sim.Tick(new PlayerIntent { Fire = true }, Dt);

            foreach (var e in sim.Horde.Events)
                if (e.Kind == Zombineta.Enemies.HordeEventKind.Tracer)
                    Assert.Less(e.X, e.FromX, "la bala perdida sigue hacia la izquierda");
        }

        [Test]
        public void APunch_TellsWhichWayItPushed()
        {
            var sim = SinNafta(MakeConfig());
            UnoSolo(sim, 1f);

            sim.Tick(new PlayerIntent { Golpe = true }, Dt);

            bool visto = false;
            foreach (var e in sim.Horde.Events)
                if (e.Kind == Zombineta.Enemies.HordeEventKind.Impact)
                {
                    visto = true;
                    Assert.AreEqual(1, e.Dir);
                }
            Assert.IsTrue(visto);
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

        // --- Cadena de golpes y remate ---------------------------------------------

        /// <summary>Deja solo los zombies pedidos (dx respecto de ella y carril) y saca a los demas de la arena.</summary>
        static Zombineta.Enemies.ZombieUnit[] Poner(RunSimulation sim, params (float dx, int lane)[] lugares)
        {
            var puestos = new Zombineta.Enemies.ZombieUnit[lugares.Length];
            var units = sim.Horde.Units;
            int n = 0;
            for (int i = 0; i < units.Length; i++)
            {
                if (!sim.Pelea.Activo(i))
                    continue;
                var u = units[i];
                if (n < lugares.Length)
                {
                    u.Alive = true;
                    u.X = sim.State.PeatonX + lugares[n].dx;
                    u.Lane = lugares[n].lane;
                    u.Stagger = 0f;
                    u.Derribado = 0f;
                    u.Preparando = -1f;
                    u.Enfriamiento = 5f;
                    u.Golpes = 3;
                    puestos[n++] = u;
                }
                else
                {
                    u.Alive = false;
                    u.CorpseLeft = 999f;
                }
            }
            return puestos;
        }

        static RunEvent Golpe(RunSimulation sim, float esperaAntes)
        {
            Avanzar(sim, esperaAntes, Quieta);
            return sim.Tick(new PlayerIntent { Golpe = true }, Dt);
        }

        static GameConfig Rapido()
        {
            var c = MakeConfig();
            c.golpeEnfriamiento = 0.2f;
            c.comboVentana = 0.6f;
            c.rematePausa = 0.55f;
            c.remateRadio = 2.4f;
            c.remateEmpuje = 3.5f;
            c.remateDerribo = 1.4f;
            return c;
        }

        /// <summary>Quita lo que el paso del tiempo les haya cambiado a los zombies puestos, para probar el golpe solo.</summary>
        static void Reponer(Zombineta.Enemies.ZombieUnit[] z, RunSimulation sim, params float[] dx)
        {
            for (int i = 0; i < z.Length; i++)
            {
                z[i].X = sim.State.PeatonX + dx[i];
                z[i].Stagger = 0f;
                z[i].Preparando = -1f;
                z[i].Enfriamiento = 5f;
            }
        }

        [Test]
        public void ThreeQuickPunches_AreAChain_AndTheThirdIsTheFinisher()
        {
            var sim = SinNafta(Rapido());
            var z = Poner(sim, (1f, 1));

            var e1 = Golpe(sim, 0f);
            Assert.AreEqual(1, sim.State.ComboPaso);
            Assert.IsTrue((e1 & RunEvent.Remate) == 0);

            var e2 = Golpe(sim, 0.25f);   // el enfriamiento corto: 0,2 s
            Assert.AreEqual(2, sim.State.ComboPaso);
            Assert.IsTrue((e2 & RunEvent.Remate) == 0);

            Avanzar(sim, 0.25f, Quieta);
            Reponer(z, sim, 1f);          // el primer golpe lo empujo: lo vuelvo a poner a tiro
            var e3 = sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            Assert.AreEqual(3, sim.State.ComboPaso);
            Assert.IsTrue((e3 & RunEvent.Remate) != 0, "el tercero es el remate");
        }

        [Test]
        public void ThePunchesOfTheChain_AreQuickButNotInstant()
        {
            var sim = SinNafta(Rapido());
            Poner(sim, (50f, 1));

            sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            Avanzar(sim, 0.1f, Quieta);
            sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            Assert.AreEqual(1, sim.State.ComboPaso, "a los 0,1 s todavia esta enfriando");

            Avanzar(sim, 0.15f, Quieta);
            sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            Assert.AreEqual(2, sim.State.ComboPaso, "a los 0,25 s ya pega el segundo");
        }

        [Test]
        public void TheChain_StartsOverIfShePausesTooLong()
        {
            var sim = SinNafta(Rapido());
            Poner(sim, (1f, 1));

            Golpe(sim, 0f);
            Golpe(sim, 1.2f);   // se paso de la ventana (0,2 + 0,6 s)

            Assert.AreEqual(1, sim.State.ComboPaso, "vuelve al primer golpe");
        }

        [Test]
        public void AfterTheFinisher_ThereIsAPause_AndTheNextPunchStartsANewChain()
        {
            var sim = SinNafta(Rapido());
            Poner(sim, (50f, 1));
            Golpe(sim, 0f);
            Golpe(sim, 0.25f);
            Golpe(sim, 0.25f);
            Assert.AreEqual(3, sim.State.ComboPaso);

            var pegado = sim.Tick(new PlayerIntent { Golpe = true }, Dt);
            Assert.AreEqual(RunEvent.None, pegado & (RunEvent.Golpe | RunEvent.GolpeAlAire), "en la pausa no pega");

            var nueva = Golpe(sim, 0.6f);
            Assert.AreEqual(1, sim.State.ComboPaso, "otra cadena, desde el primero");
            Assert.IsTrue((nueva & RunEvent.Remate) == 0);
        }

        [Test]
        public void TheFinisher_KnocksDownEveryoneInFront_AndLeavesTheOnesBehindAlone()
        {
            var sim = SinNafta(Rapido());
            // Lejos mientras se dan los dos primeros golpes: asi ninguno los toca antes del remate.
            var z = Poner(sim, (25f, 1), (26f, 1), (-25f, 1), (27f, 2), (28f, 1));
            var delante = z[0]; var detras = z[1]; var espalda = z[2]; var otroCarril = z[3]; var lejos = z[4];

            Golpe(sim, 0f);
            Golpe(sim, 0.25f);
            Avanzar(sim, 0.24f, Quieta);
            // Los zombies de prueba no se mueven solos: se los deja donde el barrido los tiene que encontrar.
            Reponer(z, sim, 1f, 2f, -1.5f, 1.2f, 7f);
            float xDetras = detras.X, xEspalda = espalda.X, xLejos = lejos.X;

            var ev = sim.Tick(new PlayerIntent { Golpe = true }, Dt);

            Assert.IsTrue((ev & RunEvent.Remate) != 0);
            Assert.IsTrue(detras.Alive && espalda.Alive, "el barrido no los mata");
            Assert.Greater(detras.Derribado, 1f, "queda en el piso");
            Assert.AreEqual(0f, espalda.Derribado, 0.0001f, "el que esta a su espalda no se toca");
            Assert.Greater(detras.Golpes, 2, "no pierde golpes");
            Assert.Greater(detras.X, xDetras + 3f, "sale empujado hacia donde pego");
            Assert.AreEqual(xEspalda, espalda.X, 0.15f, "y no sale empujado");
            Assert.Greater(otroCarril.Derribado, 0f, "un carril de diferencia tambien entra en el barrido");
            Assert.AreEqual(0f, lejos.Derribado, 0.0001f, "fuera del radio no pasa nada");
            Assert.AreEqual(xLejos, lejos.X, 0.5f);
            Assert.AreEqual(2, delante.Golpes, "el de enfrente si recibe un golpe de verdad");
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void TheFinisher_HitsWhereSheLooks_InBothDirections(int mira)
        {
            var sim = SinNafta(Rapido());
            var z = Poner(sim, (25f * mira, 1), (26f * mira, 1), (-25f * mira, 1));
            sim.State.Mira = mira;
            Golpe(sim, 0f);
            Golpe(sim, 0.25f);
            Avanzar(sim, 0.24f, Quieta);
            Reponer(z, sim, 1.2f * mira, 2f * mira, -1.5f * mira);
            sim.State.Mira = mira;

            var ev = sim.Tick(new PlayerIntent { Golpe = true }, Dt);

            Assert.IsTrue((ev & RunEvent.Remate) != 0);
            Assert.Greater(z[1].Derribado, 1f, "el de delante cae");
            Assert.AreEqual(0f, z[2].Derribado, 0.0001f, "el de atras no");
        }

        [Test]
        public void TheKnockedDown_StayDownForAWhile_ThenGetUp()
        {
            var sim = SinNafta(Rapido());
            var z = Poner(sim, (25f, 1), (26f, 1));
            Golpe(sim, 0f);
            Golpe(sim, 0.25f);
            Avanzar(sim, 0.24f, Quieta);
            Reponer(z, sim, 1f, 2f);
            Golpe(sim, 0f);
            Assert.Greater(z[1].Derribado, 1f);

            Avanzar(sim, 1.0f, Quieta);
            Assert.Greater(z[1].Derribado, 0f, "todavia en el piso");
            Avanzar(sim, 0.6f, Quieta);
            Assert.AreEqual(0f, z[1].Derribado, 0.0001f, "se levanto");
        }

        [Test]
        public void AHeavyZombie_BarelyMoves_AndGetsUpSooner()
        {
            var c = Rapido();
            var roster = ScriptableObject.CreateInstance<Zombineta.Enemies.ZombieRoster>();
            roster.types.Add(new Zombineta.Enemies.ZombieType { name = "Comun", speedMultiplier = 1f, deathChance = 1f, golpesParaMorir = 3 });
            roster.types.Add(new Zombineta.Enemies.ZombieType { name = "Pesado", speedMultiplier = 0.75f, deathChance = 1f, golpesParaMorir = 5, resisteEmpuje = 0.7f });
            c.zombies = roster;

            var sim = SinNafta(c);
            var z = Poner(sim, (25f, 1), (26f, 1), (27f, 2));   // el tercero, en otro carril: no se pisan
            z[1].Type = 1;   // pesado
            z[2].Type = 0;   // comun
            Golpe(sim, 0f);
            Golpe(sim, 0.25f);
            Avanzar(sim, 0.24f, Quieta);
            Reponer(z, sim, 1f, 2f, 1.8f);
            float pesado0 = z[1].X, comun0 = z[2].X;

            Golpe(sim, 0f);

            Assert.Less(Mathf.Abs(z[1].X - pesado0), 1.2f, "el pesado casi no se mueve");
            Assert.Greater(Mathf.Abs(z[2].X - comun0), 3f, "el comun si sale volando");
            Assert.Less(z[1].Derribado, z[2].Derribado, "el pesado se levanta antes");
        }

        [Test]
        public void OnlyTwoZombies_PrepareAnAttackAtOnce_TheRestWaitTheirTurn()
        {
            var c = Rapido();
            c.zombieSeparacion = 0f;   // los junto a proposito para que todos esten en rango
            c.maxAtacantes = 2;
            var sim = SinNafta(c);
            var z = Poner(sim, (0.8f, 1), (0.9f, 1), (-0.8f, 1), (-0.9f, 1));
            foreach (var u in z) u.Enfriamiento = 0f;

            sim.Tick(Quieta, Dt);

            int preparando = 0;
            foreach (var u in z) if (u.Preparando >= 0f) preparando++;
            Assert.AreEqual(2, preparando, "dos preparan el ataque, los otros dos esperan");
        }

        [Test]
        public void WhenOneAttackerIsDone_AWaitingOneTakesTheTurn()
        {
            var c = Rapido();
            c.zombieSeparacion = 0f;
            c.maxAtacantes = 1;
            var sim = SinNafta(c);
            var z = Poner(sim, (0.8f, 1), (-0.8f, 1));
            foreach (var u in z) u.Enfriamiento = 0f;

            sim.Tick(Quieta, Dt);
            int primero = z[0].Preparando >= 0f ? 0 : 1;
            Assert.IsTrue(z[1 - primero].Preparando < 0f, "el otro espera");

            Avanzar(sim, 0.7f, Quieta);   // el primero ya pego (0,6 s de preparacion)

            Assert.IsTrue(z[1 - primero].Preparando >= 0f || sim.State.Vida < 5, "le toca al que esperaba");
        }

        // --- Fase 4: nafta, frenado, arranque, oleadas y reintento -------------------

        static GameConfig Frenando()
        {
            var c = Rapido();
            c.paradaSegundos = 1.2f;
            c.arranqueSegundos = 0.9f;
            return c;
        }

        [Test]
        public void TheTankStartsAsShortAsTheLevelWants()
        {
            var c = MakeConfig();
            c.fuelInicial = 30f;
            Assert.AreEqual(30f, new RunSimulation(c).State.Fuel, 0.001f);

            c.fuelInicial = 0f;
            Assert.AreEqual(c.fuelMax, new RunSimulation(c).State.Fuel, 0.001f, "0 = tanque lleno");

            c.fuelInicial = 999f;
            Assert.AreEqual(c.fuelMax, new RunSimulation(c).State.Fuel, 0.001f, "nunca mas que el tanque");
        }

        [Test]
        public void RunningOutOfFuel_BrakesForAWhile_BeforeSheGetsOff()
        {
            var sim = new RunSimulation(Frenando());
            sim.State.PlayerX = 500f;
            sim.Horde.Reset(400f);
            sim.State.Fuel = 0.01f;

            // El tanque se vacia en el primer cuadro: ahi arranca el frenado, todavia sobre la moto.
            RunEvent ev = RunEvent.None;
            for (int i = 0; i < 5 && (ev & RunEvent.Frenando) == 0; i++)
                ev |= sim.Tick(Quieta, Dt);
            Assert.IsTrue((ev & RunEvent.Frenando) != 0, "avisa que empieza a frenar");
            Assert.AreEqual(RunPhase.Running, sim.State.Phase, "todavia no se bajo");
            float velocidadAlFrenar = sim.State.VelocidadAlFrenar;
            Assert.Greater(velocidadAlFrenar, 5f, "venia a toda velocidad");

            float x0 = sim.State.PlayerX, anterior = float.MaxValue;
            for (int i = 0; i < 40; i++)
            {
                sim.Tick(Quieta, Dt);
                Assert.LessOrEqual(sim.PlayerSpeed, anterior + 0.0001f, "solo frena, nunca acelera");
                anterior = sim.PlayerSpeed;
            }
            Assert.Greater(sim.State.PlayerX, x0 + 1f, "sigue avanzando mientras frena");
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);

            RunEvent fin = RunEvent.None;
            for (int i = 0; i < 120 && sim.State.Phase == RunPhase.Running; i++)
                fin |= sim.Tick(Quieta, Dt);
            Assert.AreEqual(RunPhase.Pelea, sim.State.Phase, "pasado el frenado se baja a pelear");
            Assert.IsTrue((fin & RunEvent.PeleaEmpieza) != 0);
            Assert.AreEqual(0f, sim.PlayerSpeed, 0.001f, "la moto quedo parada");
        }

        [Test]
        public void WhileBraking_TheHordeCannotCatchHer()
        {
            var c = Frenando();
            var sim = new RunSimulation(c);
            sim.State.PlayerX = 500f;
            sim.Horde.Reset(499.5f);       // la horda casi encima
            sim.State.HordeX = sim.Horde.FrontX;
            sim.State.Fuel = 0.01f;

            for (int i = 0; i < 90 && sim.State.Phase == RunPhase.Running; i++)
                sim.Tick(Quieta, Dt);

            Assert.AreNotEqual(RunPhase.Lost, sim.State.Phase, "mientras frena no la alcanzan: pelea");
        }

        [Test]
        public void IfTheMomentumCarriesHerToTheGoalWhileBraking_SheWins()
        {
            var c = Frenando();
            var sim = new RunSimulation(c);
            sim.State.PlayerX = c.goalDistance - 2f;
            sim.Horde.Reset(c.goalDistance - 100f);
            sim.State.Fuel = 0.01f;

            for (int i = 0; i < 120 && sim.State.Phase == RunPhase.Running; i++)
                sim.Tick(Quieta, Dt);

            Assert.AreEqual(RunPhase.Won, sim.State.Phase);
        }

        [Test]
        public void WithoutHelpLeft_NoBrakingTransition_ItIsTheOldDefeat()
        {
            var c = Frenando();
            c.ayudas = 0;
            var sim = new RunSimulation(c);
            sim.State.PlayerX = 500f;
            sim.Horde.Reset(460f);
            sim.State.Fuel = 0f;

            for (int i = 0; i < 600 && sim.State.Phase == RunPhase.Running; i++)
                sim.Tick(Quieta, Dt);

            Assert.AreEqual(RunPhase.Lost, sim.State.Phase);
            Assert.AreEqual(LossReason.OutOfFuel, sim.State.Loss);
        }

        [Test]
        public void AfterTheAid_TheBikeStartsOffSlowlyAndReachesNormalSpeed()
        {
            var sim = SinNafta(Frenando());
            for (int i = 0; i < 200 && sim.State.Phase == RunPhase.Running; i++)
                sim.Tick(Quieta, Dt);                 // primero frena y se baja
            Assert.AreEqual(RunPhase.Pelea, sim.State.Phase);
            sim.State.PeleaRestante = 0f;
            Avanzar(sim, 5f, Quieta);                 // llega la furgoneta y termina
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);
            Assert.Greater(sim.State.ArranqueRestante, 0f);

            sim.Tick(Quieta, Dt);
            Assert.Less(sim.PlayerSpeed, 3f, "arranca despacio");

            for (int i = 0; i < 80; i++)
                sim.Tick(Quieta, Dt);
            Assert.AreEqual(10f, sim.PlayerSpeed, 0.01f, "y llega a la velocidad normal");
        }

        [Test]
        public void TheFirstFight_HasAnExtraHeart_TheNextOnesDoNot()
        {
            var c = Rapido();
            c.vidaExtraPrimeraPelea = 1;
            var sim = SinNafta(c);
            Assert.AreEqual(1, sim.State.AyudasUsadas);
            Assert.AreEqual(c.vidaPelea + 1, sim.State.Vida);

            sim.State.PeleaRestante = 0f;
            Avanzar(sim, 5f, Quieta);
            sim.State.Fuel = 0f;
            sim.Tick(Quieta, Dt);

            Assert.AreEqual(2, sim.State.AyudasUsadas);
            Assert.AreEqual(c.vidaPelea, sim.State.Vida, "la segunda pelea ya no tiene el corazon de mas");
        }

        static GameConfig ConOleadas()
        {
            var c = Rapido();
            var roster = ScriptableObject.CreateInstance<Zombineta.Enemies.ZombieRoster>();
            roster.types.Add(new Zombineta.Enemies.ZombieType { name = "Comun", speedMultiplier = 1f, deathChance = 1f, golpesParaMorir = 3 });
            roster.types.Add(new Zombineta.Enemies.ZombieType { name = "Corredor", speedMultiplier = 1.35f, deathChance = 1f, golpesParaMorir = 2 });
            roster.types.Add(new Zombineta.Enemies.ZombieType { name = "Pesado", speedMultiplier = 0.75f, deathChance = 1f, golpesParaMorir = 5 });
            c.zombies = roster;
            c.hordeCount = 12;
            c.oleadas = new List<OleadaPelea>
            {
                new OleadaPelea { desdeSegundo = 0f,  zombies = 3, pesos = new[] { 1f, 0f, 0f }, velocidad = 0.5f },
                new OleadaPelea { desdeSegundo = 10f, zombies = 5, pesos = new[] { 0f, 1f, 0f }, velocidad = 1f },
                new OleadaPelea { desdeSegundo = 20f, zombies = 6, pesos = new[] { 0f, 0f, 1f }, velocidad = 1f },
            };
            c.peleaSegundos = 30f;
            return c;
        }

        static int Activos(RunSimulation sim)
        {
            int n = 0;
            for (int i = 0; i < sim.Horde.Units.Length; i++)
                if (sim.Pelea.Activo(i)) n++;
            return n;
        }

        [Test]
        public void TheFirstWave_IsFewAndOfTheCommonKind()
        {
            var sim = SinNafta(ConOleadas());

            Assert.AreEqual(3, Activos(sim));
            for (int i = 0; i < sim.Horde.Units.Length; i++)
                if (sim.Pelea.Activo(i))
                    Assert.AreEqual(0, sim.Horde.Units[i].Type, "solo comunes al principio");
            Assert.AreEqual(0, sim.State.Oleada);
        }

        [Test]
        public void TheWalkingSpeed_FollowsTheKindAndTheWave_SoTheViewCanChooseBetweenWalkingAndRunning()
        {
            var c = ConOleadas();
            var sim = SinNafta(c);
            var comun = sim.Horde.Units[0];
            Assert.IsTrue(sim.Pelea.Activo(0));
            Assert.AreEqual(c.zombieVelocidadPelea * 1f * 0.5f, sim.Pelea.VelocidadDe(comun), 0.001f, "comun de la primera oleada: lento");

            comun.Type = 1;   // corredor
            sim.State.Oleada = 1;
            Assert.AreEqual(c.zombieVelocidadPelea * 1.35f * 1f, sim.Pelea.VelocidadDe(comun), 0.001f, "corredor de la segunda: corre");
            Assert.Greater(sim.Pelea.VelocidadDe(comun), 2.6f, "por encima del limite de caminar lento");
        }

        [Test]
        public void TheWavesGrow_WithMoreZombiesAndNewKinds_AndAnnounceThemselves()
        {
            var sim = SinNafta(ConOleadas());
            int anuncios = 0;
            var vistos = new HashSet<int>();

            for (int t = 0; t < 60 * 29 && sim.State.Phase == RunPhase.Pelea; t++)
            {
                sim.State.Vida = 5;                     // no es lo que se prueba aca
                var ev = sim.Tick(Quieta, Dt);
                if ((ev & RunEvent.OleadaNueva) != 0) anuncios++;
                for (int i = 0; i < sim.Horde.Units.Length; i++)
                    if (sim.Pelea.Activo(i) && sim.Horde.Units[i].Alive)
                        vistos.Add(sim.Horde.Units[i].Type);
            }

            Assert.AreEqual(2, anuncios, "dos oleadas nuevas despues de la primera");
            Assert.AreEqual(2, sim.State.Oleada);
            Assert.AreEqual(6, Activos(sim), "la ultima oleada trae seis a la vez");
            CollectionAssert.Contains(vistos, 1);
            CollectionAssert.Contains(vistos, 2);
        }

        [Test]
        public void TheWaveCountsKeepGoingEvenWhenZombiesDie()
        {
            var sim = SinNafta(ConOleadas());
            for (int t = 0; t < 60 * 11; t++)
            {
                sim.State.Vida = 5;
                sim.Tick(Quieta, Dt);
            }
            Assert.AreEqual(5, Activos(sim), "a los 10 s son cinco los que pelean");
        }

        [Test]
        public void IfTheyKnockHerDownWithARetryLeft_TheFightStartsOver()
        {
            var c = Rapido();
            c.reintentosPelea = 1;
            var sim = SinNafta(c);
            var z = Poner(sim, (0.8f, 1));
            sim.State.Vida = 1;
            z[0].Enfriamiento = 0f;
            float restanteAntes = sim.State.PeleaRestante;

            RunEvent ev = RunEvent.None;
            for (int t = 0; t < 120 && (ev & RunEvent.PeleaReintenta) == 0; t++)
                ev |= sim.Tick(Quieta, Dt);

            Assert.IsTrue((ev & RunEvent.PeleaReintenta) != 0);
            Assert.AreEqual(RunPhase.Pelea, sim.State.Phase, "no pierde");
            Assert.AreEqual(c.vidaPelea, sim.State.Vida, "vida entera otra vez");
            Assert.AreEqual(c.peleaSegundos, sim.State.PeleaRestante, 0.001f, "el reloj del auxilio de nuevo");
            Assert.Greater(sim.State.Invulnerable, 1f, "un respiro para reacomodarse");
            Assert.AreEqual(0, sim.State.ReintentosPelea);
            Assert.AreEqual(1, sim.State.AyudasUsadas, "el reintento no gasta otro auxilio");
        }

        [Test]
        public void AfterUsingTheRetry_TheNextKnockdownIsTheDefeat()
        {
            var c = Rapido();
            c.reintentosPelea = 1;
            var sim = SinNafta(c);

            for (int vez = 0; vez < 2 && sim.State.Phase == RunPhase.Pelea; vez++)
            {
                var z = Poner(sim, (0.8f, 1));
                sim.State.Vida = 1;
                sim.State.Invulnerable = 0f;
                z[0].Enfriamiento = 0f;
                for (int t = 0; t < 120 && sim.State.Phase == RunPhase.Pelea && sim.State.Vida > 0 && sim.State.ReintentosPelea >= 0; t++)
                {
                    sim.Tick(Quieta, Dt);
                    if (vez == 0 && sim.State.ReintentosPelea == 0) break;
                }
            }

            Assert.AreEqual(RunPhase.Lost, sim.State.Phase);
            Assert.AreEqual(LossReason.Derribada, sim.State.Loss);
        }

        [Test]
        public void SwitchedOffWavesAndRetries_KeepTheOldBehaviour()
        {
            var c = Rapido();   // sin oleadas (MakeConfig las vacia): 'zombiesEnArena' de siempre
            c.zombiesEnArena = 6;
            var sim = SinNafta(c);
            Assert.AreEqual(6, Activos(sim));
            Assert.AreEqual(0, sim.State.ReintentosPelea);
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
