using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Art;

namespace Zombineta.Juego.Tests
{
    public class AlphaIslandsTests
    {
        static bool[] Canvas(int w, int h, params (int x, int y, int w, int h)[] boxes)
        {
            var a = new bool[w * h];
            foreach (var b in boxes)
                for (int y = b.y; y < b.y + b.h; y++)
                    for (int x = b.x; x < b.x + b.w; x++)
                        a[y * w + x] = true;
            return a;
        }

        [Test]
        public void TwoFigures_AreTwoIslands_LeftToRight()
        {
            var r = AlphaIslands.Find(Canvas(100, 50, (60, 5, 20, 40), (10, 5, 20, 40)), 100, 50, 10, 0);
            Assert.AreEqual(2, r.Count);
            Assert.AreEqual(10, r[0].x);
            Assert.AreEqual(60, r[1].x);
        }

        [Test]
        public void RowsGoTopToBottom()
        {
            // En textura de Unity, y alto = arriba.
            var r = AlphaIslands.Find(Canvas(60, 100, (5, 5, 20, 30), (5, 60, 20, 30)), 60, 100, 10, 0);
            Assert.AreEqual(60, r[0].y);
            Assert.AreEqual(5, r[1].y);
        }

        [Test]
        public void ALooseHand_IsMergedWithItsBody()
        {
            var r = AlphaIslands.Find(Canvas(100, 60, (10, 5, 20, 40), (33, 30, 4, 4)), 100, 60, 10, 5);
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(27, r[0].width);
        }

        [Test]
        public void Specks_AreIgnored()
        {
            var r = AlphaIslands.Find(Canvas(80, 50, (10, 5, 20, 40), (70, 45, 2, 2)), 80, 50, 10, 0);
            Assert.AreEqual(1, r.Count);
        }

        // Ambiguedad del brief: agrupar filas por la distancia entre CENTROS Y (con tolerancia de
        // media altura mediana) agrupa mal una pose "en el piso": es mas baja y mas corta que sus
        // vecinas de pie, asi que su centro cae lejos del centro de ellas y puede terminar mas
        // cerca del centro de la fila de ABAJO. Agrupar por superposicion/cercania de los BORDES
        // verticales de la caja (no del centro) resuelve esto: una pose en el piso comparte franja
        // vertical con sus vecinas de pie (el mismo "piso" de la escena) aunque su centro no
        // coincida. Este test reproduce el caso: una fila con dos poses de pie (altura 40 y 35) y
        // una pose en el piso (altura 20, mas baja) que se superpone con ellas solo en el borde
        // inferior; una fila de abajo con dos poses de pie mas, bien separada en Y.
        [Test]
        public void LyingPose_JoinsItsStandingRow_ByVerticalOverlap_NotByCenterDistance()
        {
            var r = AlphaIslands.Find(
                Canvas(160, 150,
                    (10, 100, 20, 40),   // fila de arriba, de pie
                    (60, 105, 20, 35),   // fila de arriba, de pie
                    (110, 90, 30, 20),   // fila de arriba, en el piso (mas baja y mas corta)
                    (10, 0, 20, 40),     // fila de abajo, de pie
                    (60, 0, 20, 40)),    // fila de abajo, de pie
                160, 150, 10, 0);

            Assert.AreEqual(5, r.Count);

            // Fila de arriba primero (de arriba hacia abajo), ordenada por X.
            Assert.AreEqual(10, r[0].x);
            Assert.AreEqual(100, r[0].y);
            Assert.AreEqual(60, r[1].x);
            Assert.AreEqual(110, r[2].x);
            Assert.AreEqual(90, r[2].y); // la pose en el piso, agrupada igual en la fila de arriba

            // Fila de abajo despues.
            Assert.AreEqual(10, r[3].x);
            Assert.AreEqual(0, r[3].y);
            Assert.AreEqual(60, r[4].x);
        }
    }
}
