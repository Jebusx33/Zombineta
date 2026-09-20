using System;
using System.Collections.Generic;
using UnityEngine;
using Zombineta.Level;

namespace Zombineta.Juego.Levels
{
    /// <summary>Como se ve cada tipo de objeto del recorrido. Arte lo cambia sin tocar codigo.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Level Item Palette", fileName = "LevelItemPalette")]
    public sealed class LevelItemPalette : ScriptableObject
    {
        [Serializable]
        public sealed class Look
        {
            public LevelEntryKind kind;
            public Sprite sprite;
            public Color color = Color.white;
            public Vector2 scale = Vector2.one;

            [Tooltip("Ya no decide el orden de dibujo (eso lo da el carril, ver LaneSorting). " +
                     "Se conserva para no romper assets existentes.")]
            public int sortingOrder = 6;

            [Tooltip("Ancho de la sombra en el piso, relativo al ancho de sombra.png (1 = igual).")]
            public float shadowWidth = 1f;

            [Tooltip("Prefab con la vista de este tipo (sprite propio, material lit, y opcional " +
                     "ShadowCaster2D/Light2D). Si esta asignado, LevelScene lo instancia como hijo " +
                     "del item y no pinta sprite/color de aca: asi arte le da luz y sombra propias " +
                     "sin tocar codigo. Sin asignar, todo sigue como antes (sprite/color de arriba).")]
            public GameObject prefab;
        }

        public List<Look> looks = new List<Look>();

        public Look Get(LevelEntryKind kind)
        {
            foreach (var look in looks)
                if (look.kind == kind)
                    return look;
            return null;
        }
    }
}
