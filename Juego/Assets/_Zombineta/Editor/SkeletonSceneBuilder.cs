using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zombineta.Flow;
using Zombineta.Juego.Flow;
using Zombineta.Juego.Screens;

namespace Zombineta.Juego.EditorTools
{
    /// <summary>
    /// Construye las escenas del esqueleto del juego: Boot, pantallas, capas y niveles de
    /// prueba, con sus canvas, botones y listeners, mas Niveles.asset, vinetas placeholder y
    /// Build Settings.
    ///
    /// Por defecto solo crea lo que falta: una pantalla que arte ya retoco no se pisa.
    /// "Reconstruir todas" las regenera desde cero (pide confirmacion).
    /// </summary>
    public static class SkeletonSceneBuilder
    {
        const string ScenesDir = "Assets/_Zombineta/Scenes";
        const string ArtDir = "Assets/_Zombineta/Art/Placeholder";
        const string LevelsPath = "Assets/_Zombineta/Settings/Niveles.asset";
        const string CreditosPath = "Assets/_Zombineta/Settings/Creditos.asset";
        const string ActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        static readonly Color Accent = new Color(0.95f, 0.55f, 0.2f);
        static readonly Color ButtonIdle = new Color(0.16f, 0.13f, 0.22f);

        [MenuItem("Zombineta/Esqueleto/Construir escenas que falten")]
        static void BuildMissing() => Build(false);

        [MenuItem("Zombineta/Esqueleto/Reconstruir todas (pisa cambios)")]
        static void RebuildAll()
        {
            if (EditorUtility.DisplayDialog("Reconstruir el esqueleto",
                    "Esto regenera todas las escenas del esqueleto y pisa cualquier cambio hecho a mano en ellas.",
                    "Reconstruir", "Cancelar"))
                Build(true);
        }

        /// <summary>
        /// Solo el flujo de creditos: crea Creditos.asset y Credits.unity si faltan, y
        /// reconstruye Menu y Final con el boton nuevo (pisa esas dos nada mas).
        /// </summary>
        [MenuItem("Zombineta/Esqueleto/Reconstruir Creditos (Menu, Final, Creditos)")]
        public static void BuildCreditsFlow()
        {
            BuildCreditos();
            int built = 0;
            built += Scene(SceneNames.Credits, false, BuildCredits);
            built += Scene(SceneNames.MainMenu, true, BuildMainMenu);
            built += Scene(SceneNames.Ending, true, BuildEnding);

            RegisterBuildScenes(LevelSceneNames());
            EditorSceneManager.OpenScene(ScenesDir + "/" + SceneNames.Boot + ".unity");
            Debug.Log("Creditos: " + built + " escena(s) construida(s) (Menu y Final reconstruidas), " +
                      EditorBuildSettings.scenes.Length + " en Build Settings.");
        }

        static List<string> LevelSceneNames()
        {
            var names = new List<string>();
            var levels = AssetDatabase.LoadAssetAtPath<LevelSequence>(LevelsPath);
            if (levels != null)
                foreach (var level in levels.levels)
                    names.Add(level.sceneName);
            return names;
        }

