using UnityEngine;
using Zombineta.Level;

namespace Zombineta.Juego.Levels
{
    /// <summary>
    /// Un objeto del recorrido ubicado en la escena: que es, en que carril esta y a cuantos
    /// metros (su X en el mundo). La simulacion no lo ve directamente: LevelScene junta todos los
    /// items al arrancar y arma el recorrido.
    ///
    /// Recuerda con que valores lo dejo el generador. Si se lo movio o edito despues, queda
    /// fijado: regenerar no lo toca.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelItem : MonoBehaviour
    {
        public LevelEntryKind kind = LevelEntryKind.Obstacle;

        [Range(0, 2)]
        [Tooltip("0 = carril de abajo, 1 = medio, 2 = arriba.")]
        public int lane = 1;

        [Tooltip("En un zombie de frente, el indice de su tipo en Zombies.asset.")]
        public int variant;

        [Min(0f)]
        [Tooltip("Metros sobre el carril. Mayor a 0: solo se agarra saltando.")]
        public float height;

        [Header("Generador")]
        [Tooltip("Lo ubico el generador.")]
        public bool generated;

        [Tooltip("Fijado a mano: regenerar no lo toca aunque no se haya movido.")]
        public bool pinned;

        [HideInInspector] public float generatedMeters;
        [HideInInspector] public int generatedLane;
        [HideInInspector] public LevelEntryKind generatedKind;
        [HideInInspector] public int generatedVariant;
        [HideInInspector] public float generatedHeight;

        // Como lo dejo acomodado la escena la ultima vez: asi se sabe si se lo arrastro (manda la
        // posicion) o se le cambio el carril en el inspector (manda el carril). Sin guardar: al
        // recargar, manda el carril.
        [System.NonSerialized] public bool hasSnapshot;
        [System.NonSerialized] public Vector2 snapshotPosition;
        [System.NonSerialized] public int snapshotLane;
        [System.NonSerialized] public float snapshotHeight;

        public float Meters(LevelLayout layout) => layout.ToMeters(transform.position.x);

        public LevelEntry ToEntry(LevelLayout layout) =>
            new LevelEntry(Meters(layout), lane, kind, height, variant);

        /// <summary>Un item generado que cambio desde que lo ubico el generador.</summary>
        public bool IsTouchedSinceGeneration(LevelLayout layout) =>
            generated &&
            (Mathf.Abs(Meters(layout) - generatedMeters) > 0.05f ||
             lane != generatedLane ||
             kind != generatedKind ||
             variant != generatedVariant ||
             Mathf.Abs(height - generatedHeight) > 0.001f);

        /// <summary>Lo que regenerar puede reemplazar: generado, sin fijar y sin tocar.</summary>
        public bool IsReplaceable(LevelLayout layout) =>
            generated && !pinned && !IsTouchedSinceGeneration(layout);

        /// <summary>Queda como si lo acabara de ubicar el generador: sin fijar y reemplazable.</summary>
        public void MarkGenerated(LevelLayout layout)
        {
            generated = true;
            pinned = false;
            generatedMeters = Meters(layout);
            generatedLane = lane;
            generatedKind = kind;
            generatedVariant = variant;
            generatedHeight = height;
        }
    }
}
