using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombineta.Level;
using Zombineta.Scenery;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// La llegada se apoya encima de la calle del escenario: si la capa Calle cambia de alto o de base,
    /// sus lineas dejan de calzar con las de la calle y se ve el parche.
    /// </summary>
    public class GoalViewTests
    {
        // Filas de la raya de arriba y de la de abajo de la calle en su dibujo (tile_pista_comun), de 1124 de alto.
        const float FilaArribaDeLaCalle = 5f;
        const float FilaAbajoDeLaCalle = 765f;

        [TestCase("Assets/_Zombineta/Settings/Escenario.asset")]
        [TestCase("Assets/_Zombineta/Settings/Escenario_Nivel2.asset")]
        public void LosBordesDeLaLlegadaSonLosDeLaCalle(string ruta)
        {
            var tileset = AssetDatabase.LoadAssetAtPath<SceneryTileset>(ruta);
            Assert.IsNotNull(tileset, ruta);
            var calle = tileset.layers.Find(l => l.name == "Calle");
            Assert.IsNotNull(calle, "falta la capa Calle en " + ruta);

            foreach (var v in calle.variants)
            {
                var sprite = v.prefab.GetComponent<SpriteRenderer>().sprite;
                float alto = sprite.rect.height;
                Assert.AreEqual(1124f, alto, sprite.name + ": cambio el dibujo de la calle, hay que medir sus bordes de nuevo");

                float arriba = calle.baselineY + v.yOffset + calle.height * v.heightScale * (alto - FilaArribaDeLaCalle) / alto;
                float abajo = calle.baselineY + v.yOffset + calle.height * v.heightScale * (alto - FilaAbajoDeLaCalle) / alto;
                Assert.AreEqual(GoalView.YBordeDeArriba, arriba, 0.005f, sprite.name + " en " + ruta);
                Assert.AreEqual(GoalView.YBordeDeAbajo, abajo, 0.005f, sprite.name + " en " + ruta);
            }
        }

        [TestCase("Level_01")]
        [TestCase("Level_02")]
        [TestCase("Tutorial")]
        [TestCase("Templates/NivelBase")]
        public void CadaEscenaTraeElDibujoDeLaLlegada(string nombre)
        {
            string ruta = "Assets/_Zombineta/Scenes/" + nombre + ".unity";
            var yaCargada = SceneManager.GetSceneByPath(ruta);
            bool estaba = yaCargada.isLoaded;
            var escena = estaba ? yaCargada : EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
            try
            {
                GoalView meta = null;
                foreach (var raiz in escena.GetRootGameObjects())
                    if (meta == null)
                        meta = raiz.GetComponentInChildren<GoalView>(true);
                Assert.IsNotNull(meta, "falta la llegada en " + nombre);

                var campos = new SerializedObject(meta);
                foreach (var campo in new[] { "run", "gate", "dibujo", "conosDeAdelante" })
                    Assert.IsNotNull(campos.FindProperty(campo).objectReferenceValue, campo + " en " + nombre);

                // Los conos de adelante se superponen al dibujo sin moverlos: tienen que medir lo mismo.
                var dibujo = (Sprite)campos.FindProperty("dibujo").objectReferenceValue;
                var conos = (Sprite)campos.FindProperty("conosDeAdelante").objectReferenceValue;
                Assert.AreEqual(dibujo.rect.size, conos.rect.size);
                Assert.AreEqual(dibujo.pixelsPerUnit, conos.pixelsPerUnit);
                Assert.AreEqual(dibujo.bounds.min, conos.bounds.min);
            }
            finally
            {
                if (!estaba)
                    EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
