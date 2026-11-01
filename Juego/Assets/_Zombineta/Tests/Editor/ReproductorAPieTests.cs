using NUnit.Framework;
using UnityEngine;
using Zombineta.Auxilio;

namespace Zombineta.Juego.Tests
{
    public class ReproductorAPieTests
    {
        AnimacionesAPie animaciones;

        static ClipAPie Clip(int cuadros, float porSegundo, bool enBucle = false)
        {
            var clip = new ClipAPie { cuadros = new Sprite[cuadros], cuadrosPorSegundo = porSegundo, enBucle = enBucle };
            for (int i = 0; i < cuadros; i++)
            {
                clip.cuadros[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
                clip.cuadros[i].name = i.ToString();
            }
            return clip;
        }

        [SetUp]
        public void Armar()
        {
            animaciones = ScriptableObject.CreateInstance<AnimacionesAPie>();
            animaciones.quieta = Clip(4, 10f, true);
            animaciones.camina = Clip(7, 10f, true);
            animaciones.golpeBajo = Clip(3, 10f);
            animaciones.golpeAlto = Clip(6, 10f);
            animaciones.lastimada = Clip(3, 10f);
            animaciones.derribada = Clip(5, 10f);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (var pose in (PoseAPie[])System.Enum.GetValues(typeof(PoseAPie)))
                foreach (var cuadro in animaciones.Clip(pose).cuadros)
                    Object.DestroyImmediate(cuadro);
            Object.DestroyImmediate(animaciones);
        }

        [Test]
        public void CaminarVuelveAEmpezarAlTerminarLosCuadros()
        {
            var r = new ReproductorAPie(animaciones);
            r.Avanzar(0.01f, true, false);
            Assert.AreEqual(PoseAPie.Camina, r.Actual);

            r.Avanzar(0.75f, true, false);

            Assert.AreEqual("0", r.Cuadro.name);
        }

        [Test]
        public void ElGolpeSeTerminaDeVerAunqueCamineYDespuesSigueCaminando()
        {
            var r = new ReproductorAPie(animaciones);
            r.Golpear(false);

            r.Avanzar(0.25f, true, false);
            Assert.AreEqual(PoseAPie.GolpeBajo, r.Actual);
            Assert.AreEqual("2", r.Cuadro.name);

            r.Avanzar(0.1f, true, false);
            Assert.AreEqual(PoseAPie.Camina, r.Actual);
        }

        [Test]
        public void ElRemateUsaElGolpeAlto()
        {
            var r = new ReproductorAPie(animaciones);
            r.Golpear(true);
            Assert.AreEqual(PoseAPie.GolpeAlto, r.Actual);
        }

        [Test]
        public void OtroGolpeReiniciaLaAnimacion()
        {
            var r = new ReproductorAPie(animaciones);
            r.Golpear(false);
            r.Avanzar(0.25f, false, false);

            r.Golpear(false);

            Assert.AreEqual("0", r.Cuadro.name);
        }

        [Test]
        public void ElDolorCortaUnGolpe()
        {
            var r = new ReproductorAPie(animaciones);
            r.Golpear(true);
            r.Lastimar();
            Assert.AreEqual(PoseAPie.Lastimada, r.Actual);
        }

        [Test]
        public void DerribadaSeQuedaEnElUltimoCuadroEIgnoraLosGolpes()
        {
            var r = new ReproductorAPie(animaciones);
            r.Avanzar(0.01f, false, true);

            r.Golpear(true);
            r.Lastimar();
            r.Avanzar(9f, false, true);

            Assert.AreEqual(PoseAPie.Derribada, r.Actual);
            Assert.AreEqual("4", r.Cuadro.name);
        }

        [Test]
        public void AlReintentarDespuesDeDerribadaVuelveAQuieta()
        {
            var r = new ReproductorAPie(animaciones);
            r.Avanzar(0.01f, false, true);

            r.Avanzar(0.01f, false, false);

            Assert.AreEqual(PoseAPie.Quieta, r.Actual);
        }
    }
}
