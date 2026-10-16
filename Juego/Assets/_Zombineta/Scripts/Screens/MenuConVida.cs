using UnityEngine;
using UnityEngine.UI;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Movimiento del menu principal: la niebla que se desliza, el faro de la moto que titila
    /// y, cada tanto, un parpadeo de luz como de un poste fallando. Solo anima lo que ya esta
    /// armado en la escena (ver MenuConVidaBuilder). Tiempo sin escalar: anda igual con la pausa.
    /// </summary>
    public sealed class MenuConVida : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Niebla
        {
            public RawImage imagen;
            [Tooltip("Vueltas de textura por segundo: negativo va hacia la izquierda.")]
            public float velocidad = -0.01f;
            [Tooltip("Cuanto respira el alfa (0 = fijo).")]
            [Range(0f, 1f)] public float respiro = 0.25f;
            public float periodo = 9f;

            [System.NonSerialized] public float alfaBase = -1f;
        }

        [SerializeField] Niebla[] nieblas = new Niebla[0];

        [Header("Faro")]
        [SerializeField] Graphic haz;
        [SerializeField] Graphic brilloFaro;
        [Tooltip("Cuanto tiembla la intensidad del faro (0 = fijo).")]
        [Range(0f, 1f)] [SerializeField] float temblor = 0.18f;

        [Header("Parpadeo")]
        [Tooltip("Velo de luz de toda la pantalla que aparece en cada parpadeo.")]
        [SerializeField] Graphic destello;
        [SerializeField] Vector2 cadaCuanto = new Vector2(5f, 11f);
        [SerializeField] float alfaDestello = 0.22f;

        float alfaHaz = -1f, alfaBrillo = -1f;
        float proximo;
        float parpadeo; // segundos que le quedan al parpadeo en curso

        void OnEnable()
        {
            proximo = Time.unscaledTime + Random.Range(cadaCuanto.x, cadaCuanto.y);
            if (destello != null)
                Alfa(destello, 0f);
        }

        void Update()
        {
            float t = Time.unscaledTime;

            foreach (var n in nieblas)
            {
                if (n == null || n.imagen == null)
                    continue;
                if (n.alfaBase < 0f)
                    n.alfaBase = n.imagen.color.a;

                var uv = n.imagen.uvRect;
                uv.x = Mathf.Repeat(uv.x + n.velocidad * Time.unscaledDeltaTime, 1f);
                n.imagen.uvRect = uv;

                float onda = Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(0.1f, n.periodo));
                Alfa(n.imagen, n.alfaBase * (1f - n.respiro * 0.5f * (1f + onda)));
            }

            // El faro: un temblor suave de motor en marcha, con algun bajon corto.
            float ruido = Mathf.PerlinNoise(t * 3.1f, 0.37f);
            float bajon = Mathf.PerlinNoise(t * 0.7f, 5.1f) > 0.82f ? 0.55f : 1f;
            float faro = (1f - temblor + temblor * ruido) * bajon;
            if (haz != null)
            {
                if (alfaHaz < 0f) alfaHaz = haz.color.a;
                Alfa(haz, alfaHaz * faro);
            }
            if (brilloFaro != null)
            {
                if (alfaBrillo < 0f) alfaBrillo = brilloFaro.color.a;
                Alfa(brilloFaro, alfaBrillo * faro);
            }

            // Parpadeo: dos golpes de luz rapidos y se apaga.
            if (destello != null)
            {
                if (parpadeo <= 0f && t >= proximo)
                {
                    parpadeo = 0.45f;
                    proximo = t + Random.Range(cadaCuanto.x, cadaCuanto.y);
                }
                if (parpadeo > 0f)
                {
                    parpadeo -= Time.unscaledDeltaTime;
                    float fase = 0.45f - parpadeo;
                    bool encendido = fase < 0.06f || (fase > 0.14f && fase < 0.24f);
                    Alfa(destello, encendido ? alfaDestello : 0f);
                    if (parpadeo <= 0f)
                        Alfa(destello, 0f);
                }
            }
        }

        static void Alfa(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }
    }
}
