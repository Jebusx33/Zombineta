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
        const int CantidadDePasos = 9;

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
            public List<Paso> pasos;
            public readonly List<int> pasosCumplidos = new List<int>();
            public readonly List<string> avisos = new List<string>();
            public float gapAlDisparar = -1f;
            public bool terminado;
            public float xAlTerminar;
            public float metaAlTerminar;
            public float xAlEmpezarElFinal = -1f;
            public int cuadros;
            public int lostEmitidos;
            public int cuadrosSeguidosSinNafta, maxSinNafta;
            public int cuadrosSeguidosSinBateria, maxSinBateria;
            public int cuadrosSeguidosSinBalas, maxSinBalas;
            /// <summary>Lo mas que retrocedio la moto de un cuadro al siguiente, en metros.</summary>
            public float retrocesoMaximo;
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
            Assert.AreEqual(CantidadDePasos, pasos.pasos.Count);

            var c = new Corrida { pasos = pasos.pasos };
            c.sim = new RunSimulation(config);
            c.nivel = new LevelRuntime(definicion);
            c.sim.Barrels = c.nivel;
            c.sesion = new TutorialSesion(c.sim, c.nivel, pasos.pasos, director.Ajustes);
            c.sesion.PasoCambiado += () =>
            {
                int cumplido = c.sesion.Progreso.Indice - 1;
                c.pasosCumplidos.Add(cumplido);
                if (c.pasos[cumplido].condicion == CondicionPaso.DisparoAcertado)
                    c.gapAlDisparar = c.sim.State.Gap; // el cuadro del tiro que pego
                if (c.sesion.EsUltimoPaso)
                    c.xAlEmpezarElFinal = c.sim.State.PlayerX;
            };
            c.sesion.Terminado += () =>
            {
                c.pasosCumplidos.Add(c.sesion.Progreso.Indice - 1);
                c.terminado = true;
                c.xAlTerminar = c.sim.State.PlayerX;
                c.metaAlTerminar = c.sim.Meta;
            };
            c.sesion.Aviso += t => c.avisos.Add(t);
            return c;
        }

        /// <summary>
        /// Un jugador que lee el cartel y hace lo que pide: va al carril del objeto que trae el
        /// paso, dispara, tira la granada. Mientras "demora" sea mayor a 0 segundos no toca nada
        /// (un lector lento): la moto sigue sola.
        /// </summary>
        sealed class Jugador
        {
            public float demora;

            /// <summary>Segundos que va de reversa al empezar el paso final, para que la horda lo alcance.</summary>
            public float reversaEnElFinal;

            /// <summary>
            /// Cuantos objetos deja pasar de largo (para ver que aparece otro). Lo que se agarra igual le
            /// llega por el iman de los pickups: lo que deja pasar es la rampa, esquivandola.
            /// </summary>
            public int objetosQueDeja;

            int cuadrosDesdeTiro;
            int cuadrosDesdeCarril;

            public PlayerIntent Decidir(TutorialSesion sesion, LevelRuntime nivel, RunState s)
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
                        if (Listo())
                            intent.Fire = true;
                        break;
                    case CondicionPaso.Granada:
                        ApagarFaro(ref intent, s);
                        if (Listo())
                            intent.Granada = true;
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
                        if (s.Gap < 25f && Listo())
                            intent.Fire = true;
                        break;
                    default:
                        // Rampa y pickups: ir al carril del objeto que trajo el paso.
                        ApagarFaro(ref intent, s);
                        IrAlObjeto(ref intent, sesion, nivel, s);
                        break;
                }
                return intent;
            }

            void IrAlObjeto(ref PlayerIntent intent, TutorialSesion sesion, LevelRuntime nivel, RunState s)
            {
                if (sesion.Colocado < 0 || s.Airborne)
                    return;
                int carril = nivel.Items[sesion.Colocado].Entry.lane;
                if (sesion.Repuestos < objetosQueDeja)
                {
                    // La esquiva: se va a otro carril.
                    if (carril == s.Lane && cuadrosDesdeCarril >= 15)
                    {
                        intent.LaneDelta = s.Lane < 2 ? 1 : -1;
                        cuadrosDesdeCarril = 0;
                    }
                    return;
                }
                if (carril != s.Lane && cuadrosDesdeCarril >= 15)
                {
                    intent.LaneDelta = carril > s.Lane ? 1 : -1;
                    cuadrosDesdeCarril = 0;
                }
            }

            bool Listo()
            {
                if (cuadrosDesdeTiro < 20)
                    return false;
                cuadrosDesdeTiro = 0;
                return true;
            }

            static void ApagarFaro(ref PlayerIntent intent, RunState s)
            {
                if (s.HeadlightOn)
                    intent.ToggleHeadlight = true;
            }
        }

        static void Jugar(Corrida c, Jugador jugador, int maxCuadros = MaxCuadros)
        {
            var s = c.sim.State;
            for (c.cuadros = 0; c.cuadros < maxCuadros && !c.terminado; c.cuadros++)
            {
                var intent = jugador.Decidir(c.sesion, c.nivel, s);
                float antes = s.PlayerX;

                // El orden del juego: TutorialDirector.Update (-150), RunController.Update
                // (-100), TutorialDirector.LateUpdate.
                c.sesion.AntesDelTick(Dt);
                var eventos = RunEvent.None;
                if (s.Phase == RunPhase.Running)
                    eventos = RunController.StepSimulation(c.sim, c.nivel, intent, Dt);
                if ((eventos & RunEvent.Lost) != 0)
                    c.lostEmitidos++;
                c.sesion.DespuesDelTick(eventos, Dt);
                c.retrocesoMaximo = Mathf.Max(c.retrocesoMaximo, antes - s.PlayerX);

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
            var todos = new List<int>();
            for (int i = 0; i < CantidadDePasos; i++)
                todos.Add(i);
            CollectionAssert.AreEqual(todos, c.pasosCumplidos, "todos los pasos, en orden");
            Assert.AreEqual(RunPhase.Won, c.sim.State.Phase, "termina ganando, en el refugio");
            Assert.GreaterOrEqual(c.xAlTerminar, c.metaAlTerminar);
            Assert.AreEqual(0, c.lostEmitidos, "la simulacion nunca llego a Lost (ni transitorio)");
            Assert.LessOrEqual(c.maxSinNafta, 1, "la nafta nunca queda en 0 (se recarga el cuadro siguiente)");
            Assert.LessOrEqual(c.maxSinBateria, 1, "la bateria nunca queda en 0");
            Assert.LessOrEqual(c.maxSinBalas, 1, "en los pasos de disparo la municion nunca queda en 0");
            Assert.LessOrEqual(c.retrocesoMaximo, 1f, "la moto nunca salta hacia atras");
        }

        [Test]
        public void UnJugadorQueHaceLoQueDiceElCartel_CompletaTodosLosPasosYLlegaAlRefugio()
        {
            var c = Armar();
            Jugar(c, new Jugador());

            VerificarCompleto(c);

            // En el paso de disparo la horda esta cerca, al alcance del tiro hacia atras.
            Assert.Greater(c.gapAlDisparar, 0f);
            Assert.Less(c.gapAlDisparar, c.sim.Config.shotRangeMeters * 0.5f);

            // El refugio aparece recien en el paso final, a unos metros.
            Assert.Greater(c.xAlEmpezarElFinal, 0f);
            Assert.AreEqual(c.xAlEmpezarElFinal + c.sesion.Ajustes.metrosHastaRefugio, c.metaAlTerminar, 1e-3f);

            // Corto: se juega en menos de un minuto y medio.
            Assert.Less(c.cuadros * Dt, 90f, "duracion del tutorial jugado como se pide");
            CollectionAssert.DoesNotContain(c.avisos, TutorialSesion.OtraVezTexto, "nunca se paso nada de largo");
        }

        [Test]
        public void SiDejaPasarUnObjeto_ApareceOtroAdelanteYIgualTermina()
        {
            var c = Armar();
            Jugar(c, new Jugador { objetosQueDeja = 1 });

            VerificarCompleto(c);
            Assert.GreaterOrEqual(c.sesion.Repuestos, 1, "repuso adelante lo que se paso de largo");
            CollectionAssert.Contains(c.avisos, TutorialSesion.OtraVezTexto);
        }

        [Test]
        public void UnJugadorLentoIgualTermina()
        {
            var c = Armar();
            // 12 s sin tocar nada con el primer cartel: la moto anda sola. El paso espera.
            Jugar(c, new Jugador { demora = 12f });

            VerificarCompleto(c);
        }

        [Test]
        public void UnJugadorQueNoHaceNada_NoPierdeYElPasoLoEspera()
        {
            var c = Armar();
            Jugar(c, new Jugador { demora = 1000f }, 60 * 60);

            Assert.IsFalse(c.terminado);
            Assert.AreEqual(0, c.sesion.Progreso.Indice, "sigue esperando el primer paso");
            Assert.AreEqual(RunPhase.Running, c.sim.State.Phase);
            Assert.AreEqual(0, c.lostEmitidos);
            Assert.Less(c.sim.State.PlayerX, c.sim.Meta, "el refugio no aparece sin haber hecho los pasos");
        }

        [Test]
        public void SiLaHordaLoAlcanzaEnElFinal_SePerdonaSinLlegarALostYIgualCompleta()
        {
            var c = Armar();
            // En el paso final va de reversa unos segundos: la horda (que arranca a 12 m, donde
            // quedo en el paso de la granada) lo alcanza.
            Jugar(c, new Jugador { reversaEnElFinal = 6f });

            VerificarCompleto(c);
            Assert.GreaterOrEqual(c.sesion.Perdones, 1, "la horda lo alcanzo al menos una vez");
            CollectionAssert.Contains(c.avisos, TutorialSesion.AlcanzadoTexto);
        }

        [Test]
        public void LosPasosEnsenanLoQueElJuegoPide()
        {
            var director = Buscar<TutorialDirector>();
            var pasos = ((TutorialPasos)Campo(director, "pasos")).pasos;
            var condiciones = new List<CondicionPaso>();
            foreach (var p in pasos)
                condiciones.Add(p.condicion);

            CollectionAssert.AreEqual(new[]
            {
                CondicionPaso.TurboSostenido, CondicionPaso.CambioCarrilAmbos, CondicionPaso.PickupNafta,
                CondicionPaso.Salto, CondicionPaso.PickupBateria, CondicionPaso.FaroSostenido,
                CondicionPaso.DisparoAcertado, CondicionPaso.Granada, CondicionPaso.LlegarMeta,
            }, condiciones);

            // El bidon explica lo central del juego: sin nafta hay que pelear a pie.
            StringAssert.Contains("pelear", pasos[2].texto);
            Assert.AreEqual(BarraHud.Nafta, pasos[2].resaltar);
            Assert.AreEqual(BarraHud.Municion, pasos[6].resaltar);
            // Solo el ultimo suelta la horda.
            for (int i = 0; i < pasos.Count; i++)
                Assert.AreEqual(i == pasos.Count - 1, pasos[i].hordaSuelta, "paso " + i);
        }
    }
}
