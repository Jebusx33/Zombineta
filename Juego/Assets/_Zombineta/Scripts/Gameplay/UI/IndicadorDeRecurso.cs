using UnityEngine;

namespace Zombineta.UI
{
    /// <summary>
    /// Como se ve un recurso (nafta o bateria) segun cuanto queda: hasta la mitad conserva su color,
    /// de ahi va virando y en el ultimo tramo queda en el color de alerta y titila.
    /// </summary>
    public static class IndicadorDeRecurso
    {
        /// <summary>Desde esta fraccion el recurso esta bajo: titila y avisa con sonido.</summary>
        public const float Bajo = 0.2f;

        const float EmpiezaACambiar = 0.5f;
        const float TitileosPorSegundo = 3f;

        public static bool EstaBajo(float fraccion) => fraccion <= Bajo;

        public static Color ColorDe(float fraccion, Color lleno, Color alerta) =>
            Color.Lerp(alerta, lleno, Mathf.InverseLerp(Bajo, EmpiezaACambiar, fraccion));

        /// <summary>1 si se ve pleno; en el tramo bajo alterna con 'apagado' para titilar.</summary>
        public static float Brillo(float fraccion, float segundos, float apagado)
        {
            if (!EstaBajo(fraccion))
                return 1f;
            return Mathf.Repeat(segundos * TitileosPorSegundo, 1f) < 0.5f ? 1f : apagado;
        }
    }
}
