using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;
using Zombineta.Player;

namespace Zombineta.Tests
{
    public class TramosTests
    {
        const float Dt = 1f / 60f;

        static List<Tramo> DosTramos() => new List<Tramo>
        {
            new Tramo { nombre = "Presentar", hasta = 500f, presionHorda = 0.8f, oscuridad = 0f },
            new Tramo { nombre = "Apagon", hasta = 1000f, presionHorda = 1.2f, oscuridad = 1f },
        };

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 2000f;
            c.startingGap = 50f;
            c.normalSpeed = 10f;
            c.hordeBaseSpeed = 10f;
            c.hordeCount = 8;
            c.hordeSeed = 5;
            c.rubberBandMaxBonus = 0f;
            c.mercyGap = 0f;
            c.tramoPressureBlendMeters = 0f;
            return c;
        }

        static PlayerIntent Normal() => new PlayerIntent { Mode = DriveMode.Normal };

        // --- Consultas ------------------------------------------------------

        [Test]
        public void WithoutTramos_EverythingIsAsAlways()
        {
            Assert.AreEqual(-1, Tramos.IndexAt(null, 100f));
            Assert.AreEqual(1f, Tramos.PresionAt(null, 100f, 80f));
            Assert.AreEqual(0f, Tramos.OscuridadAt(new List<Tramo>(), 100f, 30f));
        }

        [Test]
        public void EachMeterBelongsToItsTramo_AndTheLastOneGoesOn()
        {
            var tramos = DosTramos();

            Assert.AreEqual(0, Tramos.IndexAt(tramos, 0f));
            Assert.AreEqual(0, Tramos.IndexAt(tramos, 499f));
            Assert.AreEqual(1, Tramos.IndexAt(tramos, 500f));
            Assert.AreEqual(1, Tramos.IndexAt(tramos, 5000f), "pasado el ultimo 'hasta' sigue el ultimo");
        }

        [Test]
        public void FarFromALimit_TheValueIsTheTramosOwn()
        {
            var tramos = DosTramos();

            Assert.AreEqual(0.8f, Tramos.PresionAt(tramos, 200f, 80f), 0.0001f);
            Assert.AreEqual(1.2f, Tramos.PresionAt(tramos, 800f, 80f), 0.0001f);
            Assert.AreEqual(1f, Tramos.OscuridadAt(tramos, 800f, 30f), 0.0001f);
        }

        [Test]
        public void AcrossALimit_TheValueBlendsWithoutJumps()
        {
            var tramos = DosTramos();

            Assert.AreEqual(0.8f, Tramos.PresionAt(tramos, 460f, 80f), 0.0001f, "empieza la mezcla");
            Assert.AreEqual(0.9f, Tramos.PresionAt(tramos, 480f, 80f), 0.0001f);
            Assert.AreEqual(1.0f, Tramos.PresionAt(tramos, 500f, 80f), 0.0001f, "en el limite, la mitad");
            Assert.AreEqual(1.1f, Tramos.PresionAt(tramos, 520f, 80f), 0.0001f);
            Assert.AreEqual(1.2f, Tramos.PresionAt(tramos, 540f, 80f), 0.0001f, "termina la mezcla");
        }

        // --- En la simulacion -----------------------------------------------

        [Test]
        public void TheTramo_SetsHowFastTheHordeGoes()
        {
            var config = MakeConfig();
            config.tramos = DosTramos();
            var sim = new RunSimulation(config);

            sim.Tick(Normal(), Dt);
            Assert.AreEqual(8f, sim.HordeSpeed, 0.01f, "10 base x 0,8 del primer tramo");

            sim.State.PlayerX = 700f;
            sim.Tick(Normal(), Dt);
            Assert.AreEqual(12f, sim.HordeSpeed, 0.01f, "10 base x 1,2 del segundo");
            Assert.AreEqual(1f, sim.Oscuridad, 0.0001f);
        }

        [Test]
        public void WithoutTramos_TheHordeGoesAtItsBaseSpeed()
        {
            var sim = new RunSimulation(MakeConfig());

            sim.Tick(Normal(), Dt);

            Assert.AreEqual(10f, sim.HordeSpeed, 0.01f);
            Assert.AreEqual(0f, sim.Oscuridad);
        }

        // --- Piedad de ultimo metro -------------------------------------------

        [Test]
        public void Mercy_SlowsTheHordeOnlyWhenItIsOnTopOfYou()
        {
            var config = MakeConfig();
            config.mercyGap = 10f;
            config.mercySlowFactor = 0.5f;

            config.startingGap = 50f;
            var far = new RunSimulation(config);
            far.Tick(Normal(), Dt);
            Assert.AreEqual(10f, far.HordeSpeed, 0.01f, "lejos, sin piedad");

            config.startingGap = 5f;
            var near = new RunSimulation(config);
            near.Tick(Normal(), Dt);
            // La moto ya avanzo un tick cuando se mide la ventaja: de ahi el margen.
            Assert.AreEqual(7.5f, near.HordeSpeed, 0.15f, "a mitad de la ventaja de piedad, a mitad de camino");
        }

        [Test]
        public void Mercy_Off_ChangesNothing()
        {
            var config = MakeConfig();
            config.startingGap = 2f;
            var sim = new RunSimulation(config);

            sim.Tick(Normal(), Dt);

            Assert.AreEqual(10f, sim.HordeSpeed, 0.01f);
        }
    }
}
