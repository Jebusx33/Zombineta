using UnityEngine;
using UnityEngine.UI;

namespace Zombineta.Juego.Screens
{
    /// <summary>Escribe en su Text la version del build, la de Project Settings: no hay que tocarla a mano.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class TextoDeVersion : MonoBehaviour
    {
        public static string Version => "v" + Application.version;

        void Awake() => GetComponent<Text>().text = Version;
    }
}
