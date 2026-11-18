using NUnit.Framework;
using UnityEditor;
using Zombineta.Flow;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// Las cinematicas de entrada usan los dibujos definitivos: ninguna vineta de placeholder, y la
    /// del nivel 2 arma su pagina con las tres de Nivel02 (la pagina entera y dos recuadros encima).
    /// </summary>
    public class CinematicasTests
    {
        const string RutaNiveles = "Assets/_Zombineta/Settings/Niveles.asset";

        static LevelSequence Niveles() => AssetDatabase.LoadAssetAtPath<LevelSequence>(RutaNiveles);

        [Test]
        public void NingunaCinematicaUsaPlaceholders()
        {
            foreach (var nivel in Niveles().levels)
                foreach (var vineta in nivel.comicPanels)
                {
                    Assert.IsNotNull(vineta.image, nivel.displayName + " tiene una vineta sin dibujo");
                    StringAssert.DoesNotContain("/Placeholder/", AssetDatabase.GetAssetPath(vineta.image), nivel.displayName);
                }
        }

        [Test]
        public void ElNivel2ArmaUnaPaginaConSusTresVinetas()
        {
            var vinetas = Niveles().levels[1].comicPanels;
            Assert.AreEqual(3, vinetas.Count);
            for (int i = 0; i < vinetas.Count; i++)
            {
                Assert.AreEqual("Assets/_Zombineta/Art/Cinematicas/Nivel02/cutscene_" + (i + 1) + ".png",
                                AssetDatabase.GetAssetPath(vinetas[i].image));
                Assert.AreEqual(i == 0, ComicPages.OpensPage(vinetas, i), "vineta " + (i + 1));
            }
        }
    }
}
