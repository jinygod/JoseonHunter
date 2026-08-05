using System.IO;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class AssetImportProfileTests
    {
        private const string MusicFixturePath =
            "Assets/JoseonHunter/Audio/Music/import-profile-test-music.wav";
        private const string SfxFixturePath =
            "Assets/JoseonHunter/Audio/SFX/import-profile-test-sfx.wav";
        private const string FrontFacingFixturePath =
            "Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/import_profile_test.png";
        private const string StaticSpriteFixturePath =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/import_profile_test.png";
        private const string JoseonFolkFieldTilePath =
            "Assets/JoseonHunter/Art/World/Runtime/Battlefield/joseon_folk_field_tile.png";

        [SetUp]
        public void SetUp()
        {
            CreateMonoWaveFixture(MusicFixturePath);
            CreateMonoWaveFixture(SfxFixturePath);
            CreateTextureFixture(FrontFacingFixturePath, 256, 192);
            CreateTextureFixture(StaticSpriteFixturePath, 64, 64);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(MusicFixturePath);
            AssetDatabase.DeleteAsset(SfxFixturePath);
            AssetDatabase.DeleteAsset(FrontFacingFixturePath);
            AssetDatabase.DeleteAsset(StaticSpriteFixturePath);
            DeleteEmptyDirectory("Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes");
            DeleteEmptyDirectory("Assets/JoseonHunter/Art/StaticSprites/Runtime");
            DeleteEmptyDirectory("Assets/JoseonHunter/Art/StaticSprites");
            DeleteEmptyDirectory("Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing");
            DeleteEmptyDirectory("Assets/JoseonHunter/Audio/Music");
            DeleteEmptyDirectory("Assets/JoseonHunter/Audio/SFX");
            DeleteEmptyDirectory("Assets/JoseonHunter/Audio");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void StaticSpriteRuntimeUsesSingleCustomPivotImportProfile()
        {
            AssetDatabase.ImportAsset(StaticSpriteFixturePath, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetImporter.GetAtPath(StaticSpriteFixturePath) as TextureImporter;
            Assert.That(texture.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            var settings = new TextureImporterSettings();
            texture.ReadTextureSettings(settings);
            Assert.That(settings.spriteAlignment, Is.EqualTo((int)SpriteAlignment.Custom));
            Assert.That(settings.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.125f)));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.mipmapEnabled, Is.False);
            Assert.That(texture.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(texture.spritePixelsPerUnit, Is.EqualTo(32f));
            Assert.That(texture.GetPlatformTextureSettings("Android").overridden, Is.False);
        }

        [Test]
        public void StaticSpriteRuntimeClearsExistingAndroidOverride()
        {
            var texture = AssetImporter.GetAtPath(StaticSpriteFixturePath) as TextureImporter;
            texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, format = TextureImporterFormat.ASTC_6x6 });
            texture.SaveAndReimport();
            Assert.That(texture.GetPlatformTextureSettings("Android").overridden, Is.False);
        }

        [Test]
        public void FrontFacingRuntimeUsesTwelveCustomPivotSlices()
        {
            AssetDatabase.ImportAsset(FrontFacingFixturePath, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetImporter.GetAtPath(FrontFacingFixturePath) as TextureImporter;

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.mipmapEnabled, Is.False);
            Assert.That(texture.spritePixelsPerUnit, Is.EqualTo(32f));
            Assert.That(texture.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            var sprites = SpriteSheetMetadata.Read(texture);
            Assert.That(sprites, Has.Length.EqualTo(12));
            for (var frame = 0; frame < sprites.Length; frame++)
            {
                var sprite = sprites[frame];
                Assert.That(sprite.Name, Is.EqualTo("import_profile_test_" + frame.ToString("D2")));
                Assert.That(sprite.Rect, Is.EqualTo(new Rect((frame % 4) * 64, (frame / 4) * 64, 64, 64)));
                Assert.That(sprite.Alignment, Is.EqualTo(SpriteAlignment.Custom));
                Assert.That(sprite.Pivot, Is.EqualTo(new Vector2(0.5f, 0.125f)));
            }
        }

        [Test]
        public void PixelSpriteUsesGameplayImportProfile()
        {
            var texture = AssetImporter.GetAtPath(
                "Assets/JoseonHunter/Art/Characters/rookie_constable_player_32.png") as TextureImporter;

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.mipmapEnabled, Is.False);
            Assert.That(texture.spritePixelsPerUnit, Is.EqualTo(32f));

            var android = texture.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_6x6));
        }

        [Test]
        public void JoseonFolkFieldUsesPixelImportProfile()
        {
            var texture = AssetImporter.GetAtPath(JoseonFolkFieldTilePath) as TextureImporter;

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(texture.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.mipmapEnabled, Is.False);
            Assert.That(texture.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(texture.GetPlatformTextureSettings("Android").overridden, Is.False);
        }

        [Test]
        public void AffixSlotPartsUseReadableUncompressedPixelImportProfile()
        {
            var reelFrame = AssetImporter.GetAtPath(
                "Assets/JoseonHunter/Art/UI/AffixJackpot/SlotParts/reel_frame.png") as TextureImporter;
            Assert.That(reelFrame.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(reelFrame.GetPlatformTextureSettings("Android").overridden, Is.False);

            foreach (var name in new[] { "reel_frame", "empty_line_frame", "jackpot_burst_1", "jackpot_burst_2", "jackpot_burst_3" })
            {
                var texture = AssetImporter.GetAtPath("Assets/JoseonHunter/Art/UI/AffixJackpot/SlotParts/" + name + ".png") as TextureImporter;
                Assert.That(texture, Is.Not.Null, name);
                Assert.That(texture.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), name);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point), name);
                Assert.That(texture.mipmapEnabled, Is.False, name);
                Assert.That(texture.isReadable, Is.True, name);
                Assert.That(texture.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), name);
                Assert.That(texture.alphaIsTransparency, Is.True, name);
                Assert.That(texture.GetPlatformTextureSettings("Android").overridden, Is.False, name);
            }
        }

        [Test]
        public void MannequinRuntimeUsesThirtyEightCustomPivotSlices()
        {
            AssetDatabase.ImportAsset(
                "Assets/JoseonHunter/Art/Characters/Runtime/mannequin.png",
                ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetImporter.GetAtPath(
                "Assets/JoseonHunter/Art/Characters/Runtime/mannequin.png") as TextureImporter;

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.mipmapEnabled, Is.False);
            Assert.That(texture.spritePixelsPerUnit, Is.EqualTo(32f));
            Assert.That(texture.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            var sprites = SpriteSheetMetadata.Read(texture);
            Assert.That(sprites, Has.Length.EqualTo(38));
            foreach (var sprite in sprites)
            {
                Assert.That(sprite.Rect.size, Is.EqualTo(new Vector2(64f, 64f)));
                Assert.That(sprite.Alignment, Is.EqualTo(SpriteAlignment.Custom));
                Assert.That(sprite.Pivot, Is.EqualTo(new Vector2(0.5f, 0.125f)));
            }
        }

        [TestCase("mannequin")]
        public void LegacyCharacterRuntimeUsesThirtyEightCustomPivotSlices(string characterId)
        {
            var path = "Assets/JoseonHunter/Art/Characters/Runtime/" + characterId + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            var sprites = SpriteSheetMetadata.Read(texture);
            Assert.That(sprites, Has.Length.EqualTo(38));
            foreach (var sprite in sprites)
            {
                Assert.That(sprite.Name, Does.StartWith(characterId + "_"));
                Assert.That(sprite.Rect.size, Is.EqualTo(new Vector2(64f, 64f)));
                Assert.That(sprite.Pivot, Is.EqualTo(new Vector2(0.5f, 0.125f)));
            }
        }

        [Test]
        public void LobbyUiSpriteUsesBilinearImportProfile()
        {
            var texture = AssetImporter.GetAtPath(
                "Assets/JoseonHunter/Art/Characters/Lobby/rookie_constable.png") as TextureImporter;

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(texture.mipmapEnabled, Is.False);
            Assert.That(texture.spritePixelsPerUnit, Is.EqualTo(32f));
        }

        [Test]
        public void MusicClipStreamsWhilePreservingMonoAndSampleRate()
        {
            var music = AssetImporter.GetAtPath(MusicFixturePath) as AudioImporter;

            Assert.That(music, Is.Not.Null);
            Assert.That(music.defaultSampleSettings.loadType,
                Is.EqualTo(AudioClipLoadType.Streaming));
            Assert.That(music.defaultSampleSettings.compressionFormat,
                Is.EqualTo(AudioCompressionFormat.Vorbis));
            Assert.That(music.forceToMono, Is.False);
            Assert.That(music.defaultSampleSettings.sampleRateSetting,
                Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate));
        }

        [Test]
        public void SfxClipDecompressesOnLoadWhilePreservingMonoAndSampleRate()
        {
            var sfx = AssetImporter.GetAtPath(SfxFixturePath) as AudioImporter;

            Assert.That(sfx, Is.Not.Null);
            Assert.That(sfx.defaultSampleSettings.loadType,
                Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
            Assert.That(sfx.defaultSampleSettings.compressionFormat,
                Is.EqualTo(AudioCompressionFormat.Vorbis));
            Assert.That(sfx.forceToMono, Is.False);
            Assert.That(sfx.defaultSampleSettings.sampleRateSetting,
                Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate));
        }

        private static void CreateMonoWaveFixture(string assetPath)
        {
            var absolutePath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

            using (var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                const int sampleRate = 8000;
                const short channelCount = 1;
                const short bitsPerSample = 16;
                const int sampleCount = 8;
                const int dataLength = sampleCount * (bitsPerSample / 8);
                const int byteRate = sampleRate * channelCount * (bitsPerSample / 8);

                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataLength);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(channelCount);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)(channelCount * (bitsPerSample / 8)));
                writer.Write(bitsPerSample);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataLength);

                for (var i = 0; i < sampleCount; i++)
                {
                    writer.Write((short)0);
                }
            }
        }

        private static void CreateTextureFixture(string assetPath, int width, int height)
        {
            var absolutePath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void DeleteEmptyDirectory(string assetPath)
        {
            var absolutePath = Path.GetFullPath(assetPath);
            if (Directory.Exists(absolutePath) && Directory.GetFileSystemEntries(absolutePath).Length == 0)
            {
                Directory.Delete(absolutePath);
            }

            var metaPath = absolutePath + ".meta";
            if (File.Exists(metaPath) && !Directory.Exists(absolutePath))
            {
                File.Delete(metaPath);
            }
        }
    }
}
