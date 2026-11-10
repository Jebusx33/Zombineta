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
        [Tooltip("Cuadro (desde 0) en que el brazo ya apunta. El disparo arranca aca: el tiro sale al " +
                 "apretar, asi que no hay tiempo de mostrar como saca el arma.")]
        [UnityEngine.Serialization.FormerlySerializedAs("disparoRepiteDesde")]
        [Min(0)] public int cuadroDelTiro = 3;
        [Tooltip("Boca del arma en ese cuadro, en pixeles desde arriba a la izquierda del dibujo (mirando " +
                 "a la derecha). De ahi salen el fogonazo y la traza.")]
        public Vector2 bocaDelArma = new Vector2(449f, 95f);

        /// <summary>El cuadro en que apunta, o null si no hay dibujos de disparo.</summary>
        public Sprite CuadroDelTiro =>
            dispara != null && dispara.cuadros != null && dispara.cuadros.Length > 0
                ? dispara.cuadros[Mathf.Clamp(cuadroDelTiro, 0, dispara.cuadros.Length - 1)]
                : null;

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
