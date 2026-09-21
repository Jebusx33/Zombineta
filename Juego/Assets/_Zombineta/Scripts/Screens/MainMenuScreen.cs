using UnityEngine;

namespace Zombineta.Juego.Screens
{
    public sealed class MainMenuScreen : ScreenBase
    {
        public void Play() => Flow?.Play();

        public void OpenOptions() => Flow?.OpenOptions();

        public void OpenCredits() => Flow?.OpenCredits();

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
