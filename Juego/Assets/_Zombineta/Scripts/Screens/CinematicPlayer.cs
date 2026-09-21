using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Flow;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// La cinematica de entrada de un nivel: el titulo y despues las vinetas de comic, que se
    /// suman en la pagina (ver ComicPanel.nuevaPagina), en secuencia y con fundido. Submit pasa
    /// a la siguiente vineta; Cancel saltea todo. Lee que nivel toca del flujo: una sola escena
    /// sirve para todos.
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
                yield return PlayPanels(level.comicPanels);

            Finish();
        }

        readonly List<Image> pageImages = new List<Image>();
        int pageCount;

        IEnumerator PlayPanels(List<ComicPanel> panels)
        {
            if (panels == null || panel == null)
                yield break;
            panel.enabled = false;

            for (int i = 0; i < panels.Count && !skip; i++)
            {
                if (ComicPages.OpensPage(panels, i))
                {
                    if (i > 0)
                        yield return Fade(panelGroup, 0f);
                    ClearPage();
                    SetAlpha(panelGroup, 1f);
                }

                if (panels[i] == null)
                    continue;

                advance = false;
                var img = NextImage(panels[i].image);
                yield return FadeImage(img, 1f);
                advance = false; // un Submit durante el fundido solo lo completa
                for (float t = 0f; t < panels[i].seconds && !advance && !skip; t += Time.unscaledDeltaTime)
                    yield return null;
            }

            yield return Fade(panelGroup, 0f);
        }

        Image NextImage(Sprite sprite)
        {
            Image img;
            if (pageCount < pageImages.Count)
                img = pageImages[pageCount];
            else
            {
                var go = new GameObject("Vineta " + (pageCount + 1), typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(panel.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                img = go.GetComponent<Image>();
                img.preserveAspect = panel.preserveAspect;
                img.raycastTarget = false;
                pageImages.Add(img);
            }
            pageCount++;
            img.transform.SetAsLastSibling();
            img.sprite = sprite;
            img.enabled = sprite != null;
            img.gameObject.SetActive(true);
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        void ClearPage()
        {
            foreach (var img in pageImages)
                img.gameObject.SetActive(false);
            pageCount = 0;
        }

        IEnumerator FadeImage(Image img, float target)
        {
            var c = img.color;
            float start = c.a;
            for (float t = 0f; t < fadeSeconds && !skip && !advance; t += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Lerp(start, target, t / fadeSeconds);
                img.color = c;
                yield return null;
            }
            c.a = target;
            img.color = c;
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
