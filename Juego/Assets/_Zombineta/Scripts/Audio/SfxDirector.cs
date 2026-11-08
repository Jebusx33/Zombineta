using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;
using Zombineta.Flow;
using Zombineta.Juego.Flow;
using Zombineta.Level;
using Zombineta.Luz;
using Zombineta.UI;

namespace Zombineta.Audio
{
    /// <summary>
    /// Traduce los eventos de la partida en sonidos. Escucha RunController.Stepped, el mismo
    /// flujo que usan los efectos visuales (FxManager) y la vibracion (GamepadRumble); para
    /// Atropello y ExplosionBarril lee run.Sim.Horde.Events del mismo Epoch, tambien como
    /// FxManager; para ZombieAdelante recorre run.Level.Items buscando un ZombieFront que haya
    /// entrado dentro de distanciaAvisoZombie (12 m por defecto) por delante de la moto: cerca a
    /// proposito, porque a distanciaMaxima (30 m) la atenuacion espacial ya dio 0 y no se
    /// escuchaba; y escucha GameRoot.Flow.Changed para Victoria y Derrota (Won/Lost no disparan
    /// sonido por si solos). Tambien hace sonar el impacto de bala (de los eventos de la horda),
    /// el corte y la vuelta de la luz (LightingDirector.Oscuridad) y, si el banco del nivel las
    /// tiene, las alarmas de los autos que pasa. Vive en el prefab SonidoDeNivel, como hijo suyo.
    /// Busca su RunController si no esta cableado.
    /// </summary>
    [DefaultExecutionOrder(15)]
    public sealed class SfxDirector : MonoBehaviour
    {
        const float EpsilonRecurso = 0.0001f;
        const float SegundosEntreAvisos = 5f;

        [SerializeField] RunController run;

        [Tooltip("Distancia (m) por delante de la moto a la que suena ZombieAdelante: cerca, para " +
                 "que la atenuacion espacial no lo deje en silencio (a distanciaMaxima el volumen " +
                 "da 0).")]
        [SerializeField] float distanciaAvisoZombie = 12f;

        [Header("Alarmas de autos")]
        [Tooltip("Variante de Obstacle que es un auto (la de Obstaculo_Auto en LevelItemPalette).")]
        [SerializeField] int varianteDeAuto = 0;
        [Tooltip("Metros por delante de la moto a los que un auto puede saltar con la alarma.")]
        [SerializeField] float distanciaAlarma = 10f;
        [Tooltip("Probabilidad de que un auto suene al pasar, si ya paso el descanso entre alarmas.")]
        [Range(0f, 1f)] [SerializeField] float probabilidadAlarma = 0.35f;
        [Tooltip("Segundos de descanso entre dos alarmas (al azar entre los dos): que no suene todo el embotellamiento.")]
        [SerializeField] Vector2 descansoEntreAlarmas = new Vector2(6f, 12f);

        readonly AlertaDeRecurso alertaNafta = new AlertaDeRecurso(IndicadorDeRecurso.Bajo, SegundosEntreAvisos);
        readonly AlertaDeRecurso alertaBateria = new AlertaDeRecurso(IndicadorDeRecurso.Bajo, SegundosEntreAvisos);
        readonly HashSet<LevelRuntime.Item> zombieAdelanteSonado = new HashSet<LevelRuntime.Item>();
        readonly HashSet<LevelRuntime.Item> autosPasados = new HashSet<LevelRuntime.Item>();
        readonly CruceDeApagon apagon = new CruceDeApagon();
        float proximaAlarma;
        // El sonido de largada se debe hasta que la partida arranca a correr; se rearma con cada reintento.
        bool inicioPendiente = true;

        int lastHordeEpoch = -1;
        float prevFuel, prevBattery, prevAmmo;
        int prevGranadas, prevBombas;
        bool explotoUnArma;
        GameFlow flujo;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();
        }

        void OnEnable()
        {
            if (run != null)
            {
                run.Stepped += OnStepped;
                run.Restarted += OnRestarted;
                CachearRecursos();
                inicioPendiente = true;
            }

            flujo = GameRoot.Flow;
            if (flujo != null)
                flujo.Changed += OnFlowChanged;
        }

        void OnDisable()
        {
            if (run != null)
            {
                run.Stepped -= OnStepped;
                run.Restarted -= OnRestarted;
            }
            if (flujo != null)
                flujo.Changed -= OnFlowChanged;
        }

