using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zombineta.Juego.Levels;
using Zombineta.Luz;
using Zombineta.Player;
using Zombineta.Scenery;

namespace Zombineta.Juego.EditorTools.Luz
{
    /// <summary>
    /// Arma "TallerLuz.unity": una escena con una instancia de cada prefab de escenografia
    /// (Escenario.asset) y de cada item (LevelItemPalette.asset), a su tamano y altura reales,
    /// mas una copia estatica de la moto y el ambiente nocturno (Noche.asset via
    /// LightingDirector). Sirve para que arte tunee luces y compare assets sin jugar el nivel.
    ///
    /// Cada capa del escenario es una "fila": todas sus variantes side-by-side, en su propia
    /// franja de X (para que no se tapen entre capas que en el juego comparten altura), pero a
    /// la altura Y real (baselineY) de esa capa. Las capas con SceneryLayer.enabled == false (que
    /// SceneryManager no arma en el juego, ver SceneryManager.Rebuild) se muestran igual, pero en
    /// un bloque aparte por debajo del de las activas, atenuadas y con el titulo marcado, para que
    /// no se confundan con lo que se ve jugando. Regenerar pisa la escena entera (NewScene), asi
    /// que correrlo dos veces seguidas no duplica nada.
    /// </summary>
    public static class TallerBuilder
    {
        const string ScenesDir = "Assets/_Zombineta/Scenes";
        const string ScenePath = ScenesDir + "/TallerLuz.unity";
        const string TilesetPath = "Assets/_Zombineta/Settings/Escenario.asset";
        const string PalettePath = "Assets/_Zombineta/Settings/LevelItemPalette.asset";
        const string NochePath = "Assets/_Zombineta/Settings/Luz/Noche.asset";
        const string NivelBasePath = "Assets/_Zombineta/Scenes/Templates/NivelBase.unity";

        const float TileGap = 2f;
        const float RowGap = 6f;

        /// <summary>Separacion vertical entre el bloque de capas activas y el de apagadas.</summary>
        const float ShelfGap = 5f;

        /// <summary>Alfa que llevan los tiles de una capa apagada en el juego, para no confundirlos
        /// con lo que se ve jugando.</summary>
        const float ApagadaAlfa = 0.5f;

        const string SufijoApagada = " (apagada en el juego)";

        [MenuItem("Zombineta/Luz/Construir taller")]
        static void ConstruirDesdeMenu() => Build(true);