        /// <param name="overwrite">True pisa las escenas existentes; false solo crea las que faltan.</param>
        public static void Build(bool overwrite)
        {
            Directory.CreateDirectory(ScenesDir);

            EnsurePauseAction();
            var panels = BuildPlaceholderPanels();
            var levelScenes = new List<string>();
            foreach (var level in BuildLevels(panels).levels)
                levelScenes.Add(level.sceneName);
            BuildCreditos();

            // Ojo: cada escena nueva descarga de memoria los assets que nadie usa. Por eso las
            // referencias a assets (niveles, acciones) se cargan de nuevo justo antes de asignarlas.
            int built = 0;
            built += Scene(SceneNames.Boot, overwrite, BuildBoot);
            built += Scene(SceneNames.MainMenu, overwrite, BuildMainMenu);
            built += Scene(SceneNames.Options, overwrite, BuildOptions);
            built += Scene(SceneNames.CharacterSelect, overwrite, BuildCharacterSelect);
            built += Scene(SceneNames.Cinematic, overwrite, BuildCinematic);
            foreach (var name in levelScenes)
                built += Scene(name, overwrite, () => BuildLevel(name));
            built += Scene(SceneNames.LevelComplete, overwrite, BuildLevelComplete);
            built += Scene(SceneNames.GameOver, overwrite, BuildGameOver);
            built += Scene(SceneNames.Ending, overwrite, BuildEnding);
            built += Scene(SceneNames.Credits, overwrite, BuildCredits);
            built += Scene(SceneNames.Pause, overwrite, BuildPause);

            RegisterBuildScenes(levelScenes);
            EditorSceneManager.OpenScene(ScenesDir + "/" + SceneNames.Boot + ".unity");
            Debug.Log("Esqueleto: " + built + " escena(s) construida(s), " +
                      EditorBuildSettings.scenes.Length + " en Build Settings.");
        }

        static int Scene(string name, bool overwrite, System.Action build)
        {
            if (!overwrite && File.Exists(ScenesDir + "/" + name + ".unity"))
                return 0;
            build();
            return 1;
        }

        // --- Input, arte y datos -------------------------------------------------

        /// <summary>Suma la accion Pausa (Esc y Start) al mapa Player si no existe.</summary>
        static void EnsurePauseAction()
        {
            var asset = InputActionAsset.FromJson(File.ReadAllText(ActionsPath));
            var map = asset.FindActionMap("Player", true);
            if (map.FindAction("Pause") == null)
            {
                var action = map.AddAction("Pause", InputActionType.Button);
                action.AddBinding("<Keyboard>/escape").WithGroup("Keyboard&Mouse");
                action.AddBinding("<Gamepad>/start").WithGroup("Gamepad");
                File.WriteAllText(ActionsPath, asset.ToJson());
                AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceUpdate);
            }
            Object.DestroyImmediate(asset);
        }

