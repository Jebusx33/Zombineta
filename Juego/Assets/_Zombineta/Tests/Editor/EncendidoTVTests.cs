using NUnit.Framework;
using Zombineta.Juego.Screens;

namespace Zombineta.Juego.Tests
{
    /// <summary>El mapa aparece como una tele que se prende: linea blanca, se abre en alto, se apaga el brillo.</summary>
    public class EncendidoTVTests
    {
        [Test]
        public void ArrancaComoUnaLineaBlancaFinita()
        {
            var e = EncendidoTV.En(0f);
            Assert.AreEqual(EncendidoTV.AltoLinea, e.alto, 1e-6f);
            Assert.Less(e.ancho, 0.05f);
            Assert.AreEqual(1f, e.brillo, 1e-6f);
        }

        [Test]
        public void LaLineaLlegaDeLadoALadoAntesDeAbrirse()
        {
            var e = EncendidoTV.En(EncendidoTV.FinLinea);
            Assert.AreEqual(1f, e.ancho, 1e-4f);
            Assert.AreEqual(EncendidoTV.AltoLinea, e.alto, 1e-4f);
        }

        [Test]
        public void SeAbreEnAltoYElBrilloBaja()
        {
            float antes = 0f, brilloAntes = 2f;
            for (float t = EncendidoTV.FinLinea; t <= EncendidoTV.FinApertura; t += 0.05f)
            {
                var e = EncendidoTV.En(t);
                Assert.GreaterOrEqual(e.alto, antes);
                Assert.LessOrEqual(e.brillo, brilloAntes);
                antes = e.alto;
                brilloAntes = e.brillo;
            }
        }

        [Test]
        public void PrendidaDelTodoEsLaImagenTalCual()
        {
            var e = EncendidoTV.En(1f);
            Assert.AreEqual(1f, e.ancho, 1e-6f);
            Assert.AreEqual(1f, e.alto, 1e-6f);
            Assert.AreEqual(0f, e.brillo, 1e-6f);
            Assert.AreEqual(1f, EncendidoTV.En(5f).alto, 1e-6f, "pasado el final sigue prendida");
        }
    }
}
