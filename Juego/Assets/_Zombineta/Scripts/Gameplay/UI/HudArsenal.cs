using UnityEngine;
using UnityEngine.UI;
using Zombineta.Core;

namespace Zombineta.UI
{
    /// <summary>
    /// El arsenal en el HUD: las granadas (se lanzan desde la moto y tambien a pie) y las bombas
    /// temporizadoras (solo a pie), como dos filas chicas de iconos a la DERECHA del marco de las balas
    /// (asi no tapan el sensor de la horda ni el resto), con tantos casilleros como se pueden llevar: lleno
    /// el que se tiene, apagado el que falta, y "saltan" al recoger y al usar. Sin teclas: cual es cual lo
    /// dice la ayuda de la pelea. En la moto la fila de bombas queda apagada.
    ///
    /// Se crea sola al dar Play (arma su propio Canvas arriba del HUD y se acomoda siguiendo al marco
    /// de las balas, a la moto y a pie), asi no hay que tocar ninguna escena ni se pisa con el resto.
    /// Los iconos salen de Resources/Arsenal.
    /// </summary>
    public sealed class HudArsenal : MonoBehaviour
    {
        const float Lado = 42f;
        const float Hueco = 4f;
        const float SegundosBusqueda = 0.5f;
        const float SegundosPop = 0.3f;

