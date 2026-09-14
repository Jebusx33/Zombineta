using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;

namespace Zombineta.Juego.Levels
{
    /// <summary>
    /// La raiz de un nivel armado a mano. Sabe cuanto mide el nivel, junta los LevelItem de la
    /// escena en el recorrido que usa la simulacion, y mantiene la vista de cada item: en el
    /// editor lo acomoda a su carril y le pone la cara de su tipo; en juego lo apaga cuando la
    /// simulacion lo consume.
    ///
    /// Corre antes que RunController, que le pide el recorrido en su Awake.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [ExecuteAlways]
    public sealed class LevelScene : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] LevelItemPalette palette;

        [Tooltip("Metros hasta el refugio en este nivel. Reemplaza el goalDistance de GameConfig.")]
        [SerializeField] float goalDistance = 4000f;

        [Tooltip("Donde viven los items. Vacio = los hijos de este objeto.")]
        [SerializeField] Transform itemsRoot;

        [SerializeField] LevelGeneratorSettings generator = new LevelGeneratorSettings();

        public GameConfig Config => config;
        public LevelItemPalette Palette => palette;
        public float GoalDistance => goalDistance;
        public Transform ItemsRoot => itemsRoot != null ? itemsRoot : transform;
        public LevelGeneratorSettings Generator => generator;

        public LevelLayout Layout => config != null ? LevelLayout.From(config) : new LevelLayout(0.75f, 1.6f, 1.125f);

        public LevelItem[] Items => ItemsRoot.GetComponentsInChildren<LevelItem>(true);

        /// <summary>
        /// Clave de "Probar desde aca": la herramienta guarda ahi los metros antes de entrar en Play.
        /// Va en SessionState y no en un estatico, porque los estaticos se reinician al entrar en Play.
        /// </summary>
        public const string PlayFromKey = "zombineta.playFromMeters";

        readonly List<(LevelItem item, int index)> bound = new List<(LevelItem, int)>();
        RunController run;

        // --- Datos para la simulacion ------------------------------------------------

        /// <summary>Una copia de la configuracion con el largo de este nivel.</summary>
        public GameConfig RuntimeConfig(GameConfig source)
        {
            var copy = Instantiate(source != null ? source : config);
            copy.name = (source != null ? source.name : "GameConfig") + " (" + gameObject.scene.name + ")";
            copy.goalDistance = goalDistance;
            return copy;
        }

        /// <summary>El recorrido del nivel: un LevelEntry por cada item activo.</summary>
        public LevelDefinition BuildDefinition()
        {
            var layout = Layout;
            var definition = ScriptableObject.CreateInstance<LevelDefinition>();
            definition.name = gameObject.scene.name;
            foreach (var item in Items)
                if (item.gameObject.activeInHierarchy)
                    definition.entries.Add(item.ToEntry(layout));
            return definition;
        }

        /// <summary>
        /// Une cada item de la escena con su entrada en el LevelRuntime (que las ordena a su manera),
        /// para apagar el que la simulacion consume.
        /// </summary>
        public void Bind(RunController runController)
        {
            run = runController;
            bound.Clear();
            if (run == null || run.Level == null)
                return;

            var layout = Layout;
            var pool = new List<LevelItem>();
            foreach (var item in Items)
                if (item.gameObject.activeInHierarchy)
                    pool.Add(item);

            var runtime = run.Level.Items;
            for (int i = 0; i < runtime.Length; i++)
            {
                var entry = runtime[i].Entry;
                for (int k = 0; k < pool.Count; k++)
                {
                    var e = pool[k].ToEntry(layout);
                    if (e.kind == entry.kind && e.lane == entry.lane && e.variant == entry.variant &&
                        Mathf.Abs(e.distance - entry.distance) < 0.001f && Mathf.Abs(e.height - entry.height) < 0.001f)
                    {
                        bound.Add((pool[k], i));
                        pool.RemoveAt(k);
                        break;
                    }
                }
            }
        }

        void Start()
        {
            if (!Application.isPlaying || run == null || run.Sim == null)
                return;

#if UNITY_EDITOR
            // Probar desde aca: la moto arranca donde se estaba mirando la escena.
            float from = UnityEditor.SessionState.GetFloat(PlayFromKey, -1f);
            if (from >= 0f)
            {
                UnityEditor.SessionState.EraseFloat(PlayFromKey);
                float m = Mathf.Clamp(from, 0f, goalDistance - 10f);
                run.Sim.State.PlayerX = m;
                run.Sim.Horde.Reset(m - run.Config.startingGap);
                run.Sim.State.HordeX = run.Sim.Horde.FrontX;
            }
#endif
        }

        // --- Vista -------------------------------------------------------------------

        void Update()
        {
            if (Application.isPlaying)
            {
                if (run == null || run.Level == null)
                    return;
                var runtime = run.Level.Items;
                foreach (var (item, index) in bound)
                {
                    if (item == null)
                        continue;
                    var sr = item.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.enabled = !runtime[index].Consumed;
                }
                return;
            }

            // En el editor: cada item en su carril y con la cara de su tipo.
            foreach (var item in Items)
                ApplyVisual(item);
        }

        /// <summary>Paso de la grilla al mover un item, en metros.</summary>
        public const float GridMeters = 1f;

        /// <summary>
        /// Engancha el item a su carril y a la grilla. Si se lo arrastro, la posicion decide el
        /// carril y la X se redondea a la grilla; si no, el carril decide la altura.
        /// </summary>
        public void Snap(LevelItem item)
        {
            var layout = Layout;
            var p = (Vector2)item.transform.position;
            float meters = layout.ToMeters(p.x);

            bool fieldsChanged = !item.hasSnapshot || item.lane != item.snapshotLane ||
                                 Mathf.Abs(item.height - item.snapshotHeight) > 0.0001f;
            if (!fieldsChanged)
            {
                if (Mathf.Abs(p.y - item.snapshotPosition.y) > 0.0001f)
                    item.lane = layout.NearestLane(layout.GroundY(p.y, item.height));
                if (Mathf.Abs(p.x - item.snapshotPosition.x) > 0.0001f)
                    meters = LevelLayout.SnapMeters(meters, GridMeters);
            }

            var target = layout.ItemPosition(meters, item.lane, item.height);
            var current = item.transform.position;
            if (Mathf.Abs(current.x - target.x) > 0.0001f || Mathf.Abs(current.y - target.y) > 0.0001f || Mathf.Abs(current.z) > 0.0001f)
                item.transform.position = new Vector3(target.x, target.y, 0f);

            item.hasSnapshot = true;
            item.snapshotPosition = target;
            item.snapshotLane = item.lane;
            item.snapshotHeight = item.height;
        }

        public void ApplyVisual(LevelItem item)
        {
            if (item == null)
                return;

            Snap(item);

            var look = palette != null ? palette.Get(item.kind) : null;
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = item.gameObject.AddComponent<SpriteRenderer>();

            var sprite = look != null ? look.sprite : null;
            var color = look != null ? look.color : Color.magenta;
            var scale = look != null ? new Vector3(look.scale.x, look.scale.y, 1f) : Vector3.one;
            int order = look != null ? look.sortingOrder : 6;

            if (item.kind == LevelEntryKind.ZombieFront && config != null && config.zombies != null)
                color = config.zombies.Get(item.variant).tint;

            if (sr.sprite != sprite) sr.sprite = sprite;
            if (sr.color != color) sr.color = color;
            if (sr.sortingOrder != order) sr.sortingOrder = order;
            bool flip = item.kind == LevelEntryKind.ZombieFront;   // mira hacia la jugadora
            if (sr.flipX != flip) sr.flipX = flip;
            if (item.transform.localScale != scale) item.transform.localScale = scale;
        }

        /// <summary>El recorrido tal como esta en la escena, para validarlo.</summary>
        public List<LevelEntry> CurrentEntries()
        {
            var layout = Layout;
            var entries = new List<LevelEntry>();
            foreach (var item in Items)
                if (item.gameObject.activeInHierarchy)
                    entries.Add(item.ToEntry(layout));
            return entries;
        }

