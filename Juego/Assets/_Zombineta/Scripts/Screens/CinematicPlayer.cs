using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// La cinematica de entrada de un nivel: el titulo y despues las vinetas de comic, en
    /// secuencia y con fundido. Submit pasa a la siguiente vineta; Cancel saltea todo.
    /// Lee que nivel toca del flujo: una sola escena sirve para todos.
    /// </summary>
    public sealed class CinematicPlayer : MonoBehaviour
    {
        [SerializeField] Text title;
        [SerializeField] CanvasGroup titleGroup;
        [SerializeField] Image panel;
        [SerializeField] CanvasGroup panelGroup;
        [SerializeField] float titleSeconds = 2f;
        [SerializeField] float fadeSeconds = 0.35f;
        [SerializeField] InputActionReference submitAction;
        [SerializeField] InputActionReference cancelAction;

        bool skip;
        bool advance;

        void OnEnable()
        {
            submitAction?.action.Enable();
            cancelAction?.action.Enable();
        }

        IEnumerator Start()
        {
            // Si se dio Play directo en esta escena, Boot tarda un frame en aparecer.
            while (GameRoot.Instance == null || GameRoot.Flow == null)
                yield return null;

            var level = GameRoot.Instance.CurrentLevel;
            SetAlpha(titleGroup, 0f);
            SetAlpha(panelGroup, 0f);

            if (title != null)
                title.text = level != null ? level.displayName : "";

            yield return Show(titleGroup, titleSeconds);

            if (level != null)
            {
                foreach (var p in level.comicPanels)
                {
                    if (skip)
                        break;
                    if (panel != null)
                        panel.sprite = p.image;
                    yield return Show(panelGroup, p.seconds);
                }
            }

            Finish();
        }

        void Update()
        {
            if (Time.frameCount == (GameRoot.Instance != null ? GameRoot.Instance.LastChangeFrame : -1))
                return;
            if (cancelAction != null && cancelAction.action.WasPressedThisFrame())
                skip = true;
            if (submitAction != null && submitAction.action.WasPressedThisFrame())
                advance = true;
        }

        IEnumerator Show(CanvasGroup group, float seconds)
        {
            advance = false;
            yield return Fade(group, 1f);
            for (float t = 0f; t < seconds && !advance && !skip; t += Time.unscaledDeltaTime)
                yield return null;
            yield return Fade(group, 0f);
        }

        IEnumerator Fade(CanvasGroup group, float target)
        {
            if (group == null)
                yield break;
            float start = group.alpha;
            for (float t = 0f; t < fadeSeconds && !skip; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / fadeSeconds);
                yield return null;
            }
            group.alpha = target;
        }

        void Finish()
        {
            var flow = GameRoot.Flow;
            if (flow != null && flow.Current == GameScreen.Cinematic)
                flow.CinematicFinished();
        }

        static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
                group.alpha = alpha;
        }
    }
}
