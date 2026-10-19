namespace Zombineta.Juego.Screens
{
    public sealed class GameOverScreen : ScreenBase
    {
        // Migas de pan (solo editor): si cargar esta pantalla se traba, el Editor.log dice si llego a prenderse.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        static void Traza(string mensaje) => UnityEngine.Debug.Log("[GameOver] " + mensaje + " (t=" + UnityEngine.Time.realtimeSinceStartup.ToString("0.0") + ")");

        void Awake() => Traza("Awake");
        void OnEnable() => Traza("OnEnable");
        void Start() => Traza("Start");

        public void Retry() => Flow?.Retry();

        public void ToMenu() => Flow?.ToMainMenu();
    }
}
