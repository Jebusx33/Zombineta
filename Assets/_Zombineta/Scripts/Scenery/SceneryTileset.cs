using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Scenery
{
    /// <summary>Un tile posible dentro de una capa.</summary>
    [Serializable]
    public sealed class SceneryVariant
    {
        public Sprite sprite;

        [Tooltip("Probabilidad relativa. 0 = no se usa (sirve para apagar un tile sin borrarlo).")]
        [Min(0f)] public float weight = 1f;

        [Tooltip("Multiplica la altura de la capa solo para este tile (una tienda mas baja, un arbol mas alto).")]
        [Min(0.01f)] public float heightScale = 1f;

        [Tooltip("Corrimiento vertical propio, en unidades de mundo.")]
        public float yOffset;
    }

    /// <summary>Una capa del escenario: que tiles usa, a que profundidad y como se reparten.</summary>
    [Serializable]
    public sealed class SceneryLayer
    {
        public string name = "Capa";
        public bool enabled = true;

        [Header("Profundidad")]
        [Tooltip("0 = fijo a la camara (cielo). 1 = pegado al mundo (calle). " +
                 "Entre 0 y 1 = fondo, mas lento. Mayor a 1 = frente, mas rapido.")]
        [Min(0f)] public float parallax = 1f;

        [Tooltip("Orden de dibujo. La moto esta en 10: el fondo va abajo, el frente arriba.")]
        public int sortingOrder;

        [Header("Ubicacion y tamano")]
        [Tooltip("Altura en mundo de la base de los tiles.")]
        public float baselineY;

        [Tooltip("Altura en mundo de un tile con heightScale 1. El ancho sale de la proporcion del sprite.")]
        [Min(0.01f)] public float height = 4f;

        [Header("Reparto")]
        [Tooltip("Probabilidad de dejar un hueco despues de cada tile. 0 = pared continua.")]
        [Range(0f, 1f)] public float gapChance;

        [Tooltip("Hueco minimo, en unidades de la capa.")]
        [Min(0f)] public float gapMin = 2f;

        [Tooltip("Hueco maximo, en unidades de la capa.")]
        [Min(0f)] public float gapMax = 6f;

        [Tooltip("Evita el mismo tile dos veces seguidas.")]
        public bool noImmediateRepeat = true;

        [Tooltip("Misma semilla, mismo escenario. Cambiarla en Play Mode rearma la capa al instante.")]
        public int seed = 1;

        [Header("Look")]
        public Color tint = Color.white;

        [Tooltip("Vacio = el material por defecto (iluminado por las luces 2D). " +
                 "Para el cielo conviene uno sin iluminacion.")]
        public Material material;

        public List<SceneryVariant> variants = new List<SceneryVariant>();
    }

    /// <summary>
    /// El escenario completo como datos. Editarlo en el Inspector durante Play Mode
    /// reconstruye la escena en el acto: asi se arma y se ajusta mirando el resultado.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Scenery Tileset", fileName = "SceneryTileset")]
    public sealed class SceneryTileset : ScriptableObject
    {
        public List<SceneryLayer> layers = new List<SceneryLayer>();

        [NonSerialized] int version;

        /// <summary>Cambia cada vez que se edita el asset. El manager lo vigila para reconstruir.</summary>
        public int Version => version;

        void OnValidate() => version++;
    }
}
