using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Scenery;

namespace Zombineta.Tests
{
    public class SceneryLayoutTests
    {
        static SceneryLayout Make(int seed = 7, float gapChance = 0f, float gapMin = 0f,
                                  float gapMax = 0f, bool noRepeat = false,
                                  float[] widths = null, float[] weights = null)
        {
            widths = widths ?? new[] { 4f, 6f, 5f };
            weights = weights ?? new[] { 1f, 1f, 1f };
            return new SceneryLayout(widths, weights, gapChance, gapMin, gapMax, noRepeat,
                                     seed, startX: -10f);
        }

        static List<TilePlacement> Snapshot(SceneryLayout layout, float until)
        {
            layout.EnsureCovered(until);
            return new List<TilePlacement>(layout.Placed);
        }

        [Test]
        public void SameSeed_BuildsTheSameStreet()
        {
            var a = Snapshot(Make(seed: 42, gapChance: 0.3f, gapMin: 1f, gapMax: 3f), 500f);
            var b = Snapshot(Make(seed: 42, gapChance: 0.3f, gapMin: 1f, gapMax: 3f), 500f);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].X, b[i].X);
                Assert.AreEqual(a[i].Variant, b[i].Variant);
            }
        }

        [Test]
        public void DifferentSeed_BuildsADifferentStreet()
        {
            var a = Snapshot(Make(seed: 1), 300f);
            var b = Snapshot(Make(seed: 2), 300f);

            bool anyDifferent = false;
            for (int i = 0; i < a.Count && i < b.Count; i++)
                anyDifferent |= a[i].Variant != b[i].Variant;

            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void GrowingFrameByFrame_EqualsGrowingAllAtOnce()
        {
            // Es la propiedad que hace posible armar el escenario en tiempo real: el
            // resultado no puede depender de a que ritmo avanzo la camara.
            var steps = Make(seed: 9, gapChance: 0.4f, gapMin: 0.5f, gapMax: 4f, noRepeat: true);
            for (float x = 0f; x <= 800f; x += 0.37f)
                steps.EnsureCovered(x);
            // El paso de 0,37 no cae justo en 800: se completa el mismo destino que la
            // version de una sola vez, que es lo que se esta comparando.
            steps.EnsureCovered(800f);

            var once = Snapshot(Make(seed: 9, gapChance: 0.4f, gapMin: 0.5f, gapMax: 4f,
                                     noRepeat: true), 800f);

            Assert.AreEqual(once.Count, steps.Placed.Count);
            for (int i = 0; i < once.Count; i++)
            {
                Assert.AreEqual(once[i].X, steps.Placed[i].X, 1e-4f);
                Assert.AreEqual(once[i].Variant, steps.Placed[i].Variant);
            }
        }

        [Test]
        public void WithoutGaps_TilesAreContiguous()
        {
            var tiles = Snapshot(Make(), 400f);

            for (int i = 1; i < tiles.Count; i++)
                Assert.AreEqual(tiles[i - 1].End, tiles[i].X, 1e-4f, "no puede haber huecos");
        }

        [Test]
        public void Gaps_StayInsideTheConfiguredRange()
        {
            var tiles = Snapshot(Make(gapChance: 1f, gapMin: 2f, gapMax: 3f), 400f);

            for (int i = 1; i < tiles.Count; i++)
            {
                float gap = tiles[i].X - tiles[i - 1].End;
                Assert.GreaterOrEqual(gap, 2f - 1e-4f);
                Assert.LessOrEqual(gap, 3f + 1e-4f);
            }
        }

        [Test]
        public void AVariantWithZeroWeight_IsNeverUsed()
        {
            var tiles = Snapshot(Make(weights: new[] { 1f, 0f, 1f }), 1000f);

            foreach (var t in tiles)
                Assert.AreNotEqual(1, t.Variant);
        }

        [Test]
        public void AVariantWithoutWidth_IsNeverUsed()
        {
            // Un sprite que falta llega con ancho 0: no puede trabar la capa.
            var tiles = Snapshot(Make(widths: new[] { 4f, 0f, 5f }), 500f);

            Assert.Greater(tiles.Count, 0);
            foreach (var t in tiles)
                Assert.AreNotEqual(1, t.Variant);
        }

        [Test]
        public void NoImmediateRepeat_NeverPlacesTheSameVariantTwiceInARow()
        {
            var tiles = Snapshot(Make(noRepeat: true, weights: new[] { 10f, 1f, 1f }), 2000f);

            for (int i = 1; i < tiles.Count; i++)
                Assert.AreNotEqual(tiles[i - 1].Variant, tiles[i].Variant);
        }

        [Test]
        public void AllWeightsZero_GivesAnEmptyHarmlessLayer()
        {
            var layout = Make(weights: new[] { 0f, 0f, 0f });
            var results = new List<TilePlacement>();

            Assert.IsTrue(layout.IsEmpty);
            Assert.DoesNotThrow(() => layout.Query(0f, 100f, results));
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Query_ReturnsExactlyTheTilesThatOverlapTheWindow()
        {
            var layout = Make(gapChance: 0.5f, gapMin: 1f, gapMax: 5f);
            var results = new List<TilePlacement>();

            layout.Query(100f, 130f, results);

            Assert.Greater(results.Count, 0);
            foreach (var t in results)
                Assert.IsTrue(t.End > 100f && t.X < 130f, "solo lo que se ve");

            // Y no se olvido de ninguno.
            int expected = 0;
            foreach (var t in layout.Placed)
                if (t.End > 100f && t.X < 130f) expected++;
            Assert.AreEqual(expected, results.Count);
        }

        [Test]
        public void Query_WorksBackwards_AndGivesTheSameTilesAsBefore()
        {
            // El retroceso vuelve sobre lo recorrido: los edificios no pueden cambiar.
            var layout = Make(seed: 3, gapChance: 0.3f, gapMin: 1f, gapMax: 2f);
            var first = new List<TilePlacement>();
            var again = new List<TilePlacement>();

            layout.Query(50f, 80f, first);
            layout.Query(600f, 630f, new List<TilePlacement>());
            layout.Query(50f, 80f, again);

            Assert.AreEqual(first.Count, again.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].X, again[i].X);
                Assert.AreEqual(first[i].Variant, again[i].Variant);
            }
        }
    }

    public class ParallaxMathTests
    {
        [Test]
        public void FactorOne_IsWorldLocked()
        {
            // La calle: su origen nunca se mueve, sin importar la camara. Por eso la moto
            // y el piso quedan sincronizados por construccion.
            foreach (float cam in new[] { -12f, 0f, 37.5f, 990f })
                Assert.AreEqual(0f, ParallaxMath.LayerOriginX(cam, 1f), 1e-4f);
        }

        [Test]
        public void FactorZero_TravelsWithTheCamera()
        {
            foreach (float cam in new[] { -12f, 0f, 37.5f, 990f })
                Assert.AreEqual(cam, ParallaxMath.LayerOriginX(cam, 0f), 1e-4f);
        }

        [Test]
        public void TheVisibleCenter_IsWhereTheCameraLooksInsideTheLayer()
        {
            const float cam = 80f;
            foreach (float f in new[] { 0f, 0.5f, 1f, 2.5f })
            {
                // Un tile en el centro local visible cae justo en el centro de la camara.
                float local = ParallaxMath.LocalViewCenter(cam, f);
                float world = ParallaxMath.LayerOriginX(cam, f) + local;
                Assert.AreEqual(cam, world, 1e-3f, "factor " + f);
            }
        }

        [Test]
        public void ScreenSpeed_ScalesExactlyWithTheFactor()
        {
            const float cameraVelocity = 3f; // 12 m/s con 0,25 unidades por metro

            Assert.AreEqual(0f, ParallaxMath.ScreenVelocity(cameraVelocity, 0f), 1e-4f);
            Assert.AreEqual(-1.5f, ParallaxMath.ScreenVelocity(cameraVelocity, 0.5f), 1e-4f);
            Assert.AreEqual(-3f, ParallaxMath.ScreenVelocity(cameraVelocity, 1f), 1e-4f);
            Assert.AreEqual(-7.5f, ParallaxMath.ScreenVelocity(cameraVelocity, 2.5f), 1e-4f);
        }

        [Test]
        public void TheFrontLayer_OutrunsTheStreet_AndTheBuildingsLagBehind()
        {
            float street = System.Math.Abs(ParallaxMath.ScreenVelocity(3f, 1f));

            Assert.Greater(System.Math.Abs(ParallaxMath.ScreenVelocity(3f, 2.5f)), street);
            Assert.Less(System.Math.Abs(ParallaxMath.ScreenVelocity(3f, 0.5f)), street);
        }
    }
}
