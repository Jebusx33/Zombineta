namespace Zombineta.Juego.Screens
{
    public sealed class CharacterSelectScreen : ScreenBase
    {
        public void Choose(int index) => Flow?.ChooseCharacter(index);

        public void Back() => Flow?.Back();
    }
}
