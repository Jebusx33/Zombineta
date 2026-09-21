using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>Mapea una clave a su Sonido. Puede ser el banco global o el de un nivel.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Audio/Banco de sonidos", fileName = "BancoDeSonidos")]
    public sealed class BancoDeSonidos : ScriptableObject
    {
        [Serializable]
        public struct Entrada
        {
            public SonidoClave clave;
            public Sonido sonido;
        }

        public List<Entrada> entradas = new List<Entrada>();

        public Sonido Buscar(SonidoClave clave)
        {
            for (int i = 0; i < entradas.Count; i++)
                if (entradas[i].clave == clave)
                    return entradas[i].sonido;
            return null;
        }

        /// <summary>Resuelve una clave: el banco de nivel pisa al global; lo que el nivel no define cae al global; si ninguno la tiene, null.</summary>
        public static Sonido Resolver(BancoDeSonidos nivel, BancoDeSonidos global, SonidoClave clave)
        {
            Sonido deNivel = nivel != null ? nivel.Buscar(clave) : null;
            if (deNivel != null)
                return deNivel;
            return global != null ? global.Buscar(clave) : null;
        }
    }
}
