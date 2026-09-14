using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Nivel de prueba del esqueleto: se gana con G, se pierde con P y se pausa con la accion
    /// Pausa. Lo reemplaza el gameplay real en los proximos tramos.
    /// </summary>
    public sealed class LevelStub : MonoBehaviour
    {
        [SerializeField] Text label;
        [SerializeField] InputActionReference pauseAction;

        void OnEnable() => pauseAction?.action.Enable();

        void Start()
        {
            if (label != null)
                label.text = gameObject.scene.name.Replace('_', ' ') +
                             "\n\nG: ganar     P: perder     Esc / Start: pausa";
        }

        void Update()
        {
            var root = GameRoot.Instance;
            var flow = GameRoot.Flow;
            if (root == null || flow == null || flow.Current != GameScreen.Playing)
                return;
            if (root.Busy || Time.frameCount == root.LastChangeFrame)
                return;

            var kb = Keyboard.current;
            if (kb != null && kb.gKey.wasPressedThisFrame)
                flow.LevelWon();
            else if (kb != null && kb.pKey.wasPressedThisFrame)
                flow.LevelLost();
            else if (pauseAction != null && pauseAction.action.WasPressedThisFrame())
                flow.Pause();
        }
    }
}
