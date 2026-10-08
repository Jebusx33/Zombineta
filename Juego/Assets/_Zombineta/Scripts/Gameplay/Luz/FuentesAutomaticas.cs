using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>
    /// Para escenas armadas a mano (el taller de luz): toda Light2D que cuelgue de este objeto
    /// proyecta sombras, sin agregarle nada. En los niveles no hace falta: el escenario hace lo
    /// mismo con cada tile que arma. Sin Play revisa en cada cambio de la escena, asi una luz
    /// recien agregada empieza a proyectar al instante.
    /// </summary>
    [ExecuteAlways]
    public sealed class FuentesAutomaticas : MonoBehaviour
    {
        void OnEnable() => FuenteDeSombra.AsegurarEn(gameObject);

        void Update()
        {
            if (!Application.isPlaying)
                FuenteDeSombra.AsegurarEn(gameObject);
        }
    }
}
