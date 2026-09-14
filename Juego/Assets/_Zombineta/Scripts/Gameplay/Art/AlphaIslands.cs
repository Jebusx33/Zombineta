using System;
using System.Collections.Generic;

namespace Zombineta.Art
{
    // Caja en coordenadas de textura de Unity (x, y = esquina inferior izquierda; y = 0 abajo).
    public struct PixelRect
    {
        public int x;
        public int y;
        public int width;
        public int height;

        public PixelRect(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }

    // Recorta una hoja de sprites sin grilla exacta en islas de pixeles opacos: cada componente
    // conectado (8 vecinos) es una figura. Las cajas separadas por menos de mergeGap se unen en una
    // sola (una mano o un chorro de sangre sueltos del resto del cuerpo), las de area menor a
    // minArea se descartan (motitas/ruido) y el resultado queda ordenado por fila (de arriba hacia
    // abajo) y, dentro de cada fila, de izquierda a derecha.
    public static class AlphaIslands
    {
        public static List<PixelRect> Find(bool[] opaque, int width, int height, int minArea, int mergeGap)
        {
            var rows = FindRows(opaque, width, height, minArea, mergeGap);
            var result = new List<PixelRect>();
            foreach (var row in rows)
                result.AddRange(row);
            return result;
        }

        // Igual que Find, pero sin aplanar: cada elemento es una fila completa (de arriba hacia
        // abajo), y cada fila ya viene ordenada de izquierda a derecha. SheetSlicer la usa para
        // nombrar <hoja>_<fila>_<columna> sin tener que re-derivar donde empieza cada fila (Find
        // no expone esos cortes).
        public static List<List<PixelRect>> FindRows(bool[] opaque, int width, int height, int minArea, int mergeGap)
        {
            var boxes = FindComponents(opaque, width, height);
            boxes = MergeClose(boxes, mergeGap);
            boxes.RemoveAll(b => (long)b.width * b.height < minArea);
            return GroupIntoRows(boxes);
        }

        // --- Componentes conectados (BFS, 8 vecinos) -----------------------------------

        static List<PixelRect> FindComponents(bool[] opaque, int width, int height)
        {
            var visited = new bool[opaque.Length];
            var boxes = new List<PixelRect>();
            var queue = new Queue<int>();

            for (int start = 0; start < opaque.Length; start++)
            {
                if (!opaque[start] || visited[start])
                    continue;

                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                queue.Clear();
                queue.Enqueue(start);
                visited[start] = true;

                while (queue.Count > 0)
                {
                    int idx = queue.Dequeue();
                    int x = idx % width;
                    int y = idx / width;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;

                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;
                        int nidx = ny * width + nx;
                        if (visited[nidx] || !opaque[nidx])
                            continue;
                        visited[nidx] = true;
                        queue.Enqueue(nidx);
                    }
                }

                boxes.Add(new PixelRect(minX, minY, maxX - minX + 1, maxY - minY + 1));
            }

            return boxes;
        }

        // --- Union de cajas cercanas (brazo/chorro suelto) -----------------------------

        static List<PixelRect> MergeClose(List<PixelRect> boxes, int mergeGap)
        {
            bool mergedAny = true;
            while (mergedAny)
            {
                mergedAny = false;
                for (int i = 0; i < boxes.Count && !mergedAny; i++)
                {
                    for (int j = i + 1; j < boxes.Count; j++)
                    {
                        if (Gap(boxes[i], boxes[j]) < mergeGap)
                        {
                            boxes[i] = UnionBoxes(boxes[i], boxes[j]);
                            boxes.RemoveAt(j);
                            mergedAny = true;
                            break;
                        }
                    }
                }
            }
            return boxes;
        }

