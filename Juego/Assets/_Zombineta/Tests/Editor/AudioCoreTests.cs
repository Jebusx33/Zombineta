using NUnit.Framework;
using Zombineta.Audio;

namespace Zombineta.Juego.Tests
{
    public class AudioCoreTests
    {
        // --- Buses ---
        [Test] public void CurvaEsCuadratica()
        {
            Assert.AreEqual(0f, AudioBuses.Curva(0f), 1e-5f);
            Assert.AreEqual(0.25f, AudioBuses.Curva(0.5f), 1e-5f);
            Assert.AreEqual(1f, AudioBuses.Curva(1f), 1e-5f);
            Assert.AreEqual(1f, AudioBuses.Curva(2f), 1e-5f);
            Assert.AreEqual(0f, AudioBuses.Curva(-1f), 1e-5f);
        }

        [Test] public void GananciaMultiplicaBusYGeneral()
        {
            var b = new AudioBuses { General = 1f, Musica = 0.5f, Efectos = 1f, PausaDuck = 1f };
            Assert.AreEqual(0.25f, b.Gain(AudioBus.Musica), 1e-5f);
            b.General = 0.5f;
            Assert.AreEqual(0.0625f, b.Gain(AudioBus.Musica), 1e-5f);
        }

        [Test] public void UiYAmbienteCuelganDeEfectos()
        {
            var b = new AudioBuses { General = 1f, Musica = 1f, Efectos = 0.5f, PausaDuck = 1f };
            Assert.AreEqual(0.25f, b.Gain(AudioBus.UI), 1e-5f);
            Assert.AreEqual(0.25f, b.Gain(AudioBus.Ambiente), 1e-5f);
        }

        [Test] public void PausaCallaEfectosYAmbientePeroNoUi()
        {
            var b = new AudioBuses { General = 1f, Musica = 1f, Efectos = 1f, PausaDuck = 0f };
            Assert.AreEqual(0f, b.Gain(AudioBus.Efectos), 1e-5f);
            Assert.AreEqual(0f, b.Gain(AudioBus.Ambiente), 1e-5f);
            Assert.AreEqual(1f, b.Gain(AudioBus.UI), 1e-5f);
        }

        [Test] public void PausaBajaMusicaA40PorCiento()
        {
            var b = new AudioBuses { General = 1f, Musica = 1f, Efectos = 1f, PausaDuck = 0f };
            Assert.AreEqual(0.4f, b.Gain(AudioBus.Musica), 1e-5f);
        }

        // --- Variantes ---
        [Test] public void VarianteNuncaRepiteLaAnterior()
        {
            var p = new VariantPicker(7);
            int last = p.Pick(3);
            for (int i = 0; i < 200; i++)
            {
                int next = p.Pick(3);
                Assert.AreNotEqual(last, next);
                Assert.That(next, Is.InRange(0, 2));
                last = next;
            }
        }

        [Test] public void VarianteConUnaSolaOpcionEsCero()
        {
            var p = new VariantPicker(1);
            Assert.AreEqual(0, p.Pick(1));
            Assert.AreEqual(0, p.Pick(1));
            Assert.AreEqual(-1, p.Pick(0));
        }

        [Test] public void VarianteEsDeterministaConLaMismaSemilla()
        {
            var a = new VariantPicker(42);
            var b = new VariantPicker(42);
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(a.Pick(5), b.Pick(5));
        }

        [Test] public void VariantePrimeraPuedeSerCualquiera()
        {
            bool saw0 = false, saw1 = false, saw2 = false;
            for (int seed = 0; seed < 100; seed++)
            {
                var p = new VariantPicker(seed);
                int first = p.Pick(3);
                Assert.That(first, Is.InRange(0, 2));
                if (first == 0) saw0 = true;
                if (first == 1) saw1 = true;
                if (first == 2) saw2 = true;
            }
            Assert.IsTrue(saw0);
            Assert.IsTrue(saw1);
            Assert.IsTrue(saw2);
        }

