using UnityEngine;

namespace Zombineta.Core
{
    /// <summary>
    /// Todos los numeros de balance del juego viven aca, en un solo asset.
    /// Balancear (T23) es editar este asset en el Inspector: nunca tocar codigo.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Recorrido")]
        [Tooltip("Largo del nivel en metros. A 12 m/s, 4000 m son unos 5 y medio minutos.")]
        public float goalDistance = 4000f;
        [Tooltip("Ventaja inicial de la jugadora sobre la horda, en metros.")]
        public float startingGap = 60f;
        [Tooltip("Separacion vertical entre carriles, en unidades de mundo.")]
        public float laneSpacing = 1.6f;
        [Tooltip("Segundos que tarda el cambio de carril.")]
        public float laneChangeDuration = 0.15f;

        [Header("Velocidad")]
        public float normalSpeed = 12f;
        [Tooltip("Turbo: multiplica la velocidad y el consumo.")]
        public float turboSpeedMultiplier = 1.8f;
        [Tooltip("Retroceso: negativo, la moto va hacia atras.")]
        public float reverseSpeedMultiplier = -0.5f;

        [Header("Combustible")]
        public float fuelMax = 100f;
        [Tooltip("Consumo por segundo en modo Normal.")]
        public float fuelBurnPerSecond = 1.6f;
        public float turboBurnMultiplier = 3f;
        public float reverseBurnMultiplier = 0.5f;
        public float fuelPickupAmount = 25f;

        [Header("Bateria y faro")]
        public float batteryMax = 100f;
        [Tooltip("Drenaje por segundo con el faro encendido. 8/s = 12,5 s de luz con el tanque lleno.")]
        public float batteryDrainPerSecond = 8f;
        public float batteryPickupAmount = 40f;
        [Tooltip("Con el faro prendido la horda avanza a esta fraccion de su velocidad.")]
        [Range(0f, 1f)] public float headlightHordeSlowFactor = 0.5f;

        [Header("Pistola")]
        public int ammoMax = 6;
        public int ammoAtStart = 3;
        public int ammoPickupAmount = 2;
        [Tooltip("Metros que retrocede la horda por cada disparo.")]
        public float shotHordePushback = 15f;

        [Header("Horda")]
        [Tooltip("Apenas mas rapida que el modo Normal: mantener Normal pierde terreno de a poco.")]
        public float hordeBaseSpeed = 12.5f;
        [Tooltip("A partir de esta distancia la horda empieza a acelerar para sostener la tension.")]
        public float rubberBandStartGap = 80f;
        [Tooltip("Rango de distancia sobre el cual la aceleracion llega al maximo.")]
        public float rubberBandRange = 80f;
        [Tooltip("Velocidad extra maxima que puede ganar la horda por goma elastica.")]
        public float rubberBandMaxBonus = 6f;

        [Header("Choque contra obstaculo")]
        public float crashStunDuration = 0.8f;
        public float crashFuelPenalty = 8f;

        [Header("Salto")]
        [Tooltip("Velocidad vertical de salida por cada m/s con que se pisa la rampa. " +
                 "Mas rapido: mas alto y mas lejos.")]
        public float rampLaunchSlope = 0.75f;
        [Tooltip("Gravedad del salto en m/s2. De juego, no realista.")]
        public float jumpGravity = 20f;
        [Tooltip("Cuanto planea con la nariz arriba y cuanto cae con la nariz abajo. " +
                 "0 = inclinar no cambia el largo del salto.")]
        [Range(0f, 0.9f)] public float leanLift = 0.35f;
        [Tooltip("Grados de nariz arriba al salir de la rampa. Un salto normal sin tocar nada " +
                 "aterriza con esta inclinacion.")]
        public float launchPitch = 20f;
        [Tooltip("Giro hacia atras al salir, en grados/s por cada m/s por encima de la " +
                 "velocidad normal. A velocidad normal no gira; en turbo hay que corregir.")]
        public float launchSpinPerExcessSpeed = 2.6f;
        [Tooltip("Grados por segundo que inclina la jugadora con A/D en el aire.")]
        public float leanRate = 60f;
        public float maxPitch = 80f;
        [Tooltip("Aterrizar con menos inclinacion que esta da impulso.")]
        public float perfectLandingAngle = 8f;
        [Tooltip("Aterrizar con mas inclinacion que esta tira la moto al piso.")]
        public float safeLandingAngle = 25f;
        public float landingBoostMultiplier = 1.25f;
        public float landingBoostDuration = 1.5f;
        [Tooltip("Segundos tirada en el piso tras aterrizar mal. Mas que un choque.")]
        public float fallStunDuration = 1.6f;
        public float fallFuelPenalty = 10f;
        [Tooltip("Metros de diferencia de altura con que se agarra un pickup en el aire.")]
        public float aerialPickupTolerance = 1.5f;

        [Header("Presentacion")]
        [Tooltip("Unidades de mundo por metro simulado. La simulacion piensa en metros; " +
                 "la pantalla no puede mostrar 40 m de ventaja a escala 1:1, asi que se " +
                 "comprime. No afecta el balance, solo lo que se ve.")]
        public float worldUnitsPerMeter = 0.25f;

        [Tooltip("Metros que la camara se queda atras de la jugadora. Positivo la deja a la " +
                 "derecha de la pantalla, con aire a la izquierda para ver venir a la horda.")]
        public float cameraTrailMeters = 25f;

        [Tooltip("Unidades de mundo por metro de altura del salto. Mas que en X (0,25): " +
                 "exagerado a proposito para que el salto se lea.")]
        public float jumpHeightToWorld = 0.5f;
    }
}
