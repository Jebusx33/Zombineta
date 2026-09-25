using System;
using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;

namespace Zombineta.Tutorial
{
    /// <summary>Numeros del director que usa la sesion. Los valores por defecto son los del spec.</summary>
    [Serializable]
    public struct AjustesTutorial
    {
        [Tooltip("Metros detras del jugador donde se mantiene la horda mientras el paso no la suelta.")]
        public float distanciaHordaTenida;

        [Tooltip("Metros detras del jugador donde se mantiene la horda en el paso de disparo: " +
                 "dentro del alcance del tiro y a la vista en pantalla.")]
        public float distanciaHordaDisparo;

        [Tooltip("Metros que se aleja la horda cuando alcanza al jugador en el paso final.")]
        public float distanciaHordaAlAlcanzar;

        [Tooltip("Con la horda a menos de esto (mas lo que puede cerrar en el cuadro) se perdona el " +
                 "alcance antes del Tick, asi la simulacion nunca llega a Lost.")]
        public float margenAlcance;

        public float umbralAvisoRecurso;
        public float recargaRecurso;

        public static AjustesTutorial PorDefecto => new AjustesTutorial
        {
            distanciaHordaTenida = 60f,
            distanciaHordaDisparo = 12f,
            distanciaHordaAlAlcanzar = 35f,
            margenAlcance = 2f,
            umbralAvisoRecurso = 0.2f,
            recargaRecurso = 0.6f,
        };
    }

    /// <summary>
    /// La logica cuadro a cuadro del tutorial, en C# plano (spec
    /// docs/superpowers/specs/2026-09-24-tutorial-design.md, secciones 3 a 5). El
    /// TutorialDirector es solo el adaptador de Unity: la llama en el orden del cuadro y muestra
    /// lo que pide (carteles, avisos, camara). Un test la puede manejar igual que el juego,
    /// con una RunSimulation y un LevelRuntime reales.
    ///
    /// Orden por cuadro:
    /// 1. AntesDelTick (Update del director, -150, antes que RunController -100): recarga los
    ///    recursos en 0, mantiene la horda a su distancia (HordeSimulation.ShiftTo: no borra los
    ///    eventos ni regenera la horda) y, en el paso que la suelta, perdona el alcance ANTES de
    ///    que la simulacion lo convierta en Lost.
    /// 2. El Tick de RunController (RunController.StepSimulation).
    /// 3. DespuesDelTick (LateUpdate del director): arma la EntradaPaso (eventos, carril, items
    ///    agarrados), avanza el paso, rebobina si hace falta y deshace un Lost o un Won que se
    ///    hayan colado.
    /// </summary>
    public sealed class TutorialSesion
    {
        // --- Textos verbatim del spec (secciones 4 y 5) -------------------------------------

        public const string AvisoNaftaTexto = "¡Poca nafta! Agarrá un bidón.";
        public const string AvisoBateriaTexto = "¡Poca batería! Agarrá una batería.";
        public const string RecargaNaftaTexto = "En el juego te quedarías sin nafta: te la recargamos para que sigas.";
        public const string RecargaBateriaTexto = "En el juego te quedarías sin batería: te la recargamos para que sigas.";
        public const string AlcanzadoTexto = "¡Te alcanzaron! Dispará o usá el faro";
        public const string FinalTexto = "¡Listo! Ya sabés jugar";
        public const string RebobinadoTexto = "Volvamos a intentarlo.";

        readonly RunSimulation sim;
        readonly LevelRuntime level;
        readonly IReadOnlyList<Paso> pasos;
        readonly AjustesTutorial ajustes;
        readonly TutorialProgreso progreso;
        readonly TutorialRecursos recursos;

        // Estado de los items al final del cuadro anterior: un pickup es un false -> true.
        readonly bool[] consumidoPrevio;

        // Distancias de los items por tipo, y de todos los demas, para el rebobinado. El
        // recorrido no cambia durante el tutorial: se calculan una vez.
        readonly Dictionary<LevelEntryKind, List<float>> distanciasPorTipo = new Dictionary<LevelEntryKind, List<float>>();
        readonly Dictionary<LevelEntryKind, List<float>> otrasPorTipo = new Dictionary<LevelEntryKind, List<float>>();