        void OnRestarted()
        {
            CachearRecursos();
            zombieAdelanteSonado.Clear();
            autosPasados.Clear();
            proximaAlarma = 0f;
            inicioPendiente = true;
            lastHordeEpoch = -1;
        }

        void CachearRecursos()
        {
            if (run == null || run.Sim == null)
                return;
            var s = run.Sim.State;
            prevFuel = s.Fuel;
            prevBattery = s.Battery;
            prevAmmo = s.Ammo;
            prevGranadas = s.Granadas;
            prevBombas = s.Bombas;
            alertaNafta.Reiniciar(Fraccion(s.Fuel, run.Config.fuelMax));
            alertaBateria.Reiniciar(Fraccion(s.Battery, run.Config.batteryMax));
            apagon.Reiniciar(LightingDirector.Oscuridad);
        }

        /// <summary>Una vez por partida, en el primer cuadro en carrera: volver a la moto despues de una pelea no cuenta.</summary>
        void SonarLargada(RunState s)
        {
            var audio = AudioDirector.Instance;
            if (!inicioPendiente || audio == null || s.Phase != RunPhase.Running)
                return;
            inicioPendiente = false;
            audio.Play(SonidoClave.InicioDeNivel);
        }

        /// <summary>El corte y la vuelta de la luz, al ritmo de lo que se ve (la oscuridad ya suavizada).</summary>
        void SonarApagon()
        {
            var cambio = apagon.Avanzar(LightingDirector.Oscuridad);
            var audio = AudioDirector.Instance;
            if (audio == null || cambio == CambioDeApagon.Nada)
                return;
            audio.Play(cambio == CambioDeApagon.Empieza ? SonidoClave.ApagonEmpieza : SonidoClave.ApagonTermina);
        }

        static float Fraccion(float valor, float maximo) => maximo <= 0f ? 0f : valor / maximo;

        void AvisarRecursosBajos(RunState s)
        {
            var audio = AudioDirector.Instance;
            if (audio == null || s.Phase != RunPhase.Running)
                return;
            if (alertaNafta.Avanzar(Fraccion(s.Fuel, run.Config.fuelMax), Time.deltaTime))
                audio.Play(SonidoClave.HudNaftaBaja);
            if (alertaBateria.Avanzar(Fraccion(s.Battery, run.Config.batteryMax), Time.deltaTime))
                audio.Play(SonidoClave.HudBateriaBaja);
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            EscanearAdelante();
            EscanearEventosDeHorda();
            SonarApagon();
            explotoUnArma = false;

            var s = run.Sim.State;
            SonarLargada(s);
            AvisarRecursosBajos(s);
            prevFuel = s.Fuel;
            prevBattery = s.Battery;
            prevAmmo = s.Ammo;
            prevGranadas = s.Granadas;
            prevBombas = s.Bombas;
        }

