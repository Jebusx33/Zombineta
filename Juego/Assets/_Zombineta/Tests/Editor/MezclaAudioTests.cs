using NUnit.Framework;
using UnityEngine;
using Zombineta.Audio;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// MezclaAudio saco a un asset los numeros que antes estaban fijos en codigo. Sus valores por
    /// defecto tienen que ser exactamente los de antes (el juego suena igual sin tocar nada), y
    /// los consumidores puros (MotorTono, AudioBuses) tienen que respetar los valores nuevos.
    /// </summary>
    public class MezclaAudioTests
    {
        MezclaAudio m;

        [SetUp] public void SetUp() => m = ScriptableObject.CreateInstance<MezclaAudio>();

        [TearDown] public void TearDown() => Object.DestroyImmediate(m);

        [Test] public void LosValoresPorDefectoSonLosDeAntes()
        {
            Assert.AreEqual(MotorTono.Quieta, m.tonoQuieta, 1e-6f);
            Assert.AreEqual(MotorTono.Normal, m.tonoNormal, 1e-6f);
            Assert.AreEqual(MotorTono.Turbo, m.tonoTurbo, 1e-6f);
            Assert.AreEqual(0.15f, m.suavizadoTono, 1e-6f);
            Assert.AreEqual(1f, m.fundidoSinNafta, 1e-6f);
            Assert.AreEqual(0.3f, m.fundidoAlPerder, 1e-6f);
            Assert.AreEqual(45f, m.distanciaAmenaza, 1e-6f);
            Assert.AreEqual(0.5f, m.suavizadoTension, 1e-6f);
            Assert.AreEqual(new AudioBuses().MusicaEnPausa, m.musicaEnPausa, 1e-6f);
            Assert.AreEqual(0.2f, m.fundidoPausa, 1e-6f);
            Assert.AreEqual(new AudioBuses().ExponenteCurva, m.exponenteVolumen, 1e-6f);
        }

        [Test] public void PorDefectoNuncaEsNull()
        {
            Assert.IsNotNull(MezclaAudio.PorDefecto);
            Assert.AreSame(MezclaAudio.PorDefecto, MezclaAudio.PorDefecto);
        }

        [Test] public void MotorRespetaTonosPropios()
        {
            Assert.AreEqual(0.5f, MotorTono.Objetivo(0f, 10f, false, 0.5f, 1.5f, 2f), 1e-5f);
            Assert.AreEqual(1.0f, MotorTono.Objetivo(5f, 10f, false, 0.5f, 1.5f, 2f), 1e-5f);
            Assert.AreEqual(1.5f, MotorTono.Objetivo(10f, 10f, false, 0.5f, 1.5f, 2f), 1e-5f);
            Assert.AreEqual(2f, MotorTono.Objetivo(3f, 10f, true, 0.5f, 1.5f, 2f), 1e-5f);
        }

        [Test] public void MusicaEnPausaConfigurable()
        {
            var b = new AudioBuses { General = 1f, Musica = 1f, Efectos = 1f, PausaDuck = 0f, MusicaEnPausa = 0.7f };
            Assert.AreEqual(0.7f, b.Gain(AudioBus.Musica), 1e-5f);
            Assert.AreEqual(0f, b.Gain(AudioBus.Efectos), 1e-5f);
        }

        [Test] public void ExponenteLinealYCubico()
        {
            var b = new AudioBuses { General = 1f, Musica = 0.5f, Efectos = 1f, PausaDuck = 1f, ExponenteCurva = 1f };
            Assert.AreEqual(0.5f, b.Gain(AudioBus.Musica), 1e-5f);
            b.ExponenteCurva = 3f;
            Assert.AreEqual(0.125f, b.Gain(AudioBus.Musica), 1e-5f);
        }
    }
}
