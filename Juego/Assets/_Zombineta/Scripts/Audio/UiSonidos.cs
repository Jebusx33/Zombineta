using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zombineta.Juego.Flow;

namespace Zombineta.Audio
{
    /// <summary>
    /// En cada pantalla con menu: UiMover cuando cambia la seleccion del EventSystem (pero no en
    /// el primer frame en que se selecciona sola, sin que nadie haya movido nada), UiConfirmar en
    /// Submit y UiVolver en Cancel (solo si sonarAlCancelar, para pantallas sin accion de
    /// volver). Suena por el bus UI, que la pausa no silencia. Solo actua mientras esta escena es
    /// la de arriba, y no repite el sonido de la pantalla anterior en el mismo frame del cambio.
    /// Orden de ejecucion -500: tiene que leer TopScene/LastChangeFrame y el estado de
    /// submit/cancel antes de que el click de este mismo frame cambie de pantalla (EventSystem y
    /// las pantallas corren en el orden por defecto, 0); si UiSonidos corriera despues, un click
    /// que cambia de pantalla ya habria movido LastChangeFrame/TopScene y el sonido se perderia.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class UiSonidos : MonoBehaviour
    {
        [SerializeField] InputActionReference submitAction;
        [SerializeField] InputActionReference cancelAction;

        [Tooltip("Si esta pantalla no tiene accion de volver (Cancel no hace nada), dejar en false " +
                 "para que UiVolver no suene sin motivo. Por defecto true.")]
        [SerializeField] bool sonarAlCancelar = true;

        GameObject ultimaSeleccion;

        void OnEnable()
        {
            submitAction?.action.Enable();
            cancelAction?.action.Enable();
            ultimaSeleccion = null;
        }

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.TopScene != gameObject.scene.name)
                return;

            ActualizarSeleccion();

            if (Time.frameCount == root.LastChangeFrame)
                return; // esta tecla ya sono en la pantalla anterior.

            if (submitAction != null && submitAction.action.WasPressedThisFrame())
                AudioDirector.Instance?.Play(SonidoClave.UiConfirmar);
            if (sonarAlCancelar && cancelAction != null && cancelAction.action.WasPressedThisFrame())
                AudioDirector.Instance?.Play(SonidoClave.UiVolver);
        }

        void ActualizarSeleccion()
        {
            var es = EventSystem.current;
            if (es == null)
                return;

            var actual = es.currentSelectedGameObject;
            if (actual == null || actual.scene != gameObject.scene || !actual.activeInHierarchy)
                return;

            if (actual == ultimaSeleccion)
                return;

            // Null -> algo es la seleccion inicial automatica (ScreenBase la pone sola): no suena.
            if (ultimaSeleccion != null)
                AudioDirector.Instance?.Play(SonidoClave.UiMover);

            ultimaSeleccion = actual;
        }
    }
}