        void OnStepped(RunEvent events)
        {
            var audio = AudioDirector.Instance;
            if (audio == null || run == null || run.Sim == null)
                return;
            var state = run.Sim.State;

            if ((events & RunEvent.Shot) != 0) audio.Play(SonidoClave.Disparo);
            if (SinMunicion(events, state.Granadas, state.Bombas)) audio.Play(SonidoClave.SinBalas);
            if ((events & RunEvent.GranadaExplota) != 0) audio.Play(SonidoClave.GranadaExplota);
            if ((events & RunEvent.BombaPuesta) != 0) audio.Play(SonidoClave.BombaPuesta);
            if ((events & RunEvent.BombaExplota) != 0) audio.Play(SonidoClave.BombaExplota);
            explotoUnArma |= (events & (RunEvent.GranadaExplota | RunEvent.BombaExplota)) != 0;
            if ((events & RunEvent.LaneChanged) != 0) audio.Play(SonidoClave.CambioCarril);
            if ((events & RunEvent.Crashed) != 0) audio.Play(SonidoClave.Choque, run.ToWorldX(state.PlayerX));
            if ((events & RunEvent.HeadlightOn) != 0) audio.Play(SonidoClave.FaroOn);
            if ((events & RunEvent.HeadlightOff) != 0) audio.Play(SonidoClave.FaroOff);
            if ((events & RunEvent.RanOutOfFuel) != 0) audio.Play(SonidoClave.SinNafta);
            if ((events & RunEvent.Launched) != 0) audio.Play(SonidoClave.Salto);

            // Pelea a pie: suena cada golpe de la cadena (el paso lo da el estado), el remate y el dano.
            float aPie = run.ToWorldX(state.PeatonX);
            if ((events & RunEvent.Remate) != 0) audio.Play(SonidoClave.Remate, aPie);
            else if ((events & RunEvent.Golpe) != 0) audio.Play(state.ComboPaso == 2 ? SonidoClave.Golpe2 : SonidoClave.Golpe1, aPie);
            if ((events & RunEvent.GolpeAlAire) != 0) audio.Play(SonidoClave.GolpeAlAire, aPie);
            if ((events & RunEvent.Lastimada) != 0) audio.Play(SonidoClave.Lastimada, aPie);

            // Aterrizaje perfecto pisa al aterrizaje comun cuando vienen juntos en el mismo paso.
            if ((events & RunEvent.LandedPerfect) != 0) audio.Play(SonidoClave.AterrizajePerfecto);
            else if ((events & RunEvent.Landed) != 0) audio.Play(SonidoClave.Aterrizaje);

            if ((events & RunEvent.PickedUp) != 0)
                SonarPickups(audio, state);

            // El jefe, con sonidos prestados hasta que tenga los suyos (ver Audio-lista-de-sonidos.md).
            if ((events & RunEvent.JefeApunta) != 0) audio.Play(SonidoClave.HudBateriaBaja);
            if ((events & RunEvent.JefeDispara) != 0) audio.Play(SonidoClave.Atropello);
            if ((events & RunEvent.JefeImpacto) != 0) audio.Play(SonidoClave.ExplosionBarril);
            if ((events & RunEvent.JefeEmbiste) != 0) audio.Play(SonidoClave.ZombieAdelante);
            if ((events & RunEvent.JefeHerido) != 0) audio.Play(SonidoClave.Golpe1);
            if ((events & RunEvent.JefeRebota) != 0) audio.Play(SonidoClave.GolpeAlAire);
            if ((events & RunEvent.JefeDerrotado) != 0) audio.Play(SonidoClave.BombaExplota);
        }

        /// <summary>
        /// Regla pura del gatillo vacio: sin balas, o sin granadas ni bombas de ese tipo. Una granada o
        /// bomba tambien se niega por otras razones (ya hay una en camino, no queda a quien explotar) y
        /// ahi no suena: no estaba vacio.
        /// </summary>
        public static bool SinMunicion(RunEvent events, int granadas, int bombas) =>
            (events & RunEvent.ShotDenied) != 0 ||
            ((events & RunEvent.GranadaDenegada) != 0 && granadas <= 0) ||
            ((events & RunEvent.BombaDenegada) != 0 && bombas <= 0);

        /// <summary>Que recurso subio respecto del paso anterior: uno por cada uno que haya subido.</summary>
        void SonarPickups(AudioDirector audio, RunState state)
        {
            if (state.Fuel > prevFuel + EpsilonRecurso) audio.Play(SonidoClave.PickupNafta);
            if (state.Battery > prevBattery + EpsilonRecurso) audio.Play(SonidoClave.PickupBateria);
            if (state.Ammo > prevAmmo + EpsilonRecurso) audio.Play(SonidoClave.PickupMunicion);
            if (state.Granadas > prevGranadas) audio.Play(SonidoClave.PickupGranada);
            if (state.Bombas > prevBombas) audio.Play(SonidoClave.PickupBomba);
        }

