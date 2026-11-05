using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Level;

namespace Zombineta.Tests
{
    /// <summary>El clima de cada tramo: entra y sale con fundido, como el apagon, y sin tramos no hay clima.</summary>
    public class ClimaDeTramosTests
    {
        static List<Tramo> Tramos3() => new List<Tramo>
        {
            new Tramo { hasta = 100f },
            new Tramo { hasta = 200f, lluvia = 1f, viento = 0.5f },
            new Tramo { hasta = 300f, niebla = 0.8f },
        };

        [Test]
        public void SinTramosNoHayClima()
        {
            Assert.AreEqual(0f, Tramos.LluviaAt(null, 50f, 30f));
            Assert.AreEqual(0f, Tramos.NieblaAt(new List<Tramo>(), 50f, 30f));
            Assert.IsFalse(Tramos.TieneClima(null));
        }

        [Test]
        public void CadaTramoTieneElSuyo()
        {
            var t = Tramos3();
            Assert.AreEqual(1f, Tramos.LluviaAt(t, 150f, 30f), 0.0001f);
            Assert.AreEqual(0.5f, Tramos.VientoAt(t, 150f, 30f), 0.0001f);
            Assert.AreEqual(0.8f, Tramos.NieblaAt(t, 250f, 30f), 0.0001f);
            Assert.AreEqual(0f, Tramos.LluviaAt(t, 250f, 30f), 0.0001f);
        }

        [Test]
        public void EntraConFundidoEnElLimite()
        {
            var t = Tramos3();
            Assert.AreEqual(0.5f, Tramos.LluviaAt(t, 100f, 30f), 0.0001f, "en el limite, a mitad de camino");
            Assert.That(Tramos.LluviaAt(t, 95f, 30f), Is.GreaterThan(0f).And.LessThan(0.5f));
        }

        [Test]
        public void TieneClimaSiAlgunTramoLoTiene()
        {
            Assert.IsTrue(Tramos.TieneClima(Tramos3()));
            Assert.IsFalse(Tramos.TieneClima(new List<Tramo> { new Tramo(), new Tramo { oscuridad = 1f } }));
        }
    }
}
