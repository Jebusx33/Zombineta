using UnityEngine;
using UnityEngine.UI;

namespace Zombineta.Juego.Screens
{
    /// <summary>
    /// Muestra una pantalla de UI como en un televisor viejo. La UI se dibuja con la camara de su escena a
    /// una textura del tamano de la pantalla, y esa textura se ve en un RawImage a pantalla completa con el
    /// shader CRT (Resources/Estilo/CRT): curva de gran angular, esquinas redondeadas, lineas de barrido.
    /// Al aparecer se prende como una tele (EncendidoTV). Mientras tanto la camara de respaldo de Boot pinta
    /// el fondo de negro, porque la camara de la escena ya no dibuja en pantalla.
    /// </summary>
    public sealed class PantallaCRT : MonoBehaviour
    {
        const string RutaShader = "Estilo/CRT";

        [Tooltip("Segundos que tarda en prenderse la tele.")]
        [SerializeField] float segundosEncendido = 0.75f;
        [Range(0f, 0.4f)] [SerializeField] float curvatura = 0.12f;
        [Range(0f, 0.3f)] [SerializeField] float radioEsquinas = 0.07f;
        [Range(0f, 1f)] [SerializeField] float lineas = 0.22f;
        [SerializeField] float lineasEnElAlto = 300f;
        [Range(0f, 0.01f)] [SerializeField] float aberracion = 0.0022f;
        [Range(0f, 1f)] [SerializeField] float vineta = 0.45f;

        Camera camara;
        RawImage imagen;
        RenderTexture textura;
        Material material;
        float inicio = -1f;

        /// <summary>Segundos que tarda en prenderse.</summary>
        public float SegundosEncendido => segundosEncendido;

        /// <summary>De 0 (apagada) a 1 (prendida del todo).</summary>
        public float Encendido => inicio < 0f || segundosEncendido <= 0f
            ? 1f
            : Mathf.Clamp01((Time.unscaledTime - inicio) / segundosEncendido);

        /// <summary>
        /// Pasa la UI de 'canvas' a la tele: el canvas se dibuja con 'camara' a una textura y aparece una
        /// pantalla CRT encima. Se prende desde ya.
        /// </summary>
        public static PantallaCRT Poner(Canvas canvas, Camera camara)
        {
            // Un canvas aparte en la raiz (dentro del de la UI tomaria su modo de dibujo) y en la misma escena,
            // asi se va con ella: la escena activa puede ser otra mientras se carga.
            var go = new GameObject("Pantalla CRT", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, canvas.gameObject.scene);
            var crt = go.AddComponent<PantallaCRT>();
            crt.Armar(canvas, camara);
            return crt;
        }

        void Armar(Canvas ui, Camera cam)
        {
            camara = cam;
            var shader = Resources.Load<Shader>(RutaShader);
            if (camara == null || shader == null)
            {
                Debug.LogWarning("PantallaCRT: sin camara o sin el shader " + RutaShader + "; la pantalla se ve sin tele.", this);
                enabled = false;
                return;
            }

            CrearTextura();
            ui.renderMode = RenderMode.ScreenSpaceCamera;
            ui.worldCamera = camara;
            ui.planeDistance = 1f;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // debajo del fundido de Boot (1000)

            var rawGo = new GameObject("Tubo", typeof(RectTransform), typeof(RawImage));
            rawGo.transform.SetParent(transform, false);
            var rt = (RectTransform)rawGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            imagen = rawGo.GetComponent<RawImage>();
            imagen.raycastTarget = false;
            material = new Material(shader) { name = "CRT (runtime)" };
            imagen.material = material;
            imagen.texture = textura;

            inicio = Time.unscaledTime;
            Aplicar();
            // El golpe del encendido, junto con el punto blanco (el archivo arranca sin silencio).
            Zombineta.Audio.AudioDirector.Instance?.Play(Zombineta.Audio.SonidoClave.MapaEncendido);
        }

        void CrearTextura()
        {
            int ancho = Mathf.Max(16, Screen.width), alto = Mathf.Max(16, Screen.height);
            if (textura != null && textura.width == ancho && textura.height == alto)
                return;
            if (textura != null)
            {
                camara.targetTexture = null;
                textura.Release();
                Destroy(textura);
            }
            textura = new RenderTexture(ancho, alto, 24) { name = "Pantalla CRT", filterMode = FilterMode.Bilinear };
            camara.targetTexture = textura;
            if (imagen != null)
                imagen.texture = textura;
        }

        void LateUpdate()
        {
            CrearTextura(); // si cambio la resolucion
            Aplicar();
        }

        void Aplicar()
        {
            if (material == null)
                return;
            var e = EncendidoTV.En(Encendido);
            material.SetFloat("_Curvatura", curvatura);
            material.SetFloat("_RadioEsquina", radioEsquinas);
            material.SetFloat("_Lineas", lineas);
            material.SetFloat("_LineasCantidad", lineasEnElAlto);
            material.SetFloat("_Aberracion", aberracion);
            material.SetFloat("_Vineta", vineta);
            material.SetFloat("_Aspecto", Screen.height > 0 ? (float)Screen.width / Screen.height : 1.7777f);
            material.SetFloat("_Ancho", e.ancho);
            material.SetFloat("_Alto", e.alto);
            material.SetFloat("_Brillo", e.brillo);
            material.SetFloat("_Tiempo", Time.unscaledTime);
        }

        void OnDestroy()
        {
            if (camara != null && camara.targetTexture == textura)
                camara.targetTexture = null;
            if (textura != null)
            {
                textura.Release();
                Destroy(textura);
            }
            if (material != null)
                Destroy(material);
        }
    }
}
