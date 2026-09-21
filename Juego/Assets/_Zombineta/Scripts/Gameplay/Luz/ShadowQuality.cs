using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Core;

namespace Zombineta.Luz
{
    /// <summary>
    /// En calidad Baja las sombras se apagan: menos mallas y overdraw para una maquina lenta.
    /// Al arrancar y cada vez que cambia la calidad barre TODA la escena una sola vez (no hay
    /// recorrido por frame) y solo toca "enabled": los valores que dejo el artista (forma, dureza,
    /// etc.) no se tocan.
    ///
    /// Ese barrido no ve lo que se crea despues (tiles de SceneryManager al avanzar la camara,
    /// cuerpos de HordeView, la vista de un item armada por LevelScene en Play): si la calidad
    /// nunca vuelve a cambiar, esos ShadowCaster2D quedan con lo que traiga su prefab, prendido
    /// las mas de las veces. Por eso quien instancia algo con sombra propia llama a Apply(...)
    /// sobre el objeto recien creado: aplica la calidad actual a esa unica jerarquia (chica, no es
    /// un recorrido de toda la escena) y no necesita ningun estado ni lista propia.
    /// </summary>
    public sealed class ShadowQuality : MonoBehaviour
    {
        ShadowCaster2D[] sombras = System.Array.Empty<ShadowCaster2D>();

        void Start()
        {
            Buscar();
            AplicarATodas();
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
            AplicarATodas();
        }

        void Buscar()
        {
            sombras = Object.FindObjectsByType<ShadowCaster2D>(FindObjectsInactive.Include);
        }

        void AplicarATodas()
        {
            bool prender = GameSettings.LightQuality == LightQuality.Alta;
            for (int i = 0; i < sombras.Length; i++)
                if (sombras[i] != null)
                    sombras[i].enabled = prender;
        }

        /// <summary>
        /// Aplica la calidad de sombra ACTUAL a un objeto recien instanciado (y a sus hijos), sin
        /// esperar el proximo cambio de calidad ni recorrer la escena entera. La llaman
        /// SceneryManager al crear un tile, HordeView al crear un cuerpo y LevelScene al armar la
        /// vista de un item en Play. Si despues cambia la calidad, el barrido de arriba (evento)
        /// vuelve a pasar por todo lo que exista en ese momento, este objeto incluido.
        /// </summary>
        public static void Apply(GameObject go)
        {
            if (go == null)
                return;

            bool prender = GameSettings.LightQuality == LightQuality.Alta;
            var casters = go.GetComponentsInChildren<ShadowCaster2D>(true);
            for (int i = 0; i < casters.Length; i++)
                casters[i].enabled = prender;
        }
    }
}
