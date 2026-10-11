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
    /// Arma la escena Credits con el fondo de la ciudad, un velo oscuro al centro para que el
    /// texto se lea, el rodado de Creditos.asset y la misma ayuda de teclas que Opciones.
    /// </summary>
    public static class CreditsMenuBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.Credits + ".unity";
        const string ArtDir = "Assets/_Zombineta/Art/Creditos";
        const string FondoPath = ArtDir + "/creditos_fondo.jpg";
        const string CreditosPath = "Assets/_Zombineta/Settings/Creditos.asset";
        const string VientoGuid = "19959c1022d041e4f8db39b519eff8d9";

        // Lo que queda libre abajo para la ayuda de teclas: el texto se recorta antes.
        const float MargenInferior = 130f;

        [MenuItem("Zombineta/UI/Reconstruir creditos")]
        static void Rebuild()
        {
            if (EditorUtility.DisplayDialog("Creditos",
                    "Esto regenera la escena Credits desde cero y pisa los cambios hechos a mano en ella.",
                    "Reconstruir", "Cancelar"))
                Build();
        }

        public static void Build()
        {
            var fondo = SpriteDeFondo();
            // Oscuro en el centro y transparente a los costados: detras de la columna de texto.
            var velo = O.Degradado("creditos_velo.png", x =>
            {
                float d = Mathf.Abs(x - 0.5f) * 2f;
                return new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, d)));
            }, ArtDir);

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
            var root = canvas.transform;
            var screen = canvas.gameObject.AddComponent<CreditsScreen>();

            // El fondo cubre toda la pantalla sin deformarse, en cualquier relacion de aspecto.
            var bg = O.AddImage(root, "Fondo", Color.white, 0, 0, 1920, 1080);
            bg.sprite = fondo;
            bg.raycastTarget = false;
            var bgrt = bg.rectTransform;
            bgrt.anchorMin = bgrt.anchorMax = bgrt.pivot = new Vector2(0.5f, 0.5f);
            bgrt.anchoredPosition = Vector2.zero;
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            var sombra = O.AddImage(root, "Velo", new Color(O.Panel.r, O.Panel.g, O.Panel.b, 0.6f), 0, 0, 1920, 1080);
            sombra.sprite = velo;
            sombra.raycastTarget = false;
            Stretch(sombra.rectTransform, 0f);

            // Viewport: recorta el rodado con bordes suaves y deja libre la ayuda de abajo.
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(root, false);
            var viewport = (RectTransform)viewportGo.transform;
            Stretch(viewport, MargenInferior);
            viewportGo.GetComponent<RectMask2D>().softness = new Vector2Int(0, 90);

            var contenedorGo = new GameObject("Contenedor", typeof(RectTransform));
            contenedorGo.transform.SetParent(viewport, false);
            var contenedor = (RectTransform)contenedorGo.transform;
            contenedor.anchorMin = contenedor.anchorMax = new Vector2(0.5f, 0f);
            contenedor.pivot = new Vector2(0.5f, 1f);
            contenedor.sizeDelta = new Vector2(1600f, 0f);
            contenedor.anchoredPosition = Vector2.zero;

            O.AddAyuda(root, "Acelerar", 1471f, new[] { "Enter" });
            var volver = O.AddAyuda(root, "Volver", 1686f, new[] { "Esc" });
            var boton = volver.gameObject.AddComponent<Button>();
            boton.targetGraphic = volver;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(boton.onClick, screen.Volver);

            var sonidos = canvas.gameObject.AddComponent<UiSonidos>();
            O.Set(sonidos, "submitAction", O.FindAction("UI/Submit"));
            O.Set(sonidos, "cancelAction", O.FindAction("UI/Cancel"));

            var viento = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(VientoGuid));
            if (viento != null)
            {
                var aleatorios = new SerializedObject(canvas.gameObject.AddComponent<SonidosAleatorios>());
                var lista = aleatorios.FindProperty("sonidos");
                lista.arraySize = 1;
                lista.GetArrayElementAtIndex(0).objectReferenceValue = viento;
                aleatorios.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("No se encontro el sonido de viento de los creditos (guid " + VientoGuid + ").");
            }

            // Sin boton seleccionado: Enter acelera el rodado en vez de "clickear" Volver.
            O.Set(screen, "datos", AssetDatabase.LoadAssetAtPath<Creditos>(CreditosPath));
            O.Set(screen, "viewport", viewport);
            O.Set(screen, "contenedor", contenedor);
            O.Set(screen, "submitAction", O.FindAction("UI/Submit"));
            O.Set(screen, "cancelAction", O.FindAction("UI/Cancel"));

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Creditos reconstruidos en " + ScenePath);
        }

        static void Stretch(RectTransform rt, float margenInferior)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0f, margenInferior);
            rt.offsetMax = Vector2.zero;
        }

        static Sprite SpriteDeFondo()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(FondoPath);
            if (importer == null)
            {
                Debug.LogError("Falta el fondo de los creditos en " + FondoPath);
                return null;
            }
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(FondoPath);
        }
    }
}
