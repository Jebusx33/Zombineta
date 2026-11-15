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
        [TestCase(SonidoClave.Victoria, "level_complete", false)]
        [TestCase(SonidoClave.PickupMunicion, "pickup_ammo", false)]
        [TestCase(SonidoClave.PickupGranada, "pickup_bomb", false)]
        [TestCase(SonidoClave.PickupBomba, "pickup_bomb", false)]
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
        public void CadaNivelTieneSuAmbiente(string sonido, string clip)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sonido>(Ajustes + "Sonidos/" + sonido + ".asset");
            Assert.IsNotNull(s.clips[0], sonido);
            Assert.AreEqual(clip, s.clips[0].name);
            Assert.IsTrue(s.loop);
        }

        [Test]
        public void ElNivel2NoTieneAmbiente()
        {
            // Desde el 10/11 el Nivel 2 suena con su tema (el pulso de la autopista), la tension y el viento:
            // el ambiente aparte (autopista_tension de 2,1 s) se saco.
            var nivel = AssetDatabase.LoadAssetAtPath<AudioDeNivel>(Ajustes + "Nivel02.asset");
            Assert.IsNull(nivel.ambiente);
        }

        [Test]
        public void LaMusicaDeLaAutopistaEsLaTensionCorta()
        {
            var nivel = AssetDatabase.LoadAssetAtPath<AudioDeNivel>(Ajustes + "Nivel02.asset");
            Assert.AreEqual("mus_level2_loop", nivel.musica.name);
            // autopista_tension: 0,39 s en loop. Sin streaming, para que el loop no corte en cada vuelta.
            Assert.Less(nivel.musica.length, 0.5f);
            Assert.AreEqual(UnityEngine.AudioClipLoadType.DecompressOnLoad, nivel.musica.loadType);
            // La capa de tension dura otra cosa: va como loop propio, sin aviso de desfasaje.
            Assert.IsTrue(nivel.tensionLibre);
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

        // --- Jefe ---

        [Test]
        public void LasOrugasAceleranParaLaEmbestida()
        {
            var normal = OrugasDelJefe.Ritmo(Zombineta.Jefe.FaseDeJefe.Persecucion, 0f, 1.2f);
            var aviso = OrugasDelJefe.Ritmo(Zombineta.Jefe.FaseDeJefe.AvisoEmbestida, 0.6f, 1.2f);
            var embiste = OrugasDelJefe.Ritmo(Zombineta.Jefe.FaseDeJefe.Embestida, 0.1f, 1.2f);
            Assert.Greater(aviso.x, normal.x, "en el aviso ya sube el motor");
            Assert.Greater(embiste.x, aviso.x, "embistiendo va a fondo");
            Assert.AreEqual(1f, embiste.y, 1e-6f);
            Assert.Less(OrugasDelJefe.Ritmo(Zombineta.Jefe.FaseDeJefe.Aturdido, 0f, 1.2f).x, normal.x, "aturdido casi se para");
            Assert.AreEqual(0f, OrugasDelJefe.Ritmo(Zombineta.Jefe.FaseDeJefe.Derrotado, 0f, 1.2f).y, "derrotado se calla");
            Assert.AreEqual(0f, OrugasDelJefe.Ritmo(Zombineta.Jefe.FaseDeJefe.Inactivo, 0f, 1.2f).y, "sin duelo no suena");
        }

        [Test]
        public void AntesDeEmbestirAceleraEnElLugar()
        {
            const float aviso = 1.2f;
            var F = Zombineta.Jefe.FaseDeJefe.AvisoEmbestida;
            Assert.AreEqual(0f, OrugasDelJefe.Rugido(Zombineta.Jefe.FaseDeJefe.Persecucion, 0.5f, aviso), "fuera del aviso, nada");
            // Tres tirones: el pico de cada uno es mas fuerte que el anterior.
            float t1 = OrugasDelJefe.Rugido(F, aviso / 6f, aviso), t2 = OrugasDelJefe.Rugido(F, aviso / 2f, aviso), t3 = OrugasDelJefe.Rugido(F, aviso * 5f / 6f, aviso);
            Assert.Greater(t2, t1);
            Assert.Greater(t3, t2);
            Assert.AreEqual(1f, OrugasDelJefe.Ritmo(F, aviso, aviso).y, 1e-6f, "termina el aviso a volumen pleno");
            Assert.Greater(OrugasDelJefe.Ritmo(Zombineta.Jefe.FaseDeJefe.Embestida, 0f, aviso).x,
                           OrugasDelJefe.Ritmo(F, aviso, aviso).x + OrugasDelJefe.Rugido(F, aviso * 5f / 6f, aviso) - 0.01f, "embistiendo, mas agudo que el ultimo tiron");
        }

        [Test]
        public void LaCapaDelJefeSubeAntesDeQueAparezcaYSeVaAlDerrotarlo()
        {
            Assert.AreEqual(0f, CapaDelJefe.Intensidad(true, false, false, 3000f, 3500f, 200f), "lejos: nada");
            Assert.AreEqual(0.5f, CapaDelJefe.Intensidad(true, false, false, 3400f, 3500f, 200f), 1e-5f, "a mitad del tramo previo");
            Assert.AreEqual(1f, CapaDelJefe.Intensidad(true, true, false, 3600f, 3500f, 200f), "en el duelo, llena");
            Assert.AreEqual(0f, CapaDelJefe.Intensidad(true, true, true, 3700f, 3500f, 200f), "derrotado, se va");
            Assert.AreEqual(0f, CapaDelJefe.Intensidad(false, false, false, 3500f, 3500f, 200f), "sin jefe en el nivel, nunca");
        }

        [Test]
        public void ElTemaDelJefeSuenaEnteroApenasSeAnuncia()
        {
            Assert.AreEqual(0f, MusicDirector.VolumenDeLaCapaDelJefe(0f, 0.8f), "sin jefe, callado");
            Assert.AreEqual(0.8f, MusicDirector.VolumenDeLaCapaDelJefe(0.05f, 0.8f), "apenas se anuncia, a su volumen: arranca con un golpe");
            Assert.AreEqual(0.8f, MusicDirector.VolumenDeLaCapaDelJefe(1f, 0.8f));
        }

        [Test]
        public void ElNivel2TieneTemaDelJefeQueArrancaUnPocoAntes()
        {
            var n2 = AssetDatabase.LoadAssetAtPath<AudioDeNivel>(Ajustes + "Nivel02.asset");
            Assert.IsNotNull(n2.jefe);
            Assert.AreEqual("boss_music", n2.jefe.name);
            // Un par de segundos antes de verlo: a la velocidad normal (12 m/s) no mas de 2,5 s antes de que entre,
            // y despues el jefe tarda en llegar desde atras.
            Assert.LessOrEqual(n2.metrosAntesDelJefe, 30f);
            Assert.Greater(n2.volumenJefe, n2.volumenMusicaConJefe, "por encima del tema del nivel, que baja");
        }

        [Test]
        public void ElTemaDelNivel2BajaCuandoLlegaElJefe()
        {
            // El pulso de la autopista viene a -6,7 LUFS (el tema del Nivel 1, a -26,7): con el jefe tapaba sus sonidos.
            var n2 = AssetDatabase.LoadAssetAtPath<AudioDeNivel>(Ajustes + "Nivel02.asset");
            Assert.Less(n2.volumenMusicaConJefe, n2.volumenMusica, "con el jefe, mas bajo");
            Assert.LessOrEqual(n2.volumenMusicaConJefe, 0.3f);
            Assert.AreEqual(n2.volumenMusica, MusicDirector.VolumenDelTema(n2.volumenMusica, n2.volumenMusicaConJefe, 0f), 1e-6f);
            Assert.AreEqual(n2.volumenMusicaConJefe, MusicDirector.VolumenDelTema(n2.volumenMusica, n2.volumenMusicaConJefe, 1f), 1e-6f);
            Assert.IsFalse(AssetDatabase.LoadAssetAtPath<Sonido>(Ajustes + "Sonidos/JefeDispara.asset").posicional, "el disparo del jefe, al centro");
        }

        [Test]
        public void ElJefeJadeaSoloMientrasEstaAturdido()
        {
            Assert.IsTrue(OrugasDelJefe.Jadea(Zombineta.Jefe.FaseDeJefe.Aturdido, Zombineta.Core.RunPhase.Running));
            Assert.IsFalse(OrugasDelJefe.Jadea(Zombineta.Jefe.FaseDeJefe.Persecucion, Zombineta.Core.RunPhase.Running));
            Assert.IsFalse(OrugasDelJefe.Jadea(Zombineta.Jefe.FaseDeJefe.Aturdido, Zombineta.Core.RunPhase.Lost), "perdida la partida, se calla");
        }

        [Test]
        public void ElJefeTieneSusSonidosEnElBancoGlobal()
        {
            var banco = UnityEditor.AssetDatabase.LoadAssetAtPath<BancoDeSonidos>("Assets/_Zombineta/Settings/Audio/SonidosGlobales.asset");
            foreach (var c in new[] { SonidoClave.JefeApunta, SonidoClave.JefeDispara, SonidoClave.JefeImpacto, SonidoClave.JefeHerido,
                                      SonidoClave.JefeRebota, SonidoClave.JefeDerrotado, SonidoClave.JefeOrugas, SonidoClave.JefeCansado })
            {
                var s = banco.Buscar(c);
                Assert.IsNotNull(s, c.ToString());
                Assert.IsNotNull(s.clips[0], c.ToString());
                StringAssert.StartsWith("boss_", s.clips[0].name, c.ToString());
            }
            Assert.IsTrue(banco.Buscar(SonidoClave.JefeOrugas).loop, "las orugas son un loop");
        }

        [Test]
        public void ElNivel2UsaLaTensionDelNivel1()
        {
            var n1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioDeNivel>("Assets/_Zombineta/Settings/Audio/Nivel01.asset");
            var n2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioDeNivel>("Assets/_Zombineta/Settings/Audio/Nivel02.asset");
            Assert.AreSame(n1.tension, n2.tension);
            Assert.AreEqual(n1.volumenTension, n2.volumenTension, 1e-6f);
            Assert.IsTrue(n2.tensionLibre, "dura distinto que el pulso del Nivel 2: va libre");
        }

        // --- Paneo ---

        [Test]
        public void ElPaneoSeMideEnMetrosDesdeLaMoto()
        {
            // Con 0,75 unidades por metro, un auto 7,5 unidades adelante esta a 10 m.
            Assert.AreEqual(10f, AudioDirector.Espaciado(17.5f, 10f, 0.75f), 1e-4f);
            Assert.AreEqual(-4f, AudioDirector.Espaciado(7f, 10f, 0.75f), 1e-4f, "ya pasado: atras, a la izquierda");
        }

        // --- Lluvia ---

        [Test]
        public void ConMediaLluviaSeOyeMasFuerteYConLaPlenaIgual()
        {
            Assert.AreEqual(0.7071f, LoopsDeNivel.VolumenDeLoop(1f, 0.5f, 0.5f), 1e-3f);
            Assert.AreEqual(0.5f, LoopsDeNivel.VolumenDeLoop(1f, 0.5f, 1f), 1e-6f, "curva 1: en proporcion");
            Assert.AreEqual(0.8f, LoopsDeNivel.VolumenDeLoop(0.8f, 1f, 0.5f), 1e-6f, "lluvia plena: el volumen del Sonido");
            Assert.AreEqual(0f, LoopsDeNivel.VolumenDeLoop(1f, 0f, 0.5f), 1e-6f, "sin lluvia no suena");
        }
    }
}