        /// <param name="askFirst">True pide confirmacion (uso desde el menu). False la salta,
        /// para que la verificacion por MCP no se quede esperando un dialogo.</param>
        public static void Build(bool askFirst)
        {
            if (askFirst && !EditorUtility.DisplayDialog("Reconstruir el taller de luz",
                    "Esto regenera TallerLuz.unity y pisa cualquier cambio hecho a mano en ella.",
                    "Reconstruir", "Cancelar"))
                return;

            Directory.CreateDirectory(ScenesDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Escena nueva = se descargan los assets sin referencias en la escena anterior:
            // recargar todo lo que se va a usar recien aca, no antes.
            var tileset = AssetDatabase.LoadAssetAtPath<SceneryTileset>(TilesetPath);
            var palette = AssetDatabase.LoadAssetAtPath<LevelItemPalette>(PalettePath);
            var noche = AssetDatabase.LoadAssetAtPath<PerfilDeLuz>(NochePath);

            if (tileset == null)
                Debug.LogError("TallerBuilder: no se encontro el SceneryTileset en " + TilesetPath);
            if (palette == null)
                Debug.LogError("TallerBuilder: no se encontro la LevelItemPalette en " + PalettePath);
            if (noche == null)
                Debug.LogError("TallerBuilder: no se encontro el PerfilDeLuz en " + NochePath);

            var filas = new GameObject("Filas").transform;

            float cursor = 0f;
            int filasCreadas = 0;
            int filasApagadasCreadas = 0;
            Bounds? primeraFila = null;

            if (tileset != null)
            {
                // Solo las capas habilitadas son las que arma SceneryManager en el juego real
                // (ver SceneryManager.Rebuild: "if (cfg != null && cfg.enabled)"). Las apagadas se
                // muestran igual (arte puede querer tunearlas antes de prenderlas) pero en un
                // bloque aparte, mas abajo, para que nadie las confunda con lo que se ve jugando.
                var layers = tileset.layers.Where(l => l != null).ToList();
                var activas = layers.Where(l => l.enabled).ToList();
                var apagadas = layers.Where(l => !l.enabled).ToList();

                float minActivaY = float.PositiveInfinity;

                foreach (var layer in activas)
                {
                    cursor = BuildSceneryRow(filas, layer, cursor, true, 0f, out Bounds? rowBounds);
                    if (rowBounds.HasValue)
                    {
                        filasCreadas++;
                        if (primeraFila == null)
                            primeraFila = rowBounds;
                        minActivaY = Mathf.Min(minActivaY, rowBounds.Value.min.y);
                    }
                }

                if (apagadas.Count > 0)
                {
                    // Un solo corrimiento vertical para todo el bloque de apagadas: deja su punto
                    // mas alto (estimado con baselineY + height, sin compensar pivot: alcanza para
                    // separar con margen) a ShelfGap por debajo del punto mas bajo del bloque
                    // activo, sin per de la altura relativa entre las capas apagadas entre si.
                    float techoApagadas = apagadas.Max(l => l.baselineY + l.height);
                    float pisoActivas = float.IsPositiveInfinity(minActivaY) ? 0f : minActivaY;
                    float corrimiento = pisoActivas - ShelfGap - techoApagadas;

                    float cursorApagadas = 0f;
                    foreach (var layer in apagadas)
                    {
                        cursorApagadas = BuildSceneryRow(filas, layer, cursorApagadas, false, corrimiento, out Bounds? rowBounds);
                        if (rowBounds.HasValue)
                            filasApagadasCreadas++;
                    }
                }
            }

            Bounds? filaItems = null;
            if (palette != null)
                cursor = BuildItemsRow(filas, palette, cursor, out filaItems);

            var ambiente = new GameObject("Ambiente");
            var director = ambiente.AddComponent<LightingDirector>();
            director.Perfil = noche;
            director.Rebuild();

            Bounds? scooter = BuildScooterCopy(scene, new Vector3(-9f, 0f, 0f));

            Bounds foco = primeraFila ?? filaItems ?? scooter ?? new Bounds(new Vector3(10f, 2f, 0f), new Vector3(24f, 14f, 0f));
            if (scooter.HasValue)
                foco.Encapsulate(scooter.Value);
            BuildCamera(foco);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneDisabled(ScenePath);

            Debug.Log("TallerLuz: " + filasCreadas + " fila(s) de escenografia activa, " +
                      filasApagadasCreadas + " fila(s) apagada(s) en el juego, fila de items " +
                      (filaItems.HasValue ? "creada" : "omitida") + ", moto de referencia " +
                      (scooter.HasValue ? "copiada" : "omitida") + ". Guardada en " + ScenePath + ".");
        }

        // --- Filas de escenografia --------------------------------------------------

        /// <summary>Una fila con cada variante de una capa, side-by-side, a la altura real de esa
        /// capa (mas 'yShift', que solo usan las capas apagadas para caer en su propio bloque).
        /// Una capa apagada en el juego (SceneryLayer.enabled == false, ver 'activa') se arma igual
        /// pero atenuada, para que arte la vea sin confundirla con lo que se ve jugando.</summary>
        static float BuildSceneryRow(Transform parent, SceneryLayer layer, float startX, bool activa, float yShift, out Bounds? bounds)
        {
            bounds = null;

            var variants = layer.variants
                .Where(v => v != null && v.prefab != null)
                .OrderBy(v => v.grupo ?? string.Empty)
                .ToList();

            if (variants.Count == 0)
                return startX;

            var rowRoot = new GameObject("Fila - " + layer.name + (activa ? string.Empty : " (apagada)")).transform;
            rowRoot.SetParent(parent, false);

            float cursor = startX;
            Bounds? acc = null;

            foreach (var v in variants)
            {
                var srPrefab = v.prefab.GetComponent<SpriteRenderer>();
                if (srPrefab == null || srPrefab.sprite == null)
                {
                    Debug.LogWarning("TallerBuilder: el prefab '" + v.prefab.name + "' de la capa '" +
                                     layer.name + "' no tiene sprite, se omite.");
                    continue;
                }

                // Misma cuenta que SceneryManager: la escala sale de la altura de la capa, el
                // ancho de la proporcion del sprite, y el pivot se compensa para que el tile
                // quede con su borde izquierdo en 'cursor' y su base en baselineY + yOffset.
                Bounds b = srPrefab.sprite.bounds;
                float scale = layer.height * v.heightScale / b.size.y;
                float width = b.size.x * scale;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(v.prefab, rowRoot);
                inst.transform.localScale = new Vector3(scale, scale, 1f);
                float x = cursor - b.min.x * scale;
                float y = layer.baselineY + v.yOffset - b.min.y * scale + yShift;
                inst.transform.localPosition = new Vector3(x, y, 0f);

                var sr = inst.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingLayerName = layer.sortingLayer;
                    sr.sortingOrder = layer.sortingOrder;
                    var color = layer.tint;
                    if (!activa)
                        color.a *= ApagadaAlfa;
                    sr.color = color;
                    if (layer.material != null)
                        sr.sharedMaterial = layer.material;
                }

                Bounds worldBounds = sr != null ? sr.bounds : new Bounds(inst.transform.position, Vector3.one);
                acc = acc.HasValue ? Encapsulate(acc.Value, worldBounds) : worldBounds;

                AddWorldLabel(rowRoot, "Cartel " + v.prefab.name,
                    v.prefab.name + "\n(" + v.grupo + ")",
                    new Vector3(cursor + width * 0.5f, worldBounds.max.y + 0.3f, 0f));

                cursor += width + TileGap;
            }

            if (acc.HasValue)
            {
                string titulo = layer.name + (activa ? string.Empty : SufijoApagada);
                AddWorldLabel(rowRoot, "Titulo", titulo, new Vector3(startX, acc.Value.max.y + 1.1f, 0f), 44);
                bounds = acc;
                return cursor + RowGap;
            }

            // Ninguna variante tenia sprite: no queda nada que mostrar en esta fila.
            Object.DestroyImmediate(rowRoot.gameObject);
            return startX;
        }

