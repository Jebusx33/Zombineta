using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using Zombineta.UI;

namespace Zombineta.Juego.Tests
{
    /// <summary>Cada aviso de obstaculo queda centrado en su carril y del tamano del carril.</summary>
    public class AvisosDeCarrilTests
    {
        [Test]
        public void ElIconoMideLaFraccionDeCarrilPedida()
        {
            // Carriles a 65 px y un icono de 77 px: para medir 0,85 carriles se achica.
            Assert.AreEqual(65f * 0.85f / 77f, LaneWarningView.EscalaDelIcono(65f, 77f, 0.85f), 1e-5f);
        }

        [Test]
        public void SinMedidasNoCambiaElTamano()
        {
            Assert.AreEqual(1f, LaneWarningView.EscalaDelIcono(0f, 77f, 0.85f));
            Assert.AreEqual(1f, LaneWarningView.EscalaDelIcono(65f, 0f, 0.85f));
        }

        [TestCase("Level_01")]
        [TestCase("Level_02")]
        [TestCase("Tutorial")]
        public void LosAvisosVanCentradosEnSuCarril(string escena)
        {
            var path = "Assets/_Zombineta/Scenes/" + escena + ".unity";
            var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                int vistos = 0;
                foreach (var raiz in sc.GetRootGameObjects())
                    foreach (var v in raiz.GetComponentsInChildren<LaneWarningView>(true))
                    {
                        var so = new SerializedObject(v);
                        Assert.AreEqual(0f, so.FindProperty("alturaSobreCarril").floatValue, escena);
                        Assert.That(so.FindProperty("altoEnCarriles").floatValue, Is.LessThan(1f), escena);
                        vistos++;
                    }
                Assert.AreEqual(1, vistos, escena);
            }
            finally
            {
                EditorSceneManager.CloseScene(sc, true);
            }
        }
    }
}
