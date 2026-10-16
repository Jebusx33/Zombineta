using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Juego.Flow;
using Zombineta.Juego.Screens;
using O = Zombineta.Juego.EditorTools.OptionsMenuBuilder;

namespace Zombineta.Juego.EditorTools
{
    /// <summary>
    /// Le da movimiento al menu principal sin tocar su armado: niebla, el haz del faro de la
    /// moto apuntando a los zombies, humo del escape y un parpadeo de luz cada tanto. Para que
    /// el humo (particulas del mundo) quede entre el fondo y los botones, el Canvas pasa a
    /// dibujarse con la camara y los botones van en un Canvas propio por encima. Se puede volver
    /// a correr: saca lo que habia puesto antes.
    /// </summary>
    public static class MenuConVidaBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.MainMenu + ".unity";
        const string Fx = "Assets/_Zombineta/Art/MenuInicio/fx/";
        const string Humo = "Assets/_Zombineta/Prefabs/FxHumoMoto.prefab";
        const string Interfaz = "Interfaz";
        const string Efectos = "Efectos";

        // Orden de dibujo: fondo (Canvas raiz) < niebla y luces < humo < botones.
        const int OrdenHumo = 10;
        const int OrdenInterfaz = 20;

        [MenuItem("Zombineta/UI/Darle vida al menu principal")]
        public static void Aplicar()
        {
            var escena = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = Object.FindFirstObjectByType<MainMenuScreen>(FindObjectsInactive.Include);
            var canvas = screen.GetComponent<Canvas>();
            var root = canvas.transform;
            var cam = Camera.main;

            // Lo de una corrida anterior.
            var viejo = root.Find(Efectos);
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
            var humoViejo = GameObject.Find("Humo del escape");
            if (humoViejo != null) Object.DestroyImmediate(humoViejo);

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 0;

            // Los botones, el titulo y el pie, en un Canvas hijo que se dibuja arriba de todo.
            var interfaz = root.Find(Interfaz);
            if (interfaz == null)
            {
                var go = new GameObject(Interfaz, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                go.transform.SetParent(root, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                interfaz = rt;
                var hijos = new System.Collections.Generic.List<Transform>();
                foreach (Transform h in root)
                    if (h != interfaz && h.name != "Fondo")
                        hijos.Add(h);
                foreach (var h in hijos)
                    h.SetParent(interfaz, false);
            }
            var ci = interfaz.GetComponent<Canvas>();
            ci.overrideSorting = true;
            ci.sortingOrder = OrdenInterfaz;

            // Efectos entre el fondo y la interfaz.
            var efectosGo = new GameObject(Efectos, typeof(RectTransform));
            efectosGo.transform.SetParent(root, false);
            efectosGo.transform.SetSiblingIndex(interfaz.GetSiblingIndex());
            var efectos = (RectTransform)efectosGo.transform;
            efectos.anchorMin = Vector2.zero;
            efectos.anchorMax = Vector2.one;
            efectos.offsetMin = efectos.offsetMax = Vector2.zero;

            var aditivo = MaterialAditivo();
            var niebla = Textura("niebla");

            var lejos = Velo(efectos, "Niebla lejana", niebla, new Color(1f, 0.85f, 0.95f, 0.1f), 0f, 380f, 1920f, 520f);
            var cerca = Velo(efectos, "Niebla cercana", niebla, new Color(1f, 0.9f, 1f, 0.16f), 0f, 640f, 1920f, 460f);
            cerca.uvRect = new Rect(0.37f, 0f, 0.8f, 1f);

            var haz = Imagen(efectos, "Haz del faro", Sprite("haz"), new Color(1f, 0.93f, 0.7f, 0.7f), 760f, 260f, 1160f, 560f, aditivo);
            haz.rectTransform.localEulerAngles = new Vector3(0f, 0f, -9f);
            var brillo = Imagen(efectos, "Brillo del faro", Sprite("brillo"), new Color(1f, 0.92f, 0.65f, 0.75f), 700f, 420f, 200f, 200f, aditivo);
            var destello = O.AddImage(efectos, "Parpadeo", new Color(1f, 0.95f, 0.85f, 0f), 0f, 0f, 1920f, 1080f);
            destello.raycastTarget = false;
            destello.material = aditivo;

            var vida = efectosGo.AddComponent<MenuConVida>();
            var so = new SerializedObject(vida);
            var lista = so.FindProperty("nieblas");
            lista.arraySize = 2;
            Niebla(lista.GetArrayElementAtIndex(0), lejos, -0.006f, 0.3f, 13f);
            Niebla(lista.GetArrayElementAtIndex(1), cerca, -0.014f, 0.25f, 8f);
            so.FindProperty("haz").objectReferenceValue = haz;
            so.FindProperty("brilloFaro").objectReferenceValue = brillo;
            so.FindProperty("destello").objectReferenceValue = destello;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Humo del escape: las mismas particulas de la moto del juego, mas grandes (la moto
            // del menu ocupa media pantalla) y sin el chorro del turbo.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Humo);
            if (prefab != null)
            {
                var humo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                humo.name = "Humo del escape";
                PrefabUtility.UnpackPrefabInstance(humo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var turbo = humo.transform.Find("Turbo");
                if (turbo != null) Object.DestroyImmediate(turbo.gameObject);
                humo.transform.position = PuntoDelCanvas(canvas, 300f, 905f);
                humo.transform.localScale = Vector3.one * 3.2f;
                foreach (var r in humo.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    r.sortingOrder = OrdenHumo;

                // En el juego la emision la maneja la moto segun su velocidad; aca va fija: el
                // escape en marcha lenta y un poco de polvo en el piso.
                foreach (var ps in humo.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var e = ps.emission;
                    e.rateOverTime = ps.gameObject == humo ? 2.5f : 11f;
                }
            }

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("Menu principal con vida: " + ScenePath);
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

        static void Niebla(SerializedProperty p, RawImage img, float velocidad, float respiro, float periodo)
        {
            p.FindPropertyRelative("imagen").objectReferenceValue = img;
            p.FindPropertyRelative("velocidad").floatValue = velocidad;
            p.FindPropertyRelative("respiro").floatValue = respiro;
            p.FindPropertyRelative("periodo").floatValue = periodo;
        }

        static RawImage Velo(Transform padre, string nombre, Texture2D tex, Color color, float x, float y, float w, float h)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(padre, false);
            var img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.color = color;
            img.raycastTarget = false;
            O.Place(img.rectTransform, x, y, w, h);
            return img;
        }

        static Image Imagen(Transform padre, string nombre, Sprite sprite, Color color, float x, float y, float w, float h, Material mat)
        {
            var img = O.AddImage(padre, nombre, color, x, y, w, h);
            img.sprite = sprite;
            img.material = mat;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Material de UI que suma luz en vez de tapar: para el faro y el parpadeo.</summary>
        static Material MaterialAditivo()
        {
            const string ruta = Fx + "UI_Aditivo.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat != null)
                return mat;
            mat = new Material(Shader.Find("UI/Default"));
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            AssetDatabase.CreateAsset(mat, ruta);
            return mat;
        }

        static Texture2D Textura(string nombre)
        {
            string ruta = Fx + nombre + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (imp.wrapMode != TextureWrapMode.Repeat || imp.mipmapEnabled || !imp.alphaIsTransparency)
            {
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        static Sprite Sprite(string nombre)
        {
            string ruta = Fx + nombre + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (imp.textureType != TextureImporterType.Sprite || imp.mipmapEnabled)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }
    }
}
