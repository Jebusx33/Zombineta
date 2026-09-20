using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zombineta.Juego.Levels;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Zombineta.Luz
{
    /// <summary>
    /// Arma la luz ambiente del nivel a partir de un PerfilDeLuz: un hijo "Luz Global <capa>" por
    /// cada entrada del perfil (mas una general), cada uno con una Light2D global en Multiply para
    /// no pelear con las luces puntuales (faro, etc.) que ya cubren las seis capas. Se reconstruye
    /// solo si el perfil o alguno de sus valores cambio, asi que arte lo ve en vivo sin tocar la
    /// escena.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-150)]
    public sealed class LightingDirector : MonoBehaviour
    {
        const string ChildPrefix = "Luz Global ";
        const string GeneralName = ChildPrefix + "General";

        [SerializeField] PerfilDeLuz perfil;

        public PerfilDeLuz Perfil
        {
            get => perfil;
            set => perfil = value;
        }

        readonly List<GameObject> hijos = new List<GameObject>();
        PerfilDeLuz perfilConstruido;
        string hashConstruido;
        bool avisoSinPerfil;

        void Awake()
        {
            TomarPerfilDeLevelScene();
        }

        void OnEnable()
        {
            TomarPerfilDeLevelScene();
            Rebuild();
        }

        void OnValidate()
        {
            TomarPerfilDeLevelScene();
        }

        void Update()
        {
            if (perfil != perfilConstruido || Hash(perfil) != hashConstruido)
                Rebuild();
        }

        void TomarPerfilDeLevelScene()
        {
            if (perfil != null)
                return;

            var escena = GetComponent<LevelScene>();
            if (escena != null && escena.Perfil != null)
                perfil = escena.Perfil;
        }

        public void Rebuild()
        {
            DestruirHijos();
            perfilConstruido = perfil;
            hashConstruido = Hash(perfil);

            if (perfil == null)
            {
                if (!avisoSinPerfil)
                {
                    Debug.LogWarning("LightingDirector: sin PerfilDeLuz asignado, no se crea ninguna luz global.");
                    avisoSinPerfil = true;
                }
                return;
            }

            avisoSinPerfil = false;

            foreach (var ambiente in perfil.capas)
            {
                if (ambiente == null || string.IsNullOrEmpty(ambiente.capa))
                    continue;

                CrearLuz(ChildPrefix + ambiente.capa, ambiente.color, ambiente.intensidad, new[] { ambiente.capa });
            }

            CrearLuz(GeneralName, perfil.colorGeneral, perfil.intensidadGeneral, TodasLasCapas());
        }

        void CrearLuz(string nombre, Color color, float intensidad, string[] capas)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            go.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSave;

            var luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Global;
            luz.blendStyleIndex = 0; // Multiply: no pelea con las luces puntuales existentes.
            luz.color = color;
            luz.intensity = intensidad;

            AplicarCapas(luz, capas);
            hijos.Add(go);
        }

        static void AplicarCapas(Light2D luz, string[] nombresCapas)
        {
            var ids = new List<int>(nombresCapas.Length);
            foreach (var nombre in nombresCapas)
            {
                foreach (var capa in SortingLayer.layers)
                {
                    if (capa.name == nombre)
                    {
                        ids.Add(capa.id);
                        break;
                    }
                }
            }

#if UNITY_EDITOR
            var so = new SerializedObject(luz);
            var prop = so.FindProperty("m_ApplyToSortingLayers");
            prop.ClearArray();
            for (int i = 0; i < ids.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).intValue = ids[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        static string[] TodasLasCapas()
        {
            var capas = SortingLayer.layers;
            var nombres = new string[capas.Length];
            for (int i = 0; i < capas.Length; i++)
                nombres[i] = capas[i].name;
            return nombres;
        }

        void DestruirHijos()
        {
            foreach (var hijo in hijos)
                if (hijo != null)
                {
                    if (Application.isPlaying)
                        Destroy(hijo);
                    else
                        DestroyImmediate(hijo);
                }
            hijos.Clear();
        }

        void OnDisable()
        {
            DestruirHijos();
        }

        /// <summary>Resumen de perfil + valores, para saber si hay que reconstruir.</summary>
        static string Hash(PerfilDeLuz p)
        {
            if (p == null)
                return "null";

            var sb = new StringBuilder();
            sb.Append(p.capas.Count).Append('|');
            foreach (var a in p.capas)
            {
                if (a == null)
                {
                    sb.Append("null;");
                    continue;
                }
                sb.Append(a.capa).Append(':').Append(a.color).Append(':').Append(a.intensidad).Append(';');
            }
            sb.Append(p.colorGeneral).Append('|').Append(p.intensidadGeneral);
            return sb.ToString();
        }
    }
}
