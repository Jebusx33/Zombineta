using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Zombineta.Luz
{
    /// <summary>
    /// El faro de un auto abandonado que sigue con bateria: un destello sobre cada optica del dibujo
    /// y un haz corto en el piso. Sale solo en algunos autos, al azar. Prende fijo y cada tanto
    /// falla unos instantes: asi se lee como un faro roto y no como un error de la luz.
    /// Los destellos son sprites sin luz: una Light2D sobre el auto ilumina la chapa, no la optica.
    /// </summary>
    public sealed class FaroRoto : MonoBehaviour
    {
        [Tooltip("Que parte de los autos tiene el faro prendido.")]
        [Range(0f, 1f)] [SerializeField] float probabilidad = 0.2f;

        [Tooltip("Segundos con el faro firme entre una falla y la siguiente (al azar entre los dos).")]
        [SerializeField] Vector2 entreFallas = new Vector2(1.5f, 5f);

        [Tooltip("Cuanto dura cada falla (al azar entre los dos).")]
        [SerializeField] Vector2 duracionFalla = new Vector2(0.15f, 0.6f);

        [Tooltip("Cortes por segundo durante una falla.")]
        [SerializeField] float cortes = 16f;

        [Tooltip("Brillo que queda en lo peor de una falla (0-1).")]
        [Range(0f, 1f)] [SerializeField] float brilloEnFalla = 0.1f;

        [SerializeField] Light2D haz;
        [SerializeField] SpriteRenderer[] destellos;

        float intensidadHaz;
        float[] alfas;
        float proximoCambio;
        bool fallando;

        public float Probabilidad => probabilidad;
        public Light2D Haz => haz;
        public SpriteRenderer[] Destellos => destellos;

        void Awake()
        {
            if (Random.value >= probabilidad)
            {
                Destroy(gameObject);
                return;
            }

            intensidadHaz = haz.intensity;
            alfas = new float[destellos.Length];
            for (int i = 0; i < destellos.Length; i++)
                alfas[i] = destellos[i].color.a;
            proximoCambio = Time.time + Random.Range(entreFallas.x, entreFallas.y);
        }

        void Start()
        {
            // El orden del auto lo pone el nivel segun el carril: los destellos van justo encima.
            var auto = transform.parent != null ? transform.parent.GetComponentInParent<SpriteRenderer>() : null;
            if (auto == null)
                return;
            foreach (var d in destellos)
            {
                d.sortingLayerID = auto.sortingLayerID;
                d.sortingOrder = auto.sortingOrder + 1;
            }
        }

        void Update()
        {
            if (Time.time >= proximoCambio)
            {
                fallando = !fallando;
                var espera = fallando ? duracionFalla : entreFallas;
                proximoCambio = Time.time + Random.Range(espera.x, espera.y);
            }

            float brillo = fallando && Mathf.Repeat(Time.time * cortes, 1f) < 0.5f ? brilloEnFalla : 1f;

            haz.intensity = intensidadHaz * brillo;
            for (int i = 0; i < destellos.Length; i++)
            {
                var c = destellos[i].color;
                c.a = alfas[i] * brillo;
                destellos[i].color = c;
            }
        }
    }
}
