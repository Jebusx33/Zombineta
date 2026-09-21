using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Flow;

namespace Zombineta.Tests
{
    public class ComicPagesTests
    {
        static List<ComicPanel> Panels(params bool[] nuevaPagina)
        {
            var list = new List<ComicPanel>();
            foreach (var n in nuevaPagina)
                list.Add(new ComicPanel { nuevaPagina = n });
            return list;
        }

        [Test]
        public void PorDefectoCadaVinetaAbrePagina()
        {
            Assert.IsTrue(new ComicPanel().nuevaPagina);
        }

        [Test]
        public void LaPrimeraSiempreAbrePagina()
        {
            Assert.IsTrue(ComicPages.OpensPage(Panels(false, false), 0));
        }

        [Test]
        public void UnaPaginaAcumulada()
        {
            var p = Panels(true, false, false);
            Assert.IsTrue(ComicPages.OpensPage(p, 0));
            Assert.IsFalse(ComicPages.OpensPage(p, 1));
            Assert.IsFalse(ComicPages.OpensPage(p, 2));
        }

        [Test]
        public void DosPaginasSeguidas()
        {
            var p = Panels(true, false, true, false);
            Assert.IsFalse(ComicPages.OpensPage(p, 1));
            Assert.IsTrue(ComicPages.OpensPage(p, 2));
            Assert.IsFalse(ComicPages.OpensPage(p, 3));
        }

        [Test]
        public void FueraDeRangoNoAbre()
        {
            Assert.IsFalse(ComicPages.OpensPage(Panels(true), 5));
            Assert.IsFalse(ComicPages.OpensPage(null, 0));
        }
    }
}
