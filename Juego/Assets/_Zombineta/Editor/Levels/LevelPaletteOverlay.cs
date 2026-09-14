using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using Zombineta.Juego.Levels;
using Zombineta.Level;

namespace Zombineta.Juego.EditorTools.Levels
{
    /// <summary>
    /// La paleta del nivel en la Scene view: aparece sola cuando la escena tiene un LevelScene.
    /// Un boton por tipo arma el colocado (click en la escena pone el item en el carril y los
    /// metros bajo el mouse), y ademas: generar, fijar, ir a un punto y probar desde aca.
    /// </summary>
    [Overlay(typeof(SceneView), OverlayId, "Nivel Zombineta", defaultDisplay = true)]
    public sealed class LevelPaletteOverlay : IMGUIOverlay, ITransientOverlay
    {
        const string OverlayId = "zombineta-level-palette";

        static readonly LevelEntryKind[] Kinds =
        {
            LevelEntryKind.Obstacle, LevelEntryKind.ZombieFront, LevelEntryKind.Barrel, LevelEntryKind.Ramp,
            LevelEntryKind.Fuel, LevelEntryKind.Battery, LevelEntryKind.Ammo,
        };

        static readonly Dictionary<LevelEntryKind, string> Labels = new Dictionary<LevelEntryKind, string>
        {
            { LevelEntryKind.Obstacle, "Obstaculo" },
            { LevelEntryKind.ZombieFront, "Zombie" },
            { LevelEntryKind.Barrel, "Barril" },
            { LevelEntryKind.Ramp, "Rampa" },
            { LevelEntryKind.Fuel, "Nafta" },
            { LevelEntryKind.Battery, "Bateria" },
            { LevelEntryKind.Ammo, "Balas" },
        };

        // Lo que se esta por colocar. Estatico: lo comparten todas las Scene views.
        static LevelEntryKind? armed;
        static float placeHeight;
        static int zombieVariant;

        static LevelScene cachedScene;
        static double cachedAt = -10;

        static LevelScene Scene
        {
            get
            {
                if (cachedScene == null && EditorApplication.timeSinceStartup - cachedAt > 0.5)
                {
                    cachedScene = LevelEditorActions.FindScene();
                    cachedAt = EditorApplication.timeSinceStartup;
                }
                return cachedScene;
            }
        }

        public bool visible => !EditorApplication.isPlaying && Scene != null;

        public override void OnCreated()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public override void OnWillBeDestroyed()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // --- Panel -------------------------------------------------------------------

        public override void OnGUI()
        {
            var scene = Scene;
            if (scene == null)
                return;

            var layout = scene.Layout;
            EditorGUILayout.LabelField(scene.gameObject.scene.name + " · " + scene.GoalDistance.ToString("0") + " m", EditorStyles.boldLabel);

            // Colocar.
            EditorGUILayout.LabelField("Colocar", EditorStyles.miniBoldLabel);
            for (int i = 0; i < Kinds.Length; i += 4)
            {
                EditorGUILayout.BeginHorizontal();
                for (int k = i; k < Mathf.Min(i + 4, Kinds.Length); k++)
                {
                    var kind = Kinds[k];
                    bool on = armed == kind;
                    bool now = GUILayout.Toggle(on, Labels[kind], EditorStyles.miniButton, GUILayout.MinWidth(62));
                    if (now != on)
                        armed = now ? kind : (LevelEntryKind?)null;
                }
                EditorGUILayout.EndHorizontal();
            }

            placeHeight = Mathf.Max(0f, EditorGUILayout.FloatField("Altura (m)", placeHeight));
            var roster = scene.Config != null ? scene.Config.zombies : null;
            if (roster != null && roster.types.Count > 0)
            {
                var names = new string[roster.types.Count];
                for (int i = 0; i < names.Length; i++)
                    names[i] = roster.types[i].name;
                zombieVariant = EditorGUILayout.Popup("Tipo de zombie", Mathf.Clamp(zombieVariant, 0, names.Length - 1), names);
            }
            if (armed != null)
                EditorGUILayout.HelpBox("Click en la escena para colocar. Shift: seguir colocando. Esc: cancelar.", MessageType.None);

            // Recorrido.
            var items = scene.Items;
            int untouched = 0, pinned = 0, byHand = 0;
            foreach (var item in items)
            {
                if (!item.generated) byHand++;
                else if (item.IsReplaceable(layout)) untouched++;
                else pinned++;
            }
            var issues = LevelValidator.Check(scene.CurrentEntries());

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Recorrido", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(items.Length + " items: " + untouched + " generados, " + pinned + " fijados, " + byHand + " a mano", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(issues.Count == 0))
            {
                if (GUILayout.Button(issues.Count == 0 ? "Sin problemas" : issues.Count + " problemas: ir al siguiente", EditorStyles.miniButton))
                    GoToNextIssue(scene, issues);
            }

            // Generador.
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Generador", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            int seed = EditorGUILayout.IntField("Semilla", scene.Generator.seed);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(scene, "Semilla");
                scene.Generator.seed = seed;
                EditorUtility.SetDirty(scene);
            }
            if (GUILayout.Button("Generar / Regenerar"))
            {
                var r = LevelEditorActions.Regenerate(scene);
                Debug.Log($"Nivel regenerado: {r.removed} quitados, {r.kept} conservados, {r.added} nuevos.", scene);
            }

