using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Zombineta.Juego.Levels;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// HordeGlow mueve su transform cada cuadro para seguir a la horda. Si queda en el objeto
    /// "Nivel" (el que tiene LevelScene), arrastra todo el recorrido y los items nunca entran en
    /// pantalla. Esta prueba abre cada nivel y exige que el resplandor viva en un objeto propio,
    /// sin hijos.
    /// </summary>
    public class HordeGlowSceneTests
    {
        [TestCase("Assets/_Zombineta/Scenes/Level_01.unity")]
        [TestCase("Assets/_Zombineta/Scenes/Level_02.unity")]
        [TestCase("Assets/_Zombineta/Scenes/Templates/NivelBase.unity")]
        public void ElResplandorNoArrastraElNivel(string path)
        {
            var yaCargada = SceneManager.GetSceneByPath(path);
            var escena = yaCargada.isLoaded ? yaCargada : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                int encontrados = 0;
                foreach (var raiz in escena.GetRootGameObjects())
                {
                    foreach (var glow in raiz.GetComponentsInChildren<HordeGlow>(true))
                    {
                        encontrados++;
                        Assert.IsNull(glow.GetComponent<LevelScene>(), $"{path}: HordeGlow comparte objeto con LevelScene");
                        Assert.AreEqual(0, glow.transform.childCount, $"{path}: el objeto de HordeGlow tiene hijos que arrastraria");
                    }
                }
                Assert.AreEqual(1, encontrados, $"{path}: tiene que haber exactamente un HordeGlow");
            }
            finally
            {
                if (!yaCargada.isLoaded)
                    EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
