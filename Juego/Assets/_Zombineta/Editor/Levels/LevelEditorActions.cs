using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zombineta.Juego.Levels;
using Zombineta.Level;

namespace Zombineta.Juego.EditorTools.Levels
{
    /// <summary>
    /// Lo que se puede hacer sobre un nivel desde el editor: colocar items, generar respetando lo
    /// tocado, fijar y desfijar, y probar desde un punto. Todo con Undo. La paleta de la Scene
    /// view es solo botones que llaman aca.
    /// </summary>
    public static class LevelEditorActions
    {
        public const float GridMeters = LevelScene.GridMeters;

        public static LevelScene FindScene() => Object.FindFirstObjectByType<LevelScene>();

        // --- Colocar -----------------------------------------------------------------

        public static LevelItem Place(LevelScene scene, LevelEntryKind kind, float meters, int lane, float height = 0f, int variant = 0)
        {
            var item = CreateItem(scene, kind, LevelLayout.SnapMeters(meters, GridMeters), lane, height, variant);
            Undo.RegisterCreatedObjectUndo(item.gameObject, "Colocar " + kind);
            Selection.activeGameObject = item.gameObject;
            MarkDirty(scene);
            return item;
        }

        static LevelItem CreateItem(LevelScene scene, LevelEntryKind kind, float meters, int lane, float height, int variant)
        {
            var layout = scene.Layout;
            var go = new GameObject(ItemName(kind, meters, lane, height));
            go.transform.SetParent(scene.ItemsRoot, false);
            go.transform.position = layout.ItemPosition(meters, Mathf.Clamp(lane, 0, 2), height);

            var item = go.AddComponent<LevelItem>();
            item.kind = kind;
            item.lane = Mathf.Clamp(lane, 0, 2);
            item.height = Mathf.Max(0f, height);
            item.variant = variant;
            scene.ApplyVisual(item);
            return item;
        }

        public static string ItemName(LevelEntryKind kind, float meters, int lane, float height) =>
            kind + " " + meters.ToString("0") + "m C" + lane + (height > 0f ? " aereo" : "");

        // --- Generar -----------------------------------------------------------------

        public struct GenerateReport
        {
            public int removed;
            public int kept;
            public int added;
        }

        /// <summary>
        /// Saca los items generados sin tocar y vuelve a generar alrededor de todo lo demas
        /// (lo colocado a mano, lo fijado y lo movido).
        /// </summary>
        public static GenerateReport Regenerate(LevelScene scene)
        {
            var report = new GenerateReport();
            var layout = scene.Layout;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Regenerar nivel");

            var kept = new List<LevelEntry>();
            foreach (var item in scene.Items)
            {
                if (item.IsReplaceable(layout))
                {
                    Undo.DestroyObjectImmediate(item.gameObject);
                    report.removed++;
                }
                else
                {
                    kept.Add(item.ToEntry(layout));
                    report.kept++;
                }
            }

            foreach (var entry in LevelGenerator.Generate(scene.Generator, scene.GoalDistance, kept))
            {
                var item = CreateItem(scene, entry.kind, entry.distance, entry.lane, entry.height, entry.variant);
                item.MarkGenerated(layout);
                Undo.RegisterCreatedObjectUndo(item.gameObject, "Regenerar nivel");
                report.added++;
            }

            SortChildren(scene);
            Undo.CollapseUndoOperations(group);
            MarkDirty(scene);
            return report;
        }

        /// <summary>Los hijos del recorrido en orden de distancia: la jerarquia se lee como el nivel.</summary>
        static void SortChildren(LevelScene scene)
        {
            var root = scene.ItemsRoot;
            var children = new List<Transform>();
            foreach (Transform child in root)
                children.Add(child);
            children.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            Undo.RegisterChildrenOrderUndo(root, "Ordenar recorrido");
            for (int i = 0; i < children.Count; i++)
                children[i].SetSiblingIndex(i);
        }

        // --- Fijar -------------------------------------------------------------------

        public static int SetPinned(IEnumerable<LevelItem> items, bool pinned)
        {
            int changed = 0;
            foreach (var item in items)
            {
                if (item == null || item.pinned == pinned)
                    continue;
                Undo.RecordObject(item, pinned ? "Fijar" : "Desfijar");
                item.pinned = pinned;
                // Desfijar un item generado lo devuelve a "sin tocar" donde esta ahora.
                if (!pinned && item.generated)
                    item.MarkGenerated(LevelLayoutOf(item));
                EditorUtility.SetDirty(item);
                changed++;
            }
            return changed;
        }

        public static List<LevelItem> SelectedItems()
        {
            var items = new List<LevelItem>();
            foreach (var go in Selection.gameObjects)
            {
                var item = go.GetComponent<LevelItem>();
                if (item != null)
                    items.Add(item);
            }
            return items;
        }

        static LevelLayout LevelLayoutOf(LevelItem item)
        {
            var scene = item.GetComponentInParent<LevelScene>();
            if (scene == null)
                scene = FindScene();
            return scene != null ? scene.Layout : new LevelLayout(0.75f, 1.6f, 1.125f);
        }

        // --- Navegar y probar ----------------------------------------------------------

        /// <summary>Metros en el centro de la Scene view.</summary>
        public static float ViewMeters(LevelScene scene)
        {
            var view = SceneView.lastActiveSceneView;
            return view != null ? Mathf.Max(0f, scene.Layout.ToMeters(view.pivot.x)) : 0f;
        }

        public static void FrameMeters(LevelScene scene, float meters)
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null)
                return;
            var p = view.pivot;
            view.pivot = new Vector3(scene.Layout.ToWorldX(meters), p.y, p.z);
            view.Repaint();
        }

        /// <summary>Entra en Play con la moto a esos metros.</summary>
        public static void PlayFrom(LevelScene scene, float meters)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            SessionState.SetFloat(LevelScene.PlayFromKey, Mathf.Clamp(meters, 0f, scene.GoalDistance));
            EditorApplication.EnterPlaymode();
        }

        static void MarkDirty(LevelScene scene)
        {
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene.gameObject.scene);
        }
    }
}
