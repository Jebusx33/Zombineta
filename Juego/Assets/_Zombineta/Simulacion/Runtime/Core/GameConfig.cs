using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Core
{
    /// <summary>
    /// Una oleada de la pelea a pie: desde cuantos segundos de pelea rige, cuantos zombies hay a la
    /// vez y de que tipos (un peso por tipo, en el orden de Zombies.asset: Comun, Corredor, Pesado).
    /// </summary>
    [Serializable]
    public sealed class OleadaPelea
    {
        [Tooltip("Segundos desde que empieza la pelea a partir de los que rige esta oleada.")]
        public float desdeSegundo;
        [Tooltip("Zombies peleando a la vez.")]
        [Min(1)] public int zombies = 3;
        [Tooltip("Peso de cada tipo de zombie, en el orden de Zombies.asset (Comun, Corredor, Pesado).")]
        public float[] pesos = { 1f, 0f, 0f };
        [Tooltip("Multiplica la velocidad con la que caminan.")]
        public float velocidad = 1f;
    }

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
        [Tooltip("Nafta con la que se arranca el nivel. 0 o menos = el tanque lleno (fuelMax). Un tanque corto al principio " +
                 "hace que el primer auxilio llegue pronto, para presentar la mecanica.")]
        public float fuelInicial = 0f;
        [Tooltip("Los bidones no aparecen (no se ven ni se agarran) hasta haber usado tantos auxilios. 0 = siempre.")]
        [Min(0)] public int bidonesDesdeAuxilio = 0;
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

        [Header("Horda")]
        [Tooltip("Los tipos de zombie que pueden aparecer. Sin asset, todos son comunes.")]
        public Zombineta.Enemies.ZombieRoster zombies;

        [Tooltip("Cuantos zombies se simulan a la vez. La masa se recicla: nunca se agota.")]
        public int hordeCount = 24;

        [Tooltip("Metros que se extiende la masa hacia atras desde el frente.")]
        public float hordeDepthMeters = 14f;

        [Tooltip("Segundos que queda el cadaver antes de volver a entrar por el fondo.")]
        public float corpseSeconds = 1.2f;

        [Tooltip("Semilla de la horda: los tipos y las muertes son reproducibles entre playtests.")]
        public int hordeSeed = 2026;

        [Tooltip("Apenas mas rapida que el modo Normal: mantener Normal pierde terreno de a poco.")]
        public float hordeBaseSpeed = 12.5f;
        [Tooltip("A partir de esta distancia la horda empieza a acelerar para sostener la tension.")]
        public float rubberBandStartGap = 80f;
        [Tooltip("Rango de distancia sobre el cual la aceleracion llega al maximo.")]
        public float rubberBandRange = 80f;
        [Tooltip("Velocidad extra maxima que puede ganar la horda por goma elastica.")]
        public float rubberBandMaxBonus = 6f;

        [Tooltip("Piedad de ultimo metro: con la horda a menos de estos metros, afloja un poco. " +
                 "Da mas 'casi me agarran' y menos muertes secas. 0 = apagada.")]
        [Min(0f)] public float mercyGap = 0f;
        [Tooltip("Fraccion de su velocidad a la que queda la horda cuando esta encima (ventaja 0). " +
                 "Entre eso y la ventaja de arriba se va recuperando. Mantenerla por encima de " +
                 "normalSpeed / hordeBaseSpeed o la horda nunca alcanza a quien va en Normal.")]
        [Range(0f, 1f)] public float mercySlowFactor = 0.88f;

        [Tooltip("Metros sobre los que se reparte el cambio de presion de la horda entre dos tramos.")]
        [Min(0f)] public float tramoPressureBlendMeters = 80f;
        [Tooltip("Metros del fundido de luz al entrar y salir de un apagon.")]
        [Min(0f)] public float tramoDarknessFadeMeters = 30f;

        /// <summary>Los tramos del nivel en curso. No se guarda en el asset: los pone el nivel
        /// (LevelScene.RuntimeConfig) en su copia. Sin tramos, todo se comporta como siempre.</summary>
        [System.NonSerialized] public System.Collections.Generic.List<Zombineta.Level.Tramo> tramos;

        [Header("Disparo y explosiones")]
        [Tooltip("Alcance de la bala hacia atras, en metros.")]
        public float shotRangeMeters = 60f;
        [Tooltip("Metros alrededor de una muerte que hacen frenar a los vecinos.")]
        public float deathScareRadius = 4f;
        public float deathScareSeconds = 0.5f;
        [Tooltip("Metros en los que una explosion mata.")]
        public float explosionRadius = 8f;
        [Tooltip("Metros en los que una explosion solo asusta.")]
        public float explosionScareRadius = 16f;
        public float explosionScareSeconds = 1f;

        [Header("Contacto con el recorrido")]
        [Tooltip("Metros del centro de la moto a su punta delantera. Se agarra y se choca cuando " +
                 "la punta toca el borde de algo, no cuando el centro pasa por el centro. 0 = puntual.")]
        [Min(0f)] public float motoNariz = 0f;
        [Tooltip("Metros del centro de la moto a su parte de atras.")]
        [Min(0f)] public float motoCola = 0f;
        [Tooltip("Medio largo de los pickups cuando el nivel no lo dice, en metros.")]
        [Min(0f)] public float medioLargoRecurso = 0f;
        [Tooltip("Medio largo de los obstaculos cuando el nivel no lo dice, en metros.")]
        [Min(0f)] public float medioLargoObstaculo = 0f;
        [Tooltip("Medio largo de los zombies de frente cuando el nivel no lo dice, en metros.")]
        [Min(0f)] public float medioLargoZombie = 0f;

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
        [Tooltip("Tope de la velocidad vertical al salir de la rampa, en m/s: por mas rapido " +
                 "que se pise, no salta mas alto que esto. 0 = sin tope.")]
        [Min(0f)] public float maxLaunchVerticalSpeed = 0f;
        [Tooltip("Grados por segundo que inclina la jugadora con A/D en el aire.")]
        public float leanRate = 60f;
        [Tooltip("Inclinacion maxima de la moto en el aire, en grados. Por debajo de 'safeLandingAngle' mantener el turbo o el " +
                 "retroceso apretados en el aire nunca la hace caer; el aterrizaje perfecto (con impulso) sigue pidiendo enderezarla.")]
        public float maxPitch = 22f;
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

        [Header("Auxilio y pelea a pie")]
        [Tooltip("Pedidos de auxilio por nivel. 0 = sin auxilio: quedarse sin nafta es perder, como siempre.")]
        [Min(0)] public int ayudas = 0;
        [Tooltip("Segundos que la moto sigue avanzando, frenando y con el motor tosiendo, entre quedarse sin nafta " +
                 "y bajarse a pelear. 0 = se baja al instante.")]
        public float paradaSegundos = 1.2f;
        [Tooltip("Segundos que tarda la moto en volver a velocidad normal despues del auxilio.")]
        public float arranqueSegundos = 0.9f;
        [Tooltip("Corazones de mas en la primera pelea de la partida: el jugador todavia esta aprendiendo.")]
        [Min(0)] public int vidaExtraPrimeraPelea = 1;
        [Tooltip("Veces que, al caer derribada, la pelea se reinicia en vez de perder. Cada pelea tiene las suyas.")]
        [Min(0)] public int reintentosPelea = 0;
        [Tooltip("Oleadas de la pelea, de menos a mas. Vacio = siempre 'zombiesEnArena' del tipo que toque.")]
        public List<OleadaPelea> oleadas = new List<OleadaPelea>
        {
            new OleadaPelea { desdeSegundo = 0f,  zombies = 3, pesos = new[] { 1f, 0f, 0f },      velocidad = 0.85f },
            new OleadaPelea { desdeSegundo = 10f, zombies = 5, pesos = new[] { 0.7f, 0.3f, 0f },  velocidad = 1f },
            new OleadaPelea { desdeSegundo = 20f, zombies = 6, pesos = new[] { 0.5f, 0.3f, 0.2f }, velocidad = 1f },
        };
        [Header("Premios y botiquines de la pelea")]
        [Tooltip("Probabilidad de que un zombie que muere (a golpes o a tiros) deje un botiquin: un corazon.")]
        [Range(0f, 1f)] public float dropBotiquin = 0.15f;
        [Tooltip("Probabilidad de que deje balas (se suma a la del botiquin: nunca sueltan las dos).")]
        [Range(0f, 1f)] public float dropBalas = 0.10f;
        [Tooltip("Balas que da cada drop de balas.")]
        [Min(1)] public int dropBalasCantidad = 2;
        [Tooltip("Segundos que queda en el piso un drop antes de desaparecer.")]
        public float dropSegundos = 10f;
        [Tooltip("Metros a los que lo recoge al caminar sobre el.")]
        public float dropAlcance = 0.9f;
        [Tooltip("Balas que deja la furgoneta segun la nota de la pelea, de peor a mejor (C, B, A, S).")]
        public int[] premioBalas = { 0, 1, 2, 3 };
        [Tooltip("Bateria que deja la furgoneta segun la nota (en puntos de bateria), de peor a mejor (C, B, A, S).")]
        public float[] premioBateria = { 0f, 0f, 25f, 40f };

        [Tooltip("Segundos que tarda en llegar la furgoneta: lo que dura la pelea.")]
        [Min(1f)] public float peleaSegundos = 30f;
        [Tooltip("Golpes que aguanta la protagonista a pie.")]
        [Min(1)] public int vidaPelea = 5;
        [Tooltip("Metros de ventaja sobre la horda al volver a la moto.")]
        public float gapTrasAuxilio = 45f;
        [Tooltip("Metros a cada lado del centro por donde se puede mover a pie.")]
        public float arenaMedioAncho = 12f;
        [Tooltip("Metros por segundo caminando.")]
        public float peatonVelocidad = 6f;
        [Tooltip("Carriles por segundo al caminar de arriba a abajo (0,99 m cada uno). Con el stick a fondo.")]
        public float peatonVelocidadCarril = 3f;
        [Tooltip("Cuantos carriles de diferencia puede haber entre ella y un zombie para que se peguen. " +
                 "Ella camina entre carriles; los zombies estan en uno entero.")]
        public float toleranciaCarril = 0.6f;
        [Tooltip("Metros hacia donde mira que alcanza el golpe.")]
        public float golpeAlcance = 1.6f;
        [Tooltip("Segundos entre un golpe y el siguiente de la misma cadena (1.º y 2.º golpe).")]
        public float golpeEnfriamiento = 0.2f;
        [Tooltip("Segundos, una vez pasado el enfriamiento, que quedan para seguir la cadena. Si se pasa, vuelve al 1.º golpe.")]
        public float comboVentana = 0.6f;
        [Tooltip("Segundos de pausa despues del 3.º golpe (el remate), antes de poder empezar otra cadena.")]
        public float rematePausa = 0.55f;
        [Tooltip("Metros hacia delante (hacia donde mira) que alcanza el remate para derribar. Lo que quedo a su espalda no se toca.")]
        public float remateRadio = 2.4f;
        [Tooltip("Metros que el remate empuja a los que derriba.")]
        public float remateEmpuje = 3.5f;
        [Tooltip("Segundos que quedan en el piso los derribados por el remate.")]
        public float remateDerribo = 1.4f;
        [Tooltip("Segundos que dura el movimiento del golpe (para la vista).")]
        public float golpeDuracion = 0.18f;
        [Tooltip("Metros que empuja el golpe.")]
        public float golpeEmpuje = 2.5f;
        [Tooltip("Segundos que queda frenado el zombie golpeado.")]
        public float golpeFreno = 0.5f;
        [Tooltip("Zombies a la vez en la pelea.")]
        [Min(1)] public int zombiesEnArena = 8;
        [Tooltip("Metros por segundo de un zombie comun a pie (cada tipo lo multiplica).")]
        public float zombieVelocidadPelea = 2.2f;
        [Tooltip("Metros desde los que un zombie pega.")]
        public float zombieAlcance = 1.1f;
        [Tooltip("Metros minimos entre dos zombies vivos del mismo carril en la pelea: los demas hacen fila " +
                 "detras del que esta pegado a la protagonista, en vez de encimarse en el mismo lugar.")]
        public float zombieSeparacion = 1.1f;
        [Tooltip("Zombies que pueden estar preparando un ataque a la vez: los demas esperan su turno. Evita que la rodeen y la golpeen todos juntos.")]
        [Min(1)] public int maxAtacantes = 2;
        [Tooltip("Segundos que se prepara antes de pegar: el aviso para esquivarlo o pegarle primero.")]
        public float zombiePreparacion = 0.6f;
        [Tooltip("Segundos entre ataque y ataque de un mismo zombie.")]
        public float zombieEnfriamiento = 1f;
        [Tooltip("Segundos sin recibir dano despues de un golpe.")]
        public float invulnerableTrasGolpe = 1f;
        [Tooltip("Metros por segundo de la furgoneta.")]
        public float furgonetaVelocidad = 28f;
        [Tooltip("Metros antes del centro de la arena desde donde entra la furgoneta.")]
        public float furgonetaDesde = 24f;

        [Header("Presentacion")]
        [Tooltip("Unidades de mundo por metro simulado. La simulacion piensa en metros; " +
                 "la pantalla no puede mostrar 40 m de ventaja a escala 1:1, asi que se " +
                 "comprime. No afecta el balance, solo lo que se ve.")]
        public float worldUnitsPerMeter = 0.75f;

        [Tooltip("Metros que la camara se queda atras de la jugadora. Positivo la deja a la " +
                 "derecha de la pantalla, con aire a la izquierda para ver venir a la horda.")]
        public float cameraTrailMeters = 25f;

        [Tooltip("Unidades de mundo por metro de altura del salto. Un poco mas que en X: " +
                 "exagerado a proposito para que el salto se lea. Si se cambia la escala del " +
                 "mundo, acompanarla para que el arco no cambie de forma.")]
        public float jumpHeightToWorld = 1.125f;
    }
}
