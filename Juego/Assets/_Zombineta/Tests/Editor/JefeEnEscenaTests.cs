using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombineta.Jefe;
using Zombineta.Juego.Levels;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// El jefe esta en el nivel 2 y solo ahi, y sus dibujos acompanan a la logica: si los tiempos del
    /// asset y los de la animacion se separan, el proyectil sale sin que el brazo haya disparado.
    /// </summary>
    public class JefeEnEscenaTests
    {
        const string Animaciones = "Assets/_Zombineta/Settings/AnimacionesJefeNivel2.asset";
        const string Config = "Assets/_Zombineta/Settings/JefeNivel2.asset";

        static T Cargar<T>(string ruta) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
            Assert.IsNotNull(asset, ruta);
            return asset;
        }

        static T Buscar<T>(Scene escena) where T : Component
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                var found = raiz.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            return null;
        }

        static void ConEscena(string nombre, System.Action<Scene> verificar)
        {
            string ruta = "Assets/_Zombineta/Scenes/" + nombre + ".unity";
            var yaCargada = SceneManager.GetSceneByPath(ruta);
            bool estaba = yaCargada.isLoaded;
            var escena = estaba ? yaCargada : EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
            try
            {
                verificar(escena);
            }
            finally
            {
                if (!estaba)
                    EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void ElNivel2TraeAlJefeArmado()
        {
            ConEscena("Level_02", escena =>
            {
                var jefe = new SerializedObject(Buscar<LevelScene>(escena)).FindProperty("jefe").objectReferenceValue;
                Assert.AreEqual(Cargar<JefeConfig>(Config), jefe);

                var vista = Buscar<VistaDelJefe>(escena);
                Assert.IsNotNull(vista, "falta lo que se ve del jefe");
                var campos = new SerializedObject(vista);
                foreach (var campo in new[] { "run", "animaciones", "material", "cajaDeBalas", "granadaSuelta" })
                    Assert.IsNotNull(campos.FindProperty(campo).objectReferenceValue, campo);

                var hud = Buscar<HudJefe>(escena);
                Assert.IsNotNull(hud, "falta el HUD del duelo");
                Assert.IsNotNull(new SerializedObject(hud).FindProperty("run").objectReferenceValue);
            });
        }

        // Si no, el tanque se puede vaciar antes de llegar y la pelea a pie reaparece delante del duelo.
        [Test]
        public void LosBidonesLleganHastaElJefe()
        {
            ConEscena("Level_02", escena =>
            {
                float corte = new SerializedObject(Buscar<LevelScene>(escena)).FindProperty("bidonesHastaMetro").floatValue;
                Assert.AreEqual(Cargar<JefeConfig>(Config).metroEntrada, corte);
            });
        }

        [Test]
        public void ElNivel1NoTieneJefe()
        {
            ConEscena("Level_01", escena =>
            {
                Assert.IsNull(new SerializedObject(Buscar<LevelScene>(escena)).FindProperty("jefe").objectReferenceValue);
                Assert.IsNull(Buscar<VistaDelJefe>(escena));
            });
        }

        [Test]
        public void LosTiemposDeLaLogicaSonLosDeLasAnimaciones()
        {
            var a = Cargar<AnimacionesJefe>(Animaciones);
            var c = Cargar<JefeConfig>(Config);

            Assert.AreEqual(a.anticipacion.Duracion, c.anticipacion, 0.001f);
            Assert.AreEqual(a.ataqueFrontal.Duracion, c.disparoFrontal, 0.001f);
            Assert.AreEqual(a.ataqueElevado.Duracion, c.disparoElevado, 0.001f);
            Assert.AreEqual(a.ataqueElevado.segundos[0], c.emisionElevado, 0.001f, "emite al entrar al cuadro 02");
            Assert.AreEqual(a.embestida.segundos[1] + a.embestida.segundos[2], c.avanceEmbestida, 0.001f);
            Assert.AreEqual(a.dano.Duracion, c.dano, 0.001f);
            Assert.AreEqual(a.proyectilImpacto.Duracion, c.impacto, 0.001f);
            Assert.That(a.muerte.Duracion, Is.LessThan(c.caidaEnPantalla), "la caida se tiene que ver entera");
        }

        [Test]
        public void CadaClipTieneSusCuadrosCompletos()
        {
            var a = Cargar<AnimacionesJefe>(Animaciones);
            foreach (var clip in new[]
            {
                a.quieto, a.anticipacion, a.ataqueFrontal, a.ataqueElevado, a.embestida, a.aturdido,
                a.recuperacion, a.dano, a.muerte, a.proyectilVuelo, a.proyectilImpacto,
            })
            {
                Assert.AreEqual(clip.cuadros.Length, clip.segundos.Length);
                Assert.AreEqual(clip.cuadros.Length, clip.escala.Length);
                foreach (var cuadro in clip.cuadros)
                    Assert.IsNotNull(cuadro);
            }
        }

        [Test]
        public void ElAturdidoRepiteLosCuadrosDelPaquete()
        {
            var clip = Cargar<AnimacionesJefe>(Animaciones).aturdido;
            Assert.IsTrue(clip.enBucle);
            Assert.AreEqual("jefe_n2_aturdido_04", clip.cuadros[2].name);
            Assert.AreEqual(clip.cuadros[1], clip.cuadros[3]);
        }

        [Test]
        public void ClipSinBucleSeQuedaEnElUltimoCuadro()
        {
            var clip = Cargar<AnimacionesJefe>(Animaciones).muerte;
            Assert.AreEqual(0, clip.Indice(0f));
            Assert.AreEqual(3, clip.Indice(50f));
        }
    }
}
