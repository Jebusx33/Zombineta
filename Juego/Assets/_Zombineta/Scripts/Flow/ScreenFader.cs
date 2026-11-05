using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Zombineta.Juego.Flow
{
    /// <summary>Como tapa la pantalla: un fundido parejo o un circulo que se cierra (iris).</summary>
    public enum Transicion
    {
        Fundido,
        Iris,
    }

    /// <summary>
    /// Negro que cubre la pantalla durante los cambios de escena. Tiempo sin escalar. Con Iris, en vez
    /// de oscurecerse parejo, un circulo transparente se achica hasta cerrarse sobre Centro, y se abre
    /// desde ahi al volver.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        const string RutaIris = "Estilo/Iris";

        CanvasGroup group;
        Image negro;
        Material iris;
        float opacidad;

        CanvasGroup Group => group != null ? group : (group = GetComponent<CanvasGroup>());

        void Awake() => opacidad = Group.alpha;

        /// <summary>Como tapa la pantalla la proxima vez. Se cambia con la pantalla tapada o destapada, no a mitad.</summary>
        public Transicion Modo { get; set; }

        /// <summary>Donde se cierra el iris, de 0 a 1 en cada eje de la pantalla.</summary>
        public Vector2 Centro { get; set; } = new Vector2(0.5f, 0.5f);

        /// <summary>Si esta, el centro del iris se vuelve a pedir en cada cuadro: sigue a la protagonista mientras se cierra.</summary>
        public System.Func<Vector2> Seguir { get; set; }

        public void SetOpacity(float alpha)
        {
            opacidad = alpha;
            bool conIris = Modo == Transicion.Iris && PrepararIris();
            if (negro != null)
                negro.material = conIris ? iris : null;

            if (conIris)
            {
                if (Seguir != null)
                    Centro = Seguir();
                float aspecto = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.7777f;
                iris.SetVector("_Centro", Centro);
                iris.SetFloat("_Aspecto", aspecto);
                iris.SetFloat("_Radio", IrisMath.Radio(alpha, IrisMath.RadioParaCubrir(Centro, aspecto) + 0.02f));
                Group.alpha = alpha > 0.001f ? 1f : 0f;
            }
            else
            {
                Group.alpha = alpha;
            }
            // Mientras cubre, frena los clics: nadie aprieta un boton de una escena que se va.
            Group.blocksRaycasts = alpha > 0.01f;
        }

        bool PrepararIris()
        {
            if (negro == null)
                negro = GetComponentInChildren<Image>(true);
            if (iris == null)
            {
                var shader = Resources.Load<Shader>(RutaIris);
                if (shader != null)
                    iris = new Material(shader) { name = "Iris (runtime)" };
            }
            return negro != null && iris != null;
        }

        public IEnumerator FadeTo(float target, float seconds)
        {
            float start = opacidad;
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

        void OnDestroy()
        {
            if (iris != null)
                Destroy(iris);
        }
    }
}
