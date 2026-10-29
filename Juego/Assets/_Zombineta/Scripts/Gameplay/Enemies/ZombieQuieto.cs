using UnityEngine;

namespace Zombineta.Enemies
{
    /// <summary>
    /// El zombie parado en el camino (ZombieFront): repite su ciclo de quieto en loop. Solo dibujo;
    /// la simulacion no sabe que se mueve. Cada uno arranca en un cuadro distinto para que una fila
    /// de zombies no respire todos a la vez.
    /// </summary>
    public sealed class ZombieQuieto : MonoBehaviour
    {
        SpriteRenderer destino;
        Sprite[] cuadros;
        float fps;
        float desfase;

        /// <param name="desfase">Cuadros que se adelanta el ciclo: lo que lo separa de los demas.</param>
        public void Iniciar(SpriteRenderer destino, Sprite[] cuadros, float fps, float desfase)
        {
            this.destino = destino;
            this.cuadros = cuadros;
            this.fps = fps;
            this.desfase = desfase;
        }

        void Update()
        {
            if (destino == null || cuadros == null || cuadros.Length == 0)
                return;

            int n = cuadros.Length;
            int cuadro = Mathf.FloorToInt(Time.time * fps + desfase) % n;
            var sprite = cuadros[cuadro];
            if (destino.sprite != sprite)
                destino.sprite = sprite;
        }
    }
}
