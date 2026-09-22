using UnityEngine;
using Zombineta.Juego.Flow;

namespace Zombineta.Audio
{
    /// <summary>
    /// Loop de ambiente del nivel (AudioDeNivel.ambiente), por el bus Ambiente. Entra con un
    /// fundido de 1 s; sale con el tema, es decir, cuando este objeto se apaga o destruye junto
    /// con la escena del nivel (no necesita fundido de salida propio). Hijo del prefab
    /// SonidoDeNivel.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(FuenteConBus))]
    public sealed class AmbienteSonido : MonoBehaviour
    {
        const float Fundido = 1f;

        AudioSource source;
        FuenteConBus fuente;
        float volumenObjetivo = 1f;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            fuente = GetComponent<FuenteConBus>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            fuente.bus = AudioBus.Ambiente;
            fuente.espacial = 1f;
        }

        void OnEnable()
        {
            var ambiente = AmbienteDelNivelActual();
            var clip = ambiente != null && ambiente.clips != null && ambiente.clips.Length > 0
                ? ambiente.clips[0] : null;

            source.clip = clip;
            source.pitch = ambiente != null ? Random.Range(ambiente.tono.x, ambiente.tono.y) : 1f;
            volumenObjetivo = ambiente != null ? Random.Range(ambiente.volumen.x, ambiente.volumen.y) : 1f;
            fuente.volumenPropio = 0f;

            if (clip != null)
                source.Play();
        }

        static Sonido AmbienteDelNivelActual()
        {
            var root = GameRoot.Instance;
            var nivel = root != null ? root.CurrentLevel : null;
            var audioDeNivel = nivel != null ? nivel.audio as AudioDeNivel : null;
            return audioDeNivel != null ? audioDeNivel.ambiente : null;
        }

        void Update()
        {
            if (source.clip == null)
                return;
            fuente.volumenPropio = Mathf.MoveTowards(fuente.volumenPropio, volumenObjetivo,
                Time.unscaledDeltaTime / Fundido);
        }
    }
}
