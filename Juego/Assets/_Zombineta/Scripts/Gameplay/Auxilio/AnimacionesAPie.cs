using UnityEngine;

namespace Zombineta.Auxilio
{
    /// <summary>Una animacion de la protagonista a pie: sus cuadros y a que ritmo pasan.</summary>
    [System.Serializable]
    public sealed class ClipAPie
    {
        public Sprite[] cuadros;
        [Min(1f)] public float cuadrosPorSegundo = 10f;
        [Tooltip("Vuelve a empezar al terminar. Si no, se queda en el ultimo cuadro.")]
        public bool enBucle;

        public float Duracion => cuadros.Length / cuadrosPorSegundo;

        public int Indice(float segundos)
        {
            int i = Mathf.FloorToInt(segundos * cuadrosPorSegundo);
            return enBucle ? i % cuadros.Length : Mathf.Min(i, cuadros.Length - 1);
        }
    }

    /// <summary>
    /// Los dibujos de la protagonista en la pelea a pie. Cada carpeta de Art/Player/APie es una
    /// animacion; todos los cuadros tienen el pivote en los pies, asi que cambian sin saltar.
    /// </summary>
    [CreateAssetMenu(menuName = "Zombineta/Animaciones a pie")]
    public sealed class AnimacionesAPie : ScriptableObject
    {
        [Tooltip("Alto de la protagonista parada, en pixeles del dibujo: con esto 'altoPeaton' fija su tamano en el mundo.")]
        [Min(1f)] public float pixelesDeAlto = 308f;

        public ClipAPie quieta = new ClipAPie { cuadrosPorSegundo = 6f, enBucle = true };
        public ClipAPie camina = new ClipAPie { cuadrosPorSegundo = 12f, enBucle = true };
        [Tooltip("1.º y 2.º golpe de la cadena.")]
        public ClipAPie golpeBajo = new ClipAPie { cuadrosPorSegundo = 18f };
        [Tooltip("El remate. El golpe se resuelve al apretar: si el ritmo baja, el brazo llega tarde.")]
        public ClipAPie golpeAlto = new ClipAPie { cuadrosPorSegundo = 20f };
        public ClipAPie lastimada = new ClipAPie { cuadrosPorSegundo = 12f };
        public ClipAPie derribada = new ClipAPie { cuadrosPorSegundo = 10f };
        [Tooltip("El disparo a pie: saca el arma y tira con el brazo estirado.")]
        public ClipAPie dispara = new ClipAPie { cuadrosPorSegundo = 16f };
        [Tooltip("Cuadro (desde 0) en que el brazo ya apunta. Si vuelve a disparar con el arma afuera, " +
                 "sigue desde aca en vez de sacarla otra vez.")]
        [Min(0)] public int disparoRepiteDesde = 3;

        public ClipAPie Clip(PoseAPie pose)
        {
            switch (pose)
            {
                case PoseAPie.Camina: return camina;
                case PoseAPie.GolpeBajo: return golpeBajo;
                case PoseAPie.GolpeAlto: return golpeAlto;
                case PoseAPie.Lastimada: return lastimada;
                case PoseAPie.Derribada: return derribada;
                case PoseAPie.Dispara: return dispara;
                default: return quieta;
            }
        }
    }
}
