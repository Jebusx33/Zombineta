using NUnit.Framework;
using UnityEngine;
using Zombineta.CameraFx;

namespace Zombineta.Tests
{
    public class CameraDirectorTests
    {
        const float Dt = 1f / 60f;
        const float Aspect = 16f / 9f;

        static CameraConfig Cfg()
        {
            // Valores propios del test: el asset real se puede reajustar sin romper nada.
            var c = ScriptableObject.CreateInstance<CameraConfig>();
            c.baseSize = 6f;
            c.viewBottomY = -4.6f;
            c.lookAhead = 6f;
            c.followTime = 0.12f;
            c.wideSize = 6.3f;
            c.tightSize = 5f;
            c.safeGap = 45f;
            c.dangerGap = 8f;
            c.zoomInTime = 0.9f;
            c.zoomOutTime = 0.3f;
            c.turboExtraSize = 0.4f;
            c.turboExtraLookAhead = 1.5f;
            c.turboResponseTime = 0.3f;
            c.maxShakeOffset = 0.4f;
            c.maxShakeRoll = 1f;
            c.shakeFrequency = 18f;
            c.traumaDecay = 1.5f;
            c.punchFrequency = 3.5f;
            c.punchDamping = 0.4f;
            c.catchSize = 3.6f;
            c.catchZoomTime = 0.18f;
            c.victorySize = 7.5f;
            c.victoryOpenTime = 1f;
            c.jumpHeadroom = 2f;
            c.jumpZoomTime = 0.15f;
            return c;
        }

        static CameraDirector Make(float gap = 45f)
        {
            var d = new CameraDirector(Cfg()) { Aspect = Aspect };
            d.Snap(In(gap));
            return d;
        }

        static CameraInput In(float gap, bool turbo = false, float playerX = 0f) =>
            new CameraInput { PlayerX = playerX, PlayerY = 0f, GapMeters = gap, Turbo = turbo, GoalX = 1000f };

        static CameraPose Run(CameraDirector d, CameraInput input, float seconds, float dt = Dt)
        {
            // Cantidad entera de pasos: con t += dt el redondeo puede sumar un paso de mas.
            var pose = default(CameraPose);
            int steps = Mathf.RoundToInt(seconds / dt);
            for (int i = 0; i < steps; i++)
                pose = d.Step(input, dt);
            return pose;
        }

        static float SecondsToReach(CameraDirector d, CameraInput input, float target, bool downwards)
        {
            float t = 0f;
            while (t < 10f)
            {
                var p = d.Step(input, Dt);
                t += Dt;
                if (downwards ? p.Size <= target : p.Size >= target) return t;
            }
            return float.MaxValue;
        }

        // --- Tension --------------------------------------------------------

        [Test]
        public void Threat_IsZeroWhenSafe_OneInDanger_AndNeverGoesBackwards()
        {
            Assert.AreEqual(0f, CameraDirector.ThreatFromGap(60f, 45f, 8f));
            Assert.AreEqual(0f, CameraDirector.ThreatFromGap(45f, 45f, 8f));
            Assert.AreEqual(1f, CameraDirector.ThreatFromGap(8f, 45f, 8f));
            Assert.AreEqual(1f, CameraDirector.ThreatFromGap(-3f, 45f, 8f), "ya te alcanzo");

            float last = -1f;
            for (float gap = 60f; gap >= 0f; gap -= 0.5f)
            {
                float t = CameraDirector.ThreatFromGap(gap, 45f, 8f);
                Assert.GreaterOrEqual(t, last, "acercarse nunca baja la tension");
                last = t;
            }
        }

        [Test]
        public void TheCloserTheHorde_TheTighterTheShot()
        {
            var far = Run(Make(45f), In(45f), 4f);
            var close = Run(Make(8f), In(8f), 4f);

            Assert.AreEqual(6.3f, far.Size, 0.01f);
            Assert.AreEqual(5f, close.Size, 0.01f);
        }

