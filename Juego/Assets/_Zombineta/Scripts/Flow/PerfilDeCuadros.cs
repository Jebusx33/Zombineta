#if ZOMBINETA_PERFIL
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Zombineta.Core;

namespace Zombineta.Flow
{
    /// <summary>
    /// Solo en el build de perfil de Android (define ZOMBINETA_PERFIL, build de desarrollo). Graba el profiler en la
    /// consola en archivos de CuadrosPorArchivo cuadros (perfil_000.raw, perfil_001.raw...) para bajarlos con adb
    /// y abrirlos en el editor, y anota en logcat cada cuadro lento con la escena, los metros recorridos y los
    /// tiempos de CPU y GPU. Cada diez segundos deja un resumen con la memoria.
    /// </summary>
    public sealed class PerfilDeCuadros : MonoBehaviour
    {
        const int CuadrosPorArchivo = 1200;
        const float Lento = 0.040f;

        int archivo, cuadroEnArchivo, gcAntes, cuadros, lentos;
        float acumulado, peor;
        readonly FrameTiming[] tiempos = new FrameTiming[1];
        RunController run;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Crear()
        {
            var go = new GameObject("Perfil de cuadros");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfilDeCuadros>();
        }

        void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Profiler.maxUsedMemory = 32 * 1024 * 1024;
            foreach (var viejo in Directory.GetFiles(Application.persistentDataPath, "perfil_*"))
                File.Delete(viejo);
            Abrir();
            SceneManager.sceneLoaded += (s, m) => Debug.Log("PERFIL escena " + s.name + " archivo " + (archivo - 1) + " cuadro " + cuadroEnArchivo);
        }

        void Abrir()
        {
            Profiler.enabled = false;
            Profiler.logFile = Path.Combine(Application.persistentDataPath, "perfil_" + archivo.ToString("000"));
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            archivo++;
            cuadroEnArchivo = 0;
        }

        void Update()
        {
            FrameTimingManager.CaptureFrameTimings();
            float dt = Time.unscaledDeltaTime;
            int gc = System.GC.CollectionCount(0);

            acumulado += dt;
            cuadros++;
            peor = Mathf.Max(peor, dt);
            if (dt > Lento)
            {
                lentos++;
                string tiempo = "";
                if (FrameTimingManager.GetLatestTimings(1, tiempos) > 0)
                    tiempo = " cpu " + tiempos[0].cpuMainThreadFrameTime.ToString("0") + " render " +
                             tiempos[0].cpuRenderThreadFrameTime.ToString("0") + " gpu " + tiempos[0].gpuFrameTime.ToString("0");
                if (run == null)
                    run = FindAnyObjectByType<RunController>();
                string metros = run != null && run.Sim != null ? " metros " + run.Sim.State.PlayerX.ToString("0") : "";
                Debug.Log("PERFIL lento " + (dt * 1000f).ToString("0") + " ms archivo " + (archivo - 1) + " cuadro " + cuadroEnArchivo +
                          " escena " + SceneManager.GetActiveScene().name + metros + tiempo + (gc != gcAntes ? " GC" : ""));
            }
            gcAntes = gc;

            if (acumulado >= 10f)
            {
                Debug.Log("PERFIL resumen " + (cuadros / acumulado).ToString("0.0") + " fps peor " + (peor * 1000f).ToString("0") +
                          " ms lentos " + lentos + " escena " + SceneManager.GetActiveScene().name +
                          " memoria " + (Profiler.GetTotalAllocatedMemoryLong() >> 20) + "/" + (Profiler.GetTotalReservedMemoryLong() >> 20) +
                          " MB graficos " + (Profiler.GetAllocatedMemoryForGraphicsDriver() >> 20) + " MB C# " + (Profiler.GetMonoUsedSizeLong() >> 20) + " MB");
                acumulado = peor = 0f;
                cuadros = lentos = 0;
            }

            if (++cuadroEnArchivo >= CuadrosPorArchivo)
                Abrir();
        }
    }
}
#endif
