using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zombineta.Juego.Screens;

namespace Zombineta.Juego.Tests
{
    /// <summary>El nombre del juego se ve con el logo, no como texto, en todas las pantallas que lo muestran.</summary>
    public class LogoDelJuegoTests
    {
        const string RutaLogo = "Assets/_Zombineta/Art/UI/sprite_logo_deadlivery_1.png";
        const string RutaMenu = "Assets/_Zombineta/Scenes/MainMenu.unity";
        // El fondo del menu principal ya trae el logo dibujado.
        const string RutaFondoMenu = "Assets/_Zombineta/Art/MenuInicio/menu_fondo_v2.png";

        [Test]
        public void ElMenuPrincipalMuestraElLogoYNoElTextoViejo()
        {
            var yaCargada = SceneManager.GetSceneByPath(RutaMenu);
            bool estaba = yaCargada.isLoaded;
            var escena = estaba ? yaCargada : EditorSceneManager.OpenScene(RutaMenu, OpenSceneMode.Additive);
            try
            {
                var logo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaFondoMenu);
                bool hayLogo = false;
                foreach (var raiz in escena.GetRootGameObjects())
                {
                    foreach (var imagen in raiz.GetComponentsInChildren<Image>(true))
                        hayLogo |= imagen.sprite == logo;
                    foreach (var texto in raiz.GetComponentsInChildren<Text>(true))
                        StringAssert.DoesNotContain("DEAD", texto.text.ToUpperInvariant());
                }
                Assert.IsTrue(hayLogo, "el menu principal no tiene el logo");
            }
            finally
            {
                if (!estaba)
                    EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void LaPortadaDeLosCreditosEsElLogo()
        {
            var creditos = AssetDatabase.LoadAssetAtPath<Creditos>("Assets/_Zombineta/Settings/Creditos.asset");
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Sprite>(RutaLogo), creditos.secciones[0].imagen);
        }

        [Test]
        public void ElIconoDeLaAplicacionEsElLogo()
        {
            var iconos = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(RutaLogo), iconos[0]);
        }
    }
}
