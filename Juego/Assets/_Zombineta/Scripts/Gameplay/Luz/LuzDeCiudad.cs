using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Zombineta.Luz
{
    /// <summary>
    /// Una luz que depende de la red electrica: ventanas, faroles, el kiosco. Se va con el apagon
    /// y vuelve con el, asi el corte se lee en toda la ciudad y no solo en la luz ambiente.
    /// Toca el color y no la intensidad: un Flicker en el mismo objeto sigue titilando igual.
    /// No lleva DecorLight: el presupuesto la prenderia de nuevo en pleno apagon.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class LuzDeCiudad : MonoBehaviour
    {
        Light2D luz;
        Color color;

        void Awake()
        {
            luz = GetComponent<Light2D>();
            color = luz.color;
        }

        void Update()
        {
            float queda = 1f - Mathf.Clamp01(LightingDirector.Oscuridad);

            // Apagada del todo, y no solo negra: una luz prendida seguiria proyectando sombras.
            luz.enabled = queda > 0.01f;

            var atenuado = color * queda;
            atenuado.a = color.a;
            luz.color = atenuado;
        }
    }
}
