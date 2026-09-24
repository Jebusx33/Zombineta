using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombineta.Audio;
using Zombineta.Core;
using Zombineta.Juego.Levels;
using Zombineta.Level;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// Abre Tutorial.unity (igual que HordeGlowSceneTests) y verifica que el recorrido armado a
    /// mano en la tarea 3 cumple lo pedido: RunController usa TutorialConfig, cada tipo de item
    /// aparece al menos 3 veces, el recorrido entra en 600-700 m, y el resplandor de la horda y
    /// el sonido del nivel son unicos y no cuelgan de "Nivel" (si colgaran, arrastrarian el
    /// recorrido entero al moverse, como documenta HordeGlowSceneTests).
    /// </summary>
    public class TutorialSceneTests
    {
        const string ScenePath = "Assets/_Zombineta/Scenes/Tutorial.unity";
        const string ConfigPath = "Assets/_Zombineta/Settings/TutorialConfig.asset";

        Scene escena;
        bool yaEstabaCargada;

        [SetUp]
        public void SetUp()
        {
            var yaCargada = SceneManager.GetSceneByPath(ScenePath);
            yaEstabaCargada = yaCargada.isLoaded;
            escena = yaEstabaCargada ? yaCargada : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            if (!yaEstabaCargada)
                EditorSceneManager.CloseScene(escena, true);
        }

        static T FindInScene<T>(Scene escena) where T : Component
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                var found = raiz.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            return null;
        }

        static List<T> FindAllInScene<T>(Scene escena) where T : Component
        {
            var result = new List<T>();
            foreach (var raiz in escena.GetRootGameObjects())
                result.AddRange(raiz.GetComponentsInChildren<T>(true));
            return result;
        }

        static GameConfig LoadTutorialConfig() =>
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);

        [Test]
        public void RunControllerUsaTutorialConfig()
        {
            var run = FindInScene<RunController>(escena);
            Assert.IsNotNull(run, "La escena tiene que tener un RunController");

            var config = LoadTutorialConfig();
            Assert.IsNotNull(config, "Tiene que existir " + ConfigPath);

            var field = typeof(RunController).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "RunController tiene que tener un campo 'config'");
            Assert.AreSame(config, field.GetValue(run), "RunController.config tiene que ser TutorialConfig");
        }

        [Test]
        public void LevelSceneUsaTutorialConfig()
        {
            var levelScene = FindInScene<LevelScene>(escena);
            Assert.IsNotNull(levelScene, "La escena tiene que tener un LevelScene");
            Assert.AreSame(LoadTutorialConfig(), levelScene.Config, "LevelScene.Config tiene que ser TutorialConfig");
        }

        [Test]
        public void CadaTipoDeItemApareceAlMenosTresVeces()
        {
            var levelScene = FindInScene<LevelScene>(escena);
            Assert.IsNotNull(levelScene);

            var counts = new Dictionary<LevelEntryKind, int>();
            foreach (var item in levelScene.Items)
            {
                counts.TryGetValue(item.kind, out int n);
                counts[item.kind] = n + 1;
            }

            foreach (var kind in new[]
                     {
                         LevelEntryKind.Ramp, LevelEntryKind.Fuel, LevelEntryKind.Battery,
                         LevelEntryKind.Ammo, LevelEntryKind.ZombieFront,
                     })
            {
                counts.TryGetValue(kind, out int n);
                Assert.GreaterOrEqual(n, 3, kind + " tiene que aparecer al menos 3 veces");
            }
        }

        [Test]
        public void ElRecorridoEntraEn600Y700Metros()
        {
            var levelScene = FindInScene<LevelScene>(escena);
            Assert.IsNotNull(levelScene);

            float goalDistance = (float)typeof(LevelScene)
                .GetField("goalDistance", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(levelScene);
            Assert.GreaterOrEqual(goalDistance, 600f, "LevelScene.goalDistance");
            Assert.LessOrEqual(goalDistance, 700f, "LevelScene.goalDistance");

            var config = LoadTutorialConfig();
            Assert.GreaterOrEqual(config.goalDistance, 600f, "TutorialConfig.goalDistance");
            Assert.LessOrEqual(config.goalDistance, 700f, "TutorialConfig.goalDistance");

            var layout = levelScene.Layout;
            foreach (var item in levelScene.Items)
            {
                float metros = item.Meters(layout);
                Assert.Less(metros, goalDistance, item.name + " tiene que estar antes del refugio");
            }
        }

        [Test]
        public void HayExactamenteUnHordeGlowYNoCuelgaDeNivel()
        {
            var levelScene = FindInScene<LevelScene>(escena);
            Assert.IsNotNull(levelScene);

            var glows = FindAllInScene<HordeGlow>(escena);
            Assert.AreEqual(1, glows.Count, "Tiene que haber exactamente un HordeGlow");

            var glow = glows[0];
            Assert.IsNull(glow.GetComponent<LevelScene>(), "HordeGlow no puede compartir objeto con LevelScene");
            Assert.IsFalse(
                glow.transform.IsChildOf(levelScene.transform),
                "HordeGlow no puede colgar de Nivel: arrastraria todo el recorrido al moverse");
        }

        [Test]
        public void HayExactamenteUnSonidoDeNivelYNoCuelgaDeNivel()
        {
            var levelScene = FindInScene<LevelScene>(escena);
            Assert.IsNotNull(levelScene);

            var sonidos = FindAllInScene<SonidoDeNivel>(escena);
            Assert.AreEqual(1, sonidos.Count, "Tiene que haber exactamente un SonidoDeNivel");

            Assert.IsFalse(
                sonidos[0].transform.IsChildOf(levelScene.transform),
                "SonidoDeNivel no puede colgar de Nivel");
        }
    }
}
