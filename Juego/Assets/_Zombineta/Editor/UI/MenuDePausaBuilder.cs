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
    /// Arma el menu de pausa con el estilo del menu de opciones: el mismo panel oscuro translucido
    /// con borde rosa, titulo con subrayado, filas con resaltado rosa y la ayuda de teclas abajo.
    /// Sin imagen de fondo: solo un velo oscuro, porque se ve el juego detras. Incluye el panel
    /// "Controles" (PanelDeControles), que lista los controles del teclado o del joystick segun lo
    /// ultimo que se uso. Reusa la escena Pause: conserva el Canvas con su PauseScreen y
    /// UiSonidos, y rehace lo que cuelga de el. Se puede volver a correr.
    /// </summary>
    public static class MenuDePausaBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.Pause + ".unity";

        // Medidas en el Canvas de 1920x1080, x/y desde arriba a la izquierda. Los paneles van al centro.
        internal const float PanelW = 900f;
        internal const float PanelX = (1920f - PanelW) / 2f;
        internal const float FilaX = PanelX + 60f, FilaW = PanelW - 120f, FilaH = 64f, Separacion = 8f;

        // Pausa: seis filas.
        const float PausaY = 230f, PausaH = 620f, ListaY = PausaY + 140f;
        // Controles: mas alto, por la cantidad de lineas.
        const float ControlesY = 70f, ControlesH = 930f;

        static readonly Color Linea = new Color32(214, 122, 180, 255);

        [MenuItem("Zombineta/UI/Reconstruir menu de pausa")]
        static void Rebuild()
        {
            if (EditorUtility.DisplayDialog("Menu de pausa",
                    "Esto rehace el contenido del Canvas de la pausa (paneles, botones, controles) " +
                    "y pisa los cambios hechos a mano en el.",
                    "Reconstruir", "Cancelar"))
                Build();
        }

        public static void Build()
        {
            var panelSprite = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.UiDir + "menu_panel.png", new Vector4(40, 40, 40, 40));
            var tecla = MenuPrincipalBuilder.Importar(MenuPrincipalBuilder.UiDir + "menu_tecla.png", new Vector4(14, 14, 14, 14));
            var desvanecido = O.Degradado("opciones_desvanecido.png",
                x => new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, x))));

            var escena = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = Object.FindAnyObjectByType<PauseScreen>(FindObjectsInactive.Include);
            var root = screen.transform;

            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            // El juego se ve detras: un velo oscuro, sin imagen.
            O.AddImage(root, "Velo", new Color(0f, 0f, 0f, 0.65f), 0, 0, 1920, 1080);

            // --- Pausa --------------------------------------------------------------------
            var principal = Contenedor(root, "Pausa");
            AddPanel(principal, panelSprite, PausaY, PausaH, "PAUSA");

            // Las filas van en un layout vertical: "Saltear tutorial" se oculta fuera del tutorial
            // (lo hace PauseScreen) y las demas suben a ocupar su lugar.
            var listaGo = new GameObject("Botones", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listaGo.transform.SetParent(principal, false);
            O.Place((RectTransform)listaGo.transform, FilaX, ListaY, FilaW, 6 * FilaH + 5 * Separacion);
            var layout = listaGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = Separacion;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var seguir = AddBoton(listaGo.transform, "Seguir", screen.Resume);
            var reintentar = AddBoton(listaGo.transform, "Reintentar", screen.Retry);
            var saltear = AddBoton(listaGo.transform, "Saltear tutorial", screen.SkipTutorial);
            var opciones = AddBoton(listaGo.transform, "Opciones", screen.OpenOptions);
            var controles = AddBoton(listaGo.transform, "Controles", screen.AbrirControles);
            var salir = AddBoton(listaGo.transform, "Salir al menú", screen.ToMenu);
            Chain(new[] { seguir, reintentar, saltear, opciones, controles, salir });

            // Ayuda de teclas, abajo como en Opciones. "Continuar" tambien se puede clickear.
            O.AddAyudaMenu(principal, "Navegar", 560f, new[] { "↑", "↓" }, tecla);
            O.AddAyudaMenu(principal, "Seleccionar", 860f, new[] { "Enter" }, tecla);
            var continuar = O.AddAyudaMenu(principal, "Continuar", 1150f, new[] { "Esc" }, tecla);
            var boton = continuar.gameObject.AddComponent<Button>();
            boton.targetGraphic = continuar;
            boton.transition = Selectable.Transition.None;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(boton.onClick, screen.Resume);

            // --- Controles ----------------------------------------------------------------
            var panelControles = Contenedor(root, "Controles");
            AddPanel(panelControles, panelSprite, ControlesY, ControlesH, "CONTROLES");

            var dispositivo = O.AddText(panelControles, "Dispositivo", "", 22, O.RosaClaro,
                FilaX, ControlesY + 120f, FilaW, 36f, TextAnchor.MiddleLeft);

            var entradas = new System.Collections.Generic.List<(Text texto, string accion)>();
            float y = ControlesY + 195f;

            Encabezado(panelControles, "EN MOTO", y, desvanecido);
            y += 40f;
            foreach (var (etiqueta, accion) in new[]
                     {
                         ("Cambiar de carril", "Carril"), ("Turbo", "Turbo"), ("Retroceso", "Reverse"),
                         ("Disparar", "Fire"), ("Granada", "Granada"), ("Faro", "Headlight"),
                     })
            {
                entradas.Add((FilaDeControl(panelControles, etiqueta, y), accion));
                y += 38f;
            }
            O.AddText(panelControles, "Nota", "En el aire, turbo y retroceso inclinan la moto.", 20, O.Ayuda,
                FilaX + 20f, y - 14f, FilaW - 20f, 30f, TextAnchor.MiddleLeft);
            y += 45f;

            Encabezado(panelControles, "BEAT 'EM UP", y, desvanecido);
            y += 40f;
            foreach (var (etiqueta, accion) in new[]
                     {
                         ("Moverse", "Mover"), ("Atacar", "Golpe"), ("Disparar", "FirePie"),
                         ("Granada", "GranadaPie"), ("Bomba", "Bomba"),
                     })
            {
                entradas.Add((FilaDeControl(panelControles, etiqueta, y), accion));
                y += 38f;
            }
            y += 8f;

            Encabezado(panelControles, "GENERAL", y, desvanecido);
            y += 40f;
            entradas.Add((FilaDeControl(panelControles, "Pausa", y), "Pausa"));

            var volver = AddBoton(panelControles, "Volver", screen.CerrarControles);
            O.Place((RectTransform)volver.transform, FilaX, ControlesY + ControlesH - FilaH - 41f, FilaW, FilaH);
            volver.navigation = new Navigation { mode = Navigation.Mode.None };

            var panel = panelControles.gameObject.AddComponent<PanelDeControles>();
            var so = new SerializedObject(panel);
            so.FindProperty("dispositivo").objectReferenceValue = dispositivo;
            var lista = so.FindProperty("entradas");
            lista.arraySize = entradas.Count;
            for (int i = 0; i < entradas.Count; i++)
            {
                var e = lista.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("texto").objectReferenceValue = entradas[i].texto;
                e.FindPropertyRelative("accion").stringValue = entradas[i].accion;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            panelControles.gameObject.SetActive(false);

            O.Set(screen, "firstSelected", seguir);
            O.Set(screen, "skipTutorialButton", saltear);
            O.Set(screen, "retryButton", reintentar);
            O.Set(screen, "optionsButtonNav", opciones);
            O.Set(screen, "panelPrincipal", principal.gameObject);
            O.Set(screen, "panelControles", panelControles.gameObject);
            O.Set(screen, "controlesButton", controles);
            O.Set(screen, "volverControlesButton", volver);
            // pauseAction ya viene en la escena; si no, la busca.
            if (new SerializedObject(screen).FindProperty("pauseAction").objectReferenceValue == null)
                O.Set(screen, "pauseAction", O.FindAction("Player/Pause"));

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("Menu de pausa reconstruido en " + ScenePath);
        }

        /// <summary>Un grupo que ocupa toda la pantalla, para prender y apagar un panel entero.</summary>
        static Transform Contenedor(Transform padre, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Panel translucido con su titulo y el subrayado rosa, como en Opciones.</summary>
        internal static void AddPanel(Transform padre, Sprite sprite, float y, float alto, string titulo,
                                      float x = PanelX, float ancho = PanelW, int tamanoTitulo = 56)
        {
            var panel = O.AddImage(padre, "Panel", Color.white, x, y, ancho, alto);
            panel.sprite = sprite;
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = false;

            var texto = O.AddText(padre, "Titulo", titulo, tamanoTitulo, Color.white, x + 60f, y + 25f, ancho - 120f, 70, TextAnchor.MiddleLeft);
            texto.fontStyle = FontStyle.Bold;
            var borde = texto.gameObject.AddComponent<Outline>();
            borde.effectColor = new Color(0.1f, 0.05f, 0.12f, 1f);
            borde.effectDistance = new Vector2(2f, -2f);
            O.AddImage(padre, "Subrayado", O.Rosa, x + 62f, y + 101f, 126, 8);
        }

        /// <summary>Titulo de seccion con su linea que se desvanece, igual al de Opciones.</summary>
        static void Encabezado(Transform padre, string titulo, float yCentro, Sprite desvanecido)
        {
            O.AddText(padre, "Encabezado " + titulo, titulo, 26, O.RosaClaro, FilaX, yCentro - 20f, 300f, 40f, TextAnchor.MiddleLeft);
            float desde = FilaX + titulo.Length * 16f + 60f;
            var linea = O.AddImage(padre, "Linea " + titulo, Linea, desde, yCentro, FilaX + FilaW - desde, 3f);
            linea.sprite = desvanecido;
            linea.raycastTarget = false;
        }

        /// <summary>Una linea de controles: la accion a la izquierda y, a la derecha, su tecla (la pinta PanelDeControles).</summary>
        static Text FilaDeControl(Transform padre, string etiqueta, float yCentro)
        {
            O.AddText(padre, "Accion " + etiqueta, etiqueta, 26, Color.white, FilaX + 20f, yCentro - 18f, 380f, 36f, TextAnchor.MiddleLeft);
            var valor = O.AddText(padre, "Tecla " + etiqueta, "-", 26, O.RosaClaro, FilaX + 440f, yCentro - 18f, 340f, 36f, TextAnchor.MiddleLeft);
            valor.fontStyle = FontStyle.Bold;
            return valor;
        }

        /// <summary>Fila de la pausa: resaltado rosa y etiqueta que se aclara al seleccionarla (OpcionFila).</summary>
        internal static Button AddBoton(Transform lista, string texto, UnityEngine.Events.UnityAction accion, float ancho = FilaW)
        {
            var hit = O.AddImage(lista, "Boton " + texto, Color.clear, 0, 0, ancho, FilaH);

            var resaltado = O.AddImage(hit.transform, "Resaltado", O.Resaltado, 0, 0, ancho, FilaH);
            resaltado.sprite = O.BuiltinSprite("UI/Skin/UISprite.psd");
            resaltado.type = Image.Type.Sliced;
            resaltado.raycastTarget = false;
            resaltado.enabled = false;

            var etiqueta = O.AddText(hit.transform, "Etiqueta", texto, 34, Color.white, 30f, 0, ancho - 60f, FilaH, TextAnchor.MiddleLeft);

            var boton = hit.gameObject.AddComponent<Button>();
            boton.targetGraphic = hit;
            boton.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(boton.onClick, accion);

            var fila = hit.gameObject.AddComponent<OpcionFila>();
            O.Set(fila, "control", boton);
            O.Set(fila, "resaltado", resaltado);
            O.Set(fila, "etiqueta", etiqueta);
            var so = new SerializedObject(fila);
            so.FindProperty("etiquetaSeleccionada").colorValue = O.RosaClaro;
            so.ApplyModifiedPropertiesWithoutUndo();
            return boton;
        }

        /// <summary>Navegacion explicita arriba/abajo, dando la vuelta.</summary>
        internal static void Chain(Button[] items)
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
    }
}
