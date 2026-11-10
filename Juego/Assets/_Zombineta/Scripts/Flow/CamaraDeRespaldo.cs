using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zombineta.Juego.Flow
{
    /// <summary>
    /// Vive en Boot. Cada pantalla trae su camara, y entre que se descarga una y se carga la otra no queda
    /// ninguna: el editor muestra "No cameras rendering" y un build puede mostrar basura del ultimo
    /// cuadro. Esta camara pinta de negro y no dibuja nada (sin capas); se prende sola solo cuando no hay
    /// otra camara, asi no le cuesta nada a las pantallas.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CamaraDeRespaldo : MonoBehaviour
    {
        Camera propia;

        /// <summary>Si hace falta la camara de respaldo: ninguna otra camara prendida dibuja en pantalla.</summary>
        public static bool HaceFalta(Camera propia)
        {
            foreach (var c in Camera.allCameras)
                if (c != propia && c.targetTexture == null)
                    return false;
            return true;
        }

        void Awake()
        {
            propia = GetComponent<Camera>();
            propia.clearFlags = CameraClearFlags.SolidColor;
            propia.backgroundColor = Color.black;
            propia.cullingMask = 0;
            propia.depth = -100f;
            Revisar();
        }

        void OnEnable()
        {
            SceneManager.sceneUnloaded += AlDescargar;
            SceneManager.sceneLoaded += AlCargar;
        }

        void OnDisable()
        {
            SceneManager.sceneUnloaded -= AlDescargar;
            SceneManager.sceneLoaded -= AlCargar;
        }

        // Al descargarse una escena se revisa en el acto: puede pasar despues del LateUpdate y antes de
        // dibujar, y ese cuadro quedaria sin camara.
        void AlDescargar(Scene _) => Revisar();
        void AlCargar(Scene _, LoadSceneMode __) => Revisar();

        void LateUpdate() => Revisar();

        void Revisar()
        {
            if (propia != null)
                propia.enabled = HaceFalta(propia);
        }
    }
}
