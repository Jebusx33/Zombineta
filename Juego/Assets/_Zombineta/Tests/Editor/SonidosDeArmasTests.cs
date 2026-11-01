using NUnit.Framework;
using UnityEditor;
using Zombineta.Audio;
using Zombineta.Core;

namespace Zombineta.Juego.Tests
{
    public class SonidosDeArmasTests
    {
        static BancoDeSonidos Banco() =>
            AssetDatabase.LoadAssetAtPath<BancoDeSonidos>("Assets/_Zombineta/Settings/Audio/SonidosGlobales.asset");

        [TestCase(SonidoClave.Disparo, "player_pistol_shot")]
        [TestCase(SonidoClave.SinBalas, "player_empty_gun")]
        [TestCase(SonidoClave.SinNafta, "moto_apaga_motor")]
        [TestCase(SonidoClave.GranadaExplota, "player_granade")]
        [TestCase(SonidoClave.BombaPuesta, "player_c4_mecha")]
        [TestCase(SonidoClave.BombaExplota, "player_c4_explosion")]
        public void CadaEventoSuenaConSuClip(SonidoClave clave, string clip)
        {
            var sonido = Banco().Buscar(clave);
            Assert.IsNotNull(sonido, clave.ToString());
            Assert.AreEqual(1, sonido.clips.Length);
            Assert.AreEqual(clip, sonido.clips[0].name);
        }

        [TestCase(RunEvent.ShotDenied, 3, 3, true)]
        [TestCase(RunEvent.GranadaDenegada, 0, 3, true)]
        [TestCase(RunEvent.BombaDenegada, 3, 0, true)]
        [TestCase(RunEvent.GranadaDenegada, 1, 3, false)]
        [TestCase(RunEvent.BombaDenegada, 3, 1, false)]
        [TestCase(RunEvent.None, 0, 0, false)]
        public void ElGatilloVacioSoloSuenaSiNoQuedaNada(RunEvent ev, int granadas, int bombas, bool suena)
        {
            Assert.AreEqual(suena, SfxDirector.SinMunicion(ev, granadas, bombas));
        }

        // La explosion es otro clip que dispara la simulacion: la mecha tiene que terminar justo ahi.
        [Test]
        public void LaMechaDuraLoMismoQueLaBomba()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Zombineta/Settings/GameConfig.asset");
            var mecha = Banco().Buscar(SonidoClave.BombaPuesta);

            Assert.AreEqual(config.bombaMecha, mecha.clips[0].length, 0.02f);
            Assert.AreEqual(1f, mecha.tono.x);
            Assert.AreEqual(1f, mecha.tono.y);
        }
    }
}
