using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>
    /// Cuanto suena la capa de musica del jefe (0 a 1): sube de a poco en los metros antes de que entre, queda
    /// llena durante el duelo y se va cuando cae derrotado. Sin jefe en el nivel, nada.
    /// </summary>
    public static class CapaDelJefe
    {
        public static float Intensidad(bool hayJefe, bool enDuelo, bool derrotado, float metro, float metroEntrada, float metrosAntes)
        {
            if (!hayJefe || derrotado)
                return 0f;
            if (enDuelo)
                return 1f;
            if (metrosAntes <= 0f)
                return metro >= metroEntrada ? 1f : 0f;
            return Mathf.Clamp01((metro - (metroEntrada - metrosAntes)) / metrosAntes);
        }
    }
}
