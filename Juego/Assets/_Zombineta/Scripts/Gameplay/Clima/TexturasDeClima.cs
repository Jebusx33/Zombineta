using UnityEngine;

namespace Zombineta.Clima
{
    /// <summary>Los dibujos del clima, hechos por codigo una sola vez: no necesitan arte.</summary>
    static class TexturasDeClima
    {
        static Sprite niebla;
        static Material gota, salpicadura, papel;

        /// <summary>
        /// Niebla en jirones: ruido suave que se repite de costado sin costura, con los bordes de arriba y
        /// abajo desvanecidos para que no se vea el rectangulo.
        /// </summary>
        public static Sprite Niebla
        {
            get
            {
                if (niebla != null)
                    return niebla;
                const int w = 512, h = 128;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear,
                    name = "Niebla",
                };
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    float v = y / (float)(h - 1);
                    float alto = Mathf.Sin(v * Mathf.PI);
                    alto *= alto;
                    for (int x = 0; x < w; x++)
                    {
                        float u = x / (float)w;
                        // Mezcla de dos lecturas del ruido para que el borde derecho empalme con el izquierdo.
                        float a = Ruido(u * 6f, v * 2.5f);
                        float b = Ruido((u - 1f) * 6f, v * 2.5f);
                        float n = Mathf.Lerp(a, b, u);
                        float denso = Mathf.Clamp01((n - 0.32f) * 1.9f) * alto;
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(denso * 255f));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                niebla = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                niebla.name = "Niebla";
                return niebla;
            }
        }

        static float Ruido(float x, float y) =>
            0.6f * Mathf.PerlinNoise(x + 13.1f, y + 7.7f) + 0.4f * Mathf.PerlinNoise(x * 2.3f + 41f, y * 2.3f + 3f);

        static Material sinLuz;

        /// <summary>El material de sprite sin luz de URP 2D, para lo que se tiene que ver igual de noche.</summary>
        public static Material SinLuz => sinLuz != null ? sinLuz :
            sinLuz = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")) { name = "Clima sin luz" };

        public static Material Gota => gota != null ? gota : gota = Particulas("Gota", (x, y, s) =>
        {
            float d = Mathf.Abs(x - s * 0.5f) / (s * 0.5f);
            return Mathf.Clamp01(1f - d * d);
        });

        public static Material Salpicadura => salpicadura != null ? salpicadura : salpicadura = Particulas("Salpicadura", (x, y, s) =>
        {
            float r = Vector2.Distance(new Vector2(x, y), new Vector2(s * 0.5f, s * 0.5f)) / (s * 0.5f);
            return Mathf.Clamp01(1f - Mathf.Abs(r - 0.75f) / 0.2f);
        });

        public static Material Papel => papel != null ? papel : papel = Particulas("Papel", (x, y, s) =>
        {
            bool dentro = x > s * 0.15f && x < s * 0.85f && y > s * 0.3f && y < s * 0.7f;
            return dentro ? 1f : 0f;
        });

        /// <summary>Un material de particulas sin luz con una forma blanca: el color lo pone cada sistema.</summary>
        static Material Particulas(string nombre, System.Func<int, int, int, float> alfa)
        {
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = nombre };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(alfa(x, y, s) * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, true);

            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            var mat = new Material(shader) { name = nombre, mainTexture = tex };
            return mat;
        }
    }
}
