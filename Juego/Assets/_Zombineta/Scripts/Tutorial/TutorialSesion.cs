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

        [Tooltip("Metros delante de la moto donde aparece el objeto de cada paso (rampa, bidon, bateria).")]
        public float adelanteObjeto;

        [Tooltip("Metros desde donde empieza el paso final hasta el refugio.")]
        public float metrosHastaRefugio;

        [Tooltip("Cuanto queda en el tanque (o en la bateria) al empezar el paso de juntarlo (0 a 1): " +
                 "se ve bajo en el HUD y el pickup lo llena a la vista.")]
        public float recursoParaJuntar;

        public static AjustesTutorial PorDefecto => new AjustesTutorial
        {
            distanciaHordaTenida = 30f,
            distanciaHordaDisparo = 12f,
            distanciaHordaAlAlcanzar = 35f,
            margenAlcance = 2f,
            umbralAvisoRecurso = 0.2f,
            recargaRecurso = 0.6f,
            adelanteObjeto = 45f,
            metrosHastaRefugio = 130f,
            recursoParaJuntar = 0.3f,
        };
    }

    /// <summary>
    /// La logica cuadro a cuadro del tutorial, en C# plano. El TutorialDirector es solo el
    /// adaptador de Unity: la llama en el orden del cuadro y muestra lo que pide (carteles,
    /// avisos). Un test la puede manejar igual que el juego, con una RunSimulation y un
    /// LevelRuntime reales.
    ///
    /// El tutorial no tiene un recorrido fijo: los objetos de la escena se guardan lejos al
    /// arrancar y cada paso que necesita uno (rampa, bidon, bateria) lo pone delante de la moto,
    /// en un carril claro. Si el jugador se lo pasa, aparece otro adelante. El refugio queda lejos
    /// hasta el paso final, que lo pone a unos metros: nunca se vuelve atras ni se saltean pasos.
    ///
    /// Orden por cuadro:
    /// 1. AntesDelTick (Update del director, -150, antes que RunController -100): recarga los
    ///    recursos en 0, mantiene la horda a su distancia (HordeSimulation.ShiftTo: no borra los
    ///    eventos ni regenera la horda) y, en el paso que la suelta, perdona el alcance ANTES de
    ///    que la simulacion lo convierta en Lost.
    /// 2. El Tick de RunController (RunController.StepSimulation).
    /// 3. DespuesDelTick (LateUpdate del director): arma la EntradaPaso (eventos, carril, items
    ///    agarrados), avanza el paso, repone el objeto si se lo paso de largo y deshace un Lost o
    ///    un Won que se hayan colado.
    /// </summary>
    public sealed class TutorialSesion
    {
        public const string AvisoNaftaTexto = "¡Poca nafta! Agarrá un bidón.";
        public const string AvisoBateriaTexto = "¡Poca batería! Agarrá una batería.";
        public const string RecargaNaftaTexto = "En el juego te quedarías sin nafta: te la recargamos para que sigas.";
        public const string RecargaBateriaTexto = "En el juego te quedarías sin batería: te la recargamos para que sigas.";
        public const string AlcanzadoTexto = "¡Te alcanzaron! Dispará o tirá una granada.";
        public const string FinalTexto = "¡Listo! Ya sabés jugar";
        public const string BienTexto = "¡Bien!";
        public const string OtraVezTexto = "¡Casi! Ahí va otro.";

        readonly RunSimulation sim;
        readonly LevelRuntime level;
        readonly IReadOnlyList<Paso> pasos;
        readonly AjustesTutorial ajustes;
        readonly TutorialProgreso progreso;
        readonly TutorialRecursos recursos;

        // Estado de los items al final del cuadro anterior: un pickup es un false -> true.
        readonly bool[] consumidoPrevio;

        int prevLane;

        // El objeto que el paso actual puso delante de la moto (indice en LevelRuntime.Items), o -1.
        int colocado = -1;

        /// <summary>Pide un aviso en el panel chico.</summary>
        public event Action<string> Aviso;

        /// <summary>Se cumplio un paso y hay uno nuevo (Actual).</summary>
        public event Action PasoCambiado;

        /// <summary>Se cumplio el ultimo paso: el tutorial termino.</summary>
        public event Action Terminado;

        /// <summary>Un item del recorrido cambio de lugar (indice en LevelRuntime.Items, metros nuevos).</summary>
        public event Action<int, float> ItemMovido;

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

            // Todo lo de la escena se guarda lejos: cada paso trae lo suyo. El refugio tambien,
            // hasta el paso final.
            for (int i = 0; i < items.Length; i++)
                MoverItem(i, TutorialColocacion.Guardado + i * 20f);
            sim.FijarMeta(TutorialColocacion.MetaLejana);

            Sincronizar();
            PrepararPaso();
        }

        public TutorialProgreso Progreso => progreso;
        public Paso Actual => progreso.Actual;
        public bool EstaTerminado => progreso.Terminado;

        public bool EsUltimoPaso => !progreso.Terminado && progreso.Indice == pasos.Count - 1;

        /// <summary>
        /// Si llegar al refugio ahora termina el tutorial: solo en el ultimo paso (o ya
        /// terminado). En los demas, un Won (F2) salta al ultimo paso.
        /// </summary>
        public bool PermiteGanar => progreso.Terminado || EsUltimoPaso;

        /// <summary>Cuantas veces puso un objeto delante de la moto (el del paso y los que se paso de largo).</summary>
        public int Colocados { get; private set; }

        /// <summary>Cuantas veces repuso un objeto que el jugador se paso de largo.</summary>
        public int Repuestos { get; private set; }

        /// <summary>Cuantas veces perdono un alcance de la horda.</summary>
        public int Perdones { get; private set; }

        /// <summary>El objeto que el paso actual puso delante de la moto (indice en LevelRuntime.Items), o -1.</summary>
        public int Colocado => colocado;

        public AjustesTutorial Ajustes => ajustes;

        /// <summary>
        /// Avisa por ItemMovido donde quedo cada item. El constructor ya los guardo lejos y puso el del
        /// primer paso, pero quien dibuja (LevelScene.MoverItem) se suscribe despues de construir la sesion:
        /// el director lo llama apenas se suscribe, asi la escena se entera.
        /// </summary>
        public void AvisarPosiciones()
        {
            if (level == null)
                return;
            var items = level.Items;
            for (int i = 0; i < items.Length; i++)
                ItemMovido?.Invoke(i, items[i].Entry.distance);
        }

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

        // --- Lo que cada paso necesita al empezar ---------------------------------------------

        void PrepararPaso()
        {
            colocado = -1;
            var actual = progreso.Actual;
            if (actual == null)
                return;

            var s = sim.State;
            var cfg = sim.Config;

            // El recurso a juntar arranca bajo: se ve en el HUD y el pickup lo llena a la vista.
            if (actual.condicion == CondicionPaso.PickupNafta)
                s.Fuel = Mathf.Min(s.Fuel, cfg.fuelMax * ajustes.recursoParaJuntar);
            if (actual.condicion == CondicionPaso.PickupBateria)
                s.Battery = Mathf.Min(s.Battery, cfg.batteryMax * ajustes.recursoParaJuntar);

            var kind = TutorialColocacion.ItemDe(actual.condicion);
            if (kind.HasValue)
                Colocar(kind.Value, TutorialColocacion.CarrilPara(actual.condicion, s.Lane));

            // El paso final suelta la horda y pone el refugio a la vista.
            if (actual.hordaSuelta)
                sim.FijarMeta(s.PlayerX + ajustes.metrosHastaRefugio);
        }

        /// <summary>Pone un objeto del tipo delante de la moto, en el carril pedido.</summary>
        void Colocar(LevelEntryKind kind, int carril)
        {
            if (level == null)
                return;
            var items = level.Items;
            int elegido = -1;
            for (int i = 0; i < items.Length && elegido < 0; i++)
                if (items[i].Entry.kind == kind && items[i].Entry.lane == carril && items[i].Entry.height <= 0f)
                    elegido = i;
            // Sin uno en ese carril, cualquiera del tipo.
            for (int i = 0; i < items.Length && elegido < 0; i++)
                if (items[i].Entry.kind == kind && items[i].Entry.height <= 0f)
                    elegido = i;
            if (elegido < 0)
                return;

            MoverItem(elegido, sim.State.PlayerX + ajustes.adelanteObjeto);
            colocado = elegido;
            Colocados++;
        }

        void MoverItem(int indice, float metros)
        {
            level.Mover(indice, metros);
            if (indice < consumidoPrevio.Length)
                consumidoPrevio[indice] = false;
            ItemMovido?.Invoke(indice, metros);
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

            var d = recursos.Evaluar(fuel01, battery01, s.Ammo, ConArmas);

            if (d.recargarNafta)
            {
                s.Fuel = cfg.fuelMax * recursos.Recarga;
                Aviso?.Invoke(RecargaNaftaTexto);
            }
            else if (d.avisoNafta && !JuntandoNafta)
                Aviso?.Invoke(AvisoNaftaTexto);

            if (d.recargarBateria)
            {
                s.Battery = cfg.batteryMax * recursos.Recarga;
                Aviso?.Invoke(RecargaBateriaTexto);
            }
            else if (d.avisoBateria && !JuntandoBateria)
                Aviso?.Invoke(AvisoBateriaTexto);

            if (d.recargarMunicion)
                s.Ammo = Mathf.Max(1, Mathf.RoundToInt(cfg.ammoMax * recursos.Recarga));

            // En el paso de la granada (y en el final) siempre hay una para tirar.
            if (ConGranada && s.Granadas <= 0)
                s.Granadas = 1;
        }

        // Mientras el paso es juntar ese recurso, el cartel ya lo dice: el aviso de "poco" sobra.
        bool JuntandoNafta => progreso.Actual != null && progreso.Actual.condicion == CondicionPaso.PickupNafta;
        bool JuntandoBateria => progreso.Actual != null && progreso.Actual.condicion == CondicionPaso.PickupBateria;

        /// <summary>La municion se recarga en 0 en los pasos de disparo, de la granada y en el ultimo.</summary>
        bool ConArmas
        {
            get
            {
                var actual = progreso.Actual;
                return actual != null && (actual.condicion == CondicionPaso.DisparoAcertado ||
                                          actual.condicion == CondicionPaso.Granada || EsUltimoPaso);
            }
        }

        bool ConGranada
        {
            get
            {
                var actual = progreso.Actual;
                return actual != null && (actual.condicion == CondicionPaso.Granada || EsUltimoPaso);
            }
        }

        bool HordaSuelta => progreso.Actual != null && progreso.Actual.hordaSuelta;

        /// <summary>En los pasos de disparo y de granada la horda esta cerca, al alcance y a la vista.</summary>
        float DistanciaTenida()
        {
            var actual = progreso.Actual;
            return actual != null && (actual.condicion == CondicionPaso.DisparoAcertado ||
                                      actual.condicion == CondicionPaso.Granada)
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
                // Llego al refugio sin estar en el ultimo paso (F2): los pasos que faltaban se dejan
                // y la llegada cuenta como la del ultimo.
                if (progreso.SaltarAlUltimo())
                    PasoCambiado?.Invoke();
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
            {
                Aviso?.Invoke(BienTexto);
                PrepararPaso();
                PasoCambiado?.Invoke();
                return;
            }

            if (s.Phase == RunPhase.Running)
                ReponerSiSePaso(s);
        }

        /// <summary>Si el objeto del paso quedo atras sin usarlo, aparece otro adelante.</summary>
        void ReponerSiSePaso(RunState s)
        {
            if (colocado < 0 || level == null)
                return;
            var item = level.Items[colocado];
            if (!TutorialColocacion.SePasoDeLargo(item.Entry.distance, s.PlayerX, item.Consumed, s.Airborne))
                return;

            var actual = progreso.Actual;
            Colocar(item.Entry.kind, TutorialColocacion.CarrilPara(actual.condicion, s.Lane));
            Repuestos++;
            Aviso?.Invoke(OtraVezTexto);
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
    }
}