        [Test]
        public void ZoomingIn_KeepsTheRoadAheadVisible()
        {
            // La decision de diseno central: el plano se cierra desde atras. La moto queda
            // siempre a lookAhead del borde derecho, este cerrado o abierto.
            foreach (float gap in new[] { 60f, 30f, 15f, 8f, 2f })
            {
                var d = Make(gap);
                var p = Run(d, In(gap, playerX: 50f), 4f);
                float rightEdge = p.X + d.HalfWidth(p.Size);
                Assert.AreEqual(6f, rightEdge - 50f, 0.01f, "gap " + gap);
            }
        }

        [Test]
        public void TheBottomOfTheScreenStaysPut_WhileZooming()
        {
            // Asi la calle no se corre: al cerrarse se pierde cielo, no piso.
            foreach (float gap in new[] { 60f, 8f })
            {
                var p = Run(Make(gap), In(gap), 4f);
                Assert.AreEqual(-4.6f, p.Y - p.Size, 0.01f);
            }
        }

        [Test]
        public void PushingTheHordeBack_OpensFasterThanItClosed()
        {
            // Cerrarse es lento (el miedo se instala); abrirse es rapido (el alivio se siente ya).
            var d = Make(45f);
            float closing = SecondsToReach(d, In(8f), 5f + 0.1f * 1.3f, downwards: true);
            float opening = SecondsToReach(d, In(45f), 6.3f - 0.1f * 1.3f, downwards: false);

            Assert.Less(opening, closing * 0.5f);
        }

        [Test]
        public void Turbo_WidensTheShot_AndLetsYouSeeFurtherAhead()
        {
            var normal = Make(30f);
            var fast = Make(30f);
            var pn = Run(normal, In(30f, turbo: false, playerX: 20f), 3f);
            var pt = Run(fast, In(30f, turbo: true, playerX: 20f), 3f);

            Assert.Greater(pt.Size, pn.Size + 0.3f);
            float aheadNormal = pn.X + normal.HalfWidth(pn.Size) - 20f;
            float aheadFast = pt.X + fast.HalfWidth(pt.Size) - 20f;
            Assert.AreEqual(1.5f, aheadFast - aheadNormal, 0.02f);
        }

        // --- Interruptores de Opciones ---------------------------------------

        [Test]
        public void WithEffectsOff_TheCameraIsTheClassicOne()
        {
            var d = new CameraDirector(Cfg()) { Aspect = Aspect, EffectsEnabled = false };
            d.Snap(In(8f, turbo: true));
            d.AddTrauma(1f);
            d.Punch(1f, -1f);
            d.BeginCatch();

            var p = Run(d, In(8f, turbo: true, playerX: 10f), 2f);

            Assert.AreEqual(6f, p.Size, 0.001f, "tamano fijo, sin tension ni turbo ni atrapada");
            Assert.AreEqual(10f + 6f - d.HalfWidth(6f), p.X, 0.01f);
            Assert.AreEqual(0f, p.Roll);
        }

        [Test]
        public void WithShakeOff_ThereAreNoShakesOrPunches_ButTheTensionFramingStays()
        {
            var calm = Make(8f);
            var hit = Make(8f);
            hit.ShakeEnabled = false;
            hit.AddTrauma(1f);
            hit.Punch(1f, -1f);

            var pc = Run(calm, In(8f), 0.25f);
            var ph = Run(hit, In(8f), 0.25f);

            Assert.AreEqual(pc.X, ph.X, 1e-4f);
            Assert.AreEqual(pc.Y, ph.Y, 1e-4f);
            Assert.AreEqual(pc.Size, ph.Size, 1e-4f);
            Assert.AreEqual(0f, ph.Roll);
            Assert.AreEqual(5f, ph.Size, 0.05f, "el plano sigue cerrado por la horda");
        }

        // --- Golpes -----------------------------------------------------------

