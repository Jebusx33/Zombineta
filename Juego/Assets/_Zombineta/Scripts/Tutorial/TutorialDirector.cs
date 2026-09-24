using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Juego.Screens;
using Zombineta.Player;
using Zombineta.UI;

namespace Zombineta.Tutorial
{
    /// <summary>
    /// Encadena los pasos del tutorial sin tocar las reglas del juego (spec
    /// docs/superpowers/specs/2026-09-24-tutorial-design.md, secciones 3 a 6): arma el
    /// EntradaPaso de cada cuadro a partir de RunController, avisa y recarga recursos en 0,
    /// mantiene la horda a raya hasta el ultimo paso y perdona si te alcanza, resalta la barra
    /// del HUD del paso actual y rebobina si un lector lento se paso de largo un tramo entero.
    ///
    /// Orden de ejecucion -150: antes que RunController (-100). Update aplica la recarga de
    /// recursos ANTES del Tick de este cuadro (para que la nafta nunca llegue a 0 en la
    /// simulacion); LateUpdate (siempre despues de todos los Update, sea cual sea su orden) lee
    /// lo que dejo el Tick: eventos, horda, rebobinado y el cartel del paso.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class TutorialDirector : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] TutorialPasos pasos;
        [SerializeField] TutorialCartel cartel;
        [SerializeField] HudView hud;
        [SerializeField] CameraFollow camaraFollow;

        [Header("Horda")]
        [Tooltip("Metros detras del jugador donde se mantiene la horda mientras el paso no la suelta.")]
        [SerializeField] float distanciaHordaTenida = 60f;
        [Tooltip("Metros que se aleja la horda cuando alcanza al jugador en el paso final.")]
        [SerializeField] float distanciaHordaAlAlcanzar = 35f;

        [Header("Recursos")]
        [SerializeField] float umbralAvisoRecurso = 0.2f;
        [SerializeField] float recargaRecurso = 0.6f;

        [Header("Tiempos")]
        [SerializeField] float duracionAviso = 2.5f;

        // --- Textos verbatim del spec (§4-§5) ---------------------------------------------

        const string AvisoNaftaTexto = "¡Poca nafta! Agarrá un bidón.";
        const string AvisoBateriaTexto = "¡Poca batería! Agarrá una batería.";
        const string RecargaNaftaTexto = "En el juego te quedarías sin nafta: te la recargamos para que sigas.";
        const string RecargaBateriaTexto = "En el juego te quedarías sin batería: te la recargamos para que sigas.";
        const string AlcanzadoTexto = "¡Te alcanzaron! Dispará o usá el faro";
        const string FinalTexto = "¡Listo! Ya sabés jugar";
        const string RebobinadoTexto = "Volvamos a intentarlo.";

        TutorialProgreso progreso;
        TutorialRecursos recursos;
        InputDeviceTracker tracker;

        RunEvent eventosDelCuadro;
        int prevLane;
        float prevFuel, prevBattery;
        int prevAmmo;

        ControlScheme esquemaActual;
        bool finalMostrado;

        void OnEnable()
        {
            if (run != null)
                run.Stepped += AcumularEventos;

            tracker = InputDeviceTracker.Shared;
            if (tracker != null)
            {
                esquemaActual = tracker.Current;
                tracker.Changed += OnEsquemaCambiado;
            }
        }

        void OnDisable()
        {
            if (run != null)
                run.Stepped -= AcumularEventos;
            if (tracker != null)
                tracker.Changed -= OnEsquemaCambiado;
            tracker = null;
        }

        void Start()
        {
            progreso = new TutorialProgreso(pasos != null ? (IReadOnlyList<Paso>)pasos.pasos : new List<Paso>());
            recursos = new TutorialRecursos(umbralAvisoRecurso, recargaRecurso);

            if (run != null && run.Sim != null)
            {
                var s = run.Sim.State;
                prevLane = s.Lane;
                prevFuel = s.Fuel;
                prevBattery = s.Battery;
                prevAmmo = s.Ammo;
            }

            MostrarPasoActual();
        }

