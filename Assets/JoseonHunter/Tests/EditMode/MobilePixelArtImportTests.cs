using System.IO;
using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class MobilePixelArtImportTests
    {
        private const string FixtureRoot =
            "Assets/JoseonHunter/Art/StaticSprites/Runtime/Enemies/ImportTests/";
        private const string SingleAssetFixture = FixtureRoot + "single_asset.png";
        private const string MultiAssetFixture = FixtureRoot + "multi_asset.png";
        private const string CombatAnimationRoot =
            "Assets/JoseonHunter/Art/Animation/";

        [SetUp]
        public void SetUp()
        {
            CreateFixture(SingleAssetFixture, false);
            CreateFixture(MultiAssetFixture, true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(FixtureRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void RuntimePolishTextureUsesCrispMobileProfile()
        {
            var importer = AssetImporter.GetAtPath(SingleAssetFixture) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.GetPlatformTextureSettings("Android").overridden, Is.False);
        }

        [Test]
        public void WeaponPolishTextureRemainsReadableForPixelContactMasks()
        {
            var importer = AssetImporter.GetAtPath(
                "Assets/JoseonHunter/Art/Weapons/Runtime/Polish/Gakgung/gakgung_arrow.png")
                as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.isReadable, Is.True);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.GetPlatformTextureSettings("Android").overridden, Is.False);
        }

        [Test]
        public void ValidatorAcceptsOnePrincipalAssetWithTinyDetachedPixels()
        {
            Assert.That(SinglePngAssetValidator.Validate(SingleAssetFixture), Is.Empty);
        }

        [Test]
        public void ValidatorRejectsMultipleOpaqueIslandsMarkedAsIndependentAssets()
        {
            Assert.That(
                SinglePngAssetValidator.Validate(MultiAssetFixture),
                Does.Contain("multiple independent asset islands"));
        }

        [Test]
        public void ApprovedPolishBatchContainsOneRenderedAssetPerPng()
        {
            foreach (var root in new[]
            {
                "Assets/JoseonHunter/Art/StaticSprites/Runtime/Elites",
                "Assets/JoseonHunter/Art/Weapons/Runtime/Polish",
                "Assets/JoseonHunter/Art/World/Runtime/Battlefield"
            })
            {
                foreach (var path in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
                {
                    Assert.That(SinglePngAssetValidator.Validate(path), Is.Empty, path);
                }
            }

            Assert.That(
                SinglePngAssetValidator.Validate(
                    "Assets/JoseonHunter/Art/StaticSprites/Runtime/Heroes/han_yeonhwa.png"),
                Is.Empty);
        }

        [Test]
        public void CombatAnimationBatchContainsExpectedIndividualFrames()
        {
            var frames = Directory.GetFiles(CombatAnimationRoot, "*.png", SearchOption.AllDirectories);
            Assert.That(frames, Has.Length.EqualTo(48));
            Assert.That(frames, Has.All.Matches<string>(path =>
                Path.GetFileName(path).StartsWith("walk_") ||
                Path.GetFileName(path).StartsWith("idle_")));
        }

        [Test]
        public void CombatAnimationFramesUseCrispReadableProfile()
        {
            foreach (var path in Directory.GetFiles(CombatAnimationRoot, "*.png", SearchOption.AllDirectories))
            {
                var assetPath = path.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null, assetPath);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), assetPath);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), assetPath);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f), assetPath);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), assetPath);
                Assert.That(importer.mipmapEnabled, Is.False, assetPath);
                Assert.That(importer.isReadable, Is.True, assetPath);
                Assert.That(
                    importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed),
                    assetPath);
            }
        }

        private static void CreateFixture(string path, bool includeSecondAsset)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var clear = new Color32(0, 0, 0, 0);
            var solid = new Color32(220, 80, 50, 255);
            var pixels = new Color32[32 * 32];
            for (var index = 0; index < pixels.Length; index++) pixels[index] = clear;
            Fill(pixels, 32, 5, 7, 10, 14, solid);
            pixels[2 * 32 + 2] = solid;
            if (includeSecondAsset) Fill(pixels, 32, 22, 10, 7, 10, solid);
            texture.SetPixels32(pixels);
            texture.Apply();

            var absolutePath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void Fill(
            Color32[] pixels,
            int width,
            int left,
            int bottom,
            int fillWidth,
            int fillHeight,
            Color32 color)
        {
            for (var y = bottom; y < bottom + fillHeight; y++)
            for (var x = left; x < left + fillWidth; x++)
                pixels[y * width + x] = color;
        }
    }
}
