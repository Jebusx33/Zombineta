using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Juego.Screens
{
    /// <summary>Una seccion de los creditos: un titulo y las lineas debajo (puede no tener).</summary>
    [Serializable]
    public sealed class Seccion
    {
        public string titulo = "";
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
