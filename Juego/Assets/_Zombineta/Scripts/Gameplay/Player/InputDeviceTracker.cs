using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Zombineta.Player
{
    public enum ControlScheme { KeyboardMouse, Gamepad }

    /// <summary>
    /// Recuerda con que se jugo por ultima vez (teclado/mouse o joystick) mirando las acciones que
    /// se ejecutan. La ayuda de controles lo usa para mostrar los botones correctos.
    /// </summary>
    public sealed class InputDeviceTracker
    {
        public static InputDeviceTracker Shared { get; private set; }

        public ControlScheme Current { get; private set; } = ControlScheme.KeyboardMouse;
        public event Action<ControlScheme> Changed;

        bool attached;

        // Se crea y adjunta dos veces: en SubsystemRegistration (bien temprano) y de nuevo en
        // AfterSceneLoad. El Input System reinicia su propio estado al entrar en Play despues de
        // SubsystemRegistration, lo que puede dejar esa primera suscripcion sin efecto; repetirla
        // en AfterSceneLoad (ya con el nivel cargado) asegura que quede activa para la partida.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSharedEarly() => ResetShared();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ResetSharedAfterSceneLoad() => ResetShared();

        static void ResetShared()
        {
            Shared?.Detach();
            Shared = new InputDeviceTracker();
            Shared.Attach();
        }

#if UNITY_EDITOR
        // En el editor, Shared se desadjunta al volver a Edit mode (no al salir de Play: en ese
        // momento el juego todavia puede estar leyendo Current un frame mas, y de cualquier forma
        // el enganche en AfterSceneLoad recrea y readjunta Shared en la proxima entrada a Play).
        // [InitializeOnLoadMethod] corre una sola vez por dominio, asi que esta suscripcion no se
        // duplica entre sesiones de Play sucesivas.
        [UnityEditor.InitializeOnLoadMethod]
        static void WatchPlayModeInEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.EnteredEditMode)
                Shared?.Detach();
        }
#endif

        public static ControlScheme SchemeOf(InputDevice device) =>
            device is Gamepad || device is Joystick ? ControlScheme.Gamepad : ControlScheme.KeyboardMouse;

        public void Attach()
        {
            if (attached) return;
            InputSystem.onActionChange += OnActionChange;
            attached = true;
        }

        public void Detach()
        {
            if (!attached) return;
            InputSystem.onActionChange -= OnActionChange;
            attached = false;
        }

        void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed || !(obj is InputAction action))
                return;
            var device = action.activeControl?.device;
            if (device == null)
                return;
            var scheme = SchemeOf(device);
            if (scheme == Current)
                return;
            Current = scheme;
            Changed?.Invoke(scheme);
        }
    }
}
