using NUnit.Framework;
using UnityEngine;
using Zombineta.Player;

namespace Zombineta.Juego.Tests
{
    /// <summary>El fogonazo y la traza salen de la boca del arma del dibujo, no del centro de la moto.</summary>
    public class PuntaDelArmaTests
    {
        Texture2D textura;
        Sprite cuadro;

        [SetUp]
        public void Armar()
        {
            // Un cuadro de 200 x 100 pixeles, pivote abajo al centro y 100 pixeles por unidad.
            textura = new Texture2D(200, 100);
            cuadro = Sprite.Create(textura, new Rect(0f, 0f, 200f, 100f), new Vector2(0.5f, 0f), 100f);
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(cuadro);
            Object.DestroyImmediate(textura);
        }

        [Test]
        public void ElPixelSeMideDesdeArribaALaIzquierda()
        {
            var p = PuntaDelArma.Local(cuadro, new Vector2(20f, 30f), false);
            Assert.AreEqual(-0.8f, p.x, 1e-5f, "20 px desde la izquierda, el pivote esta en 100");
            Assert.AreEqual(0.7f, p.y, 1e-5f, "30 px desde arriba de 100: 70 px sobre el pivote");
        }

        [Test]
        public void EspejadoQuedaDelOtroLadoDelPivote()
        {
            var p = PuntaDelArma.Local(cuadro, new Vector2(20f, 30f), true);
            Assert.AreEqual(0.8f, p.x, 1e-5f);
            Assert.AreEqual(0.7f, p.y, 1e-5f);
        }

        [Test]
        public void EnElMundoSumaLaPosicionYLaEscalaDelDibujo()
        {
            var go = new GameObject("dibujo");
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                go.transform.position = new Vector3(10f, 2f, 0f);
                go.transform.localScale = new Vector3(2f, 2f, 1f);
                var p = PuntaDelArma.EnMundo(sr, cuadro, new Vector2(20f, 30f));
                Assert.AreEqual(10f - 1.6f, p.x, 1e-4f);
                Assert.AreEqual(2f + 1.4f, p.y, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
