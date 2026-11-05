using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Player;

namespace Zombineta.Tests
{
    /// <summary>Cerca del refugio la nafta alcanza siempre: no se queda sin nafta viendo la meta.</summary>
    public class NaftaDeGraciaTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 1000f;
            c.startingGap = 500f;
            c.normalSpeed = 10f;
            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 2f;
            c.batteryMax = 100f;
            c.hordeBaseSpeed = 1f;
            c.rubberBandMaxBonus = 0f;
            c.hordeCount = 4;
            c.crashFuelPenalty = 8f;
            c.crashStunDuration = 0.2f;
            c.paradaSegundos = 0f;
            c.metrosDeGracia = 200f;
            c.reservaDeGracia = 1f;
            return c;
        }

        static RunSimulation Moto(GameConfig c, float metros, float nafta)
        {
            var sim = new RunSimulation(c);
            sim.State.PlayerX = metros;
            sim.Horde.Reset(metros - 400f);
            sim.State.Fuel = nafta;
            return sim;
        }

        static RunEvent HastaElFinal(RunSimulation sim, float tope = 60f)
        {
            var todo = RunEvent.None;
            for (float t = 0f; t < tope && sim.State.Phase == RunPhase.Running; t += Dt)
                todo |= sim.Tick(PlayerIntent.Idle, Dt);
            return todo;
        }

        [Test]
        public void ConPocaNaftaCercaDeLaMetaLlegaIgual()
        {
            // A 150 m, a 10 m/s y 2 por segundo, harian falta 30: tiene 5.
            var sim = Moto(MakeConfig(), 850f, 5f);

            var ev = HastaElFinal(sim);

            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.Won);
            Assert.AreEqual(RunEvent.None, ev & RunEvent.RanOutOfFuel);
            Assert.That(sim.State.Fuel, Is.GreaterThanOrEqualTo(1f - 0.001f));
        }

        [Test]
        public void ElIndicadorSigueBajandoSinCongelarse()
        {
            var sim = Moto(MakeConfig(), 850f, 5f);
            float antes = sim.State.Fuel;
            for (int i = 0; i < 120; i++)
                sim.Tick(PlayerIntent.Idle, Dt);

            Assert.That(sim.State.Fuel, Is.LessThan(antes), "baja");
            Assert.That(sim.State.Fuel, Is.GreaterThan(1f), "pero alcanza");
        }

        [Test]
        public void SiLaNaftaAlcanzaSeGastaComoSiempre()
        {
            var sim = Moto(MakeConfig(), 850f, 80f);
            sim.Tick(PlayerIntent.Idle, Dt);
            Assert.AreEqual(80f - 2f * Dt, sim.State.Fuel, 0.0001f);
        }

        [Test]
        public void UnChoqueEnElTramoDeGraciaNoVaciaElTanque()
        {
            var sim = Moto(MakeConfig(), 900f, 3f);
            sim.ApplyCrash();
            Assert.AreEqual(1f, sim.State.Fuel, 0.0001f);
            Assert.AreNotEqual(RunEvent.None, HastaElFinal(sim) & RunEvent.Won);
        }

        [Test]
        public void LejosDeLaMetaSeQuedaSinNaftaComoSiempre()
        {
            var c = MakeConfig();
            c.peleaSinNafta = false;
            var sim = Moto(c, 300f, 1f);

            var ev = HastaElFinal(sim, 5f);

            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.RanOutOfFuel);
            Assert.AreEqual(0f, sim.State.Fuel);
        }

        [Test]
        public void ConCeroMetrosDeGraciaNoAyuda()
        {
            var c = MakeConfig();
            c.metrosDeGracia = 0f;
            c.peleaSinNafta = false;
            var sim = Moto(c, 950f, 0.5f);

            var ev = HastaElFinal(sim, 5f);

            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.RanOutOfFuel);
        }
    }
}
