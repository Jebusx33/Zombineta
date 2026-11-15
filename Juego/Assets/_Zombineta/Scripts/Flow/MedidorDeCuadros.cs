#if ZOMBINETA_MEDIDOR
using UnityEngine;

namespace Zombineta.Flow
{
    /// <summary>
    /// Solo en los builds de prueba (define ZOMBINETA_MEDIDOR, que pone BuildAndroid): arriba a la izquierda
    /// muestra los cuadros por segundo del ultimo segundo, el cuadro mas lento (los tirones), cuantos pasaron
    /// de 25 ms, la API grafica en uso y la resolucion. Sirve para medir en la consola sin conectar el profiler.
    /// </summary>
    public sealed class MedidorDeCuadros : MonoBehaviour
    {
        float acumulado, peor;
        int cuadros, lentos;
        string texto = "";
        GUIStyle estilo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            var go = new GameObject("Medidor de cuadros");
            DontDestroyOnLoad(go);
            go.AddComponent<MedidorDeCuadros>();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            acumulado += dt;
            cuadros++;
            peor = Mathf.Max(peor, dt);
            if (dt > 0.025f)
                lentos++;
            if (acumulado < 1f)
                return;

            texto = (cuadros / acumulado).ToString("0") + " fps | peor " + (peor * 1000f).ToString("0") + " ms | " +
                    lentos + " lentos | " + SystemInfo.graphicsDeviceType + " | " + Screen.width + "x" + Screen.height +
                    " | " + (System.GC.GetTotalMemory(false) / (1024 * 1024)) + " MB C#";
            acumulado = 0f;
            peor = 0f;
            cuadros = 0;
            lentos = 0;
        }

        void OnGUI()
        {
            if (estilo == null)
                estilo = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 40) };
            var r = new Rect(8, 6, Screen.width, estilo.fontSize * 1.6f);
            GUI.color = Color.black;
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), texto, estilo);
            GUI.color = Color.yellow;
            GUI.Label(r, texto, estilo);
        }
    }
}
#endif
