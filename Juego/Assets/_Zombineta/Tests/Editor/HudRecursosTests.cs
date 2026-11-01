using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zombineta.Audio;
using Zombineta.UI;

namespace Zombineta.Juego.Tests
{
    public class HudRecursosTests
    {
        static readonly Color Lleno = Color.yellow;
        static readonly Color Alerta = Color.red;

        [TestCase(1f)]
        [TestCase(0.5f)]
        public void ConMuchoRecursoConservaSuColor(float fraccion)
        {
            Assert.AreEqual(Lleno, IndicadorDeRecurso.ColorDe(fraccion, Lleno, Alerta));
        }

        [Test]
        public void AlBajarVaCambiandoDeColor()
        {
            var medio = IndicadorDeRecurso.ColorDe(0.35f, Lleno, Alerta);
            Assert.AreNotEqual(Lleno, medio);
            Assert.AreNotEqual(Alerta, medio);
        }

        [TestCase(0.2f)]
        [TestCase(0f)]
        public void EnElTramoBajoQuedaEnColorDeAlerta(float fraccion)
        {
            Assert.AreEqual(Alerta, IndicadorDeRecurso.ColorDe(fraccion, Lleno, Alerta));
        }

        [Test]
        public void SoloTitilaEnElUltimoVeintePorCiento()
        {
            Assert.AreEqual(1f, IndicadorDeRecurso.Brillo(0.21f, 0.2f, 0.1f));
            Assert.AreEqual(1f, IndicadorDeRecurso.Brillo(0.2f, 0f, 0.1f));
            Assert.AreEqual(0.1f, IndicadorDeRecurso.Brillo(0.2f, 0.2f, 0.1f));
        }

        static AlertaDeRecurso Alarma(float inicio)
        {
            var a = new AlertaDeRecurso(0.2f, 5f);
            a.Reiniciar(inicio);
            return a;
        }

        [Test]
        public void AvisaAlEntrarEnElTramoBajo()
        {
            var a = Alarma(0.25f);
            Assert.IsFalse(a.Avanzar(0.21f, 0.1f));
            Assert.IsTrue(a.Avanzar(0.2f, 0.1f));
        }

        [Test]
        public void MientrasSeGastaRepiteElAviso()
        {
            var a = Alarma(0.21f);
            Assert.IsTrue(a.Avanzar(0.2f, 0.1f));
            Assert.IsFalse(a.Avanzar(0.19f, 4f));
            Assert.IsTrue(a.Avanzar(0.18f, 1f));
        }

        [Test]
        public void SiNoSeGastaNoInsiste()
        {
            var a = Alarma(0.21f);
            a.Avanzar(0.2f, 0.1f);
            Assert.IsFalse(a.Avanzar(0.2f, 60f));
        }

        [Test]
        public void ConElTanqueVacioSeCalla()
        {
            var a = Alarma(0.01f);
            a.Avanzar(0.005f, 0.1f);
            Assert.IsFalse(a.Avanzar(0f, 60f));
        }

        [Test]
        public void AlRecargarYVolverABajarAvisaDeNuevo()
        {
            var a = Alarma(0.21f);
            a.Avanzar(0.2f, 0.1f);
            a.Avanzar(0.8f, 0.1f);
            a.Avanzar(0.21f, 0.1f);
            Assert.IsTrue(a.Avanzar(0.2f, 0.1f));
        }

        [TestCase(SonidoClave.HudNaftaBaja)]
        [TestCase(SonidoClave.HudBateriaBaja)]
        public void LosAvisosTienenSonido(SonidoClave clave)
        {
            var banco = AssetDatabase.LoadAssetAtPath<BancoDeSonidos>("Assets/_Zombineta/Settings/Audio/SonidosGlobales.asset");
            var sonido = banco.Buscar(clave);
            Assert.IsNotNull(sonido);
            Assert.IsNotNull(sonido.clips[0]);
        }

        [Test]
        public void ElPrefabTieneTodoConectado()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Zombineta/Prefabs/HudPartida.prefab");
            var so = new SerializedObject(prefab.GetComponent<HudPaneles>());
            var campo = so.GetIterator();
            while (campo.NextVisible(true))
                if (campo.propertyType == SerializedPropertyType.ObjectReference && campo.name != "m_Script")
                    Assert.IsNotNull(campo.objectReferenceValue, campo.propertyPath);
            Assert.AreEqual(10, so.FindProperty("digitos").arraySize);
        }
    }
}
