using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Zombineta.Audio;
using Zombineta.Juego.Levels;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// SonidoDeNivel (y sus hijos MotorSonido, HordaSonido y AmbienteSonido) actualizan cosas
    /// cada cuadro igual que HordeGlow: si el prefab quedara colgado del objeto "Nivel" (el que
    /// tiene LevelScene) arrastraria todo el recorrido, igual que paso con el resplandor. Esta
    /// prueba abre cada escena de nivel y exige exactamente una instancia, fuera de "Nivel", con
    /// sus cuatro hijos.
    /// </summary>
    public class SonidoDeNivelSceneTests
    {
        [TestCase("Assets/_Zombineta/Scenes/Level_01.unity")]
        [TestCase("Assets/_Zombineta/Scenes/Level_02.unity")]
        [TestCase("Assets/_Zombineta/Scenes/Templates/NivelBase.unity")]
        public void ElSonidoDeNivelNoCuelgaDeNivel(string path)
        {
            var yaCargada = SceneManager.GetSceneByPath(path);
            var escena = yaCargada.isLoaded ? yaCargada : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                int encontrados = 0;
                foreach (var raiz in escena.GetRootGameObjects())
                {
                    foreach (var sonido in raiz.GetComponentsInChildren<SonidoDeNivel>(true))
                    {
                        encontrados++;
                        Assert.IsNull(sonido.GetComponent<LevelScene>(),
                            $"{path}: SonidoDeNivel comparte objeto con LevelScene");

                        Assert.IsNotNull(sonido.GetComponentInChildren<SfxDirector>(true),
                            $"{path}: falta el hijo SfxDirector");
                        Assert.IsNotNull(sonido.GetComponentInChildren<MotorSonido>(true),
                            $"{path}: falta el hijo MotorSonido");
                        Assert.IsNotNull(sonido.GetComponentInChildren<HordaSonido>(true),
                            $"{path}: falta el hijo HordaSonido");
                        Assert.IsNotNull(sonido.GetComponentInChildren<AmbienteSonido>(true),
                            $"{path}: falta el hijo AmbienteSonido");
                    }
                }
                Assert.AreEqual(1, encontrados, $"{path}: tiene que haber exactamente un SonidoDeNivel");
            }
            finally
            {
                if (!yaCargada.isLoaded)
                    EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
