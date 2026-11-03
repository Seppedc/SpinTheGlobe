using UnityEditor;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>Music tracks (any audio under an "/Audio/Music/" folder) stream from disk instead of being
    /// decompressed into memory, and are never mono.</summary>
    public sealed class MusicImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Audio/Music/")) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = false;
            importer.loadInBackground = true;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            importer.defaultSampleSettings = s;
        }
    }
}
