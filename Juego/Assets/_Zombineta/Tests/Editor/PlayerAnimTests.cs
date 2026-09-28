using NUnit.Framework;
using Zombineta.Player;

namespace Zombineta.Juego.Tests
{
    public class PlayerAnimTests
    {
        static PlayerAnim Nueva() => new PlayerAnim(4, 8, 8, 12, 12, 4f, 16f, 16f, 20f, 8f);

        [Test] public void IdleDaLaVueltaSola()
        {
            var a = Nueva();
            for (int i = 0; i < 20; i++) a.Tick(0.05f); // 1 s a 4 fps: dio la vuelta justo
            Assert.AreEqual(PlayerClip.Idle, a.Clip);
            Assert.AreEqual(0, a.Frame);
        }

        [Test] public void DownVuelveSolaAIdle()
        {
            var a = Nueva();
            a.Play(PlayerClip.Down);
            a.Tick(0.1f); // 0,1 s x 16 fps = cuadro 1
            Assert.AreEqual(PlayerClip.Down, a.Clip);
            Assert.AreEqual(1, a.Frame);
            a.Tick(0.4f); // 0,5 s totales x 16 fps = 8: termino (8 cuadros)
            Assert.AreEqual(PlayerClip.Idle, a.Clip);
        }

        [Test] public void UpVuelveSolaAIdle()
        {
            var a = Nueva();
            a.Play(PlayerClip.Up);
            a.Tick(0.5f);
            Assert.AreEqual(PlayerClip.Idle, a.Clip);
        }

        [Test] public void ShootVuelveSolaAIdle()
        {
            var a = Nueva();
            a.Play(PlayerClip.Shoot);
            a.Tick(0.3f); // 0,3 s x 20 fps = cuadro 6, todavia dentro de los 12
            Assert.AreEqual(PlayerClip.Shoot, a.Clip);
            Assert.AreEqual(6, a.Frame);
            a.Tick(0.4f); // 0,7 s totales x 20 fps = 14 >= 12: termino
            Assert.AreEqual(PlayerClip.Idle, a.Clip);
        }

        [Test] public void CrashSeCongelaEnElUltimoCuadro()
        {
            var a = Nueva();
            a.Play(PlayerClip.Crash);
            for (int i = 0; i < 30; i++) a.Tick(0.1f); // de sobra: 12 cuadros a 8 fps son 1,5 s
            Assert.AreEqual(PlayerClip.Crash, a.Clip);
            Assert.AreEqual(11, a.Frame);
            Assert.IsTrue(a.Finished);
        }

        [Test] public void JugarDeVueltaReiniciaElCuadro()
        {
            var a = Nueva();
            a.Play(PlayerClip.Shoot);
            a.Tick(0.2f);
            a.Play(PlayerClip.Shoot); // un segundo disparo antes de que termine el primero
            Assert.AreEqual(0, a.Frame);
            Assert.IsFalse(a.Finished);
        }

        [Test] public void ClipsSinCuadrosNuncaSeVanDeRango()
        {
            var a = new PlayerAnim(0, 0, 0, 0, 0, 4f, 16f, 16f, 20f, 8f);
            a.Play(PlayerClip.Down);
            a.Tick(1f);
            Assert.AreEqual(0, a.Frame);
            a.Play(PlayerClip.Crash);
            a.Tick(1f);
            Assert.AreEqual(0, a.Frame);
        }
    }
}
