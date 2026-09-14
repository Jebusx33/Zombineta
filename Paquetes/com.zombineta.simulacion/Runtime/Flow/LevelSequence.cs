using System;
using System.Collections.Generic;
using UnityEngine;
using Zombineta.Level;

namespace Zombineta.Flow
{
    /// <summary>Un nivel de la campana: su recorrido y lo que cuenta la cinematica de entrada.</summary>
    [Serializable]
    public sealed class LevelInfo
    {
        public string displayName = "Nivel";

        [Tooltip("Placas de la cinematica de entrada, en orden. Placeholder hasta que haya animatica.")]
        [TextArea(1, 3)]
        public string[] introLines = new string[0];

        public LevelDefinition route;
    }

    /// <summary>
    /// Los niveles en orden. Ganar uno lleva a la cinematica del siguiente; ganar el
    /// ultimo lleva al final. Agregar un nivel es agregar una entrada aca.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Level Sequence", fileName = "LevelSequence")]
    public sealed class LevelSequence : ScriptableObject
    {
        public List<LevelInfo> levels = new List<LevelInfo>();
    }
}
