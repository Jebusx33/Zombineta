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

        [MenuItem("Zombineta/Build/Android (consolas de 1 GB)")]
        static void DesdeElMenu()
        {
            if (!EditorUtility.DisplayDialog("Build de Android",
                    "Cambia la plataforma a Android (reimporta las texturas la primera vez) y genera el APK en " +
                    "Builds/Android.", "Generar", "Cancelar"))
                return;
            Build();
        }

        /// <summary>
        /// El APK para jugar: solo OpenGL ES 3, sin medidor. En la X55 corrio parejo con OpenGL (37-40 fps, sin
        /// tirones) y con Vulkan se sentian tirones.
        /// </summary>
        public static void Build() => Generar(true, false, "");

        /// <summary>
        /// Dos APK de prueba para comparar en la consola, los dos con el medidor de cuadros en pantalla
        /// (MedidorDeCuadros): uno con Vulkan (y OpenGL de respaldo) y otro solo con OpenGL ES 3.
        /// </summary>
        public static void BuildPrueba()
        {
            if (Generar(false, true, "-prueba-vulkan"))
                Generar(true, true, "-prueba-opengl");
        }

        /// <summary>Solo el de OpenGL con el medidor.</summary>
        public static void BuildPruebaOpenGL()
        {
            Generar(true, true, "-prueba-opengl");
            VolverAWindowsEnBatch();
        }

        /// <summary>Solo el de Vulkan (con OpenGL de respaldo) con el medidor.</summary>
        public static void BuildPruebaVulkan()
        {
            Generar(false, true, "-prueba-vulkan");
            VolverAWindowsEnBatch();
        }

        /// <summary>
        /// Build de desarrollo para buscar tirones: Vulkan (con OpenGL de respaldo), el medidor en pantalla y el
        /// profiler grabando en la consola (PerfilDeCuadros). Los archivos se bajan con adb desde
        /// /sdcard/Android/data/com.uchar.deadlivery/files.
        /// </summary>
        public static void BuildPerfil()
        {
            bool tiempos = PlayerSettings.enableFrameTimingStats;
            PlayerSettings.enableFrameTimingStats = true;
            try
            {
                Generar(false, new[] { "ZOMBINETA_MEDIDOR", "ZOMBINETA_PERFIL" }, "-perfil", BuildOptions.Development);
            }
            finally
            {
                PlayerSettings.enableFrameTimingStats = tiempos;
                AssetDatabase.SaveAssets();
            }
            VolverAWindowsEnBatch();
        }

        // En modo batch el proyecto queda en Windows al terminar, como lo usa el equipo: abrirlo despues
        // en Android obligaria a esperar otra reimportacion.
        static void VolverAWindowsEnBatch()
        {
            if (Application.isBatchMode && EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }

        static bool Generar(bool soloOpenGL, bool medidor, string sufijo) =>
            Generar(soloOpenGL, medidor ? new[] { "ZOMBINETA_MEDIDOR" } : null, sufijo, BuildOptions.None);

        static bool Generar(bool soloOpenGL, string[] defines, string sufijo, BuildOptions extra)
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
                options = extra,
                extraScriptingDefines = defines,
            };
            BuildReport reporte;
            using (new SinTelemetria())
                reporte = BuildPipeline.BuildPlayer(opciones);
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

            // ASTC y no ETC2: ETC2 solo comprime texturas con lados multiplos de 4 y las demas (62, entre ellas los
            // arboles, los edificios y el jefe) quedaban crudas, a 4 bytes por pixel. Con 1 GB de RAM la consola se
            // quedaba sin memoria en el nivel y se congelaba segundos enteros. La Mali-G52 de la X55 lee ASTC.
            PlayerSettings.Android.textureCompressionFormats = new[] { TextureCompressionFormat.ASTC };
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            ComprimirLoQueQuedaCrudo();
            // Sin tope de tamano: las texturas tienen que importarse igual que en PC. El escenario y otros datos
            // se miden en pixeles del dibujo (el avance de cada tile, la boca del arma): con un tope de 1024 el
            // asfalto (3552 px) quedaba chico y aparecian huecos oscuros en la calle. Ademas el tope queda
            // guardado en Library y tambien achicaba las texturas del editor.
            EditorUserBuildSettings.overrideMaxTextureSize = 0;
            EditorUserBuildSettings.buildAppBundle = false;
            AssetDatabase.SaveAssets();
        }

        // Debajo de esto (iconos y paneles de UI) no vale la pena: pesan poco y se ven mejor crudos.
        public const int PixelesMinimosParaComprimir = 512 * 512;

        /// <summary>
        /// Lo que en PC quedo sin comprimir (las hojas del jefe, el cielo, las luces de los edificios) en Android
        /// pesa 4 bytes por pixel. Les pone un ajuste solo para Android con ASTC 4x4 (1 byte por pixel, casi sin
        /// diferencia a la vista). Respeta los que ya tienen un ajuste de Android puesto a mano. PC no cambia.
        /// </summary>
        public static int ComprimirLoQueQuedaCrudo()
        {
            int cambiadas = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Zombineta/Art" }))
                {
                    if (!(AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is TextureImporter imp))
                        continue;
                    if (!DebeComprimirseEnAndroid(imp))
                        continue;
                    var pc = imp.GetDefaultPlatformTextureSettings();
                    var android = imp.GetPlatformTextureSettings("Android");
                    android.overridden = true;
                    android.maxTextureSize = pc.maxTextureSize;
                    android.format = TextureImporterFormat.ASTC_4x4;
                    android.textureCompression = TextureImporterCompression.CompressedHQ;
                    imp.SetPlatformTextureSettings(android);
                    imp.SaveAndReimport();
                    cambiadas++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            return cambiadas;
        }

        public static bool DebeComprimirseEnAndroid(TextureImporter imp)
        {
            if (imp.GetDefaultPlatformTextureSettings().textureCompression != TextureImporterCompression.Uncompressed)
                return false;
            if (imp.GetPlatformTextureSettings("Android").overridden)
                return false;
            imp.GetSourceTextureWidthAndHeight(out int ancho, out int alto);
            return ancho * alto >= PixelesMinimosParaComprimir;
        }

        /// <summary>
        /// Apaga los servicios de Unity (Engine Diagnostics y el envio de eventos) solo mientras se arma el APK y
        /// despues los deja como estaban. En la consola, sin internet, el envio trababa el juego hasta 1,5 s
        /// (EarlyUpdate.InsightsUpdate en el profiler). El build de PC no se toca.
        /// </summary>
        sealed class SinTelemetria : System.IDisposable
        {
            readonly bool diagnosticos;
            readonly SerializedObject conexion;
            readonly bool conectado;

            public SinTelemetria()
            {
                diagnosticos = UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled;
                UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled = false;
                var ajustes = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/UnityConnectSettings.asset");
                if (ajustes.Length > 0)
                {
                    conexion = new SerializedObject(ajustes[0]);
                    var activo = conexion.FindProperty("m_Enabled");
                    conectado = activo != null && activo.boolValue;
                    if (activo != null)
                    {
                        activo.boolValue = false;
                        conexion.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            public void Dispose()
            {
                UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled = diagnosticos;
                var activo = conexion?.FindProperty("m_Enabled");
                if (activo != null)
                {
                    activo.boolValue = conectado;
                    conexion.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }
}
