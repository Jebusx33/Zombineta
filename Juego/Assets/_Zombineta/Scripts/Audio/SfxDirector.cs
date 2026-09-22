using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;
using Zombineta.Flow;
using Zombineta.Juego.Flow;
using Zombineta.Level;

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
    /// sonido por si solos). Vive en el prefab SonidoDeNivel, como hijo suyo. Busca su
    /// RunController si no esta cableado.
    /// </summary>
    [DefaultExecutionOrder(15)]
    public sealed class SfxDirector : MonoBehaviour
    {
        const float EpsilonRecurso = 0.0001f;

        [SerializeField] RunController run;

        [Tooltip("Distancia (m) por delante de la moto a la que suena ZombieAdelante: cerca, para " +
                 "que la atenuacion espacial no lo deje en silencio (a distanciaMaxima el volumen " +
                 "da 0).")]
        [SerializeField] float distanciaAvisoZombie = 12f;

        readonly HashSet<LevelRuntime.Item> zombieAdelanteSonado = new HashSet<LevelRuntime.Item>();

        int lastHordeEpoch = -1;
        float prevFuel, prevBattery, prevAmmo;
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
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null)
                return;

            EscanearZombiesAdelante();
            EscanearEventosDeHorda();

            var s = run.Sim.State;
            prevFuel = s.Fuel;
            prevBattery = s.Battery;
            prevAmmo = s.Ammo;
        }

        void OnStepped(RunEvent events)
        {
            var audio = AudioDirector.Instance;
            if (audio == null || run == null || run.Sim == null)
                return;
            var state = run.Sim.State;

            if ((events & RunEvent.Shot) != 0) audio.Play(SonidoClave.Disparo);
            if ((events & RunEvent.ShotDenied) != 0) audio.Play(SonidoClave.SinBalas);
            if ((events & RunEvent.LaneChanged) != 0) audio.Play(SonidoClave.CambioCarril);
            if ((events & RunEvent.Crashed) != 0) audio.Play(SonidoClave.Choque, run.ToWorldX(state.PlayerX));
            if ((events & RunEvent.HeadlightOn) != 0) audio.Play(SonidoClave.FaroOn);
            if ((events & RunEvent.HeadlightOff) != 0) audio.Play(SonidoClave.FaroOff);
            if ((events & RunEvent.RanOutOfFuel) != 0) audio.Play(SonidoClave.SinNafta);
            if ((events & RunEvent.Launched) != 0) audio.Play(SonidoClave.Salto);

            // Aterrizaje perfecto pisa al aterrizaje comun cuando vienen juntos en el mismo paso.
            if ((events & RunEvent.LandedPerfect) != 0) audio.Play(SonidoClave.AterrizajePerfecto);
            else if ((events & RunEvent.Landed) != 0) audio.Play(SonidoClave.Aterrizaje);

            if ((events & RunEvent.PickedUp) != 0)
                SonarPickups(audio, state);
        }

        /// <summary>Que recurso subio respecto del paso anterior: uno por cada uno que haya subido.</summary>
        void SonarPickups(AudioDirector audio, RunState state)
        {
            if (state.Fuel > prevFuel + EpsilonRecurso) audio.Play(SonidoClave.PickupNafta);
            if (state.Battery > prevBattery + EpsilonRecurso) audio.Play(SonidoClave.PickupBateria);
            if (state.Ammo > prevAmmo + EpsilonRecurso) audio.Play(SonidoClave.PickupMunicion);
        }

        void EscanearZombiesAdelante()
        {
            var audio = AudioDirector.Instance;
            if (audio == null || run.Level == null)
                return;

            float playerX = run.Sim.State.PlayerX;
            var items = run.Level.Items;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.Entry.kind != LevelEntryKind.ZombieFront)
                    continue;
                if (zombieAdelanteSonado.Contains(item))
                    continue; // ya sonado: no lo vuelve a evaluar cuadro a cuadro.

                float dx = item.Entry.distance - playerX;
                if (!EnRangoDeAviso(dx, distanciaAvisoZombie))
                    continue;

                zombieAdelanteSonado.Add(item);
                audio.Play(SonidoClave.ZombieAdelante, run.ToWorldX(item.Entry.distance));
            }
        }

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

            for (int i = 0; i < horde.Events.Count; i++)
            {
                var e = horde.Events[i];
                if (e.Kind == HordeEventKind.Death && e.Cause == DeathCause.RunOver)
                    audio.Play(SonidoClave.Atropello, run.ToWorldX(e.X));
                else if (e.Kind == HordeEventKind.Explosion)
                    audio.Play(SonidoClave.ExplosionBarril, run.ToWorldX(e.X));
            }
        }

        void OnFlowChanged(GameScreen from, GameScreen to)
        {
            var audio = AudioDirector.Instance;
            if (audio == null)
                return;

            if (to == GameScreen.LevelComplete)
                audio.Play(SonidoClave.Victoria);
            else if (to == GameScreen.GameOver)
                audio.Play(SonidoClave.Derrota);
        }
    }
}
