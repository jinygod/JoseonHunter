using JoseonHunter.Editor.AssetProduction;
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
        private const string AffixJackpotUiRoot = "Assets/JoseonHunter/Art/UI/AffixJackpot/";
        private const string MicroSlotUiRoot = "Assets/JoseonHunter/Art/UI/AffixJackpot/MicroSlot/";
        private const string AppraisalUiRoot = "Assets/JoseonHunter/Art/UI/AffixJackpot/Appraisal/";
        private const string LobbyArtRoot = "Assets/JoseonHunter/Art/Characters/Lobby/";
        private const string CharacterRuntimeRoot = "Assets/JoseonHunter/Art/Characters/Runtime/";
        private const string FrontFacingCharacterRuntimeRoot =
            "Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/";
        private const string StaticSpriteRuntimeRoot =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/";
        private const string WeaponPolishRuntimeRoot =
            "Assets/JoseonHunter/Art/Weapons/Runtime/Polish/";
        private const string WeaponPotentialRuntimeRoot =
            "Assets/JoseonHunter/Art/Weapons/Runtime/Potentials/";
        private const string JangseungGeumjulArtRoot =
            "Assets/JoseonHunter/Art/VFX/JangseungGeumjul/";
        private const string WorldRuntimeRoot =
            "Assets/JoseonHunter/Art/World/Runtime/";
        private const string CombatAnimationRoot =
            "Assets/JoseonHunter/Art/Animation/";
        private const string EnemyRuntimeRoot =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/";
        private const string EliteRuntimeRoot =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Elites/";
        private const string BossRuntimeRoot =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Bosses/";
        private const string PickupRuntimeRoot =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Pickups/";
        private const string HanYeonhwaRuntimePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png";

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
            texture.spritePixelsPerUnit = assetPath.StartsWith(
                    WeaponPolishRuntimeRoot,
                    System.StringComparison.Ordinal)
                ? WeaponPixelAssetContract.PolishPixelsPerUnit
                : IsMobilePixelRuntime(assetPath) ? 64f : 32f;
            texture.alphaIsTransparency = true;
            if (IsSingleRuntimeSprite(assetPath))
            {
                texture.spriteImportMode = SpriteImportMode.Single;
                texture.isReadable = assetPath.StartsWith(
                    WeaponPolishRuntimeRoot,
                    System.StringComparison.Ordinal)
                    || assetPath.StartsWith(CombatAnimationRoot, System.StringComparison.Ordinal)
                    || assetPath.StartsWith(MicroSlotUiRoot, System.StringComparison.Ordinal)
                    || assetPath.StartsWith(AppraisalUiRoot, System.StringComparison.Ordinal);
                SetSingleSpritePivot(
                    texture,
                    assetPath.StartsWith(StaticSpriteRuntimeRoot, System.StringComparison.Ordinal)
                    || assetPath.StartsWith(CombatAnimationRoot, System.StringComparison.Ordinal)
                        ? new Vector2(0.5f, 0.125f)
                        : new Vector2(0.5f, 0.5f));
                texture.textureCompression = TextureImporterCompression.Uncompressed;
                texture.ClearPlatformTextureSettings("Android");
                return;
            }
            if (assetPath.StartsWith(CharacterRuntimeRoot, System.StringComparison.Ordinal) &&
                assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) &&
                IsCharacterSheet(assetPath))
            {
                texture.spriteImportMode = SpriteImportMode.Multiple;
                var isFrontFacing = assetPath.StartsWith(FrontFacingCharacterRuntimeRoot, System.StringComparison.Ordinal);
                texture.spritesheet = CharacterSprites(
                    System.IO.Path.GetFileNameWithoutExtension(assetPath),
                    isFrontFacing ? 12 : 38,
                    isFrontFacing ? 4 : 6);
            }
            else
            {
                texture.spriteImportMode = SpriteImportMode.Single;
            }
            if (!assetPath.StartsWith(JangseungGeumjulArtRoot, System.StringComparison.Ordinal) &&
                !assetPath.StartsWith(AffixJackpotUiRoot, System.StringComparison.Ordinal) &&
                !assetPath.StartsWith(WeaponPotentialRuntimeRoot, System.StringComparison.Ordinal))
            {
                texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    format = TextureImporterFormat.ASTC_6x6,
                });
            }
        }

        private static bool IsMobilePixelRuntime(string path)
        {
            return path.Equals(HanYeonhwaRuntimePath, System.StringComparison.Ordinal)
                || path.StartsWith(EnemyRuntimeRoot, System.StringComparison.Ordinal)
                || path.StartsWith(EliteRuntimeRoot, System.StringComparison.Ordinal)
                || path.StartsWith(BossRuntimeRoot, System.StringComparison.Ordinal)
                || path.StartsWith(PickupRuntimeRoot, System.StringComparison.Ordinal)
                || path.StartsWith(WeaponPolishRuntimeRoot, System.StringComparison.Ordinal)
                || path.StartsWith(WorldRuntimeRoot, System.StringComparison.Ordinal)
                || path.StartsWith(CombatAnimationRoot, System.StringComparison.Ordinal)
                || path.StartsWith(MicroSlotUiRoot, System.StringComparison.Ordinal)
                || path.StartsWith(AppraisalUiRoot, System.StringComparison.Ordinal);
        }

        private static bool IsSingleRuntimeSprite(string path)
        {
            return path.StartsWith(StaticSpriteRuntimeRoot, System.StringComparison.Ordinal)
                || path.StartsWith(WeaponPolishRuntimeRoot, System.StringComparison.Ordinal)
                || path.StartsWith(WorldRuntimeRoot, System.StringComparison.Ordinal)
                || path.StartsWith(CombatAnimationRoot, System.StringComparison.Ordinal)
                || path.StartsWith(MicroSlotUiRoot, System.StringComparison.Ordinal)
                || path.StartsWith(AppraisalUiRoot, System.StringComparison.Ordinal);
        }

        private static SpriteMetaData[] CharacterSprites(string characterId, int frameCount, int columns)
        {
            var sprites = new SpriteMetaData[frameCount];
            for (var frame = 0; frame < sprites.Length; frame++)
            {
                sprites[frame] = new SpriteMetaData
                {
                    name = characterId + "_" + frame.ToString("D2"),
                    rect = new Rect((frame % columns) * 64, (frame / columns) * 64, 64, 64),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.125f)
                };
            }
            return sprites;
        }

        private static void SetSingleSpritePivot(TextureImporter texture, Vector2 pivot)
        {
            var settings = new TextureImporterSettings();
            texture.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            texture.SetTextureSettings(settings);
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
                && !path.StartsWith(AffixJackpotUiRoot, System.StringComparison.Ordinal)
                || path.StartsWith(LobbyArtRoot, System.StringComparison.Ordinal);
        }
    }
}
