using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetImport
{
    public sealed class JoseonAssetPostprocessor : AssetPostprocessor
    {
        private const string PixelRoot = "Assets/JoseonHunter/Art/";
        private const string MusicRoot = "Assets/JoseonHunter/Audio/Music/";
        private const string SfxRoot = "Assets/JoseonHunter/Audio/SFX/";
        private const string UiAudioRoot = "Assets/JoseonHunter/Audio/UI/";
        private const string UiArtRoot = "Assets/JoseonHunter/Art/UI/";
        private const string LobbyArtRoot = "Assets/JoseonHunter/Art/Characters/Lobby/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(PixelRoot, System.StringComparison.Ordinal))
            {
                return;
            }

            var texture = (TextureImporter)assetImporter;
            texture.textureType = TextureImporterType.Sprite;
            texture.filterMode = IsBilinearArt(assetPath) ? FilterMode.Bilinear : FilterMode.Point;
            texture.mipmapEnabled = false;
            texture.spritePixelsPerUnit = 32f;
            texture.alphaIsTransparency = true;
            texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                format = TextureImporterFormat.ASTC_6x6,
            });
        }

        private void OnPreprocessAudio()
        {
            var isMusic = assetPath.StartsWith(MusicRoot, System.StringComparison.Ordinal);
            var isSfx = assetPath.StartsWith(SfxRoot, System.StringComparison.Ordinal)
                || assetPath.StartsWith(UiAudioRoot, System.StringComparison.Ordinal);
            if (!isMusic && !isSfx)
            {
                return;
            }

            var audio = (AudioImporter)assetImporter;
            var settings = audio.defaultSampleSettings;
            settings.loadType = isMusic
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            audio.defaultSampleSettings = settings;
            audio.forceToMono = false;
        }

        private static bool IsBilinearArt(string path)
        {
            return path.StartsWith(UiArtRoot, System.StringComparison.Ordinal)
                || path.StartsWith(LobbyArtRoot, System.StringComparison.Ordinal);
        }
    }
}
