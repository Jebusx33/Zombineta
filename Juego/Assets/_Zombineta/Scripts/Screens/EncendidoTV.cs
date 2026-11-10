using UnityEngine;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Como se prende un televisor viejo, de 0 (apagado) a 1 (prendido): primero aparece una linea blanca
    /// que se estira de lado a lado, despues se abre en alto mostrando la imagen, y el brillo de mas se
    /// va apagando. Lo dibuja el shader CRT (Resources/Estilo/CRT) con lo que devuelve esto.
    /// </summary>
    public static class EncendidoTV
    {
        /// <summary>Hasta aca (fraccion del encendido) la linea se estira de lado a lado.</summary>
        public const float FinLinea = 0.2f;
        /// <summary>Hasta aca la imagen se abre en alto.</summary>
        public const float FinApertura = 0.65f;
        /// <summary>Alto de la linea, en fraccion del alto de la pantalla.</summary>
        public const float AltoLinea = 0.006f;

        public struct Estado
        {
            /// <summary>Ancho visible desde el centro (1 = toda la pantalla).</summary>
            public float ancho;
            /// <summary>Alto visible desde el centro (1 = toda la pantalla).</summary>
            public float alto;
            /// <summary>Cuanto se lava la imagen hacia el blanco (1 = blanco puro).</summary>
            public float brillo;
        }

        public static Estado En(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < FinLinea)
            {
                float k = SalidaSuave(t / FinLinea);
                return new Estado { ancho = Mathf.Lerp(0.02f, 1f, k), alto = AltoLinea, brillo = 1f };
            }
            if (t < FinApertura)
            {
                float k = SalidaSuave((t - FinLinea) / (FinApertura - FinLinea));
                return new Estado { ancho = 1f, alto = Mathf.Lerp(AltoLinea, 1f, k), brillo = Mathf.Lerp(1f, 0.25f, k) };
            }
            float f = (t - FinApertura) / (1f - FinApertura);
            return new Estado { ancho = 1f, alto = 1f, brillo = Mathf.Lerp(0.25f, 0f, SalidaSuave(f)) };
        }

        // Arranca rapido y frena al final.
        static float SalidaSuave(float x)
        {
            x = Mathf.Clamp01(x);
            float u = 1f - x;
            return 1f - u * u * u;
        }
    }
}
