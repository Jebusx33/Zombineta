using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>Lo que un nivel suma al audio: su tema, su capa de tension, el ambiente y su banco de efectos.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Audio/Audio de nivel", fileName = "AudioDeNivel")]
    public sealed class AudioDeNivel : ScriptableObject
    {
        [Tooltip("El tema del nivel. Fundido cruzado al entrar.")]
        public AudioClip musica;

        [Tooltip("Volumen del tema (0-1). Para emparejar un tema muy fuerte con los de los otros niveles sin tocar el archivo.")]
        [Range(0f, 1f)] public float volumenMusica = 1f;

        [Tooltip("Volumen del tema con el jefe (0-1): baja de a poco junto con la capa del jefe (metrosAntesDelJefe), " +
                 "asi el duelo se oye. En un nivel sin jefe no se usa.")]
        [Range(0f, 1f)] public float volumenMusicaConJefe = 1f;

        [Tooltip("Capa de tension: misma duracion que musica, arranca en el mismo dspTime en loop y a volumen 0.")]
        public AudioClip tension;

        [Tooltip("Volumen de la tension con la horda encima (0-1). Lejos, siempre 0; en el medio, sube de a poco.")]
        [Range(0f, 1f)] public float volumenTension = 1f;

        [Tooltip("La tension es un loop propio (percusion, coro) que no tiene que ir a tiempo con el tema: " +
                 "puede durar distinto sin aviso de desfasaje.")]
        public bool tensionLibre;

        [Tooltip("Tema del jefe: arranca desde su comienzo cuando el jefe esta por aparecer, se suma al tema del nivel " +
                 "en loop y se va al derrotarlo. Vacio: sin tema del jefe. No tiene que durar lo mismo que el tema.")]
        public AudioClip jefe;

        [Tooltip("Volumen del tema del jefe (0-1).")]
        [Range(0f, 1f)] public float volumenJefe = 0.6f;

        [Tooltip("Metros antes de que entre el jefe en que arranca su tema y el del nivel empieza a bajar. " +
                 "El jefe tarda un par de segundos mas en verse: llega desde atras.")]
        [Min(1f)] public float metrosAntesDelJefe = 200f;

        [Tooltip("Loop de ambiente del nivel, por el bus Ambiente.")]
        public Sonido ambiente;

        [Tooltip("Pisa al banco global para las claves que define.")]
        public BancoDeSonidos banco;
    }
}
