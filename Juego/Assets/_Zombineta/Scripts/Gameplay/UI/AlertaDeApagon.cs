using UnityEngine;
using UnityEngine.UI;
using Zombineta.Audio;
using Zombineta.Core;
using Zombineta.Juego.Screens;
using Zombineta.Level;
using Zombineta.Player;

namespace Zombineta.UI
{
    /// <summary>
    /// La radio que avisa los apagones: unos segundos antes de entrar a un tramo oscuro aparece
    /// arriba a la derecha, se queda un rato y se va. Sale de los tramos del nivel, asi que no hay
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
        [Range(0.05f, 0.9f)] [SerializeField] float alto = 0.22f;

        [Tooltip("Separacion del borde de arriba a la derecha, en unidades del Canvas.")]
        [SerializeField] Vector2 margen = new Vector2(24f, 24f);

        [Tooltip("Segundos que tarda en entrar y en salir.")]
        [Min(0.01f)] [SerializeField] float transicion = 0.35f;

        [Header("Aviso de luces")]
        [Tooltip("Oscuridad del tramo (0 a 1) desde la que, con las luces apagadas, aparece el aviso de encenderlas.")]
        [Range(0.05f, 1f)] [SerializeField] float oscuridadParaAvisar = 0.35f;

        RectTransform panel;
        Text luces;
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
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);

            vista = go.AddComponent<Image>();
            vista.sprite = imagen;
            vista.preserveAspect = true;
            vista.raycastTarget = false;
            vista.enabled = false;

            // Aviso de luces: abajo al centro, parpadea como el de poca nafta.
            var lg = new GameObject("Aviso de luces", typeof(RectTransform), typeof(Text));
            lg.transform.SetParent(transform, false);
            luces = lg.GetComponent<Text>();
            luces.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            luces.fontSize = 46;
            luces.fontStyle = FontStyle.Bold;
            luces.alignment = TextAnchor.MiddleCenter;
            luces.horizontalOverflow = HorizontalWrapMode.Overflow;
            luces.raycastTarget = false;
            luces.color = new Color(1f, 0.9f, 0.4f);
            var lr = luces.rectTransform;
            lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0.5f, 0f);
            lr.anchoredPosition = new Vector2(0f, 130f);
            lr.sizeDelta = new Vector2(1500f, 70f);
            var sombra = lg.AddComponent<Outline>();
            sombra.effectColor = Color.black;
            sombra.effectDistance = new Vector2(3f, -3f);
            luces.enabled = false;
        }

        /// <summary>
        /// En un tramo oscuro con las luces apagadas (y bateria para prenderlas): "¡PRESIONA [tecla]!"
        /// con la tecla o el boton del dispositivo que se esta usando. Se va solo al prenderlas.
        /// </summary>
        void AvisarLuces(RunState state)
        {
            bool avisar = state.Phase == RunPhase.Running && !state.HeadlightOn && state.Battery > 0f &&
                          run.Sim.Oscuridad >= oscuridadParaAvisar;
            if (luces.enabled != avisar)
                luces.enabled = avisar;
            if (!avisar)
                return;

            var esquema = InputDeviceTracker.Shared != null ? InputDeviceTracker.Shared.Current : ControlScheme.KeyboardMouse;
            string tecla = ControlHints.Accion("Headlight", esquema);
            luces.text = "¡PRESIONÁ " + (string.IsNullOrEmpty(tecla) ? "LA TECLA DE LUCES" : tecla.ToUpperInvariant()) + " PARA LAS LUCES!";
            var c = luces.color;
            c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));   // el mismo latido que "¡POCA NAFTA!"
            luces.color = c;
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
            AvisarLuces(state);
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
            panel.anchoredPosition = new Vector2(-margen.x + escondida * (1f - t), -margen.y);
        }
    }
}