        void AcumularEventos(RunEvent e) => eventosDelCuadro |= e;

        void OnEsquemaCambiado(ControlScheme scheme)
        {
            esquemaActual = scheme;
            if (!finalMostrado && progreso != null && progreso.Actual != null && cartel != null)
                cartel.MostrarPasoInmediato(ControlHints.Resolver(progreso.Actual.texto, esquemaActual));
        }

        // --- Update (-150, antes que RunController): recarga los recursos ANTES del Tick -------

        void Update()
        {
            if (run == null || run.Sim == null || progreso == null)
                return;

            var state = run.Sim.State;
            if (state.Phase != RunPhase.Running)
                return;

            AplicarRecarga(state);
        }

        void AplicarRecarga(RunState state)
        {
            var cfg = run.Config;
            float fuel01 = cfg.fuelMax > 0f ? state.Fuel / cfg.fuelMax : 0f;
            float battery01 = cfg.batteryMax > 0f ? state.Battery / cfg.batteryMax : 0f;
            bool recargaMunicion = EsPasoDeRecargaMunicion();

            var d = recursos.Evaluar(fuel01, battery01, state.Ammo, recargaMunicion);

            if (d.recargarNafta)
            {
                state.Fuel = cfg.fuelMax * recursos.Recarga;
                cartel.MostrarAviso(RecargaNaftaTexto, duracionAviso);
            }
            else if (d.avisoNafta)
            {
                cartel.MostrarAviso(AvisoNaftaTexto, duracionAviso);
            }

            if (d.recargarBateria)
            {
                state.Battery = cfg.batteryMax * recursos.Recarga;
                cartel.MostrarAviso(RecargaBateriaTexto, duracionAviso);
            }
            else if (d.avisoBateria)
            {
                cartel.MostrarAviso(AvisoBateriaTexto, duracionAviso);
            }

            if (d.recargarMunicion)
                state.Ammo = Mathf.Max(1, Mathf.RoundToInt(cfg.ammoMax * recursos.Recarga));
        }

        bool EsPasoDeRecargaMunicion() =>
            progreso.Indice == 8 || progreso.Indice == 9;

        // --- LateUpdate: siempre despues de todos los Update de este cuadro (incluido el Tick) -

        void LateUpdate()
        {
            if (run == null || run.Sim == null || progreso == null)
                return;

            var state = run.Sim.State;

            ActualizarHorda(state);

            if (!finalMostrado && state.Phase == RunPhase.Won)
            {
                finalMostrado = true;
                cartel.Resaltar(null);
                cartel.MostrarPaso(FinalTexto);
                eventosDelCuadro = RunEvent.None;
                return;
            }

            if (state.Phase != RunPhase.Running)
            {
                eventosDelCuadro = RunEvent.None;
                return;
            }

            var entrada = ConstruirEntrada(state);
            bool avanzo = !progreso.Terminado && progreso.Avanzar(entrada);
            ActualizarCache(state);
            eventosDelCuadro = RunEvent.None;

            if (avanzo)
                MostrarPasoActual();

            var actual = progreso.Actual;
            if (cartel != null)
                cartel.Resaltar(actual != null ? RectsDe(actual.resaltar) : null);

            if (actual != null)
                AplicarRebobinadoSiHaceFalta(state, actual);
        }

