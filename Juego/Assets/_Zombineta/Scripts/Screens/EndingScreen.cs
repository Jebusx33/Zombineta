namespace Zombineta.Juego.Screens
{
    public sealed class EndingScreen : ScreenBase
    {
        public void ToMenu() => Flow?.ToMainMenu();
    }
}