        int prevLane;

        /// <summary>Pide un aviso en el panel chico (texto del spec).</summary>
        public event Action<string> Aviso;

        /// <summary>Se cumplio un paso y hay uno nuevo (Actual).</summary>
        public event Action PasoCambiado;

        /// <summary>Se cumplio el ultimo paso: el tutorial termino.</summary>
        public event Action Terminado;

        /// <summary>El jugador se movio de golpe (rebobinado): la camara tiene que saltar.</summary>
        public event Action Rebobinado;

        public TutorialSesion(RunSimulation sim, LevelRuntime level, IReadOnlyList<Paso> pasos, AjustesTutorial ajustes)
        {
            this.sim = sim ?? throw new ArgumentNullException(nameof(sim));
            this.level = level;
            this.pasos = pasos ?? new List<Paso>();
            this.ajustes = ajustes;
            progreso = new TutorialProgreso(this.pasos);
            recursos = new TutorialRecursos(ajustes.umbralAvisoRecurso, ajustes.recargaRecurso);

            var items = level != null ? level.Items : new LevelRuntime.Item[0];
            consumidoPrevio = new bool[items.Length];
            PrepararDistancias(items);
            Sincronizar();
        }

        public TutorialProgreso Progreso => progreso;
        public Paso Actual => progreso.Actual;
        public bool EstaTerminado => progreso.Terminado;

        public bool EsUltimoPaso => !progreso.Terminado && progreso.Indice == pasos.Count - 1;

        /// <summary>
        /// Si llegar al refugio ahora termina el tutorial: solo en el ultimo paso (o ya
        /// terminado). En los demas, un Won (F2, un dt enorme) se deshace y se rebobina.
        /// </summary>
        public bool PermiteGanar => progreso.Terminado || EsUltimoPaso;

        /// <summary>Cuantas veces rebobino (para tests y depuracion).</summary>
        public int Rebobinados { get; private set; }

        /// <summary>Cuantas veces perdono un alcance de la horda.</summary>
        public int Perdones { get; private set; }

        public AjustesTutorial Ajustes => ajustes;

        /// <summary>
        /// Toma como punto de partida el estado actual (carril e items). Lo llama el constructor
        /// y el director cuando la partida se reinicia sin recargar la escena.
        /// </summary>
        public void Sincronizar()
        {
            prevLane = sim.State.Lane;
            var items = level != null ? level.Items : null;
            if (items == null)
                return;
            for (int i = 0; i < consumidoPrevio.Length && i < items.Length; i++)
                consumidoPrevio[i] = items[i].Consumed;
        }

        // --- 1. Antes del Tick ------------------------------------------------------------

        public void AntesDelTick(float dt)
        {
            if (progreso.Terminado)
                return;

            var s = sim.State;
            if (s.Phase != RunPhase.Running)
                return;

            AplicarRecarga(s);

            if (!HordaSuelta)
            {
                MoverHorda(s.PlayerX - DistanciaTenida());
                return;
            }

            // Paso que suelta la horda: si en este Tick te puede alcanzar, se perdona antes, asi
            // la simulacion nunca emite Lost (el motor, la camara y el flujo lo tratarian como un
            // Game Over). La condicion de la simulacion es HordeX >= PlayerX (CheckEndConditions).
            if (s.PlayerX - s.HordeX < MargenAlcance(dt))
                Perdonar(s);
        }

