using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zombineta.Juego.Flow
{
    /// <summary>
    /// Play desde cualquier escena: si Boot no esta cargada, la carga al lado sin descargar la
    /// que se estaba mirando. Asi arte prueba su pantalla sin recorrer todo el juego.
    /// </summary>
    public static class Bootstrapper
    {
        /// <summary>La escena desde la que se dio Play.</summary>
        public static string EntryScene { get; private set; }

        // La recarga de dominio esta desactivada: los estaticos sobreviven entre sesiones de Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => EntryScene = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureBoot()
        {
            EntryScene = SceneManager.GetActiveScene().name;
            if (SceneManager.GetSceneByName(SceneNames.Boot).isLoaded)
                return;
            SceneManager.LoadScene(SceneNames.Boot, LoadSceneMode.Additive);
        }
    }
}
