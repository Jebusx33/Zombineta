using UnityEngine;

namespace Zombineta.CameraFx
{
    /// <summary>
    /// Todo el lenguaje de camara en un asset. Se ajusta en el Inspector con el juego
    /// corriendo: los cambios en un ScriptableObject durante Play Mode quedan guardados.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Camera Config", fileName = "CameraConfig")]
    public sealed class CameraConfig : ScriptableObject
    {
        [Header("Encuadre base")]
        [Tooltip("Tamano ortografico con los efectos apagados: el encuadre de siempre.")]
        public float baseSize = 6f;

        [Tooltip("Altura en mundo del borde inferior de la pantalla. Queda fijo al hacer zoom, " +
                 "asi la calle no se corre: se gana o se pierde cielo.")]
        public float viewBottomY = -4.6f;

        [Tooltip("Unidades entre la moto y el borde derecho de la pantalla. Se conserva al " +
                 "hacer zoom: los obstaculos se ven venir con el mismo tiempo de reaccion.")]
        public float lookAhead = 6.2f;

        [Tooltip("Segundos que tarda la camara en alcanzar a la moto.")]
        public float followTime = 0.12f;

        [Header("Tension: distancia a la horda")]
        [Tooltip("Plano abierto: horda lejos.")]
        public float wideSize = 6.3f;

        [Tooltip("Plano cerrado: horda encima.")]
        public float tightSize = 5f;

        [Tooltip("Metros a partir de los cuales la horda no genera tension.")]
        public float safeGap = 45f;

        [Tooltip("Metros a los que la tension es maxima.")]
        public float dangerGap = 8f;

        [Tooltip("Segundos para cerrarse. Lento: el miedo se va instalando.")]
        public float zoomInTime = 0.9f;

        [Tooltip("Segundos para abrirse. Rapido: el alivio de alejar a la horda se siente al instante.")]
        public float zoomOutTime = 0.3f;

        [Header("Turbo")]
        public float turboExtraSize = 0.35f;

        [Tooltip("Unidades extra de vista adelante en turbo: mas rapido, mas lejos se ve.")]
        public float turboExtraLookAhead = 1.5f;

        public float turboResponseTime = 0.4f;

        [Header("Sacudida (trauma)")]
        [Tooltip("Desplazamiento maximo, en unidades, con trauma 1.")]
        public float maxShakeOffset = 0.35f;

        [Tooltip("Giro maximo, en grados, con trauma 1. Poco: girar mucho deja ver los bordes del escenario.")]
        public float maxShakeRoll = 1f;

        [Tooltip("Frecuencia del ruido de la sacudida.")]
        public float shakeFrequency = 18f;

        [Tooltip("Trauma que se pierde por segundo.")]
        public float traumaDecay = 1.5f;

        [Header("Golpes (resorte)")]
        public float punchFrequency = 3.5f;

        [Range(0.05f, 1f)] public float punchDamping = 0.4f;

        [Header("Choque")]
        public float crashTrauma = 0.6f;

        [Tooltip("La moto frena en seco y la camara sigue de largo: latigazo hacia adelante.")]
        public float crashPunchForward = 0.6f;

        [Tooltip("Golpe de zoom al chocar. Negativo = hacia adentro.")]
        public float crashPunchZoom = -0.45f;

        [Tooltip("Segundos de pausa total en el impacto.")]
        public float hitstopSeconds = 0.06f;

        [Header("Disparo")]
        public float shotTrauma = 0.18f;

        [Tooltip("La pistola dispara hacia atras: el retroceso empuja la camara hacia adelante.")]
        public float shotPunchForward = 0.3f;

        [Header("Atrapada")]
        public float catchSize = 3.6f;
        public float catchZoomTime = 0.18f;

        [Range(0.05f, 1f)] public float catchTimeScale = 0.25f;

        public float catchTrauma = 0.5f;

        [Tooltip("Segundos reales antes de mostrar el Game Over.")]
        public float catchHoldSeconds = 1.2f;

        [Header("Victoria")]
        public float victorySize = 7.5f;
        public float victoryOpenTime = 1f;

        [Tooltip("Segundos reales antes de mostrar la pantalla de victoria.")]
        public float victoryHoldSeconds = 1.6f;

        [Header("Salto")]
        [Tooltip("Unidades que se dejan libres por encima del carril mas la altura del salto. " +
                 "Incluye el alto de la moto: si es menor, la moto se sale por arriba.")]
        public float jumpHeadroom = 2f;

        [Tooltip("Segundos para abrirse cuando lo que manda es el salto. Mas rapido que el " +
                 "zoom normal: la moto sube rapido.")]
        public float jumpZoomTime = 0.15f;

        [Tooltip("Aterrizaje perfecto: golpecito de satisfaccion.")]
        public float perfectLandingTrauma = 0.12f;
        public float perfectLandingPunchForward = 0.25f;

        [Header("Horda")]
        [Tooltip("Sacudida de una explosion de barril.")]
        public float explosionTrauma = 0.5f;

        [Tooltip("Sacudida al arrollar a un zombie de frente.")]
        public float ramTrauma = 0.25f;
    }
}
