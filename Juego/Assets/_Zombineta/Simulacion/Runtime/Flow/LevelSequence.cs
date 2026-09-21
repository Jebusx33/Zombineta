using System;
using System.Collections.Generic;
using UnityEngine;
using Zombineta.Level;

namespace Zombineta.Flow
{
    /// <summary>Una vineta de la cinematica: la imagen y cuanto dura en pantalla.</summary>
    [Serializable]
    public sealed class ComicPanel
    {
        public Sprite image;

        [Min(0.5f)]
        [Tooltip("Segundos en pantalla si nadie la pasa antes.")]
        public float seconds = 3f;

        [Tooltip("Si esta prendido, la pagina anterior se va y esta vineta arranca una limpia. " +
                 "Apagado, se suma encima de las que ya estan. La primera siempre abre pagina.")]
        public bool nuevaPagina = true;
    }

    /// <summary>Un nivel de la campana: su recorrido y lo que cuenta la cinematica de entrada.</summary>
    [Serializable]
    public sealed class LevelInfo
    {
        public string displayName = "Nivel";

        [Tooltip("Placas de la cinematica de entrada, en orden. Placeholder hasta que haya animatica.")]
        [TextArea(1, 3)]
        public string[] introLines = new string[0];

        public LevelDefinition route;

        [Tooltip("Escena del nivel en el juego definitivo. Tiene que estar en Build Settings.")]
        public string sceneName = "";

        [Tooltip("Vinetas de la cinematica de entrada, en orden. El titulo es displayName.")]
        public List<ComicPanel> comicPanels = new List<ComicPanel>();

        // Tipado como ScriptableObject: LevelInfo vive en Zombineta.Simulacion, que no ve
        // Zombineta.Juego (donde esta AudioDeNivel). El juego lo castea. Aditivo, sin
        // arrastrar dependencias nuevas a la simulacion.
        [Tooltip("un AudioDeNivel")]
        public ScriptableObject audio;
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
