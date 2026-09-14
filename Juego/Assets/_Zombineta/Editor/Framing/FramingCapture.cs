using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zombineta.Juego.EditorTools.Framing
{
    /// <summary>
    /// Saca una foto de lo que ve la camara principal a 1920x1080 y le mezcla encima la imagen de
    /// referencia de FramingGuide. Sirve para comparar el encuadre sin depender de la ventana del
    /// Game view (ni de su tamano, ni de enfocarla). Solo editor.
    /// </summary>
    public static class FramingCapture
    {
        public const int Width = 1920;
        public const int Height = 1080;

        [MenuItem("Zombineta/Encuadre/Capturar comparacion")]
        static void MenuCapture()
        {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/Encuadre"));
            var path = Path.Combine(dir, "comparacion_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            if (Capture(path, FramingGuide.Opacity))
                Debug.Log("FramingCapture: captura guardada en " + path);
        }

        /// <summary>
        /// Renderiza Camera.main y escribe un PNG en outPath con la referencia mezclada a
        /// guideOpacity (0 = solo el render). Devuelve false si no hay camara.
        /// </summary>
        public static bool Capture(string outPath, float guideOpacity)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("FramingCapture: no hay Camera.main para capturar.");
                return false;
            }

            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            try
            {
                var rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
                var previousTarget = cam.targetTexture;
                var previousActive = RenderTexture.active;
                try
                {
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    shot.Apply(false);
                }
                finally
                {
                    cam.targetTexture = previousTarget;
                    RenderTexture.active = previousActive;
                    RenderTexture.ReleaseTemporary(rt);
                }

                float alpha = Mathf.Clamp01(guideOpacity);
                var guide = alpha > 0f ? FramingGuide.LoadTexture(FramingGuide.ImagePath) : null;
                if (guide != null)
                    Blend(shot, guide, alpha);

                var dir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllBytes(outPath, shot.EncodeToPNG());
                return true;
            }
            finally
            {
                // Pase lo que pase (render, mezcla o disco), la textura temporal no queda viva.
                Object.DestroyImmediate(shot);
            }
        }

        /// <summary>Mezcla la guia (estirada a la foto, como el overlay a pantalla completa) con alfa fijo.</summary>
        static void Blend(Texture2D shot, Texture2D guide, float alpha)
        {
            var pixels = shot.GetPixels32();
            bool sameSize = guide.width == Width && guide.height == Height;
            var guidePixels = sameSize ? guide.GetPixels32() : null;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    Color32 g = sameSize
                        ? guidePixels[i]
                        : (Color32)guide.GetPixelBilinear((x + 0.5f) / Width, (y + 0.5f) / Height);
                    float a = alpha * g.a / 255f;
                    var p = pixels[i];
                    pixels[i] = new Color32(
                        (byte)(p.r + (g.r - p.r) * a),
                        (byte)(p.g + (g.g - p.g) * a),
                        (byte)(p.b + (g.b - p.b) * a),
                        255);
                }
            }

            shot.SetPixels32(pixels);
            shot.Apply(false);
        }
    }
}
