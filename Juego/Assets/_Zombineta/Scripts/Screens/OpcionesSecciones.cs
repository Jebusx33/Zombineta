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
            public Graphic marca;
            public Selectable[] controles;
        }

        [SerializeField] Seccion[] secciones;
        [SerializeField] float anchoMarcaActiva = 16f;
        [SerializeField] float anchoMarca = 10f;

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
                if (s.marca == null)
                    continue;
                bool activa = actual != null && s.controles != null &&
                              Array.Exists(s.controles, c => c != null && c.gameObject == actual);
                var rt = s.marca.rectTransform;
                rt.sizeDelta = new Vector2(activa ? anchoMarcaActiva : anchoMarca, rt.sizeDelta.y);
            }
        }
    }
}
