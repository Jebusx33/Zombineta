using UnityEngine;
using Zombineta.Flow;

namespace Zombineta.Audio
{
    /// <summary>Un tema por pantalla. Opciones y Personaje pueden quedar vacias: siguen con el tema que venia.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Audio/Musica del juego", fileName = "Musica")]
    public sealed class MusicaDelJuego : ScriptableObject
    {
        public AudioClip menu;
        public AudioClip opciones;
        public AudioClip personaje;
        public AudioClip cinematica;
        public AudioClip nivelCompleto;
        public AudioClip gameOver;
        public AudioClip ending;
        public AudioClip creditos;

        [Tooltip("Segundos del fundido cruzado entre pantallas.")]
        public float fundido = 1.5f;

        /// <summary>El clip de cada pantalla. Playing y Paused devuelven null: los maneja el director (tema del nivel y ducking).</summary>
        public AudioClip Para(GameScreen pantalla)
        {
            switch (pantalla)
            {
                case GameScreen.MainMenu: return menu;
                case GameScreen.Options: return opciones;
                case GameScreen.CharacterSelect: return personaje;
                case GameScreen.Cinematic: return cinematica;
                case GameScreen.LevelComplete: return nivelCompleto;
                case GameScreen.GameOver: return gameOver;
                case GameScreen.Ending: return ending;
                case GameScreen.Credits: return creditos;
                default: return null; // Playing, Paused
            }
        }
    }
}
