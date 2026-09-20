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
    }
}
