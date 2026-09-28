namespace Zombineta.Audio
{
    /// <summary>
    /// Volumen base de un loop (Motor, Horda, ambiente): el punto medio fijo del rango
    /// `Sonido.volumen`, resuelto una sola vez al arrancar el loop. No se sortea por cuadro como
    /// en un efecto de un solo disparo, porque en un loop un valor al azar por cuadro se notaria
    /// como parpadeo.
    /// </summary>
    public static class SonidoVolumen
    {
        public static float VolumenBase(Sonido s) =>
            s != null ? (s.volumen.x + s.volumen.y) * 0.5f : 1f;
    }
}
