using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Juego.Flow;
using Zombineta.Juego.Screens;
using O = Zombineta.Juego.EditorTools.OptionsMenuBuilder;

namespace Zombineta.Juego.EditorTools
{
    /// <summary>
    /// Arma el menu principal con el diseno de la referencia (menu_referencia_v2.jpg): el fondo
    /// con el logo, un panel oscuro con borde rosa a la derecha, cinco botones con icono
    /// (Iniciar, Tutorial, Opciones, Creditos, Salir) donde el seleccionado se ve como una tira
    /// clara, la ayuda de teclas y la version. Reusa la escena MainMenu: conserva el Canvas con
    /// su MainMenuScreen y UiSonidos, y rehace todo lo que cuelga de el. Se puede volver a correr.
    /// Saca la niebla y el faro de MenuConVidaBuilder, que estaban ubicados sobre el boceto
    /// anterior y no calzan con el arte nuevo; el humo del escape se conserva, en el cano de la
    /// moto nueva (HumoX/HumoY).
    /// </summary>
    public static class MenuPrincipalBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.MainMenu + ".unity";
        internal const string ArteDir = "Assets/_Zombineta/Art/MenuInicio/";
        internal const string UiDir = ArteDir + "ui/";

        // Medidas en el Canvas de 1920x1080, x/y desde arriba a la izquierda (como la referencia).
        const float PanelX = 1020f, PanelY = 384f, PanelW = 816f, PanelH = 654f;
        const float FilaX = 1066f, FilaY = 438f, FilaW = 648f, FilaH = 80f, FilaPaso = 90f;

        // Humo: sale de la punta del caño de escape de la moto del fondo. Orden de dibujo:
        // fondo (Canvas raiz) < humo < interfaz.
        const string HumoPrefab = "Assets/_Zombineta/Prefabs/FxHumoMoto.prefab";
        const float HumoX = 330f, HumoY = 862f;
        const int OrdenHumo = 10, OrdenInterfaz = 20;

        static readonly Color Blanco = Color.white;
        static readonly Color Oscuro = new Color32(31, 20, 46, 255);
        static readonly Color Ayuda = new Color32(228, 218, 238, 255);

        [MenuItem("Zombineta/UI/Reconstruir menu principal")]
        static void Rebuild()
        {
            if (EditorUtility.DisplayDialog("Menu principal",
                    "Esto rehace el contenido del Canvas del menu principal (fondo, panel, botones) " +
                    "y pisa los cambios hechos a mano en el. Tambien saca la niebla y el faro; el humo del escape se vuelve a poner.",
                    "Reconstruir", "Cancelar"))
                Build();
        }

