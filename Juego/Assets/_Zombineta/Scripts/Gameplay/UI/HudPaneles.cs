using System;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.UI
{
    /// <summary>
    /// El HUD de la partida: el panel de nafta y bateria, el de armamento y la barra de avance. Vive en
    /// Prefabs/HudPartida.prefab, asi arte lo ajusta en un solo lugar para todos los niveles. A pie la
    /// nafta, la bateria y el avance no sirven: se esconden y el armamento sube a ocupar su lugar.
    /// </summary>
    public sealed class HudPaneles : MonoBehaviour
    {
        const float SegundosPop = 0.3f;
        const float VelocidadLatido = 12f;

        [Serializable]
        public sealed class Numero
        {
            public Image decena;
            public Image unidad;

            public void Poner(int valor, Sprite[] digitos)
            {
                int n = Mathf.Clamp(valor, 0, 99);
                decena.sprite = digitos[n / 10];
                unidad.sprite = digitos[n % 10];
            }

            public void Pintar(Color color)
            {
                decena.color = color;
                unidad.color = color;
            }
        }

        [Serializable]
        public sealed class Medidor
        {
            public Image anillo;
            public Image icono;
            public Color lleno = Color.white;

            public void Dibujar(float fraccion, Color alerta, float apagado)
            {
                anillo.fillAmount = fraccion;
                float brillo = IndicadorDeRecurso.Brillo(fraccion, Time.unscaledTime, apagado);
                var c = IndicadorDeRecurso.ColorDe(fraccion, lleno, alerta);
                c.a = brillo;
                anillo.color = c;
                icono.color = new Color(1f, 1f, 1f, brillo);
            }
        }

        [Serializable]
        public sealed class Arma
        {
            public Image icono;
            public Numero cantidad;
            [NonSerialized] public int antes = -1;
            [NonSerialized] public float pop;
        }

        [Header("Paneles")]
        [SerializeField] GameObject panelRecursos;
        [SerializeField] RectTransform panelArmamento;
        [Tooltip("Donde queda el armamento en la pelea a pie, desde la esquina de arriba a la izquierda.")]
        [SerializeField] Vector2 armamentoEnPelea = new Vector2(28f, -50f);

        [Header("Recursos")]
        [SerializeField] Medidor nafta;
        [SerializeField] Medidor bateria;
        [SerializeField] Color alerta = new Color(0.95f, 0.15f, 0.12f);
        [Tooltip("Cuanto se ve en la parte apagada del titileo.")]
        [SerializeField, Range(0f, 1f)] float apagado = 0.2f;

        [Header("Armamento")]
        [Tooltip("Del 0 al 9, en orden.")]
        [SerializeField] Sprite[] digitos;
        [SerializeField] Numero balas;
        [SerializeField] Numero balasMaximo;
        [SerializeField] Arma granadas;
        [SerializeField] Arma bombas;
        [Tooltip("Las bombas solo sirven a pie: en la moto se ven apagadas.")]
        [SerializeField, Range(0f, 1f)] float bombasEnLaMoto = 0.35f;

        [Header("Barra de avance")]
        [SerializeField] GameObject barra;
        [SerializeField] Image progreso;
        [SerializeField] RectTransform marcador;
        [Tooltip("Los centros de los puntos de la barra: se pintan cuando la moto los pasa.")]
        [SerializeField] Image[] nodos;
        [SerializeField] Color nodoPasado = new Color(0.93f, 0.13f, 0.72f);
        [SerializeField] Color nodoPendiente = new Color(0.04f, 0.01f, 0.08f);

        Vector2 armamentoEnMoto;
        bool enPelea;

        public RectTransform NaftaRect => nafta.anillo.rectTransform;
        public RectTransform BateriaRect => bateria.anillo.rectTransform;
        public RectTransform BalasRect => balas.decena.rectTransform;

        void Awake() => armamentoEnMoto = panelArmamento.anchoredPosition;

        public void Dibujar(RunState s, GameConfig cfg, float avance)
        {
            bool pelea = s.Phase == RunPhase.Pelea;
            if (pelea != enPelea)
                CambiarDeModo(pelea);

            if (!pelea)
            {
                nafta.Dibujar(cfg.fuelMax <= 0f ? 0f : s.Fuel / cfg.fuelMax, alerta, apagado);
                bateria.Dibujar(cfg.batteryMax <= 0f ? 0f : s.Battery / cfg.batteryMax, alerta, apagado);
                DibujarAvance(avance);
            }

            balas.Poner(s.Ammo, digitos);
            balasMaximo.Poner(cfg.ammoMax, digitos);
            DibujarArma(granadas, s.Granadas, 1f, pelea && s.Ayuda == AyudaDeOleada.Granada);
            DibujarArma(bombas, s.Bombas, pelea ? 1f : bombasEnLaMoto, pelea && s.Ayuda == AyudaDeOleada.Bomba);
        }

        void CambiarDeModo(bool pelea)
        {
            enPelea = pelea;
            panelRecursos.SetActive(!pelea);
            barra.SetActive(!pelea);
            panelArmamento.anchoredPosition = pelea ? armamentoEnPelea : armamentoEnMoto;
        }

        void DibujarAvance(float avance)
        {
            progreso.fillAmount = avance;

            var linea = progreso.rectTransform;
            var p = marcador.anchoredPosition;
            p.x = linea.anchoredPosition.x + (avance - linea.pivot.x) * linea.rect.width;
            marcador.anchoredPosition = p;

            for (int i = 0; i < nodos.Length; i++)
                nodos[i].color = avance >= i / (nodos.Length - 1f) ? nodoPasado : nodoPendiente;
        }

        void DibujarArma(Arma arma, int cantidad, float alfa, bool pedida)
        {
            arma.cantidad.Poner(cantidad, digitos);

            // Salta al recoger o al usar; una oleada didactica la hace latir hasta que se usa.
            if (arma.antes >= 0 && cantidad != arma.antes)
                arma.pop = SegundosPop;
            arma.antes = cantidad;
            arma.pop = Mathf.Max(0f, arma.pop - Time.unscaledDeltaTime);
            float salto = 0.4f * Mathf.Sin(arma.pop / SegundosPop * Mathf.PI);
            float latido = pedida ? 0.1f + 0.1f * Mathf.Sin(Time.unscaledTime * VelocidadLatido) : 0f;
            arma.icono.rectTransform.localScale = Vector3.one * (1f + salto + latido);

            arma.icono.color = new Color(1f, 1f, 1f, alfa);
            var c = arma.cantidad.decena.color;
            c.a = alfa;
            arma.cantidad.Pintar(c);
        }
    }
}
