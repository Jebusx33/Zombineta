using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>
    /// En Game Over, Ending y Creditos: cada tanto (intervalo al azar, segundos reales) suena uno
    /// de la lista, por ejemplo un gemido lejano. Arranca esperando un intervalo entero antes del
    /// primero y nunca dispara dos superpuestos: el propio temporizador secuencial lo garantiza.
    /// </summary>
    public sealed class SonidosAleatorios : MonoBehaviour
    {
        [SerializeField] Sonido[] sonidos;
        [SerializeField] Vector2 intervalo = new Vector2(6f, 14f);

        float espera;
        int ultimoIndice = -1;

        void OnEnable() => Reprogramar();

        void Update()
        {
            if (sonidos == null || sonidos.Length == 0)
                return;

            espera -= Time.unscaledDeltaTime;
            if (espera > 0f)
                return;

            var director = AudioDirector.Instance;
            if (director == null)
            {
                Reprogramar();
                return;
            }

            director.Play(ElegirSonido());
            Reprogramar();
        }

        void Reprogramar() => espera = Random.Range(intervalo.x, intervalo.y);

        Sonido ElegirSonido()
        {
            if (sonidos.Length == 1)
            {
                ultimoIndice = 0;
                return sonidos[0];
            }

            int idx = Random.Range(0, sonidos.Length);
            if (idx == ultimoIndice)
                idx = (idx + 1) % sonidos.Length;
            ultimoIndice = idx;
            return sonidos[idx];
        }
    }
}
