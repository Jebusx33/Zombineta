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

            public LayerRuntime(Transform parent, SceneryLayer cfg)
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
                    widths[i] = b.size.x * scales[i];
                    weights[i] = v.weight;
                    mins[i] = b.min;
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

                var go = Object.Instantiate(v.prefab, root);

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = cfg.sortingOrder;
                    sr.sortingLayerName = cfg.sortingLayer;
                    sr.color = cfg.tint;
                    if (cfg.material != null)
                        sr.sharedMaterial = cfg.material;
                }

                return go.transform;
            }

            public void Dispose()
            {
                if (root != null)
                    Object.Destroy(root.gameObject);
            }
        }
    }
}
