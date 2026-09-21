using UnityEngine;

namespace Zombineta.Audio
{
    /// <summary>Los tres numeros de SonidoEspacial, editables sin tocar codigo.</summary>
    [CreateAssetMenu(menuName = "Zombineta/Audio/Espacial", fileName = "Espacial")]
    public sealed class EspacialAsset : ScriptableObject
    {
        public EspacialConfig config = new EspacialConfig { anchoPaneo = 12f, distanciaPlena = 4f, distanciaMaxima = 30f };
    }
}
