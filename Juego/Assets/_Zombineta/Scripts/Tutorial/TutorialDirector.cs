using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Juego.Screens;
using Zombineta.Player;
using Zombineta.UI;

namespace Zombineta.Tutorial
{
    /// <summary>
    /// Adaptador de Unity del tutorial (spec docs/superpowers/specs/2026-09-24-tutorial-design.md,
    /// secciones 3 a 6). La logica de cada cuadro vive en TutorialSesion (C# plano, testeada de
    /// punta a punta); aca solo se la llama en el orden correcto y se muestra lo que pide:
    /// cartel del paso, avisos, resaltado de la barra del HUD y el salto de camara al rebobinar.
    ///
    /// Orden de ejecucion -150: antes que RunController (-100). Update llama a
    /// TutorialSesion.AntesDelTick (recargas, horda tenida y perdon del alcance ANTES del Tick,
    /// asi los eventos del Tick llegan enteros a sonido y efectos y nunca se emite Lost);
    /// LateUpdate (despues de todos los Update) llama a DespuesDelTick con los eventos del Tick.
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
        [Tooltip("Metros detras del jugador donde se mantiene la horda en el paso de disparo: dentro " +
                 "del alcance del tiro (shotRangeMeters) y con los primeros zombies a la vista.")]
        [SerializeField] float distanciaHordaDisparo = 12f;
        [Tooltip("Metros que se aleja la horda cuando alcanza al jugador en el paso final.")]
        [SerializeField] float distanciaHordaAlAlcanzar = 35f;
        [Tooltip("Con la horda a menos de esto (mas lo que puede cerrar en el cuadro) se perdona el " +
                 "alcance antes del Tick.")]
        [SerializeField] float margenAlcance = 2f;

        [Header("Recursos")]
        [SerializeField] float umbralAvisoRecurso = 0.2f;
        [SerializeField] float recargaRecurso = 0.6f;

        [Header("Tiempos")]
        [SerializeField] float duracionAviso = 2.5f;

        TutorialSesion sesion;
        InputDeviceTracker tracker;
        RunEvent eventosDelCuadro;
        ControlScheme esquemaActual;

        // Los rectangulos a resaltar, armados una vez (no en cada cuadro).
        RectTransform[] rectsNafta, rectsBateria, rectsMunicion, rectsAmenaza;

        /// <summary>Los numeros de este director, tal como los usa la sesion.</summary>
        public AjustesTutorial Ajustes => new AjustesTutorial
        {
            distanciaHordaTenida = distanciaHordaTenida,
            distanciaHordaDisparo = distanciaHordaDisparo,
            distanciaHordaAlAlcanzar = distanciaHordaAlAlcanzar,
            margenAlcance = margenAlcance,
            umbralAvisoRecurso = umbralAvisoRecurso,
            recargaRecurso = recargaRecurso,
        };

        /// <summary>
        /// Si llegar al refugio ahora termina el tutorial (solo en el ultimo paso). Lo consulta
        /// LevelFlowBridge antes de arrancar el plano de victoria. Sin sesion, no traba nada.
        /// </summary>
        public bool PermiteGanar => sesion == null || sesion.PermiteGanar;

        /// <summary>La sesion en curso (null antes de Start). Para depurar por la CLI.</summary>
        public TutorialSesion Sesion => sesion;

        void OnEnable()
        {
            if (run != null)
            {
                run.Stepped += AcumularEventos;
                run.Restarted += AlReiniciar;
            }

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
            {
                run.Stepped -= AcumularEventos;
                run.Restarted -= AlReiniciar;
            }
            if (tracker != null)
                tracker.Changed -= OnEsquemaCambiado;
            tracker = null;
        }

        void Start()
        {
            ArmarRects();

            if (run == null || run.Sim == null)
                return;

            var lista = pasos != null ? (IReadOnlyList<Paso>)pasos.pasos : new List<Paso>();
            sesion = new TutorialSesion(run.Sim, run.Level, lista, Ajustes);
            sesion.Aviso += MostrarAviso;
            sesion.PasoCambiado += MostrarPasoActual;
            sesion.Terminado += MostrarFinal;
            sesion.Rebobinado += SaltarCamara;

            MostrarPasoActual();
        }

        void AcumularEventos(RunEvent e) => eventosDelCuadro |= e;

        void AlReiniciar() => sesion?.Sincronizar();

        void OnEsquemaCambiado(ControlScheme scheme)
        {
            esquemaActual = scheme;
            var actual = sesion?.Actual;
            if (actual != null && cartel != null)
                cartel.MostrarPasoInmediato(ControlHints.Resolver(actual.texto, esquemaActual));
        }

        // --- Update (-150, antes que RunController): recargas y horda ANTES del Tick ----------

        void Update()
        {
            sesion?.AntesDelTick(Time.deltaTime);
        }

        // --- LateUpdate: despues de todos los Update de este cuadro (incluido el Tick) --------

        void LateUpdate()
        {
            if (sesion == null)
                return;

            sesion.DespuesDelTick(eventosDelCuadro, Time.deltaTime);
            eventosDelCuadro = RunEvent.None;
        }

        // --- Lo que pide la sesion --------------------------------------------------------

        void MostrarPasoActual()
        {
            var actual = sesion?.Actual;
            if (cartel == null || actual == null)
                return;
            cartel.MostrarPaso(ControlHints.Resolver(actual.texto, esquemaActual));
            cartel.Resaltar(RectsDe(actual.resaltar));
        }

        void MostrarFinal()
        {
            if (cartel == null)
                return;
            cartel.Resaltar(null);
            cartel.MostrarPaso(TutorialSesion.FinalTexto);
        }

        void MostrarAviso(string texto)
        {
            if (cartel != null)
                cartel.MostrarAviso(texto, duracionAviso);
        }

        void SaltarCamara()
        {
            // Sin esto la camara cruzaria en un cuadro todo el tramo rebobinado.
            if (camaraFollow != null)
                camaraFollow.SnapNextFrame();
        }

        void ArmarRects()
        {
            if (hud == null)
                return;
            rectsNafta = Uno(hud.FuelBarRect);
            rectsBateria = Uno(hud.BatteryBarRect);
            rectsMunicion = hud.AmmoPipRects;
            rectsAmenaza = Uno(hud.ThreatBarRect);
        }

        RectTransform[] RectsDe(BarraHud barra)
        {
            switch (barra)
            {
                case BarraHud.Nafta: return rectsNafta;
                case BarraHud.Bateria: return rectsBateria;
                case BarraHud.Municion: return rectsMunicion;
                case BarraHud.Amenaza: return rectsAmenaza;
                default: return null;
            }
        }

        static RectTransform[] Uno(RectTransform t) => t != null ? new[] { t } : null;
    }
}
