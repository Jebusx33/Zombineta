using NUnit.Framework;
using UnityEditor;
using Zombineta.Scenery;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// El avance de cada tile se mide en pixeles del dibujo importado. Si la textura se importa mas chica
    /// (un tope de tamano, como el que tuvo el build de Android), el avance queda mas largo que el dibujo y
    /// la calle se llena de huecos oscuros.
    /// </summary>
    public class EscenarioSinHuecosTests
    {
        [TestCase("Assets/_Zombineta/Settings/Escenario.asset")]
        [TestCase("Assets/_Zombineta/Settings/Escenario_Nivel2.asset")]
        public void NingunTileAvanzaMasQueSuDibujo(string ruta)
        {
            var tileset = AssetDatabase.LoadAssetAtPath<SceneryTileset>(ruta);
            Assert.IsNotNull(tileset, ruta);
            Assert.AreEqual(0, EditorUserBuildSettings.overrideMaxTextureSize,
                "hay un tope de tamano de texturas en el editor: achica los dibujos y rompe las medidas en pixeles");

            foreach (var capa in tileset.layers)
                foreach (var v in capa.variants)
                {
                    if (v.prefab == null || v.avancePx <= 0f)
                        continue;
                    var sr = v.prefab.GetComponentInChildren<UnityEngine.SpriteRenderer>(true);
                    if (sr == null || sr.sprite == null)
                        continue;
                    Assert.LessOrEqual(v.avancePx, sr.sprite.rect.width + 0.5f,
                        capa.name + "/" + v.prefab.name + ": avanza " + v.avancePx + " px y el dibujo mide " + sr.sprite.rect.width);
                }
        }
    }
}
