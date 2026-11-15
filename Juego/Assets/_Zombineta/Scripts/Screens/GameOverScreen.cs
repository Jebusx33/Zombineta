using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Flow;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Perder: primero una pagina de comic que se arma de a vinetas, como las cinematicas pero mas
    /// rapido (Submit pasa a la siguiente, Cancel la completa de una), y recien al final aparece el
    /// menu para reintentar o volver. Mientras corre la secuencia el menu no se puede tocar, asi el
    /// Enter que adelanta una vineta no aprieta Reintentar.
    /// </summary>
    public sealed class GameOverScreen : ScreenBase
    {
        [Tooltip("Las vinetas, en orden: cada una se suma encima de las anteriores.")]
        [SerializeField] Image[] paginas;

        [Tooltip("El velo, el panel, los botones y las ayudas: aparecen juntos al terminar la secuencia.")]
        [SerializeField] CanvasGroup menu;

        [Tooltip("La ayuda de Enter/Esc de la secuencia: se va cuando aparece el menu.")]
        [SerializeField] CanvasGroup ayudaDeLaSecuencia;

        [SerializeField] float segundosPorPagina = 1.2f;
        [SerializeField] float fundido = 0.25f;
        [SerializeField] float fundidoDelMenu = 0.35f;
        [SerializeField] InputActionReference submitAction;
        [SerializeField] InputActionReference cancelAction;

        bool saltear;
        bool adelantar;
        bool menuListo;

        // Migas de pan (solo editor): si cargar esta pantalla se traba, el Editor.log dice si llego a prenderse.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        static void Traza(string mensaje) => Debug.Log("[GameOver] " + mensaje + " (t=" + Time.realtimeSinceStartup.ToString("0.0") + ")");

        public void Retry() => Flow?.Retry();

        public void ToMenu() => Flow?.ToMainMenu();

        void Awake()
        {
            Traza("Awake");
            MostrarMenu(0f, false);
            if (paginas != null)
                foreach (var p in paginas)
                    if (p != null)
                        Alfa(p, 0f);
        }

        void OnEnable()
        {
            Traza("OnEnable");
            submitAction?.action.Enable();
            cancelAction?.action.Enable();
        }

        IEnumerator Start()
        {
            Traza("Start");
            if (paginas != null)
            {
                foreach (var p in paginas)
                {
                    if (p == null)
                        continue;
                    if (saltear)
                    {
                        Alfa(p, 1f);
                        continue;
                    }

                    adelantar = false;
                    yield return Aparecer(p);
                    adelantar = false; // un Submit durante el fundido solo lo completa
                    for (float t = 0f; t < segundosPorPagina && !adelantar && !saltear; t += Time.unscaledDeltaTime)
                        yield return null;
                }
            }

            // El menu aparece con su fundido y recien despues se puede tocar.
            for (float t = 0f; t < fundidoDelMenu; t += Time.unscaledDeltaTime)
            {
                float k = t / fundidoDelMenu;
                MostrarMenu(k, false);
                if (ayudaDeLaSecuencia != null)
                    ayudaDeLaSecuencia.alpha = 1f - k;
                yield return null;
            }
            if (ayudaDeLaSecuencia != null)
                ayudaDeLaSecuencia.alpha = 0f;
            MostrarMenu(1f, true);
            menuListo = true;
        }

        IEnumerator Aparecer(Image img)
        {
            for (float t = 0f; t < fundido && !saltear && !adelantar; t += Time.unscaledDeltaTime)
            {
                Alfa(img, t / fundido);
                yield return null;
            }
            Alfa(img, 1f);
        }

        protected override void Update()
        {
            if (menuListo)
            {
                base.Update();
                return;
            }

            if (InputConsumed)
                return;
            if (cancelAction != null && cancelAction.action.WasPressedThisFrame())
                saltear = true;
            if (submitAction != null && submitAction.action.WasPressedThisFrame())
                adelantar = true;
        }

        void MostrarMenu(float alfa, bool activo)
        {
            if (menu == null)
                return;
            menu.alpha = alfa;
            menu.interactable = activo;
            menu.blocksRaycasts = activo;
        }

        static void Alfa(Image img, float a)
        {
            var c = img.color;
            c.a = a;
            img.color = c;
        }
    }
}
