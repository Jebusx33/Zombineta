using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zombineta.Audio;
using Zombineta.Juego.Flow;
using Zombineta.Juego.Screens;

namespace Zombineta.Juego.EditorTools
{
    /// <summary>
    /// Arma la escena Options con el estilo del menu principal: su mismo fondo, un panel oscuro
    /// con borde rosa a la derecha, pestanas de seccion arriba (Sonido, Pantalla y Jugabilidad,
    /// la activa como tira clara) y las filas agrupadas por seccion, mas la ayuda de teclas
    /// debajo. Las medidas son en 1920x1080, contadas desde la esquina de arriba a la izquierda.
    /// </summary>
    public static class OptionsMenuBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.Options + ".unity";
        internal const string ArtDir = "Assets/_Zombineta/Art/UI/Opciones";
        const string ActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        // Panel y Chip los sigue usando la pantalla de creditos.
        internal static readonly Color Panel = new Color32(28, 20, 38, 255);
        internal static readonly Color Chip = new Color32(90, 83, 99, 235);

        // La paleta del menu principal: rosa de acento, blanco y violeta oscuro.
        internal static readonly Color Rosa = new Color32(233, 31, 128, 255);
        internal static readonly Color RosaClaro = new Color32(255, 170, 214, 255);
        internal static readonly Color Ayuda = new Color32(228, 218, 238, 255);
        static readonly Color Pista = new Color32(150, 45, 125, 255);
        static readonly Color Manija = Rosa;
        static readonly Color Linea = new Color32(214, 122, 180, 255);
        internal static readonly Color Resaltado = new Color(0.91f, 0.12f, 0.5f, 0.3f);

        // Panel (a la derecha, como el del menu principal: a la izquierda queda el arte) y filas.
        const float PanelX = 1000f, PanelY = 110f, PanelW = 860f, PanelH = 860f;
        const float FilaX = 1040f, FilaW = 780f, FilaH = 44f;
        const float EtiquetaX = 20f, ControlX = 320f, ValorX = 600f;

        // Tira clara de la seleccion, la misma del menu principal.
        static Sprite spriteSeleccion;

        [MenuItem("Zombineta/UI/Reconstruir menu de opciones")]
        static void Rebuild()
        {
            if (EditorUtility.DisplayDialog("Menu de opciones",
                    "Esto regenera la escena Options desde cero y pisa los cambios hechos a mano en ella.",
                    "Reconstruir", "Cancelar"))
                Build();
        }

