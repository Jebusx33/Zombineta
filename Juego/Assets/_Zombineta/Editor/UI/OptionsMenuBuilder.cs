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
    /// Arma la escena Options segun la referencia de arte: fondo en degradado, titulo dorado,
    /// pestanas de seccion a la izquierda y un panel con las filas agrupadas en Sonido, Pantalla
    /// y Jugabilidad, mas la ayuda de teclas abajo a la derecha. Las medidas son las de la
    /// referencia en 1920x1080, contadas desde la esquina de arriba a la izquierda.
    /// </summary>
    public static class OptionsMenuBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.Options + ".unity";
        internal const string ArtDir = "Assets/_Zombineta/Art/UI/Opciones";
        const string ActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        static readonly Color FondoIzq = new Color32(27, 18, 37, 255);
        static readonly Color FondoDer = new Color32(250, 250, 250, 255);
        internal static readonly Color Panel = new Color32(28, 20, 38, 255);
        internal static readonly Color Magenta = new Color32(242, 27, 214, 255);
        static readonly Color Pista = new Color32(142, 28, 110, 255);
        internal static readonly Color Dorado = new Color32(255, 210, 26, 255);
        static readonly Color Manija = new Color32(184, 146, 10, 255);
        static readonly Color Linea = new Color32(216, 214, 222, 255);
        internal static readonly Color Chip = new Color32(90, 83, 99, 235);
        static readonly Color Resaltado = new Color(0.95f, 0.11f, 0.84f, 0.16f);

        // Panel y filas.
        const float PanelX = 590f, PanelY = 262f, PanelW = 1000f, PanelH = 673f;
        const float FilaX = 632f, FilaW = 868f, FilaH = 44f;
        const float EtiquetaX = 20f, ControlX = 285f, ValorX = 558f;

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
            var fondo = Degradado("opciones_fondo.png", x => Color.Lerp(FondoIzq, FondoDer, x));
            // Blanco con alfa: se tine por Image.color, sirve para el panel y para las lineas.
            var desvanecido = Degradado("opciones_desvanecido.png",
                x => new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, x))));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = AddCanvas();
            var root = canvas.transform;
            var screen = canvas.gameObject.AddComponent<OptionsScreen>();

            var bg = AddImage(root, "Fondo", Color.white, 0, 0, 1920, 1080);
            bg.sprite = fondo;

            // Titulo con su subrayado.
            var titulo = AddText(root, "Titulo", "OPCIONES", 58, Dorado, 103, 108, 600, 70, TextAnchor.MiddleLeft);
            titulo.fontStyle = FontStyle.Bold;
            var borde = titulo.gameObject.AddComponent<Outline>();
            borde.effectColor = new Color(0.1f, 0.05f, 0.12f, 1f);
            borde.effectDistance = new Vector2(2f, -2f);
            AddImage(root, "Subrayado", Magenta, 105, 172, 126, 13);

            // Panel de opciones.
            var panel = AddImage(root, "Panel", Panel, PanelX, PanelY, PanelW, PanelH);
            panel.sprite = desvanecido;
            var filas = panel.transform;

            float y = 300f - PanelY;
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
            AddPestana(root, lista.GetArrayElementAtIndex(0), "Sonido", 262f, general, musica, efectos);
            AddPestana(root, lista.GetArrayElementAtIndex(1), "Pantalla", 353f, fullscreen, camara, luz);
            AddPestana(root, lista.GetArrayElementAtIndex(2), "Jugabilidad", 444f, sacudidas, sangre, vibracion);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Ayuda de teclas. "Volver" tambien se puede clickear.
            AddAyuda(root, "Navegar", 1255f, new[] { "↑", "↓" });
            AddAyuda(root, "Ajustar", 1471f, new[] { "←", "→" });
            var volver = AddAyuda(root, "Volver", 1686f, new[] { "Esc" });
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
            AddText(panel, "Encabezado " + titulo, titulo, 24, Color.white, x, y - 20f, 300f, 40f, TextAnchor.MiddleLeft);
            // La linea arranca despues del texto (mayusculas de 24 px: ~14 px por letra).
            float desde = x + titulo.Length * 14f + 66f;
            var linea = AddImage(panel, "Linea " + titulo, Linea, desde, y, 1470f - PanelX - desde, 3f);
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

            var etiqueta = AddText(rt, "Etiqueta", label, 24, Color.white, EtiquetaX, 0, 260, FilaH, TextAnchor.MiddleLeft);
            return (rt, resaltado, etiqueta);
        }

        static void Fila(RectTransform fila, Selectable control, Image resaltado, Text etiqueta, Text valor)
        {
            var f = fila.gameObject.AddComponent<OpcionFila>();
            Set(f, "control", control);
            Set(f, "resaltado", resaltado);
            Set(f, "etiqueta", etiqueta);
            Set(f, "valor", valor);
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

            var valor = AddText(fila, "Valor", "50%", 18, Color.white, ValorX, 0, 100, FilaH, TextAnchor.MiddleLeft);
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

            var valor = AddText(fila, "Valor", "Sí", 18, Color.white, ValorX, 0, 100, FilaH, TextAnchor.MiddleLeft);
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

        static void AddPestana(Transform root, SerializedProperty seccion, string label, float y, params Selectable[] controles)
        {
            const float x = 95f, w = 438f, h = 70f;

            var borde = AddImage(root, "Pestana " + label, Magenta, x, y, w, h);
            AddImage(borde.transform, "Fondo", Color.white, 1, 1, w - 2, h - 2).raycastTarget = false;
            AddText(borde.transform, "Texto", label, 36, Color.black, 90, 0, w - 100, h, TextAnchor.MiddleLeft);

            // Marca a la izquierda de la caja: crece hacia afuera en la seccion activa.
            var marca = AddImage(borde.transform, "Marca", Magenta, 0, 0, 10, h);
            marca.rectTransform.pivot = new Vector2(1f, 1f);
            marca.rectTransform.anchoredPosition = Vector2.zero;
            marca.raycastTarget = false;

            var boton = borde.gameObject.AddComponent<Button>();
            boton.targetGraphic = borde;
            boton.transition = Selectable.Transition.None;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };

            seccion.FindPropertyRelative("pestana").objectReferenceValue = boton;
            seccion.FindPropertyRelative("marca").objectReferenceValue = marca;
            var lista = seccion.FindPropertyRelative("controles");
            lista.arraySize = controles.Length;
            for (int i = 0; i < controles.Length; i++)
                lista.GetArrayElementAtIndex(i).objectReferenceValue = controles[i];
        }

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
