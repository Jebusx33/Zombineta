using System;
using System.IO;
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Telemetria
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Anota cada partida de prueba en un CSV (ver SesionTelemetria): un evento por fila y un
    /// resumen al final. Se crea sola al darle Play (no hay que ponerla en ninguna escena), sigue
    /// a cualquier RunController que aparezca (nivel nuevo, reintento) y solo existe en el editor y
    /// en builds de desarrollo.
    ///
    /// Los archivos quedan en Juego/Playtests/ (editor) o en la carpeta de datos del juego
    /// (build). Cada fila se escribe en el momento: si el editor se cuelga, el archivo conserva
    /// todo hasta el ultimo evento.
    /// </summary>
    public sealed class TelemetriaDePlaytest : MonoBehaviour
    {
        const float SegundosEntreBusquedas = 0.5f;

        static TelemetriaDePlaytest instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarStatic() => instancia = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null)
                return;
            var go = new GameObject("TelemetriaDePlaytest");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<TelemetriaDePlaytest>();
        }

        string ruta;
        bool escribible = true;
        RunController run;
        SesionTelemetria sesion;
        int intento;
        bool cerrada;
        float proximaBusqueda;

        void Awake()
        {
            try
            {
#if UNITY_EDITOR
                string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Playtests"));
#else
                string carpeta = Path.Combine(Application.persistentDataPath, "Playtests");
#endif
                Directory.CreateDirectory(carpeta);
                ruta = Path.Combine(carpeta, SesionTelemetria.NombreDeArchivo(DateTime.Now));
                File.WriteAllText(ruta, SesionTelemetria.Cabecera + "\n");
                Debug.Log("[Playtest] anotando la partida en " + ruta);
            }
            catch (Exception e)
            {
                escribible = false;
                Debug.LogWarning("[Playtest] no se pudo crear el archivo, no se anota nada: " + e.Message);
            }
        }

        void Update()
        {
            if (!escribible)
                return;

            if (run == null && Time.unscaledTime >= proximaBusqueda)
            {
                proximaBusqueda = Time.unscaledTime + SegundosEntreBusquedas;
                var nuevo = FindAnyObjectByType<RunController>();
                if (nuevo != null)
                    Enlazar(nuevo);
            }

            if (run == null || run.Sim == null || sesion == null)
                return;

            var state = run.Sim.State;
            if (!run.Paused)
                sesion.Tiempo(state, Time.unscaledDeltaTime);

            if (!cerrada && (state.Phase == RunPhase.Won || state.Phase == RunPhase.Lost))
                Cerrar(state.Phase == RunPhase.Won ? "ganada" : "perdida");
        }

        void Enlazar(RunController nuevo)
        {
            Desenlazar();
            run = nuevo;
            run.Stepped += AlPasar;
            run.Restarted += AlReiniciar;
            Empezar();
        }

        void Desenlazar()
        {
            if (run == null)
                return;
            run.Stepped -= AlPasar;
            run.Restarted -= AlReiniciar;
            run = null;
        }

        void Empezar()
        {
            intento++;
            cerrada = false;
            sesion = new SesionTelemetria(run.gameObject.scene.name, intento);
            if (run.Sim != null)
                Escribir(sesion.Inicio(Time.realtimeSinceStartup, run.Sim.State));
        }

        void AlReiniciar()
        {
            if (!cerrada && sesion != null && run != null && run.Sim != null)
                Cerrar("reiniciada");
            Empezar();
        }

        void AlPasar(RunEvent ev)
        {
            if (sesion == null || run == null || run.Sim == null)
                return;
            foreach (var fila in sesion.Eventos(Time.realtimeSinceStartup, run.Sim.State, ev))
                Escribir(fila);
        }

        void Cerrar(string resultado)
        {
            cerrada = true;
            if (sesion != null && run != null && run.Sim != null)
                Escribir(sesion.Resumen(Time.realtimeSinceStartup, run.Sim.State, resultado));
        }

        void Escribir(string fila)
        {
            if (!escribible || ruta == null)
                return;
            try
            {
                File.AppendAllText(ruta, fila + "\n");
            }
            catch (Exception e)
            {
                escribible = false;
                Debug.LogWarning("[Playtest] dejo de anotar: " + e.Message);
            }
        }

        void OnApplicationQuit()
        {
            // Salir a mitad de partida tambien deja su resumen.
            if (!cerrada && sesion != null && run != null && run.Sim != null)
                Cerrar("abandonada");
        }

        void OnDestroy()
        {
            Desenlazar();
            if (instancia == this)
                instancia = null;
        }
    }
#endif
}
