using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;
using Zombineta.Player;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// Si se acaba la nafta en pleno apagon, la luz vuelve para pelear y sigue prendida hasta que ese apagon termina;
    /// el apagon siguiente se aplica normal.
    /// </summary>
    public class ApagonEnLaPeleaTests
    {
        const float Dt = 1f / 60f;

        // Tramos: 0-600 apagon, 600-1000 luz, 1000-1500 apagon.
        static GameConfig Config()
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
            c.paradaSegundos = 0f;
            c.peleaSinNafta = true;
            c.tramoDarknessFadeMeters = 0f;
            c.tramos = new List<Tramo>
            {
                new Tramo { nombre = "Apagon", hasta = 600f, oscuridad = 1f },
                new Tramo { nombre = "Luz", hasta = 1000f, oscuridad = 0f },
                new Tramo { nombre = "Apagon 2", hasta = 1500f, oscuridad = 1f },
            };
            return c;
        }

        static RunSimulation SinNaftaEn(float metros)
        {
            var sim = new RunSimulation(Config());
            sim.State.PlayerX = metros;
            sim.Horde.Reset(metros - 40f);
            sim.State.Fuel = 0f;
            sim.Tick(PlayerIntent.Idle, Dt);
            return sim;
        }

        [Test]
        public void RunningOutOfFuel_InABlackout_TurnsTheLightsOn_ForTheFight()
        {
            var sim = SinNaftaEn(500f);

            Assert.AreEqual(RunPhase.Pelea, sim.State.Phase);
            Assert.AreEqual(1f, sim.OscuridadDelTramo, "el tramo sigue siendo un apagon");
            Assert.AreEqual(0f, sim.Oscuridad, "pero se pelea con luz");
            Assert.IsTrue(sim.State.ApagonAnulado);
        }

        [Test]
        public void TheLightsStayOn_UntilTheBlackoutEnds_AndTheNextBlackoutIsNormal()
        {
            var sim = SinNaftaEn(500f);
            sim.State.Phase = RunPhase.Running;               // termino la pelea y vuelve a la moto
            sim.State.Fuel = 100f;

            sim.State.PlayerX = 580f;                          // todavia en el mismo apagon
            sim.Tick(PlayerIntent.Idle, Dt);
            Assert.AreEqual(0f, sim.Oscuridad, "sigue con luz");

            sim.State.PlayerX = 800f;                          // salio del apagon
            sim.Tick(PlayerIntent.Idle, Dt);
            Assert.IsFalse(sim.State.ApagonAnulado, "se apaga la marca");
            Assert.AreEqual(0f, sim.Oscuridad);

            sim.State.PlayerX = 1100f;                         // el proximo apagon llega como siempre
            sim.Tick(PlayerIntent.Idle, Dt);
            Assert.AreEqual(1f, sim.Oscuridad);
        }

        [Test]
        public void RunningOutOfFuel_WhereThereIsLight_ChangesNothing()
        {
            var sim = SinNaftaEn(800f);

            Assert.AreEqual(RunPhase.Pelea, sim.State.Phase);
            Assert.IsFalse(sim.State.ApagonAnulado);
            Assert.AreEqual(0f, sim.Oscuridad);
        }

        [Test]
        public void ARestart_ClearsIt()
        {
            var sim = SinNaftaEn(500f);
            Assert.IsTrue(sim.State.ApagonAnulado);

            sim.Reset();

            Assert.IsFalse(sim.State.ApagonAnulado);
        }
    }
}
