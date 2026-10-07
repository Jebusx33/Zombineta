using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Level
{
    /// <summary>
    /// Un pedazo del recorrido con su propio ritmo: cuanto aprieta la horda, cuanta luz hay y
    /// que tan cargada esta la calle. Un nivel es una lista de tramos, de la largada al refugio:
    /// asi se arma el arco (presentar, desarrollar, giro, cierre) sin tocar codigo.
    /// </summary>
    [Serializable]
    public sealed class Tramo
    {
        public string nombre = "Tramo";

        [Tooltip("Metros desde la largada donde termina este tramo. Empieza donde termina el anterior.")]
        public float hasta = 1000f;

        [Header("Ritmo")]
        [Tooltip("Multiplica la velocidad de la horda mientras la moto esta en este tramo. " +
                 "1 = la de GameConfig. Menos de 1 es un respiro; mas de 1, un apreton.")]
        [Min(0f)] public float presionHorda = 1f;

        [Tooltip("0 = luz normal del nivel. 1 = apagon: la calle queda a oscuras y solo el faro deja ver.")]
        [Range(0f, 1f)] public float oscuridad;

        [Header("Densidad al generar (multiplica los numeros del generador)")]
        [Tooltip("Obstaculos sueltos. 0 = ninguno.")]
        [Min(0f)] public float obstaculos = 1f;

        [Tooltip("Nafta, bateria y balas. 2 = el doble de seguido.")]
        [Min(0f)] public float recursos = 1f;

        [Tooltip("Rampas, barriles y zombies de frente.")]
        [Min(0f)] public float piezas = 1f;
    }

    /// <summary>
    /// Consultas sobre una lista de tramos. C# plano: lo usan la simulacion (presion), la luz
    /// (oscuridad) y el generador (densidades). Sin tramos todo vale lo de siempre: presion 1,
    /// oscuridad 0, densidades 1.
    /// </summary>
    public static class Tramos
    {
        /// <summary>Indice del tramo que contiene esos metros; -1 si no hay tramos. Pasado el
        /// ultimo "hasta" sigue valiendo el ultimo tramo.</summary>
        public static int IndexAt(IReadOnlyList<Tramo> tramos, float meters)
        {
            if (tramos == null || tramos.Count == 0)
                return -1;

            for (int i = 0; i < tramos.Count; i++)
                if (meters < tramos[i].hasta)
                    return i;
            return tramos.Count - 1;
        }

        public static Tramo At(IReadOnlyList<Tramo> tramos, float meters)
        {
            int i = IndexAt(tramos, meters);
            return i < 0 ? null : tramos[i];
        }

        /// <summary>Donde empieza el tramo i, en metros.</summary>
        public static float Desde(IReadOnlyList<Tramo> tramos, int i) => i <= 0 ? 0f : tramos[i - 1].hasta;

        /// <summary>Presion de la horda a esos metros, con el cambio entre tramos repartido en
        /// "transicion" metros centrados en el limite (sin saltos de velocidad).</summary>
        public static float PresionAt(IReadOnlyList<Tramo> tramos, float meters, float transicion) =>
            Suavizado(tramos, meters, transicion, 1f, t => t.presionHorda);

        /// <summary>Oscuridad (0..1) a esos metros, con el fundido de entrada y salida.</summary>
        public static float OscuridadAt(IReadOnlyList<Tramo> tramos, float meters, float transicion) =>
            Suavizado(tramos, meters, transicion, 0f, t => t.oscuridad);

        static float Suavizado(IReadOnlyList<Tramo> tramos, float meters, float transicion, float sinTramos,
            Func<Tramo, float> valor)
        {
            int i = IndexAt(tramos, meters);
            if (i < 0)
                return sinTramos;

            float propio = valor(tramos[i]);
            float mitad = transicion * 0.5f;
            if (mitad <= 0f)
                return propio;

            // Cerca del limite con el tramo anterior: se viene mezclando desde su valor.
            float desde = Desde(tramos, i);
            if (i > 0 && meters - desde < mitad)
                return Mathf.Lerp(valor(tramos[i - 1]), propio, 0.5f + (meters - desde) / transicion);

            // Cerca del limite con el siguiente: se empieza a mezclar hacia el suyo.
            float hasta = tramos[i].hasta;
            if (i < tramos.Count - 1 && hasta - meters < mitad)
                return Mathf.Lerp(propio, valor(tramos[i + 1]), 0.5f - (hasta - meters) / transicion);

            return propio;
        }
    }
}
