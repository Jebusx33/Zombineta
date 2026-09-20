using NUnit.Framework;
using UnityEngine;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    public class FlickerMathTests
    {
        [Test]
        public void ItStaysBetweenMinAndMax()
        {
            for (float t = 0f; t < 5f; t += 0.05f)
            {
                float v = FlickerMath.Intensity(t, 7, 9f, 0.4f, 1f);
                Assert.GreaterOrEqual(v, 0.4f);
                Assert.LessOrEqual(v, 1f);
            }
        }

        [Test]
        public void TwoSeeds_DoNotBlinkTogether()
        {
            bool different = false;
            for (float t = 0f; t < 2f && !different; t += 0.05f)
                different = Mathf.Abs(FlickerMath.Intensity(t, 1, 9f, 0f, 1f) -
                                      FlickerMath.Intensity(t, 2, 9f, 0f, 1f)) > 0.05f;
            Assert.IsTrue(different);
        }

        [Test]
        public void FrequencyZero_IsASteadyLight()
        {
            Assert.AreEqual(FlickerMath.Intensity(0f, 3, 0f, 0.2f, 1f),
                            FlickerMath.Intensity(4f, 3, 0f, 0.2f, 1f), 1e-5f);
        }
    }
}
