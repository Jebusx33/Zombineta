using System.Collections.Generic;
using UnityEngine;
using Zombineta.Level;

namespace Zombineta.Tutorial
{
    /// <summary>Que hacer con la posicion del jugador y de la horda para no colgar el tutorial.</summary>
    public readonly struct RebobinadoDecision
    {
        public readonly bool Rebobinar;

        /// <summary>Nuevo PlayerX. Solo tiene sentido si Rebobinar es true.</summary>
        public readonly float PlayerX;

        /// <summary>Si hay que volver a poner Consumed = false en los items del tipo del paso actual.</summary>
        public readonly bool LimpiarItems;

        public RebobinadoDecision(bool rebobinar, float playerX, bool limpiarItems)
        {
            Rebobinar = rebobinar;
            PlayerX = playerX;
            LimpiarItems = limpiarItems;
        }

        public static readonly RebobinadoDecision Ninguno = new RebobinadoDecision(false, 0f, false);
    }

    /// <summary>
    /// C# plano y testeado: decide si el tutorial se paso de largo un paso basado en items (rampa,
    /// nafta, bateria, municion) o si se acerco demasiado a la meta sin haber terminado, y a donde
    /// hay que volver. TutorialSesion es quien mueve al jugador y a la horda con el resultado:
    /// esta clase no toca RunState ni escena.
    /// </summary>
    public static class TutorialRewind
    {
        /// <summary>Cuanto pasado el ultimo item del tipo hace falta para considerar que se lo salteo.</summary>
        public const float MargenItem = 10f;

        /// <summary>
        /// Lo mismo para las rampas: el salto dura mas que MargenItem (unos 11 m a velocidad normal
        /// y unos 35 m con turbo), asi que con 10 m se podia rebobinar a mitad de un salto bueno.
        /// </summary>
        public const float MargenRampa = 45f;

        /// <summary>Cuanto se retrocede desde el primer item del tipo al rebobinar por items (como maximo).</summary>
        public const float RetrocesoItem = 30f;

        /// <summary>Distancia minima entre el punto de llegada del rebobinado y cualquier otro item.</summary>
        public const float DistanciaLibre = 8f;

        /// <summary>Cuanto antes de la meta empieza la zona de seguridad de los pasos que no son el ultimo.</summary>
        public const float MargenMeta = 40f;

        /// <summary>Cuanto se retrocede al rebobinar por cercania a la meta.</summary>
        public const float RetrocesoMeta = 150f;

        /// <summary>
        /// El tipo de item del recorrido que le corresponde a cada condicion basada en items, o null.
        /// DisparoAcertado no tiene: se le dispara a la horda de atras, no a un item (los zombies
        /// de frente solo se pueden arrollar).
        /// </summary>
        public static LevelEntryKind? ItemDe(CondicionPaso condicion)
        {
            switch (condicion)
            {
                case CondicionPaso.Salto: return LevelEntryKind.Ramp;
                case CondicionPaso.PickupNafta: return LevelEntryKind.Fuel;
                case CondicionPaso.PickupBateria: return LevelEntryKind.Battery;
                case CondicionPaso.PickupMunicion: return LevelEntryKind.Ammo;
                default: return null;
            }
        }

        /// <summary>Cuanto hay que pasarse del ultimo item del tipo para rebobinar.</summary>
        public static float MargenDe(LevelEntryKind kind) =>
            kind == LevelEntryKind.Ramp ? MargenRampa : MargenItem;

        /// <summary>
        /// A donde volver para repetir un tramo que empieza en "primera": el primer punto del hueco
        /// de antes del tramo (desde primera - RetrocesoItem hasta primera - DistanciaLibre) que
        /// quede a DistanciaLibre o mas de todos los demas items, en cualquier carril. Asi no se
        /// cae encima del tramo anterior (por ejemplo, sobre la ultima rampa al volver a los
        /// bidones). Si no hay ninguno libre, primera - RetrocesoItem. Nunca negativo.
        /// </summary>
        public static float DestinoAntesDelTramo(float primera, IReadOnlyList<float> otrosItems)
        {
            float desde = Mathf.Max(0f, primera - RetrocesoItem);
            float hasta = primera - DistanciaLibre;

            for (float x = desde; x <= hasta; x += 1f)
                if (Libre(x, otrosItems))
                    return x;

            return desde;
        }

        static bool Libre(float x, IReadOnlyList<float> otros)
        {
            if (otros == null)
                return true;
            for (int i = 0; i < otros.Count; i++)
                if (Mathf.Abs(otros[i] - x) < DistanciaLibre)
                    return false;
            return true;
        }

        /// <summary>
        /// Decide si hay que rebobinar. Nunca en el aire (se cortaria un salto). Primero mira si el
        /// paso es de items y ya se paso el ultimo de su tipo; si no, y el paso no es el ultimo,
        /// mira si se acerco demasiado a la meta.
        /// </summary>
        /// <param name="condicion">Condicion del paso actual.</param>
        /// <param name="esUltimoPaso">Si el paso actual es el ultimo de la lista (el que suelta la horda).</param>
        /// <param name="distanciasDelItem">Distancias (en metros) de los items del tipo de este paso en el recorrido. Puede ser vacia o null.</param>
        /// <param name="distanciasOtrosItems">Distancias de todos los demas items del recorrido (otros tipos), para elegir donde caer. Puede ser null.</param>
        /// <param name="playerX">Posicion actual del jugador, en metros.</param>
        /// <param name="goalDistance">Metros hasta el refugio.</param>
        /// <param name="enElAire">Si la moto esta volando.</param>
        public static RebobinadoDecision Decidir(
            CondicionPaso condicion,
            bool esUltimoPaso,
            IReadOnlyList<float> distanciasDelItem,
            IReadOnlyList<float> distanciasOtrosItems,
            float playerX,
            float goalDistance,
            bool enElAire)
        {
            if (enElAire)
                return RebobinadoDecision.Ninguno;

            var kind = ItemDe(condicion);
            if (kind.HasValue && distanciasDelItem != null && distanciasDelItem.Count > 0)
            {
                float primera = distanciasDelItem[0];
                float ultima = distanciasDelItem[0];
                for (int i = 1; i < distanciasDelItem.Count; i++)
                {
                    if (distanciasDelItem[i] < primera) primera = distanciasDelItem[i];
                    if (distanciasDelItem[i] > ultima) ultima = distanciasDelItem[i];
                }

                if (playerX > ultima + MargenDe(kind.Value))
                    return new RebobinadoDecision(true, DestinoAntesDelTramo(primera, distanciasOtrosItems), true);
            }

            if (!esUltimoPaso && playerX > goalDistance - MargenMeta)
                return new RebobinadoDecision(true, Mathf.Max(0f, playerX - RetrocesoMeta), false);

            return RebobinadoDecision.Ninguno;
        }
    }
}
