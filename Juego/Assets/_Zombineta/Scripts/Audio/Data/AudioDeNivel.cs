using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>Lo que un nivel suma al audio: su tema, su capa de tension, el ambiente y su banco de efectos.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Audio/Audio de nivel", fileName = "AudioDeNivel")]
    public sealed class AudioDeNivel : ScriptableObject
    {
        [Tooltip("El tema del nivel. Fundido cruzado al entrar.")]
        public AudioClip musica;

        [Tooltip("Capa de tension: misma duracion que musica, arranca en el mismo dspTime en loop y a volumen 0.")]
        public AudioClip tension;

        [Tooltip("Volumen de la tension con la horda encima (0-1). Lejos, siempre 0; en el medio, sube de a poco.")]
        [Range(0f, 1f)] public float volumenTension = 1f;

        [Tooltip("La tension es un loop propio (percusion, coro) que no tiene que ir a tiempo con el tema: " +
                 "puede durar distinto sin aviso de desfasaje.")]
        public bool tensionLibre;

        [Tooltip("Loop de ambiente del nivel, por el bus Ambiente.")]
        public Sonido ambiente;

        [Tooltip("Pisa al banco global para las claves que define.")]
        public BancoDeSonidos banco;
    }
}
