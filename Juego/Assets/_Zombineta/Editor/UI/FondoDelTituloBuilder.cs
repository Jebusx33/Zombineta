using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Juego.Flow;
using Zombineta.Juego.Screens;

namespace Zombineta.Juego.EditorTools
{
    /// <summary>
    /// Pone una imagen de fondo detras del titulo de la cinematica de entrada ("Nivel 1: La
    /// salida"), antes de que empiecen las vinetas. La imagen se funde junto con el titulo: el
    /// titulo pasa a vivir en un grupo propio ("Titulo") que lleva la imagen, un velo suave y el
    /// texto, y CinematicPlayer lo sigue manejando igual. Las vinetas se ven sobre el fondo negro
    /// de siempre. Se puede volver a correr: solo cambia la imagen si ya esta armado.
    /// </summary>
    public static class FondoDelTituloBuilder
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/" + SceneNames.Cinematic + ".unity";
        // El fondo de los creditos: la misma ciudad destruida al atardecer.
        const string ImagenPath = "Assets/_Zombineta/Art/Creditos/creditos_fondo.jpg";
        const string Contenedor = "Titulo";
        const string NombreImagen = "Fondo del titulo";

        [MenuItem("Zombineta/UI/Fondo del titulo de la cinematica")]
        public static void Aplicar()
        {
            var imagen = AssetDatabase.LoadAssetAtPath<Sprite>(ImagenPath);
            if (imagen == null)
            {
                Debug.LogError("No se encontro la imagen de fondo: " + ImagenPath);
                return;
            }

            var escena = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = Object.FindAnyObjectByType<CinematicPlayer>(FindObjectsInactive.Include);
            var so = new SerializedObject(player);
            var texto = (Text)so.FindProperty("title").objectReferenceValue;
            var grupoViejo = (CanvasGroup)so.FindProperty("titleGroup").objectReferenceValue;
            var padre = texto.transform.parent;

            // Ya armado: solo se actualiza la imagen.
            if (padre.name == Contenedor && padre.Find(NombreImagen) != null)
            {
                padre.Find(NombreImagen).GetComponent<Image>().sprite = imagen;
                Guardar(escena);
                return;
            }

            var go = new GameObject(Contenedor, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(padre, false);
            go.transform.SetSiblingIndex(texto.transform.GetSiblingIndex());
            Estirar((RectTransform)go.transform);

            var grupo = go.GetComponent<CanvasGroup>();
            grupo.alpha = 0f; // CinematicPlayer lo funde al empezar
            grupo.interactable = false;
            grupo.blocksRaycasts = false;

            var fondo = Capa(go.transform, NombreImagen, Color.white);
            fondo.sprite = imagen;
            // Velo suave para que el titulo se lea sobre cualquier imagen.
            Capa(go.transform, "Velo", new Color(0f, 0f, 0f, 0.3f));

            // El texto pasa adentro del grupo, delante de la imagen.
            texto.transform.SetParent(go.transform, false);
            texto.transform.SetAsLastSibling();
            if (texto.GetComponent<Outline>() == null)
            {
                var borde = texto.gameObject.AddComponent<Outline>();
                borde.effectColor = new Color(0.1f, 0.05f, 0.12f, 1f);
                borde.effectDistance = new Vector2(2f, -2f);
            }

            // El grupo viejo estaba en el propio texto: ya no se usa.
            if (grupoViejo != null && grupoViejo.gameObject == texto.gameObject)
                Object.DestroyImmediate(grupoViejo);

            so.FindProperty("titleGroup").objectReferenceValue = grupo;
            so.ApplyModifiedPropertiesWithoutUndo();
            Guardar(escena);
        }

        static Image Capa(Transform padre, string nombre, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            Estirar((RectTransform)go.transform);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Guardar(UnityEngine.SceneManagement.Scene escena)
        {
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("Fondo del titulo de la cinematica aplicado en " + ScenePath);
        }
    }
}
