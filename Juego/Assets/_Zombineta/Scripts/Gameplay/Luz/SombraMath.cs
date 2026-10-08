using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>
    /// La cuenta de las sombras proyectadas. C# plano, sin escena: hacia donde cae la sombra de
    /// algo parado en el piso segun donde esta la luz, y como armar esa deformacion (una silueta
    /// acostada e inclinada) con dos Transform encadenados, que es lo unico que Unity deja
    /// hacerle a un SpriteRenderer.
    /// </summary>
    public static class SombraMath
    {
        /// <summary>
        /// A donde va a parar la punta de algo de 1 unidad de alto parado en "pies", alumbrado
        /// desde "luz". La sombra se aleja de la luz; cuanto mas lejos esta la luz, mas rasante
        /// llega y mas larga es la sombra. "aplastado" achica la parte vertical: el piso se ve
        /// en escorzo, no de frente.
        /// </summary>
        public static Vector2 Vector(Vector2 pies, Vector2 luz, float alcance, float largoCerca, float largoLejos,
            float aplastado)
        {
            var d = pies - luz;
            float distancia = d.magnitude;
            // Con la luz justo encima de los pies no hay direccion: cae hacia la camara.
            var direccion = distancia > 0.0001f ? d / distancia : Vector2.down;
            float largo = Mathf.Lerp(largoCerca, largoLejos, alcance > 0f ? Mathf.Clamp01(distancia / alcance) : 1f);
            return new Vector2(direccion.x * largo, direccion.y * largo * aplastado);
        }

        /// <summary>Cuanto pesa una luz sobre algo a esa distancia: 1 pegada, 0 en el borde de su alcance.</summary>
        public static float Peso(float distancia, float alcance, float fuerza)
        {
            if (alcance <= 0f)
                return 0f;
            float t = 1f - Mathf.Clamp01(distancia / alcance);
            return fuerza * t * t * (3f - 2f * t);
        }

        /// <summary>
        /// La sombra unica de algo alumbrado por varias luces: el promedio de lo que tira cada
        /// una (sumaVectores = suma de peso * vector), con la de ambiente ocupando el lugar que
        /// las luces dejan libre. Es continua: una luz que entra o sale del alcance lo hace con
        /// peso cero, asi que la sombra nunca salta.
        /// </summary>
        public static Vector2 Mezclar(Vector2 sumaVectores, float sumaPesos, Vector2 ambiente)
        {
            float pesoAmbiente = Mathf.Max(0f, 1f - sumaPesos);
            float total = sumaPesos + pesoAmbiente;
            return total > 0f ? (sumaVectores + pesoAmbiente * ambiente) / total : ambiente;
        }

        /// <summary>Que tan marcada queda: la de ambiente sola lejos de todo, a pleno bajo una luz.</summary>
        public static float Intensidad(float sumaPesos, float fuerzaAmbiente) =>
            Mathf.Lerp(fuerzaAmbiente, 1f, Mathf.Clamp01(sumaPesos));

        /// <summary>
        /// Descompone la matriz 2x2 [[a, b], [c, d]] en giro(fuera) * escala(sx, sy) * giro(dentro),
        /// en grados: el padre lleva el giro de afuera y la escala, el hijo el giro de adentro.
        /// La escala Y sale negativa cuando la matriz espeja (la sombra cae hacia la camara).
        /// </summary>
        public static void Descomponer(float a, float b, float c, float d,
            out float giroFuera, out Vector2 escala, out float giroDentro)
        {
            float e = (a + d) * 0.5f, f = (a - d) * 0.5f, g = (c + b) * 0.5f, h = (c - b) * 0.5f;
            float q = Mathf.Sqrt(e * e + h * h), r = Mathf.Sqrt(f * f + g * g);
            float a1 = Mathf.Atan2(g, f), a2 = Mathf.Atan2(h, e);

            escala = new Vector2(q + r, q - r);
            giroDentro = (a2 - a1) * 0.5f * Mathf.Rad2Deg;
            giroFuera = (a2 + a1) * 0.5f * Mathf.Rad2Deg;
        }
    }
}
