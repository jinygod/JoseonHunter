using System.IO;
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

        [SetUp]
        public void SetUp()
        {
            CreateMonoWaveFixture(MusicFixturePath);
            CreateMonoWaveFixture(SfxFixturePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(MusicFixturePath);
            AssetDatabase.DeleteAsset(SfxFixturePath);
            DeleteEmptyDirectory("Assets/JoseonHunter/Audio/Music");
            DeleteEmptyDirectory("Assets/JoseonHunter/Audio/SFX");
            DeleteEmptyDirectory("Assets/JoseonHunter/Audio");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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
            Assert.That(texture.spritesheet, Has.Length.EqualTo(38));
            foreach (var sprite in texture.spritesheet)
            {
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(64f, 64f)));
                Assert.That(sprite.alignment, Is.EqualTo((int)SpriteAlignment.Custom));
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(0.5f, 0.125f)));
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