        /// <summary>Fila con los prefabs de LevelItemPalette (todos menos ZombieFront, que no tiene).</summary>
        static float BuildItemsRow(Transform parent, LevelItemPalette palette, float startX, out Bounds? bounds)
        {
            bounds = null;

            var rowRoot = new GameObject("Fila - Items").transform;
            rowRoot.SetParent(parent, false);

            float cursor = startX;
            Bounds? acc = null;

            foreach (var look in palette.looks)
            {
                if (look == null || look.prefab == null)
                    continue; // ZombieFront: no tiene prefab propio en la paleta.

                var srPrefab = look.prefab.GetComponent<SpriteRenderer>();
                Bounds spriteBounds = srPrefab != null && srPrefab.sprite != null
                    ? srPrefab.sprite.bounds
                    : new Bounds(Vector3.zero, Vector3.one);
                float width = spriteBounds.size.x * Mathf.Abs(look.scale.x);

                // Mismo patron que LevelScene.ApplyLookPrefab: el item lleva la escala de la
                // paleta, el prefab del look va adentro sin escala propia.
                var holder = new GameObject(look.kind.ToString());
                holder.transform.SetParent(rowRoot, false);
                holder.transform.localPosition = new Vector3(cursor + width * 0.5f, 0f, 0f);
                holder.transform.localScale = new Vector3(look.scale.x, look.scale.y, 1f);

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(look.prefab, holder.transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;

                var sr = inst.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingLayerName = "Juego";
                    sr.sortingOrder = look.sortingOrder;
                }

                Bounds worldBounds = sr != null ? sr.bounds : new Bounds(holder.transform.position, Vector3.one);
                acc = acc.HasValue ? Encapsulate(acc.Value, worldBounds) : worldBounds;

                AddWorldLabel(rowRoot, "Cartel " + look.kind,
                    look.prefab.name + "\n(" + look.kind + ")",
                    new Vector3(cursor + width * 0.5f, worldBounds.max.y + 0.3f, 0f));

                cursor += width + TileGap;
            }

            if (acc.HasValue)
            {
                AddWorldLabel(rowRoot, "Titulo", "Items (LevelItemPalette)", new Vector3(startX, acc.Value.max.y + 1.1f, 0f), 44);
                bounds = acc;
                return cursor + RowGap;
            }

            Object.DestroyImmediate(rowRoot.gameObject);
            return startX;
        }

