using UnityEngine;

namespace Zombineta.Player
{
    /// <summary>
    /// De donde sale el tiro: un punto del dibujo (la boca del arma) pasado a coordenadas. El punto se
    /// mide en pixeles desde la esquina de arriba a la izquierda del cuadro, como se lee en cualquier
    /// editor de imagenes; el fogonazo y la traza nacen ahi y no en el centro de la moto.
    /// </summary>
    public static class PuntaDelArma
    {
        /// <summary>
        /// El punto en unidades locales del SpriteRenderer (antes de su escala y rotacion). Con flipX el
        /// dibujo se espeja sobre el pivote, igual que lo hace el SpriteRenderer.
        /// </summary>
        public static Vector2 Local(Sprite cuadro, Vector2 pixelDesdeArriba, bool flipX)
        {
            if (cuadro == null || cuadro.pixelsPerUnit <= 0f)
                return Vector2.zero;
            var rect = cuadro.rect;
            var pivote = cuadro.pivot; // en pixeles, desde abajo a la izquierda del cuadro
            float x = (pixelDesdeArriba.x - pivote.x) / cuadro.pixelsPerUnit;
            float y = (rect.height - pixelDesdeArriba.y - pivote.y) / cuadro.pixelsPerUnit;
            return new Vector2(flipX ? -x : x, y);
        }

        /// <summary>El punto en el mundo, con la posicion, la escala y la rotacion del dibujo.</summary>
        public static Vector3 EnMundo(SpriteRenderer dibujo, Sprite cuadro, Vector2 pixelDesdeArriba)
        {
            var local = Local(cuadro, pixelDesdeArriba, dibujo.flipX);
            return dibujo.transform.TransformPoint(new Vector3(local.x, local.y, 0f));
        }
    }
}
