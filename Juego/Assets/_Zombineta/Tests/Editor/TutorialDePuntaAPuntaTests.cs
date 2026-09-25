using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombineta.Core;
using Zombineta.Juego.Levels;
using Zombineta.Level;
using Zombineta.Player;
using Zombineta.Tutorial;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// El tutorial de punta a punta, sin Play: la misma partida que arma RunController.Awake con
    /// Tutorial.unity (TutorialConfig pasado por LevelScene.RuntimeConfig y el recorrido de
    /// LevelScene.BuildDefinition, los LevelItem de la escena), los pasos de TutorialPasos.asset y
    /// los ajustes del TutorialDirector de la escena. Un "jugador" de test manda PlayerIntent
    /// (lo mismo que produce PlayerInputReader con el teclado o el joystick) a dt fijo, y cada
    /// cuadro corre en el orden del juego: AntesDelTick, RunController.StepSimulation,
    /// DespuesDelTick. Asi se prueba que cada paso se puede cumplir jugando, no forzando el
    /// progreso.
    /// </summary>
    public class TutorialDePuntaAPuntaTests
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/Tutorial.unity";
        const float Dt = 1f / 60f;
        const int MaxCuadros = 60 * 400;

        Scene escena;
        bool yaEstabaCargada;
        readonly List<Object> creados = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            var yaCargada = SceneManager.GetSceneByPath(ScenePath);
            yaEstabaCargada = yaCargada.isLoaded;
            escena = yaEstabaCargada ? yaCargada : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in creados)
                if (o != null)
                    Object.DestroyImmediate(o);
            creados.Clear();
            if (!yaEstabaCargada)
                EditorSceneManager.CloseScene(escena, true);
        }

        T Buscar<T>() where T : Component
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                var found = raiz.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            return null;
        }

        static object Campo(object o, string nombre) =>
            o.GetType().GetField(nombre, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);

        /// <summary>Lo que registra una corrida.</summary>
        sealed class Corrida
        {
            public RunSimulation sim;
            public LevelRuntime nivel;
            public TutorialSesion sesion;
            public readonly List<int> pasosCumplidos = new List<int>();
            public readonly List<string> avisos = new List<string>();
            public float bateriaAlCumplirPaso6 = -1f;
            public float gapAlCumplirPaso9 = -1f;
            public bool terminado;
            public float xAlTerminar;
            public int cuadros;
            public int lostEmitidos;
            public int cuadrosSeguidosSinNafta, maxSinNafta;
            public int cuadrosSeguidosSinBateria, maxSinBateria;
            public int cuadrosSeguidosSinBalas, maxSinBalas;
        }

        Corrida Armar()
        {
            var levelScene = Buscar<LevelScene>();
            var run = Buscar<RunController>();
            var director = Buscar<TutorialDirector>();
            Assert.IsNotNull(levelScene);
            Assert.IsNotNull(run);
            Assert.IsNotNull(director);

            // Igual que RunController.Awake.
            var config = levelScene.RuntimeConfig((GameConfig)Campo(run, "config"));
            var definicion = levelScene.BuildDefinition();
            creados.Add(config);
            creados.Add(definicion);

            var pasos = (TutorialPasos)Campo(director, "pasos");
            Assert.IsNotNull(pasos);
            Assert.AreEqual(10, pasos.pasos.Count);

            var c = new Corrida();
            c.sim = new RunSimulation(config);
            c.nivel = new LevelRuntime(definicion);
            c.sim.Barrels = c.nivel;
            c.sesion = new TutorialSesion(c.sim, c.nivel, pasos.pasos, director.Ajustes);
            c.sesion.PasoCambiado += () =>
            {
                c.pasosCumplidos.Add(c.sesion.Progreso.Indice - 1);
                if (c.sesion.Progreso.Indice == 6)
                    c.bateriaAlCumplirPaso6 = c.sim.State.Battery;
                if (c.sesion.Progreso.Indice == 9)
                    c.gapAlCumplirPaso9 = c.sim.State.Gap; // el cuadro del tiro que pego
            };
            c.sesion.Terminado += () =>
            {
                c.pasosCumplidos.Add(c.sesion.Progreso.Indice - 1);
                c.terminado = true;
                c.xAlTerminar = c.sim.State.PlayerX;
            };
            c.sesion.Aviso += t => c.avisos.Add(t);
            return c;
        }

        /// <summary>
        /// Un jugador que lee el cartel y hace lo que pide. Mientras "demora" sea mayor a 0
        /// segundos no toca nada (un lector lento): la moto sigue sola.
        /// </summary>
        sealed class Jugador
        {
            public float demora;

            /// <summary>Segundos que va de reversa al empezar el paso final, para que la horda lo alcance.</summary>
            public float reversaEnElFinal;

            int cuadrosDesdeTiro;
            int cuadrosDesdeCarril;

            public PlayerIntent Decidir(TutorialSesion sesion, RunState s)
            {
                var intent = PlayerIntent.Idle;
                if (demora > 0f)
                {
                    demora -= Dt;
                    return intent;
                }

                cuadrosDesdeTiro++;
                cuadrosDesdeCarril++;
                var paso = sesion.Actual;
                if (paso == null)
                    return intent;

                switch (paso.condicion)
                {
                    case CondicionPaso.TurboSostenido:
                        intent.Mode = DriveMode.Turbo;
                        break;
                    case CondicionPaso.ReversaSostenida:
                        intent.Mode = DriveMode.Reverse;
                        break;
                    case CondicionPaso.CambioCarrilAmbos:
                        if (cuadrosDesdeCarril >= 15)
                        {
                            // Arriba desde el medio, despues abajo.
                            intent.LaneDelta = s.Lane < 2 ? 1 : -1;
                            cuadrosDesdeCarril = 0;
                        }
                        break;
                    case CondicionPaso.FaroSostenido:
                        if (!s.HeadlightOn)
                            intent.ToggleHeadlight = true;
                        break;
                    case CondicionPaso.DisparoAcertado:
                        ApagarFaro(ref intent, s);
                        Disparar(ref intent);
                        break;
                    case CondicionPaso.LlegarMeta:
                        if (reversaEnElFinal > 0f)
                        {
                            reversaEnElFinal -= Dt;
                            intent.Mode = DriveMode.Reverse;
                            ApagarFaro(ref intent, s);
                            break;
                        }
                        intent.Mode = DriveMode.Turbo;
                        if (s.Gap < 25f)
                        {
                            Disparar(ref intent);
                            if (!s.HeadlightOn)
                                intent.ToggleHeadlight = true;
                        }
                        break;
                    default:
                        // Rampa y pickups: seguir andando. Todo el recorrido del tutorial tiene
                        // sus items en los tres carriles.
                        ApagarFaro(ref intent, s);
                        break;
                }
                return intent;
            }

            void Disparar(ref PlayerIntent intent)
            {
                if (cuadrosDesdeTiro < 20)
                    return;
                intent.Fire = true;
                cuadrosDesdeTiro = 0;
            }

            static void ApagarFaro(ref PlayerIntent intent, RunState s)
            {
                if (s.HeadlightOn)
                    intent.ToggleHeadlight = true;
            }
        }

        static void Jugar(Corrida c, Jugador jugador)
        {
            var s = c.sim.State;
            for (c.cuadros = 0; c.cuadros < MaxCuadros && !c.terminado; c.cuadros++)
            {
                var intent = jugador.Decidir(c.sesion, s);

                // El orden del juego: TutorialDirector.Update (-150), RunController.Update
                // (-100), TutorialDirector.LateUpdate.
                c.sesion.AntesDelTick(Dt);
                var eventos = RunEvent.None;
                if (s.Phase == RunPhase.Running)
                    eventos = RunController.StepSimulation(c.sim, c.nivel, intent, Dt);
                if ((eventos & RunEvent.Lost) != 0)
                    c.lostEmitidos++;
                c.sesion.DespuesDelTick(eventos, Dt);

                var actual = c.sesion.Actual;
                Assert.AreNotEqual(RunPhase.Lost, s.Phase, "cuadro " + c.cuadros + ": en el tutorial nunca se pierde");
                if (!c.terminado)
                    Assert.AreEqual(RunPhase.Running, s.Phase, "cuadro " + c.cuadros + ": sin terminar, la partida sigue");

                Contar(s.Fuel <= 0f, ref c.cuadrosSeguidosSinNafta, ref c.maxSinNafta);
                Contar(s.Battery <= 0f, ref c.cuadrosSeguidosSinBateria, ref c.maxSinBateria);
                bool pasoConBalas = actual != null &&
                                    (actual.condicion == CondicionPaso.DisparoAcertado || actual.hordaSuelta);
                Contar(pasoConBalas && s.Ammo <= 0, ref c.cuadrosSeguidosSinBalas, ref c.maxSinBalas);
            }
        }

        static void Contar(bool enCero, ref int seguidos, ref int max)
        {
            seguidos = enCero ? seguidos + 1 : 0;
            if (seguidos > max)
                max = seguidos;
        }

        static void VerificarCompleto(Corrida c)
        {
            Assert.IsTrue(c.terminado, "el tutorial termino (" + c.sesion.Progreso.Indice + " pasos, x=" +
                                       c.sim.State.PlayerX + ", " + c.cuadros + " cuadros)");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, c.pasosCumplidos, "los 10 pasos, en orden");
            Assert.AreEqual(RunPhase.Won, c.sim.State.Phase, "termina ganando, en el refugio");
            Assert.GreaterOrEqual(c.xAlTerminar, c.sim.Config.goalDistance);
            Assert.AreEqual(0, c.lostEmitidos, "la simulacion nunca llego a Lost (ni transitorio)");
            Assert.LessOrEqual(c.maxSinNafta, 1, "la nafta nunca queda en 0 (se recarga el cuadro siguiente)");
            Assert.LessOrEqual(c.maxSinBateria, 1, "la bateria nunca queda en 0");
            Assert.LessOrEqual(c.maxSinBalas, 1, "en los pasos de disparo la municion nunca queda en 0");
        }

        [Test]
        public void UnJugadorQueHaceLoQueDiceElCartel_CompletaLosDiezPasosYLlegaAlRefugio()
        {
            var c = Armar();
            Jugar(c, new Jugador());

            VerificarCompleto(c);

            // C1: la bateria arranca llena y recien el faro la gasta; el paso 6 se cumple igual.
            Assert.AreEqual(c.sim.Config.batteryMax, c.bateriaAlCumplirPaso6, 1e-3f,
                "el paso 6 se cumplio agarrando una bateria con la bateria al maximo");

            // C2: en el paso de disparo la horda esta cerca, al alcance del tiro hacia atras.
            Assert.Greater(c.gapAlCumplirPaso9, 0f);
            Assert.Less(c.gapAlCumplirPaso9, c.sim.Config.shotRangeMeters * 0.5f);
        }

        [Test]
        public void UnJugadorLentoRebobinaYIgualCompleta()
        {
            var c = Armar();
            // 52 s sin tocar nada con el primer cartel: la moto anda sola, pasa por todos los
            // tramos (se come rampas, bidones, baterias y balas) y llega al guardia de la meta.
            Jugar(c, new Jugador { demora = 52f });

            VerificarCompleto(c);
            Assert.GreaterOrEqual(c.sesion.Rebobinados, 2, "rebobino por la meta y por los tramos que se paso");
            CollectionAssert.Contains(c.avisos, TutorialSesion.RebobinadoTexto);
            CollectionAssert.Contains(c.avisos, TutorialSesion.RecargaNaftaTexto, "se quedo sin nafta esperando");
        }

        [Test]
        public void SiLaHordaLoAlcanzaEnElFinal_SePerdonaSinLlegarALostYIgualCompleta()
        {
            var c = Armar();
            // En el paso final va de reversa unos segundos: la horda (que arranca a 15 m, donde
            // quedo en el paso de disparo) lo alcanza.
            Jugar(c, new Jugador { reversaEnElFinal = 6f });

            VerificarCompleto(c);
            Assert.GreaterOrEqual(c.sesion.Perdones, 1, "la horda lo alcanzo al menos una vez");
            CollectionAssert.Contains(c.avisos, TutorialSesion.AlcanzadoTexto);
        }

        [Test]
        public void ElPasoDeDisparoPideDispararleALaHorda()
        {
            var director = Buscar<TutorialDirector>();
            var pasos = (TutorialPasos)Campo(director, "pasos");
            var paso9 = pasos.pasos[8];
            Assert.AreEqual(CondicionPaso.DisparoAcertado, paso9.condicion);
            Assert.AreEqual("{Fire} dispara hacia atrás. Pegale a la horda.", paso9.texto);
            Assert.AreEqual(BarraHud.Municion, paso9.resaltar);
            Assert.AreEqual(1f, paso9.cantidad, 1e-4f);
        }
    }
}
