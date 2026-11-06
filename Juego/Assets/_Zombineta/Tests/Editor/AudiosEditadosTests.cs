using NUnit.Framework;
using UnityEditor;
using Zombineta.Audio;
using Zombineta.Enemies;

namespace Zombineta.Juego.Tests
{
    /// <summary>Los audios editados del 06/11: cada efecto con su clip y las reglas de los sonidos nuevos.</summary>
    public class AudiosEditadosTests
    {
        const string Ajustes = "Assets/_Zombineta/Settings/Audio/";

        static BancoDeSonidos Banco(string nombre) =>
            AssetDatabase.LoadAssetAtPath<BancoDeSonidos>(Ajustes + nombre + ".asset");

        [TestCase(SonidoClave.CambioCarril, "moto_carril", false)]
        [TestCase(SonidoClave.Choque, "moto_choque_01", false)]
        [TestCase(SonidoClave.Salto, "moto_salto", false)]
        [TestCase(SonidoClave.Aterrizaje, "moto_aterriza", false)]
        [TestCase(SonidoClave.Atropello, "zombie_runover", false)]
        [TestCase(SonidoClave.ApagonEmpieza, "world_blackout_start", false)]
        [TestCase(SonidoClave.ApagonTermina, "world_blackout_end", false)]
        [TestCase(SonidoClave.ImpactoDeBala, "zombie_hit_bullet", false)]
        [TestCase(SonidoClave.MotorCambio, "moto_cambio", false)]
        [TestCase(SonidoClave.InicioDeNivel, "level_start", false)]
        [TestCase(SonidoClave.Lluvia, "amb_rain", true)]
        [TestCase(SonidoClave.Viento, "amb_wind", true)]
        [TestCase(SonidoClave.FaroTitila, "moto_faro_titila", true)]
        public void CadaEfectoSuenaConSuClip(SonidoClave clave, string clip, bool loop)
        {
            var sonido = Banco("SonidosGlobales").Buscar(clave);
            Assert.IsNotNull(sonido, clave.ToString());
            Assert.AreEqual(1, sonido.clips.Length, clave.ToString());
            Assert.AreEqual(clip, sonido.clips[0].name);
            // AudioDirector.Play rechaza los loops: los maneja LoopsDeNivel.
            Assert.AreEqual(loop, sonido.loop, clave.ToString());
        }

        [Test]
        public void LasAlarmasSoloSuenanEnElNivel2()
        {
            Assert.IsNull(Banco("SonidosGlobales").Buscar(SonidoClave.AlarmaDeAuto), "el Nivel 1 no tiene embotellamiento");
            var alarma = Banco("BancoNivel02").Buscar(SonidoClave.AlarmaDeAuto);
            Assert.IsNotNull(alarma);
            Assert.AreEqual(2, alarma.clips.Length);
            Assert.IsTrue(alarma.posicional, "suena desde el auto");
            Assert.IsFalse(alarma.loop);
        }

        [TestCase("AmbienteNivel1", "amb_level1_city")]
        [TestCase("AmbienteNivel2", "autopista_tension")]
        public void CadaNivelTieneSuAmbiente(string sonido, string clip)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sonido>(Ajustes + "Sonidos/" + sonido + ".asset");
            Assert.IsNotNull(s.clips[0], sonido);
            Assert.AreEqual(clip, s.clips[0].name);
            Assert.IsTrue(s.loop);
        }

        [Test]
        public void ElAmbienteDelNivel2YaNoEsUnZumbido()
        {
            // El viejo duraba 0,39 s y en loop sonaba casi tres veces por segundo.
            var s = AssetDatabase.LoadAssetAtPath<Sonido>(Ajustes + "Sonidos/AmbienteNivel2.asset");
            Assert.Greater(s.clips[0].length, 2f);
        }

