using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Zombineta.Juego.EditorTools.Art
{
    // Recorta una hoja en una grilla pareja (columnas x filas), a diferencia de SheetSlicer (que
    // detecta islas de alfa): sirve cuando el tamano de celda ya divide justo, como las hojas
    // "layered" del player. El orden de los sprites es como se lee la hoja, de arriba a abajo y
    // de izquierda a derecha (fila 0 = la fila de mas arriba en la imagen), aunque el Rect que
    // usa Unity mide Y desde abajo.
    public static class GridSlicer
    {
        // Unity reduce en silencio una textura mas ancha que esto al importar (ver la guia de
        // iluminacion/arte, seccion 3.1): las hojas del player llegan a 3840 px de lado, asi que
        // el maxTextureSize por defecto (2048) las recortaria antes de que este metodo las mida.
        const int DefaultMaxTextureSize = 4096;

        /// <summary>
        /// Recorta 'path' en 'columns' x 'rows' celdas iguales y devuelve los sprites resultantes
        /// en orden de lectura (fila 0, columna 0..N; fila 1, columna 0..N; ...).
        /// </summary>
        public static List<Sprite> Slice(string path, int columns, int rows, Vector2 pivot,
                                          int maxTextureSize = DefaultMaxTextureSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("GridSlicer: no se encontro el TextureImporter de '" + path + "'.");
                return new List<Sprite>();
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            if (importer.maxTextureSize < maxTextureSize)
                importer.maxTextureSize = maxTextureSize;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            // Releer despues del reimport: el Texture2D anterior puede haber quedado invalido.
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogError("GridSlicer: no se pudo leer la textura en '" + path + "' despues de reimportarla.");
                return new List<Sprite>();
            }

            if (tex.width % columns != 0 || tex.height % rows != 0)
                Debug.LogWarning("GridSlicer: '" + path + "' mide " + tex.width + "x" + tex.height +
                    " y no divide justo en " + columns + "x" + rows + " (queda un resto de pixeles).");

            int cellW = tex.width / columns;
            int cellH = tex.height / rows;
            string hoja = Path.GetFileNameWithoutExtension(path);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(tex);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new List<SpriteRect>(columns * rows);
            var ordenNombres = new List<string>(columns * rows);
            for (int fila = 0; fila < rows; fila++)
            {
                // La fila 0 es la de arriba en la imagen; el Rect mide Y para arriba desde abajo.
                int y = tex.height - (fila + 1) * cellH;
                for (int columna = 0; columna < columns; columna++)
                {
                    string nombre = hoja + "_" + fila + "_" + columna;
                    ordenNombres.Add(nombre);
                    spriteRects.Add(new SpriteRect
                    {
                        name = nombre,
                        rect = new Rect(columna * cellW, y, cellW, cellH),
                        alignment = SpriteAlignment.Custom,
                        pivot = pivot,
                        spriteID = GUID.Generate(),
                    });
                }
            }

            dataProvider.SetSpriteRects(spriteRects.ToArray());

            var nameIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            var pairs = new List<SpriteNameFileIdPair>(spriteRects.Count);
            foreach (var r in spriteRects)
                pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            nameIdProvider.SetNameFileIdPairs(pairs);
            dataProvider.Apply();

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var porNombre = new Dictionary<string, Sprite>(spriteRects.Count);
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj is Sprite s)
                    porNombre[s.name] = s;

            var result = new List<Sprite>(ordenNombres.Count);
            foreach (var nombre in ordenNombres)
                if (porNombre.TryGetValue(nombre, out var s))
                    result.Add(s);

            Debug.Log("GridSlicer: '" + hoja + "' -> " + result.Count + " sprites (" + columns + "x" + rows + ").");
            return result;
        }
    }
}
