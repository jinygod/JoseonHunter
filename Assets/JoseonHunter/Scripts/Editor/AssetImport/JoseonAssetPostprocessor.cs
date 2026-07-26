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
        private const string CharacterRuntimeRoot = "Assets/JoseonHunter/Art/Characters/Runtime/";

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
            if (assetPath.StartsWith(CharacterRuntimeRoot, System.StringComparison.Ordinal) &&
                assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) &&
                IsCharacterSheet(assetPath))
            {
                texture.spriteImportMode = SpriteImportMode.Multiple;
                texture.spritesheet = CharacterSprites(System.IO.Path.GetFileNameWithoutExtension(assetPath));
            }
            else
            {
                texture.spriteImportMode = SpriteImportMode.Single;
            }
            texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                format = TextureImporterFormat.ASTC_6x6,
            });
        }

        private static SpriteMetaData[] CharacterSprites(string characterId)
        {
            var sprites = new SpriteMetaData[38];
            for (var frame = 0; frame < sprites.Length; frame++)
            {
                sprites[frame] = new SpriteMetaData
                {
                    name = characterId + "_" + frame.ToString("D2"),
                    rect = new Rect((frame % 6) * 64, (frame / 6) * 64, 64, 64),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.125f)
                };
            }
            return sprites;
        }

        private static bool IsCharacterSheet(string path)
        {
            var id = System.IO.Path.GetFileNameWithoutExtension(path);
            return id.IndexOf("portrait", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                id.IndexOf("locked", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                id.IndexOf("palette", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                id.IndexOf("cosmetic", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                id.IndexOf("source_layers", System.StringComparison.OrdinalIgnoreCase) < 0;
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
