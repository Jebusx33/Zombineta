using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;
using Zombineta.Juego.Screens;
using Zombineta.Player;

namespace Zombineta.Auxilio
{
    /// <summary>
    /// El HUD de la pelea a pie: vida, cuanto falta para el auxilio y ayudas que quedan, mas los
    /// carteles grandes ("¡SIN NAFTA!", "¡AUXILIO!") y la vineta roja con poca vida. Va en el
    /// Canvas del HUD y arma sus propios elementos.
    /// </summary>
    public sealed class HudPelea : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Sprite vineta;
        [Tooltip("Con esta vida o menos, los bordes de la pantalla se ponen rojos.")]
        [SerializeField] int vidaCritica = 2;
        [SerializeField] float segundosCartel = 1.4f;
        [Tooltip("Segundos que queda en pantalla el contador de la cadena de golpes.")]
        [SerializeField] float segundosCombo = 0.9f;
        [Tooltip("Segundos que dura el destello al recibir un golpe.")]
        [SerializeField] float segundosGolpeRecibido = 0.35f;
        [Tooltip("Tamano de la letra de la ayuda de controles (fijo, no se achica).")]
        [SerializeField] int tamanoAyuda = 38;
        [Tooltip("Color de las teclas clave (golpe y disparo) en la ayuda de controles.")]
        [SerializeField] Color colorTeclasClave = new Color(1f, 0.3f, 0.25f);
        [Tooltip("Segundos que queda en pantalla la nota de la pelea con sus premios.")]
        [SerializeField] float segundosNota = 3f;
        [Tooltip("Segundos que queda el aviso de lo recogido del suelo (botiquin, balas).")]
        [SerializeField] float segundosRecogida = 1.1f;

        Text auxilio, vida, ayudas, cartel, controles, combo, pocaNafta, nota, premios, recogida;
        Image barra, fondoBarra, rojo;
        GameObject panel;
        float cartelRestante;
        float comboRestante;
        float notaRestante;
        float notaOpacidadPremios = 1f;
        float recogidaRestante;
        float golpeRecibido;     // 1 al recibir un golpe, baja a 0
        Vector2 vidaPosicion;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rojo = NuevaImagen("Vineta", transform, new Color(1f, 1f, 1f, 0f));
            rojo.sprite = vineta;
            Estirar(rojo.rectTransform);

            panel = new GameObject("Pelea", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var pr = (RectTransform)panel.transform;
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 1f);
            pr.anchoredPosition = new Vector2(0f, -30f);
            pr.sizeDelta = new Vector2(620f, 130f);

            auxilio = NuevoTexto("Auxilio", panel.transform, font, 34, TextAnchor.UpperCenter, new Vector2(0f, 0f), new Vector2(620f, 44f));
            fondoBarra = NuevaImagen("Barra fondo", panel.transform, new Color(0f, 0f, 0f, 0.6f));
            Ubicar(fondoBarra.rectTransform, new Vector2(0f, -48f), new Vector2(520f, 18f));
            barra = NuevaImagen("Barra", fondoBarra.transform, new Color(1f, 0.82f, 0.25f, 1f));
            barra.rectTransform.anchorMin = new Vector2(0f, 0f);
            barra.rectTransform.anchorMax = new Vector2(0f, 1f);
            barra.rectTransform.pivot = new Vector2(0f, 0.5f);
            barra.rectTransform.anchoredPosition = Vector2.zero;
            vida = NuevoTexto("Vida", panel.transform, font, 40, TextAnchor.UpperCenter, new Vector2(0f, -72f), new Vector2(620f, 50f));
            vida.color = new Color(1f, 0.3f, 0.35f);
            vidaPosicion = vida.rectTransform.anchoredPosition;

            // El contador de la cadena de golpes, debajo de la vida.
            combo = NuevoTexto("Combo", panel.transform, font, 46, TextAnchor.UpperCenter, new Vector2(0f, -122f), new Vector2(620f, 60f));
            combo.fontStyle = FontStyle.Bold;
            var comboSombra = combo.gameObject.AddComponent<Outline>();
            comboSombra.effectColor = Color.black;
            comboSombra.effectDistance = new Vector2(3f, -3f);
            combo.enabled = false;