        // Distancia minima entre los bordes de dos cajas (0 si se tocan o se superponen).
        static int Gap(PixelRect a, PixelRect b)
        {
            int gapX = Math.Max(0, Math.Max(a.x - (b.x + b.width), b.x - (a.x + a.width)));
            int gapY = VerticalGap(a, b);
            if (gapX > 0 && gapY > 0)
                return (int)Math.Round(Math.Sqrt((double)gapX * gapX + (double)gapY * gapY));
            return Math.Max(gapX, gapY);
        }

        static int VerticalGap(PixelRect a, PixelRect b)
        {
            return Math.Max(0, Math.Max(a.y - (b.y + b.height), b.y - (a.y + a.height)));
        }

        static PixelRect UnionBoxes(PixelRect a, PixelRect b)
        {
            int minX = Math.Min(a.x, b.x);
            int minY = Math.Min(a.y, b.y);
            int maxX = Math.Max(a.x + a.width, b.x + b.width);
            int maxY = Math.Max(a.y + a.height, b.y + b.height);
            return new PixelRect(minX, minY, maxX - minX, maxY - minY);
        }

        // --- Orden por fila (arriba->abajo) y columna (izq->der) -----------------------

        // En una hoja de personajes las poses de una misma fila comparten "piso" (la misma linea
        // de base): sus cajas se TOCAN o se SUPERPONEN en Y aunque difieran mucho de altura (una
        // pose en el piso es mas baja y mas corta que sus vecinas de pie, y su CENTRO cae lejos del
        // centro de ellas: agrupar por distancia entre centros con tolerancia de media altura
        // mediana la deja afuera de su fila, o peor, la une a la fila de abajo). Entre filas
        // distintas, en cambio, siempre queda un margen sin pixeles opacos. Por eso las filas se
        // agrupan por la SUPERPOSICION/CERCANIA de los bordes verticales de cada caja (no del
        // centro), con una tolerancia chica y fija que cubre el desalineado de un par de pixeles
        // entre poses de una misma fila sin llegar a puentear el margen real entre filas.
        // Publico: SheetSlicer lo reusa para reconstruir donde empieza cada fila (Find ya
        // devuelve las cajas ordenadas fila por fila, pero no expone los cortes entre filas).
        public const float RowOverlapTolerancePx = 4f;

        static List<List<PixelRect>> GroupIntoRows(List<PixelRect> boxes)
        {
            int n = boxes.Count;
            if (n == 0)
                return new List<List<PixelRect>>();

            float tolerance = RowOverlapTolerancePx;

            var parent = new int[n];
            for (int i = 0; i < n; i++)
                parent[i] = i;

            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                if (VerticalGap(boxes[i], boxes[j]) < tolerance)
                    UnionFind(parent, i, j);
            }

            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                int root = FindRoot(parent, i);
                if (!groups.TryGetValue(root, out var list))
                    groups[root] = list = new List<int>();
                list.Add(i);
            }

            var rows = new List<List<int>>(groups.Values);
            // De arriba hacia abajo: por el borde superior mas alto del grupo (no el promedio de
            // centros), asi una pose en el piso no le baja el orden a toda su fila.
            rows.Sort((ra, rb) => RowTop(boxes, rb).CompareTo(RowTop(boxes, ra)));

            var result = new List<List<PixelRect>>(rows.Count);
            foreach (var row in rows)
            {
                row.Sort((ia, ib) => boxes[ia].x.CompareTo(boxes[ib].x));
                var rowBoxes = new List<PixelRect>(row.Count);
                foreach (var i in row)
                    rowBoxes.Add(boxes[i]);
                result.Add(rowBoxes);
            }

            return result;
        }

        static int RowTop(List<PixelRect> boxes, List<int> group)
        {
            int top = int.MinValue;
            foreach (var i in group)
                top = Math.Max(top, boxes[i].y + boxes[i].height);
            return top;
        }

        static int FindRoot(int[] parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }

        static void UnionFind(int[] parent, int a, int b)
        {
            int ra = FindRoot(parent, a);
            int rb = FindRoot(parent, b);
            if (ra != rb)
                parent[ra] = rb;
        }
    }
}
