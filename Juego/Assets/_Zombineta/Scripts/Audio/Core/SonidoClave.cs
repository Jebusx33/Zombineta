namespace Zombineta.Audio
{
    /// <summary>Cada sonido que el juego sabe pedir. Agregar al final: los bancos lo guardan como int.</summary>
    public enum SonidoClave
    {
        Disparo, SinBalas, CambioCarril, Choque, Atropello, ExplosionBarril,
        PickupNafta, PickupBateria, PickupMunicion, FaroOn, FaroOff, SinNafta,
        Salto, Aterrizaje, AterrizajePerfecto, Victoria, Derrota,
        Motor, Horda, ZombieAdelante,
        UiMover, UiConfirmar, UiVolver,
        // Pelea a pie: cada golpe de la cadena, el remate, el golpe al aire y recibir un golpe.
        Golpe1, Golpe2, Remate, GolpeAlAire, Lastimada,
    }
}
