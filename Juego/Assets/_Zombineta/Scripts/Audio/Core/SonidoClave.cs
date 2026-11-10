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
        // Motor en capas: el ralenti (loop, moto quieta) y el cambio (al entrar en turbo).
        MotorRalenti, MotorCambio,
        // Recoger una granada o una bomba temporizadora (el arsenal que se lleva a la pelea).
        PickupGranada, PickupBomba,
        // Las armas: la explosion de la granada, los pitidos de la bomba puesta y su explosion.
        GranadaExplota, BombaPuesta, BombaExplota,
        // Avisos del HUD: la nafta o la bateria entran en el tramo bajo.
        HudNaftaBaja, HudBateriaBaja,
        // El mundo: se corta la luz del apagon, vuelve, y las alarmas de los autos abandonados.
        ApagonEmpieza, ApagonTermina, AlarmaDeAuto,
        // La bala que le pega a un zombie (aunque lo mate).
        ImpactoDeBala,
        // Loops que siguen al clima del tramo y al faro titilando con la bateria baja (LoopsDeNivel).
        Lluvia, Viento, FaroTitila,
        // Una vez por partida, al arrancar a correr (no al volver a la moto despues de una pelea).
        InicioDeNivel,

        // El jefe: fija el carril, dispara, cae el proyectil, le pegan aturdido, rebota un tiro, cae derrotado,
        // y el loop mecanico de sus orugas (OrugasDelJefe), que acelera en la embestida.
        JefeApunta, JefeDispara, JefeImpacto, JefeHerido, JefeRebota, JefeDerrotado, JefeOrugas,

        // La tele del mapa se prende (PantallaCRT): suena con el punto blanco del encendido.
        MapaEncendido,
    }
}
