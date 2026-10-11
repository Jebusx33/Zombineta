using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;
using Zombineta.Fx;
using Zombineta.Level;
using Zombineta.Luz;

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

        [Tooltip("El ritmo del nivel, de la largada al refugio: presion de la horda, luz y densidad " +
                 "de cada pedazo. Vacio = todo parejo, como siempre.")]
        [SerializeField] List<Tramo> tramos = new List<Tramo>();

        [Header("Looks")]
        [Tooltip("Aspectos por tipo de zombie, para la cara del ZombieFront. Sin asignar: tinte de Zombies.asset.")]
        [SerializeField] ZombieLookSet looks;
        [Tooltip("Semilla del sorteo de look por item: mismo seed, misma pinta.")]
        [SerializeField] int lookSeed = 12345;

        [Header("Sombra")]
        [SerializeField] Sprite shadowSprite;
        [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.45f);

        [Header("Luz")]
        [Tooltip("Ambiente de este nivel: cuanta luz recibe cada capa de dibujo. Lo usa LightingDirector.")]
        [SerializeField] PerfilDeLuz perfil;

        public GameConfig Config => config;
        public LevelItemPalette Palette => palette;
        public float GoalDistance => goalDistance;
        public Transform ItemsRoot => itemsRoot != null ? itemsRoot : transform;
        public LevelGeneratorSettings Generator => generator;
        public List<Tramo> Tramos => tramos;
        public PerfilDeLuz Perfil => perfil;

        // Sin config asignada: usar los valores actuales de Settings/GameConfig.asset (no los
        // defaults viejos de la clase GameConfig, que ya no coinciden con el asset en uso).
        public LevelLayout Layout => config != null ? LevelLayout.From(config) : new LevelLayout(0.75f, 0.99f, 2.1375f);

        public LevelItem[] Items => ItemsRoot.GetComponentsInChildren<LevelItem>(true);

        /// <summary>
        /// Clave de "Probar desde aca": la herramienta guarda ahi los metros antes de entrar en Play.
        /// Va en SessionState y no en un estatico, porque los estaticos se reinician al entrar en Play.
        /// </summary>
        public const string PlayFromKey = "zombineta.playFromMeters";

        /// <summary>Nombre fijo del hijo que dibuja la sombra: asi se lo puede encontrar y reusar.</summary>
        const string ShadowChildName = "Sombra";

        /// <summary>Nombre fijo del hijo que instancia el prefab del look: asi se lo puede encontrar,
        /// reusar y (en juego) apagar junto con el item consumido.</summary>
        const string LookChildName = "Vista";

        readonly List<(LevelItem item, int index, GroundShadow shadow, GameObject look)> bound =
            new List<(LevelItem, int, GroundShadow, GameObject)>();
        RunController run;

        // --- Datos para la simulacion ------------------------------------------------

        /// <summary>Una copia de la configuracion con el largo de este nivel.</summary>
        public GameConfig RuntimeConfig(GameConfig source)
        {
            var copy = Instantiate(source != null ? source : config);
            copy.name = (source != null ? source.name : "GameConfig") + " (" + gameObject.scene.name + ")";
            copy.goalDistance = goalDistance;
            copy.tramos = tramos;
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
        /// En juego (Play o build) la vista del prefab tiene que existir desde el arranque: el
        /// Update de este componente en juego solo prende/apaga (no instancia), y la instancia que
        /// arma el editor es HideFlags.DontSave, asi que una escena recien cargada no la trae
        /// guardada. Se arma una sola vez aca, con Instantiate comun (no PrefabUtility, que es de
        /// editor). Por DefaultExecutionOrder(-200) esto corre antes que RunController.Awake()
        /// (-100), que es quien llama a Bind() y busca el hijo "Vista".
        /// </summary>
        void Awake()
        {
            if (!Application.isPlaying)
                return;

            foreach (var item in Items)
            {
                EnsurePlayLook(item);
                EnsurePlayShadow(item);
            }
        }

        /// <summary>
        /// En juego, lo que esta en el piso tira su sombra proyectada; la sombra ovalada que
        /// pueda venir guardada en la escena se va. Lo que flota la conserva: marca su carril.
        /// </summary>
        void EnsurePlayShadow(LevelItem item)
        {
            if (item.kind == LevelEntryKind.Ramp || item.height > 0f)
                return;

            var vieja = item.transform.Find(ShadowChildName);
            if (vieja != null)
            {
                vieja.gameObject.SetActive(false);
                Destroy(vieja.gameObject);
            }

            ApplyProjectedShadow(item);
        }

        /// <summary>Sombra proyectada del item, sobre el sprite que de verdad se ve (el del prefab
        /// "Vista" si lo hay, o el propio). Debajo de todo lo de su carril.</summary>
        void ApplyProjectedShadow(LevelItem item)
        {
            var vista = item.transform.Find(LookChildName);
            var sr = vista != null ? vista.GetComponent<SpriteRenderer>() : null;
            if (sr == null)
                sr = item.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;

            SombraProyectada.Poner(sr.gameObject, sr, "", LaneSorting.Order(item.lane, SortSlot.Shadow), true);
        }

        static void RemoveProjectedShadow(LevelItem item)
        {
            foreach (var sombra in item.GetComponentsInChildren<SombraProyectada>(true))
            {
                if (Application.isPlaying)
                    Destroy(sombra);
                else
                    DestroyImmediate(sombra);
            }
        }

        void EnsurePlayLook(LevelItem item)
        {
            var look = palette != null ? palette.Get(item.kind, item.variant) : null;
            if (look == null)
                return;

            // Siempre aplica la escala de la paleta al entrar en Play, incluso si no hay prefab
            // o si el editor ya creo el hijo "Vista" en esta sesion (sin reload de escena).
            // Sin esto, la escala guardada en el .unity (ej. 1.9 del asset original) persiste
            // en Play aunque en el editor se vea bien porque ApplyVisual la pisa cada frame.
            // ZombieFront excepcion: su escala combina palette * typeScale * lookScale (calculado
            // en ApplyVisual); conserva la que guardo el editor en la escena.
            if (item.kind != LevelEntryKind.ZombieFront)
            {
                var scale = new Vector3(look.scale.x, look.scale.y, 1f);
                if (item.transform.localScale != scale)
                    item.transform.localScale = scale;
            }

            if (look.prefab == null)
                return;

            // El sprite propio del item no se pinta cuando hay prefab (ver ApplyVisual): puede
            // haber quedado guardado en true en una escena que nunca paso por el editor con esta
            // paleta. Apagarlo aca de nuevo es gratis e idempotente.
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null && sr.enabled)
                sr.enabled = false;

            var existente = item.transform.Find(LookChildName);
            if (existente != null)
            {
                SyncRotation(existente, look);
                ApoyarVista(item, look, existente);
                return; // Ya la armo el editor en esta misma sesion (sin reload de escena).
            }

            var go = Instantiate(look.prefab, item.transform);
            go.name = LookChildName;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = look.prefab.transform.localRotation; // la inclinacion del prefab (arte) se respeta
            go.transform.localScale = Vector3.one;
            go.hideFlags = HideFlags.None;
            ApoyarVista(item, look, go.transform);
            ShadowCasterQuality.Apply(go);

            var prefabSr = go.GetComponent<SpriteRenderer>();
            if (prefabSr != null)
            {
                prefabSr.sortingLayerName = LaneSorting.GameLayer;
                prefabSr.sortingOrder = ItemOrder(item);
            }
        }

        /// <summary>Orden de dibujo de un item: lo mismo que calculaba ApplyVisual, factoreado para
        /// que EnsurePlayLook (juego) y ApplyVisual (editor) no se puedan desincronizar.</summary>
        static int ItemOrder(LevelItem item)
        {
            bool isRamp = item.kind == LevelEntryKind.Ramp;
            return isRamp
                ? LaneSorting.Order(item.lane, SortSlot.Shadow) + 1
                : LaneSorting.Order(item.lane, SortSlot.Item);
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
                        var shadowT = pool[k].transform.Find(ShadowChildName);
                        var shadow = shadowT != null ? shadowT.GetComponent<GroundShadow>() : null;
                        var lookT = pool[k].transform.Find(LookChildName);
                        var look = lookT != null ? lookT.gameObject : null;
                        bound.Add((pool[k], i, shadow, look));
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
                foreach (var (item, index, shadow, look) in bound)
                {
                    if (item == null)
                        continue;
                    bool visible = !runtime[index].Consumed;
                    // Con prefab, la vista vive en "Vista" (hijo): apagar ese GameObject entero
                    // (sprite, luz y sombra propia del prefab juntos). Sin prefab, el sprite sigue
                    // en el propio item, como antes.
                    if (look != null)
                        look.SetActive(visible);
                    else
                    {
                        var sr = item.GetComponent<SpriteRenderer>();
                        if (sr != null)
                            sr.enabled = visible;
                    }
                    if (shadow != null)
                        shadow.Visible = visible;
                }
                return;
            }

            // En el editor: cada item en su carril y con la cara de su tipo. Un solo fetch de
            // Items por frame (GetComponentsInChildren aloca): el indice de cada item sale de su
            // posicion aca, no de buscarlo de nuevo adentro de ApplyVisual.
            var items = Items;
            for (int i = 0; i < items.Length; i++)
                ApplyVisual(items[i], i);
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

        /// <summary>
        /// Para llamadas externas de a un item (por ejemplo LevelEditorActions), sin indice a
        /// mano: lo busca una vez en Items. El bucle de Update ya tiene el indice y llama
        /// directamente a la sobrecarga de abajo, sin este fetch por item.
        /// </summary>
        public void ApplyVisual(LevelItem item)
        {
            if (item == null)
                return;

            ApplyVisual(item, IndexOf(item));
        }

        public void ApplyVisual(LevelItem item, int index)
        {
            if (item == null)
                return;

            Snap(item);

            var look = palette != null ? palette.Get(item.kind, item.variant) : null;
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = item.gameObject.AddComponent<SpriteRenderer>();

            var sprite = look != null ? look.sprite : null;
            var color = look != null ? look.color : Color.magenta;
            var scale = look != null ? new Vector3(look.scale.x, look.scale.y, 1f) : Vector3.one;

            // La rampa se pisa: va justo encima de la sombra del carril, no donde van los items.
            bool isRamp = item.kind == LevelEntryKind.Ramp;
            int order = ItemOrder(item);

            // Escala del look del ZombieFront: empareja la resolucion de su hoja, no el tamano del cuerpo,
            // asi que la sombra (hija del item) la descuenta.
            float lookScale = 1f;

            if (item.kind == LevelEntryKind.ZombieFront)
            {
                ZombieLook zombieLook = null;
                if (looks != null)
                {
                    var pool = looks.For(item.variant);
                    int idx = ZombieLookPicker.Pick(lookSeed, index, 0, pool.Count, -1);
                    zombieLook = idx >= 0 ? pool[idx] : null;
                }

                if (zombieLook != null && zombieLook.walk != null && zombieLook.walk.Length > 0)
                {
                    sprite = zombieLook.walk[0];
                    color = Color.white;
                    if (zombieLook.scale > 0f)
                        lookScale = zombieLook.scale;
                }
                else if (config != null && config.zombies != null)
                {
                    color = config.zombies.Get(item.variant).tint;
                }

                // Mismo tamano que en la horda: escala del tipo y del look sobre la de la paleta.
                float typeScale = config != null && config.zombies != null ? config.zombies.Get(item.variant).scale : 1f;
                scale = new Vector3(scale.x * typeScale * lookScale, scale.y * typeScale * lookScale, 1f);
            }

            // El carril y el generador siguen decidiendo la escala del item, tenga o no prefab.
            if (item.transform.localScale != scale) item.transform.localScale = scale;

            if (look != null && look.prefab != null)
            {
                // La vista viene del prefab: el propio SpriteRenderer del item queda apagado y sin
                // pintar, para no dibujar el sprite dos veces.
                ApplyLookPrefab(item, look, order);
                if (sr.enabled) sr.enabled = false;
            }
            else
            {
                RemoveLookPrefab(item);
                if (!sr.enabled) sr.enabled = true;
                if (sr.sprite != sprite) sr.sprite = sprite;
                if (sr.color != color) sr.color = color;
                if (sr.sortingOrder != order) sr.sortingOrder = order;
                if (sr.sortingLayerName != LaneSorting.GameLayer) sr.sortingLayerName = LaneSorting.GameLayer;
                bool flip = item.kind == LevelEntryKind.ZombieFront;   // mira hacia la jugadora
                if (sr.flipX != flip) sr.flipX = flip;
            }

            // La rampa ya se pisa: no necesita sombra. Lo que flota lleva la ovalada, que marca
            // su carril; lo que esta en el piso tira su sombra proyectada.
            if (isRamp)
            {
                RemoveShadow(item);
                RemoveProjectedShadow(item);
            }
            else if (item.height > 0f)
            {
                RemoveProjectedShadow(item);
                ApplyShadow(item, look, lookScale);
            }
            else
            {
                RemoveShadow(item);
                ApplyProjectedShadow(item);
            }
        }

        /// <summary>
        /// Posicion del item dentro de Items: la usa el sorteo de look para que cada ZombieFront
        /// tenga siempre la misma pinta, sin importar cuantas veces se llame a ApplyVisual. Solo
        /// la usa la sobrecarga de un item sin indice; el bucle de Update ya trae el suyo.
        /// </summary>
        int IndexOf(LevelItem item)
        {
            var items = Items;
            for (int i = 0; i < items.Length; i++)
                if (items[i] == item)
                    return i;
            return 0;
        }

        GroundShadow ApplyShadow(LevelItem item, LevelItemPalette.Look look, float lookScale = 1f)
        {
            var t = item.transform.Find(ShadowChildName);
            GroundShadow shadow;
            if (t == null)
            {
                var go = new GameObject(ShadowChildName);
                go.transform.SetParent(item.transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = shadowSprite;
                sr.color = shadowColor;
                shadow = go.AddComponent<GroundShadow>();
                shadow.Init(sr);
            }
            else
            {
                shadow = t.GetComponent<GroundShadow>();
            }

            if (shadow == null)
                return null;

            // En el editor GroundShadow no pasa por Awake al abrir la escena: sin esto su alfa de
            // referencia queda en 1 y Place guarda la sombra opaca en la escena.
            shadow.SetColor(shadowColor);
            shadow.Width = (look != null ? look.shadowWidth : 1f) / (lookScale > 0f ? lookScale : 1f);

            var layout = Layout;
            float groundY = layout.LaneY(item.lane);
            float heightWorld = item.height * layout.JumpHeightToWorld;
            shadow.Place(item.transform.position.x, groundY, heightWorld, item.lane);
            return shadow;
        }

        void RemoveShadow(LevelItem item)
        {
            var t = item.transform.Find(ShadowChildName);
            if (t == null)
                return;

            if (Application.isPlaying)
                Destroy(t.gameObject);
            else
                DestroyImmediate(t.gameObject);
        }

        /// <summary>
        /// Instancia (o reusa) el prefab del look como hijo fijo "Vista": arte le da luz y sombra
        /// propias sin tocar este script. Solo la capa y el orden de dibujo los sigue poniendo el
        /// sistema, igual que antes con el sprite propio del item.
        /// </summary>
        void ApplyLookPrefab(LevelItem item, LevelItemPalette.Look look, int order)
        {
            var t = item.transform.Find(LookChildName);
            GameObject go = t != null ? t.gameObject : null;

            bool needsNew = go == null;
#if UNITY_EDITOR
            if (!needsNew && UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(go) != (Object)look.prefab)
                needsNew = true;
#endif

            if (needsNew)
            {
                if (go != null)
                {
                    if (Application.isPlaying)
                        Destroy(go);
                    else
                        DestroyImmediate(go);
                }

#if UNITY_EDITOR
                go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(look.prefab, item.transform);
#else
                go = Instantiate(look.prefab, item.transform);
#endif
                go.name = LookChildName;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = look.prefab.transform.localRotation; // la inclinacion del prefab (arte) se respeta
                go.transform.localScale = Vector3.one;
                go.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSave;
            }

            // La raiz de una instancia de prefab no hereda su rotacion: se copia aca, asi ajustar
            // la inclinacion en el prefab se ve en la escena sin reabrirla.
            SyncRotation(go.transform, look);
            ApoyarVista(item, look, go.transform);

            // Contrato del prefab: sprite y material en la raiz. Sombra/luz (si las tiene) son
            // hijas suyas y no necesitan capa ni orden propios.
            var prefabSr = go.GetComponent<SpriteRenderer>();
            if (prefabSr != null)
            {
                if (prefabSr.sortingLayerName != LaneSorting.GameLayer) prefabSr.sortingLayerName = LaneSorting.GameLayer;
                if (prefabSr.sortingOrder != order) prefabSr.sortingOrder = order;
            }
        }

        /// <summary>Cuanto queda la base del dibujo por debajo de la linea del carril, en unidades
        /// de mundo: la huella de algo apoyado en el piso visto de tres cuartos.</summary>
        const float HuellaBajoElCarril = 0.3f;

        /// <summary>
        /// Apoya la vista de un obstaculo (o barril) en su carril: sube o baja el dibujo hasta
        /// que su punto mas bajo queda apenas por debajo de la linea, igual que los pies de un
        /// personaje. Asi se lee en que carril esta sin importar donde tenga el pivot el sprite
        /// ni cuanto este girado. Es solo visual: el choque sigue siendo la linea del carril.
        /// </summary>
        static void ApoyarVista(LevelItem item, LevelItemPalette.Look look, Transform vista)
        {
            float y = 0f;
            bool seApoya = (item.kind == LevelEntryKind.Obstacle || item.kind == LevelEntryKind.Barrel) && item.height <= 0f;
            var sr = seApoya ? vista.GetComponent<SpriteRenderer>() : null;
            float escala = Mathf.Abs(item.transform.lossyScale.y);
            if (sr != null && sr.sprite != null && escala > 0.0001f)
            {
                float masBajo = SpriteApoyo.MasBajo(sr.sprite, vista.localEulerAngles.z);
                y = -masBajo + (look.ajusteY - HuellaBajoElCarril) / escala;
            }

            var posicion = new Vector3(0f, y, 0f);
            if (vista.localPosition != posicion)
                vista.localPosition = posicion;
        }

        static void SyncRotation(Transform vista, LevelItemPalette.Look look)
        {
            var rotation = look.prefab.transform.localRotation;
            if (vista.localRotation != rotation)
                vista.localRotation = rotation;
        }

        void RemoveLookPrefab(LevelItem item)
        {
            var t = item.transform.Find(LookChildName);
            if (t == null)
                return;

            if (Application.isPlaying)
                Destroy(t.gameObject);
            else
                DestroyImmediate(t.gameObject);
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
        static readonly Color TramoColor = new Color(1f, 0.55f, 0.9f, 0.9f);

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

            // Tramos: donde termina cada uno, con su ritmo. Los apagones, sombreados.
            float desde = 0f;
            var tramoStyle = new GUIStyle(labelStyle) { normal = { textColor = TramoColor } };
            foreach (var tramo in tramos)
            {
                if (tramo == null)
                    continue;
                float hasta = Mathf.Min(tramo.hasta, goalDistance);
                float x0 = layout.ToWorldX(desde), x1 = layout.ToWorldX(hasta);
                if (x1 >= minX && x0 <= maxX)
                {
                    if (tramo.oscuridad > 0f)
                    {
                        float a = Mathf.Max(x0, minX), b = Mathf.Min(x1, maxX);
                        Gizmos.color = new Color(0f, 0f, 0f, 0.35f * tramo.oscuridad);
                        Gizmos.DrawCube(new Vector3((a + b) * 0.5f, (top + bottom) * 0.5f), new Vector3(b - a, top - bottom, 0f));
                    }
                    Gizmos.color = TramoColor;
                    Gizmos.DrawLine(new Vector3(x1, bottom), new Vector3(x1, top + 1.2f));
                    UnityEditor.Handles.Label(new Vector3(Mathf.Max(x0, minX) + 0.2f, top + 1.4f),
                        tramo.nombre + "  horda x" + tramo.presionHorda.ToString("0.##") +
                        (tramo.oscuridad > 0f ? "  APAGON" : ""), tramoStyle);
                }
                desde = hasta;
            }

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
