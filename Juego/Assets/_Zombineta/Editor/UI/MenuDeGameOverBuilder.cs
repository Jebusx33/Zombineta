using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Juego.Flow;
using Zombineta.Juego.Screens;
using O = Zombineta.Juego.EditorTools.OptionsMenuBuilder;
using P = Zombineta.Juego.EditorTools.MenuDePausaBuilder;

namespace Zombineta.Juego.EditorTools
{
    /// <summary>
    /// Arma la pantalla de GameOver: primero la pagina de comic que se arma de a vinetas
    /// (Art/Screens/GameOver/gameover_page1..4) y despues el menu con el estilo del menu principal,
    /// la pausa y las opciones: el mismo panel oscuro translucido con borde rosa, titulo con
    /// subrayado, filas con resaltado rosa y ayuda de teclas. El titulo es el que ya tenia la escena
    /// (se lee antes de rehacerla), asi que cambiarlo a mano y volver a correr esto lo conserva.
    /// Reusa la escena GameOver: conserva el Canvas con su GameOverScreen, SonidosAleatorios y
    /// UiSonidos. Se puede volver a correr.
    /// </summary>
    public static class MenuDeGameOverBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.GameOver + ".unity";
        const string TituloPorDefecto = "CAISTE ANTE LA HORA";
        const string ArteDir = "Assets/_Zombineta/Art/Screens/GameOver/";
        const int Paginas = 4;

        // Panel abajo a la izquierda, sobre el piso y la moto: al centro tapaba a los zombies, que son
        // el remate de la pagina. Titulo, dos filas y aire abajo, por encima de la ayuda de teclas.
        const float PanelW = 760f, PanelX = 60f, PanelH = 340f, PanelY = 610f, ListaY = PanelY + 140f;
        const float FilaX = PanelX + 60f, FilaW = PanelW - 120f;

        [MenuItem("Zombineta/UI/Reconstruir pantalla de GameOver")]
        static void Rebuild()
        {
            if (EditorUtility.DisplayDialog("Pantalla de GameOver",
                    "Esto rehace el contenido del Canvas de GameOver (vinetas, panel y botones), conservando " +
                    "su titulo, y pisa los demas cambios hechos a mano en el.",
                    "Reconstruir", "Cancelar"))
                Build();
        }

        public static void Build()
        {
            var panelSprite = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.UiDir + "menu_panel.png", new Vector4(40, 40, 40, 40));
            var tecla = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.UiDir + "menu_tecla.png", new Vector4(14, 14, 14, 14));

            var escena = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = Object.FindAnyObjectByType<GameOverScreen>(FindObjectsInactive.Include);
            var root = screen.transform;

            // Lo que puso el equipo: el titulo.
            string titulo = TituloPorDefecto;
            var tituloViejo = root.Find("Titulo") ?? root.Find("Menu/Titulo");
            if (tituloViejo != null && tituloViejo.TryGetComponent<Text>(out var ti) && !string.IsNullOrWhiteSpace(ti.text))
                titulo = ti.text;

            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            // La pagina de comic: cada vineta ocupa la pantalla entera y se suma encima de las anteriores.
            var secuencia = new GameObject("Secuencia", typeof(RectTransform));
            secuencia.transform.SetParent(root, false);
            O.Place((RectTransform)secuencia.transform, 0, 0, 1920, 1080);
            var paginas = new Image[Paginas];
            for (int i = 0; i < Paginas; i++)
            {
                paginas[i] = O.AddImage(secuencia.transform, "Pagina " + (i + 1), Color.white, 0, 0, 1920, 1080);
                paginas[i].sprite = ImportarPagina(ArteDir + "gameover_page" + (i + 1) + ".png");
                paginas[i].raycastTarget = false;
            }

            var ayuda = O.AddText(root, "Ayuda de la secuencia", "Enter: siguiente     Esc: saltear", 26, Color.white,
                360f, 1015f, 1200f, 50f, TextAnchor.MiddleCenter);
            ayuda.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            var ayudaGrupo = ayuda.gameObject.AddComponent<CanvasGroup>();
            ayudaGrupo.interactable = false;
            ayudaGrupo.blocksRaycasts = false;

            // El menu va todo junto en un grupo, que aparece cuando termina la secuencia.
            var menuGo = new GameObject("Menu", typeof(RectTransform), typeof(CanvasGroup));
            menuGo.transform.SetParent(root, false);
            O.Place((RectTransform)menuGo.transform, 0, 0, 1920, 1080);
            var menuRoot = menuGo.transform;

            // Velo para que el panel y la ayuda se lean sobre la pagina.
            var velo = O.AddImage(menuRoot, "Velo", new Color(0f, 0f, 0f, 0.35f), 0, 0, 1920, 1080);
            velo.raycastTarget = false;

            P.AddPanel(menuRoot, panelSprite, PanelY, PanelH, titulo, PanelX, PanelW);

            var listaGo = new GameObject("Botones", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listaGo.transform.SetParent(menuRoot, false);
            O.Place((RectTransform)listaGo.transform, FilaX, ListaY, FilaW, 2 * P.FilaH + P.Separacion);
            var layout = listaGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = P.Separacion;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var reintentar = P.AddBoton(listaGo.transform, "Reintentar", screen.Retry, FilaW);
            var menu = P.AddBoton(listaGo.transform, "Menú principal", screen.ToMenu, FilaW);
            P.Chain(new[] { reintentar, menu });

            O.AddAyudaMenu(menuRoot, "Navegar", 700f, new[] { "↑", "↓" }, tecla);
            O.AddAyudaMenu(menuRoot, "Seleccionar", 1000f, new[] { "Enter" }, tecla);

            O.Set(screen, "firstSelected", reintentar);
            O.Set(screen, "menu", menuGo.GetComponent<CanvasGroup>());
            O.Set(screen, "ayudaDeLaSecuencia", ayudaGrupo);
            O.Set(screen, "submitAction", O.FindAction("UI/Submit"));
            O.Set(screen, "cancelAction", O.FindAction("UI/Cancel"));
            var lista = new SerializedObject(screen);
            var prop = lista.FindProperty("paginas");
            prop.arraySize = Paginas;
            for (int i = 0; i < Paginas; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = paginas[i];
            lista.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("Pantalla de GameOver reconstruida en " + ScenePath);
        }

        /// <summary>Las vinetas, como las de las cinematicas: sprite entero, sin mipmaps y comprimidas.</summary>
        static Sprite ImportarPagina(string ruta)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (imp == null)
            {
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
                imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }
    }
}
