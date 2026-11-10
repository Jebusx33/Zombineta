using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zombineta.Juego.Levels;
using Zombineta.Level;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// El zombie parado en el camino mide en juego lo mismo que en el editor. Antes, al dar Play se
    /// respetaba la escala guardada en la escena: el tutorial tenia zombies de 4,75 (una escala de
    /// cuando las hojas eran otras) y en el editor se veian de 1,9.
    /// </summary>
    public class ZombiesDeFrenteTests
    {
        [Test]
        public void AlEntrarEnPlayLaEscalaGuardadaNoManda()
        {
            var sc = EditorSceneManager.OpenScene("Assets/_Zombineta/Scenes/Level_01.unity", OpenSceneMode.Additive);
            try
            {
                LevelScene nivel = null;
                foreach (var raiz in sc.GetRootGameObjects())
                    if (nivel == null)
                        nivel = raiz.GetComponentInChildren<LevelScene>(true);
                Assert.IsNotNull(nivel);

                var items = nivel.Items;
                int indice = System.Array.FindIndex(items, i => i.kind == LevelEntryKind.ZombieFront);
                Assert.GreaterOrEqual(indice, 0, "el nivel 1 tiene zombies de frente");
                var item = items[indice];

                // La escala que pone el editor (paleta por tipo por look).
                nivel.ApplyVisual(item, indice);
                var esperada = item.transform.localScale;

                // Una escala vieja guardada en la escena...
                item.transform.localScale = new Vector3(4.75f, 4.75f, 1f);
                // ...y lo que hace LevelScene.Awake al entrar en Play.
                typeof(LevelScene).GetMethod("EnsurePlayLook", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(nivel, new object[] { item, indice });

                Assert.AreEqual(esperada.x, item.transform.localScale.x, 1e-4f);
                Assert.AreEqual(esperada.y, item.transform.localScale.y, 1e-4f);
            }
            finally
            {
                EditorSceneManager.CloseScene(sc, true);
            }
        }
    }
}
