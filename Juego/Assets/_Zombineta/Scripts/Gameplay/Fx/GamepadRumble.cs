using UnityEngine;
using UnityEngine.InputSystem;
using Zombineta.Core;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Fx
{
    /// <summary>
    /// Aplica la vibracion del RumbleDirector al Gamepad activo: escucha los eventos de la
    /// partida, dispara la vibracion de "atrapada" al perder contra la horda (que sigue
    /// sonando durante la camara lenta del final) y apaga todo si la jugadora desactivo la
    /// opcion, el juego perdio el foco, o el flujo ya no esta jugando (y la atrapada termino).
    /// </summary>
    public sealed class GamepadRumble : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] RumbleConfig config;

        RumbleDirector director;
        bool caughtTriggered;
        float appliedLow, appliedHigh;
        bool silenced;
        Gamepad lastPad;

        void Awake() => director = new RumbleDirector(config);

        void OnEnable()
        {
            if (run != null)
                run.Stepped += director.Trigger;
        }

        void OnDisable()
        {
            if (run != null)
                run.Stepped -= director.Trigger;
            Silence(force: true);
        }

        void OnDestroy()
        {
            if (run != null)
                run.Stepped -= director.Trigger;
            Silence(force: true);
        }

        void Update()
        {
            bool caught = run != null && run.Sim != null &&
                          run.Sim.State.Phase == RunPhase.Lost && run.Sim.State.Loss == LossReason.CaughtByHorde;

            if (!caught)
                caughtTriggered = false;

            bool playing = GameRoot.Flow != null && GameRoot.Flow.Current == GameScreen.Playing;

            if (!GameSettings.Vibration || (!playing && !caught))
            {
                Silence();
                return;
            }

            if (caught && !caughtTriggered)
            {
                caughtTriggered = true;
                director.Trigger(RumbleKind.Caught);
            }

            director.Tick(Time.unscaledDeltaTime);
            Apply();
        }

        void Apply()
        {
            if (Mathf.Approximately(director.Low, appliedLow) && Mathf.Approximately(director.High, appliedHigh))
                return;
            appliedLow = director.Low;
            appliedHigh = director.High;

            var pad = Gamepad.current;
            lastPad = pad;
            if (appliedLow > 0f || appliedHigh > 0f)
                silenced = false;
            pad?.SetMotorSpeeds(appliedLow, appliedHigh);
        }

        /// <summary>
        /// Apaga el motor. Se llama cada frame mientras no corresponde vibrar, asi que una vez
        /// aplicado el silencio no repite SetMotorSpeeds/ResetHaptics hasta que algo vuelva a
        /// sonar (Apply lo nota). "force" (OnDisable, OnDestroy, perdida de foco) lo pasa por
        /// alto para garantizar que el control quede en silencio pase lo que pase con la bandera.
        /// </summary>
        void Silence(bool force = false)
        {
            // Siempre se vacia el director (solo memoria): un evento del mismo frame en que se
            // pauso no queda congelado para sonar al seguir.
            director.Stop();
            if (silenced && !force)
                return;
            silenced = true;
            appliedLow = appliedHigh = 0f;

            var pad = lastPad;
            lastPad = null;
            if (pad == null)
                return;
            pad.SetMotorSpeeds(0f, 0f);
            pad.ResetHaptics();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus)
                Silence(force: true);
        }

#if UNITY_EDITOR
        /// <summary>Solo para verificacion desde el editor: lo que esta sonando ahora mismo.</summary>
        public float DebugLow => director != null ? director.Low : 0f;
        public float DebugHigh => director != null ? director.High : 0f;
#endif
    }
}
