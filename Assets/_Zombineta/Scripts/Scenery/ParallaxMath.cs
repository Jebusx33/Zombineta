namespace Zombineta.Scenery
{
    /// <summary>
    /// Parallax por factor, relativo a la camara:
    ///   0   = fijo a la camara (infinitamente lejos: el cielo)
    ///   0-1 = detras de la calle, se mueve mas lento que el mundo (edificios)
    ///   1   = pegado al mundo (la calle: donde viven la moto, la horda y los obstaculos)
    ///   >1  = delante de la calle, pasa mas rapido que el mundo (capa frontal)
    ///
    /// La calle a factor 1 es lo que sincroniza el fondo con la jugadora: no se mueve
    /// "a la velocidad de la moto", directamente es el mismo espacio en el que la moto
    /// avanza. No hay nada que ajustar para que coincidan.
    /// </summary>
    public static class ParallaxMath
    {
        /// <summary>Posicion X de la raiz de una capa cuando la camara esta en cameraX.</summary>
        public static float LayerOriginX(float cameraX, float factor) => cameraX * (1f - factor);

        /// <summary>Centro, en coordenadas locales de la capa, de lo que la camara ve.</summary>
        public static float LocalViewCenter(float cameraX, float factor) => cameraX * factor;

        /// <summary>
        /// Velocidad en pantalla de algo apoyado en la capa, para una camara que se mueve a
        /// cameraVelocity. Negativa: el paisaje corre hacia la izquierda cuando se avanza.
        /// </summary>
        public static float ScreenVelocity(float cameraVelocity, float factor) =>
            -cameraVelocity * factor;
    }
}
