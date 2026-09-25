using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;
using Zombineta.Player;
using Zombineta.Tutorial;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// TutorialSesion con una RunSimulation y un LevelRuntime reales pero chicos, armados en el
    /// test: cada cuadro se corre en el mismo orden que en el juego (AntesDelTick, el Tick de
    /// RunController.StepSimulation, DespuesDelTick). El recorrido real del tutorial se prueba
    /// entero en TutorialDePuntaAPuntaTests.
    /// </summary>
    public class TutorialSesionTests
    {
        const float Dt = 1f / 60f;

        readonly List<Object> creados = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in creados)
                if (o != null)
                    Object.DestroyImmediate(o);
            creados.Clear();
        }

        GameConfig Config()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 650f;
            creados.Add(c);
            return c;
        }

        LevelRuntime Nivel(params LevelEntry[] entradas)
        {
            var d = ScriptableObject.CreateInstance<LevelDefinition>();
            d.entries.AddRange(entradas);
            creados.Add(d);
            return new LevelRuntime(d);
        }

        static Paso P(CondicionPaso c, float cantidad = 1f, bool suelta = false) =>
            new Paso { condicion = c, cantidad = cantidad, texto = c.ToString(), hordaSuelta = suelta };

        /// <summary>Un cuadro, en el orden del juego. Devuelve los eventos del Tick.</summary>
        static RunEvent Cuadro(TutorialSesion sesion, RunSimulation sim, LevelRuntime nivel, PlayerIntent intent)
        {
            sesion.AntesDelTick(Dt);
            var ev = RunEvent.None;
            if (sim.State.Phase == RunPhase.Running)
                ev = RunController.StepSimulation(sim, nivel, intent, Dt);
            sesion.DespuesDelTick(ev, Dt);
            return ev;
        }

        [Test]
        public void PickupDeBateriaConLaBateriaAlMaximo_CompletaElPaso()
        {
            var cfg = Config();
            var sim = new RunSimulation(cfg);
            var nivel = Nivel(new LevelEntry(20f, 1, LevelEntryKind.Battery));
            sim.Barrels = nivel;
            var sesion = new TutorialSesion(sim, nivel, new List<Paso> { P(CondicionPaso.PickupBateria), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            Assert.AreEqual(cfg.batteryMax, sim.State.Battery, "arranca llena");
            for (int i = 0; i < 300 && sesion.Progreso.Indice == 0; i++)
                Cuadro(sesion, sim, nivel, PlayerIntent.Idle);

            Assert.AreEqual(1, sesion.Progreso.Indice, "agarrar la bateria cuenta aunque el valor no suba");
            Assert.AreEqual(cfg.batteryMax, sim.State.Battery, "la bateria sigue al maximo");
            Assert.IsTrue(nivel.Items[0].Consumed);
        }

        [Test]
        public void PickupDeOtroTipoNoCompletaElPasoDeBateria()
        {
            var sim = new RunSimulation(Config());
            var nivel = Nivel(new LevelEntry(20f, 1, LevelEntryKind.Fuel));
            var sesion = new TutorialSesion(sim, nivel, new List<Paso> { P(CondicionPaso.PickupBateria), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            for (int i = 0; i < 180; i++)
                Cuadro(sesion, sim, nivel, PlayerIntent.Idle);

            Assert.IsTrue(nivel.Items[0].Consumed, "el bidon se agarro");
            Assert.AreEqual(0, sesion.Progreso.Indice, "pero no es una bateria");
        }

        [Test]
        public void LaHordaTenidaConservaEventosLooksYCarriles()
        {
            var sim = new RunSimulation(Config());
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.TurboSostenido, 100f), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            Cuadro(sesion, sim, nivel, PlayerIntent.Idle);
            var generaciones = new int[sim.Horde.Units.Length];
            var carriles = new int[sim.Horde.Units.Length];
            for (int i = 0; i < generaciones.Length; i++)
            {
                generaciones[i] = sim.Horde.Units[i].Generation;
                carriles[i] = sim.Horde.Units[i].Lane;
            }

            // Muchos cuadros de horda tenida: la distancia se mantiene y nadie se regenera (con
            // HordeSimulation.Reset cada cuadro, cada zombie cambiaba de Generation y de carril).
            for (int i = 0; i < 120; i++)
                Cuadro(sesion, sim, nivel, PlayerIntent.Idle);
            sesion.AntesDelTick(Dt);
            Assert.AreEqual(60f, sim.State.PlayerX - sim.State.HordeX, 1e-3f);
            for (int i = 0; i < generaciones.Length; i++)
            {
                Assert.AreEqual(generaciones[i], sim.Horde.Units[i].Generation, "no se regenera cada cuadro");
                Assert.AreEqual(carriles[i], sim.Horde.Units[i].Lane, "carriles estables");
            }

            // Un tiro deja eventos en la horda (traza, y muerte, impacto o miss): tienen que
            // sobrevivir al DespuesDelTick y al AntesDelTick del cuadro siguiente, que son los
            // que mueven la horda, para que sonido y efectos los lean.
            var ev = RunController.StepSimulation(sim, nivel, new PlayerIntent { Mode = DriveMode.Normal, Fire = true }, Dt);
            int eventos = sim.Horde.Events.Count;
            Assert.Greater(eventos, 0);
            sesion.DespuesDelTick(ev, Dt);
            sesion.AntesDelTick(Dt);
            Assert.AreEqual(eventos, sim.Horde.Events.Count, "mover la horda no borra sus eventos");
        }

        [Test]
        public void EnElPasoDeDisparo_LaHordaQuedaAlAlcanceYUnTiroLaPega()
        {
            var cfg = Config();
            var sim = new RunSimulation(cfg);
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.DisparoAcertado), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            Cuadro(sesion, sim, nivel, PlayerIntent.Idle);
            sesion.AntesDelTick(Dt);
            float gap = sim.State.PlayerX - sim.State.HordeX;
            Assert.AreEqual(AjustesTutorial.PorDefecto.distanciaHordaDisparo, gap, 1e-3f);
            Assert.Less(gap, cfg.shotRangeMeters, "bien adentro del alcance del tiro");

            // Sin cambiar de carril: la horda tiene zombies en los tres.
            var ev = RunController.StepSimulation(sim, nivel, new PlayerIntent { Mode = DriveMode.Normal, Fire = true }, Dt);
            sesion.DespuesDelTick(ev, Dt);

            Assert.AreEqual(RunEvent.Shot, ev & (RunEvent.Shot | RunEvent.ShotMissed), "el tiro pego");
            Assert.AreEqual(1, sesion.Progreso.Indice, "el paso de disparo se cumplio");
        }

        [Test]
        public void ElAlcanceSePerdonaAntesDelTick_SinEmitirLost()
        {
            var cfg = Config();
            var sim = new RunSimulation(cfg);
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel, new List<Paso> { P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);
            string aviso = null;
            sesion.Aviso += t => aviso = t;

            sim.State.PlayerX = 300f;
            sim.Horde.ShiftTo(299.5f);
            sim.State.HordeX = sim.Horde.FrontX;

            var ev = Cuadro(sesion, sim, nivel, PlayerIntent.Idle);

            Assert.AreEqual(RunEvent.None, ev & RunEvent.Lost, "nunca llega a Lost: el motor y el flujo no se enteran");
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);
            Assert.AreEqual(1, sesion.Perdones);
            Assert.AreEqual(TutorialSesion.AlcanzadoTexto, aviso);
            Assert.Greater(sim.State.PlayerX - sim.State.HordeX, 30f, "la horda quedo unos 35 m atras");
        }

        [Test]
        public void UnLostQueSeCuelaIgualSeDeshace()
        {
            var sim = new RunSimulation(Config());
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel, new List<Paso> { P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            sim.State.Phase = RunPhase.Lost; // como un F3 o un dt enorme
            sim.State.Loss = LossReason.CaughtByHorde;
            sesion.DespuesDelTick(RunEvent.Lost, Dt);

            Assert.AreEqual(RunPhase.Running, sim.State.Phase);
            Assert.AreEqual(LossReason.None, sim.State.Loss);
        }

        [Test]
        public void UnWonEnUnPasoQueNoEsElUltimo_NoTerminaElTutorialYRebobina()
        {
            var cfg = Config();
            var sim = new RunSimulation(cfg);
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.TurboSostenido, 1.5f), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);
            Assert.IsFalse(sesion.PermiteGanar);

            // F2 en el editor: a la meta.
            sim.State.PlayerX = cfg.goalDistance;
            sim.State.Phase = RunPhase.Won;
            sesion.DespuesDelTick(RunEvent.None, Dt);

            Assert.IsFalse(sesion.EstaTerminado);
            Assert.AreEqual(0, sesion.Progreso.Indice);
            Assert.AreEqual(RunPhase.Running, sim.State.Phase);
            Assert.AreEqual(cfg.goalDistance - TutorialRewind.RetrocesoMeta, sim.State.PlayerX, 1e-3f);
            Assert.AreEqual(1, sesion.Rebobinados);
        }

        [Test]
        public void UnWonEnElUltimoPaso_TerminaElTutorial()
        {
            var cfg = Config();
            var sim = new RunSimulation(cfg);
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel, new List<Paso> { P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);
            bool terminado = false;
            sesion.Terminado += () => terminado = true;
            Assert.IsTrue(sesion.PermiteGanar);

            sim.State.PlayerX = cfg.goalDistance;
            sim.State.Phase = RunPhase.Won;
            sesion.DespuesDelTick(RunEvent.Won, Dt);

            Assert.IsTrue(sesion.EstaTerminado);
            Assert.IsTrue(terminado);
            Assert.AreEqual(RunPhase.Won, sim.State.Phase);
        }

        [Test]
        public void LaMunicionSeRecargaSoloEnElPasoDeDisparoYEnElUltimo()
        {
            var sim = new RunSimulation(Config());
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso>
                {
                    P(CondicionPaso.TurboSostenido, 100f),
                    P(CondicionPaso.DisparoAcertado),
                    P(CondicionPaso.LlegarMeta, 0f, true),
                },
                AjustesTutorial.PorDefecto);

            sim.State.Ammo = 0;
            sesion.AntesDelTick(Dt);
            Assert.AreEqual(0, sim.State.Ammo, "en un paso cualquiera no se recarga");

            // Otra sesion sobre la misma partida, arrancando en el paso de disparo.
            var s2 = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.DisparoAcertado), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);
            s2.AntesDelTick(Dt);
            Assert.AreEqual(4, sim.State.Ammo, "paso de disparo: 60 % de 6, redondeado");

            var s3 = new TutorialSesion(sim, nivel, new List<Paso> { P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);
            sim.State.Ammo = 0;
            s3.AntesDelTick(Dt);
            Assert.AreEqual(4, sim.State.Ammo, "ultimo paso");
        }

        [Test]
        public void LaNaftaEnCeroSeRecargaAntesDelTick()
        {
            var cfg = Config();
            var sim = new RunSimulation(cfg);
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.TurboSostenido, 100f), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            sim.State.Fuel = 0f;
            var ev = Cuadro(sesion, sim, nivel, PlayerIntent.Idle);

            Assert.AreEqual(RunEvent.None, ev & RunEvent.RanOutOfFuel);
            Assert.Greater(sim.State.Fuel, cfg.fuelMax * 0.59f);
            Assert.Greater(sim.Progress01, 0f, "la moto siguio andando ese mismo cuadro");
        }

        [Test]
        public void NuncaRebobinaEnElAire()
        {
            var cfg = Config();
            var sim = new RunSimulation(cfg);
            var nivel = Nivel(new LevelEntry(90f, 1, LevelEntryKind.Ramp), new LevelEntry(190f, 1, LevelEntryKind.Fuel));
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.Salto), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            // Pasado el margen de la rampa, pero volando (como si hubiera saltado de otra cosa).
            sim.State.PlayerX = 90f + TutorialRewind.MargenRampa + 5f;
            sim.State.Airborne = true;
            sim.State.Height = 2f;
            sesion.DespuesDelTick(RunEvent.None, Dt);
            Assert.AreEqual(0, sesion.Rebobinados);

            sim.State.Airborne = false;
            sim.State.Height = 0f;
            sesion.DespuesDelTick(RunEvent.None, Dt);
            Assert.AreEqual(1, sesion.Rebobinados);
            Assert.AreEqual(60f, sim.State.PlayerX, 1e-3f);
        }
    }
}
