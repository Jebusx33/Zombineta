using UnityEngine;

namespace Zombineta.Audio
{
    public enum CambioDeApagon { Nada, Empieza, Termina }

    /// <summary>
    /// En que cuadro se corta la luz y en cual vuelve, mirando la oscuridad que se ve (0 a 1). El
    /// corte suena apenas la luz empieza a bajar en serio; la vuelta, cuando ya recupero buena parte
    /// de lo que habia perdido. Se mide contra lo mas oscuro que llego ese apagon, asi un apagon a
    /// medias tambien tiene su vuelta. Si la luz vuelve de golpe (pelear en pleno apagon), suena la
    /// vuelta en ese momento.
    /// </summary>
    public sealed class CruceDeApagon
    {
        /// <summary>Oscuridad desde la que cuenta como corte de luz.</summary>
        public const float Corte = 0.25f;

        /// <summary>La luz volvio cuando la oscuridad baja a esta fraccion de lo mas oscuro del apagon.</summary>
        public const float Vuelta = 0.6f;

        bool apagado;
        float pico;

        /// <summary>
        /// Listo para el proximo corte. Despues de la vuelta la oscuridad sigue bajando un rato por
        /// encima de Corte: el proximo apagon cuenta recien cuando la luz volvio del todo.
        /// </summary>
        bool armado;

        /// <summary>Empieza de cero sin sonar: si ya arranca a oscuras, el corte no se escucha.</summary>
        public void Reiniciar(float oscuridad)
        {
            apagado = oscuridad >= Corte;
            armado = !apagado;
            pico = apagado ? oscuridad : 0f;
        }

        public CambioDeApagon Avanzar(float oscuridad)
        {
            if (!apagado)
            {
                if (oscuridad < Corte)
                {
                    armado = true;
                    return CambioDeApagon.Nada;
                }
                if (!armado)
                    return CambioDeApagon.Nada;
                apagado = true;
                armado = false;
                pico = oscuridad;
                return CambioDeApagon.Empieza;
            }

            pico = Mathf.Max(pico, oscuridad);
            if (oscuridad > pico * Vuelta)
                return CambioDeApagon.Nada;
            apagado = false;
            pico = 0f;
            return CambioDeApagon.Termina;
        }
    }
}
