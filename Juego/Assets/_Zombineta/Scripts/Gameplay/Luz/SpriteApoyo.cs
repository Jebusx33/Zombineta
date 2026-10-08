using System.Collections.Generic;
using UnityEngine;

namespace Zombineta.Luz
{
    /// <summary>
    /// Donde toca el piso un sprite, en sus unidades locales: el centro (en X) de lo dibujado y
    /// la fila mas baja con pixeles opacos de verdad. Ni el rectangulo del sprite ni su malla
    /// sirven: los dos incluyen el aire de abajo y cualquier sombra suave pintada en el arte, y
    /// una sombra proyectada que nazca ahi deja al personaje flotando.
    ///
    /// Se calcula una vez por sprite. La textura no hace falta que sea legible: se copia chica
    /// a una RenderTexture una sola vez por hoja y de ahi se lee el alfa.
    /// </summary>
    public static class SpriteApoyo
    {
        // La hoja se lee a un cuarto de su tamano: alcanza para ubicar los pies y pesa 16 veces menos.
        const int Reduccion = 4;

        // Por debajo de esto un pixel es aire o una sombra suave pintada, no el cuerpo.
        const byte AlfaSolido = 128;

        // Pixeles solidos que tiene que tener una fila para contar como apoyo (no un punto suelto).
        const int MinimoPorFila = 2;

        sealed class Alfa
        {
            public byte[] datos;
            public int ancho, alto;
            public float escalaX, escalaY; // de pixel de la hoja a pixel de la copia
        }

        static readonly Dictionary<Sprite, Vector2> apoyos = new Dictionary<Sprite, Vector2>();
        static readonly Dictionary<Texture2D, Alfa> hojas = new Dictionary<Texture2D, Alfa>();

        public static Vector2 De(Sprite sprite)
        {
            if (apoyos.TryGetValue(sprite, out var apoyo))
                return apoyo;

            if (!PorAlfa(sprite, out apoyo))
                apoyo = PorMalla(sprite);

            apoyos[sprite] = apoyo;
            return apoyo;
        }

        static bool PorAlfa(Sprite sprite, out Vector2 apoyo)
        {
            apoyo = default;
            // En un atlas con sprites rotados la cuenta de abajo no vale: que resuelva la malla.
            if (sprite.texture == null || (sprite.packed && sprite.packingRotation != SpritePackingRotation.None))
                return false;

            var alfa = AlfaDe(sprite.texture);
            if (alfa == null)
                return false;

            var rect = sprite.textureRect;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(rect.xMin * alfa.escalaX), 0, alfa.ancho - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(rect.xMax * alfa.escalaX), x0 + 1, alfa.ancho);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(rect.yMin * alfa.escalaY), 0, alfa.alto - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(rect.yMax * alfa.escalaY), y0 + 1, alfa.alto);

            int piso = -1, izquierda = int.MaxValue, derecha = int.MinValue;
            for (int y = y0; y < y1; y++)
            {
                int solidos = 0;
                int fila = y * alfa.ancho;
                for (int x = x0; x < x1; x++)
                {
                    if (alfa.datos[fila + x] < AlfaSolido)
                        continue;
                    solidos++;
                    if (x < izquierda) izquierda = x;
                    if (x > derecha) derecha = x;
                }
                if (piso < 0 && solidos >= MinimoPorFila)
                    piso = y;
            }

            if (piso < 0)
                return false;

