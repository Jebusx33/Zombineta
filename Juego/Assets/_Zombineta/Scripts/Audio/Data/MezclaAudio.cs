using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>
    /// Los numeros finos de la mezcla que no son de un sonido en particular: tono del motor,
    /// capa de tension, pausa y curva de los sliders. Vive en Settings/Audio/Mezcla.asset y lo
    /// sostiene el AudioDirector de Boot; todos lo leen cada cuadro, asi que un cambio en el
    /// Inspector se oye al instante, tambien en Play. Los valores por defecto son los que el
    /// juego usaba cuando esto estaba en codigo.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Audio/Mezcla", fileName = "Mezcla")]
    public sealed class MezclaAudio : ScriptableObject
    {
        [Header("Motor")]
        [Tooltip("Tono del loop con la moto quieta.")]
        [Min(0.1f)] public float tonoQuieta = 0.8f;

        [Tooltip("Tono a velocidad normal. Entre quieta y normal sube en proporcion a la velocidad.")]
        [Min(0.1f)] public float tonoNormal = 1.2f;

        [Tooltip("Tono con turbo.")]
        [Min(0.1f)] public float tonoTurbo = 1.45f;

        [Tooltip("Segundos que tarda el tono en alcanzar el valor nuevo (mas alto = mas perezoso).")]
        [Min(0f)] public float suavizadoTono = 0.15f;

        [Tooltip("Segundos en que el motor se apaga al quedarse sin nafta.")]
        [Min(0.01f)] public float fundidoSinNafta = 1f;

        [Tooltip("Segundos en que el motor se apaga al perder.")]
        [Min(0.01f)] public float fundidoAlPerder = 0.3f;

        [Header("Capa de tension")]
        [Tooltip("A cuantos metros de la moto la horda empieza a subir la tension. Encima de la moto, maxima.")]
        [Min(0.01f)] public float distanciaAmenaza = 45f;

        [Tooltip("Segundos que tarda la tension en seguir a la horda.")]
        [Min(0f)] public float suavizadoTension = 0.5f;

        [Header("Pausa")]
        [Tooltip("Volumen de la musica durante la pausa (1 = igual que jugando). Los efectos se callan siempre.")]
        [Range(0f, 1f)] public float musicaEnPausa = 0.4f;

        [Tooltip("Segundos del fundido al pausar y al volver a jugar.")]
        [Min(0.01f)] public float fundidoPausa = 0.2f;

        [Header("Sliders de volumen")]
        [Tooltip("Curva de los sliders de Opciones: ganancia = valor ^ exponente. 1 = lineal; 2 = se siente mas parejo al oido.")]
        [Range(1f, 4f)] public float exponenteVolumen = 2f;

        static MezclaAudio porDefecto;

        /// <summary>Instancia con los valores por defecto, para cuando no hay asset asignado.</summary>
        public static MezclaAudio PorDefecto
        {
            get
            {
                if (porDefecto == null)
                {
                    porDefecto = CreateInstance<MezclaAudio>();
                    porDefecto.hideFlags = HideFlags.HideAndDontSave;
                }
                return porDefecto;
            }
        }
    }
}
