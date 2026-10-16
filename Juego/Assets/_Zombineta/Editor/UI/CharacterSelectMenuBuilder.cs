using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Audio;
using Zombineta.Juego.Flow;
using Zombineta.Juego.Screens;
using O = Zombineta.Juego.EditorTools.OptionsMenuBuilder;

namespace Zombineta.Juego.EditorTools
{
    /// <summary>
    /// Arma la escena CharacterSelect con el arte de la referencia: fondo, la ficha del personaje
    /// elegido (cada una es una imagen entera de 1920x1080), el marco con el titulo y la ayuda de
    /// teclas, y el boton Volver abajo a la derecha.
    /// </summary>
    public static class CharacterSelectMenuBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.CharacterSelect + ".unity";
        const string ArtDir = "Assets/_Zombineta/Art/Screens/SelectPlayer/";

        [MenuItem("Zombineta/UI/Reconstruir seleccion de personaje")]
        static void Rebuild()
        {
            if (EditorUtility.DisplayDialog("Seleccion de personaje",
                    "Esto regenera la escena CharacterSelect desde cero y pisa los cambios hechos a mano en ella.",
                    "Reconstruir", "Cancelar"))
                Build();
        }

        public static void Build()
        {
            var fondo = Sprite("bd_char_select");
            var marco = Sprite("bd_char_select_frame");
            var berenice = Sprite("bd_char_select_berenice");
            var hedy = Sprite("bd_char_select_hedy_l");
            var volverArte = Sprite("btn_back");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Pantalla base: trae su camara y el unico AudioListener.
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            var c = cam.GetComponent<Camera>();
            c.orthographic = true;
            c.orthographicSize = 6f;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var canvas = O.AddCanvas(0);
            var screen = canvas.gameObject.AddComponent<CharacterSelectScreen>();

            // Todo el arte esta pensado en 1920x1080: va en un lienzo de ese tamano que entra
            // entero en cualquier pantalla, con bandas negras si no es 16:9.
            var lienzoGo = new GameObject("Lienzo", typeof(RectTransform));
            lienzoGo.transform.SetParent(canvas.transform, false);
            var lienzo = (RectTransform)lienzoGo.transform;
            lienzo.anchorMin = lienzo.anchorMax = lienzo.pivot = new Vector2(0.5f, 0.5f);
            lienzo.sizeDelta = new Vector2(1920f, 1080f);
            var ajuste = lienzoGo.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajuste.aspectRatio = 16f / 9f;

            Capa(lienzo, "Fondo", fondo);
            var ficha = Capa(lienzo, "Ficha", berenice);
            Capa(lienzo, "Marco", marco);

            // El area de los personajes: con el foco ahi, izquierda/derecha cambian y Enter elige.
            var selector = O.AddImage(lienzo, "Selector", new Color(0f, 0f, 0f, 0f), 0f, 120f, 1920f, 860f);
            var boton = selector.gameObject.AddComponent<Button>();
            boton.targetGraphic = selector;
            boton.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(boton.onClick, screen.Confirmar);
            O.Set(selector.gameObject.AddComponent<CambioDePersonaje>(), "pantalla", screen);

            // Volver: donde lo muestra la referencia. El arte es solo el texto, sobre transparente:
            // el borde blanco y el relleno oscuro van por detras (un Outline sobre la imagen
            // borronearia el texto).
            var borde = O.AddImage(lienzo, "Borde Volver", Color.white, 1714f, 998f, 175f, 55f);
            borde.raycastTarget = false;
            var relleno = O.AddImage(lienzo, "Relleno Volver", new Color32(4, 4, 16, 255), 1717f, 1001f, 169f, 49f);
            relleno.raycastTarget = false;
            var volver = O.AddImage(lienzo, "Boton Volver", Color.white, 1717f, 1001f, 169f, 49f);
            volver.sprite = volverArte;
            var vb = volver.gameObject.AddComponent<Button>();
            vb.targetGraphic = volver;
            UnityEventTools.AddPersistentListener(vb.onClick, screen.Back);

            boton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = vb };
            vb.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = boton };

            var sonidos = canvas.gameObject.AddComponent<UiSonidos>();
            O.Set(sonidos, "submitAction", O.FindAction("UI/Submit"));
            O.Set(sonidos, "cancelAction", O.FindAction("UI/Cancel"));

            O.Set(screen, "firstSelected", boton);
            O.Set(screen, "ficha", ficha);
            O.Set(screen, "cancelAction", O.FindAction("UI/Cancel"));
            var so = new SerializedObject(screen);
            var fichas = so.FindProperty("fichas");
            fichas.arraySize = 2;
            fichas.GetArrayElementAtIndex(0).objectReferenceValue = berenice;
            fichas.GetArrayElementAtIndex(1).objectReferenceValue = hedy;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Seleccion de personaje reconstruida en " + ScenePath);
        }

        static Image Capa(RectTransform padre, string nombre, Sprite sprite)
        {
            var img = O.AddImage(padre, nombre, Color.white, 0f, 0f, 1920f, 1080f);
            img.sprite = sprite;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        static Sprite Sprite(string nombre)
        {
            string ruta = ArtDir + nombre + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (importer == null)
            {
                Debug.LogError("Falta " + ruta);
                return null;
            }
            // Cada imagen es una sola pieza de pantalla completa: si Unity la importa como
            // "Multiple" la corta en pedazos y la ficha muestra solo un recorte.
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled ||
                importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }
    }
}
