using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>
    /// El ambiente de un nivel: cuanta luz general recibe cada capa de dibujo. Es lo que define si
    /// el nivel es noche cerrada o atardecer, y arte lo edita sin tocar la escena.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Perfil de luz", fileName = "PerfilDeLuz")]
    public sealed class PerfilDeLuz : ScriptableObject
    {
        [Serializable]
        public sealed class Ambiente
        {
            [Tooltip("Nombre de la Sorting Layer: Cielo, Fondo, Calle, Juego o Frente.")]
            public string capa = "Fondo";

            public Color color = Color.white;

            [Min(0f)] public float intensidad = 1f;
        }

        public List<Ambiente> capas = new List<Ambiente>();

        [Header("Luz general (todas las capas)")]
        [Tooltip("Se suma al color de CADA capa (color*intensidad de la capa + colorGeneral*intensidadGeneral), " +
                 "no crea una luz propia: URP 2D solo permite una luz global por capa y estilo de mezcla, asi " +
                 "que una luz 'General' aparte se ignora. Si una capa no tiene entrada propia pero General " +
                 "aporta algo, igual se crea su luz con solo esa contribucion.")]
        public Color colorGeneral = Color.white;

        [Min(0f)] public float intensidadGeneral;

        [NonSerialized] int version;

        /// <summary>Cambia cada vez que se edita el asset. LightingDirector lo vigila para reconstruir.</summary>
        public int Version => version;

        void OnValidate() => version++;

        public bool TryGet(string capa, out Color color, out float intensidad)
        {
            foreach (var a in capas)
                if (a != null && a.capa == capa)
                {
                    color = a.color;
                    intensidad = a.intensidad;
                    return true;
                }

            color = Color.black;
            intensidad = 0f;
            return false;
        }

        /// <summary>Una capa del juego con su color final ya resuelto (propio + General), lista para
        /// una Light2D con intensity = 1.</summary>
        public readonly struct CapaResuelta
        {
            public readonly string capa;
            public readonly Color color;

            public CapaResuelta(string capa, Color color)
            {
                this.capa = capa;
                this.color = color;
            }
        }

        /// <summary>
        /// Funcion pura: para cada nombre de capa de 'capasDelJuego', combina su color*intensidad
        /// propios (si tiene entrada en 'capas') con colorGeneral*intensidadGeneral. Una capa sin
        /// entrada propia solo aparece en el resultado si General aporta algo (intensidadGeneral > 0);
        /// asi una capa completamente sin luz (ni propia ni General) no genera ninguna Light2D. El
        /// alfa se fuerza a 1 en cada paso: es el color de una luz, no una transparencia.
        /// </summary>
        public List<CapaResuelta> Resolver(IEnumerable<string> capasDelJuego)
        {
            var resultado = new List<CapaResuelta>();
            var general = colorGeneral * intensidadGeneral;
            general.a = 1f;
            bool generalAporta = intensidadGeneral > 0f;

            foreach (var nombre in capasDelJuego)
            {
                bool tienePropia = TryGet(nombre, out var colorPropio, out var intensidadPropia);
                if (!tienePropia && !generalAporta)
                    continue;

                var propio = tienePropia ? colorPropio * intensidadPropia : Color.black;
                propio.a = 1f;

                var final = propio + general;
                final.a = 1f;
                resultado.Add(new CapaResuelta(nombre, final));
            }

            return resultado;
        }
    }
}
