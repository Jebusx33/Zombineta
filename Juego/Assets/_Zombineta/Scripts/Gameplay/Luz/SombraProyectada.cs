using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Luz
{
    /// <summary>
    /// Sombra proyectada en el piso: una copia oscura del sprite, acostada e inclinada, que nace
    /// en sus pies. Es una sola por objeto: su direccion y su largo salen de mezclar todas las
    /// FuenteDeSombra cercanas segun cuanto pesa cada una, asi gira de forma continua al pasar
    /// de una luz a otra en vez de saltar. Lejos de toda luz queda la sombra corta de ambiente,
    /// para que nada parezca flotar.
    ///
    /// No hace falta ponerla a mano: la horda, los items del nivel, la moto y las capas del
    /// escenario marcadas para eso la agregan solas (ver Poner). Funciona tambien sin dar Play.
    /// Con la calidad de luz en Baja solo queda la sombra de ambiente.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(500)]
    public sealed class SombraProyectada : MonoBehaviour
    {
        const string NombreRaiz = "Sombras proyectadas";

        [Tooltip("El sprite que proyecta. Vacio = el de este objeto o el primero de sus hijos.")]
        [SerializeField] SpriteRenderer origen;

        [Tooltip("Color de la sombra. El alfa es el de una sombra a pleno, pegada a la luz.")]
        [SerializeField] Color color = new Color(0f, 0f, 0.03f, 0.55f);

        [Tooltip("Desmarcado, la sombra no mira las luces: queda fija en la direccion de ambiente. " +
                 "Para lo que se mueve distinto que las luces (arboles del fondo), donde seguirlas " +
                 "hace que la sombra se corra sola.")]
        [SerializeField] bool seguirLuces = true;

        [Header("Forma")]
        [Tooltip("Largo de la sombra con la luz pegada, como fraccion del alto del sprite.")]
        [SerializeField] float largoCerca = 0.5f;
        [Tooltip("Largo de la sombra en el borde del alcance de la luz.")]
        [SerializeField] float largoLejos = 1.4f;
        [Tooltip("Cuanto se achica la sombra en vertical: el piso se ve en escorzo.")]
        [Range(0.05f, 1f)] [SerializeField] float aplastado = 0.45f;
        [Tooltip("Unidades de mundo que la sombra se mete por debajo de los pies. Sin esto queda " +
                 "una rendija entre el personaje y su sombra y parece que flota.")]
        [Min(0f)] [SerializeField] float hundido = 0.1f;

        [Header("Ambiente (lejos de toda luz)")]
        [Tooltip("Hacia donde cae la sombra de ambiente, como fraccion del alto del sprite.")]
        [SerializeField] Vector2 ambiente = new Vector2(0.22f, -0.2f);
        [Tooltip("Que tan marcada es la sombra de ambiente respecto de una a pleno.")]
        [Range(0f, 1f)] [SerializeField] float fuerzaAmbiente = 0.7f;

        [Header("Dibujo")]
        [Tooltip("Sorting Layer donde se dibuja. Vacio = la misma del sprite.")]
        [SerializeField] string capa = "";
        [Tooltip("Orden de dibujo: relativo al sprite (negativo = por debajo), o tal cual si se marca Orden Absoluto.")]
        [SerializeField] int orden = -1;
        [SerializeField] bool ordenAbsoluto;

        Transform raiz;
        SpriteRenderer copia;
        bool visible = true;
        static Transform contenedor;

        /// <summary>Apagarla sin sacar el componente: algo que esta en el aire no proyecta desde sus pies.</summary>
        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        /// <summary>El orden de dibujo, para lo que cambia de carril (se dibuja debajo de lo de su carril).</summary>
        public int Orden
        {
            get => orden;
            set => orden = value;
        }

        /// <summary>
        /// Le pone una sombra proyectada a ese sprite si todavia no la tiene. En el editor (sin
        /// Play) no se guarda en la escena: se vuelve a poner sola.
        /// </summary>
        public static SombraProyectada Poner(GameObject donde, SpriteRenderer sprite, string capa, int orden,
            bool ordenAbsoluto, bool seguirLuces = true)
        {
            var sombra = donde.GetComponent<SombraProyectada>();
            if (sombra == null)
            {
                sombra = donde.AddComponent<SombraProyectada>();
                if (!Application.isPlaying)
                    sombra.hideFlags = HideFlags.DontSave;
            }

            sombra.origen = sprite;
            sombra.capa = capa;
            sombra.orden = orden;
            sombra.ordenAbsoluto = ordenAbsoluto;
            sombra.seguirLuces = seguirLuces;
            return sombra;
        }

        SpriteRenderer Origen
        {
            get
            {
                if (origen == null)
                    origen = GetComponentInChildren<SpriteRenderer>();
                return origen;
            }
        }

        void OnEnable() => Actualizar();

        void OnDisable() => Destruir();

        void LateUpdate() => Actualizar();

        void Actualizar()
        {
            var sr = Origen;
            // isVisible: lo que esta fuera de cuadro no gasta nada (un nivel tiene cientos de items).
            if (!visible || sr == null || sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy ||
                (Application.isPlaying && !sr.isVisible))
            {
                if (copia != null)
                    copia.enabled = false;
                return;
            }

            Asegurar();

            // Los pies de verdad (la fila mas baja con pixeles opacos), no el borde del sprite:
            // si la sombra nace mas abajo, el personaje parece flotar.
            Vector2 apoyoLocal, pies;
            if (Mathf.Abs(Mathf.DeltaAngle(sr.transform.eulerAngles.z, 0f)) < 0.5f)
            {
                apoyoLocal = SpriteApoyo.De(sr.sprite);
                pies = sr.transform.TransformPoint(apoyoLocal);
            }
            else
            {
                // Girado (un auto inclinado, la moto caida): lo que toca el piso es otra parte.
                pies = SpriteApoyo.PiesGirado(sr);
                apoyoLocal = sr.transform.InverseTransformPoint(pies);
            }
            pies.y += hundido;

            // Todas las luces que llegan tiran para su lado, cada una con su peso: la sombra es
            // el promedio. Al alejarse de una y acercarse a otra gira de a poco, sin saltos.
            var suma = Vector2.zero;
            float pesos = 0f;
            if (seguirLuces && GameSettings.LightQuality == LightQuality.Alta)
            {
                var fuentes = FuenteDeSombra.Activas;
                for (int i = 0; i < fuentes.Count; i++)
                {
                    var f = fuentes[i];
                    if (f == null || !f.Proyecta)
                        continue;
                    float peso = SombraMath.Peso(Vector2.Distance(pies, f.Posicion), f.alcance, f.fuerza);
                    if (peso <= 0f)
                        continue;
                    suma += peso * SombraMath.Vector(pies, f.Posicion, f.alcance, largoCerca, largoLejos, aplastado);
                    pesos += peso;
                }
            }

            var v = SombraMath.Mezclar(suma, pesos, ambiente);
            Colocar(sr, pies, apoyoLocal, v, SombraMath.Intensidad(pesos, fuerzaAmbiente));
        }

        void Colocar(SpriteRenderer sr, Vector2 pies, Vector2 apoyoLocal, Vector2 v, float peso)
        {
            // El ancho queda igual; el alto del sprite se acuesta sobre el vector de la sombra.
            SombraMath.Descomponer(1f, v.x, 0f, v.y, out float giroFuera, out var escala, out float giroDentro);

            raiz.SetPositionAndRotation(new Vector3(pies.x, pies.y, 0f), Quaternion.Euler(0f, 0f, giroFuera));
            raiz.localScale = new Vector3(escala.x, escala.y, 1f);

            var t = copia.transform;
            var giro = Quaternion.Euler(0f, 0f, giroDentro + sr.transform.eulerAngles.z);
            var tamano = sr.transform.lossyScale;
            t.localRotation = giro;
            t.localScale = new Vector3(tamano.x, tamano.y, 1f);
            // El apoyo del sprite tiene que caer justo en la raiz: de ahi nace la sombra.
            t.localPosition = -(giro * new Vector3(apoyoLocal.x * tamano.x, apoyoLocal.y * tamano.y, 0f));

            copia.sprite = sr.sprite;
            copia.flipX = sr.flipX;
            copia.flipY = sr.flipY;
            copia.sharedMaterial = sr.sharedMaterial;
            copia.sortingLayerID = string.IsNullOrEmpty(capa) ? sr.sortingLayerID : SortingLayer.NameToID(capa);
            copia.sortingOrder = ordenAbsoluto ? orden : sr.sortingOrder + orden;

            var c = color;
            c.a *= peso;
            copia.color = c;
            copia.enabled = c.a > 0.01f;
        }

        void Asegurar()
        {
            if (raiz != null)
                return;

            // Fuera del objeto que proyecta: la deformacion no puede heredar su giro ni su escala.
            // En el editor no se guardan en la escena; en juego son objetos comunes, que se van
            // con la escena (DontSave los dejaria vivos entre un nivel y el siguiente).
            var go = new GameObject("Sombra de " + name);
            var hijo = new GameObject("Silueta");
            if (!Application.isPlaying)
            {
                go.hideFlags = HideFlags.DontSave;
                hijo.hideFlags = HideFlags.DontSave;
            }
            go.transform.SetParent(Contenedor(), false);
            hijo.transform.SetParent(go.transform, false);

            raiz = go.transform;
            copia = hijo.AddComponent<SpriteRenderer>();
            copia.enabled = false;
        }

        static Transform Contenedor()
        {
            if (contenedor == null)
            {
                var go = GameObject.Find(NombreRaiz);
                if (go == null)
                {
                    go = new GameObject(NombreRaiz);
                    if (!Application.isPlaying)
                        go.hideFlags = HideFlags.DontSave;
                }
                contenedor = go.transform;
            }
            return contenedor;
        }

        void Destruir()
        {
            if (raiz == null)
                return;
            if (Application.isPlaying)
                Destroy(raiz.gameObject);
            else
                DestroyImmediate(raiz.gameObject);
            raiz = null;
            copia = null;
        }
    }
}