        public static void Build()
        {
            var fondo = Importar(ArteDir + "menu_fondo_v2.png", Vector4.zero, comprimir: true);
            var panelSprite = Importar(UiDir + "menu_panel.png", new Vector4(40, 40, 40, 40));
            var seleccion = Importar(UiDir + "menu_boton_sel.png", new Vector4(26, 24, 26, 24));
            var linea = Importar(UiDir + "menu_linea.png", Vector4.zero);
            var tecla = Importar(UiDir + "menu_tecla.png", new Vector4(14, 14, 14, 14));

            var escena = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = Object.FindAnyObjectByType<MainMenuScreen>(FindObjectsInactive.Include);
            var canvas = screen.GetComponent<Canvas>();
            var root = canvas.transform;

            // Lo anterior: el boceto, los botones viejos y los efectos de MenuConVidaBuilder.
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            var humo = GameObject.Find("Humo del escape");
            if (humo != null) Object.DestroyImmediate(humo);

            // El Canvas se dibuja con la camara para que el humo (particulas del mundo) quede entre
            // el fondo y la interfaz, que va en un Canvas hijo por encima de todo.
            var cam = Camera.main;
            cam.backgroundColor = new Color32(40, 26, 74, 255);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 0;

            var bg = O.AddImage(root, "Fondo", Blanco, 0, 0, 1920, 1080);
            bg.sprite = fondo;
            bg.raycastTarget = false;

            var uiGo = new GameObject("Interfaz", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            uiGo.transform.SetParent(root, false);
            var ui = (RectTransform)uiGo.transform;
            ui.anchorMin = Vector2.zero;
            ui.anchorMax = Vector2.one;
            ui.offsetMin = ui.offsetMax = Vector2.zero;
            var canvasUi = uiGo.GetComponent<Canvas>();
            canvasUi.overrideSorting = true;
            canvasUi.sortingOrder = OrdenInterfaz;

            var panel = O.AddImage(ui, "Panel", Blanco, PanelX, PanelY, PanelW, PanelH);
            panel.sprite = panelSprite;
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = false;

            var items = new (string texto, string icono, UnityEngine.Events.UnityAction accion)[]
            {
                ("Iniciar", "icono_iniciar", screen.Play),
                ("Tutorial", "icono_tutorial", screen.OpenTutorial),
                ("Opciones", "icono_opciones", screen.OpenOptions),
                ("Créditos", "icono_creditos", screen.OpenCredits),
                ("Salir", "icono_salir", screen.Quit),
            };

            var botones = new Button[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                float y = FilaY + i * FilaPaso;
                botones[i] = AddBoton(ui, items[i].texto, Importar(UiDir + items[i].icono + ".png", Vector4.zero),
                    seleccion, y, items[i].accion);

                var sep = O.AddImage(ui,"Linea " + items[i].texto, new Color(1f, 1f, 1f, 0.9f), FilaX + 2f, y + 95f, FilaW - 24f, 2f);
                sep.sprite = linea;
                sep.raycastTarget = false;
            }
            Chain(botones);

            // Ayuda de teclas.
            AddAyuda(ui, "↑/↓", 1116f, 66f, "Navegar", 1200f);
            AddAyuda(ui, "Enter", 1352f, 76f, "Seleccionar", 1444f);

            var version = O.AddText(ui, "Pie", TextoDeVersion.Version, 22, new Color(1f, 1f, 1f, 0.55f),
                1560f, 1040f, 330f, 30f, TextAnchor.MiddleRight);
            version.gameObject.AddComponent<TextoDeVersion>();

            O.Set(screen, "firstSelected", botones[0]);

            // Humo del escape: las mismas particulas de la moto del juego, mas grandes (la moto del
            // menu ocupa media pantalla) y sin el chorro del turbo.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumoPrefab);
            if (prefab != null)
            {
                var humoNuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                humoNuevo.name = "Humo del escape";
                PrefabUtility.UnpackPrefabInstance(humoNuevo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var turbo = humoNuevo.transform.Find("Turbo");
                if (turbo != null) Object.DestroyImmediate(turbo.gameObject);
                humoNuevo.transform.position = PuntoDelCanvas(canvas, HumoX, HumoY);
                humoNuevo.transform.localScale = Vector3.one * 3.2f;
                foreach (var r in humoNuevo.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    r.sortingOrder = OrdenHumo;

                // En el juego la emision la maneja la moto segun su velocidad; aca va fija: el
                // escape en marcha lenta y un poco de polvo en el piso.
                foreach (var ps in humoNuevo.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var e = ps.emission;
                    e.rateOverTime = ps.gameObject == humoNuevo ? 2.5f : 11f;
                }
            }
            else
                Debug.LogWarning("No se encontro el prefab del humo: " + HumoPrefab);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("Menu principal reconstruido en " + ScenePath);

            void AddAyuda(Transform padre, string teclaTexto, float x, float w, string etiqueta, float xEtiqueta)
            {
                var caja = O.AddImage(padre, "Tecla " + teclaTexto, Blanco, x, 953f, w, 36f);
                caja.sprite = tecla;
                caja.type = Image.Type.Sliced;
                caja.raycastTarget = false;
                O.AddText(caja.transform, "Texto", teclaTexto, 20, Blanco, 0, 0, w, 36f, TextAnchor.MiddleCenter);
                O.AddText(padre, "Ayuda " + etiqueta, etiqueta, 26, Ayuda, xEtiqueta, 949f, 220f, 44f, TextAnchor.MiddleLeft);
            }
        }

        /// <summary>El punto del Canvas (x/y desde arriba a la izquierda, en 1920x1080) en el mundo.</summary>
        static Vector3 PuntoDelCanvas(Canvas canvas, float x, float y)
        {
            var cam = canvas.worldCamera;
            float alto = cam.orthographicSize * 2f;
            float ancho = alto * 16f / 9f;
            var c = cam.transform.position;
            return new Vector3(c.x - ancho * 0.5f + x / 1920f * ancho, c.y + alto * 0.5f - y / 1080f * alto, 0f);
        }

        static Button AddBoton(Transform root, string nombre, Sprite icono, Sprite seleccion, float y,
                               UnityEngine.Events.UnityAction accion)
        {
            // La fila entera es el area clickeable: una imagen transparente.
            var fila = O.AddImage(root, "Boton " + nombre, new Color(1f, 1f, 1f, 0f), FilaX, y, FilaW, FilaH);

            var resaltado = O.AddImage(fila.transform, "Resaltado", Blanco, 0, 0, FilaW, FilaH);
            resaltado.sprite = seleccion;
            resaltado.type = Image.Type.Sliced;
            resaltado.raycastTarget = false;

            var img = O.AddImage(fila.transform, "Icono", Blanco, 44f, 16f, 48f, 48f);
            img.sprite = icono;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var texto = O.AddText(fila.transform, "Texto", nombre, 42, Blanco, 121f, 0f, 480f, FilaH, TextAnchor.MiddleLeft);

            var boton = fila.gameObject.AddComponent<Button>();
            boton.targetGraphic = fila;
            boton.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(boton.onClick, accion);

            var visual = fila.gameObject.AddComponent<BotonDeMenu>();
            var so = new SerializedObject(visual);
            so.FindProperty("resaltado").objectReferenceValue = resaltado.gameObject;
            so.FindProperty("icono").objectReferenceValue = img;
            so.FindProperty("texto").objectReferenceValue = texto;
            so.FindProperty("colorNormal").colorValue = Blanco;
            so.FindProperty("colorSeleccionado").colorValue = Oscuro;
            so.ApplyModifiedPropertiesWithoutUndo();
            return boton;
        }

        /// <summary>Navegacion explicita arriba/abajo, dando la vuelta.</summary>
        static void Chain(Button[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = items[(i - 1 + items.Length) % items.Length],
                    selectOnDown = items[(i + 1) % items.Length],
                };
            }
        }

        /// <summary>Deja la textura como sprite de UI (con su borde para 9-slice) y la devuelve.</summary>
        internal static Sprite Importar(string ruta, Vector4 borde, bool comprimir = false)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (imp == null)
            {
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
                imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = borde;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = !comprimir;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 2048;
            imp.textureCompression = comprimir ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }
    }
}
