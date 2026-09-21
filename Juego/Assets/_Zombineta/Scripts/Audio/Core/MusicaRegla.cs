namespace Zombineta.Audio
{
    public enum MusicaAccion { Seguir, Cruzar, Silenciar }

    /// <summary>Que hace la musica ante un cambio de pantalla. Sin tema nuevo, sigue el que venia.</summary>
    public static class MusicaRegla
    {
        public static MusicaAccion Decidir(object actual, object nuevo)
        {
            if (nuevo == null || ReferenceEquals(actual, nuevo))
                return MusicaAccion.Seguir;
            return MusicaAccion.Cruzar;
        }
    }
}
