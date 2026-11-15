using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zombineta.Juego.Screens;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// El game over arma su pagina de comic de a vinetas y recien despues muestra el menu: si falta
    /// una pieza, la secuencia se corta o el menu aparece de entrada y el Enter que adelanta una
    /// vineta aprieta Reintentar.
    /// </summary>
    public class GameOverSecuenciaTests
    {
        const string Ruta = "Assets/_Zombineta/Scenes/GameOver.unity";

        [Test]
        public void LaPantallaTraeLasCuatroVinetasYElMenuAparte()
        {
            var yaCargada = SceneManager.GetSceneByPath(Ruta);
            bool estaba = yaCargada.isLoaded;
            var escena = estaba ? yaCargada : EditorSceneManager.OpenScene(Ruta, OpenSceneMode.Additive);
            try
            {
                GameOverScreen pantalla = null;
                foreach (var raiz in escena.GetRootGameObjects())
                    if (pantalla == null)
                        pantalla = raiz.GetComponentInChildren<GameOverScreen>(true);
                Assert.IsNotNull(pantalla);

                var campos = new SerializedObject(pantalla);
                foreach (var campo in new[] { "menu", "ayudaDeLaSecuencia", "submitAction", "cancelAction", "firstSelected" })
                    Assert.IsNotNull(campos.FindProperty(campo).objectReferenceValue, campo);

                var paginas = campos.FindProperty("paginas");
                Assert.AreEqual(4, paginas.arraySize);
                for (int i = 0; i < paginas.arraySize; i++)
                {
                    var img = (Image)paginas.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.IsNotNull(img, "vineta " + (i + 1));
                    Assert.IsNotNull(img.sprite, "vineta " + (i + 1) + " sin dibujo");
                    StringAssert.EndsWith("gameover_page" + (i + 1) + ".png", AssetDatabase.GetAssetPath(img.sprite));
                }

                // Los botones viven dentro del grupo del menu: mientras esta apagado no se pueden apretar.
                var menu = (CanvasGroup)campos.FindProperty("menu").objectReferenceValue;
                var primero = (Selectable)campos.FindProperty("firstSelected").objectReferenceValue;
                Assert.IsTrue(primero.transform.IsChildOf(menu.transform), "Reintentar quedo fuera del menu");

                // Y las vinetas, detras del menu.
                var ultima = (Image)paginas.GetArrayElementAtIndex(3).objectReferenceValue;
                Assert.Less(ultima.transform.parent.GetSiblingIndex(), menu.transform.GetSiblingIndex());
            }
            finally
            {
                if (!estaba)
                    EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
