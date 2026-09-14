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
        // Valores verificados contra las dos hojas que ya se recortaron con esta herramienta
        // (zombies_poses.png: mergeGap 8 ya sobre-une filas completas; zombie_hombre.png: minArea
        // 200 recorta de mas una de las poses de muerte mas chicas). No sirven necesariamente para
        // una hoja nueva: si el conteo de islas no da lo esperado, ajustar y volver a probar.
        const int DefaultMinArea = 150;
        const int DefaultMergeGap = 6;
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

            Slice(tex, DefaultMinArea, DefaultMergeGap, false, out bool applied);
            if (applied)
                return;

            // Slice ya registro un warning con el detalle de cuantos sprites habia y cuantas islas
            // se detectaron ahora; aca solo se pide confirmacion para forzar la sobreescritura.
            bool overwrite = EditorUtility.DisplayDialog(
                "Recortar por transparencia",
                "'" + tex.name + "' ya tiene sprites recortados y la cantidad de islas detectada ahora es " +
                "distinta (ver la consola). Sobreescribir de todos modos?",
                "Sobreescribir",
                "Cancelar");
            if (overwrite)
                Slice(tex, DefaultMinArea, DefaultMergeGap, true);
        }

        [MenuItem("Assets/Zombineta/Recortar por transparencia", true)]
        static bool ValidateMenuSlice() => Selection.activeObject is Texture2D;

        // Recorta 'tex' y escribe un SpriteRect por isla en su importer. Publico para poder ajustar
        // minArea/mergeGap por hoja (por ejemplo desde una herramienta o un RunCommand) sin pasar
        // por el menu, que usa los valores por defecto.
        //
        // Si la textura ya tenia sprites recortados y la cantidad de islas detectada ahora es
        // distinta, no sobreescribe (para no perder un recorte bueno por una corrida con
        // parametros mal ajustados) salvo que overwriteOnCountChange sea true.
        public static List<PixelRect> Slice(Texture2D tex, int minArea, int mergeGap, bool overwriteOnCountChange = false)
        {
            return Slice(tex, minArea, mergeGap, overwriteOnCountChange, out _);
        }

        public static List<PixelRect> Slice(Texture2D tex, int minArea, int mergeGap, bool overwriteOnCountChange, out bool applied)
        {
            applied = false;

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
            if (tex == null)
            {
                Debug.LogError("SheetSlicer: no se pudo releer la textura en '" + path + "' despues de reimportarla.");
                importer.isReadable = wasReadable;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                return new List<PixelRect>();
            }

            string hoja = Path.GetFileNameWithoutExtension(path);
            var rows = FindRows(tex, minArea, mergeGap);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(tex);
            dataProvider.InitSpriteEditorDataProvider();

            var existingRects = dataProvider.GetSpriteRects();
            int existingCount = existingRects != null ? existingRects.Length : 0;
            int newCount = 0;
            foreach (var row in rows)
                newCount += row.Count;

            if (existingCount > 0 && existingCount != newCount && !overwriteOnCountChange)
            {
                Debug.LogWarning("SheetSlicer: '" + hoja + "' ya tenia " + existingCount + " sprites recortados y ahora se " +
                    "detectaron " + newCount + " islas (minArea=" + minArea + ", mergeGap=" + mergeGap + "). No se " +
                    "sobreescribe: llamar con overwriteOnCountChange:true (o confirmar en el dialogo del menu) para forzarlo.");
                importer.isReadable = wasReadable;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                return Flatten(rows);
            }

            var spriteRects = new List<SpriteRect>(newCount);
            var counts = new List<int>(rows.Count);
            for (int fila = 0; fila < rows.Count; fila++)
            {
                var row = rows[fila];
                counts.Add(row.Count);
                for (int columna = 0; columna < row.Count; columna++)
                {
                    var box = row[columna];
                    spriteRects.Add(new SpriteRect
                    {
                        name = hoja + "_" + fila + "_" + columna,
                        rect = new Rect(box.x, box.y, box.width, box.height),
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0f),
                        spriteID = GUID.Generate(),
                    });
                }
            }

            // TextureImporter.spritesheet ya no tiene efecto (removido en esta version): escribir
            // los SpriteRect por el ISpriteEditorDataProvider, que es lo que usa el propio
            // Sprite Editor.
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

            applied = true;
            return Flatten(rows);
        }

        // AlphaIslands.FindRows es la unica fuente de la agrupacion en filas: SheetSlicer no vuelve
        // a derivar donde empieza cada fila (antes lo hacia con su propio VerticalGap, con un
        // criterio que podia no coincidir con el de AlphaIslands.SortIntoRows).
        static List<List<PixelRect>> FindRows(Texture2D tex, int minArea, int mergeGap)
        {
            int width = tex.width;
            int height = tex.height;
            var pixels = tex.GetPixels32();
            var opaque = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                opaque[i] = pixels[i].a > AlphaThreshold;
            return AlphaIslands.FindRows(opaque, width, height, minArea, mergeGap);
        }

        static List<PixelRect> Flatten(List<List<PixelRect>> rows)
        {
            var result = new List<PixelRect>();
            foreach (var row in rows)
                result.AddRange(row);
            return result;
        }
    }
}
