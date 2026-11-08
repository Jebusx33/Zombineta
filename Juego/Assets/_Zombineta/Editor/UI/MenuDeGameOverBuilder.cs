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
    /// Arma la pantalla de GameOver con el estilo del menu principal, la pausa y las opciones: el
    /// mismo panel oscuro translucido con borde rosa, titulo con subrayado, filas con resaltado
    /// rosa y ayuda de teclas. La imagen de fondo y el titulo son los que ya tenia la escena (se
    /// leen antes de rehacerla), asi que cambiarlos a mano y volver a correr esto los conserva.
    /// Reusa la escena GameOver: conserva el Canvas con su GameOverScreen, SonidosAleatorios y
    /// UiSonidos. Se puede volver a correr.
    /// </summary>
    public static class MenuDeGameOverBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.GameOver + ".unity";
        const string TituloPorDefecto = "CAISTE ANTE LA HORA";

        // Panel al centro: titulo, dos filas y aire abajo.
        const float PanelH = 340f, PanelY = (1080f - PanelH) / 2f, ListaY = PanelY + 140f;

        [MenuItem("Zombineta/UI/Reconstruir pantalla de GameOver")]
        static void Rebuild()
        {
            if (EditorUtility.DisplayDialog("Pantalla de GameOver",
                    "Esto rehace el contenido del Canvas de GameOver (panel y botones), conservando " +
                    "su imagen de fondo y su titulo, y pisa los demas cambios hechos a mano en el.",
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

            // Lo que puso el equipo: la imagen de fondo (con su tinte) y el titulo.
            Sprite imagen = null;
            var tinte = new Color(0.46f, 0.24f, 0.25f, 1f);
            string titulo = TituloPorDefecto;
            var fondoViejo = root.Find("Fondo");
            if (fondoViejo != null && fondoViejo.TryGetComponent<Image>(out var fi))
            {
                imagen = fi.sprite;
                tinte = fi.color;
            }
            var tituloViejo = root.Find("Titulo");
            if (tituloViejo != null && tituloViejo.TryGetComponent<Text>(out var ti) && !string.IsNullOrWhiteSpace(ti.text))
                titulo = ti.text;

            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            var bg = O.AddImage(root, "Fondo", tinte, 0, 0, 1920, 1080);
            bg.sprite = imagen;
            bg.raycastTarget = false;
            // Velo para que el panel y la ayuda se lean sobre cualquier imagen.
            var velo = O.AddImage(root, "Velo", new Color(0f, 0f, 0f, 0.35f), 0, 0, 1920, 1080);
            velo.raycastTarget = false;

            P.AddPanel(root, panelSprite, PanelY, PanelH, titulo);

            var listaGo = new GameObject("Botones", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listaGo.transform.SetParent(root, false);
            O.Place((RectTransform)listaGo.transform, P.FilaX, ListaY, P.FilaW, 2 * P.FilaH + P.Separacion);
            var layout = listaGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = P.Separacion;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var reintentar = P.AddBoton(listaGo.transform, "Reintentar", screen.Retry);
            var menu = P.AddBoton(listaGo.transform, "Menú principal", screen.ToMenu);
            P.Chain(new[] { reintentar, menu });

            O.AddAyudaMenu(root, "Navegar", 700f, new[] { "↑", "↓" }, tecla);
            O.AddAyudaMenu(root, "Seleccionar", 1000f, new[] { "Enter" }, tecla);

            O.Set(screen, "firstSelected", reintentar);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("Pantalla de GameOver reconstruida en " + ScenePath);
        }
    }
}
