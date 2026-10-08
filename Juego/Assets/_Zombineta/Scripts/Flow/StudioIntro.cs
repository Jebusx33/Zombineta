using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using Zombineta.Audio;

namespace Zombineta.Juego.Flow
{
    /// <summary>
    /// La intro del estudio: un video a pantalla completa que se ve una sola vez, al arrancar el
    /// juego desde Boot y antes del menu principal. Vive en Boot y arma su propia pantalla por
    /// codigo (un Canvas por encima del fundido), asi la escena solo necesita este componente
    /// con el video asignado. Cualquier tecla o boton la saltea.
    /// </summary>
    public sealed class StudioIntro : MonoBehaviour
    {
        // Por encima del fundido de Boot (1000): el video tapa el negro, no al reves.
        const int SortingOrder = 2000;

        [SerializeField] VideoClip video;

        [Tooltip("Segundos del fundido a negro al terminar o al saltearla.")]
        [SerializeField] float fadeOutSeconds = 0.3f;

        [Tooltip("Segundos iniciales en los que todavia no se puede saltear: evita que la tecla " +
                 "con la que se abrio el juego la corte.")]
        [SerializeField] float skipGuardSeconds = 0.4f;

        [Tooltip("Si el video no llega a prepararse en estos segundos, se sigue al menu sin mostrarlo.")]
        [SerializeField] float prepareTimeoutSeconds = 4f;

        public bool HasVideo => video != null;

        /// <summary>Reproduce la intro y termina cuando el video acaba, se saltea o falla.</summary>
        public IEnumerator Reproducir()
        {
            if (video == null)
                yield break;

            var textura = new RenderTexture((int)video.width, (int)video.height, 0);
            var canvasGroup = CrearPantalla(textura, (float)video.width / video.height);

            var audioGo = new GameObject("Audio");
            audioGo.transform.SetParent(canvasGroup.transform, false);
            var audio = audioGo.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            // Por el bus de musica: respeta los volumenes de Opciones.
            audioGo.AddComponent<FuenteConBus>().bus = AudioBus.Musica;

            // En Boot todavia no hay nadie escuchando (el AudioListener viene con la camara del
            // menu, que carga despues): sin uno, el video se reproduce mudo. Este se va con la
            // intro, antes de que llegue el del menu.
            if (FindAnyObjectByType<AudioListener>() == null)
                audioGo.AddComponent<AudioListener>();

            var player = canvasGroup.gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.source = VideoSource.VideoClip;
            player.clip = video;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = textura;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, audio);

            bool termino = false, fallo = false;
            player.loopPointReached += _ => termino = true;
            player.errorReceived += (_, mensaje) =>
            {
                Debug.LogWarning("StudioIntro: no se pudo reproducir la intro (" + mensaje + ").", this);
                fallo = true;
            };

            player.Prepare();
            for (float t = 0f; !player.isPrepared && !fallo && t < prepareTimeoutSeconds; t += Time.unscaledDeltaTime)
                yield return null;

            if (player.isPrepared && !fallo)
            {
                player.Play();

                // Tope por si el fin del video nunca avisa: su duracion y un margen.
                float tope = (float)video.length + 2f;
                for (float t = 0f; !termino && !fallo && t < tope; t += Time.unscaledDeltaTime)
                {
                    if (t >= skipGuardSeconds && SkipPressed())
                        break;
                    yield return null;
                }

                // A negro (el fundido de Boot esta debajo), bajando tambien el sonido.
                float volumen = audio.GetComponent<FuenteConBus>().volumenPropio;
                for (float t = 0f; t < fadeOutSeconds; t += Time.unscaledDeltaTime)
                {
                    float k = 1f - t / fadeOutSeconds;
                    canvasGroup.alpha = k;
                    audio.GetComponent<FuenteConBus>().volumenPropio = volumen * k;
                    yield return null;
                }
            }

            player.Stop();
            Destroy(canvasGroup.gameObject);
            textura.Release();
            Destroy(textura);
        }

        CanvasGroup CrearPantalla(RenderTexture textura, float aspecto)
        {
            var go = new GameObject("Intro del estudio");
            go.transform.SetParent(transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;

            // Negro detras: si la pantalla no es 16:9, el video queda centrado con bandas.
            var fondo = NuevoHijo(go.transform, "Fondo").AddComponent<Image>();
            fondo.color = Color.black;
            Estirar(fondo.rectTransform);

            var imagen = NuevoHijo(go.transform, "Video").AddComponent<RawImage>();
            imagen.texture = textura;
            Estirar(imagen.rectTransform);
            var ajuste = imagen.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajuste.aspectRatio = aspecto;

            return group;
        }

        static GameObject NuevoHijo(Transform padre, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return go;
        }

        static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static bool SkipPressed()
        {
            var teclado = Keyboard.current;
            if (teclado != null && teclado.anyKey.wasPressedThisFrame)
                return true;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                return true;

            var mando = Gamepad.current;
            return mando != null && (mando.buttonSouth.wasPressedThisFrame || mando.buttonEast.wasPressedThisFrame ||
                                     mando.startButton.wasPressedThisFrame);
        }
    }
}
