using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Zombineta.Juego.EditorTools;
using Zombineta.Luz;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// Lo que se ajusto para que la X55 (1 GB de RAM) no se quede sin memoria ni se trabe. Todo es solo de
    /// Android: PC queda como estaba.
    /// </summary>
    public class RendimientoAndroidTests
    {
        const string Hoja = "Assets/_Zombineta/Art/Zombies/ZombieCorredor_caminar.png";

        [TearDown]
        public void Limpiar()
        {
            SpriteApoyo.ForzarAsincronico = null;
            Reiniciar();
        }

        static void Reiniciar() =>
            typeof(SpriteApoyo).GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

        [Test]
        public void AndroidComprimeConAstc()
        {
            Assert.AreEqual(TextureCompressionFormat.ASTC, PlayerSettings.Android.textureCompressionFormats[0],
                "con ETC2 las texturas con lados que no son multiplo de 4 quedan crudas en Android");
        }

        [Test]
        public void NingunaTexturaGrandeQuedaCrudaEnAndroid()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Zombineta/Art" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(ruta) is TextureImporter imp)
                    Assert.IsFalse(BuildAndroid.DebeComprimirseEnAndroid(imp),
                        ruta + " queda sin comprimir en Android: correr BuildAndroid.ComprimirLoQueQuedaCrudo");
            }
        }

        [Test]
        public void LaLecturaSinEsperarDaLosMismosPiesQueLaInmediata()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Hoja);
            Assert.IsNotNull(sprite, Hoja);

            SpriteApoyo.ForzarAsincronico = false;
            Reiniciar();
            var inmediata = SpriteApoyo.De(sprite);

            SpriteApoyo.ForzarAsincronico = true;
            Reiniciar();
            SpriteApoyo.De(sprite); // pide la lectura y mientras tanto usa la malla
            AsyncGPUReadback.WaitAllRequests();
            var sinEsperar = SpriteApoyo.De(sprite);

            Assert.AreEqual(inmediata.x, sinEsperar.x, 1e-4f);
            Assert.AreEqual(inmediata.y, sinEsperar.y, 1e-4f);
        }
    }
}