#if UNITY_EDITOR
        // --- Guias -------------------------------------------------------------------

        static readonly Color LaneColor = new Color(1f, 1f, 1f, 0.25f);
        static readonly Color RulerColor = new Color(1f, 1f, 1f, 0.5f);
        static readonly Color StartColor = new Color(0.3f, 0.9f, 0.4f, 0.9f);
        static readonly Color GoalColor = new Color(0.3f, 0.7f, 1f, 0.9f);
        static readonly Color IssueColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        static readonly Color PinColor = new Color(1f, 0.85f, 0.2f, 0.9f);

        void OnDrawGizmos()
        {
            if (Application.isPlaying || config == null)
                return;

            var layout = Layout;
            float goalX = layout.ToWorldX(goalDistance);
            float bottom = layout.LaneY(0) - layout.LaneSpacing * 0.5f;
            float top = layout.LaneY(RunSimulation.LaneCount - 1) + layout.LaneSpacing * 0.5f;

            // Solo lo que se ve: el nivel mide miles de unidades.
            float minX = 0f, maxX = goalX;
            var view = UnityEditor.SceneView.currentDrawingSceneView;
            if (view != null && view.camera != null && view.camera.orthographic)
            {
                var cam = view.camera;
                float half = cam.orthographicSize * cam.aspect;
                minX = Mathf.Max(minX, cam.transform.position.x - half);
                maxX = Mathf.Min(maxX, cam.transform.position.x + half);
            }

            Gizmos.color = LaneColor;
            for (int lane = 0; lane < RunSimulation.LaneCount; lane++)
                Gizmos.DrawLine(new Vector3(minX, layout.LaneY(lane)), new Vector3(maxX, layout.LaneY(lane)));

            // Regla cada 50 m.
            var labelStyle = new GUIStyle(UnityEditor.EditorStyles.miniBoldLabel) { normal = { textColor = RulerColor } };
            const float step = 50f;
            for (float m = Mathf.Ceil(layout.ToMeters(minX) / step) * step; m <= layout.ToMeters(maxX); m += step)
            {
                float x = layout.ToWorldX(m);
                Gizmos.color = RulerColor;
                Gizmos.DrawLine(new Vector3(x, bottom), new Vector3(x, bottom - 0.4f));
                UnityEditor.Handles.Label(new Vector3(x, bottom - 0.4f), m.ToString("0") + " m", labelStyle);
            }

            Gizmos.color = StartColor;
            Gizmos.DrawLine(new Vector3(0f, bottom), new Vector3(0f, top));
            UnityEditor.Handles.Label(new Vector3(0f, top + 0.4f), "Largada", labelStyle);
            Gizmos.color = GoalColor;
            Gizmos.DrawLine(new Vector3(goalX, bottom), new Vector3(goalX, top));
            UnityEditor.Handles.Label(new Vector3(goalX, top + 0.4f), "Refugio " + goalDistance.ToString("0") + " m", labelStyle);

            // Fijados.
            Gizmos.color = PinColor;
            foreach (var item in Items)
            {
                if (!item.pinned && !item.IsTouchedSinceGeneration(layout))
                    continue;
                var p = item.transform.position;
                if (p.x < minX - 1f || p.x > maxX + 1f)
                    continue;
                Gizmos.DrawWireSphere(p + new Vector3(0f, 0.55f), 0.12f);
            }

            // Problemas en rojo.
            var issueStyle = new GUIStyle(labelStyle) { normal = { textColor = IssueColor } };
            foreach (var issue in LevelValidator.Check(CurrentEntries()))
            {
                float x = layout.ToWorldX(issue.distance);
                if (x < minX - 1f || x > maxX + 1f)
                    continue;
                Gizmos.color = IssueColor;
                if (issue.lane < 0)
                {
                    Gizmos.DrawLine(new Vector3(x, bottom), new Vector3(x, top));
                    UnityEditor.Handles.Label(new Vector3(x, top + 0.8f), issue.Message, issueStyle);
                }
                else
                {
                    float y = layout.LaneY(issue.lane);
                    Gizmos.DrawWireCube(new Vector3(x, y), new Vector3(1.2f, 1.2f, 0f));
                    UnityEditor.Handles.Label(new Vector3(x, y + 0.9f), issue.Message, issueStyle);
                }
            }
        }
#endif
    }
}
