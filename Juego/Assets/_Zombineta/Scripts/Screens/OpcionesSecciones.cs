using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Pestanas de la izquierda del menu de opciones (Sonido, Pantalla, Jugabilidad): con un
    /// click llevan la seleccion a la primera fila de su seccion, y marcan la seccion en la que
    /// esta la fila seleccionada.
    /// </summary>
    public sealed class OpcionesSecciones : MonoBehaviour
    {
        [Serializable]
        public sealed class Seccion
        {
            public Button pestana;
            [Tooltip("Opcional: se enciende solo en la seccion activa (la tira clara de la pestana).")]
            public GameObject resaltado;
            [Tooltip("Opcional: el texto de la pestana; cambia de color en la seccion activa.")]
            public Text etiqueta;
            [Tooltip("Opcional: marca que crece en la seccion activa.")]
            public Graphic marca;
            public Selectable[] controles;
        }

        [SerializeField] Seccion[] secciones;
        [SerializeField] float anchoMarcaActiva = 16f;
        [SerializeField] float anchoMarca = 10f;
        [SerializeField] Color etiquetaNormal = Color.white;
        [SerializeField] Color etiquetaActiva = new Color(0.12f, 0.08f, 0.18f, 1f);

        void Awake()
        {
            foreach (var s in secciones)
            {
                var seccion = s;
                seccion.pestana?.onClick.AddListener(() => Ir(seccion));
            }
        }

        static void Ir(Seccion seccion)
        {
            var es = EventSystem.current;
            if (es != null && seccion.controles != null && seccion.controles.Length > 0 && seccion.controles[0] != null)
                es.SetSelectedGameObject(seccion.controles[0].gameObject);
        }

        void LateUpdate()
        {
            var es = EventSystem.current;
            var actual = es != null ? es.currentSelectedGameObject : null;

            foreach (var s in secciones)
            {
                bool activa = actual != null && s.controles != null &&
                              Array.Exists(s.controles, c => c != null && c.gameObject == actual);
                if (s.resaltado != null)
                    s.resaltado.SetActive(activa);
                if (s.etiqueta != null)
                    s.etiqueta.color = activa ? etiquetaActiva : etiquetaNormal;
                if (s.marca == null)
                    continue;
                var rt = s.marca.rectTransform;
                rt.sizeDelta = new Vector2(activa ? anchoMarcaActiva : anchoMarca, rt.sizeDelta.y);
            }
        }
    }
}
