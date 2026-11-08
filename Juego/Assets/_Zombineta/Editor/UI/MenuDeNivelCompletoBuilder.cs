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
    /// Arma la pantalla de nivel completo con el estilo del menu principal, la pausa y las
    /// opciones: panel oscuro translucido con borde rosa, titulo con subrayado, el detalle del
    /// nivel y una fila "Continuar" con el resaltado rosa. El panel va a la derecha para no tapar
    /// la imagen. La imagen y el titulo son los que ya tenia la escena (se leen antes de rehacerla);
    /// el tinte de la imagen es TinteDeLaImagen, que se cambia aca y se vuelve a correr.
    /// Reusa la escena LevelComplete: conserva el Canvas con su LevelCompleteScreen y UiSonidos.
    /// </summary>
    public static class MenuDeNivelCompletoBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.LevelComplete + ".unity";
        const string TituloPorDefecto = "ENTREGA EXITOSA";

        // La imagen es un boceto en grises: este color la tine (se multiplica). Un rosa suave
        // acompana el rosa de la interfaz y se siente calido, como una entrega lograda; el violeta
        // de la interfaz sobre un fondo verde no pegaba. Alternativas: violeta (0.82, 0.72, 1),
        // ambar (1, 0.84, 0.58); blanco (1, 1, 1) deja la imagen como viene.
        static readonly Color TinteDeLaImagen = new Color(1f, 0.80f, 0.90f, 1f);

        // Panel a la derecha: la protagonista y la nena quedan libres a la izquierda.
        const float PanelW = 640f, PanelH = 320f;
        const float PanelX = 1920f - PanelW - 60f, PanelY = (1080f - PanelH) / 2f;
        const float FilaX = PanelX + 60f, FilaW = PanelW - 120f;

        [MenuItem("Zombineta/UI/Reconstruir pantalla de nivel completo")]
        static void Rebuild()
        {
            if (EditorUtility.DisplayDialog("Pantalla de nivel completo",
                    "Esto rehace el contenido del Canvas de LevelComplete (panel y boton), conservando " +
                    "su imagen y su titulo, y pisa los demas cambios hechos a mano en el.",
                    "Reconstruir", "Cancelar"))
                Build();
        }

        public static void Build()
        {
            var panelSprite = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.UiDir + "menu_panel.png", new Vector4(40, 40, 40, 40));
            var tecla = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.UiDir + "menu_tecla.png", new Vector4(14, 14, 14, 14));

            var escena = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = Object.FindAnyObjectByType<LevelCompleteScreen>(FindObjectsInactive.Include);
            var root = screen.transform;

            // Lo que puso el equipo: la imagen y el titulo.
            Sprite imagen = null;
            string titulo = TituloPorDefecto;
            var fondoViejo = root.Find("Fondo");
            if (fondoViejo != null && fondoViejo.TryGetComponent<Image>(out var fi))
                imagen = fi.sprite;
            var tituloViejo = root.Find("Titulo");
            if (tituloViejo != null && tituloViejo.TryGetComponent<Text>(out var ti) && !string.IsNullOrWhiteSpace(ti.text))
                titulo = ti.text;

            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            var bg = O.AddImage(root, "Fondo", TinteDeLaImagen, 0, 0, 1920, 1080);
            bg.sprite = imagen;
            bg.raycastTarget = false;

            P.AddPanel(root, panelSprite, PanelY, PanelH, titulo, PanelX, PanelW, 46);

            // "Nivel 1: La salida superado": lo escribe LevelCompleteScreen al abrirse.
            var detalle = O.AddText(root, "Detalle", "Nivel superado", 28, O.RosaClaro,
                FilaX, PanelY + 124f, FilaW, 44f, TextAnchor.MiddleLeft);

            var continuar = P.AddBoton(root, "Continuar", screen.Continue, FilaW);
            O.Place((RectTransform)continuar.transform, FilaX, PanelY + 190f, FilaW, 64f);
            continuar.navigation = new Navigation { mode = Navigation.Mode.None };

            O.AddAyudaMenu(root, "Continuar", PanelX, new[] { "Enter" }, tecla);

            O.Set(screen, "detail", detalle);
            O.Set(screen, "firstSelected", continuar);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("Pantalla de nivel completo reconstruida en " + ScenePath);
        }
    }
}
