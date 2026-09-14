using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Enemies
{
    /// <summary>
    /// Un aspecto concreto que puede tomar un zombie: sus cuadros de caminata, impacto y muerte,
    /// mas como se anima. El arte diferencia a cada uno; el codigo solo elige y reproduce.
    /// </summary>
    [Serializable]
    public sealed class ZombieLook
    {
        public string name;
        public Sprite[] walk;
        public Sprite[] hit;
        public Sprite[] death;
        public float walkFps = 8f;

        [Tooltip("Compensa la resolucion de la hoja de origen, no el tamano de juego: las hojas completas " +
                 "(Anciano, HombreRojo) van en 1 y las de Zombies_poses en 2,5 porque sus figuras salen " +
                 "~2,3 veces mas chicas. El tamano en pantalla lo dan HordeView.bodyScale y la escala del tipo. " +
                 "La sombra del zombie descuenta este valor.")]
        public float scale = 1f;
        public bool poseBob;
    }

    /// <summary>Los looks disponibles para un tipo de zombie (Comun, Corredor, Pesado, ...).</summary>
    [Serializable]
    public sealed class TypeLooks
    {
        public string typeName;
        public List<ZombieLook> looks = new List<ZombieLook>();
    }

    /// <summary>
    /// Arquetipos visuales por tipo de zombie: reemplaza el tinte unico por variedad de sprites.
    /// Se balancea desde Settings/ZombieLooks.asset, nunca desde codigo.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Zombie Look Set", fileName = "ZombieLooks")]
    public sealed class ZombieLookSet : ScriptableObject
    {
        public List<TypeLooks> types = new List<TypeLooks>();

        static readonly List<ZombieLook> Empty = new List<ZombieLook>();

        /// <summary>Los looks del tipo pedido, o una lista vacia si no tiene ninguno.</summary>
        public IReadOnlyList<ZombieLook> For(int typeIndex)
        {
            if (types == null || typeIndex < 0 || typeIndex >= types.Count)
                return Empty;
            var t = types[typeIndex];
            return t != null && t.looks != null ? t.looks : Empty;
        }
    }
}
