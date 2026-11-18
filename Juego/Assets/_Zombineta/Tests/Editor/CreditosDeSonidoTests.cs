using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zombineta.Juego.Screens;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// Los efectos de sonido de Pixabay se reconocen en los creditos: su logo y cada autor, en
    /// columnas para que la lista no se haga eterna.
    /// </summary>
    public class CreditosDeSonidoTests
    {
        const string RutaCreditos = "Assets/_Zombineta/Settings/Creditos.asset";
        const string RutaLogo = "Assets/_Zombineta/Art/Creditos/pixabay_logo.png";

        static Seccion Sonidos()
        {
            var creditos = AssetDatabase.LoadAssetAtPath<Creditos>(RutaCreditos);
            return creditos.secciones.Find(s => s.titulo == "Efectos de sonido");
        }

        [Test]
        public void LosCreditosNombranAPixabayYASusAutores()
        {
            var seccion = Sonidos();
            Assert.IsNotNull(seccion, "falta la seccion de efectos de sonido");
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Sprite>(RutaLogo), seccion.imagen);
            Assert.AreEqual(24, seccion.lineas.Length);
            Assert.AreEqual(seccion.lineas.Length, seccion.lineas.Distinct().Count(), "autor repetido");
            CollectionAssert.Contains(seccion.lineas, "freesound_community");
            Assert.AreEqual(3, seccion.columnas);
        }

        [Test]
        public void LosAutoresSeArmanEnTresColumnasDeOcho()
        {
            var seccion = Sonidos();
            var datos = ScriptableObject.CreateInstance<Creditos>();
            datos.secciones.Add(seccion);

            var go = new GameObject("Creditos de prueba", typeof(RectTransform));
            try
            {
                var contenedor = new GameObject("Contenedor", typeof(RectTransform)).GetComponent<RectTransform>();
                contenedor.SetParent(go.transform, false);
                var pantalla = go.AddComponent<CreditsScreen>();
                var campos = new SerializedObject(pantalla);
                campos.FindProperty("datos").objectReferenceValue = datos;
                campos.FindProperty("contenedor").objectReferenceValue = contenedor;
                campos.ApplyModifiedPropertiesWithoutUndo();

                typeof(CreditsScreen).GetMethod("ArmarLineas",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(pantalla, null);

                var nombres = contenedor.GetComponentsInChildren<Text>()
                    .Where(t => seccion.lineas.Contains(t.text)).ToArray();
                Assert.AreEqual(24, nombres.Length);
                Assert.AreEqual(3, nombres.Select(t => t.rectTransform.anchoredPosition.x).Distinct().Count());
                Assert.AreEqual(8, nombres.Select(t => t.rectTransform.anchoredPosition.y).Distinct().Count());

                var logo = contenedor.GetComponentsInChildren<Image>().Single(i => i.sprite == seccion.imagen);
                Assert.Greater(logo.rectTransform.anchoredPosition.y, nombres.Max(t => t.rectTransform.anchoredPosition.y),
                    "el logo va arriba de los nombres");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(datos);
            }
        }
    }
}
