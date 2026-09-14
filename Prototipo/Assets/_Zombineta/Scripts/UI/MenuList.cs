using UnityEngine;
using UnityEngine.UI;

namespace Zombineta.UI
{
    /// <summary>
    /// Una lista vertical de opciones con una seleccionada. Solo presentacion: el input lo
    /// lee ScreenFlow, que es el unico que sabe que teclas existen en cada pantalla.
    /// </summary>
    public sealed class MenuList : MonoBehaviour
    {
        [SerializeField] Text[] items;
        [SerializeField] Color normalColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] Color selectedColor = new Color(1f, 0.78f, 0.3f);

        string[] labels;

        public int Selected { get; private set; }
        public int Count => items != null ? items.Length : 0;

        void Awake() => CacheLabels();

        void OnEnable() => Refresh();

        void CacheLabels()
        {
            if (labels != null || items == null)
                return;
            labels = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
                labels[i] = items[i] != null ? items[i].text : "";
        }

        /// <summary>Mueve la seleccion, dando la vuelta en los extremos.</summary>
        public void Move(int delta)
        {
            if (Count == 0 || delta == 0)
                return;
            Selected = ((Selected + delta) % Count + Count) % Count;
            Refresh();
        }

        public void Select(int index)
        {
            if (Count == 0)
                return;
            Selected = Mathf.Clamp(index, 0, Count - 1);
            Refresh();
        }

        /// <summary>Cambia el texto de una opcion (valores de Opciones, por ejemplo).</summary>
        public void SetLabel(int index, string text)
        {
            CacheLabels();
            if (labels == null || index < 0 || index >= labels.Length)
                return;
            labels[index] = text;
            Refresh();
        }

        void Refresh()
        {
            CacheLabels();
            if (items == null)
                return;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    continue;
                bool on = i == Selected;
                // Marcadores ASCII: la fuente de placeholder no garantiza flechas unicode.
                items[i].text = on ? "> " + labels[i] + " <" : labels[i];
                items[i].color = on ? selectedColor : normalColor;
            }
        }
    }
}
