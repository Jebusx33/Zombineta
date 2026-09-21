using System.Collections.Generic;
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
    ///
    /// Nombre: se llamaba "ShadowQuality", que choca con el enum del motor
    /// (UnityEngine.ShadowQuality, de sombras 3D) y obligaba a calificar cada llamada como
    /// Zombineta.Luz.ShadowQuality.Apply(...) en cualquier archivo con "using UnityEngine;".
    /// Renombrada para que los llamadores puedan usarla sin calificar.
    /// </summary>
    public sealed class ShadowCasterQuality : MonoBehaviour
    {
        ShadowCaster2D[] sombras = System.Array.Empty<ShadowCaster2D>();

        /// <summary>
        /// Estado que dejo el autor (enabled del ShadowCaster2D la primera vez que se lo ve, sea
        /// por el barrido de este componente o por Apply sobre un objeto recien instanciado). En
        /// Alta se restaura ESE estado, no se fuerza true a ciegas: asi no se pisa un caster que
        /// arte apago a proposito. En Baja se apaga siempre, sin excepcion.
        /// </summary>
        static readonly Dictionary<ShadowCaster2D, bool> estadoAutor = new Dictionary<ShadowCaster2D, bool>();

        // Sin recarga de dominio en este proyecto: sin este reinicio, una sesion de Play anterior
        // dejaria referencias a casters ya destruidos (y sus estados) vivas en el diccionario.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarEstatico() => estadoAutor.Clear();

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
            for (int i = 0; i < sombras.Length; i++)
                RegistrarEstadoAutor(sombras[i]);
        }

        void AplicarATodas()
        {
            bool alta = GameSettings.LightQuality == LightQuality.Alta;
            for (int i = 0; i < sombras.Length; i++)
                if (sombras[i] != null)
                    sombras[i].enabled = alta && EstadoAutor(sombras[i]);
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

            bool alta = GameSettings.LightQuality == LightQuality.Alta;
            var casters = go.GetComponentsInChildren<ShadowCaster2D>(true);
            for (int i = 0; i < casters.Length; i++)
            {
                RegistrarEstadoAutor(casters[i]);
                casters[i].enabled = alta && EstadoAutor(casters[i]);
            }
        }

        /// <summary>Guarda el enabled de este caster la PRIMERA vez que se lo ve (antes de que
        /// cualquier barrido de calidad lo toque): ese es el que dejo el autor.</summary>
        static void RegistrarEstadoAutor(ShadowCaster2D caster)
        {
            if (caster != null && !estadoAutor.ContainsKey(caster))
                estadoAutor[caster] = caster.enabled;
        }

        static bool EstadoAutor(ShadowCaster2D caster) =>
            caster == null || !estadoAutor.TryGetValue(caster, out var prendido) || prendido;
    }
}
