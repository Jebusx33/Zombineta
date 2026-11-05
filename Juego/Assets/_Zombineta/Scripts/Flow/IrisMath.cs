using UnityEngine;

namespace Zombineta.Juego.Flow
{
    /// <summary>Las cuentas de la transicion de iris, sin Unity de por medio: se pueden testear sueltas.</summary>
    public static class IrisMath
    {
        /// <summary>
        /// Radio que tapa toda la pantalla desde ese centro, en altos de pantalla: la distancia a la
        /// esquina mas lejana. El centro va de 0 a 1 en cada eje; aspecto es ancho / alto.
        /// </summary>
        public static float RadioParaCubrir(Vector2 centro, float aspecto)
        {
            float dx = Mathf.Max(centro.x, 1f - centro.x) * aspecto;
            float dy = Mathf.Max(centro.y, 1f - centro.y);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Radio para un grado de cierre: 0 = abierto del todo, 1 = cerrado. Se frena al final de cada
        /// lado, y el ultimo tramo antes de cerrar va mas lento para que se vea el circulo chico.
        /// </summary>
        public static float Radio(float cierre, float radioAbierto)
        {
            float t = Mathf.Clamp01(cierre);
            float suave = t * t * (3f - 2f * t);
            return radioAbierto * Mathf.Pow(1f - suave, 1.6f);
        }

        /// <summary>El centro en pantalla (0 a 1) de un punto del mundo, sin salirse de los bordes.</summary>
        public static Vector2 CentroEnPantalla(Vector3 viewport)
        {
            return new Vector2(Mathf.Clamp(viewport.x, 0.05f, 0.95f), Mathf.Clamp(viewport.y, 0.05f, 0.95f));
        }
    }
}
