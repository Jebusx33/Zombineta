using UnityEngine;

namespace Zombineta.Jefe
{
    /// <summary>Una animacion del jefe: cada cuadro tiene su duracion y su factor de escala.</summary>
    [System.Serializable]
    public sealed class ClipDeJefe
    {
        public Sprite[] cuadros;
        public float[] segundos;
        [Tooltip("Compensa las diferencias de tamano entre dibujos. Va igual en X y en Y.")]
        public float[] escala;
        public bool enBucle;

        public float Duracion
        {
            get
            {
                float total = 0f;
                foreach (var s in segundos)
                    total += s;
                return total;
            }
        }

        /// <summary>El cuadro que toca a esos segundos del clip. Sin bucle se queda en el ultimo.</summary>
        public int Indice(float tiempo)
        {
            float t = enBucle ? Mathf.Repeat(tiempo, Duracion) : tiempo;
            for (int i = 0; i < cuadros.Length; i++)
            {
                if (t < segundos[i])
                    return i;
                t -= segundos[i];
            }
            return cuadros.Length - 1;
        }
    }

    /// <summary>
    /// Los dibujos del jefe y de su proyectil. Son poses clave: se arman desde el JSON del paquete de
    /// arte, que trae el orden, los tiempos y la escala de cada cuadro.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Animaciones del jefe")]
    public sealed class AnimacionesJefe : ScriptableObject
    {
        [Tooltip("La pose de persecucion, entre ataque y ataque.")]
        public ClipDeJefe quieto;
        public ClipDeJefe anticipacion;
        public ClipDeJefe ataqueFrontal;
        public ClipDeJefe ataqueElevado;
        public ClipDeJefe embestida;
        public ClipDeJefe aturdido;
        [Tooltip("Al final del aturdimiento, antes de volver a pelear.")]
        public ClipDeJefe recuperacion;
        public ClipDeJefe dano;
        public ClipDeJefe muerte;
        public ClipDeJefe proyectilVuelo;
        public ClipDeJefe proyectilImpacto;

        [Tooltip("Boca del brazo en la descarga del disparo frontal, desde el apoyo del jefe, en unidades de mundo.")]
        public Vector2 bocaFrontal = new Vector2(2.58f, 1.83f);
        [Tooltip("Boca del brazo en la descarga del disparo elevado.")]
        public Vector2 bocaElevado = new Vector2(1.68f, 3.23f);
    }
}