        static Bounds Encapsulate(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        // --- Carteles ----------------------------------------------------------------

        /// <summary>Cartel legible en la Scene view: un Canvas World Space con un Text, sin
        /// EventSystem (no hace falta, no es interactivo).</summary>
        static void AddWorldLabel(Transform parent, string name, string content, Vector3 localPos, int fontSize = 30)
        {
            const float pixelWidth = 520f;
            const float pixelHeight = 170f;
            const float worldWidth = 3.4f;
            float scale = worldWidth / pixelWidth;

            var canvasGO = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            canvasGO.transform.SetParent(parent, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "Frente";
            canvas.sortingOrder = 1000;

            var rect = canvasGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(pixelWidth, pixelHeight);
            rect.pivot = new Vector2(0.5f, 0f);

            canvasGO.transform.localScale = new Vector3(scale, scale, scale);
            canvasGO.transform.localPosition = localPos;

            var textGO = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(canvasGO.transform, false);
            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            var text = textGO.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.LowerCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        // --- Moto de referencia --------------------------------------------------------

        /// <summary>
        /// Copia "Scooter" de NivelBase.unity (no es un prefab: vive armada a mano en esa
        /// escena), le saca los scripts de juego que necesitan un RunController (ScooterView,
        /// WheelDustView, HeadlightView) y apaga el rastro y el polvo, que son efectos de
        /// movimiento sin sentido en una moto quieta. El faro (Light2D) queda encendido con la
        /// intensidad y el color que ya trae NivelBase; como se le saco HeadlightView (que es
        /// quien fija targetSortingLayers en Awake, ver HeadlightView.cs), se lo dejamos en las
        /// mismas dos capas que usa en el juego real (Calle y Juego) por codigo, con la API
        /// publica de Light2D, no con SerializedObject (mismo motivo que HeadlightView.cs).
        /// </summary>
        static Bounds? BuildScooterCopy(Scene targetScene, Vector3 position)
        {
            Scene nivelBase = default;
            GameObject copy = null;
            try
            {
                nivelBase = EditorSceneManager.OpenScene(NivelBasePath, OpenSceneMode.Additive);

                GameObject source = null;
                foreach (var go in nivelBase.GetRootGameObjects())
                    if (go.name == "Scooter")
                    {
                        source = go;
                        break;
                    }

                if (source == null)
                {
                    Debug.LogWarning("TallerBuilder: no se encontro 'Scooter' en " + NivelBasePath +
                                     "; se omite la moto de referencia.");
                    return null;
                }

                SceneManager.SetActiveScene(targetScene);
                copy = Object.Instantiate(source);
                copy.name = "Scooter (referencia)";
            }
            finally
            {
                if (nivelBase.IsValid())
                    EditorSceneManager.CloseScene(nivelBase, true);
            }

            if (copy == null)
                return null;

            copy.transform.position = position;
            copy.transform.rotation = Quaternion.identity;

            foreach (var c in copy.GetComponentsInChildren<ScooterView>(true))
                Object.DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<WheelDustView>(true))
                Object.DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<HeadlightView>(true))
                Object.DestroyImmediate(c);

            var headlight = copy.transform.Find("Headlight");
            if (headlight != null)
            {
                var light2d = headlight.GetComponent<Light2D>();
                if (light2d != null)
                    light2d.targetSortingLayers = new[]
                    {
                        SortingLayer.NameToID("Calle"),
                        SortingLayer.NameToID("Juego"),
                    };
            }

            var dust = copy.transform.Find("Dust");
            if (dust != null)
                dust.gameObject.SetActive(false);
            var tracer = copy.transform.Find("Tracer");
            if (tracer != null)
                tracer.gameObject.SetActive(false);

            var body = copy.transform.Find("Body");
            if (body != null)
            {
                var sr = body.GetComponent<SpriteRenderer>();
                if (sr != null)
                    return sr.bounds;
            }

            return null;
        }

        // --- Camara --------------------------------------------------------------------

        static void BuildCamera(Bounds foco)
        {
            var camGO = new GameObject("Camara");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;

            const float margen = 1.2f;
            const float aspectoSupuesto = 16f / 9f;
            float porAltura = Mathf.Max(foco.extents.y, 1f) * margen;
            float porAncho = Mathf.Max(foco.extents.x, 1f) * margen / aspectoSupuesto;
            cam.orthographicSize = Mathf.Max(porAltura, porAncho);

            camGO.transform.position = new Vector3(foco.center.x, foco.center.y, -10f);
        }

        // --- Build Settings --------------------------------------------------------------

        /// <summary>Suma o actualiza TallerLuz en Build Settings, siempre desactivada, sin tocar
        /// el orden ni el estado de las demas entradas.</summary>
        static void RegisterSceneDisabled(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int idx = scenes.FindIndex(s => s.path == scenePath);
            if (idx >= 0)
                scenes[idx] = new EditorBuildSettingsScene(scenePath, false);
            else
                scenes.Add(new EditorBuildSettingsScene(scenePath, false));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
