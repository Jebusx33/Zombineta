using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Zombineta.Audio;
using Zombineta.Core;
using Zombineta.Flow;

namespace Zombineta.Juego.Tests
{
    public class AudioDataTests
    {
        // --- Banco de sonidos: Resolver ---

        static Sonido NuevoSonido() => ScriptableObject.CreateInstance<Sonido>();

        static BancoDeSonidos NuevoBanco(params (SonidoClave clave, Sonido sonido)[] entradas)
        {
            var banco = ScriptableObject.CreateInstance<BancoDeSonidos>();
            foreach (var e in entradas)
                banco.entradas.Add(new BancoDeSonidos.Entrada { clave = e.clave, sonido = e.sonido });
            return banco;
        }

        [Test] public void ResolverNivelPisaGlobal()
        {
            var sNivel = NuevoSonido();
            var sGlobal = NuevoSonido();
            var nivel = NuevoBanco((SonidoClave.Disparo, sNivel));
            var global = NuevoBanco((SonidoClave.Disparo, sGlobal));
            Assert.AreSame(sNivel, BancoDeSonidos.Resolver(nivel, global, SonidoClave.Disparo));
        }

        [Test] public void ResolverClaveQueElNivelNoTieneCaeAlGlobal()
        {
            var sGlobal = NuevoSonido();
            var nivel = NuevoBanco((SonidoClave.Disparo, NuevoSonido()));
            var global = NuevoBanco((SonidoClave.Choque, sGlobal));
            Assert.AreSame(sGlobal, BancoDeSonidos.Resolver(nivel, global, SonidoClave.Choque));
        }

        [Test] public void ResolverNingunoTieneLaClaveEsNull()
        {
            var nivel = NuevoBanco((SonidoClave.Disparo, NuevoSonido()));
            var global = NuevoBanco((SonidoClave.Choque, NuevoSonido()));
            Assert.IsNull(BancoDeSonidos.Resolver(nivel, global, SonidoClave.Salto));
        }

        [Test] public void ResolverBancoDeNivelNuloCaeAlGlobal()
        {
            var sGlobal = NuevoSonido();
            var global = NuevoBanco((SonidoClave.Disparo, sGlobal));
            Assert.AreSame(sGlobal, BancoDeSonidos.Resolver(null, global, SonidoClave.Disparo));
        }

        // --- MusicaDelJuego.Para ---

        static AudioClip ClipDeMentira(string nombre) => AudioClip.Create(nombre, 1, 1, 44100, false);

        [Test] public void ParaCadaPantallaDevuelveSuCampo()
        {
            var m = ScriptableObject.CreateInstance<MusicaDelJuego>();
            m.menu = ClipDeMentira("menu");
            m.opciones = ClipDeMentira("opciones");
            m.personaje = ClipDeMentira("personaje");
            m.cinematica = ClipDeMentira("cinematica");
            m.nivelCompleto = ClipDeMentira("nivelCompleto");
            m.gameOver = ClipDeMentira("gameOver");
            m.ending = ClipDeMentira("ending");
            m.creditos = ClipDeMentira("creditos");

            Assert.AreEqual(m.menu, m.Para(GameScreen.MainMenu));
            Assert.AreEqual(m.opciones, m.Para(GameScreen.Options));
            Assert.AreEqual(m.personaje, m.Para(GameScreen.CharacterSelect));
            Assert.AreEqual(m.cinematica, m.Para(GameScreen.Cinematic));
            Assert.AreEqual(m.nivelCompleto, m.Para(GameScreen.LevelComplete));
            Assert.AreEqual(m.gameOver, m.Para(GameScreen.GameOver));
            Assert.AreEqual(m.ending, m.Para(GameScreen.Ending));
            Assert.AreEqual(m.creditos, m.Para(GameScreen.Credits));
        }

        [Test] public void ParaPlayingYPausedDevuelveNull()
        {
            var m = ScriptableObject.CreateInstance<MusicaDelJuego>();
            m.menu = ClipDeMentira("menu");
            Assert.IsNull(m.Para(GameScreen.Playing));
            Assert.IsNull(m.Para(GameScreen.Paused));
        }

        // --- Volumenes ---

        const string MusicKey = "zombineta.musicVolume";
        const string SfxKey = "zombineta.sfxVolume";
        float? prevMusic;
        float? prevSfx;

        [SetUp] public void SetUp()
        {
            prevMusic = PlayerPrefs.HasKey(MusicKey) ? (float?)PlayerPrefs.GetFloat(MusicKey) : null;
            prevSfx = PlayerPrefs.HasKey(SfxKey) ? (float?)PlayerPrefs.GetFloat(SfxKey) : null;
        }

        [TearDown] public void TearDown()
        {
            if (prevMusic.HasValue) PlayerPrefs.SetFloat(MusicKey, prevMusic.Value); else PlayerPrefs.DeleteKey(MusicKey);
            if (prevSfx.HasValue) PlayerPrefs.SetFloat(SfxKey, prevSfx.Value); else PlayerPrefs.DeleteKey(SfxKey);
            ResetCache();
        }

        // GameSettings cachea el valor leido en un campo estatico privado (mismo patron que
        // Volume). Para que cada test lea PlayerPrefs de nuevo hay que limpiar ese cache.
        static void ResetCache()
        {
            var t = typeof(GameSettings);
            t.GetField("musicVolume", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            t.GetField("sfxVolume", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
        }

        [Test] public void MusicVolumeDefaultEsPuntoOcho()
        {
            PlayerPrefs.DeleteKey(MusicKey);
            ResetCache();
            Assert.AreEqual(0.8f, GameSettings.MusicVolume, 1e-5f);
        }

        [Test] public void SfxVolumeDefaultEsPuntoOcho()
        {
            PlayerPrefs.DeleteKey(SfxKey);
            ResetCache();
            Assert.AreEqual(0.8f, GameSettings.SfxVolume, 1e-5f);
        }

        [Test] public void LosVolumenesSeClampean()
        {
            GameSettings.MusicVolume = 2f;
            Assert.AreEqual(1f, GameSettings.MusicVolume, 1e-5f);
            GameSettings.MusicVolume = -1f;
            Assert.AreEqual(0f, GameSettings.MusicVolume, 1e-5f);

            GameSettings.SfxVolume = 5f;
            Assert.AreEqual(1f, GameSettings.SfxVolume, 1e-5f);
            GameSettings.SfxVolume = -5f;
            Assert.AreEqual(0f, GameSettings.SfxVolume, 1e-5f);
        }
    }
}