        /// <summary>Lo que viene por delante: los zombies de frente y los autos que pueden saltar con la alarma.</summary>
        void EscanearAdelante()
        {
            var audio = AudioDirector.Instance;
            if (audio == null || run.Level == null)
                return;

            // Solo los niveles con alarmas en su banco (el Nivel 2): en el resto los autos no suenan.
            bool hayAlarmas = run.Sim.State.Phase == RunPhase.Running && audio.Resolver(SonidoClave.AlarmaDeAuto) != null;
            float playerX = run.Sim.State.PlayerX;
            var items = run.Level.Items;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.Entry.kind == LevelEntryKind.ZombieFront)
                    AvisarZombieAdelante(audio, item, playerX);
                else if (hayAlarmas && item.Entry.kind == LevelEntryKind.Obstacle && item.Entry.variant == varianteDeAuto)
                    TalVezAlarma(audio, item, playerX);
            }
        }

        void AvisarZombieAdelante(AudioDirector audio, LevelRuntime.Item item, float playerX)
        {
            if (zombieAdelanteSonado.Contains(item))
                return; // ya sonado: no lo vuelve a evaluar cuadro a cuadro.

            float dx = item.Entry.distance - playerX;
            if (!EnRangoDeAviso(dx, distanciaAvisoZombie))
                return;

            zombieAdelanteSonado.Add(item);
            audio.Play(SonidoClave.ZombieAdelante, run.ToWorldX(item.Entry.distance));
        }

        /// <summary>Cada auto tira una sola vez, al entrar en rango; si toca, suena desde donde esta.</summary>
        void TalVezAlarma(AudioDirector audio, LevelRuntime.Item item, float playerX)
        {
            if (autosPasados.Contains(item) || !EnRangoDeAviso(item.Entry.distance - playerX, distanciaAlarma))
                return;
            autosPasados.Add(item);

            if (!SuenaAlarma(Random.value, probabilidadAlarma, Time.time, proximaAlarma))
                return;
            proximaAlarma = Time.time + Random.Range(descansoEntreAlarmas.x, descansoEntreAlarmas.y);
            audio.Play(SonidoClave.AlarmaDeAuto, run.ToWorldX(item.Entry.distance));
        }

        /// <summary>Regla pura de la alarma: paso el descanso desde la ultima y el sorteo (0 a 1) cae dentro de la probabilidad.</summary>
        public static bool SuenaAlarma(float azar, float probabilidad, float ahora, float proxima) =>
            ahora >= proxima && azar < probabilidad;

        /// <summary>
        /// Regla pura del impacto de bala: un zombie que muere de un tiro, o uno que lo aguanta. El que
        /// lo aguanta deja un Impact justo despues del trazador de la bala; los golpes a pie tambien
        /// dejan Impact, pero sin trazador antes.
        /// </summary>
        public static bool EsImpactoDeBala(HordeEvent e, bool trasTrazador) =>
            (e.Kind == HordeEventKind.Death && e.Cause == DeathCause.Bullet) ||
            (e.Kind == HordeEventKind.Impact && trasTrazador);

        /// <summary>
        /// Regla pura de disparo de ZombieAdelante: adelante de la moto (dx > 0) y dentro del
        /// aviso (dx <= aviso). A distanciaAvisoZombie = 12 m la atenuacion espacial da ~0,69
        /// (con distanciaPlena=4/distanciaMaxima=30 por defecto), asi que se escucha; a
        /// distanciaMaxima (30 m, el umbral viejo) el volumen ya dio 0.
        /// </summary>
        public static bool EnRangoDeAviso(float dx, float aviso) => dx > 0f && dx <= aviso;

        void EscanearEventosDeHorda()
        {
            var horde = run.Sim.Horde;
            if (horde.Epoch == lastHordeEpoch)
                return;
            lastHordeEpoch = horde.Epoch;

            var audio = AudioDirector.Instance;
            if (audio == null)
                return;

            bool trasTrazador = false;
            for (int i = 0; i < horde.Events.Count; i++)
            {
                var e = horde.Events[i];
                if (EsImpactoDeBala(e, trasTrazador))
                    audio.Play(SonidoClave.ImpactoDeBala, run.ToWorldX(e.X));
                trasTrazador = e.Kind == HordeEventKind.Tracer;

                if (e.Kind == HordeEventKind.Death && e.Cause == DeathCause.RunOver)
                    audio.Play(SonidoClave.Atropello, run.ToWorldX(e.X));
                else if (e.Kind == HordeEventKind.Explosion && !explotoUnArma)
                    audio.Play(SonidoClave.ExplosionBarril, run.ToWorldX(e.X));
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        static void Traza(string mensaje) => Debug.Log("[Sfx] " + mensaje + " (t=" + Time.realtimeSinceStartup.ToString("0.0") + ")");

        void OnFlowChanged(GameScreen from, GameScreen to)
        {
            Traza("cambio de pantalla " + from + " -> " + to);
            var audio = AudioDirector.Instance;
            if (audio == null)
                return;

            if (to == GameScreen.LevelComplete)
                audio.Play(SonidoClave.Victoria);
            else if (to == GameScreen.GameOver)
                audio.Play(SonidoClave.Derrota);
            Traza("sonido de pantalla listo");
        }
    }
}