        void AplicarRecarga(RunState s)
        {
            var cfg = sim.Config;
            float fuel01 = cfg.fuelMax > 0f ? s.Fuel / cfg.fuelMax : 0f;
            float battery01 = cfg.batteryMax > 0f ? s.Battery / cfg.batteryMax : 0f;

            var d = recursos.Evaluar(fuel01, battery01, s.Ammo, RecargaMunicion);

            if (d.recargarNafta)
            {
                s.Fuel = cfg.fuelMax * recursos.Recarga;
                Aviso?.Invoke(RecargaNaftaTexto);
            }
            else if (d.avisoNafta)
                Aviso?.Invoke(AvisoNaftaTexto);

            if (d.recargarBateria)
            {
                s.Battery = cfg.batteryMax * recursos.Recarga;
                Aviso?.Invoke(RecargaBateriaTexto);
            }
            else if (d.avisoBateria)
                Aviso?.Invoke(AvisoBateriaTexto);

            if (d.recargarMunicion)
                s.Ammo = Mathf.Max(1, Mathf.RoundToInt(cfg.ammoMax * recursos.Recarga));
        }

        /// <summary>La municion se recarga en 0 en el paso de disparo y en el ultimo (spec: pasos 9 y 10).</summary>
        bool RecargaMunicion
        {
            get
            {
                var actual = progreso.Actual;
                return actual != null && (actual.condicion == CondicionPaso.DisparoAcertado || EsUltimoPaso);
            }
        }

        bool HordaSuelta => progreso.Actual != null && progreso.Actual.hordaSuelta;

        float DistanciaTenida()
        {
            var actual = progreso.Actual;
            return actual != null && actual.condicion == CondicionPaso.DisparoAcertado
                ? ajustes.distanciaHordaDisparo
                : ajustes.distanciaHordaTenida;
        }

        /// <summary>
        /// El margen fijo mas lo mas que puede cerrarse la distancia en un Tick de dt: el zombie
        /// mas rapido con toda la goma elastica, contra la moto yendo de reversa.
        /// </summary>
        float MargenAlcance(float dt)
        {
            var cfg = sim.Config;
            float multMax = 0f;
            var units = sim.Horde.Units;
            for (int i = 0; i < units.Length; i++)
                if (units[i].Alive)
                    multMax = Mathf.Max(multMax, sim.Horde.Type(units[i].Type).speedMultiplier);

            float horda = (cfg.hordeBaseSpeed + Mathf.Max(0f, cfg.rubberBandMaxBonus)) * multMax;
            float moto = Mathf.Max(0f, -cfg.normalSpeed * cfg.reverseSpeedMultiplier);
            return ajustes.margenAlcance + (horda + moto) * Mathf.Max(0f, dt);
        }

        void Perdonar(RunState s)
        {
            MoverHorda(s.PlayerX - ajustes.distanciaHordaAlAlcanzar);
            Perdones++;
            Aviso?.Invoke(AlcanzadoTexto);
        }

        void MoverHorda(float frente)
        {
            sim.Horde.ShiftTo(frente);
            sim.State.HordeX = sim.Horde.FrontX;
        }

        // --- 3. Despues del Tick ----------------------------------------------------------

        /// <param name="eventos">Los eventos del Tick de este cuadro (RunController.Stepped).</param>
        /// <param name="dt">El dt del cuadro (el mismo que uso el Tick).</param>
        public void DespuesDelTick(RunEvent eventos, float dt)
        {
            if (progreso.Terminado)
                return;

            var s = sim.State;

            if (s.Phase == RunPhase.Lost)
            {
                // Red de seguridad: AntesDelTick ya perdona el alcance antes de que pase, pero si
                // un Lost se cuela igual (un dt enorme, un F3), en el tutorial nunca hay Game Over.
                s.Phase = RunPhase.Running;
                s.Loss = LossReason.None;
                if (HordaSuelta)
                    Perdonar(s);
                else
                    MoverHorda(s.PlayerX - DistanciaTenida());
            }

            if (s.Phase == RunPhase.Won && !PermiteGanar)
            {
                // Llego al refugio sin estar en el ultimo paso (F2, o un salto de tiempo que se
                // comio el guardia de la meta): no cuenta. Se deshace y se vuelve como el guardia.
                s.Phase = RunPhase.Running;
                Rebobinar(s, Mathf.Max(0f, s.PlayerX - TutorialRewind.RetrocesoMeta), null);
                ContarPickups(out _, out _, out _);
                prevLane = s.Lane;
                return;
            }

            if (s.Phase != RunPhase.Running && s.Phase != RunPhase.Won)
                return;

            ContarPickups(out int nafta, out int bateria, out int municion);

            var entrada = new EntradaPaso
            {
                dt = dt,
                eventos = eventos,
                modo = s.Mode,
                faroPrendido = s.HeadlightOn,
                deltaCarril = s.Lane - prevLane,
                pickNafta = nafta,
                pickBateria = bateria,
                pickMunicion = municion,
                meta = s.Phase == RunPhase.Won,
                acierto = (eventos & RunEvent.Shot) != 0 && (eventos & RunEvent.ShotMissed) == 0,
            };
            prevLane = s.Lane;

            bool avanzo = progreso.Avanzar(entrada);

            if (progreso.Terminado)
            {
                Terminado?.Invoke();
                return;
            }

            if (avanzo)
                PasoCambiado?.Invoke();

            if (s.Phase == RunPhase.Running)
                RebobinarSiHaceFalta(s);
        }

