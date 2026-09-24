using UnityEngine;
using Zombineta.Core;
using Zombineta.Juego.Flow;

namespace Zombineta.Audio
{
    /// <summary>
    /// Raiz del prefab del sonido de nivel: engancha el banco de sonidos del nivel actual al
    /// AudioDirector mientras esta activo y le pasa la posicion de la moto y la amenaza de la
    /// horda a MusicDirector cada cuadro. Vive suelto en la escena (nunca en "Nivel": ver la
    /// trampa de HordeGlow en HordeGlowSceneTests, que HordeGlow arrastraba todo el recorrido).
    /// Busca su RunController si no esta cableado: el prefab no puede guardar una referencia
    /// de escena. Orden de ejecucion -50 (por debajo de 0, por encima de AudioDirector/-900 y
    /// GameRoot/-1000): asi BancoNivel queda puesto antes de que MotorSonido/HordaSonido/
    /// AmbienteSonido (orden por defecto, 0) resuelvan sus clips en su propio OnEnable.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SonidoDeNivel : MonoBehaviour
    {
        [SerializeField] RunController run;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();
        }

        void OnEnable()
        {
            var audio = AudioDirector.Instance;
            if (audio == null)
                return;

            audio.BancoNivel = BancoDelNivelActual();
            audio.WorldUnitsPerMeter = run != null ? run.Config.worldUnitsPerMeter : 1f;
        }

        void OnDisable()
        {
            var audio = AudioDirector.Instance;
            if (audio != null)
                audio.BancoNivel = null;
        }

        void Update()
        {
            if (run == null || run.Sim == null)
                return;

            var state = run.Sim.State;

            var audio = AudioDirector.Instance;
            if (audio != null)
                audio.PlayerWorldX = run.ToWorldX(state.PlayerX);

            var music = MusicDirector.Instance;
            if (music != null)
                music.SetAmenaza(1f - Mathf.Clamp01(state.Gap / AudioDirector.Mezcla.distanciaAmenaza));
        }

        static BancoDeSonidos BancoDelNivelActual()
        {
            var root = GameRoot.Instance;
            var nivel = root != null ? root.CurrentLevel : null;
            var audioDeNivel = nivel != null ? nivel.audio as AudioDeNivel : null;
            return audioDeNivel != null ? audioDeNivel.banco : null;
        }
    }
}
