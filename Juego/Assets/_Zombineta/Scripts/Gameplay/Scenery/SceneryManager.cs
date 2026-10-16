using System;
using System.Collections.Generic;
using UnityEngine;
using Zombineta.Luz;

namespace Zombineta.Scenery
{
    /// <summary>
    /// Arma el escenario por capas a medida que la camara avanza, con parallax.
    ///
    /// Corre despues de CameraFollow para leer la camara ya movida en este frame: si
    /// corriera antes, el fondo iria un frame atrasado respecto de la calle y se notaria
    /// como un temblor.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SceneryManager : MonoBehaviour
    {
        [SerializeField] SceneryTileset tileset;
        [SerializeField] Camera cam;

        [Tooltip("Unidades de mundo que se construyen fuera de cuadro a cada lado, " +
                 "para que nada aparezca de golpe en el borde.")]
        [SerializeField] float margin = 4f;

        // Donde arranca cada capa. Tiene que quedar a la izquierda de lo mas a la izquierda
        // que la camara llegue a ver (la largada), con cualquier factor de parallax.
        const float LayoutStartX = -80f;

        // Donde se dibuja la sombra que tira una capa del escenario: sobre la calle, por encima
        // de todos sus tiles.
        const string CapaDeSombras = "Calle";
        const int OrdenDeSombras = 60;

        readonly List<LayerRuntime> layers = new List<LayerRuntime>();
        int builtVersion = -1;

        public SceneryTileset Tileset => tileset;

        void Start()
        {
            if (cam == null)
                cam = Camera.main;
            Rebuild();
        }

        void LateUpdate()
        {
            if (tileset == null || cam == null)
                return;

            // Edicion en vivo: cualquier cambio en el asset rearma el escenario.
            if (tileset.Version != builtVersion)
                Rebuild();

            float camX = cam.transform.position.x;
            float halfWidth = cam.orthographicSize * cam.aspect + margin;

            for (int i = 0; i < layers.Count; i++)
                layers[i].Refresh(camX, halfWidth);
        }

        void Rebuild()
        {
            for (int i = 0; i < layers.Count; i++)
                layers[i].Dispose();
            layers.Clear();

            if (tileset != null)
            {
                foreach (var cfg in tileset.layers)
                    if (cfg != null && cfg.enabled)
                        layers.Add(new LayerRuntime(transform, cfg, ReservasDe(cfg)));

                builtVersion = tileset.Version;
            }
        }

        /// <summary>
        /// Los tramos que otra capa le reserva a esta (Huecos Para): donde va la calle lateral de
        /// un cruce no se ponen edificios. Solo vale entre capas con el mismo parallax, porque
        /// comparten coordenadas; con otro parallax el hueco se correria y se avisa.
        /// </summary>
        Func<float, Reserva?> ReservasDe(SceneryLayer cfg)
        {
            if (string.IsNullOrEmpty(cfg.huecosPara))
                return null;

            return x =>
            {
                foreach (var otra in layers)
                {
                    if (otra.Name != cfg.huecosPara)
                        continue;
                    if (!Mathf.Approximately(otra.Parallax, cfg.parallax))
                    {
                        Debug.LogWarning("SceneryManager: la capa '" + cfg.name + "' deja huecos para '" +
                                         cfg.huecosPara + "', pero tienen distinto parallax: no se respetan.");
                        return null;
                    }
                    return otra.ProximoAcompanante(x);
                }
                return null;
            };
        }

        /// <summary>Una capa viva: su raiz, su reparto de tiles y las instancias de prefab que recicla.</summary>
        sealed class LayerRuntime
        {
            readonly SceneryLayer cfg;
            readonly Transform root;
            readonly SceneryLayout layout;
            readonly float[] scales;
            readonly Vector3[] mins; // bounds.min del sprite de cada variante, para el pivot
            readonly List<Transform>[] poolPorVariante;
            readonly int[] usadosPorVariante;
            readonly List<TilePlacement> visible = new List<TilePlacement>();

            // Por variante: donde arranca y termina su acompanante, medido desde el X del tile.
            readonly float[] acompDesde;
            readonly float[] acompHasta;
            float acompAlcance;

            public string Name => cfg.name;
            public float Parallax => cfg.parallax;

            public LayerRuntime(Transform parent, SceneryLayer cfg, Func<float, Reserva?> reservas)
            {
                this.cfg = cfg;

                root = new GameObject(cfg.name).transform;
                root.SetParent(parent, false);

                int n = cfg.variants.Count;
                var widths = new float[n];
                var weights = new float[n];
                scales = new float[n];
                mins = new Vector3[n];
                poolPorVariante = new List<Transform>[n];
                usadosPorVariante = new int[n];
                acompDesde = new float[n];
                acompHasta = new float[n];

                for (int i = 0; i < n; i++)
                {
                    poolPorVariante[i] = new List<Transform>();

                    var v = cfg.variants[i];
                    if (v == null || v.prefab == null)
                        continue; // sin prefab migrado: ancho 0, el layout la ignora

                    var sr = v.prefab.GetComponent<SpriteRenderer>();
                    if (sr == null || sr.sprite == null)
                    {
                        Debug.LogWarning("SceneryManager: el prefab '" + v.prefab.name +
                                         "' de la capa '" + cfg.name + "' no tiene sprite.");
                        continue;
                    }

                    Bounds b = sr.sprite.bounds;
                    scales[i] = cfg.height * v.heightScale / b.size.y;
                    // Un tile en diagonal avanza menos que su ancho: sus bordes inclinados encastran.
                    float ancho = v.avancePx > 0f ? v.avancePx / sr.sprite.pixelsPerUnit : b.size.x;
                    widths[i] = ancho * scales[i];
                    weights[i] = v.weight;
                    mins[i] = b.min;

                    var acomp = Acompanante(v);
                    if (acomp != null)
                    {
                        float k = sr.sprite.pixelsPerUnit / acomp.sprite.pixelsPerUnit;
                        acompDesde[i] = v.acompananteOffsetPx.x / sr.sprite.pixelsPerUnit * scales[i];
                        acompHasta[i] = acompDesde[i] + acomp.sprite.bounds.size.x * k * scales[i];
                        acompAlcance = Mathf.Max(acompAlcance, acompHasta[i]);
                    }
                }

                layout = new SceneryLayout(widths, weights, cfg.gapChance, cfg.gapMin,
                                           cfg.gapMax, cfg.noImmediateRepeat, cfg.seed,
                                           LayoutStartX, reservas);
            }

            static SpriteRenderer Acompanante(SceneryVariant v)
            {
                if (v.acompanante == null)
                    return null;
                var sr = v.acompanante.GetComponent<SpriteRenderer>();
                return sr != null && sr.sprite != null ? sr : null;
            }

            /// <summary>El primer acompanante de esta capa que termina despues de x, como tramo reservado.</summary>
            public Reserva? ProximoAcompanante(float x)
            {
                if (acompAlcance <= 0f)
                    return null;

                // Lo que esta un poco mas adelante tambien cuenta: el tile que lo trae puede
                // empezar antes de x y su acompanante terminar despues.
                layout.EnsureCovered(x + acompAlcance + 1f);
                var placed = layout.Placed;
                for (int i = 0; i < placed.Count; i++)
                {
                    var t = placed[i];
                    if (acompHasta[t.Variant] <= 0f)
                        continue;
                    float hasta = t.X + acompHasta[t.Variant];
                    if (hasta > x)
                        return new Reserva(t.X + acompDesde[t.Variant], hasta);
                }
                return null;
            }

            public void Refresh(float camX, float halfWidth)
            {
                root.localPosition = new Vector3(ParallaxMath.LayerOriginX(camX, cfg.parallax), 0f, 0f);

                float center = ParallaxMath.LocalViewCenter(camX, cfg.parallax);
                layout.Query(center - halfWidth, center + halfWidth, visible);

                for (int i = 0; i < usadosPorVariante.Length; i++)
                    usadosPorVariante[i] = 0;

                for (int i = 0; i < visible.Count; i++)
                    Place(visible[i]);

                for (int vi = 0; vi < poolPorVariante.Length; vi++)
                {
                    var list = poolPorVariante[vi];
                    for (int i = usadosPorVariante[vi]; i < list.Count; i++)
                        if (list[i].gameObject.activeSelf)
                            list[i].gameObject.SetActive(false);
                }
            }

            void Place(TilePlacement tile)
            {
                int vi = tile.Variant;
                var v = cfg.variants[vi];
                float s = scales[vi];

                Transform inst = GetInstance(vi, v);
                if (inst == null)
                    return;

                // Compensa el pivot del sprite, sea cual sea: el tile siempre queda con su
                // borde izquierdo en tile.X y su base en la linea de la capa.
                inst.localScale = new Vector3(s, s, 1f);
                inst.localPosition = new Vector3(
                    tile.X - mins[vi].x * s,
                    cfg.baselineY + v.yOffset - mins[vi].y * s,
                    0f);

                if (!inst.gameObject.activeSelf)
                    inst.gameObject.SetActive(true);
            }

            Transform GetInstance(int variantIndex, SceneryVariant v)
            {
                var list = poolPorVariante[variantIndex];
                int used = usadosPorVariante[variantIndex];

                Transform inst;
                if (used < list.Count)
                {
                    inst = list[used];
                }
                else
                {
                    inst = CreateInstance(v);
                    if (inst == null)
                        return null;
                    list.Add(inst);
                }

                usadosPorVariante[variantIndex] = used + 1;
                return inst;
            }

            Transform CreateInstance(SceneryVariant v)
            {
                if (v.prefab == null)
                    return null;

                var go = UnityEngine.Object.Instantiate(v.prefab, root);

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                    PonerAcompanante(go.transform, sr.sprite, v);

                ShadowCasterQuality.Apply(go);

                if (sr != null)
                {
                    sr.sortingOrder = cfg.sortingOrder + v.ordenExtra;
                    sr.sortingLayerName = cfg.sortingLayer;
                    sr.color = cfg.tint;
                    // Solo pisa el material del prefab si la capa tiene uno propio asignado
                    // (Escenario.asset, campo "Material"); sin uno (caso normal: null en las ocho
                    // capas menos Cielo, que ahora tampoco lo tiene), el tile usa el material Lit
                    // que ya trae su prefab, para que reciba la luz de ambiente igual que el resto.
                    if (cfg.material != null)
                        sr.sharedMaterial = cfg.material;

                    if (cfg.proyectaSombra)
                    {
                        // Fija, sin mirar las luces: esta capa se mueve a otra velocidad que la de
                        // los edificios que las tienen, y una sombra que las siguiera se correria sola.
                        SombraProyectada.Poner(go, sr, CapaDeSombras, OrdenDeSombras, true, false);
                    }
                }

                // Cada luz que traiga el prefab pasa a proyectar sombras, sin cablear nada.
                FuenteDeSombra.AsegurarEn(go);

                return go.transform;
            }

            /// <summary>
            /// El acompanante va como hijo del tile: se mueve, se recicla y se apaga con el, asi
            /// nunca se despegan. Su esquina de abajo a la izquierda cae en el punto pedido del tile.
            /// </summary>
            static void PonerAcompanante(Transform tile, Sprite tileSprite, SceneryVariant v)
            {
                var acomp = Acompanante(v);
                if (acomp == null)
                    return;

                var go = UnityEngine.Object.Instantiate(v.acompanante, tile);
                go.name = v.acompanante.name;
                // Mismo dibujante, misma densidad de pixeles: se compensa si se importaron distinto.
                float k = tileSprite.pixelsPerUnit / acomp.sprite.pixelsPerUnit;
                var esquina = (Vector2)tileSprite.bounds.min + v.acompananteOffsetPx / tileSprite.pixelsPerUnit;
                go.transform.localScale = new Vector3(k, k, 1f);
                go.transform.localPosition = new Vector3(
                    esquina.x - acomp.sprite.bounds.min.x * k,
                    esquina.y - acomp.sprite.bounds.min.y * k,
                    0f);

                var sr = go.GetComponent<SpriteRenderer>();
                sr.sortingLayerName = v.acompananteCapa;
                sr.sortingOrder = v.acompananteOrden;
            }

            public void Dispose()
            {
                if (root != null)
                    UnityEngine.Object.Destroy(root.gameObject);
            }
        }
    }
}
