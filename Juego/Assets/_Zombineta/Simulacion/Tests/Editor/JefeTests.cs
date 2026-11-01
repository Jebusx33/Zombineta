using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Jefe;
using Zombineta.Player;

namespace Zombineta.Tests
{
    /// <summary>El duelo en moto contra el jefe del nivel 2.</summary>
    public class JefeTests
    {
        const float Dt = 1f / 60f;
        const float Entrada = 100f;

        static JefeConfig MakeJefe()
        {
            var j = ScriptableObject.CreateInstance<JefeConfig>();
            j.metroEntrada = Entrada;
            j.segundosEntrada = 0.5f;
            j.pausaEntreAcciones = 0.2f;
            return j;
        }

        static GameConfig MakeConfig(JefeConfig jefe)
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 200f;
            c.startingGap = 60f;
            c.normalSpeed = 10f;
            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 1f;
            c.batteryMax = 100f;
            c.ammoMax = 6;
            c.ammoAtStart = 6;
            c.hordeBaseSpeed = 10f;
            c.rubberBandMaxBonus = 0f;
            c.hordeCount = 8;
            c.hordeDepthMeters = 12f;
            c.corpseSeconds = 1f;
            c.hordeSeed = 11;
            c.shotRangeMeters = 60f;
            c.explosionRadius = 8f;
            c.explosionScareRadius = 16f;
            c.explosionScareSeconds = 1f;
            c.deathScareRadius = 4f;
            c.deathScareSeconds = 0.5f;
            c.retrocesoAciertos = 0;
            c.granadaMax = 3;
            c.granadaInicial = 3;
            c.granadaDistancia = 10f;
            c.granadaVuelo = 0.5f;
            c.laneChangeDuration = 0.1f;
            c.motoNariz = 1f;
            c.motoCola = 1f;
            c.imanCarriles = 1f;
            c.paradaSegundos = 0f;
            c.jefe = jefe;
            return c;
        }

        static RunSimulation Duelo(JefeConfig jefe = null)
        {
            var sim = new RunSimulation(MakeConfig(jefe ?? MakeJefe()));
            sim.State.PlayerX = Entrada - 1f;
            sim.Horde.Reset(sim.State.PlayerX - 60f);
            Correr(sim, 0.2f);
            return sim;
        }

        static RunEvent Correr(RunSimulation sim, float segundos, int carrilDelta = 0)
        {
            var todo = RunEvent.None;
            for (float t = 0f; t < segundos; t += Dt)
                todo |= sim.Tick(new PlayerIntent { LaneDelta = t == 0f ? carrilDelta : 0 }, Dt);
            return todo;
        }

        static RunEvent HastaFase(RunSimulation sim, FaseDeJefe fase, float tope = 30f, bool esquivar = true)
        {
            var todo = RunEvent.None;
            for (float t = 0f; t < tope && sim.Jefe.Fase != fase; t += Dt)
            {
                // Se corre siempre del carril fijado, para llegar a la fase sin que la moto pierda corazones.
                int delta = 0;
                if (esquivar && sim.Jefe.AvisoDeCarril && sim.Jefe.Carril == sim.State.Lane)
                    delta = sim.State.Lane == 0 ? 1 : -1;
                todo |= sim.Tick(new PlayerIntent { LaneDelta = delta }, Dt);
            }
            Assert.AreEqual(fase, sim.Jefe.Fase);
            return todo;
        }

        static RunEvent Disparar(RunSimulation sim) => sim.Tick(new PlayerIntent { Fire = true }, Dt);

        static void Alinear(RunSimulation sim)
        {
            for (int i = 0; i < 3 && sim.State.Lane != sim.Jefe.Carril; i++)
                sim.Tick(new PlayerIntent { LaneDelta = sim.Jefe.Carril - sim.State.Lane }, Dt);
            Correr(sim, 0.15f);
        }

        [Test]
        public void SinJefeElNivelSeJuegaComoSiempre()
        {
            var sim = new RunSimulation(MakeConfig(null));
            sim.State.PlayerX = 199.9f;
            Assert.AreNotEqual(RunEvent.None, Correr(sim, 0.1f) & RunEvent.Won);
            Assert.IsFalse(sim.Jefe.EnDuelo);
        }

