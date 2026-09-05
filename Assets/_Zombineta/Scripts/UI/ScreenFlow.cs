using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.UI
{
    /// <summary>
    /// Menu -> Level -> Victoria / Game Over -> Menu, con paneles sobre una sola
    /// escena. Es el flujo del documento de Game Design recortado a lo que el
    /// prototipo necesita: sin seleccion de personaje ni cinematicas.
    /// </summary>
    public sealed class ScreenFlow : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Header("Paneles")]
        [SerializeField] GameObject menuPanel;
        [SerializeField] GameObject hudPanel;
        [SerializeField] GameObject wonPanel;
        [SerializeField] GameObject lostPanel;

        [Header("Textos variables")]
        [SerializeField] Text lostReasonLabel;
        [SerializeField] Text wonTimeLabel;

        enum Screen { Menu, Playing, Won, Lost }

        Screen current = Screen.Menu;

        void Start()
        {
            if (run != null)
                run.Paused = true;
            Show(Screen.Menu);
        }

        void Update()
        {
            if (run == null || run.Sim == null)
                return;

            var kb = Keyboard.current;
            bool confirm = kb != null &&
                           (kb.enterKey.wasPressedThisFrame
                            || kb.numpadEnterKey.wasPressedThisFrame
                            || kb.spaceKey.wasPressedThisFrame);

            switch (current)
            {
                case Screen.Menu:
                    if (confirm)
                    {
                        run.Restart();
                        Show(Screen.Playing);
                    }
                    break;

                case Screen.Playing:
                    if (run.Sim.State.Phase == RunPhase.Won) Show(Screen.Won);
                    else if (run.Sim.State.Phase == RunPhase.Lost) Show(Screen.Lost);
                    break;

                case Screen.Won:
                case Screen.Lost:
                    // R reinicia (lo maneja RunController); Enter vuelve al menu.
                    if (run.Sim.State.Phase == RunPhase.Running)
                        Show(Screen.Playing);
                    else if (confirm)
                    {
                        run.Paused = true;
                        Show(Screen.Menu);
                    }
                    break;
            }
        }

        void Show(Screen screen)
        {
            current = screen;

            if (menuPanel != null) menuPanel.SetActive(screen == Screen.Menu);
            if (hudPanel != null) hudPanel.SetActive(screen == Screen.Playing);
            if (wonPanel != null) wonPanel.SetActive(screen == Screen.Won);
            if (lostPanel != null) lostPanel.SetActive(screen == Screen.Lost);

            if (screen == Screen.Lost && lostReasonLabel != null)
            {
                lostReasonLabel.text = run.Sim.State.Loss == LossReason.OutOfFuel
                    ? "Te quedaste sin nafta y la horda te alcanzo."
                    : "La horda te alcanzo.";
            }

            if (screen == Screen.Won && wonTimeLabel != null)
            {
                wonTimeLabel.text =
                    "Llegaste al refugio en " + run.Sim.State.Elapsed.ToString("0.0") + " s";
            }
        }
    }
}
