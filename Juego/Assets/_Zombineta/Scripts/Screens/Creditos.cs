using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Una seccion de los creditos: un titulo y las lineas debajo (puede no tener). Una linea
    /// "Nombre | Rol" se arma en dos columnas; una linea vacia deja un espacio. Con varias
    /// columnas las lineas se reparten de arriba abajo y despues a la derecha, como una lista
    /// larga de nombres.
    /// </summary>
    [Serializable]
    public sealed class Seccion
    {
        public string titulo = "";
        [Tooltip("Titulo grande, sin subrayado: el nombre del juego al principio, el saludo al final.")]
        public bool portada;
        [Tooltip("En una portada, dibujo que reemplaza al titulo (el logo del juego). En una seccion comun, " +
                 "logo chico debajo del titulo (el de un sitio de recursos). Vacio: solo el titulo.")]
        public Sprite imagen;
        [Tooltip("Columnas en que se reparten las lineas. 1 (o 0): una por renglon, con \"Nombre | Rol\".")]
        [Min(0)] public int columnas = 1;
        public string[] lineas = new string[0];
    }

    /// <summary>
    /// Lo que se ve en la pantalla de creditos: las secciones en orden, que el equipo edita,
    /// mas la velocidad del rodado y cuanto se espera al final antes de volver solo al menu.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Creditos", fileName = "Creditos")]
    public sealed class Creditos : ScriptableObject
    {
        public List<Seccion> secciones = new List<Seccion>();

        [Tooltip("Pixeles por segundo que sube el texto (tiempo real, no de juego).")]
        public float velocidad = 60f;

        [Tooltip("Segundos de espera despues de que la ultima linea pasa el borde de arriba.")]
        public float pausaFinal = 2f;
    }
}
