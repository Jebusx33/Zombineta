using NUnit.Framework;
using Zombineta.Core;

namespace Zombineta.Juego.Tests
{
    public class LaneSortingTests
    {
        [Test]
        public void TheLowerLane_IsDrawnInFront()
        {
            Assert.Greater(LaneSorting.Order(0f, SortSlot.Zombie), LaneSorting.Order(1f, SortSlot.Zombie));
            Assert.Greater(LaneSorting.Order(1f, SortSlot.Item), LaneSorting.Order(2f, SortSlot.Player));
        }

        [Test]
        public void ChangingLane_MovesTheOrderGradually()
        {
            int from = LaneSorting.Order(2f, SortSlot.Player);
            int mid = LaneSorting.Order(1.5f, SortSlot.Player);
            int to = LaneSorting.Order(1f, SortSlot.Player);
            Assert.Greater(mid, from);
            Assert.Less(mid, to);
        }

        [Test]
        public void InsideALane_ShadowItemZombiePlayerEffect()
        {
            Assert.Less(LaneSorting.Order(1f, SortSlot.Shadow), LaneSorting.Order(1f, SortSlot.Item));
            Assert.Less(LaneSorting.Order(1f, SortSlot.Item), LaneSorting.Order(1f, SortSlot.Zombie));
            Assert.Less(LaneSorting.Order(1f, SortSlot.Zombie), LaneSorting.Order(1f, SortSlot.Player));
            Assert.Less(LaneSorting.Order(1f, SortSlot.Player), LaneSorting.Order(1f, SortSlot.Effect));
        }

        [Test]
        public void AShadowInTheLaneBelow_IsInFrontOfAnEffectInTheLaneAbove()
        {
            // Un carril entero pesa mas que cualquier slot: la escena se lee de abajo hacia arriba.
            Assert.Greater(LaneSorting.Order(0f, SortSlot.Shadow), LaneSorting.Order(1f, SortSlot.Effect));
        }

        [Test]
        public void EverythingStaysBetweenTheStreetAndTheForeground()
        {
            // Calle en -20 y primer plano en 40 son ordenes de capas de escenario en OTRA sorting
            // layer de valores bajos; el juego vive en su propio rango positivo.
            Assert.Greater(LaneSorting.Order(2f, SortSlot.Shadow), 0);
            Assert.Less(LaneSorting.Order(-0.5f, SortSlot.Effect), 32767);
        }
    }
}
