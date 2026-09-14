using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Core;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>Capa de opciones. Escribe GameSettings apenas cambia algo: no hay "aplicar".</summary>
    public sealed class OptionsScreen : ScreenBase
    {
        [SerializeField] Slider volume;
        [SerializeField] Toggle fullscreen;
        [SerializeField] Toggle cameraEffects;
        [SerializeField] Toggle cameraShake;
        [SerializeField] Toggle gore;
        [SerializeField] InputActionReference cancelAction;

        void Awake()
        {
            volume?.onValueChanged.AddListener(v => { GameSettings.Volume = v; GameRoot.Instance?.ApplySettings(); });
            fullscreen?.onValueChanged.AddListener(v => { GameSettings.Fullscreen = v; GameRoot.Instance?.ApplySettings(); });
            cameraEffects?.onValueChanged.AddListener(v => GameSettings.CameraEffects = v);
            cameraShake?.onValueChanged.AddListener(v => GameSettings.CameraShake = v);
            gore?.onValueChanged.AddListener(v => GameSettings.Gore = v);
        }

        void OnEnable()
        {
            cancelAction?.action.Enable();
            volume?.SetValueWithoutNotify(GameSettings.Volume);
            fullscreen?.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            cameraEffects?.SetIsOnWithoutNotify(GameSettings.CameraEffects);
            cameraShake?.SetIsOnWithoutNotify(GameSettings.CameraShake);
            gore?.SetIsOnWithoutNotify(GameSettings.Gore);
        }

        protected override void Update()
        {
            base.Update();
            if (InputConsumed || cancelAction == null)
                return;
            if (GameRoot.Instance.TopScene == gameObject.scene.name && cancelAction.action.WasPressedThisFrame())
                Back();
        }

        public void Back() => Flow?.Back();
    }
}