        /// <summary>Cuenta los items que pasaron de Consumed false a true desde el cuadro anterior.</summary>
        void ContarPickups(out int nafta, out int bateria, out int municion)
        {
            nafta = bateria = municion = 0;
            if (level == null)
                return;

            var items = level.Items;
            for (int i = 0; i < consumidoPrevio.Length && i < items.Length; i++)
            {
                bool ahora = items[i].Consumed;
                if (ahora && !consumidoPrevio[i])
                {
                    switch (items[i].Entry.kind)
                    {
                        case LevelEntryKind.Fuel: nafta++; break;
                        case LevelEntryKind.Battery: bateria++; break;
                        case LevelEntryKind.Ammo: municion++; break;
                    }
                }
                consumidoPrevio[i] = ahora;
            }
        }

        // --- Rebobinado: nunca dejar que la moto (que avanza sola) se saltee un tramo entero --

        void RebobinarSiHaceFalta(RunState s)
        {
            var actual = progreso.Actual;
            if (actual == null)
                return;

            var kind = TutorialRewind.ItemDe(actual.condicion);
            List<float> distancias = null, otras = null;
            if (kind.HasValue)
            {
                distanciasPorTipo.TryGetValue(kind.Value, out distancias);
                otrasPorTipo.TryGetValue(kind.Value, out otras);
            }

            var decision = TutorialRewind.Decidir(
                actual.condicion, EsUltimoPaso, distancias, otras, s.PlayerX, sim.Config.goalDistance, s.Airborne);

            if (decision.Rebobinar)
                Rebobinar(s, decision.PlayerX, decision.LimpiarItems ? kind : null);
        }

        /// <summary>Las mismas escrituras que "Probar desde aca": PlayerX, y la horda detras suyo.</summary>
        void Rebobinar(RunState s, float playerX, LevelEntryKind? limpiar)
        {
            s.PlayerX = playerX;
            MoverHorda(s.PlayerX - DistanciaTenida());

            if (limpiar.HasValue && level != null)
            {
                var items = level.Items;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].Entry.kind != limpiar.Value)
                        continue;
                    items[i].Consumed = false;
                    if (i < consumidoPrevio.Length)
                        consumidoPrevio[i] = false;
                }
            }

            Rebobinados++;
            Rebobinado?.Invoke();
            Aviso?.Invoke(RebobinadoTexto);
        }

        void PrepararDistancias(LevelRuntime.Item[] items)
        {
            foreach (LevelEntryKind kind in new[]
                     {
                         LevelEntryKind.Ramp, LevelEntryKind.Fuel, LevelEntryKind.Battery, LevelEntryKind.Ammo,
                     })
            {
                var propias = new List<float>();
                var otras = new List<float>();
                foreach (var item in items)
                    (item.Entry.kind == kind ? propias : otras).Add(item.Entry.distance);
                distanciasPorTipo[kind] = propias;
                otrasPorTipo[kind] = otras;
            }
        }
    }
}
