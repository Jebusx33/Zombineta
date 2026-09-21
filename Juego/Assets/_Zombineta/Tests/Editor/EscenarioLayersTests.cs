using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Scenery;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// Mapeo de capas (item 11a del arreglo final): cada capa de Escenario.asset tiene que apuntar
    /// a una Sorting Layer que exista de verdad entre las del juego, y LaneSorting.GameLayer tiene
    /// que ser una de ellas. Ambas cosas se verifican contra el proyecto real (SortingLayer.layers),
    /// no contra una lista fija, para que la prueba falle si alguna vez se borra o se renombra una
    /// capa en Tags and Layers.
    /// </summary>
    public class EscenarioLayersTests
    {
        const string EscenarioPath = "Assets/_Zombineta/Settings/Escenario.asset";

        static HashSet<string> CapasDelJuego()
        {
            var nombres = new HashSet<string>();
            foreach (var capa in SortingLayer.layers)
                if (capa.name != "Default")
                    nombres.Add(capa.name);
            return nombres;
        }

        [Test]
        public void GameLayerExistsAmongTheLayersOfTheGame()
        {
            CollectionAssert.Contains(CapasDelJuego(), LaneSorting.GameLayer);
        }

        [Test]
        public void EveryEscenarioLayerPointsToAnExistingSortingLayer()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<SceneryTileset>(EscenarioPath);
            Assert.IsNotNull(tileset, "No se encontro el SceneryTileset en " + EscenarioPath);

            var capasDelJuego = CapasDelJuego();
            Assert.Greater(capasDelJuego.Count, 0, "El proyecto no tiene Sorting Layers propias (fuera de Default).");

            foreach (var capa in tileset.layers)
            {
                Assert.IsNotNull(capa, "Escenario.asset tiene una entrada de capa nula.");
                CollectionAssert.Contains(capasDelJuego, capa.sortingLayer,
                    "La capa '" + capa.name + "' de Escenario.asset apunta a un sortingLayer ('" +
                    capa.sortingLayer + "') que no existe entre las Sorting Layers del juego.");
            }
        }
    }
}
