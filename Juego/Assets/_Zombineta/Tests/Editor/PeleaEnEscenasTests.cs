using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombineta.Auxilio;
using Zombineta.Juego.Levels;
using Zombineta.UI;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// Cada vez que se acaba la nafta se pelea a pie, en todos los niveles. Una escena con la pelea prendida
    /// (<c>peleaSinNafta</c>) tiene que traer lo que se ve de ella: el HUD de la pelea, la protagonista a pie y el
    /// marco de las balas. Si falta alguno, la pelea correria "a ciegas" (sin protagonista ni carteles). El tutorial,
    /// que no tiene pelea, tiene que traerla apagada.
    /// </summary>
    public class PeleaEnEscenasTests
    {
        const string Carpeta = "Assets/_Zombineta/Scenes/";

        static readonly string[] NivelesConPelea =
        {
            "Level_01_Auxilio_v2",
            "Level_02",
        };

        static T Buscar<T>(Scene escena) where T : Component
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                var found = raiz.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            return null;
        }

        static void ConEscena(string nombre, System.Action<Scene> verificar)
        {
            string ruta = Carpeta + nombre + ".unity";
            var yaCargada = SceneManager.GetSceneByPath(ruta);
            bool estaba = yaCargada.isLoaded;
            var escena = estaba ? yaCargada : EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
            try
            {
                verificar(escena);
            }
            finally
            {
                if (!estaba)
                    EditorSceneManager.CloseScene(escena, true);
            }
        }

        [TestCaseSource(nameof(NivelesConPelea))]
        public void ElNivel_PeleaAlQuedarseSinNafta_YTraeLaPeleaArmada(string nombre)
        {
            ConEscena(nombre, escena =>
            {
                var nivel = Buscar<LevelScene>(escena);
                Assert.IsNotNull(nivel, nombre + ": falta el LevelScene");
                Assert.IsTrue(nivel.PeleaSinNafta, nombre + ": todos los niveles se resuelven peleando cuando se acaba la nafta");
                Assert.IsNotNull(Buscar<HudPelea>(escena), nombre + ": falta el HudPelea (carteles, vida y oleadas)");
                Assert.IsNotNull(Buscar<VistaDelAuxilio>(escena), nombre + ": falta la VistaDelAuxilio (la protagonista a pie)");
                Assert.IsNotNull(Buscar<HudMunicionPelea>(escena), nombre + ": falta el HudMunicionPelea (el marco de las balas)");
            });
        }

        [Test]
        public void ElTutorial_NoTienePelea()
        {
            ConEscena("Tutorial", escena =>
            {
                var nivel = Buscar<LevelScene>(escena);
                Assert.IsNotNull(nivel);
                Assert.IsFalse(nivel.PeleaSinNafta, "el tutorial no se pelea: no tiene la pelea armada");
            });
        }
    }
}
