using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Zombineta.Luz
{
    /// <summary>
    /// Marca una luz como origen de sombras proyectadas: lo que tenga una SombraProyectada y
    /// este dentro de su alcance tira una sombra en el piso, alejandose de este punto.
    ///
    /// No hace falta ponerla a mano: el escenario le agrega una a cada Light2D de sus prefabs
    /// (ver AsegurarEn). Ponerla a mano en un prefab (en la luz o en un hijo suyo, para correr el
    /// punto de donde "sale") sirve para ajustar el alcance y la fuerza de esa luz en particular.
    ///
    /// No usa las sombras de URP 2D: esas recortan la luz en el plano de la pantalla y en vista
    /// lateral no sirven para luces de fondo.
    /// </summary>
    [ExecuteAlways]
    public sealed class FuenteDeSombra : MonoBehaviour
    {
        const string NombreAuto = "Fuente de sombra";

        // Como se traduce una luz a una fuente cuando nadie la ajusto a mano.
        const float AlcanceMinimo = 9f;
        const float AlcanceMaximo = 18f;
        const float AlcancePorRadio = 1.6f;
        const float AlcanceFreeform = 13f;
        const float FuerzaPorIntensidad = 2.5f;
        const float FuerzaMinima = 0.35f;

        [Tooltip("Hasta donde llega, en unidades de mundo. En el borde la sombra ya no se ve.")]
        [Min(0.1f)] public float alcance = 14f;

        [Tooltip("Que tan marcadas son las sombras de esta luz (0 = no proyecta).")]
        [Range(0f, 1f)] public float fuerza = 1f;

        static readonly List<FuenteDeSombra> activas = new List<FuenteDeSombra>();

        Light2D luz;

        // La recarga de dominio esta desactivada: los estaticos sobreviven entre sesiones de Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => activas.RemoveAll(f => f == null);

        /// <summary>Las fuentes prendidas en este momento.</summary>
        public static IReadOnlyList<FuenteDeSombra> Activas => activas;

        public Vector2 Posicion => transform.position;

        /// <summary>Una luz apagada (por presupuesto o por calidad baja) no proyecta nada.</summary>
        public bool Proyecta => luz == null || luz.isActiveAndEnabled;

        void OnEnable()
        {
            luz = GetComponentInParent<Light2D>();
            if (!activas.Contains(this))
                activas.Add(this);
        }

        void OnDisable() => activas.Remove(this);

        /// <summary>
        /// Le agrega una fuente a cada Light2D de ese objeto que todavia no tenga una (propia o en
        /// un hijo), en el centro de la luz y con alcance y fuerza sacados de la luz misma. Es lo
        /// que hace que las sombras aparezcan solas al sumar un prefab con luz al escenario.
        /// </summary>
        public static void AsegurarEn(GameObject raiz)
        {
            foreach (var l in raiz.GetComponentsInChildren<Light2D>(true))
            {
                if (l.lightType == Light2D.LightType.Global || l.GetComponentInChildren<FuenteDeSombra>(true) != null)
                    continue;

                var go = new GameObject(NombreAuto);
                if (!Application.isPlaying)
                    go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(l.transform, false);
                go.transform.position = Centro(l);

                var fuente = go.AddComponent<FuenteDeSombra>();
                fuente.alcance = AlcanceDe(l);
                fuente.fuerza = Mathf.Clamp(l.intensity * FuerzaPorIntensidad, FuerzaMinima, 1f);
            }
        }

        static Vector3 Centro(Light2D l)
        {
            var centro = l.transform.position;
            if (l.lightType == Light2D.LightType.Freeform && l.shapePath != null && l.shapePath.Length > 0)
            {
                var suma = Vector3.zero;
                foreach (var p in l.shapePath)
                    suma += l.transform.TransformPoint(p);
                centro = suma / l.shapePath.Length;
            }
            return new Vector3(centro.x, centro.y, 0f);
        }

        static float AlcanceDe(Light2D l)
        {
            if (l.lightType != Light2D.LightType.Point)
                return AlcanceFreeform;
            float radio = l.pointLightOuterRadius * Mathf.Abs(l.transform.lossyScale.x);
            return Mathf.Clamp(radio * AlcancePorRadio, AlcanceMinimo, AlcanceMaximo);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, alcance);
        }
#endif
    }
}