        [Test]
        public void EntraAlLlegarAlMetroYLlenaElTanque()
        {
            var sim = new RunSimulation(MakeConfig(MakeJefe()));
            sim.State.PlayerX = Entrada - 1f;
            sim.State.Fuel = 15f;
            sim.Horde.Reset(sim.State.PlayerX - 60f);

            var ev = Correr(sim, 0.2f);

            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.JefeEntra);
            Assert.AreEqual(100f, sim.State.Fuel);
        }

        [Test]
        public void SinNaftaEsperaAQueTermineLaPelea()
        {
            var sim = new RunSimulation(MakeConfig(MakeJefe()));
            sim.State.PlayerX = Entrada + 1f;
            sim.State.Fuel = 0f;

            sim.Tick(PlayerIntent.Idle, Dt);

            Assert.IsFalse(sim.Jefe.EnDuelo);
            Assert.AreEqual(RunPhase.Pelea, sim.State.Phase);
        }

        [Test]
        public void EnElDueloNoSeGastaNafta()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Aturdido);
            Assert.AreEqual(100f, sim.State.Fuel);
        }

        [Test]
        public void LaHordaSeQuedaAtrasYNoAlcanza()
        {
            var sim = Duelo();
            float antes = sim.State.Gap;
            Correr(sim, 3f, 1);
            Assert.That(sim.State.Gap, Is.GreaterThan(antes));
            sim.State.PlayerX = sim.State.HordeX - 1f;
            Correr(sim, 0.1f);
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);
        }

        [Test]
        public void ElCarrilSeFijaAlEmpezarElAtaqueYNoSigueALaMoto()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Anticipacion);
            int fijado = sim.Jefe.Carril;

            Correr(sim, 0.3f, fijado == 0 ? 1 : -1);

            Assert.AreNotEqual(fijado, sim.State.Lane);
            Assert.AreEqual(fijado, sim.Jefe.Carril);
        }

        [Test]
        public void ElProyectilFrontalPegaUnaSolaVezEnElCarrilFijado()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Anticipacion);

            var ev = Correr(sim, 1.7f);

            Assert.AreEqual(2, sim.Jefe.Corazones);
            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.JefeDispara);
            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.JefeGolpea);
            Assert.IsFalse(sim.Jefe.ProyectilActivo);
        }

        [Test]
        public void CambiarDeCarrilEsquivaElProyectil()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Anticipacion);
            Correr(sim, 0.5f, sim.Jefe.Carril == 0 ? 1 : -1);

            HastaFase(sim, FaseDeJefe.Persecucion, 3f, false);
            Assert.AreEqual(3, sim.Jefe.Corazones);
        }

        [Test]
        public void EmiteUnSoloProyectilPorAtaque()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Anticipacion);
            int emisiones = 0;
            for (float t = 0f; t < 2f; t += 0.05f)
                if ((sim.Tick(PlayerIntent.Idle, 0.05f) & RunEvent.JefeDispara) != 0)
                    emisiones++;
            Assert.AreEqual(1, emisiones);
        }

        [Test]
        public void ElElevadoEmiteAunqueUnCuadroLargoSalteeElMomento()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Persecucion);   // ya paso el frontal
            HastaFase(sim, FaseDeJefe.Anticipacion);
            Assert.IsTrue(sim.Jefe.AtaqueElevado);
            HastaFase(sim, FaseDeJefe.Disparo, 3f, false);

            var ev = sim.Tick(PlayerIntent.Idle, 0.3f);

            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.JefeDispara);
            Assert.IsTrue(sim.Jefe.ProyectilElevado);
        }

        [Test]
        public void ElElevadoCaeSobreElCarrilFijadoAunqueLaMotoSiga()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Persecucion);
            HastaFase(sim, FaseDeJefe.Anticipacion);
            int fijado = sim.Jefe.Carril;
            Correr(sim, 0.4f, fijado == 0 ? 1 : -1);

            var ev = Correr(sim, 2.5f);

            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.JefeImpacto);
            Assert.AreEqual(fijado, sim.Jefe.ImpactoCarril);
            Assert.IsTrue(sim.Jefe.ImpactoEnElPiso);
            Assert.AreEqual(3, sim.Jefe.Corazones);
        }

        [Test]
        public void SinAturdirNoSeLoLastima()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Anticipacion);
            int vida = sim.Jefe.Vida;

            var ev = Disparar(sim);

            Assert.AreEqual(vida, sim.Jefe.Vida);
            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.JefeRebota);
            Assert.AreEqual(5, sim.State.Ammo, "la bala se gasta igual");
        }

        [Test]
        public void AturdidoLaBalaLoLastimaSoloEnSuCarril()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Aturdido);
            Alinear(sim);
            int vida = sim.Jefe.Vida;

            var ev = Disparar(sim);

            Assert.AreEqual(vida - 1, sim.Jefe.Vida);
            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.JefeHerido);

            Correr(sim, 0.15f, sim.Jefe.Carril == 0 ? 1 : -1);
            Disparar(sim);
            Assert.AreEqual(vida - 1, sim.Jefe.Vida, "desde otro carril no llega");
        }

        [Test]
        public void LaGranadaLoLastimaAturdido()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Aturdido);
            int vida = sim.Jefe.Vida;

            sim.Tick(new PlayerIntent { Granada = true }, Dt);
            Correr(sim, 0.7f);

            Assert.AreEqual(vida - 3, sim.Jefe.Vida);
        }

        [Test]
        public void ConMediaVidaPasaAlPatronConEmbestida()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Aturdido);
            Alinear(sim);
            sim.State.Granadas = 3;
            for (int i = 0; i < 2; i++)
            {
                sim.Tick(new PlayerIntent { Granada = true }, Dt);
                Correr(sim, 0.7f);
            }
            Assert.AreEqual(6, sim.Jefe.Vida);

            HastaFase(sim, FaseDeJefe.AvisoEmbestida);
        }

        [Test]
        public void LaEmbestidaPegaEnElCarrilFijadoYPierdeTerreno()
        {
            var jefe = MakeJefe();
            jefe.patronPrimeraMitad = new[] { AccionDeJefe.Embestida };
            var sim = Duelo(jefe);
            HastaFase(sim, FaseDeJefe.AvisoEmbestida);

            var ev = Correr(sim, jefe.avisoEmbestida + jefe.avanceEmbestida + 0.1f);

            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.JefeEmbiste);
            Assert.AreEqual(2, sim.Jefe.Corazones);
            HastaFase(sim, FaseDeJefe.Persecucion, 3f, false);
            Assert.AreEqual(jefe.separacion, sim.Jefe.Separacion, 0.01f);
        }

        [Test]
        public void ConLaInvulnerabilidadUnGolpeNoSigueAlOtro()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Anticipacion);
            Correr(sim, 1.8f);
            Assert.AreEqual(2, sim.Jefe.Corazones);
            Assert.That(sim.Jefe.Invulnerable, Is.GreaterThan(0f));
        }

        [Test]
        public void SinCorazonesEsGameOverUnaSolaVez()
        {
            var jefe = MakeJefe();
            jefe.corazones = 1;
            var sim = Duelo(jefe);
            HastaFase(sim, FaseDeJefe.Anticipacion);

            var ev = Correr(sim, 3f);

            Assert.AreEqual(RunPhase.Lost, sim.State.Phase);
            Assert.AreEqual(LossReason.AlcanzadaPorJefe, sim.State.Loss);
            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.Lost);
        }

        [Test]
        public void CruzarLaMetaNoTerminaElNivelConElJefeVivo()
        {
            var sim = Duelo();
            sim.State.PlayerX = 300f;
            Correr(sim, 0.5f);
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);
            Assert.That(sim.Meta, Is.GreaterThan(sim.State.PlayerX));
        }

        [Test]
        public void AlDerrotarloCaeUnaVezYElRefugioApareceAdelante()
        {
            var jefe = MakeJefe();
            jefe.vida = 1;
            var sim = Duelo(jefe);
            HastaFase(sim, FaseDeJefe.Aturdido);
            Alinear(sim);

            var ev = Disparar(sim);

            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.JefeDerrotado);
            Assert.AreEqual(FaseDeJefe.Derrotado, sim.Jefe.Fase);
            Assert.AreEqual(sim.State.PlayerX + jefe.metrosAlRefugio, sim.Meta, 0.5f);
            Assert.AreEqual(RunEvent.None, Disparar(sim) & (RunEvent.JefeHerido | RunEvent.JefeDerrotado));

            Correr(sim, jefe.caidaEnPantalla - 0.5f);
            Assert.AreEqual(jefe.separacion, sim.Jefe.Separacion, 0.01f, "mientras cae sigue en pantalla");

            Assert.AreNotEqual(RunEvent.None, Correr(sim, 8f) & RunEvent.Won);
        }

        [Test]
        public void LaCajaDaBalasCuandoElCargadorNoEstaLleno()
        {
            var sim = new RunSimulation(MakeConfig(MakeJefe()));
            sim.State.PlayerX = Entrada - 1f;
            sim.Horde.Reset(sim.State.PlayerX - 60f);
            sim.State.Ammo = 0;

            HastaFase(sim, FaseDeJefe.Anticipacion);

            Assert.IsTrue(sim.Jefe.CajaDeBalas.Activo);
            Assert.AreNotEqual(RunEvent.None, HastaRecoger(sim, sim.Jefe.CajaDeBalas) & RunEvent.PickedUp);
            Assert.AreEqual(4, sim.State.Ammo);
        }

        [Test]
        public void AparecenGranadasParaRecogerMientrasNoEsteAlTope()
        {
            var sim = new RunSimulation(MakeConfig(MakeJefe()));
            sim.State.PlayerX = Entrada - 1f;
            sim.Horde.Reset(sim.State.PlayerX - 60f);
            sim.State.Granadas = 0;

            HastaFase(sim, FaseDeJefe.Anticipacion);

            Assert.IsTrue(sim.Jefe.GranadaSuelta.Activo);
            Assert.AreNotEqual(RunEvent.None, HastaRecoger(sim, sim.Jefe.GranadaSuelta) & RunEvent.PickedUp);
            Assert.AreEqual(1, sim.State.Granadas);
        }

        [Test]
        public void ConTodoAlTopeNoApareceNada()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Anticipacion);
            Assert.IsFalse(sim.Jefe.CajaDeBalas.Activo);
            Assert.IsFalse(sim.Jefe.GranadaSuelta.Activo);
        }

        [Test]
        public void LaGranadaSePierdeSiPasaLejos()
        {
            var sim = new RunSimulation(MakeConfig(MakeJefe()));
            sim.State.PlayerX = Entrada - 1f;
            sim.Horde.Reset(sim.State.PlayerX - 60f);
            sim.State.Granadas = 0;
            sim.State.Lane = 0;
            sim.State.LaneVisual = 0f;

            HastaFase(sim, FaseDeJefe.Anticipacion, 30f, false);
            Assert.AreEqual(2, sim.Jefe.GranadaSuelta.Carril);

            for (float t = 0f; t < 10f && sim.Jefe.GranadaSuelta.Activo; t += Dt)
            {
                sim.State.LaneVisual = 0f;
                sim.Tick(PlayerIntent.Idle, Dt);
            }

            Assert.AreEqual(0, sim.State.Granadas);
        }

        // Pone la moto en su carril: alcanzarlo con el iman de los carriles vecinos se prueba aparte.
        static RunEvent HastaRecoger(RunSimulation sim, RecursoDelDuelo recurso)
        {
            var todo = RunEvent.None;
            for (float t = 0f; t < 10f && recurso.Activo; t += Dt)
            {
                sim.State.LaneVisual = recurso.Carril;
                todo |= sim.Tick(PlayerIntent.Idle, Dt);
            }
            return todo;
        }

        [Test]
        public void ReiniciarDejaElDueloComoAlPrincipio()
        {
            var sim = Duelo();
            HastaFase(sim, FaseDeJefe.Anticipacion);
            Correr(sim, 3f);

            sim.Reset();

            Assert.AreEqual(FaseDeJefe.Inactivo, sim.Jefe.Fase);
            Assert.AreEqual(12, sim.Jefe.Vida);
            Assert.AreEqual(3, sim.Jefe.Corazones);
            Assert.IsFalse(sim.Jefe.ProyectilActivo);
            Assert.AreEqual(200f, sim.Meta);
        }
    }
}
