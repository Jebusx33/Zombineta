using System.Collections;
using UnityEngine;

namespace Zombineta.Juego.Flow
{
    /// <summary>Negro que cubre la pantalla durante los cambios de escena. Tiempo sin escalar.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        CanvasGroup group;

        CanvasGroup Group => group != null ? group : (group = GetComponent<CanvasGroup>());

        public void SetOpacity(float alpha)
        {
            Group.alpha = alpha;
            // Mientras cubre, frena los clics: nadie aprieta un boton de una escena que se va.
            Group.blocksRaycasts = alpha > 0.01f;
        }

        public IEnumerator FadeTo(float target, float seconds)
        {
            float start = Group.alpha;
            if (seconds <= 0f)
            {
                SetOpacity(target);
                yield break;
            }

            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetOpacity(Mathf.Lerp(start, target, t / seconds));
                yield return null;
            }
            SetOpacity(target);
        }
    }
}
