using NUnit.Framework;
using UnityEngine;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    public class PerfilDeLuzTests
    {
        PerfilDeLuz perfil;

        [SetUp]
        public void SetUp()
        {
            perfil = ScriptableObject.CreateInstance<PerfilDeLuz>();
            perfil.capas.Add(new PerfilDeLuz.Ambiente { capa = "Fondo", color = new Color(0.2f, 0.25f, 0.6f), intensidad = 0.35f });
            perfil.capas.Add(new PerfilDeLuz.Ambiente { capa = "Calle", color = new Color(0.3f, 0.3f, 0.5f), intensidad = 0.2f });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(perfil);

        [Test]
        public void ItFindsTheAmbientOfALayer()
        {
            Assert.IsTrue(perfil.TryGet("Calle", out var color, out float intensidad));
            Assert.AreEqual(0.2f, intensidad, 1e-4f);
            Assert.AreEqual(new Color(0.3f, 0.3f, 0.5f), color);
        }

        [Test]
        public void ALayerWithoutAmbient_IsNotLit()
        {
            Assert.IsFalse(perfil.TryGet("Frente", out _, out _));
        }

        [Test]
        public void TheLookupIsCaseSensitive_LikeSortingLayers()
        {
            Assert.IsFalse(perfil.TryGet("calle", out _, out _));
        }

        // --- Resolver: arreglo 2 (Cielo y General), item 11b -----------------------------------

        [Test]
        public void EmptyProfile_ResolvesToNoLayers()
        {
            var vacio = ScriptableObject.CreateInstance<PerfilDeLuz>();
            var resultado = vacio.Resolver(new[] { "Cielo", "Fondo", "Calle", "Juego", "Frente" });
            Assert.AreEqual(0, resultado.Count);
            Object.DestroyImmediate(vacio);
        }

        [Test]
        public void ALayerWithOwnAmbient_ResolvesToColorTimesIntensity()
        {
            var resultado = perfil.Resolver(new[] { "Calle" });

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("Calle", resultado[0].capa);
            var esperado = new Color(0.3f, 0.3f, 0.5f) * 0.2f;
            esperado.a = 1f;
            AssertColorApprox(esperado, resultado[0].color);
        }

        [Test]
        public void ALayerWithoutOwnAmbientAndWithoutGeneral_IsNotResolved()
        {
            var resultado = perfil.Resolver(new[] { "Frente" });
            Assert.AreEqual(0, resultado.Count);
        }

        [Test]
        public void GeneralAlone_CreatesTheLayerEvenWithoutItsOwnAmbient()
        {
            perfil.colorGeneral = new Color(0.1f, 0.1f, 0.1f);
            perfil.intensidadGeneral = 1f;

            var resultado = perfil.Resolver(new[] { "Frente" });

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("Frente", resultado[0].capa);
            AssertColorApprox(new Color(0.1f, 0.1f, 0.1f, 1f), resultado[0].color);
        }

        [Test]
        public void GeneralSumsOnTopOfTheLayersOwnAmbient()
        {
            perfil.colorGeneral = new Color(0.1f, 0f, 0f);
            perfil.intensidadGeneral = 1f;

            var resultado = perfil.Resolver(new[] { "Calle" });

            var esperado = new Color(0.3f, 0.3f, 0.5f) * 0.2f + new Color(0.1f, 0f, 0f);
            esperado.a = 1f;
            AssertColorApprox(esperado, resultado[0].color);
        }

        static void AssertColorApprox(Color esperado, Color actual)
        {
            const float tolerancia = 1e-4f;
            Assert.AreEqual(esperado.r, actual.r, tolerancia);
            Assert.AreEqual(esperado.g, actual.g, tolerancia);
            Assert.AreEqual(esperado.b, actual.b, tolerancia);
            Assert.AreEqual(esperado.a, actual.a, tolerancia);
        }
    }
}
