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
        public Color colorGeneral = Color.white;

        [Min(0f)] public float intensidadGeneral;

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
    }
}