            // Seleccion.
            var selected = LevelEditorActions.SelectedItems();
            using (new EditorGUI.DisabledScope(selected.Count == 0))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Fijar (" + selected.Count + ")", EditorStyles.miniButtonLeft))
                    LevelEditorActions.SetPinned(selected, true);
                if (GUILayout.Button("Desfijar", EditorStyles.miniButtonRight))
                    LevelEditorActions.SetPinned(selected, false);
                EditorGUILayout.EndHorizontal();
            }

            // Vista.
            EditorGUILayout.Space(4);
            float viewMeters = LevelEditorActions.ViewMeters(scene);
            EditorGUI.BeginChangeCheck();
            float go = EditorGUILayout.Slider("Ir a (m)", viewMeters, 0f, scene.GoalDistance);
            if (EditorGUI.EndChangeCheck())
                LevelEditorActions.FrameMeters(scene, go);

            if (GUILayout.Button("▶ Probar desde aca (" + viewMeters.ToString("0") + " m)"))
                LevelEditorActions.PlayFrom(scene, viewMeters);
        }

        static void GoToNextIssue(LevelScene scene, List<LevelIssue> issues)
        {
            float here = LevelEditorActions.ViewMeters(scene);
            var next = issues[0];
            foreach (var issue in issues)
                if (issue.distance > here + 1f)
                {
                    next = issue;
                    break;
                }
            LevelEditorActions.FrameMeters(scene, next.distance);
            Debug.Log(next.Message + " a " + next.distance.ToString("0") + " m" + (next.lane >= 0 ? ", carril " + next.lane : ""), scene);
        }

        // --- Colocado con el mouse ---------------------------------------------------------

        static void OnSceneGUI(SceneView view)
        {
            if (armed == null || EditorApplication.isPlaying)
                return;
            var scene = Scene;
            if (scene == null)
            {
                armed = null;
                return;
            }

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                armed = null;
                e.Use();
                return;
            }

            int control = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(control);

            var layout = scene.Layout;
            Vector2 world = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            var kind = armed.Value;
            float height = kind == LevelEntryKind.Ramp || kind == LevelEntryKind.Barrel || kind == LevelEntryKind.ZombieFront ? 0f : placeHeight;
            float meters = Mathf.Max(0f, LevelLayout.SnapMeters(layout.ToMeters(world.x), LevelEditorActions.GridMeters));
            int lane = layout.NearestLane(layout.GroundY(world.y, height));
            int variant = kind == LevelEntryKind.ZombieFront ? zombieVariant : 0;

            if (e.type == EventType.Repaint)
            {
                var at = layout.ItemPosition(meters, lane, height);
                Handles.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                Handles.DrawWireCube(at, new Vector3(0.9f, 0.9f, 0f));
                Handles.Label(at + new Vector2(0.5f, 0.8f), Labels[kind] + " · " + meters.ToString("0") + " m · C" + lane, EditorStyles.whiteBoldLabel);
            }
            else if (e.type == EventType.MouseMove)
            {
                view.Repaint();
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                LevelEditorActions.Place(scene, kind, meters, lane, height, variant);
                if (!e.shift)
                    armed = null;
                e.Use();
            }
        }
    }
}
