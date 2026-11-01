using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zombineta.Juego.Screens;

namespace Zombineta.Juego.Tests
{
    public class TextoDeVersionTests
    {
        const string RutaMenu = "Assets/_Zombineta/Scenes/MainMenu.unity";

        [Test]
        public void LaVersionSaleDeProjectSettings()
        {
            Assert.AreEqual("v" + PlayerSettings.bundleVersion, TextoDeVersion.Version);
        }

        [Test]
        public void ElPieDelMenuPrincipalMuestraLaVersion()
        {
            var yaCargada = SceneManager.GetSceneByPath(RutaMenu);
            bool estaba = yaCargada.isLoaded;
            var escena = estaba ? yaCargada : EditorSceneManager.OpenScene(RutaMenu, OpenSceneMode.Additive);
            try
            {
                TextoDeVersion version = null;
                foreach (var raiz in escena.GetRootGameObjects())
                {
                    if (version == null)
                        version = raiz.GetComponentInChildren<TextoDeVersion>(true);
                    foreach (var texto in raiz.GetComponentsInChildren<Text>(true))
                        StringAssert.DoesNotContain("Esqueleto", texto.text);
                }
                Assert.IsNotNull(version, "el pie del menu no tiene TextoDeVersion");
            }
            finally
            {
                if (!estaba)
                    EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
