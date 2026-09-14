using NUnit.Framework;
using UnityEngine;
using Zombineta.Juego.Levels;

namespace Zombineta.Juego.Tests
{
    public class LevelLayoutTests
    {
        static readonly LevelLayout Layout = new LevelLayout(0.75f, 1.6f, 1.125f);

        [Test]
        public void MetersAndWorldX_GoBackAndForth()
        {
            Assert.AreEqual(300f, Layout.ToWorldX(400f), 1e-4f);
            Assert.AreEqual(400f, Layout.ToMeters(300f), 1e-4f);
        }

        [Test]
        public void LaneHeights_MatchTheRunController()
        {
            Assert.AreEqual(-1.6f, Layout.LaneY(0), 1e-4f);
            Assert.AreEqual(0f, Layout.LaneY(1), 1e-4f);
            Assert.AreEqual(1.6f, Layout.LaneY(2), 1e-4f);
        }

        [Test]
        public void TheNearestLane_IsClampedToTheThreeLanes()
        {
            Assert.AreEqual(1, Layout.NearestLane(0.7f));
            Assert.AreEqual(2, Layout.NearestLane(0.9f));
            Assert.AreEqual(0, Layout.NearestLane(-9f));
            Assert.AreEqual(2, Layout.NearestLane(9f));
        }

        [Test]
        public void SnapMeters_RoundsToTheGrid()
        {
            Assert.AreEqual(12f, LevelLayout.SnapMeters(12.4f, 1f));
            Assert.AreEqual(13f, LevelLayout.SnapMeters(12.6f, 1f));
            Assert.AreEqual(12.4f, LevelLayout.SnapMeters(12.4f, 0f), "paso 0 = sin grilla");
        }

        [Test]
        public void AnAerialItem_IsDrawnAboveItsLane_AndKnowsItsGround()
        {
            Vector2 p = Layout.ItemPosition(100f, 2, 6f);
            Assert.AreEqual(75f, p.x, 1e-4f);
            Assert.AreEqual(1.6f + 6f * 1.125f, p.y, 1e-4f);
            Assert.AreEqual(1.6f, Layout.GroundY(p.y, 6f), 1e-4f);
            Assert.AreEqual(2, Layout.NearestLane(Layout.GroundY(p.y, 6f)));
        }
    }
}
