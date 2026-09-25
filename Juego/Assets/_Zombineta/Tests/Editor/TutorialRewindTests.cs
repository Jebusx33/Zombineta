using NUnit.Framework;
using Zombineta.Level;
using Zombineta.Tutorial;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// TutorialRewind es C# plano: decide si el tutorial se paso de largo un paso (el jugador
    /// avanza solo, a un lector lento se le puede escapar toda una zona de items) y a donde
    /// volver. TutorialSesion es quien aplica la decision sobre el RunState.
    /// </summary>
    public class TutorialRewindTests
    {
        // El recorrido de Tutorial.unity, por tipo (todos en los tres carriles).
        static readonly float[] Rampas = { 90f, 125f, 160f };
        static readonly float[] Bidones = { 190f, 220f, 250f };
        static readonly float[] NoBidones = { 90f, 125f, 160f, 280f, 310f, 340f, 370f, 400f, 430f, 510f, 545f, 580f };
        static readonly float[] NoRampas = { 190f, 220f, 250f, 280f, 310f, 340f, 370f, 400f, 430f, 510f, 545f, 580f };

        static RebobinadoDecision Decidir(
            CondicionPaso condicion, float[] propias, float[] otras, float playerX,
            bool esUltimo = false, bool enElAire = false, float meta = 650f) =>
            TutorialRewind.Decidir(condicion, esUltimo, propias, otras, playerX, meta, enElAire);

        [Test]
        public void ItemDe_MapeaCadaCondicionBasadaEnItemsASuTipo()
        {
            Assert.AreEqual(LevelEntryKind.Ramp, TutorialRewind.ItemDe(CondicionPaso.Salto));
            Assert.AreEqual(LevelEntryKind.Fuel, TutorialRewind.ItemDe(CondicionPaso.PickupNafta));
            Assert.AreEqual(LevelEntryKind.Battery, TutorialRewind.ItemDe(CondicionPaso.PickupBateria));
            Assert.AreEqual(LevelEntryKind.Ammo, TutorialRewind.ItemDe(CondicionPaso.PickupMunicion));
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

        [Test]
        public void ItemDe_ElDisparoNoTieneItem_SeLeDisparaALaHordaDeAtras()
        {
            // El arma solo tira hacia atras (RunSimulation.ApplyFire): los zombies de frente no
            // se pueden balear, solo arrollar. El paso 9 no depende de ningun item.
            Assert.IsNull(TutorialRewind.ItemDe(CondicionPaso.DisparoAcertado));
        }

        // --- Rebobinado por items -------------------------------------------------------

        [Test]
        public void NoRebobinaMientrasNoSePaseElUltimoItemMasElMargen()
        {
            Assert.IsFalse(Decidir(CondicionPaso.PickupNafta, Bidones, NoBidones, 255f).Rebobinar);
            Assert.IsFalse(Decidir(CondicionPaso.PickupNafta, Bidones, NoBidones, 260f).Rebobinar);
        }

        [Test]
        public void RebobinaAlPasarElUltimoItemMasElMargen_AlHuecoAntesDelTramo()
        {
            var d = Decidir(CondicionPaso.PickupNafta, Bidones, NoBidones, 261f);
            Assert.IsTrue(d.Rebobinar);
            Assert.IsTrue(d.LimpiarItems);
            // 190 - 30 = 160 caeria justo sobre la ultima rampa: el primer punto a 8 m o mas de
            // cualquier otro item es 168 (y sigue a 22 m del primer bidon).
            Assert.AreEqual(168f, d.PlayerX, 1e-4f);
        }

        [Test]
        public void ElDestinoQuedaLejosDeTodosLosDemasItems()
        {
            foreach (var (propias, otras) in new[]
                     {
                         (Rampas, NoRampas),
                         (Bidones, NoBidones),
                         (new[] { 280f, 310f, 340f }, new[] { 90f, 125f, 160f, 190f, 220f, 250f, 370f, 400f, 430f }),
                         (new[] { 370f, 400f, 430f }, new[] { 90f, 125f, 160f, 190f, 220f, 250f, 280f, 310f, 340f }),
                     })
            {
                float destino = TutorialRewind.DestinoAntesDelTramo(propias[0], otras);
                Assert.LessOrEqual(destino, propias[0] - TutorialRewind.DistanciaLibre, "antes del tramo");
                foreach (float o in otras)
                    Assert.GreaterOrEqual(System.Math.Abs(o - destino), TutorialRewind.DistanciaLibre,
                        "destino " + destino + " demasiado cerca del item en " + o);
            }
        }

        [Test]
        public void SinHuecoLibreCaeAPrimeraMenosElRetroceso()
        {
            // Items cada 5 m antes del tramo: no hay ningun punto libre, se usa primera - 30.
            var otras = new float[12];
            for (int i = 0; i < otras.Length; i++)
                otras[i] = 150f + i * 5f;
            Assert.AreEqual(170f, TutorialRewind.DestinoAntesDelTramo(200f, otras), 1e-4f);
        }

        [Test]
        public void RebobinadoPorItemsNuncaDaUnaPosicionNegativa()
        {
            var d = Decidir(CondicionPaso.PickupNafta, new[] { 5f }, null, 20f);
            Assert.IsTrue(d.Rebobinar);
            Assert.AreEqual(0f, d.PlayerX, 1e-4f);
        }

        [Test]
        public void LasRampasUsanUnMargenMasLargo_QueUnSaltoConTurbo()
        {
            // Un salto con turbo dura unos 35 m: con 10 m de margen se rebobinaba en pleno vuelo
            // (o recien aterrizado, antes de que el Landed cuente).
            Assert.AreEqual(45f, TutorialRewind.MargenRampa, 1e-4f);
            Assert.IsFalse(Decidir(CondicionPaso.Salto, Rampas, NoRampas, 200f).Rebobinar);
            Assert.IsFalse(Decidir(CondicionPaso.Salto, Rampas, NoRampas, 205f).Rebobinar);

            var d = Decidir(CondicionPaso.Salto, Rampas, NoRampas, 206f);
            Assert.IsTrue(d.Rebobinar);
            Assert.AreEqual(60f, d.PlayerX, 1e-4f); // 90 - 30: antes de la primera rampa no hay nada
        }

        [Test]
        public void NuncaRebobinaEnElAire()
        {
            // Ni por items...
            Assert.IsFalse(Decidir(CondicionPaso.Salto, Rampas, NoRampas, 400f, enElAire: true).Rebobinar);
            Assert.IsFalse(Decidir(CondicionPaso.PickupNafta, Bidones, NoBidones, 300f, enElAire: true).Rebobinar);
            // ...ni por la meta.
            Assert.IsFalse(Decidir(CondicionPaso.FaroSostenido, null, null, 640f, enElAire: true).Rebobinar);

            // Al aterrizar si.
            Assert.IsTrue(Decidir(CondicionPaso.Salto, Rampas, NoRampas, 400f).Rebobinar);
        }

        [Test]
        public void SinDistanciasDeItemNoRebobinaPorItems()
        {
            var d = Decidir(CondicionPaso.PickupNafta, new float[0], null, 900f);
            // Cae al chequeo de meta (900 > 650 - 40), asi que igual rebobina, pero por meta.
            Assert.IsTrue(d.Rebobinar);
            Assert.IsFalse(d.LimpiarItems);
            Assert.AreEqual(750f, d.PlayerX, 1e-4f); // 900 - 150
        }

        [Test]
        public void CondicionSinItemIgnoraLasDistanciasYUsaSoloElChequeoDeMeta()
        {
            Assert.IsFalse(Decidir(CondicionPaso.FaroSostenido, null, null, 100f).Rebobinar);
            Assert.IsFalse(Decidir(CondicionPaso.DisparoAcertado, Bidones, NoBidones, 500f).Rebobinar);
        }

        // --- Rebobinado por cercania a la meta --------------------------------------------

        [Test]
        public void NoRebobinaPorMetaMientrasFaltaMasDelMargen()
        {
            Assert.IsFalse(Decidir(CondicionPaso.FaroSostenido, null, null, 609f).Rebobinar);
        }

        [Test]
        public void RebobinaPorMetaAlEntrarEnElMargen()
        {
            var d = Decidir(CondicionPaso.FaroSostenido, null, null, 615f);
            Assert.IsTrue(d.Rebobinar);
            Assert.AreEqual(465f, d.PlayerX, 1e-4f); // 615 - 150
            Assert.IsFalse(d.LimpiarItems);
        }

        [Test]
        public void ElPasoDeDisparoRebobinaPorMeta()
        {
            // Sin item propio, el paso 9 solo tiene el guardia de la meta.
            var d = Decidir(CondicionPaso.DisparoAcertado, null, null, 615f);
            Assert.IsTrue(d.Rebobinar);
            Assert.IsFalse(d.LimpiarItems);
            Assert.AreEqual(465f, d.PlayerX, 1e-4f);
        }

        [Test]
        public void ElUltimoPasoNuncaRebobinaPorMeta()
        {
            Assert.IsFalse(Decidir(CondicionPaso.LlegarMeta, null, null, 649f, esUltimo: true).Rebobinar);
        }

        [Test]
        public void ElChequeoDeItemsTienePrioridadSobreElDeMeta()
        {
            // Un paso de items cerca de la meta: si se paso el item, rebobina por item (limpia
            // los items), no por meta.
            var d = Decidir(CondicionPaso.PickupMunicion, new[] { 370f, 400f, 430f }, new[] { 340f }, 615f);
            Assert.IsTrue(d.Rebobinar);
            Assert.IsTrue(d.LimpiarItems);
            Assert.AreEqual(348f, d.PlayerX, 1e-4f); // 370 - 30 = 340 es una bateria: 348
        }
    }
}
