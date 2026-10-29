using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.UI
{
    /// <summary>
    /// El HUD de municion de la pelea a pie. A pie la moto esta parada y sin faro, y no hay horda
    /// que perseguir: el combustible, la bateria y el sensor de proximidad de la horda no sirven
    /// para nada, asi que se esconden. La municion si importa (se dispara
    /// para donde se mira), y se muestra arriba a la izquierda, en el lugar del combustible.
    ///
    /// Es un script aparte y no un cambio en HudView para no tocar el HUD de la moto: arma copias
    /// del marco y las balas del HUD de siempre (mismas imagenes) y las prende solo en la pelea.
    /// Al terminar, vuelve el HUD de antes.
    /// </summary>
    public sealed class HudMunicionPelea : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Tooltip("El HUD de siempre: de ahi salen las balas.")]
        [SerializeField] HudView hud;

        [Tooltip("El marco de la municion del HUD de siempre ('Ammo').")]
        [SerializeField] RectTransform marco;

        [Tooltip("Lo que se esconde en la pelea: combustible, bateria y sensor de proximidad de la horda (con sus barras y textos).")]
        [SerializeField] GameObject[] esconder;

        [Tooltip("Donde queda el marco copiado, desde la esquina de arriba a la izquierda, en pixeles del Canvas.")]
        [SerializeField] Vector2 posicion = new Vector2(40f, -40f);

        /// <summary>El marco de la municion que se ve ahora (el de la pelea o el de la moto). Null si todavia no esta armado.</summary>
        public RectTransform MarcoVisible => !inicializado ? null : enPelea ? marcoCopia : marco;

        RectTransform marcoCopia;
        RectTransform[] originales;
        RectTransform[] copias;
        Image[] copiasImagen;
        bool enPelea;
        bool inicializado;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();
            if (hud == null)
                hud = GetComponent<HudView>();
            if (marco == null || hud == null)
            {
                Debug.LogWarning("HudMunicionPelea sin marco o HudView asignado: no se muestra la municion de la pelea.", this);
                return;
            }

            marcoCopia = Instantiate(marco, marco.parent);
            marcoCopia.name = "Ammo (pelea)";
            marcoCopia.anchoredPosition = posicion;

            originales = hud.AmmoPipRects;
            copias = new RectTransform[originales.Length];
            copiasImagen = new Image[originales.Length];
            for (int i = 0; i < originales.Length; i++)
            {
                var copia = Instantiate(originales[i], originales[i].parent);
                copia.name = originales[i].name + " (pelea)";
                // Cada bala queda en el mismo lugar del marco que en el original.
                copia.anchoredPosition = posicion + (originales[i].anchoredPosition - marco.anchoredPosition);
                copias[i] = copia;
                copiasImagen[i] = copia.GetComponent<Image>();
            }

            // Como en el HUD de la moto (donde el marco es el ultimo de la lista): el marco va ADELANTE y las balas
            // por detras, asi no tapan el dibujo del marco.
            marcoCopia.SetAsLastSibling();

            Mostrar(false, true);
            inicializado = true;
        }

        void LateUpdate()
        {
            if (!inicializado || run == null || run.Sim == null)
                return;

            var s = run.Sim.State;
            bool pelea = s.Phase == RunPhase.Pelea;
            if (pelea != enPelea)
                Mostrar(pelea, false);

            if (pelea)
                for (int i = 0; i < copiasImagen.Length; i++)
                    if (copiasImagen[i] != null)
                        copiasImagen[i].enabled = i < s.Ammo;
        }

        /// <summary>En la pelea: lo de la pelea prendido y el HUD de la moto (sin la municion) apagado.</summary>
        void Mostrar(bool pelea, bool forzar)
        {
            if (!forzar && pelea == enPelea)
                return;
            enPelea = pelea;

            marcoCopia.gameObject.SetActive(pelea);
            foreach (var copia in copias)
                if (copia != null)
                    copia.gameObject.SetActive(pelea);

            // El marco y las balas de siempre se apagan porque las copias ocupan su lugar.
            marco.gameObject.SetActive(!pelea);
            foreach (var original in originales)
                if (original != null)
                    original.gameObject.SetActive(!pelea);
            foreach (var go in esconder)
                if (go != null)
                    go.SetActive(!pelea);
        }
    }
}
