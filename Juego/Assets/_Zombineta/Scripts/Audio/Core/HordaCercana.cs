using System;
using System.Collections.Generic;
using Zombineta.Enemies;

namespace Zombineta.Audio
{
    /// <summary>
    /// Donde suena la amenaza, en metros desde quien escucha (negativo = atras). En la calle es
    /// el frente de la horda que persigue a la moto; en la pelea a pie, el zombie vivo mas
    /// cercano a la protagonista, venga del lado que venga.
    /// </summary>
    public static class HordaCercana
    {
        /// <param name="enPelea">Si la protagonista esta peleando a pie.</param>
        /// <param name="oyenteX">Donde esta quien escucha: la moto, o la protagonista en la pelea.</param>
        /// <param name="frenteX">El frente de la horda (vale fuera de la pelea).</param>
        /// <param name="units">Los zombies de la pelea.</param>
        /// <param name="activo">Si el zombie de ese indice esta en la pelea.</param>
        /// <returns>dx en metros, o null en una pelea sin zombies vivos (silencio).</returns>
        public static float? Dx(bool enPelea, float oyenteX, float frenteX,
                                IReadOnlyList<ZombieUnit> units, Func<int, bool> activo)
        {
            if (!enPelea)
                return frenteX - oyenteX;

            float? mejor = null;
            if (units == null)
                return null;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null || !u.Alive || (activo != null && !activo(i)))
                    continue;
                float dx = u.X - oyenteX;
                if (!mejor.HasValue || Math.Abs(dx) < Math.Abs(mejor.Value))
                    mejor = dx;
            }
            return mejor;
        }
    }
}
