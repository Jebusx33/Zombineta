using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Zombineta.Juego.EditorTools
{
    /// <summary>
    /// Build de Android pensado para consolas chicas como la Powkiddy X55 (RK3566, Mali-G52, 1 GB de RAM,
    /// pantalla de 1280x720 y controles de gamepad). Solo toca los ajustes de Android, asi que el build de
    /// Windows queda igual. Se puede correr desde el menu o en modo batch con
    /// -executeMethod Zombineta.Juego.EditorTools.BuildAndroid.Build.
    /// </summary>
    public static class BuildAndroid
    {
        const string Identificador = "com.uchar.deadlivery";
        const string Salida = "../Builds/Android";

        // Con 1 GB de RAM entre todo el sistema, ninguna textura pasa de este lado. A 720 de alto alcanza:
        // los dibujos mas grandes (las hojas del jefe y los fondos) igual se ven a menos de eso en pantalla.
        const int TexturaMaxima = 1024;

        [MenuItem("Zombineta/Build/Android (consolas de 1 GB)")]
        static void DesdeElMenu()
        {
            if (!EditorUtility.DisplayDialog("Build de Android",
                    "Cambia la plataforma a Android (reimporta las texturas la primera vez) y genera el APK en " +
                    "Builds/Android.", "Generar", "Cancelar"))
                return;
            Build();
        }

        /// <summary>El APK para jugar: Vulkan con OpenGL ES 3 de respaldo, sin medidor.</summary>
        public static void Build() => Generar(false, false, "");

        /// <summary>
        /// Dos APK de prueba para comparar en la consola, los dos con el medidor de cuadros en pantalla
        /// (MedidorDeCuadros): uno con Vulkan (y OpenGL de respaldo) y otro solo con OpenGL ES 3.
        /// </summary>
        public static void BuildPrueba()
        {
            if (Generar(false, true, "-prueba-vulkan"))
                Generar(true, true, "-prueba-opengl");
        }

        static bool Generar(bool soloOpenGL, bool medidor, string sufijo)
        {
            Configurar(soloOpenGL);

            var escenas = EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
            string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", Salida));
            Directory.CreateDirectory(carpeta);
            string apk = Path.Combine(carpeta, "DeadLivery-" + PlayerSettings.bundleVersion + sufijo + ".apk");

            var opciones = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
                extraScriptingDefines = medidor ? new[] { "ZOMBINETA_MEDIDOR" } : null,
            };
            var reporte = BuildPipeline.BuildPlayer(opciones);
            var r = reporte.summary;
            string resumen = "Build de Android: " + r.result + ", " + (r.totalSize / (1024f * 1024f)).ToString("0.0") +
                             " MB, " + r.totalTime.TotalMinutes.ToString("0.0") + " min, " + r.totalErrors + " errores -> " + apk;
            Debug.Log(resumen);
            bool bien = r.result == BuildResult.Succeeded;
            if (Application.isBatchMode && !bien)
                EditorApplication.Exit(1);
            return bien;
        }

        /// <summary>Los ajustes de Android. Quedan guardados en el proyecto; los de Windows no se tocan.</summary>
        public static void Configurar(bool soloOpenGL = false)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, Identificador);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            // La X55 tiene Android de 64 bits; ARMv7 queda por si otra consola de las baratas trae 32.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            // Vulkan primero y OpenGL ES 3 de respaldo: Unity usa la primera que el aparato soporte. Si
            // Vulkan arranca pero se ve mal (drivers viejos), sacarlo de esta lista deja solo OpenGL.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, soloOpenGL
                ? new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 }
                : new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan, UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            // Siempre acostada: el sensor de algunas consolas da vuelta la imagen si se rota sola.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;

            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
            EditorUserBuildSettings.overrideMaxTextureSize = TexturaMaxima;
            EditorUserBuildSettings.buildAppBundle = false;
            AssetDatabase.SaveAssets();
        }
    }
}