            // De pixel de la copia a pixel de la hoja, y de ahi a unidades locales del sprite
            // (relativas a su pivot, como sprite.bounds).
            float pisoHoja = piso / alfa.escalaY;
            float centroHoja = (izquierda + derecha + 1) * 0.5f / alfa.escalaX;
            var desfase = sprite.textureRectOffset;
            float ppu = sprite.pixelsPerUnit;
            apoyo = new Vector2(
                (centroHoja - rect.xMin + desfase.x - sprite.pivot.x) / ppu,
                (pisoHoja - rect.yMin + desfase.y - sprite.pivot.y) / ppu);
            return true;
        }

        static Alfa AlfaDe(Texture2D hoja)
        {
            if (hojas.TryGetValue(hoja, out var alfa))
                return alfa;

            int ancho = Mathf.Max(1, hoja.width / Reduccion);
            int alto = Mathf.Max(1, hoja.height / Reduccion);

            var rt = RenderTexture.GetTemporary(ancho, alto, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var anterior = RenderTexture.active;
            Texture2D copia = null;
            try
            {
                Graphics.Blit(hoja, rt);
                RenderTexture.active = rt;
                copia = new Texture2D(ancho, alto, TextureFormat.RGBA32, false, true);
                copia.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
                copia.Apply(false);

                var pixeles = copia.GetPixels32();
                alfa = new Alfa
                {
                    datos = new byte[pixeles.Length],
                    ancho = ancho,
                    alto = alto,
                    escalaX = (float)ancho / hoja.width,
                    escalaY = (float)alto / hoja.height,
                };
                for (int i = 0; i < pixeles.Length; i++)
                    alfa.datos[i] = pixeles[i].a;
            }
            catch (System.Exception)
            {
                alfa = null; // Sin lectura posible: la malla hace de respaldo.
            }
            finally
            {
                RenderTexture.active = anterior;
                RenderTexture.ReleaseTemporary(rt);
                if (copia != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(copia);
                    else
                        Object.DestroyImmediate(copia);
                }
            }

            hojas[hoja] = alfa;
            return alfa;
        }

        static readonly Dictionary<Sprite, Vector2[]> mallas = new Dictionary<Sprite, Vector2[]>();
        static readonly Dictionary<(Sprite, int), float> bajos = new Dictionary<(Sprite, int), float>();

        /// <summary>La malla del sprite, guardada: sprite.vertices arma un arreglo nuevo en cada llamada.</summary>
        static Vector2[] Malla(Sprite sprite)
        {
            if (!mallas.TryGetValue(sprite, out var malla))
            {
                malla = sprite.vertices ?? new Vector2[0];
                mallas[sprite] = malla;
            }
            return malla;
        }

        /// <summary>
        /// La Y mas baja de lo dibujado con el sprite girado esos grados, en sus unidades locales
        /// (respecto del pivot). Para apoyar en el piso algo que esta inclinado.
        /// </summary>
        public static float MasBajo(Sprite sprite, float grados)
        {
            var clave = (sprite, Mathf.RoundToInt(grados * 10f));
            if (bajos.TryGetValue(clave, out float bajo))
                return bajo;

            var malla = Malla(sprite);
            if (malla.Length == 0)
            {
                bajo = sprite.bounds.min.y;
            }
            else
            {
                float seno = Mathf.Sin(grados * Mathf.Deg2Rad), coseno = Mathf.Cos(grados * Mathf.Deg2Rad);
                bajo = float.MaxValue;
                for (int i = 0; i < malla.Length; i++)
                    bajo = Mathf.Min(bajo, malla[i].x * seno + malla[i].y * coseno);
            }

            bajos[clave] = bajo;
            return bajo;
        }

        /// <summary>
        /// Donde toca el piso un sprite que esta girado, en el mundo: el punto mas bajo de su
        /// malla ya girada, centrado en su ancho. Con el sprite derecho conviene De(), que mira
        /// los pixeles; girado, la fila mas baja de la textura ya no es la de abajo.
        /// </summary>
        public static Vector2 PiesGirado(SpriteRenderer sr)
        {
            var malla = Malla(sr.sprite);
            if (malla.Length == 0)
                return sr.bounds.center - new Vector3(0f, sr.bounds.extents.y, 0f);

            var t = sr.transform;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue;
            for (int i = 0; i < malla.Length; i++)
            {
                var p = t.TransformPoint(malla[i]);
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
            }
            return new Vector2((minX + maxX) * 0.5f, minY);
        }

        /// <summary>Respaldo: el borde de abajo de la malla del sprite (o de su rectangulo).</summary>
        static Vector2 PorMalla(Sprite sprite)
        {
            var vertices = Malla(sprite);
            if (vertices == null || vertices.Length == 0)
            {
                var limites = sprite.bounds;
                return new Vector2(limites.center.x, limites.min.y);
            }

            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                var p = vertices[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
            }
            return new Vector2((minX + maxX) * 0.5f, minY);
        }
    }
}
