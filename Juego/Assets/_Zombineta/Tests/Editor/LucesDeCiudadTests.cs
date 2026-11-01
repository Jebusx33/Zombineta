using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Luz;
using Zombineta.Scenery;

namespace Zombineta.Juego.Tests
{
    public class LucesDeCiudadTests
    {
        const string Tiles = "Assets/_Zombineta/Art/Tileset/";

        [TestCase("Level1/Prefabs/tile_edificio_1")]
        [TestCase("Level1/Prefabs/tile_edificio_2")]
        [TestCase("Level1/Prefabs/tile_edificio_3")]
        [TestCase("Level2/Prefabs/tile_edificio_4")]
        [TestCase("Level2/Prefabs/tile_edificio_5")]
        public void LaLuzDeLasVentanasSoloLeLlegaAlFondoYSeVaConElApagon(string tile)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Tiles + tile + ".prefab");
            Light2D ventanas = null;
            foreach (var luz in prefab.GetComponentsInChildren<Light2D>(true))
                if (luz.lightType == Light2D.LightType.Sprite)
                    ventanas = luz;

            Assert.IsNotNull(ventanas, "el edificio no tiene luz de ventanas");
            Assert.IsNotNull(ventanas.lightCookieSprite, "la luz de ventanas no tiene mascara");
            Assert.AreEqual(new[] { SortingLayer.NameToID("Fondo") }, ventanas.targetSortingLayers);
            Assert.IsNotNull(ventanas.GetComponent<LuzDeCiudad>());
        }

        // La mascara tiene forma de ventana: lo que se dibuje delante en esa capa se pinta con ella.
        [TestCase("Escenario")]
        [TestCase("Escenario_Nivel2")]
        public void SoloLosEdificiosVanEnLaCapaFondo(string escenario)
        {
            var tileset = AssetDatabase.LoadAssetAtPath<SceneryTileset>("Assets/_Zombineta/Settings/" + escenario + ".asset");
            foreach (var capa in tileset.layers)
                if (capa.name != "Edificios")
                    Assert.AreNotEqual("Fondo", capa.sortingLayer, capa.name);
        }

        // Van en el paredon y no en los edificios: avanzan con la calle que tienen que alumbrar.
        [TestCase("tile_paredon_1")]
        [TestCase("tile_paredon_2")]
        [TestCase("tile_paredon_3")]
        public void LosFarolesDeLaAutopistaAlumbranElParedonYLaCalleYSeVanConElApagon(string tile)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Tiles + "Level2/Prefabs/" + tile + ".prefab");
            var farol = prefab.GetComponentInChildren<Light2D>(true);

            Assert.IsNotNull(farol, "el paredon no tiene farol");
            Assert.AreEqual(Light2D.LightType.Point, farol.lightType);
            Assert.AreEqual(
                new[] { SortingLayer.NameToID("Arboles"), SortingLayer.NameToID("Calle"), SortingLayer.NameToID("Juego") },
                farol.targetSortingLayers);
            Assert.IsFalse(farol.shadowsEnabled);
            Assert.IsNotNull(farol.GetComponent<LuzDeCiudad>());
        }

        // Es lo mismo que la calle de abajo: en otra capa no empalman y le pega la luz de las ventanas.
        [Test]
        public void LaBocacalleDeArribaVaEnLaMismaCapaQueLaDeAbajo()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<SceneryTileset>("Assets/_Zombineta/Settings/Escenario.asset");
            foreach (var capa in tileset.layers)
                foreach (var v in capa.variants)
                    if (v.acompanante != null)
                        Assert.AreEqual(capa.sortingLayer, v.acompananteCapa, capa.name);
        }

        [Test]
        public void ElFaroRotoSaleSoloEnAlgunosAutos()
        {
            var auto = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Zombineta/Art/Level/Prefabs/Obstaculo_Auto.prefab");
            var faro = auto.GetComponentInChildren<FaroRoto>(true);

            Assert.IsNotNull(faro);
            Assert.That(faro.Probabilidad, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.IsNotNull(faro.Haz);
            Assert.AreEqual(2, faro.Destellos.Length);
        }
    }
}
