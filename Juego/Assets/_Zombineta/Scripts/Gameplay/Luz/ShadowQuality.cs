using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;

namespace Zombineta.Luz
{
    /// <summary>
    /// En calidad Baja las sombras se apagan: menos mallas y overdraw para una maquina lenta.
    /// Busca los ShadowCaster2D del nivel una sola vez al arrancar y de nuevo cuando cambia la
    /// calidad (no hay recorrido por frame), y solo toca su "enabled": los valores que dejo el
    /// artista (forma, dureza, etc.) no se tocan.
    /// </summary>
    public sealed class ShadowQuality : MonoBehaviour
    {
        ShadowCaster2D[] sombras = System.Array.Empty<ShadowCaster2D>();

        void Start()
        {
            Buscar();
            Aplicar();
        }

        void OnEnable()
        {
            GameSettings.LightQualityChanged += OnLightQualityChanged;
        }

        void OnDisable()
        {
            GameSettings.LightQualityChanged -= OnLightQualityChanged;
        }

        void OnLightQualityChanged(LightQuality calidad)
        {
            Buscar();
            Aplicar();
        }

        void Buscar()
        {
            sombras = Object.FindObjectsByType<ShadowCaster2D>(FindObjectsInactive.Include);
        }

        void Aplicar()
        {
            bool prender = GameSettings.LightQuality == LightQuality.Alta;
            for (int i = 0; i < sombras.Length; i++)
                if (sombras[i] != null)
                    sombras[i].enabled = prender;
        }
    }
}
