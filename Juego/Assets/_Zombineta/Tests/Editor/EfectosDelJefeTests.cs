using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zombineta.Jefe;

namespace Zombineta.Juego.Tests
{
    /// <summary>El humo del tanque acompana al motor, y los prefabs del plasma traen las capas que usa EfectosDelJefe.</summary>
    public class EfectosDelJefeTests
    {
        const float Rodando = 9f, AFondo = 42f, Hollin = 26f, Aviso = 1.2f;

        static (float humo, float hollin) Humo(FaseDeJefe fase, float t = 0f) =>
            EfectosDelJefe.Humo(fase, t, Aviso, Rodando, AFondo, Hollin);

        [Test]
        public void SinDueloNoHumea()
        {
            Assert.AreEqual((0f, 0f), Humo(FaseDeJefe.Inactivo));
        }

        [Test]
        public void RodandoHumeaPocoYSinHollin()
        {
            Assert.AreEqual((Rodando, 0f), Humo(FaseDeJefe.Persecucion));
            Assert.AreEqual((Rodando, 0f), Humo(FaseDeJefe.Disparo));
        }

        [Test]
        public void AlAcelerarParaLaEmbestidaHumeaCadaVezMasYEchaHollin()
        {
            var inicio = Humo(FaseDeJefe.AvisoEmbestida, 0f);
            var medio = Humo(FaseDeJefe.AvisoEmbestida, Aviso * 0.5f);
            var fin = Humo(FaseDeJefe.AvisoEmbestida, Aviso);

            Assert.AreEqual(Rodando, inicio.humo, 0.001f);
            Assert.That(medio.humo, Is.GreaterThan(inicio.humo));
            Assert.That(medio.hollin, Is.GreaterThan(0f));
            Assert.AreEqual((AFondo, Hollin), fin);
            Assert.AreEqual((AFondo, Hollin), Humo(FaseDeJefe.Embestida));
        }

        [Test]
        public void AturdidoApenasHumea()
        {
            var (humo, hollin) = Humo(FaseDeJefe.Aturdido);
            Assert.That(humo, Is.LessThan(Rodando));
            Assert.AreEqual(0f, hollin);
        }

        static ParticleSystem Prefab(string nombre)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Zombineta/Prefabs/" + nombre + ".prefab");
            Assert.IsNotNull(go, nombre);
            return go.GetComponent<ParticleSystem>();
        }

        // EfectosDelJefe maneja la cantidad de estas capas por su indice: el orden en que las arma FxBuilder.
        [Test]
        public void ElHumoYLaEstelaTraenSusDosCapasContinuas()
        {
            foreach (var (nombre, hijo) in new[] { ("FxJefeHumo", "Hollin"), ("FxPlasmaEstela", "Vaho") })
            {
                var capas = Prefab(nombre).GetComponentsInChildren<ParticleSystem>(true);
                Assert.AreEqual(2, capas.Length, nombre);
                Assert.AreEqual(hijo, capas[1].name, nombre);
                foreach (var c in capas)
                    Assert.IsTrue(c.main.loop, nombre + "/" + c.name);
            }
        }

        [Test]
        public void ElDisparoYElImpactoSonRafagasConPiso()
        {
            foreach (var nombre in new[] { "FxPlasmaDisparo", "FxPlasmaImpacto" })
            {
                var raiz = Prefab(nombre);
                Assert.IsFalse(raiz.main.loop, nombre);
                Assert.IsNotNull(raiz.transform.Find("Piso"), nombre + ": sin piso las gotas no salpican");
            }
            Assert.IsNotNull(Prefab("FxPlasmaImpacto").transform.Find("Piso/Charco"), "el charco cuelga del piso");
        }
    }
}
