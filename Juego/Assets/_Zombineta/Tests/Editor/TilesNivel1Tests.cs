using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Luz;
using Zombineta.Scenery;

namespace Zombineta.Juego.Tests
{
    /// <summary>Los tiles del Nivel 1 del 09/11: la bocacalle a escala de los edificios, los arboles enteros y la rampa.</summary>
    public class TilesNivel1Tests
    {
        const string Tiles = "Assets/_Zombineta/Art/Tileset/";

        static SceneryTileset Escenario() =>
            AssetDatabase.LoadAssetAtPath<SceneryTileset>("Assets/_Zombineta/Settings/Escenario.asset");

        static SceneryLayer Capa(string nombre)
        {
            foreach (var capa in Escenario().layers)
                if (capa.name == nombre)
                    return capa;
            return null;
        }

        static SceneryVariant LaDeLaBocacalle()
        {
            foreach (var v in Capa("Calle").variants)
                if (v.acompanante != null)
                    return v;
            return null;
        }

        [Test]
        public void LaBocacalleDeArribaTieneElAltoDeLosEdificios()
        {
            var v = LaDeLaBocacalle();
            Assert.IsNotNull(v, "la calle no tiene bocacalle");
            var calle = v.prefab.GetComponent<SpriteRenderer>().sprite;
            var arriba = v.acompanante.GetComponent<SpriteRenderer>().sprite;

            // Lo mismo que hace SceneryManager: la escala de la calle por la del acompanante.
            float escalaCalle = Capa("Calle").height / calle.bounds.size.y;
            float k = calle.pixelsPerUnit / arriba.pixelsPerUnit * v.acompananteEscala;
            float alto = arriba.bounds.size.y * k * escalaCalle;

            Assert.AreEqual(Capa("Edificios").height, alto, 0.05f);
        }

        [Test]
        public void LaBocacalleDeArribaApoyaSobreLaCalle()
        {
            var v = LaDeLaBocacalle();
            var calle = v.prefab.GetComponent<SpriteRenderer>().sprite;
            Assert.AreEqual(calle.rect.height, v.acompananteOffsetPx.y, 0.5f, "arranca en el borde de arriba de la calle");
            Assert.AreEqual(Capa("Calle").baselineY + Capa("Calle").height, Capa("Edificios").baselineY, 0.01f,
                "y ahi mismo apoyan los edificios");
        }

        [Test]
        public void LasVentanasDeLaBocacalleSeIluminanYSeVanConElApagon()
        {
            var prefab = LaDeLaBocacalle().acompanante;
            Light2D ventanas = null;
            foreach (var luz in prefab.GetComponentsInChildren<Light2D>(true))
                if (luz.lightType == Light2D.LightType.Sprite)
                    ventanas = luz;

            Assert.IsNotNull(ventanas);
            Assert.IsNotNull(ventanas.lightCookieSprite);
            var dibujo = prefab.GetComponent<SpriteRenderer>().sprite.bounds;
            var mascara = ventanas.lightCookieSprite.bounds;
            Assert.AreEqual(dibujo.size.x, mascara.size.x, 0.05f, "ancho de la mascara");
            Assert.AreEqual(dibujo.size.y, mascara.size.y, 0.05f, "alto de la mascara");
            // Esta en la capa de la calle: la luz de sus ventanas tambien.
            Assert.AreEqual(new[] { SortingLayer.NameToID("Calle") }, ventanas.targetSortingLayers);
            Assert.IsNotNull(ventanas.GetComponent<LuzDeCiudad>());
        }

        [Test]
        public void ElFarolYElSemaforoDeLaBocacalleQuedanSobreElDibujo()
        {
            var prefab = LaDeLaBocacalle().acompanante;
            var dibujo = prefab.GetComponent<SpriteRenderer>().sprite.bounds;
            int puntuales = 0;
            foreach (var luz in prefab.GetComponentsInChildren<Light2D>(true))
            {
                if (luz.lightType != Light2D.LightType.Point)
                    continue;
                puntuales++;
                Assert.IsTrue(dibujo.Contains(new Vector3(luz.transform.localPosition.x, luz.transform.localPosition.y, 0f)), luz.name);
                Assert.IsNotNull(luz.GetComponent<LuzDeCiudad>(), luz.name);
            }
            Assert.AreEqual(2, puntuales);
        }

        [TestCase("Arboles")]
        [TestCase("Frontal")]
        public void LosDosArbolesSalenConElMismoTamanoDeDibujo(string nombre)
        {
            var capa = Capa(nombre);
            float? escala = null;
            int arboles = 0;
            foreach (var v in capa.variants)
            {
                var sr = v.prefab.GetComponent<SpriteRenderer>();
                if (!sr.sprite.name.Contains("arbol"))
                    continue;
                arboles++;
                StringAssert.EndsWith("_v5", sr.sprite.name);
                float e = capa.height * v.heightScale / sr.sprite.bounds.size.y;
                if (escala.HasValue)
                    Assert.AreEqual(escala.Value, e, 0.0005f, "los dos arboles a la misma escala");
                escala = e;
            }
            Assert.AreEqual(2, arboles);
        }

        [Test]
        public void LaRampaCabeEnSuCarril()
        {
            var rampa = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Zombineta/Art/Level/Prefabs/Rampa.prefab")
                .GetComponent<SpriteRenderer>().sprite;
            StringAssert.Contains("rampa_v5", rampa.name);
            var paleta = AssetDatabase.LoadAssetAtPath<Zombineta.Juego.Levels.LevelItemPalette>("Assets/_Zombineta/Settings/LevelItemPalette.asset");
            var config = AssetDatabase.LoadAssetAtPath<Zombineta.Core.GameConfig>("Assets/_Zombineta/Settings/GameConfig.asset");
            float escala = paleta.Get(Zombineta.Level.LevelEntryKind.Ramp).scale.y;
            float carril = config.laneSpacing;

            // El item va en el centro de su carril: la base de la rampa tiene que caer en la mitad de adelante
            // del carril (no en el de abajo ni fuera de la calle), y no medir mucho mas que un carril.
            float base_ = rampa.bounds.min.y * escala;
            Assert.That(base_, Is.LessThan(0f).And.GreaterThan(-carril / 2f), "la base, dentro de su carril");
            Assert.That(rampa.bounds.size.y * escala, Is.LessThanOrEqualTo(1.5f * carril), "alto");
        }
    }
}
