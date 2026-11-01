using UnityEngine;

namespace Zombineta.Jefe
{
    /// <summary>Lo que hace el jefe en cada paso de su patron.</summary>
    public enum AccionDeJefe
    {
        /// <summary>Proyectil recto por el carril fijado: hay que salir de ese carril antes de que llegue.</summary>
        Frontal = 0,
        /// <summary>Mortero: vuela en arco y cae junto a la moto, sobre el carril fijado y marcado en el piso.</summary>
        Elevado = 1,
        /// <summary>Avisa, se lanza por el carril fijado y despues pierde terreno.</summary>
        Embestida = 2,
        /// <summary>Queda sin fuerzas: es el unico momento en que se lo puede lastimar.</summary>
        Aturdido = 3,
    }

    /// <summary>
    /// Todos los numeros del duelo con un jefe. El nivel que lo tiene lo asigna en LevelScene; sin este
    /// asset no hay jefe y el nivel se juega como siempre. Los segundos de las animaciones tienen que
    /// coincidir con los del asset AnimacionesJefe: la logica decide y el dibujo la acompana.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Jefe", fileName = "Jefe")]
    public sealed class JefeConfig : ScriptableObject
    {
        [Header("Entrada")]
        [Tooltip("Metro del recorrido donde empieza el duelo.")]
        [Min(0f)] public float metroEntrada = 3500f;
        [Tooltip("Segundos que tarda en llegar desde fuera de pantalla. Da tiempo a pasar lo que quedo en el camino.")]
        [Min(0.1f)] public float segundosEntrada = 2.5f;
        [Tooltip("Metros detras de la moto donde aparece, fuera de pantalla.")]
        [Min(0f)] public float separacionEntrada = 30f;
        [Tooltip("Metros por delante de la moto desde donde se despeja el camino al empezar el duelo.")]
        [Min(0f)] public float despejarDesde = 35f;
        [Tooltip("Velocidad de la horda durante el duelo, como fraccion de la normal: se queda atras.")]
        [Range(0f, 1f)] public float hordaEnDuelo = 0.5f;

        [Header("Resistencia")]
        [Min(1)] public int vida = 12;
        [Tooltip("Con esta vida o menos pasa al patron de la segunda mitad, al terminar el ciclo en curso.")]
        [Min(0)] public int vidaSegundaMitad = 6;
        [Min(0)] public int danoBala = 1;
        [Min(0)] public int danoGranada = 3;

        [Header("Moto")]
        [Tooltip("Golpes que aguanta la moto en el duelo. Con el ultimo es Game Over.")]
        [Min(1)] public int corazones = 3;
        [Tooltip("Segundos sin recibir otro golpe despues de uno.")]
        [Min(0f)] public float invulnerable = 2f;
        [Tooltip("Segundos que la moto queda frenada por un golpe.")]
        [Min(0f)] public float frenoPorGolpe = 0.4f;

        [Header("Persecucion")]
        [Tooltip("Metros detras de la moto a los que va. Se mantiene aunque la moto acelere o frene.")]
        [Min(0f)] public float separacion = 11f;
        [Tooltip("Carriles por segundo al ponerse en el carril que apunta.")]
        [Min(0.1f)] public float velocidadCarril = 4f;
        [Tooltip("Segundos entre una accion y la siguiente.")]
        [Min(0f)] public float pausaEntreAcciones = 0.6f;
        [Tooltip("Patron de la primera mitad. Se repite.")]
        public AccionDeJefe[] patronPrimeraMitad = { AccionDeJefe.Frontal, AccionDeJefe.Elevado, AccionDeJefe.Aturdido };
        [Tooltip("Patron de la segunda mitad. Se repite.")]
        public AccionDeJefe[] patronSegundaMitad =
        {
            AccionDeJefe.Frontal, AccionDeJefe.Embestida, AccionDeJefe.Aturdido,
            AccionDeJefe.Elevado, AccionDeJefe.Embestida, AccionDeJefe.Aturdido,
        };

