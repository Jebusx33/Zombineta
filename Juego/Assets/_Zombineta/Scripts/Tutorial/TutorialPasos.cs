using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Tutorial
{
    /// <summary>Que hay que hacer para que un paso del tutorial se de por cumplido.</summary>
    public enum CondicionPaso
    {
        TurboSostenido,
        ReversaSostenida,
        CambioCarrilAmbos,
        Salto,
        PickupNafta,
        PickupBateria,
        PickupMunicion,
        FaroSostenido,
        DisparoAcertado,
        LlegarMeta,
    }

    /// <summary>Que barra del HUD resalta el paso, si alguna.</summary>
    public enum BarraHud
    {
        Ninguna,
        Nafta,
        Bateria,
        Municion,
        Amenaza,
    }

    /// <summary>Un cartel del tutorial y la condicion que lo cierra.</summary>
    [Serializable]
    public class Paso
    {
        [TextArea] public string texto;
        public CondicionPaso condicion;

        /// <summary>Segundos o veces, segun la condicion. CambioCarrilAmbos y LlegarMeta la ignoran.</summary>
        public float cantidad;
        public BarraHud resaltar;

        /// <summary>Si este paso es el que suelta la horda. Solo el ultimo lo tiene en true.</summary>
        public bool hordaSuelta;
    }

    /// <summary>Lista ordenada de pasos del tutorial. La recorre TutorialProgreso.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Tutorial/Pasos", fileName = "TutorialPasos")]
    public sealed class TutorialPasos : ScriptableObject
    {
        public List<Paso> pasos = new List<Paso>();
    }
}
