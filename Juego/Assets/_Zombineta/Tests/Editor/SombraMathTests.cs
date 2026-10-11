using NUnit.Framework;
using UnityEngine;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    public class SombraMathTests
    {
        // Rehace la matriz con giro(fuera) * escala * giro(dentro) y la aplica a un punto.
        static Vector2 Aplicar(float giroFuera, Vector2 escala, float giroDentro, Vector2 p)
        {
            Vector2 q = Quaternion.Euler(0f, 0f, giroDentro) * p;
            q = new Vector2(q.x * escala.x, q.y * escala.y);
            return Quaternion.Euler(0f, 0f, giroFuera) * q;
        }

        [TestCase(0.8f, -0.5f)]   // luz arriba a la izquierda: cae hacia la camara y a la derecha
        [TestCase(-1.2f, -0.3f)]
        [TestCase(0.6f, 0.4f)]    // luz mas abajo que los pies: cae hacia el fondo
        [TestCase(0f, -0.6f)]     // luz justo encima: espejo aplastado
        public void TheTwoTransforms_RebuildTheSlantedShadow(float vx, float vy)
        {
            SombraMath.Descomponer(1f, vx, 0f, vy, out float fuera, out var escala, out float dentro);

            // El ancho no cambia y la punta (0, 1) cae en el vector de la sombra.
            var ancho = Aplicar(fuera, escala, dentro, new Vector2(1f, 0f));
            var punta = Aplicar(fuera, escala, dentro, new Vector2(0f, 1f));

            Assert.AreEqual(1f, ancho.x, 1e-4f);
            Assert.AreEqual(0f, ancho.y, 1e-4f);
            Assert.AreEqual(vx, punta.x, 1e-4f);
            Assert.AreEqual(vy, punta.y, 1e-4f);
        }

        [Test]
        public void TheShadow_FallsAwayFromTheLight()
        {
            // Luz arriba y a la izquierda de los pies.
            var v = SombraMath.Vector(new Vector2(0f, 0f), new Vector2(-3f, 4f), 10f, 0.5f, 1.5f, 0.5f);

            Assert.Greater(v.x, 0f, "se aleja hacia la derecha");
            Assert.Less(v.y, 0f, "y hacia la camara");
        }

        [Test]
        public void TheFartherTheLight_TheLongerTheShadow()
        {
            var cerca = SombraMath.Vector(Vector2.zero, new Vector2(-1f, 0f), 10f, 0.5f, 1.5f, 1f);
            var lejos = SombraMath.Vector(Vector2.zero, new Vector2(-9f, 0f), 10f, 0.5f, 1.5f, 1f);

            Assert.Greater(lejos.magnitude, cerca.magnitude);
            Assert.AreEqual(0.6f, cerca.magnitude, 1e-4f);
        }

        [Test]
        public void TheGround_IsSeenForeshortened()
        {
            var v = SombraMath.Vector(Vector2.zero, new Vector2(0f, 5f), 10f, 1f, 1f, 0.4f);

            Assert.AreEqual(0f, v.x, 1e-4f);
            Assert.AreEqual(-0.4f, v.y, 1e-4f);
        }

        [Test]
        public void FarFromEveryLight_TheShadowIsTheAmbientOne()
        {
            var ambiente = new Vector2(0.2f, -0.2f);

            Assert.AreEqual(ambiente, SombraMath.Mezclar(Vector2.zero, 0f, ambiente));
            Assert.AreEqual(0.7f, SombraMath.Intensidad(0f, 0.7f), 1e-4f);
        }

        [Test]
        public void UnderAFullLight_TheAmbientOneIsGone()
        {
            var deLaLuz = new Vector2(1f, -0.5f);

            var v = SombraMath.Mezclar(1f * deLaLuz, 1f, new Vector2(0.2f, -0.2f));

            Assert.AreEqual(deLaLuz.x, v.x, 1e-4f);
            Assert.AreEqual(deLaLuz.y, v.y, 1e-4f);
            Assert.AreEqual(1f, SombraMath.Intensidad(1f, 0.7f), 1e-4f);
        }

        [Test]
        public void ALightEnteringItsReach_DoesNotMakeTheShadowJump()
        {
            var ambiente = new Vector2(0.2f, -0.2f);
            var deLaLuz = new Vector2(-1.2f, -0.6f);

            // Peso apenas mayor que cero: la sombra casi no se mueve de la de ambiente.
            var v = SombraMath.Mezclar(0.01f * deLaLuz, 0.01f, ambiente);

            Assert.Less((v - ambiente).magnitude, 0.02f);
        }

        [Test]
        public void TwoLights_PullTheShadowBetweenThem()
        {
            var a = new Vector2(1f, -0.4f);
            var b = new Vector2(-1f, -0.4f);

            var v = SombraMath.Mezclar(0.6f * a + 0.6f * b, 1.2f, new Vector2(0.2f, -0.2f));

            Assert.AreEqual(0f, v.x, 1e-4f, "tiran igual para cada lado");
            Assert.AreEqual(-0.4f, v.y, 1e-4f);
        }

        [Test]
        public void ALight_WeighsLessWithDistance_AndNothingOutOfReach()
        {
            Assert.AreEqual(1f, SombraMath.Peso(0f, 10f, 1f), 1e-4f);
            Assert.AreEqual(0.5f, SombraMath.Peso(5f, 10f, 1f), 1e-4f);
            Assert.AreEqual(0f, SombraMath.Peso(10f, 10f, 1f), 1e-4f);
            Assert.AreEqual(0f, SombraMath.Peso(25f, 10f, 1f), 1e-4f);
            Assert.AreEqual(0.25f, SombraMath.Peso(5f, 10f, 0.5f), 1e-4f);
        }
    }
}
