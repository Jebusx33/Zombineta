using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Seleccion de repartidora: izquierda y derecha cambian de personaje (la ficha de cada uno es
    /// una imagen entera, con el elegido a color y el otro en gris), Enter o un clic confirman y
    /// Volver (o cancelar) vuelve al menu. La eleccion queda en el flujo; por ahora el juego usa
    /// siempre a Berenice.
    /// </summary>
    public sealed class CharacterSelectScreen : ScreenBase
    {
        [Tooltip("La capa que muestra a los personajes y la ficha del elegido.")]
        [SerializeField] Image ficha;

        [Tooltip("Una imagen por personaje, en el orden del flujo: 0 Berenice, 1 Hedy-L.")]
        [SerializeField] Sprite[] fichas;

        [SerializeField] InputActionReference cancelAction;

        [Tooltip("Segundos del fundido al cambiar de personaje.")]
        [Min(0f)] [SerializeField] float fundido = 0.12f;

        int actual;
        float aparicion = 1f;

        public int Actual => actual;

        void OnEnable()
        {
            cancelAction?.action.Enable();
            actual = Flow != null ? Mathf.Clamp(Flow.CharacterIndex, 0, Cantidad - 1) : 0;
            Mostrar(false);
        }

        int Cantidad => fichas != null && fichas.Length > 0 ? fichas.Length : 1;

        public void Choose(int index) => Flow?.ChooseCharacter(index);

        /// <summary>Elige al personaje que se esta mostrando.</summary>
        public void Confirmar() => Choose(actual);

        /// <summary>El siguiente (+1) o el anterior (-1), dando la vuelta.</summary>
        public void Cambiar(int paso)
        {
            if (paso == 0 || Cantidad < 2)
                return;
            actual = (actual + paso + Cantidad) % Cantidad;
            Mostrar(true);
        }

        public void Back() => Flow?.Back();

        void Mostrar(bool conFundido)
        {
            if (ficha == null || fichas == null || fichas.Length == 0)
                return;
            ficha.sprite = fichas[actual];
            aparicion = conFundido && fundido > 0f ? 0f : 1f;
            Pintar();
        }

        void Pintar()
        {
            if (ficha == null)
                return;
            var c = ficha.color;
            c.a = Mathf.Lerp(0.35f, 1f, aparicion);
            ficha.color = c;
        }

        protected override void Update()
        {
            base.Update();

            if (aparicion < 1f)
            {
                aparicion = Mathf.MoveTowards(aparicion, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fundido));
                Pintar();
            }

            if (InputConsumed || cancelAction == null)
                return;
            var root = Zombineta.Juego.Flow.GameRoot.Instance;
            if (root != null && root.TopScene == gameObject.scene.name && cancelAction.action.WasPressedThisFrame())
                Back();
        }
    }
}