        [Header("Disparos")]
        [Tooltip("Segundos de la animacion de anticipacion. Desde que empieza, el carril queda fijado y marcado.")]
        [Min(0.1f)] public float anticipacion = 0.95f;
        [Tooltip("Segundos de la animacion del disparo frontal. Emite al entrar.")]
        [Min(0.1f)] public float disparoFrontal = 0.5f;
        [Tooltip("Segundos de la animacion del disparo elevado.")]
        [Min(0.1f)] public float disparoElevado = 0.6f;
        [Tooltip("Tiempo de la animacion del disparo elevado en que emite.")]
        [Min(0f)] public float emisionElevado = 0.18f;
        [Tooltip("Metros desde el jefe hasta la boca del brazo en el disparo frontal.")]
        public float bocaFrontal = 3.4f;
        [Tooltip("Metros desde el jefe hasta la boca del brazo en el disparo elevado.")]
        public float bocaElevado = 2.2f;

        [Header("Proyectil")]
        [Tooltip("Metros por segundo que el proyectil frontal le gana a la moto.")]
        [Min(0.1f)] public float velocidadProyectil = 13f;
        [Tooltip("Segundos de vuelo del proyectil elevado.")]
        [Min(0.1f)] public float vueloElevado = 1.1f;
        [Tooltip("Metros de la masa del proyectil: lo que pega. La cola no cuenta.")]
        [Min(0f)] public float radioProyectil = 0.2f;
        [Tooltip("Metros por delante de la moto donde se retira un proyectil que no pego.")]
        [Min(0f)] public float salidaProyectil = 22f;
        [Tooltip("Vida maxima de seguridad del proyectil, en segundos.")]
        [Min(0.1f)] public float vidaProyectil = 4f;
        [Tooltip("Segundos de la animacion de impacto.")]
        [Min(0.01f)] public float impacto = 0.28f;

        [Header("Embestida")]
        [Tooltip("Segundos de aviso antes de lanzarse.")]
        [Min(0.1f)] public float avisoEmbestida = 1.2f;
        [Tooltip("Segundos del avance. Son los cuadros 02 y 03 de la animacion.")]
        [Min(0.1f)] public float avanceEmbestida = 0.44f;
        [Tooltip("Metros detras de la moto donde termina el avance.")]
        public float separacionEmbestida = -2f;
        [Tooltip("Metros desde el jefe hasta la punta de sus orugas: lo que choca con la moto.")]
        [Min(0f)] public float frente = 3f;
        [Tooltip("Segundos que tarda en volver a su distancia.")]
        [Min(0.1f)] public float regresoEmbestida = 1.5f;

        [Header("Aturdido")]
        [Tooltip("Segundos en que se lo puede lastimar.")]
        [Min(0.1f)] public float segundosAturdido = 3.5f;
        [Tooltip("Segundos de la animacion de dano.")]
        [Min(0.01f)] public float dano = 0.33f;

        [Header("Municion")]
        [Tooltip("Al empezar cada ciclo aparecen en el camino una caja de balas y una granada, las que no esten al tope. " +
                 "Balas que trae la caja.")]
        [Min(1)] public int balasPorCaja = 4;
        [Tooltip("Metros por delante de la moto donde aparece la caja.")]
        [Min(1f)] public float cajaAdelante = 40f;
        [Tooltip("Metros por delante de la moto donde aparece la granada. Mas lejos que la caja: no se agarran en el mismo momento.")]
        [Min(1f)] public float granadaAdelante = 52f;

        [Header("Final")]
        [Tooltip("Segundos que sigue en pantalla al caer: lo que dura la animacion de muerte y un momento mas. Despues queda atras.")]
        [Min(0f)] public float caidaEnPantalla = 2.4f;
        [Tooltip("Metros hasta el refugio una vez derrotado.")]
        [Min(1f)] public float metrosAlRefugio = 60f;
    }
}