        public static void Build()
        {
            // El mismo fondo y panel que el menu principal (ver MenuPrincipalBuilder).
            var fondo = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.ArteDir + "menu_fondo_v2.png", Vector4.zero, comprimir: true);
            var panelSprite = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.UiDir + "menu_panel.png", new Vector4(40, 40, 40, 40));
            spriteSeleccion = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.UiDir + "menu_boton_sel.png", new Vector4(26, 24, 26, 24));
            var tecla = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.UiDir + "menu_tecla.png", new Vector4(14, 14, 14, 14));
            // Blanco con alfa: se tine por Image.color, sirve para las lineas de cada seccion.
            var desvanecido = Degradado("opciones_desvanecido.png",
                x => new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, x))));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = AddCanvas();
            var root = canvas.transform;
            var screen = canvas.gameObject.AddComponent<OptionsScreen>();

            var bg = AddImage(root, "Fondo", Color.white, 0, 0, 1920, 1080);
            bg.sprite = fondo;
            bg.raycastTarget = false;

            // Panel de opciones.
            var panel = AddImage(root, "Panel", Color.white, PanelX, PanelY, PanelW, PanelH);
            panel.sprite = panelSprite;
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = false;
            var filas = panel.transform;

            // Titulo con su subrayado, arriba del panel.
            var titulo = AddText(root, "Titulo", "OPCIONES", 56, Color.white, FilaX, 128, 600, 70, TextAnchor.MiddleLeft);
            titulo.fontStyle = FontStyle.Bold;
            var borde = titulo.gameObject.AddComponent<Outline>();
            borde.effectColor = new Color(0.1f, 0.05f, 0.12f, 1f);
            borde.effectDistance = new Vector2(2f, -2f);
            AddImage(root, "Subrayado", Rosa, FilaX + 2f, 204, 126, 8);

            float y = 335f - PanelY;
            AddEncabezado(filas, "SONIDO", y, desvanecido);
            var general = AddSlider(filas, "Volumen", y += 50f);
            var musica = AddSlider(filas, "Música", y += 48f);
            var efectos = AddSlider(filas, "Efectos", y += 48f);

            AddEncabezado(filas, "PANTALLA", y += 66f, desvanecido);
            var fullscreen = AddToggle(filas, "Pantalla completa", y += 50f);
            var camara = AddToggle(filas, "Efectos de cámara", y += 48f);
            var luz = AddDropdown(filas, "Iluminación", y += 48f, new[] { "Alta", "Baja" });

            AddEncabezado(filas, "JUGABILIDAD", y += 66f, desvanecido);
            var sacudidas = AddToggle(filas, "Sacudidas", y += 50f);
            var sangre = AddToggle(filas, "Sangre alta", y += 48f);
            var vibracion = AddToggle(filas, "Vibración", y += 48f);

            var orden = new Selectable[] { general, musica, efectos, fullscreen, camara, luz, sacudidas, sangre, vibracion };
            Chain(orden);

            // Pestanas: atajos a cada seccion, fuera de la cadena de arriba/abajo.
            var secciones = canvas.gameObject.AddComponent<OpcionesSecciones>();
            var so = new SerializedObject(secciones);
            var lista = so.FindProperty("secciones");
            lista.arraySize = 3;
            AddPestana(root, lista.GetArrayElementAtIndex(0), "Sonido", FilaX, general, musica, efectos);
            AddPestana(root, lista.GetArrayElementAtIndex(1), "Pantalla", FilaX + 268f, fullscreen, camara, luz);
            AddPestana(root, lista.GetArrayElementAtIndex(2), "Jugabilidad", FilaX + 536f, sacudidas, sangre, vibracion);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Ayuda de teclas, debajo del panel. "Volver" tambien se puede clickear.
            AddAyudaMenu(root, "Navegar", FilaX, new[] { "↑", "↓" }, tecla);
            AddAyudaMenu(root, "Ajustar", FilaX + 270f, new[] { "←", "→" }, tecla);
            var volver = AddAyudaMenu(root, "Volver", FilaX + 540f, new[] { "Esc" }, tecla);
            var boton = volver.gameObject.AddComponent<Button>();
            boton.targetGraphic = volver;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(boton.onClick, screen.Back);

            var sonidos = canvas.gameObject.AddComponent<UiSonidos>();
            Set(sonidos, "submitAction", FindAction("UI/Submit"));
            Set(sonidos, "cancelAction", FindAction("UI/Cancel"));

            Set(screen, "volumeGeneral", general);
            Set(screen, "volumeMusica", musica);
            Set(screen, "volumeEfectos", efectos);
            Set(screen, "fullscreen", fullscreen);
            Set(screen, "cameraEffects", camara);
            Set(screen, "cameraShake", sacudidas);
            Set(screen, "gore", sangre);
            Set(screen, "vibration", vibracion);
            Set(screen, "lightQuality", luz);
            Set(screen, "cancelAction", FindAction("UI/Cancel"));
            Set(screen, "firstSelected", general);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Menu de opciones reconstruido en " + ScenePath);
        }

        // --- Filas -------------------------------------------------------------------

        static void AddEncabezado(Transform panel, string titulo, float y, Sprite desvanecido)
        {
            float x = FilaX - PanelX;
            AddText(panel, "Encabezado " + titulo, titulo, 26, RosaClaro, x, y - 20f, 300f, 40f, TextAnchor.MiddleLeft);
            // La linea arranca despues del texto (mayusculas de 26 px: ~16 px por letra).
            float desde = x + titulo.Length * 16f + 60f;
            var linea = AddImage(panel, "Linea " + titulo, Linea, desde, y, FilaX + FilaW - PanelX - desde, 3f);
            linea.sprite = desvanecido;
            linea.raycastTarget = false;
        }

        /// <summary>Fila con resaltado y etiqueta; devuelve la fila para colgarle el control.</summary>
        static (RectTransform fila, Image resaltado, Text etiqueta) AddFila(Transform panel, string label, float y)
        {
            var go = new GameObject("Fila " + label, typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var rt = (RectTransform)go.transform;
            Place(rt, FilaX - PanelX, y - FilaH / 2f, FilaW, FilaH);

            // Transparente pero con raycast: el mouse sobre la fila la selecciona.
            var hit = go.AddComponent<Image>();
            hit.color = Color.clear;

            var resaltado = AddImage(rt, "Resaltado", Resaltado, 0, 0, FilaW, FilaH);
            resaltado.sprite = BuiltinSprite("UI/Skin/UISprite.psd");
            resaltado.type = Image.Type.Sliced;
            resaltado.raycastTarget = false;
            resaltado.enabled = false;

            var etiqueta = AddText(rt, "Etiqueta", label, 28, Color.white, EtiquetaX, 0, 290, FilaH, TextAnchor.MiddleLeft);
            return (rt, resaltado, etiqueta);
        }

        static void Fila(RectTransform fila, Selectable control, Image resaltado, Text etiqueta, Text valor)
        {
            var f = fila.gameObject.AddComponent<OpcionFila>();
            Set(f, "control", control);
            Set(f, "resaltado", resaltado);
            Set(f, "etiqueta", etiqueta);
            Set(f, "valor", valor);
            var so = new SerializedObject(f);
            so.FindProperty("etiquetaSeleccionada").colorValue = RosaClaro;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Slider AddSlider(Transform panel, string label, float y)
        {
            var (fila, resaltado, etiqueta) = AddFila(panel, label, y);

            var go = new GameObject("Opcion " + label, typeof(RectTransform));
            go.transform.SetParent(fila, false);
            Place((RectTransform)go.transform, ControlX, (FilaH - 32f) / 2f, 248f, 32f);

            var pista = AddImage(go.transform, "Pista", Pista, 0, 8, 248, 16);

            var area = new GameObject("Handle Slide Area", typeof(RectTransform));
            area.transform.SetParent(go.transform, false);
            var art = (RectTransform)area.transform;
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(8f, 0f);
            art.offsetMax = new Vector2(-8f, 0f);

            var manija = AddImage(area.transform, "Handle", Manija, 0, 0, 16, 32);
            var mrt = manija.rectTransform;
            mrt.anchorMin = new Vector2(0f, 0f);
            mrt.anchorMax = new Vector2(0f, 1f);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(16f, 0f);
            mrt.anchoredPosition = Vector2.zero;
            var contorno = manija.gameObject.AddComponent<Outline>();
            contorno.effectColor = Color.black;
            contorno.effectDistance = new Vector2(2f, -2f);

            var slider = go.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            slider.handleRect = mrt;
            slider.targetGraphic = manija;
            slider.transition = Selectable.Transition.None;
            pista.raycastTarget = true;

            var valor = AddText(fila, "Valor", "50%", 22, Color.white, ValorX, 0, 100, FilaH, TextAnchor.MiddleLeft);
            Fila(fila, slider, resaltado, etiqueta, valor);
            return slider;
        }

        static Toggle AddToggle(Transform panel, string label, float y)
        {
            var (fila, resaltado, etiqueta) = AddFila(panel, label, y);

            // El toggle ocupa toda la fila: click en cualquier parte la cambia.
            var go = new GameObject("Opcion " + label, typeof(RectTransform));
            go.transform.SetParent(fila, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var zona = go.AddComponent<Image>();
            zona.color = Color.clear;

            var caja = AddImage(go.transform, "Caja", Pista, ControlX, (FilaH - 26f) / 2f, 26, 26);
            var interior = AddImage(caja.transform, "Interior", Panel, 3, 3, 20, 20);
            var tilde = AddImage(interior.transform, "Tilde", Manija, 3, 3, 14, 14);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = zona;
            toggle.graphic = tilde;
            toggle.transition = Selectable.Transition.None;
            toggle.isOn = true;
            go.AddComponent<AjusteConFlechas>();

            var valor = AddText(fila, "Valor", "Sí", 22, Color.white, ValorX, 0, 100, FilaH, TextAnchor.MiddleLeft);
            Fila(fila, toggle, resaltado, etiqueta, valor);
            return toggle;
        }

        static Dropdown AddDropdown(Transform panel, string label, float y, string[] options)
        {
            var (fila, resaltado, etiqueta) = AddFila(panel, label, y);

            var go = DefaultControls.CreateDropdown(new DefaultControls.Resources
            {
                standard = BuiltinSprite("UI/Skin/UISprite.psd"),
                background = BuiltinSprite("UI/Skin/Background.psd"),
                checkmark = BuiltinSprite("UI/Skin/Checkmark.psd"),
                dropdown = BuiltinSprite("UI/Skin/DropdownArrow.psd"),
                mask = BuiltinSprite("UI/Skin/UIMask.psd"),
            });
            go.name = "Opcion " + label;
            go.transform.SetParent(fila, false);
            Place((RectTransform)go.transform, ControlX, (FilaH - 32f) / 2f, 248f, 32f);

            var fondo = go.GetComponent<Image>();
            fondo.color = new Color(Pista.r, Pista.g, Pista.b, 0.55f);

            var dropdown = go.GetComponent<Dropdown>();
            dropdown.transition = Selectable.Transition.None;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));

            // Sin flecha de desplegar: a los lados, las de "Ajustar".
            var flecha = go.transform.Find("Arrow");
            if (flecha != null)
                Object.DestroyImmediate(flecha.gameObject);
            var caption = dropdown.captionText;
            caption.font = UiFont;
            caption.fontSize = 20;
            caption.color = Color.white;
            caption.alignment = TextAnchor.MiddleCenter;
            var crt = caption.rectTransform;
            crt.offsetMin = new Vector2(24f, 0f);
            crt.offsetMax = new Vector2(-24f, 0f);
            AddText(go.transform, "Menos", "‹", 26, Color.white, 4, 0, 20, 32, TextAnchor.MiddleCenter);
            AddText(go.transform, "Mas", "›", 26, Color.white, 224, 0, 20, 32, TextAnchor.MiddleCenter);

            // La lista desplegada (Template arranca inactiva) con la misma letra y colores.
            if (dropdown.template != null)
            {
                foreach (var t in dropdown.template.GetComponentsInChildren<Text>(true))
                {
                    t.font = UiFont;
                    t.fontSize = 20;
                    t.color = Color.white;
                }
                var tfondo = dropdown.template.GetComponent<Image>();
                if (tfondo != null)
                    tfondo.color = Panel;
                foreach (var item in dropdown.template.GetComponentsInChildren<Toggle>(true))
                {
                    var colors = item.colors;
                    colors.normalColor = Panel;
                    colors.highlightedColor = Pista;
                    colors.selectedColor = Pista;
                    item.colors = colors;
                }
            }

            go.AddComponent<AjusteConFlechas>();
            Fila(fila, dropdown, resaltado, etiqueta, null);
            return dropdown;
        }

        // --- Pestanas y ayuda --------------------------------------------------------

        /// <summary>
        /// Pestana arriba del panel: la de la seccion activa se ve como la tira clara del menu
        /// principal, con el texto oscuro; las demas, texto blanco sobre el panel.
        /// </summary>
        static void AddPestana(Transform root, SerializedProperty seccion, string label, float x, params Selectable[] controles)
        {
            const float y = 226f, w = 252f, h = 64f;

            var area = AddImage(root, "Pestana " + label, Color.clear, x, y, w, h);

            var resaltado = AddImage(area.transform, "Resaltado", Color.white, 0, 0, w, h);
            resaltado.sprite = spriteSeleccion;
            resaltado.type = Image.Type.Sliced;
            resaltado.raycastTarget = false;

            var texto = AddText(area.transform, "Texto", label, 32, Color.white, 0, 0, w, h, TextAnchor.MiddleCenter);

            var boton = area.gameObject.AddComponent<Button>();
            boton.targetGraphic = area;
            boton.transition = Selectable.Transition.None;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };

            seccion.FindPropertyRelative("pestana").objectReferenceValue = boton;
            seccion.FindPropertyRelative("resaltado").objectReferenceValue = resaltado.gameObject;
            seccion.FindPropertyRelative("etiqueta").objectReferenceValue = texto;
            var lista = seccion.FindPropertyRelative("controles");
            lista.arraySize = controles.Length;
            for (int i = 0; i < controles.Length; i++)
                lista.GetArrayElementAtIndex(i).objectReferenceValue = controles[i];
        }

        /// <summary>La ayuda de teclas del menu principal: teclas con contorno y el texto al lado.</summary>
        internal static Image AddAyudaMenu(Transform root, string label, float x, string[] teclas, Sprite tecla)
        {
            const float y = 984f, h = 44f;

            float ancho = 0f;
            foreach (var t in teclas)
                ancho += TeclaAncho(t) + 8f;
            var contenedor = AddImage(root, "Ayuda " + label, Color.clear, x, y, ancho + 8f + 170f, h);

            float kx = 0f;
            foreach (var t in teclas)
            {
                float kw = TeclaAncho(t);
                var caja = AddImage(contenedor.transform, "Tecla " + t, Color.white, kx, 4f, kw, 36f);
                caja.sprite = tecla;
                caja.type = Image.Type.Sliced;
                caja.raycastTarget = false;
                AddText(caja.transform, "Texto", t, 20, Color.white, 0, 0, kw, 36f, TextAnchor.MiddleCenter);
                kx += kw + 8f;
            }
            AddText(contenedor.transform, "Texto", label, 26, Ayuda, kx + 8f, 0, 170f, h, TextAnchor.MiddleLeft);
            return contenedor;
        }

        static float TeclaAncho(string tecla) => tecla.Length > 3 ? 76f : tecla.Length > 1 ? 56f : 44f;

        internal static Image AddAyuda(Transform root, string label, float x, string[] teclas)
        {
            const float y = 988f, w = 200f, h = 55f;
            var chip = AddImage(root, "Ayuda " + label, Chip, x, y, w, h);
            chip.sprite = BuiltinSprite("UI/Skin/UISprite.psd");
            chip.type = Image.Type.Sliced;

            float kx = 17f;
            foreach (var tecla in teclas)
            {
                float kw = tecla.Length > 3 ? 70f : tecla.Length > 1 ? 46f : 36f;
                var caja = AddImage(chip.transform, "Tecla " + tecla, Color.white, kx, 9, kw, 37);
                caja.sprite = BuiltinSprite("UI/Skin/UISprite.psd");
                caja.type = Image.Type.Sliced;
                caja.raycastTarget = false;
                var dentro = AddImage(caja.transform, "Dentro", Chip, 2, 2, kw - 4, 33);
                dentro.sprite = caja.sprite;
                dentro.type = Image.Type.Sliced;
                dentro.raycastTarget = false;
                AddText(caja.transform, "Texto", tecla, 20, Color.white, 0, 0, kw, 37, TextAnchor.MiddleCenter);
                kx += kw + 7f;
            }
            AddText(chip.transform, "Texto", label, 22, Color.white, kx + 12f, 0, w - kx - 12f, h, TextAnchor.MiddleLeft);
            return chip;
        }

        // --- Piezas ------------------------------------------------------------------

        internal static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        internal static Sprite BuiltinSprite(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        internal static Canvas AddCanvas(int sortingOrder = 200)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>Ubica un rect con x/y desde arriba a la izquierda del padre, como en la referencia.</summary>
        internal static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        internal static Image AddImage(Transform parent, string name, Color color, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            Place(image.rectTransform, x, y, w, h);
            return image;
        }

        internal static Text AddText(Transform parent, string name, string content, int size, Color color,
                            float x, float y, float w, float h, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiFont;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            Place(text.rectTransform, x, y, w, h);
            return text;
        }

        /// <summary>Textura de 256x4 en degradado horizontal, guardada como sprite.</summary>
        internal static Sprite Degradado(string file, System.Func<float, Color> color, string dir = ArtDir)
        {
            Directory.CreateDirectory(dir);
            string path = dir + "/" + file;

            var tex = new Texture2D(256, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < 256; x++)
            {
                var c = color(x / 255f);
                for (int y = 0; y < 4; y++)
                    tex.SetPixel(x, y, c);
            }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Navegacion explicita arriba/abajo, dando la vuelta; izquierda/derecha ajustan.</summary>
        static void Chain(Selectable[] items)
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

        internal static InputActionReference FindAction(string path)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ActionsPath))
                if (o is InputActionReference r && r.action != null &&
                    r.action.actionMap.name + "/" + r.action.name == path)
                    return r;
            Debug.LogWarning("No se encontro la accion " + path + " en " + ActionsPath);
            return null;
        }

        internal static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError("No existe el campo '" + field + "' en " + target.GetType().Name, target);
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
