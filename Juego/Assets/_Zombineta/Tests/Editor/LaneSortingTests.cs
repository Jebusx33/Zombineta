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
            // Calle/fondo van en ordenes negativos y el primer plano de escenario en 2000/2010,
            // todos en la MISMA sorting layer que los carriles: el juego vive en el rango positivo
            // de en medio, por debajo del primer plano.
            Assert.Greater(LaneSorting.Order(2f, SortSlot.Shadow), 0);
            Assert.Less(LaneSorting.Order(-0.5f, SortSlot.Effect), 32767);
            // Ni con el carril mas adelantado (visualLane negativo, fuera de rango) el contenido de
            // carril puede llegar a pisar el primer plano del escenario.
            Assert.Less(LaneSorting.Order(-0.5f, SortSlot.Effect), 2000);
        }

        [Test]
        public void TheGameLayer_IsNamedAndExists()
        {
            Assert.AreEqual("Juego", LaneSorting.GameLayer);
            Assert.IsTrue(System.Array.Exists(UnityEngine.SortingLayer.layers,
                l => l.name == LaneSorting.GameLayer), "falta la Sorting Layer del juego");
        }
    }
}