        void ActualizarHorda(RunState state)
        {
            bool suelta = progreso.Terminado || (progreso.Actual != null && progreso.Actual.hordaSuelta);

            if (!suelta)
            {
                run.Sim.Horde.Reset(state.PlayerX - distanciaHordaTenida);
                state.HordeX = run.Sim.Horde.FrontX;
                return;
            }

            if (state.Phase == RunPhase.Lost)
            {
                // Nunca hay Game Over en el tutorial: se perdona, se aleja la horda y se repite
                // el paso final. Cubre tanto CaughtByHorde como un OutOfFuel que se hubiera
                // colado (la recarga de Update ya deberia haberlo evitado, pero si pasa se
                // deshace igual que el alcance de la horda).
                state.Phase = RunPhase.Running;
                state.Loss = LossReason.None;
                run.Sim.Horde.Reset(state.PlayerX - distanciaHordaAlAlcanzar);
                state.HordeX = run.Sim.Horde.FrontX;
                if (cartel != null)
                    cartel.MostrarAviso(AlcanzadoTexto, duracionAviso);
            }
        }

        EntradaPaso ConstruirEntrada(RunState state)
        {
            int deltaCarril = state.Lane - prevLane;
            bool acierto = (eventosDelCuadro & RunEvent.Shot) != 0 && (eventosDelCuadro & RunEvent.ShotMissed) == 0;

            return new EntradaPaso
            {
                dt = Time.deltaTime,
                eventos = eventosDelCuadro,
                modo = state.Mode,
                faroPrendido = state.HeadlightOn,
                deltaCarril = deltaCarril,
                nafta = state.Fuel,
                naftaPrevia = prevFuel,
                bateria = state.Battery,
                bateriaPrevia = prevBattery,
                municion = state.Ammo,
                municionPrevia = prevAmmo,
                meta = state.Phase == RunPhase.Won,
                acierto = acierto,
            };
        }

        void ActualizarCache(RunState state)
        {
            prevLane = state.Lane;
            prevFuel = state.Fuel;
            prevBattery = state.Battery;
            prevAmmo = state.Ammo;
        }

        void MostrarPasoActual()
        {
            if (cartel == null || progreso.Actual == null)
                return;
            cartel.MostrarPaso(ControlHints.Resolver(progreso.Actual.texto, esquemaActual));
        }

        RectTransform[] RectsDe(BarraHud barra)
        {
            if (hud == null)
                return null;

            switch (barra)
            {
                case BarraHud.Nafta: return Uno(hud.FuelBarRect);
                case BarraHud.Bateria: return Uno(hud.BatteryBarRect);
                case BarraHud.Municion: return hud.AmmoPipRects;
                case BarraHud.Amenaza: return Uno(hud.ThreatBarRect);
                default: return null;
            }
        }

        static RectTransform[] Uno(RectTransform t) => t != null ? new[] { t } : null;

        // --- Rebobinado: nunca dejar que la moto (que avanza sola) se salteé un tramo entero ---

        void AplicarRebobinadoSiHaceFalta(RunState state, Paso actual)
        {
            bool esUltimo = pasos != null && progreso.Indice == pasos.pasos.Count - 1;

            List<float> distancias = null;
            var kind = TutorialRewind.ItemDe(actual.condicion);
            if (kind.HasValue && run.Level != null)
            {
                distancias = new List<float>();
                foreach (var item in run.Level.Items)
                    if (item.Entry.kind == kind.Value)
                        distancias.Add(item.Entry.distance);
            }

            var decision = TutorialRewind.Decidir(
                actual.condicion, esUltimo, distancias, state.PlayerX, run.Config.goalDistance);

            if (!decision.Rebobinar)
                return;

            // Las mismas escrituras que "Probar desde aca": PlayerX y la horda detras suyo.
            state.PlayerX = decision.PlayerX;
            run.Sim.Horde.Reset(state.PlayerX - distanciaHordaTenida);
            state.HordeX = run.Sim.Horde.FrontX;

            if (decision.LimpiarItems && kind.HasValue && run.Level != null)
                foreach (var item in run.Level.Items)
                    if (item.Entry.kind == kind.Value)
                        item.Consumed = false;

            if (camaraFollow != null)
                camaraFollow.SnapNextFrame();

            if (cartel != null)
                cartel.MostrarAviso(RebobinadoTexto, duracionAviso);
        }
    }
}