        static HudArsenal instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarStatic() => instancia = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null)
                return;
            var go = new GameObject("HudArsenal");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<HudArsenal>();
        }

        sealed class Fila
        {
            public Image[] casilleros;
            public int antes = -1;
            public float pop;
            public int indicePop = -1;
            public RectTransform padre;
            public int maximo;
        }

        RunController run;
        HudView hud;
        HudMunicionPelea hudPelea;
        RectTransform raiz;
        Fila granadas, bombas;
        Sprite spriteGranada, spriteBomba;
        float proximaBusqueda;
        readonly Vector3[] esquinas = new Vector3[4];

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var go = new GameObject("Arsenal", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            raiz = (RectTransform)go.transform;
            raiz.anchorMin = Vector2.zero;
            raiz.anchorMax = Vector2.one;
            raiz.offsetMin = raiz.offsetMax = Vector2.zero;
            go.SetActive(false);

            spriteGranada = Resources.Load<Sprite>("Arsenal/Granada");
            spriteBomba = Resources.Load<Sprite>("Arsenal/BombaTemporal");
        }

        void Update()
        {
            if (run == null && Time.unscaledTime >= proximaBusqueda)
            {
                proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
                run = FindAnyObjectByType<RunController>();
                hud = run != null ? FindAnyObjectByType<HudView>() : null;
                hudPelea = run != null ? FindAnyObjectByType<HudMunicionPelea>() : null;
                granadas = bombas = null;
                for (int i = raiz.childCount - 1; i >= 0; i--)
                    Destroy(raiz.GetChild(i).gameObject);
            }

            bool visible = run != null && run.Sim != null && hud != null &&
                           (run.Sim.State.Phase == RunPhase.Running || run.Sim.State.Phase == RunPhase.Pelea);
            if (raiz.gameObject.activeSelf != visible)
                raiz.gameObject.SetActive(visible);
            if (!visible)
                return;

            var s = run.Sim.State;
            var cfg = run.Config;
            if (granadas == null || granadas.maximo != cfg.granadaMax || bombas.maximo != cfg.bombaMax)
                Armar(cfg);

            bool pelea = s.Phase == RunPhase.Pelea;

            Posicionar();
            // Las granadas sirven en la moto y a pie; las bombas, solo a pie (en la moto la fila queda apagada).
            Dibujar(granadas, s.Granadas, 1f);
            Dibujar(bombas, s.Bombas, pelea ? 1f : 0.35f);
        }

        /// <summary>Arma las dos filas segun cuantas granadas y bombas se pueden llevar.</summary>
        void Armar(GameConfig cfg)
        {
            for (int i = raiz.childCount - 1; i >= 0; i--)
                Destroy(raiz.GetChild(i).gameObject);
            granadas = NuevaFila("Granadas", spriteGranada, cfg.granadaMax);
            bombas = NuevaFila("Bombas", spriteBomba, cfg.bombaMax);
        }

        Fila NuevaFila(string nombre, Sprite icono, int maximo)
        {
            var f = new Fila { maximo = maximo, casilleros = new Image[maximo] };
            var padre = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
            padre.SetParent(raiz, false);
            padre.anchorMin = padre.anchorMax = padre.pivot = new Vector2(0.5f, 0.5f);
            padre.sizeDelta = new Vector2(Lado, Lado);
            f.padre = padre;

            for (int i = 0; i < maximo; i++)
            {
                var go = new GameObject(nombre + " " + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(padre, false);
                var im = go.GetComponent<Image>();
                im.sprite = icono;
                im.preserveAspect = true;
                im.raycastTarget = false;
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(Lado, Lado);
                rt.anchoredPosition = new Vector2(i * (Lado + Hueco), 0f);
                f.casilleros[i] = im;
            }

            return f;
        }

        /// <summary>
        /// A la derecha del marco de las balas que se ve ahora (el de la moto o el de la pelea), pegadas a su
        /// borde de arriba: la fila de granadas y debajo la de bombas. No tapan nada de la columna de la izquierda.
        /// </summary>
        void Posicionar()
        {
            if (!BordeDelMarco(out Vector2 arribaDerecha))
                arribaDerecha = new Vector2(-raiz.rect.width * 0.5f + 330f, raiz.rect.height * 0.5f - 95f);

            float x = arribaDerecha.x + 18f + Lado * 0.5f;
            float y = arribaDerecha.y - Lado * 0.5f;
            granadas.padre.anchoredPosition = new Vector2(x, y);
            bombas.padre.anchoredPosition = new Vector2(x, y - Lado - 4f);
        }

        /// <summary>La esquina de arriba a la derecha del marco de las balas visible, en coordenadas de este Canvas.</summary>
        bool BordeDelMarco(out Vector2 local)
        {
            local = Vector2.zero;
            RectTransform marco = hudPelea != null ? hudPelea.MarcoVisible : null;
            if (marco != null && marco.gameObject.activeInHierarchy)
            {
                marco.GetWorldCorners(esquinas);
                return AlCanvas(esquinas[2], out local);
            }

            // Sin marco: el borde de arriba a la derecha de las balas del HUD.
            var pips = hud.AmmoPipRects;
            bool alguno = false;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var pip in pips)
            {
                if (pip == null || !pip.gameObject.activeInHierarchy)
                    continue;
                pip.GetWorldCorners(esquinas);
                maxX = Mathf.Max(maxX, esquinas[2].x);
                maxY = Mathf.Max(maxY, esquinas[2].y);
                alguno = true;
            }
            if (!alguno || !AlCanvas(new Vector3(maxX, maxY, 0f), out local))
                return false;
            // Sin el marco, el borde de las balas queda adentro del dibujo: se deja lugar para su borde.
            local += new Vector2(46f, 14f);
            return true;
        }

        bool AlCanvas(Vector3 mundo, out Vector2 local)
        {
            var pantalla = RectTransformUtility.WorldToScreenPoint(null, mundo);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, pantalla, null, out local);
        }

        void Dibujar(Fila f, int cantidad, float alfaActiva)
        {
            // Un casillero que se llena (o se vacia) salta un momento.
            if (f.antes >= 0 && cantidad != f.antes)
            {
                f.pop = SegundosPop;
                f.indicePop = cantidad > f.antes ? cantidad - 1 : cantidad;
            }
            f.antes = cantidad;
            f.pop = Mathf.Max(0f, f.pop - Time.unscaledDeltaTime);
            float k = f.pop / SegundosPop;
            float escala = 1f + 0.4f * Mathf.Sin(k * Mathf.PI);

            for (int i = 0; i < f.casilleros.Length; i++)
            {
                var im = f.casilleros[i];
                bool lleno = i < cantidad;
                var c = lleno ? Color.white : new Color(0.25f, 0.25f, 0.28f);
                c.a = (lleno ? 1f : 0.35f) * alfaActiva;
                im.color = c;
                // Salta el casillero que se acaba de llenar o de vaciar.
                im.rectTransform.localScale = Vector3.one * (i == f.indicePop ? escala : 1f);
            }
        }

        void OnDestroy()
        {
            if (instancia == this)
                instancia = null;
        }
    }
}
