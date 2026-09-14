namespace Zombineta.Juego.Screens
{
    public sealed class GameOverScreen : ScreenBase
    {
        public void Retry() => Flow?.Retry();

        public void ToMenu() => Flow?.ToMainMenu();
    }
}
