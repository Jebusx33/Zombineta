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
            c.peleaSinNafta = false;   // el tutorial no tiene pelea
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
        public void ElPasoDeLaBateriaLaDejaBajaYAgarrarlaLoCumple()
        {
            var cfg = Config();
            var sim = new RunSimulation(cfg);
            var nivel = Nivel(new LevelEntry(20f, 1, LevelEntryKind.Battery));
            sim.Barrels = nivel;
            var sesion = new TutorialSesion(sim, nivel, new List<Paso> { P(CondicionPaso.PickupBateria), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            Assert.AreEqual(cfg.batteryMax * AjustesTutorial.PorDefecto.recursoParaJuntar, sim.State.Battery, 1e-3f,
                "arranca baja: se ve en el HUD");
            for (int i = 0; i < 300 && sesion.Progreso.Indice == 0; i++)
                Cuadro(sesion, sim, nivel, PlayerIntent.Idle);

            Assert.AreEqual(1, sesion.Progreso.Indice);
            Assert.IsTrue(nivel.Items[0].Consumed);
        }

        [Test]
        public void PickupDeOtroTipoNoCompletaElPasoDeBateria()
        {
            var sim = new RunSimulation(Config());
            // La bateria del paso va al carril de al lado; un bidon en el de la moto.
            var nivel = Nivel(new LevelEntry(20f, 1, LevelEntryKind.Fuel), new LevelEntry(20f, 2, LevelEntryKind.Battery));
            // La bateria del paso, bien lejos: el iman de los pickups la traeria desde el carril de al lado.
            var ajustes = AjustesTutorial.PorDefecto;
            ajustes.adelanteObjeto = 500f;
            var sesion = new TutorialSesion(sim, nivel, new List<Paso> { P(CondicionPaso.PickupBateria), P(CondicionPaso.LlegarMeta, 0f, true) },
                ajustes);
            nivel.Mover(0, sim.State.PlayerX + 10f);

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
            Assert.AreEqual(AjustesTutorial.PorDefecto.distanciaHordaTenida, sim.State.PlayerX - sim.State.HordeX, 1e-3f);
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
            // Ya en el paso final, con el refugio adelante (el paso lo pone a unos metros de la moto).
            sim.State.PlayerX = 300f;
            var sesion = new TutorialSesion(sim, nivel, new List<Paso> { P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);
            string aviso = null;
            sesion.Aviso += t => aviso = t;

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
        public void UnWonEnUnPasoQueNoEsElUltimo_DejaLosPasosQueFaltanYTerminaSinVolverAtras()
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

            Assert.IsTrue(sesion.EstaTerminado, "la llegada cuenta como la del ultimo paso");
            Assert.AreEqual(RunPhase.Won, sim.State.Phase);
            Assert.AreEqual(cfg.goalDistance, sim.State.PlayerX, 1e-3f, "nunca vuelve atras");
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
        public void LoQueSePasoDeLargo_ApareceAdelante_YNuncaEnElAire()
        {
            var cfg = Config();
            var sim = new RunSimulation(cfg);
            var nivel = Nivel(new LevelEntry(90f, 1, LevelEntryKind.Ramp), new LevelEntry(190f, 1, LevelEntryKind.Fuel));
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.Salto), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);
            string aviso = null;
            sesion.Aviso += t => aviso = t;
            var rampa = System.Array.Find(nivel.Items, i => i.Entry.kind == LevelEntryKind.Ramp);
            float adelante = AjustesTutorial.PorDefecto.adelanteObjeto;
            Assert.AreEqual(sim.State.PlayerX + adelante, rampa.Entry.distance, 1e-3f, "el paso trae su rampa");

            // Pasada la rampa, pero volando (como si hubiera saltado de otra cosa).
            sim.State.PlayerX = rampa.Entry.distance + 10f;
            sim.State.Airborne = true;
            sim.State.Height = 2f;
            sesion.DespuesDelTick(RunEvent.None, Dt);
            Assert.AreEqual(0, sesion.Repuestos);

            sim.State.Airborne = false;
            sim.State.Height = 0f;
            float x = sim.State.PlayerX;
            sesion.DespuesDelTick(RunEvent.None, Dt);
            Assert.AreEqual(1, sesion.Repuestos);
            Assert.AreEqual(TutorialSesion.OtraVezTexto, aviso);
            Assert.AreEqual(x, sim.State.PlayerX, 1e-3f, "la moto no se mueve: la rampa viene a ella");
            Assert.AreEqual(x + adelante, rampa.Entry.distance, 1e-3f);
            Assert.IsFalse(rampa.Consumed);
        }

        [Test]
        public void CadaPasoTraeSuObjeto_LaRampaEnElCarrilDeLaMotoYLoQueSeAgarraEnElDeAlLado()
        {
            var sim = new RunSimulation(Config());
            var nivel = Nivel(
                new LevelEntry(90f, 0, LevelEntryKind.Ramp), new LevelEntry(90f, 1, LevelEntryKind.Ramp),
                new LevelEntry(120f, 1, LevelEntryKind.Fuel), new LevelEntry(120f, 2, LevelEntryKind.Fuel));
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.Salto), P(CondicionPaso.PickupNafta), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            Assert.AreEqual(1, sim.State.Lane, "la moto arranca en el medio");
            var rampa = nivel.Items[sesion.Colocado].Entry;
            Assert.AreEqual(LevelEntryKind.Ramp, rampa.kind);
            Assert.AreEqual(1, rampa.lane, "la rampa, en su carril");
            foreach (var i in nivel.Items)
                if (i.Entry.kind == LevelEntryKind.Fuel)
                    Assert.Greater(i.Entry.distance, 1000f, "lo de los otros pasos queda guardado lejos");

            // Salta: el paso siguiente trae el bidon al carril de al lado.
            sesion.DespuesDelTick(RunEvent.Launched | RunEvent.Landed, Dt);
            Assert.AreEqual(1, sesion.Progreso.Indice);
            var bidon = nivel.Items[sesion.Colocado].Entry;
            Assert.AreEqual(LevelEntryKind.Fuel, bidon.kind);
            Assert.AreEqual(2, bidon.lane, "el bidon, en el carril de al lado: hay que ir a buscarlo");
            Assert.AreEqual(sim.State.PlayerX + AjustesTutorial.PorDefecto.adelanteObjeto, bidon.distance, 1e-3f);
        }

        [Test]
        public void QuienSeSuscribeDespuesSeEnteraDeDondeQuedoCadaObjeto()
        {
            var sim = new RunSimulation(Config());
            var nivel = Nivel(new LevelEntry(55f, 0, LevelEntryKind.Ramp), new LevelEntry(100f, 1, LevelEntryKind.Fuel));
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.PickupNafta), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);

            // La escena se suscribe despues de construir la sesion (TutorialDirector.Start).
            var recibidos = new Dictionary<int, float>();
            sesion.ItemMovido += (i, metros) => recibidos[i] = metros;
            sesion.AvisarPosiciones();

            Assert.AreEqual(nivel.Items.Length, recibidos.Count, "cada objeto");
            for (int i = 0; i < nivel.Items.Length; i++)
                Assert.AreEqual(nivel.Items[i].Entry.distance, recibidos[i], 1e-3f);
            Assert.Greater(recibidos[0], 1000f, "la rampa de la escena, guardada lejos (no queda en su lugar original)");
        }

        [Test]
        public void ElRefugioApareceRecienEnElPasoFinal()
        {
            var sim = new RunSimulation(Config());
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.TurboSostenido, 0.01f), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);
            Assert.AreEqual(TutorialColocacion.MetaLejana, sim.Meta, "mientras tanto el refugio esta lejos");

            sim.State.Mode = DriveMode.Turbo;
            sesion.DespuesDelTick(RunEvent.None, Dt);

            Assert.AreEqual(1, sesion.Progreso.Indice, "paso final");
            Assert.AreEqual(sim.State.PlayerX + AjustesTutorial.PorDefecto.metrosHastaRefugio, sim.Meta, 1e-3f);
        }

        [Test]
        public void EnElPasoDeLaGranadaHayUnaParaTirarYTirarlaLoCumple()
        {
            var sim = new RunSimulation(Config());
            var nivel = Nivel();
            var sesion = new TutorialSesion(sim, nivel,
                new List<Paso> { P(CondicionPaso.Granada), P(CondicionPaso.LlegarMeta, 0f, true) },
                AjustesTutorial.PorDefecto);
            sim.State.Granadas = 0;

            Cuadro(sesion, sim, nivel, PlayerIntent.Idle);
            Assert.GreaterOrEqual(sim.State.Granadas, 1, "siempre hay una para tirar");
            Assert.AreEqual(AjustesTutorial.PorDefecto.distanciaHordaDisparo, sim.State.PlayerX - sim.State.HordeX, 0.5f,
                "la horda cerca, como en el paso de disparo");

            var ev = RunEvent.None;
            for (int i = 0; i < 10 && sesion.Progreso.Indice == 0; i++)
                ev |= Cuadro(sesion, sim, nivel, new PlayerIntent { Mode = DriveMode.Normal, Granada = i == 0 });

            Assert.AreNotEqual(RunEvent.None, ev & RunEvent.GranadaLanzada);
            Assert.AreEqual(1, sesion.Progreso.Indice, "tirar la granada cumple el paso");
        }
    }
}
