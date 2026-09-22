using UnityEditor;
using UnityEngine;

namespace Zombineta.Juego.EditorTools.Audio
{
    /// <summary>
    /// Ajustes de importacion por defecto para el audio del juego, aplicados solo la primera vez
    /// que Unity ve el archivo (importSettingsMissing): si alguien ya toco el importer a mano, no
    /// se lo pisa. Musica y Ambiente van en streaming Vorbis (loops largos, se decodifican sobre
    /// la marcha); los efectos cortos van descomprimidos en memoria (ADPCM) y sin cargar en
    /// segundo plano, para que salgan sin latencia.
    /// </summary>
    sealed class AudioImportRules : AssetPostprocessor
    {
        const float CalidadMusicaYAmbiente = 0.7f;

        void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            if (!importer.importSettingsMissing)
                return;

            string ruta = assetPath.Replace('\\', '/');

            if (ruta.Contains("/Audio/Musica/") || ruta.Contains("/Audio/Ambiente/"))
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = CalidadMusicaYAmbiente;
                importer.defaultSampleSettings = settings;
            }
            else if (ruta.Contains("/Audio/Sfx/"))
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = false;
            }
        }
    }
}
