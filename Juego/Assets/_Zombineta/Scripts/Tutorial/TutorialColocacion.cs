using Zombineta.Level;

namespace Zombineta.Tutorial
{
    /// <summary>
    /// C# plano y testeado: donde va el objeto de cada paso del tutorial y cuando se lo paso de largo.
    /// El tutorial no tiene un recorrido fijo: cada paso que necesita un objeto (rampa, bidon,
    /// bateria, balas) lo pone unos metros delante de la moto cuando empieza, y si el jugador se lo
    /// pasa, le pone otro. Asi nunca hay que volver atras ni saltear pasos.
    /// </summary>
    public static class TutorialColocacion
    {
        /// <summary>Donde se guardan los objetos que todavia no se usan: lejos de todo lo que se ve.</summary>
        public const float Guardado = 50000f;

        /// <summary>El refugio mientras no llega el paso final: mas lejos que cualquier objeto guardado.</summary>
        public const float MetaLejana = 100000f;

        /// <summary>Metros que tiene que quedar atras un objeto sin agarrar para contarlo como pasado de largo.</summary>
        public const float MargenPasado = 3f;

        /// <summary>El tipo de objeto que pide cada condicion, o null si no pide ninguno.</summary>
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

        /// <summary>
        /// En que carril va el objeto. La rampa, en el de la moto: el paso es saltar, no apuntarle.
        /// Lo que se agarra, en el de al lado: asi se practica cambiar de carril para ir a buscarlo.
        /// </summary>
        public static int CarrilPara(CondicionPaso condicion, int carrilActual)
        {
            if (condicion == CondicionPaso.Salto)
                return carrilActual;
            return carrilActual == 1 ? 2 : 1;
        }

        /// <summary>Si el objeto ya quedo atras sin agarrarlo. En el aire todavia no: se puede caer encima.</summary>
        public static bool SePasoDeLargo(float itemX, float playerX, bool agarrado, bool enElAire) =>
            !agarrado && !enElAire && itemX < playerX - MargenPasado;
    }
}
