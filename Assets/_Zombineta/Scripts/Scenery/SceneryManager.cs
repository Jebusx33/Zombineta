using System.Collections.Generic;
using UnityEngine;

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
                        layers.Add(new LayerRuntime(transform, cfg));

                builtVersion = tileset.Version;
            }
        }

        /// <summary>Una capa viva: su raiz, su reparto de tiles y los sprites que recicla.</summary>
        sealed class LayerRuntime
        {
            readonly SceneryLayer cfg;
            readonly Transform root;
            readonly SceneryLayout layout;
            readonly float[] scales;
            readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
            readonly List<TilePlacement> visible = new List<TilePlacement>();

            public LayerRuntime(Transform parent, SceneryLayer cfg)
            {
                this.cfg = cfg;

                root = new GameObject(cfg.name).transform;
                root.SetParent(parent, false);

                int n = cfg.variants.Count;
                var widths = new float[n];
                var weights = new float[n];
                scales = new float[n];

                for (int i = 0; i < n; i++)
                {
                    var v = cfg.variants[i];
                    if (v == null || v.sprite == null)
                        continue; // ancho 0: el layout la ignora

                    Vector3 size = v.sprite.bounds.size;
                    scales[i] = cfg.height * v.heightScale / size.y;
                    widths[i] = size.x * scales[i];
                    weights[i] = v.weight;
                }

                layout = new SceneryLayout(widths, weights, cfg.gapChance, cfg.gapMin,
                                           cfg.gapMax, cfg.noImmediateRepeat, cfg.seed,
                                           LayoutStartX);
            }

            public void Refresh(float camX, float halfWidth)
            {
                root.localPosition = new Vector3(ParallaxMath.LayerOriginX(camX, cfg.parallax), 0f, 0f);

                float center = ParallaxMath.LocalViewCenter(camX, cfg.parallax);
                layout.Query(center - halfWidth, center + halfWidth, visible);

                while (pool.Count < visible.Count)
                    pool.Add(CreateRenderer());

                for (int i = 0; i < visible.Count; i++)
                    Place(pool[i], visible[i]);

                for (int i = visible.Count; i < pool.Count; i++)
                    if (pool[i].enabled)
                        pool[i].enabled = false;
            }

            void Place(SpriteRenderer sr, TilePlacement tile)
            {
                var v = cfg.variants[tile.Variant];
                float s = scales[tile.Variant];

                if (sr.sprite != v.sprite)
                    sr.sprite = v.sprite;

                // Compensa el pivot del sprite, sea cual sea: el tile siempre queda con su
                // borde izquierdo en tile.X y su base en la linea de la capa.
                Bounds b = v.sprite.bounds;
                sr.transform.localScale = new Vector3(s, s, 1f);
                sr.transform.localPosition = new Vector3(
                    tile.X - b.min.x * s,
                    cfg.baselineY + v.yOffset - b.min.y * s,
                    0f);

                sr.enabled = true;
            }

            SpriteRenderer CreateRenderer()
            {
                var go = new GameObject("Tile");
                go.transform.SetParent(root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = cfg.sortingOrder;
                sr.color = cfg.tint;
                if (cfg.material != null)
                    sr.sharedMaterial = cfg.material;
                sr.enabled = false;
                return sr;
            }

            public void Dispose()
            {
                if (root != null)
                    Object.Destroy(root.gameObject);
            }
        }
    }
}
