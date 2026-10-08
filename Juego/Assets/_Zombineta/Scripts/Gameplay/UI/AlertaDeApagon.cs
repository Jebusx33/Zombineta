using UnityEngine;
using UnityEngine.UI;
using Zombineta.Audio;
using Zombineta.Core;
using Zombineta.Level;

namespace Zombineta.UI
{
    /// <summary>
    /// La radio que avisa los apagones: unos segundos antes de entrar a un tramo oscuro aparece
    /// abajo a la derecha, se queda un rato y se va. Sale de los tramos del nivel, asi que no hay
    /// nada que cablear por apagon: alcanza con que el nivel tenga un tramo con oscuridad.
    /// Va en el Canvas del HUD y arma su propia imagen.
    /// </summary>
    public sealed class AlertaDeApagon : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Tooltip("La radio con su globo. Sin imagen no se muestra nada (el sonido suena igual).")]
        [SerializeField] Sprite imagen;

        [Tooltip("Opcional: suena en el momento en que aparece la alerta.")]
        [SerializeField] Sonido sonido;

        [Header("Cuando")]
        [Tooltip("Segundos antes del apagon en que aparece, yendo a velocidad normal.")]
        [Range(1f, 20f)] [SerializeField] float segundosAntes = 7f;

        [Tooltip("Segundos que se queda en pantalla.")]
        [Min(0.5f)] [SerializeField] float duracion = 5f;

        [Header("Donde")]
        [Tooltip("Alto de la imagen, como fraccion del alto de la pantalla.")]
        [Range(0.1f, 0.9f)] [SerializeField] float alto = 0.5f;

        [Tooltip("Separacion del borde de abajo a la derecha, en unidades del Canvas.")]
        [SerializeField] Vector2 margen = new Vector2(24f, 24f);

        [Tooltip("Segundos que tarda en entrar y en salir.")]
        [Min(0.01f)] [SerializeField] float transicion = 0.35f;

        RectTransform panel;
        Image vista;
        int avisado = -1;
        float ultimaX;
        float restante;   // segundos que le quedan en pantalla
        float asomada;    // 0 escondida, 1 a la vista

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();
            Armar();
        }

        void Armar()
        {
            var go = new GameObject("Alerta de apagon", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            panel = (RectTransform)go.transform;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0f);

            vista = go.AddComponent<Image>();
            vista.sprite = imagen;
            vista.preserveAspect = true;
            vista.raycastTarget = false;
            vista.enabled = false;
        }

        void Update()
        {
            if (run == null || run.Sim == null || panel == null)
                return;

            var state = run.Sim.State;

            // Reinicio del nivel (o un rebobinado): los apagones vuelven a avisarse.
            if (state.PlayerX < ultimaX - 20f)
                avisado = -1;
            ultimaX = state.PlayerX;

            if (state.Phase == RunPhase.Running)
            {
                float metros = Tramos.MetrosHastaApagon(run.Config.tramos, state.PlayerX, out int indice);
                if (indice >= 0 && indice != avisado && metros <= segundosAntes * run.Config.normalSpeed)
                {
                    avisado = indice;
                    restante = duracion;
                    if (sonido != null && AudioDirector.Instance != null)
                        AudioDirector.Instance.Play(sonido);
                }
            }
            else
            {
                restante = 0f; // Perdio o llego: la radio se va.
            }

            restante = Mathf.Max(0f, restante - Time.deltaTime);
            float objetivo = restante > 0f ? 1f : 0f;
            asomada = Mathf.MoveTowards(asomada, objetivo, Time.deltaTime / transicion);
            Dibujar();
        }

        void Dibujar()
        {
            bool seVe = asomada > 0.001f && imagen != null;
            if (vista.enabled != seVe)
                vista.enabled = seVe;
            if (!seVe)
                return;

            // El tamano sale del alto real del Canvas: se ve igual en cualquier resolucion.
            var canvas = (RectTransform)transform;
            float altoPx = canvas.rect.height * alto;
            float anchoPx = altoPx * imagen.rect.width / imagen.rect.height;
            panel.sizeDelta = new Vector2(anchoPx, altoPx);

            // Entra desde afuera de la pantalla, por la derecha, frenando al llegar.
            float t = 1f - (1f - asomada) * (1f - asomada);
            float escondida = anchoPx + margen.x;
            panel.anchoredPosition = new Vector2(-margen.x + escondida * (1f - t), margen.y);
        }
    }
}
