using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Zombineta.Art;

namespace Zombineta.Juego.EditorTools.Art
{
    // Recorta una hoja de sprites sin grilla exacta en un sprite por figura, detectando las islas
    // de pixeles opacos (ver Zombineta.Art.AlphaIslands). Pensado para hojas donde las figuras no
    // caen en celdas parejas: una pose puede tener un brazo o un chorro de sangre como isla
    // separada (se une con mergeGap) y otra puede ser mas baja/ancha que sus vecinas de fila (una
    // pose tirada en el piso).
    public static class SheetSlicer
    {
        const int DefaultMinArea = 200;
        const int DefaultMergeGap = 12;
        const byte AlphaThreshold = 8; // descarta el antialiasing casi transparente del borde

        [MenuItem("Assets/Zombineta/Recortar por transparencia")]
        static void MenuSlice()
        {
            var tex = Selection.activeObject as Texture2D;
            if (tex == null)
            {
                Debug.LogWarning("SheetSlicer: selecciona una textura (Sprite) para recortar.");
                return;
            }
            Slice(tex, DefaultMinArea, DefaultMergeGap);
        }

        [MenuItem("Assets/Zombineta/Recortar por transparencia", true)]
        static bool ValidateMenuSlice() => Selection.activeObject is Texture2D;

        // Recorta 'tex' y escribe un SpriteRect por isla en su importer. Publico para poder ajustar
        // minArea/mergeGap por hoja (por ejemplo desde una herramienta o un RunCommand) sin pasar
        // por el menu, que usa los valores por defecto.
        public static List<PixelRect> Slice(Texture2D tex, int minArea, int mergeGap)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("SheetSlicer: no se encontro el TextureImporter de '" + path + "'.");
                return new List<PixelRect>();
            }

            bool wasReadable = importer.isReadable;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            // Releer la textura: tras el reimport isReadable=true el objeto anterior puede haber
            // quedado invalido.
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            var islands = FindIslands(tex, minArea, mergeGap);
            string hoja = Path.GetFileNameWithoutExtension(path);

            var spriteRects = new List<SpriteRect>(islands.Count);
            var counts = new List<int>();
            int fila = 0, columna = 0;
            float tolerance = AlphaIslands.RowOverlapTolerancePx;
            int rowMinY = 0, rowMaxY = 0;

            for (int i = 0; i < islands.Count; i++)
            {
                var box = islands[i];
                if (i == 0)
                {
                    rowMinY = box.y;
                    rowMaxY = box.y + box.height;
                }
                else if (VerticalGap(rowMinY, rowMaxY, box.y, box.y + box.height) >= tolerance)
                {
                    counts.Add(columna);
                    fila++;
                    columna = 0;
                    rowMinY = box.y;
                    rowMaxY = box.y + box.height;
                }
                else
                {
                    rowMinY = Mathf.Min(rowMinY, box.y);
                    rowMaxY = Mathf.Max(rowMaxY, box.y + box.height);
                }

                spriteRects.Add(new SpriteRect
                {
                    name = hoja + "_" + fila + "_" + columna,
                    rect = new Rect(box.x, box.y, box.width, box.height),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = GUID.Generate(),
                });
                columna++;
            }
            counts.Add(columna);

            // TextureImporter.spritesheet ya no tiene efecto (removido en esta version): escribir
            // los SpriteRect por el ISpriteEditorDataProvider, que es lo que usa el propio
            // Sprite Editor.
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(tex);
            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(spriteRects.ToArray());

            var nameIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            var pairs = new List<SpriteNameFileIdPair>(spriteRects.Count);
            foreach (var r in spriteRects)
                pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            nameIdProvider.SetNameFileIdPairs(pairs);

            dataProvider.Apply();

            importer.isReadable = wasReadable;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var report = new StringBuilder();
            report.Append("SheetSlicer: '" + hoja + "' -> " + spriteRects.Count + " sprites en " + counts.Count + " filas (");
            for (int i = 0; i < counts.Count; i++)
            {
                if (i > 0) report.Append(", ");
                report.Append("fila " + i + ": " + counts[i]);
            }
            report.Append(").");
            Debug.Log(report.ToString());

            return islands;
        }

        static List<PixelRect> FindIslands(Texture2D tex, int minArea, int mergeGap)
        {
            int width = tex.width;
            int height = tex.height;
            var pixels = tex.GetPixels32();
            var opaque = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                opaque[i] = pixels[i].a > AlphaThreshold;
            return AlphaIslands.Find(opaque, width, height, minArea, mergeGap);
        }

        static int VerticalGap(int aMin, int aMax, int bMin, int bMax)
        {
            return Mathf.Max(0, Mathf.Max(aMin - bMax, bMin - aMax));
        }
    }
}
