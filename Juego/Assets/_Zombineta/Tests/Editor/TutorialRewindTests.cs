using NUnit.Framework;
using Zombineta.Level;
using Zombineta.Tutorial;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// TutorialRewind es C# plano: decide si el tutorial se paso de largo un paso (el jugador
    /// avanza solo, a un lector lento se le puede escapar toda una zona de items) y a donde
    /// volver. El TutorialDirector (Tarea 4) es quien aplica la decision sobre el RunState.
    /// </summary>
    public class TutorialRewindTests
    {
        [Test]
        public void ItemDe_MapeaCadaCondicionBasadaEnItemsASuTipo()
        {
            Assert.AreEqual(LevelEntryKind.Ramp, TutorialRewind.ItemDe(CondicionPaso.Salto));
            Assert.AreEqual(LevelEntryKind.Fuel, TutorialRewind.ItemDe(CondicionPaso.PickupNafta));
            Assert.AreEqual(LevelEntryKind.Battery, TutorialRewind.ItemDe(CondicionPaso.PickupBateria));
            Assert.AreEqual(LevelEntryKind.Ammo, TutorialRewind.ItemDe(CondicionPaso.PickupMunicion));
            Assert.AreEqual(LevelEntryKind.ZombieFront, TutorialRewind.ItemDe(CondicionPaso.DisparoAcertado));
        }

        [Test]
        public void ItemDe_CondicionesSinItemDevuelvenNull()
        {
            Assert.IsNull(TutorialRewind.ItemDe(CondicionPaso.TurboSostenido));
            Assert.IsNull(TutorialRewind.ItemDe(CondicionPaso.ReversaSostenida));
            Assert.IsNull(TutorialRewind.ItemDe(CondicionPaso.CambioCarrilAmbos));
            Assert.IsNull(TutorialRewind.ItemDe(CondicionPaso.FaroSostenido));
            Assert.IsNull(TutorialRewind.ItemDe(CondicionPaso.LlegarMeta));
        }

        // --- Rebobinado por items -------------------------------------------------------

        [Test]
        public void NoRebobinaMientrasNoSePaseElUltimoItemMasElMargen()
        {
            var d = TutorialRewind.Decidir(
                CondicionPaso.PickupNafta, esUltimoPaso: false,
                distanciasDelItem: new[] { 190f, 220f, 250f }, playerX: 255f, goalDistance: 650f);
            Assert.IsFalse(d.Rebobinar);
        }

        [Test]
        public void RebobinaAlPasarElUltimoItemMasElMargen()
        {
            var d = TutorialRewind.Decidir(
                CondicionPaso.PickupNafta, esUltimoPaso: false,
                distanciasDelItem: new[] { 190f, 220f, 250f }, playerX: 261f, goalDistance: 650f);
            Assert.IsTrue(d.Rebobinar);
            Assert.AreEqual(160f, d.PlayerX, 1e-4f); // primera (190) - 30
            Assert.IsTrue(d.LimpiarItems);
        }

        [Test]
        public void RebobinadoPorItemsNuncaDaUnaPosicionNegativa()
        {
            var d = TutorialRewind.Decidir(
                CondicionPaso.Salto, esUltimoPaso: false,
                distanciasDelItem: new[] { 5f }, playerX: 20f, goalDistance: 650f);
            Assert.IsTrue(d.Rebobinar);
            Assert.AreEqual(0f, d.PlayerX, 1e-4f);
        }

        [Test]
        public void SinDistanciasDeItemNoRebobinaPorItems()
        {
            var d = TutorialRewind.Decidir(
                CondicionPaso.PickupNafta, esUltimoPaso: false,
                distanciasDelItem: new float[0], playerX: 900f, goalDistance: 650f);
            // Cae al chequeo de meta (900 > 650 - 40), asi que igual rebobina, pero por meta.
            Assert.IsTrue(d.Rebobinar);
            Assert.IsFalse(d.LimpiarItems);
            Assert.AreEqual(750f, d.PlayerX, 1e-4f); // 900 - 150
        }

        [Test]
        public void CondicionSinItemIgnoraLasDistanciasYUsaSoloElChequeoDeMeta()
        {
            var d = TutorialRewind.Decidir(
                CondicionPaso.FaroSostenido, esUltimoPaso: false,
                distanciasDelItem: null, playerX: 100f, goalDistance: 650f);
            Assert.IsFalse(d.Rebobinar);
        }

        // --- Rebobinado por cercania a la meta --------------------------------------------

        [Test]
        public void NoRebobinaPorMetaMientrasFaltaMasDelMargen()
        {
            var d = TutorialRewind.Decidir(
                CondicionPaso.FaroSostenido, esUltimoPaso: false,
                distanciasDelItem: null, playerX: 609f, goalDistance: 650f);
            Assert.IsFalse(d.Rebobinar);
        }

        [Test]
        public void RebobinaPorMetaAlEntrarEnElMargen()
        {
            var d = TutorialRewind.Decidir(
                CondicionPaso.FaroSostenido, esUltimoPaso: false,
                distanciasDelItem: null, playerX: 615f, goalDistance: 650f);
            Assert.IsTrue(d.Rebobinar);
            Assert.AreEqual(465f, d.PlayerX, 1e-4f); // 615 - 150
            Assert.IsFalse(d.LimpiarItems);
        }

        [Test]
        public void ElUltimoPasoNuncaRebobinaPorMeta()
        {
            var d = TutorialRewind.Decidir(
                CondicionPaso.LlegarMeta, esUltimoPaso: true,
                distanciasDelItem: null, playerX: 649f, goalDistance: 650f);
            Assert.IsFalse(d.Rebobinar);
        }

        [Test]
        public void ElChequeoDeItemsTienePrioridadSobreElDeMeta()
        {
            // Un paso de items cerca de la meta: si se paso el item, rebobina por item (limpia
            // los items), no por meta.
            var d = TutorialRewind.Decidir(
                CondicionPaso.DisparoAcertado, esUltimoPaso: false,
                distanciasDelItem: new[] { 510f, 545f, 580f }, playerX: 615f, goalDistance: 650f);
            Assert.IsTrue(d.Rebobinar);
            Assert.IsTrue(d.LimpiarItems);
            Assert.AreEqual(480f, d.PlayerX, 1e-4f); // 510 - 30
        }
    }
}