        // --- Espacial ---
        static readonly EspacialConfig C = new EspacialConfig { anchoPaneo = 12f, distanciaPlena = 4f, distanciaMaxima = 30f };

        [Test] public void PaneoAtrasIzquierdaAdelanteDerecha()
        {
            Assert.AreEqual(-0.5f, SonidoEspacial.Pan(-6f, C), 1e-5f);
            Assert.AreEqual(0.5f, SonidoEspacial.Pan(6f, C), 1e-5f);
            Assert.AreEqual(-1f, SonidoEspacial.Pan(-50f, C), 1e-5f);
            Assert.AreEqual(1f, SonidoEspacial.Pan(50f, C), 1e-5f);
            Assert.AreEqual(0f, SonidoEspacial.Pan(0f, C), 1e-5f);
        }

        [Test] public void VolumenPlenoCercaYCeroLejos()
        {
            Assert.AreEqual(1f, SonidoEspacial.Volumen(0f, C), 1e-5f);
            Assert.AreEqual(1f, SonidoEspacial.Volumen(-4f, C), 1e-5f);
            Assert.AreEqual(0.5f, SonidoEspacial.Volumen(17f, C), 1e-5f);
            Assert.AreEqual(0f, SonidoEspacial.Volumen(-30f, C), 1e-5f);
            Assert.AreEqual(0f, SonidoEspacial.Volumen(99f, C), 1e-5f);
        }

        // --- Musica ---
        [Test] public void MismoTemaSigue()
        {
            var tema = new object();
            Assert.AreEqual(MusicaAccion.Seguir, MusicaRegla.Decidir(tema, tema));
        }

        [Test] public void SinTemaNuevoSigueElQueVenia()
        {
            Assert.AreEqual(MusicaAccion.Seguir, MusicaRegla.Decidir(new object(), null));
            Assert.AreEqual(MusicaAccion.Seguir, MusicaRegla.Decidir(null, null));
        }

        [Test] public void TemaDistintoCruza()
        {
            Assert.AreEqual(MusicaAccion.Cruzar, MusicaRegla.Decidir(new object(), new object()));
            Assert.AreEqual(MusicaAccion.Cruzar, MusicaRegla.Decidir(null, new object()));
        }

        // --- Zombie adelante ---
        [Test] public void ZombieAdelanteSuenaSoloDentroDelAviso()
        {
            Assert.IsTrue(SfxDirector.EnRangoDeAviso(1f, 12f));
            Assert.IsTrue(SfxDirector.EnRangoDeAviso(12f, 12f)); // borde inclusive
            Assert.IsFalse(SfxDirector.EnRangoDeAviso(12.01f, 12f)); // justo afuera
            Assert.IsFalse(SfxDirector.EnRangoDeAviso(0f, 12f)); // encima de la moto, no adelante
            Assert.IsFalse(SfxDirector.EnRangoDeAviso(-1f, 12f)); // atras de la moto
            Assert.IsFalse(SfxDirector.EnRangoDeAviso(20f, 12f)); // el rango viejo (30 m) ya no alcanza
        }

        // --- Motor ---
        [Test] public void TonoDelMotor()
        {
            Assert.AreEqual(0.8f, MotorTono.Objetivo(0f, 12f, false), 1e-5f);
            Assert.AreEqual(1.2f, MotorTono.Objetivo(12f, 12f, false), 1e-5f);
            Assert.AreEqual(1.0f, MotorTono.Objetivo(6f, 12f, false), 1e-5f);
            Assert.AreEqual(1.45f, MotorTono.Objetivo(12f, 12f, true), 1e-5f);
            Assert.AreEqual(0.8f, MotorTono.Objetivo(-6f, 12f, false), 1e-5f);
            Assert.AreEqual(1.2f, MotorTono.Objetivo(30f, 12f, false), 1e-5f);
        }
    }
}