        [Test]
        public void ElSonidoDeNivelTieneLosLoops()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_Zombineta/Art/Audio/SonidoDelNivel.prefab");
            Assert.IsNotNull(prefab.GetComponentInChildren<LoopsDeNivel>(true));
        }

        // --- Corte y vuelta de la luz ---

        [Test]
        public void ElApagonSuenaUnaVezAlCortarseYUnaAlVolver()
        {
            var a = new CruceDeApagon();
            a.Reiniciar(0f);
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(0.1f));
            Assert.AreEqual(CambioDeApagon.Empieza, a.Avanzar(0.3f));
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(0.8f));
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(1f));
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(0.7f), "todavia esta oscuro");
            Assert.AreEqual(CambioDeApagon.Termina, a.Avanzar(0.55f));
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(0.2f));
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(0f));
        }

        [Test]
        public void MientrasLaLuzTerminaDeVolverNoSeCortaDeNuevo()
        {
            // La vuelta suena con la oscuridad todavia en 0,55: seguir bajando no es otro apagon.
            var a = new CruceDeApagon();
            a.Reiniciar(0f);
            a.Avanzar(1f);
            Assert.AreEqual(CambioDeApagon.Termina, a.Avanzar(0.55f));
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(0.5f));
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(0.3f));
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(0.1f));
            Assert.AreEqual(CambioDeApagon.Empieza, a.Avanzar(0.5f), "el apagon siguiente si suena");
        }

        [Test]
        public void SiArrancaAOscurasNoSuenaElCorte()
        {
            var a = new CruceDeApagon();
            a.Reiniciar(1f);
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(1f));
            Assert.AreEqual(CambioDeApagon.Termina, a.Avanzar(0f));
        }

        [Test]
        public void UnApagonAMediasTambienVuelve()
        {
            var a = new CruceDeApagon();
            a.Reiniciar(0f);
            Assert.AreEqual(CambioDeApagon.Empieza, a.Avanzar(0.4f));
            Assert.AreEqual(CambioDeApagon.Nada, a.Avanzar(0.35f));
            Assert.AreEqual(CambioDeApagon.Termina, a.Avanzar(0.2f));
        }

        [Test]
        public void PrenderLaLuzDeGolpeSuenaLaVuelta()
        {
            // Pelear en pleno apagon prende la luz: la oscuridad cae a 0 enseguida.
            var a = new CruceDeApagon();
            a.Reiniciar(0f);
            a.Avanzar(1f);
            Assert.AreEqual(CambioDeApagon.Termina, a.Avanzar(0f));
        }

        // --- Impacto de bala ---

        [Test]
        public void LaBalaQueMataSuena() =>
            Assert.IsTrue(SfxDirector.EsImpactoDeBala(
                new HordeEvent { Kind = HordeEventKind.Death, Cause = DeathCause.Bullet }, false));

        [Test]
        public void LaBalaQueAguantaSuenaTrasSuTrazador() =>
            Assert.IsTrue(SfxDirector.EsImpactoDeBala(new HordeEvent { Kind = HordeEventKind.Impact }, true));

        [Test]
        public void UnGolpeAPieNoEsUnaBala() =>
            Assert.IsFalse(SfxDirector.EsImpactoDeBala(new HordeEvent { Kind = HordeEventKind.Impact }, false));

        [TestCase(DeathCause.RunOver)]
        [TestCase(DeathCause.Explosion)]
        [TestCase(DeathCause.Golpe)]
        public void OtrasMuertesNoSuenanABala(DeathCause causa) =>
            Assert.IsFalse(SfxDirector.EsImpactoDeBala(
                new HordeEvent { Kind = HordeEventKind.Death, Cause = causa }, true));

        // --- Alarmas y faro ---

        [TestCase(0.1f, 10f, 5f, true)]
        [TestCase(0.9f, 10f, 5f, false)]
        [TestCase(0.1f, 4f, 5f, false)]
        public void LaAlarmaRespetaElSorteoYElDescanso(float azar, float ahora, float proxima, bool suena) =>
            Assert.AreEqual(suena, SfxDirector.SuenaAlarma(azar, 0.35f, ahora, proxima));

        [TestCase(true, true, 0.2f, true)]
        [TestCase(true, true, 0.5f, false)]
        [TestCase(true, true, 0f, false)]
        [TestCase(true, false, 0.2f, false)]
        [TestCase(false, true, 0.2f, false)]
        public void ElFaroChisporroteaConLaBateriaBaja(bool corriendo, bool prendido, float carga, bool suena) =>
            Assert.AreEqual(suena, LoopsDeNivel.FaroTitila(corriendo, prendido, carga, 0.25f));
    }
}