            ayudas = NuevoTexto("Ayudas", transform, font, 26, TextAnchor.UpperRight, Vector2.zero, new Vector2(400f, 40f));
            var ar = ayudas.rectTransform;
            ar.anchorMin = ar.anchorMax = ar.pivot = new Vector2(1f, 1f);
            ar.anchoredPosition = new Vector2(-30f, -260f);

            cartel = NuevoTexto("Cartel", transform, font, 110, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1600f, 200f));
            var cr = cartel.rectTransform;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 0.5f);
            cr.anchoredPosition = new Vector2(0f, 120f);
            cartel.fontStyle = FontStyle.Bold;
            var sombra = cartel.gameObject.AddComponent<Outline>();
            sombra.effectColor = Color.black;
            sombra.effectDistance = new Vector2(5f, -5f);
            cartel.enabled = false;

            // La nota de la pelea (letra grande) y debajo lo que dejo el auxilio.
            nota = NuevoTexto("Nota", transform, font, 150, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(900f, 220f));
            var nr = nota.rectTransform;
            nr.anchorMin = nr.anchorMax = nr.pivot = new Vector2(0.5f, 0.5f);
            nr.anchoredPosition = new Vector2(0f, -40f);
            nota.fontStyle = FontStyle.Bold;
            var notaSombra = nota.gameObject.AddComponent<Outline>();
            notaSombra.effectColor = Color.black;
            notaSombra.effectDistance = new Vector2(6f, -6f);
            nota.enabled = false;

            premios = NuevoTexto("Premios", transform, font, 44, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1200f, 70f));
            var mr = premios.rectTransform;
            mr.anchorMin = mr.anchorMax = mr.pivot = new Vector2(0.5f, 0.5f);
            mr.anchoredPosition = new Vector2(0f, -170f);
            premios.fontStyle = FontStyle.Bold;
            var premiosSombra = premios.gameObject.AddComponent<Outline>();
            premiosSombra.effectColor = Color.black;
            premiosSombra.effectDistance = new Vector2(3f, -3f);
            premios.enabled = false;

            // Lo que se levanta del suelo: aparece bajo la vida.
            recogida = NuevoTexto("Recogida", transform, font, 44, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(800f, 70f));
            var rr = recogida.rectTransform;
            rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(0.5f, 1f);
            rr.anchoredPosition = new Vector2(0f, -190f);
            recogida.fontStyle = FontStyle.Bold;
            var recogidaSombra = recogida.gameObject.AddComponent<Outline>();
            recogidaSombra.effectColor = Color.black;
            recogidaSombra.effectDistance = new Vector2(3f, -3f);
            recogida.enabled = false;

            // Aviso de poca nafta (solo en niveles con auxilio): debajo del marco del combustible.
            pocaNafta = NuevoTexto("Poca nafta", transform, font, 30, TextAnchor.UpperLeft, Vector2.zero, new Vector2(420f, 44f));
            var pr2 = pocaNafta.rectTransform;
            pr2.anchorMin = pr2.anchorMax = pr2.pivot = new Vector2(0f, 1f);
            pr2.anchoredPosition = new Vector2(48f, -124f);
            pocaNafta.fontStyle = FontStyle.Bold;
            pocaNafta.color = new Color(1f, 0.35f, 0.25f);
            var ps2 = pocaNafta.gameObject.AddComponent<Outline>();
            ps2.effectColor = Color.black;
            ps2.effectDistance = new Vector2(2f, -2f);
            pocaNafta.enabled = false;

            // Ayuda de controles abajo, con las teclas o botones del dispositivo que se esta usando.
            controles = NuevoTexto("Controles", transform, font, 30, TextAnchor.LowerCenter, Vector2.zero, new Vector2(1700f, 60f));
            var kr = controles.rectTransform;
            kr.anchorMin = kr.anchorMax = kr.pivot = new Vector2(0.5f, 0f);
            kr.anchoredPosition = new Vector2(0f, 36f);
            var ks = controles.gameObject.AddComponent<Outline>();
            ks.effectColor = Color.black;
            ks.effectDistance = new Vector2(2f, -2f);
            controles.enabled = false;
        }

        void OnEnable()
        {
            if (run != null)
                run.Stepped += AlPasar;
        }

        void OnDisable()
        {
            if (run != null)
                run.Stepped -= AlPasar;
        }

        void AlPasar(RunEvent ev)
        {
            // La moto empieza a frenar: el aviso sale en ese momento, antes de que se baje.
            if ((ev & RunEvent.Frenando) != 0)
                Mostrar("¡SIN NAFTA!", new Color(1f, 0.85f, 0.2f));
            if ((ev & RunEvent.PeleaEmpieza) != 0)
            {
                Mostrar("¡AGUANTÁ!", new Color(1f, 0.6f, 0.2f));
            }
            if ((ev & RunEvent.OleadaNueva) != 0 && run != null && run.Sim != null)
            {
                int n = run.Sim.State.Oleada + 1;
                bool ultima = run.Config.oleadas != null && n >= run.Config.oleadas.Count;
                Mostrar(ultima ? "¡ÚLTIMA OLEADA!" : "OLEADA " + n, ultima ? new Color(1f, 0.4f, 0.3f) : Color.white);
            }
            if ((ev & RunEvent.PeleaReintenta) != 0)
            {
                Mostrar("¡OTRA VEZ!", new Color(1f, 0.5f, 0.5f));
            }
            if ((ev & RunEvent.AuxilioTermina) != 0)
            {
                Mostrar("¡A CORRER!", new Color(0.55f, 1f, 0.55f));
                MostrarNota();
            }
            if ((ev & RunEvent.BotiquinRecogido) != 0)
                MostrarRecogida("+1 ♥", new Color(0.4f, 1f, 0.5f));
            if ((ev & RunEvent.DropLleno) != 0 && run != null && run.Sim != null)
                MostrarRecogida(run.Sim.State.DropLlenoKind == 0 ? "VIDA LLENA" : "MUNICIÓN LLENA", new Color(1f, 1f, 1f));
            if ((ev & RunEvent.BalasRecogidas) != 0 && run != null)
                MostrarRecogida("+" + run.Config.dropBalasCantidad + " BALAS", new Color(1f, 0.85f, 0.3f));
            if ((ev & RunEvent.AuxilioLlega) != 0)
                Mostrar("¡AUXILIO!", new Color(0.55f, 1f, 0.55f));

            // La cadena de golpes: el paso lo da el estado. Los golpes al aire tambien cuentan.
            if (run != null && run.Sim != null && (ev & (RunEvent.Golpe | RunEvent.GolpeAlAire)) != 0)
            {
                int paso = run.Sim.State.ComboPaso;
                if ((ev & RunEvent.Remate) != 0)
                    MostrarCombo("¡REMATE!", new Color(1f, 0.45f, 0.15f), 56);
                else if (paso >= 2)
                    MostrarCombo("COMBO x" + paso, new Color(1f, 0.9f, 0.3f), 46);
                else
                    MostrarCombo("GOLPE", Color.white, 40);
            }
            if ((ev & RunEvent.Lastimada) != 0)
                golpeRecibido = 1f;
        }

        void MostrarRecogida(string texto, Color color)
        {
            recogida.text = texto;
            recogida.color = color;
            recogidaRestante = segundosRecogida;
        }

        /// <summary>La nota de la pelea y lo que dejo: balas y bateria (o nada, con la peor nota).</summary>
        void MostrarNota()
        {
            if (run == null || run.Sim == null)
                return;
            var s = run.Sim.State;
            nota.text = s.Nota.ToString();
            nota.color = ColorDeNota(s.Nota);

            var partes = new System.Collections.Generic.List<string>();
            if (s.PremioBalas > 0)
                partes.Add("+" + s.PremioBalas + (s.PremioBalas == 1 ? " BALA" : " BALAS"));
            else if (s.PremioBalasLleno)
                partes.Add("MUNICIÓN LLENA");
            if (s.PremioBateria > 0.5f && run.Config.batteryMax > 0f)
                partes.Add("+" + Mathf.RoundToInt(s.PremioBateria / run.Config.batteryMax * 100f) + "% BATERÍA");
            else if (s.PremioBateriaLleno)
                partes.Add("BATERÍA LLENA");
            premios.text = partes.Count > 0 ? string.Join("   ", partes) : "SIN PREMIO";
            notaOpacidadPremios = partes.Count > 0 ? 1f : 0.6f;
            notaRestante = segundosNota;
        }

        static Color ColorDeNota(NotaPelea n)
        {
            switch (n)
            {
                case NotaPelea.S: return new Color(1f, 0.85f, 0.2f);
                case NotaPelea.A: return new Color(0.5f, 1f, 0.55f);
                case NotaPelea.B: return new Color(0.6f, 0.85f, 1f);
                default: return new Color(0.75f, 0.75f, 0.75f);
            }
        }

        void MostrarCombo(string texto, Color color, int tamano)
        {
            combo.text = texto;
            combo.color = color;
            combo.fontSize = tamano;
            comboRestante = segundosCombo;
        }

        void Mostrar(string texto, Color color)
        {
            cartel.text = texto;
            cartel.color = color;
            cartelRestante = segundosCartel;
        }

        void Update()
        {
            if (run == null || run.Sim == null)
                return;

            var s = run.Sim.State;
            var c = run.Config;
            bool pelea = s.Phase == RunPhase.Pelea;

            panel.SetActive(pelea);
            if (pelea)
            {
                auxilio.text = s.FurgonetaEnCamino ? "¡LLEGO EL AUXILIO!" : "AUXILIO EN " + Mathf.CeilToInt(s.PeleaRestante);
                float llegada = c.peleaSegundos > 0f ? 1f - s.PeleaRestante / c.peleaSegundos : 1f;
                barra.rectTransform.sizeDelta = new Vector2(fondoBarra.rectTransform.rect.width * Mathf.Clamp01(llegada), 0f);
                vida.text = new string('♥', Mathf.Max(0, s.Vida)) + new string('♡', Mathf.Max(0, c.vidaPelea - s.Vida));
            }

            ayudas.enabled = c.ayudas > 0;
            if (ayudas.enabled)
                ayudas.text = "AUXILIOS: " + s.Ayudas;

            ActualizarControles(pelea);

            // Poca nafta (en niveles con auxilio y con auxilios todavia): parpadea para que no sorprenda.
            bool aviso = !pelea && s.Phase == RunPhase.Running && c.ayudas > 0 && s.Ayudas > 0 &&
                         s.Fuel > 0f && c.fuelMax > 0f && s.Fuel / c.fuelMax < 0.25f;
            pocaNafta.enabled = aviso;
            if (aviso)
            {
                pocaNafta.text = "¡POCA NAFTA!";
                var pn = pocaNafta.color;
                pn.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                pocaNafta.color = pn;
            }

            // Recibir un golpe: los corazones se sacuden y se agrandan, y los bordes se ponen rojos un instante.
            golpeRecibido = Mathf.MoveTowards(golpeRecibido, 0f, Time.unscaledDeltaTime / Mathf.Max(0.05f, segundosGolpeRecibido));
            vida.rectTransform.anchoredPosition = vidaPosicion + new Vector2(Mathf.Sin(Time.unscaledTime * 60f) * 10f * golpeRecibido, 0f);
            vida.rectTransform.localScale = Vector3.one * (1f + 0.35f * golpeRecibido);

            float rojoObjetivo = pelea && s.Vida <= vidaCritica ? 0.28f + 0.1f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            var rc = rojo.color;
            rc.a = Mathf.MoveTowards(rc.a, rojoObjetivo, Time.unscaledDeltaTime * 2f);
            // El destello del golpe pisa al aviso de poca vida mientras dura.
            rc.a = Mathf.Max(rc.a, 0.55f * golpeRecibido);
            rojo.color = rc;

            if (comboRestante > 0f)
            {
                comboRestante -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(comboRestante / segundosCombo);
                combo.enabled = pelea;
                // Entra grande y se asienta; en el ultimo tercio se desvanece.
                combo.rectTransform.localScale = Vector3.one * (1f + 0.3f * Mathf.Clamp01((k - 0.8f) / 0.2f));
                var cc = combo.color;
                cc.a = Mathf.Clamp01(k * 3f);
                combo.color = cc;
            }
            else if (combo.enabled)
                combo.enabled = false;

            if (notaRestante > 0f)
            {
                notaRestante -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(notaRestante / segundosNota);
                // Entra grande y se asienta; en el ultimo tramo se desvanece.
                float entrada = Mathf.Clamp01((k - 0.9f) / 0.1f);
                float alfa = Mathf.Clamp01(k * 4f);
                nota.enabled = premios.enabled = true;
                nota.rectTransform.localScale = Vector3.one * (1f + 0.5f * entrada);
                var nc = nota.color;
                nc.a = alfa;
                nota.color = nc;
                var pc = premios.color;
                pc.a = alfa * notaOpacidadPremios;
                premios.color = pc;
            }
            else if (nota.enabled)
                nota.enabled = premios.enabled = false;

            if (recogidaRestante > 0f)
            {
                recogidaRestante -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(recogidaRestante / segundosRecogida);
                recogida.enabled = pelea;
                recogida.rectTransform.localScale = Vector3.one * (1f + 0.3f * Mathf.Clamp01((k - 0.7f) / 0.3f));
                var rec = recogida.color;
                rec.a = Mathf.Clamp01(k * 3f);
                recogida.color = rec;
            }
            else if (recogida.enabled)
                recogida.enabled = false;

            if (cartelRestante > 0f)
            {
                cartelRestante -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(cartelRestante / segundosCartel);
                cartel.enabled = true;
                float escala = 1f + (1f - k) * 0.15f;
                cartel.rectTransform.localScale = Vector3.one * escala;
                var col = cartel.color;
                col.a = Mathf.Clamp01(k * 3f);
                cartel.color = col;
            }
            else if (cartel.enabled)
                cartel.enabled = false;
        }

        /// <summary>
        /// La ayuda de controles de la pelea: solo en la primera (ahi se aprende), todo el tiempo que
        /// dura (incluido el reintento), con el mismo tamano y las teclas de golpe y disparo en rojo.
        /// En las peleas siguientes ya no sale.
        /// </summary>
        void ActualizarControles(bool pelea)
        {
            bool primera = run.Sim.State.AyudasUsadas <= 1;
            controles.enabled = pelea && primera && !run.Sim.State.FurgonetaEnCamino;
            if (!controles.enabled)
                return;

            var scheme = InputDeviceTracker.Shared != null ? InputDeviceTracker.Shared.Current : ControlScheme.KeyboardMouse;
            controles.supportRichText = true;
            controles.fontSize = tamanoAyuda;
            controles.text = ControlHints.TextForPelea(scheme, "#" + ColorUtility.ToHtmlStringRGB(colorTeclasClave));
        }

        static Text NuevoTexto(string nombre, Transform padre, Font font, int tam, TextAnchor anclaje, Vector2 pos, Vector2 tamano)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(padre, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = tam;
            t.alignment = anclaje;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Ubicar(t.rectTransform, pos, tamano);
            return t;
        }

        static Image NuevaImagen(string nombre, Transform padre, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            var i = go.GetComponent<Image>();
            i.color = color;
            i.raycastTarget = false;
            return i;
        }

        static void Ubicar(RectTransform rt, Vector2 pos, Vector2 tamano)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamano;
        }

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
