using UnityEngine;
using UnityEngine.UI;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Screens
{
    public sealed class LevelCompleteScreen : ScreenBase
    {
        [SerializeField] Text detail;

        void Start()
        {
            var level = GameRoot.Instance != null ? GameRoot.Instance.CurrentLevel : null;
            if (detail != null && level != null)
                detail.text = level.displayName + " superado";
        }

        public void Continue() => Flow?.Continue();
    }
}