        [Test]
        public void ACrash_ShakesTheCamera_AndTheShakeFadesAway()
        {
            var d = Make(30f);
            d.AddTrauma(0.6f);
            bool moved = false;
            for (int i = 0; i < 20; i++)
            {
                var p = d.Step(In(30f), Dt);
                moved |= Mathf.Abs(p.Roll) > 1e-3f;
            }
            Assert.IsTrue(moved, "con trauma la camara tiembla");

            Run(d, In(30f), 1f);
            Assert.AreEqual(0f, d.Trauma, "el trauma se disipa");
            Assert.AreEqual(0f, d.Step(In(30f), Dt).Roll);
        }

        [Test]
        public void APunch_OvershootsAndComesBackToRest()
        {
            var d = Make(30f);
            var still = Make(30f);
            d.Punch(0.6f, -0.45f);

            float maxForward = 0f, minZoom = 0f;
            for (int i = 0; i < 30; i++)
            {
                var p = d.Step(In(30f), Dt);
                var s = still.Step(In(30f), Dt);
                maxForward = Mathf.Max(maxForward, p.X - s.X);
                minZoom = Mathf.Min(minZoom, p.Size - s.Size);
            }
            Assert.Greater(maxForward, 0.05f, "latigazo hacia adelante");
            Assert.Less(minZoom, -0.05f, "golpe de zoom hacia adentro");

            var pEnd = Run(d, In(30f), 3f);
            var sEnd = Run(still, In(30f), 3f);
            Assert.AreEqual(sEnd.X, pEnd.X, 0.005f, "vuelve a su lugar");
            Assert.AreEqual(sEnd.Size, pEnd.Size, 0.005f);
        }

        // --- Finales ----------------------------------------------------------

        [Test]
        public void BeingCaught_ClosesTheShotOnTheScooter()
        {
            var d = Make(3f);
            d.BeginCatch();
            var p = Run(d, In(0f, playerX: 40f), 1.5f);

            Assert.AreEqual(3.6f, p.Size, 0.02f);
            Assert.AreEqual(40f, p.X, 0.05f, "centrada en la moto");
        }

        [Test]
        public void ReachingTheRefuge_OpensTheShotOverIt()
        {
            var d = Make(30f);
            d.Snap(In(30f, playerX: 998f));   // llegando a la meta, no desde la largada
            d.BeginVictory();
            var p = Run(d, In(30f, playerX: 998f), 6f);

            Assert.AreEqual(7.5f, p.Size, 0.02f);
            Assert.AreEqual(1000f, p.X, 0.05f, "centrada en el refugio");
        }

        [Test]
        public void Snap_GoesBackToFollowing_AndJumpsStraightToTheShot()
        {
            var d = Make(30f);
            d.BeginCatch();
            d.AddTrauma(1f);

            d.Snap(In(8f));

            Assert.AreEqual(CameraMode.Follow, d.Mode);
            Assert.AreEqual(0f, d.Trauma);
            Assert.AreEqual(5f, d.Step(In(8f), 0f).Size, 0.001f);
        }

        [Test]
        public void TheShotIsTheSameAt30AndAt144FramesPerSecond()
        {
            var slow = Make(45f);
            var fast = Make(45f);
            var ps = Run(slow, In(10f, playerX: 5f), 1f, 1f / 30f);
            var pf = Run(fast, In(10f, playerX: 5f), 1f, 1f / 144f);

            Assert.AreEqual(ps.Size, pf.Size, 0.02f);
            Assert.AreEqual(ps.X, pf.X, 0.03f);
        }

        [Test]
        public void Jump_OpensTheShotSoTheScooterStaysInFrame()
        {
            var d = Make(gap: 6f);              // horda encima: plano cerrado (5)
            var input = In(6f);
            input.PlayerY = 1.6f;               // carril de arriba
            input.JumpHeight = 3.3f;            // pico de un salto en turbo

            var pose = Run(d, input, 1f);

            float top = pose.Y + pose.Size;     // borde de arriba de la pantalla
            Assert.GreaterOrEqual(top, 1.6f + 3.3f + 2f - 0.05f);
        }

        [Test]
        public void NoJump_KeepsTheTensionFraming()
        {
            var d = Make(gap: 6f);
            var pose = Run(d, In(6f), 3f);

            Assert.AreEqual(5f, pose.Size, 0.02f);
        }
    }
}
