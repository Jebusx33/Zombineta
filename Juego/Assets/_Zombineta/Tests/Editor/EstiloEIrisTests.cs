using NUnit.Framework;
using UnityEngine;
using Zombineta.Estilo;
using Zombineta.Juego.Flow;

namespace Zombineta.Juego.Tests
{
    /// <summary>Las cuentas de la transicion de iris y la mezcla de cada estilo de imagen.</summary>
    public class EstiloEIrisTests
    {
        [Test]
        public void DesdeElCentroElIrisCubreHastaLaEsquina()
        {
            float r = IrisMath.RadioParaCubrir(new Vector2(0.5f, 0.5f), 16f / 9f);
            Assert.AreEqual(Mathf.Sqrt((8f / 9f) * (8f / 9f) + 0.25f), r, 0.0001f);
        }

        [Test]
        public void DesdeUnCostadoHaceFaltaMasRadio()
        {
            float centro = IrisMath.RadioParaCubrir(new Vector2(0.5f, 0.5f), 16f / 9f);
            float costado = IrisMath.RadioParaCubrir(new Vector2(0.8f, 0.3f), 16f / 9f);
            Assert.That(costado, Is.GreaterThan(centro));
        }

        [Test]
        public void AbiertoNoTapaYCerradoTapaTodo()
        {
            Assert.AreEqual(2f, IrisMath.Radio(0f, 2f), 0.0001f);
            Assert.AreEqual(0f, IrisMath.Radio(1f, 2f), 0.0001f);
            Assert.That(IrisMath.Radio(0.5f, 2f), Is.GreaterThan(0f).And.LessThan(2f));
        }

        [Test]
        public void ElCentroNoSeSaleDeLaPantalla()
        {
            var c = IrisMath.CentroEnPantalla(new Vector3(1.4f, -0.2f, 5f));
            Assert.AreEqual(0.95f, c.x, 0.0001f);
            Assert.AreEqual(0.05f, c.y, 0.0001f);
        }

        [Test]
        public void SinEstiloNoSePrendeNada()
        {
            Assert.IsFalse(EstiloDeImagen.MezclaDe(EstiloVisual.Normal, 0f).Algo);
        }

        [Test]
        public void BlancoYNegroSacaElColorSinSepiaNiProyector()
        {
            var m = EstiloDeImagen.MezclaDe(EstiloVisual.BlancoYNegro, 0f);
            Assert.AreEqual(1f, m.sinColor);
            Assert.AreEqual(0f, m.sepia);
            Assert.AreEqual(0f, m.proyector);
        }

        [Test]
        public void LaPeliculaViejaLlevaSepiaGranoYProyector()
        {
            var m = EstiloDeImagen.MezclaDe(EstiloVisual.PeliculaVieja, 0f);
            Assert.AreEqual(1f, m.sepia);
            Assert.AreEqual(1f, m.grano);
            Assert.AreEqual(1f, m.proyector);
        }

        [Test]
        public void LaDesaturacionDePasoSeSumaAlEstiloDelNivel()
        {
            Assert.AreEqual(0.85f, EstiloDeImagen.MezclaDe(EstiloVisual.Normal, 0.85f).sinColor, 0.0001f);
            Assert.AreEqual(1f, EstiloDeImagen.MezclaDe(EstiloVisual.BlancoYNegro, 0.3f).sinColor, 0.0001f);
        }
    }
}