        static InputActionReference FindAction(string path)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ActionsPath))
                if (o is InputActionReference r && r.action != null &&
                    r.action.actionMap.name + "/" + r.action.name == path)
                    return r;
            Debug.LogWarning("No se encontro la accion " + path + " en " + ActionsPath);
            return null;
        }

        /// <summary>Vinetas de color con marco y su numero en bloques. Se reemplazan por el comic.</summary>
        static List<Sprite> BuildPlaceholderPanels()
        {
            Directory.CreateDirectory(ArtDir);
            var colors = new[]
            {
                new Color(0.55f, 0.2f, 0.25f), new Color(0.2f, 0.35f, 0.55f), new Color(0.25f, 0.5f, 0.3f),
                new Color(0.55f, 0.45f, 0.2f), new Color(0.4f, 0.25f, 0.55f), new Color(0.2f, 0.5f, 0.5f),
            };

            var sprites = new List<Sprite>();
            for (int i = 0; i < colors.Length; i++)
            {
                string path = ArtDir + "/vineta_0" + (i + 1) + ".png";
                if (!File.Exists(path))
                {
                    const int w = 960, h = 540;
                    var pixels = new Color32[w * h];
                    var ink = new Color32(13, 13, 18, 255);
                    for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        bool border = x < 14 || x >= w - 14 || y < 14 || y >= h - 14;
                        bool block = y > h - 110 && y < h - 50 && x > 50 && x < 50 + (i + 1) * 80 && (x - 50) % 80 < 56;
                        float shade = 0.8f + 0.2f * (x + y) / (float)(w + h);
                        var c = colors[i] * shade;
                        c.a = 1f;
                        pixels[y * w + x] = border || block ? ink : (Color32)c;
                    }

                    var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);

                    AssetDatabase.ImportAsset(path);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(path));
            }
            return sprites;
        }

        /// <summary>Crea Niveles.asset con dos niveles de prueba si no tiene ninguno. No pisa lo editado.</summary>
        static LevelSequence BuildLevels(List<Sprite> panels)
        {
            var levels = AssetDatabase.LoadAssetAtPath<LevelSequence>(LevelsPath);
            if (levels == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LevelsPath));
                levels = ScriptableObject.CreateInstance<LevelSequence>();
                AssetDatabase.CreateAsset(levels, LevelsPath);
            }

            if (levels.levels.Count == 0)
            {
                levels.levels.Add(NewLevel("Nivel 1: La salida", "Level_01", panels, 0));
                levels.levels.Add(NewLevel("Nivel 2: El puente", "Level_02", panels, 3));
                EditorUtility.SetDirty(levels);
                AssetDatabase.SaveAssets();
            }
            return levels;
        }

        /// <summary>Crea Creditos.asset con secciones placeholder si no tiene ninguna. No pisa lo editado.</summary>
        static Creditos BuildCreditos()
        {
            var creditos = AssetDatabase.LoadAssetAtPath<Creditos>(CreditosPath);
            if (creditos == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CreditosPath));
                creditos = ScriptableObject.CreateInstance<Creditos>();
                AssetDatabase.CreateAsset(creditos, CreditosPath);
            }

            if (creditos.secciones.Count == 0)
            {
                creditos.secciones.Add(new Seccion { titulo = "Zombineta" });
                creditos.secciones.Add(new Seccion
                {
                    titulo = "Equipo",
                    lineas = new[] { "Germán", "Jesús", "José", "Juana", "Seba" },
                });
                creditos.secciones.Add(new Seccion { titulo = "Taller de Proyecto Integral 212" });
                creditos.secciones.Add(new Seccion { titulo = "Gracias por jugar" });
                EditorUtility.SetDirty(creditos);
                AssetDatabase.SaveAssets();
            }
            return creditos;
        }

        static LevelInfo NewLevel(string title, string scene, List<Sprite> panels, int first)
        {
            var level = new LevelInfo { displayName = title, sceneName = scene };
            for (int i = first; i < first + 3 && i < panels.Count; i++)
                level.comicPanels.Add(new ComicPanel { image = panels[i], seconds = 2.5f });
            return level;
        }

        static void RegisterBuildScenes(List<string> levelScenes)
        {
            var names = new List<string>
            {
                SceneNames.Boot, SceneNames.MainMenu, SceneNames.Options, SceneNames.CharacterSelect, SceneNames.Cinematic,
            };
            names.AddRange(levelScenes);
            names.AddRange(new[]
            {
                SceneNames.LevelComplete, SceneNames.GameOver, SceneNames.Ending, SceneNames.Credits, SceneNames.Pause,
            });

            var managedPaths = new HashSet<string>();
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var n in names)
            {
                string path = ScenesDir + "/" + n + ".unity";
                managedPaths.Add(path);
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            // Conserva entradas que este metodo no administra (p.ej. escenas de arte como
            // TallerLuz.unity, con su enabled tal cual estaba), al final de la lista, en vez de
            // pisar toda la lista de Build Settings.
            foreach (var existing in EditorBuildSettings.scenes)
                if (!managedPaths.Contains(existing.path))
                    scenes.Add(existing);

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // --- Escenas ---------------------------------------------------------------

        static void BuildBoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("GameRoot").AddComponent<GameRoot>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvas = AddCanvas("Fundido", 1000);
            var black = AddImage(canvas.transform, "Negro", Color.black);
            var fader = black.gameObject.AddComponent<ScreenFader>();

            Set(root, "levels", AssetDatabase.LoadAssetAtPath<LevelSequence>(LevelsPath));
            Set(root, "fader", fader);
            Save(scene, SceneNames.Boot);
        }

        static void BuildMainMenu()
        {
            var (scene, canvas) = NewScreen(true, 0, new Color(0.12f, 0.08f, 0.16f), "ZOMBINETA");
            var screen = canvas.gameObject.AddComponent<MainMenuScreen>();

            var buttons = AddMenu(canvas.transform, 60f,
                ("Jugar", screen.Play),
                ("Opciones", screen.OpenOptions),
                ("Créditos", screen.OpenCredits),
                ("Salir", screen.Quit));

            AddText(canvas.transform, "Pie", "Esqueleto del juego definitivo - pantallas placeholder", 26,
                new Vector2(0f, -470f), new Vector2(1600f, 60f));
            Set(screen, "firstSelected", buttons[0]);
            Save(scene, SceneNames.MainMenu);
        }

        static void BuildOptions()
        {
            var (scene, canvas) = NewScreen(false, 200, new Color(0.05f, 0.04f, 0.08f, 1f), "OPCIONES");
            var screen = canvas.gameObject.AddComponent<OptionsScreen>();

            var volume = AddSlider(canvas.transform, "Volumen", new Vector2(120f, 190f));
            var fullscreen = AddToggle(canvas.transform, "Pantalla completa", new Vector2(0f, 100f));
            var effects = AddToggle(canvas.transform, "Efectos de camara", new Vector2(0f, 20f));
            var shake = AddToggle(canvas.transform, "Sacudidas", new Vector2(0f, -60f));
            var gore = AddToggle(canvas.transform, "Sangre alta", new Vector2(0f, -140f));
            var vibration = AddToggle(canvas.transform, "Vibracion", new Vector2(0f, -220f));
            var lightQuality = AddDropdown(canvas.transform, "Iluminacion", new Vector2(120f, -300f), new[] { "Alta", "Baja" });
            var back = AddButton(canvas.transform, "Volver", new Vector2(0f, -380f), screen.Back);

            Chain(new Selectable[] { volume, fullscreen, effects, shake, gore, vibration, lightQuality, back });

            Set(screen, "volume", volume);
            Set(screen, "fullscreen", fullscreen);
            Set(screen, "cameraEffects", effects);
            Set(screen, "cameraShake", shake);
            Set(screen, "gore", gore);
            Set(screen, "vibration", vibration);
            Set(screen, "lightQuality", lightQuality);
            Set(screen, "cancelAction", FindAction("UI/Cancel"));
            Set(screen, "firstSelected", volume);
            Save(scene, SceneNames.Options);
        }

        static void BuildCharacterSelect()
        {
            var (scene, canvas) = NewScreen(true, 0, new Color(0.1f, 0.12f, 0.18f), "ELEGI TU REPARTIDORA");
            var screen = canvas.gameObject.AddComponent<CharacterSelectScreen>();

            var a = AddButton(canvas.transform, "Repartidora A", new Vector2(0f, 110f), null);
            UnityEventTools.AddIntPersistentListener(a.onClick, screen.Choose, 0);
            var b = AddButton(canvas.transform, "Repartidora B", new Vector2(0f, 6f), null);
            UnityEventTools.AddIntPersistentListener(b.onClick, screen.Choose, 1);
            var back = AddButton(canvas.transform, "Volver", new Vector2(0f, -150f), screen.Back);

            Chain(new Selectable[] { a, b, back });
            Set(screen, "firstSelected", a);
            Save(scene, SceneNames.CharacterSelect);
        }

        static void BuildCinematic()
        {
            var (scene, canvas) = NewScreen(true, 0, Color.black, "");
            Object.DestroyImmediate(canvas.transform.Find("Titulo").gameObject);
            var player = canvas.gameObject.AddComponent<CinematicPlayer>();

            var panel = AddImage(canvas.transform, "Vineta", Color.white, false);
            panel.rectTransform.sizeDelta = new Vector2(1440f, 810f);
            panel.preserveAspect = true;
            var panelGroup = panel.gameObject.AddComponent<CanvasGroup>();

            var title = AddText(canvas.transform, "Titulo del nivel", "Nivel", 84, Vector2.zero, new Vector2(1700f, 200f));
            var titleGroup = title.gameObject.AddComponent<CanvasGroup>();

            AddText(canvas.transform, "Ayuda", "Enter: siguiente     Esc: saltear", 26,
                new Vector2(0f, -500f), new Vector2(1200f, 50f));

            Set(player, "title", title);
            Set(player, "titleGroup", titleGroup);
            Set(player, "panel", panel);
            Set(player, "panelGroup", panelGroup);
            Set(player, "submitAction", FindAction("UI/Submit"));
            Set(player, "cancelAction", FindAction("UI/Cancel"));
            Save(scene, SceneNames.Cinematic);
        }

        static void BuildLevel(string sceneName)
        {
            var (scene, canvas) = NewScreen(true, 0, new Color(0.18f, 0.18f, 0.22f), "");
            Object.DestroyImmediate(canvas.transform.Find("Titulo").gameObject);

            // Una franja de "calle" para que se lea que es un nivel y no un menu.
            var street = AddImage(canvas.transform, "Calle", new Color(0.26f, 0.26f, 0.3f), false);
            street.rectTransform.anchorMin = new Vector2(0f, 0f);
            street.rectTransform.anchorMax = new Vector2(1f, 0.42f);
            street.rectTransform.offsetMin = street.rectTransform.offsetMax = Vector2.zero;

            var stub = canvas.gameObject.AddComponent<LevelStub>();
            var label = AddText(canvas.transform, "Etiqueta", sceneName, 56, new Vector2(0f, 180f), new Vector2(1700f, 300f));

            Set(stub, "label", label);
            Set(stub, "pauseAction", FindAction("Player/Pause"));
            Save(scene, sceneName);
        }

        static void BuildLevelComplete()
        {
            var (scene, canvas) = NewScreen(true, 0, new Color(0.08f, 0.16f, 0.1f), "NIVEL COMPLETO");
            var screen = canvas.gameObject.AddComponent<LevelCompleteScreen>();
            var detail = AddText(canvas.transform, "Detalle", "", 40, new Vector2(0f, 180f), new Vector2(1400f, 80f));
            var cont = AddButton(canvas.transform, "Continuar", new Vector2(0f, -60f), screen.Continue);

            Set(screen, "detail", detail);
            Set(screen, "firstSelected", cont);
            Save(scene, SceneNames.LevelComplete);
        }

        static void BuildGameOver()
        {
            var (scene, canvas) = NewScreen(true, 0, new Color(0.2f, 0.05f, 0.06f), "TE ALCANZARON");
            var screen = canvas.gameObject.AddComponent<GameOverScreen>();
            var buttons = AddMenu(canvas.transform, 40f, ("Reintentar", screen.Retry), ("Menu principal", screen.ToMenu));
            Set(screen, "firstSelected", buttons[0]);
            Save(scene, SceneNames.GameOver);
        }

        static void BuildEnding()
        {
            var (scene, canvas) = NewScreen(true, 0, new Color(0.08f, 0.1f, 0.16f), "FIN");
            var screen = canvas.gameObject.AddComponent<EndingScreen>();
            AddText(canvas.transform, "Texto", "Llegaste al refugio. El pedido llego caliente.", 40,
                new Vector2(0f, 160f), new Vector2(1500f, 80f));
            var continuar = AddButton(canvas.transform, "Continuar", new Vector2(0f, -80f), screen.ShowCredits);
            Set(screen, "firstSelected", continuar);
            Save(scene, SceneNames.Ending);
        }

        // El boton Volver queda fijo en (0,-500) con alto 84: su borde de arriba esta a
        // alto/2 - 500 del centro del canvas. El viewport recorta a este margen desde abajo
        // (mismo pixel de referencia que anchoredPosition, sin depender de la resolucion real),
        // que le deja mas de 40px de aire libre sobre el boton.
        const float VolverY = -500f;
        const float ViewportMargenInferior = 160f;

        static void BuildCredits()
        {
            var (scene, canvas) = NewScreen(true, 0, new Color(0.05f, 0.05f, 0.08f), "");
            Object.DestroyImmediate(canvas.transform.Find("Titulo").gameObject);
            var screen = canvas.gameObject.AddComponent<CreditsScreen>();

            // Viewport: recorta con RectMask2D desde arriba del canvas hasta el margen de abajo,
            // que deja el boton Volver siempre libre y fuera del area con scroll.
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(canvas.transform, false);
            var viewport = (RectTransform)viewportGo.transform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(0f, ViewportMargenInferior);
            viewport.offsetMax = Vector2.zero;

            var contenedorGo = new GameObject("Contenedor", typeof(RectTransform));
            contenedorGo.transform.SetParent(viewport, false);
            var contenedor = (RectTransform)contenedorGo.transform;
            contenedor.anchorMin = contenedor.anchorMax = new Vector2(0.5f, 0f);
            contenedor.pivot = new Vector2(0.5f, 1f);
            contenedor.sizeDelta = new Vector2(1600f, 0f);
            contenedor.anchoredPosition = Vector2.zero;

            var volver = AddButton(canvas.transform, "Volver", new Vector2(0f, VolverY), screen.Volver);

            // Se recarga justo antes de asignarlo: la escena nueva descarga los assets sin uso.
            var creditos = AssetDatabase.LoadAssetAtPath<Creditos>(CreditosPath);
            Set(screen, "datos", creditos);
            Set(screen, "viewport", viewport);
            Set(screen, "contenedor", contenedor);
            Set(screen, "submitAction", FindAction("UI/Submit"));
            Set(screen, "cancelAction", FindAction("UI/Cancel"));
            Set(screen, "firstSelected", volver);
            Save(scene, SceneNames.Credits);
        }

        static void BuildPause()
        {
            var (scene, canvas) = NewScreen(false, 100, new Color(0f, 0f, 0f, 0.7f), "PAUSA");
            var screen = canvas.gameObject.AddComponent<PauseScreen>();
            var buttons = AddMenu(canvas.transform, 120f,
                ("Seguir", screen.Resume),
                ("Reintentar", screen.Retry),
                ("Opciones", screen.OpenOptions),
                ("Salir al menu", screen.ToMenu));
            Set(screen, "firstSelected", buttons[0]);
            Set(screen, "pauseAction", FindAction("Player/Pause"));

            // Ayuda de controles debajo de los botones: cambia sola segun el ultimo dispositivo usado.
            var hintsText = AddText(canvas.transform, "AyudaControles",
                ControlHints.TextFor(Zombineta.Player.ControlScheme.KeyboardMouse), 24,
                new Vector2(0f, -310f), new Vector2(1700f, 100f));
            hintsText.color = new Color(1f, 1f, 1f, 0.85f);
            var hints = hintsText.gameObject.AddComponent<ControlHints>();
            Set(hints, "text", hintsText);

            Save(scene, SceneNames.Pause);
        }

        // --- Piezas de UI -----------------------------------------------------------

        static (Scene scene, Canvas canvas) NewScreen(bool isBase, int sortingOrder, Color background, string title)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Las bases traen su camara (y el unico AudioListener); las capas se dibujan encima.
            if (isBase)
            {
                var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam.tag = "MainCamera";
                var c = cam.GetComponent<Camera>();
                c.orthographic = true;
                c.orthographicSize = 6f;
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = Color.black;
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }

            var canvas = AddCanvas("Canvas", sortingOrder);
            AddImage(canvas.transform, "Fondo", background);
            AddText(canvas.transform, "Titulo", title, 76, new Vector2(0f, 340f), new Vector2(1700f, 140f));
            return (scene, canvas);
        }

        static void Save(Scene scene, string name) =>
            EditorSceneManager.SaveScene(scene, ScenesDir + "/" + name + ".unity");

        static Canvas AddCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static Image AddImage(Transform parent, string name, Color color, bool stretch = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            if (stretch)
            {
                var rt = image.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            return image;
        }

        static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Text AddText(Transform parent, string name, string content, int size, Vector2 position, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiFont;
            text.text = content;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = box;
            return text;
        }

        static Button AddButton(Transform parent, string label, Vector2 position, UnityAction onClick)
        {
            var go = new GameObject("Boton " + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(560f, 84f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            Paint(button);
            AddText(go.transform, "Texto", label, 38, Vector2.zero, rt.sizeDelta);

            if (onClick != null)
                UnityEventTools.AddPersistentListener(button.onClick, onClick);
            return button;
        }

        static List<Button> AddMenu(Transform parent, float startY, params (string label, UnityAction action)[] items)
        {
            var buttons = new List<Button>();
            for (int i = 0; i < items.Length; i++)
                buttons.Add(AddButton(parent, items[i].label, new Vector2(0f, startY - i * 104f), items[i].action));
            Chain(buttons.ToArray());
            return buttons;
        }

        static Toggle AddToggle(Transform parent, string label, Vector2 position)
        {
            var go = DefaultControls.CreateToggle(new DefaultControls.Resources());
            go.name = "Opcion " + label;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = position;
            rt.localScale = Vector3.one * 2.4f;

            var text = go.GetComponentInChildren<Text>();
            text.text = label;
            text.font = UiFont;
            text.fontSize = 14;
            text.color = Color.white;
            // El rect del texto es bajito: sin esto el Text oculta la linea que no entra.
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var toggle = go.GetComponent<Toggle>();
            Paint(toggle);
            if (toggle.graphic != null)
                toggle.graphic.color = Accent;
            return toggle;
        }

        static Slider AddSlider(Transform parent, string label, Vector2 position)
        {
            AddText(parent, "Etiqueta " + label, label, 38, position + new Vector2(-420f, 0f), new Vector2(300f, 60f));

            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = "Opcion " + label;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = position;
            rt.localScale = Vector3.one * 2.6f;

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            Paint(slider);
            return slider;
        }

        /// <summary>Desplegable con etiqueta a la izquierda, mismo estilo que AddSlider.</summary>
        static Dropdown AddDropdown(Transform parent, string label, Vector2 position, string[] options)
        {
            AddText(parent, "Etiqueta " + label, label, 38, position + new Vector2(-420f, 0f), new Vector2(300f, 60f));

            var go = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            go.name = "Opcion " + label;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = position;
            rt.localScale = Vector3.one * 2.4f;

            var dropdown = go.GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));

            // Solo la etiqueta visible (Template arranca inactivo: GetComponentInChildren sin
            // includeInactive no lo trae, asi que la lista desplegada conserva la tipografia por
            // defecto de Unity, mas legible sobre fondo claro que la fuente del resto del juego).
            var text = go.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.font = UiFont;
                text.color = Color.white;
            }

            Paint(dropdown);
            return dropdown;
        }

        /// <summary>Colores legibles con teclado: la opcion seleccionada se ve naranja.</summary>
        static void Paint(Selectable selectable)
        {
            if (selectable.targetGraphic != null)
                selectable.targetGraphic.color = Color.white;
            var colors = selectable.colors;
            colors.normalColor = ButtonIdle;
            colors.highlightedColor = Accent;
            colors.selectedColor = Accent;
            colors.pressedColor = Accent * 0.8f;
            colors.colorMultiplier = 1f;
            selectable.colors = colors;
        }

        /// <summary>Navegacion explicita arriba/abajo, dando la vuelta.</summary>
        static void Chain(Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = items[(i - 1 + items.Length) % items.Length],
                    selectOnDown = items[(i + 1) % items.Length],
                };
                items[i].navigation = nav;
            }
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError("No existe el campo '" + field + "' en " + target.GetType().Name, target);
                return;
            }
            if (value == null)
                Debug.LogWarning("Referencia vacia para '" + field + "' en " + target.GetType().Name, target);
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
