using JoseonHunter.Editor.AssetProduction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponPixelAssetContractTests
    {
        private const string FixturePath = "Assets/JoseonHunter/Tests/EditMode/WeaponPixelAssetContractFixture.png";

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(FixturePath);

        [Test]
        public void ValidateRejectsAntiAliasedSourceAndActiveMaskOutsideSprite()
        {
            var source = Texture(2, new Color32(255, 255, 255, 128), new Color32(0, 0, 0, 0));
            var mask = Texture(2, new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255));
            File.WriteAllBytes(FixturePath, source.EncodeToPNG());
            AssetDatabase.ImportAsset(FixturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(FixturePath);
            importer.spritePixelsPerUnit = 32f; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point; importer.isReadable = true;
            importer.SaveAndReimport();
            var errors = WeaponPixelAssetContract.Validate(source, mask, importer);
            Assert.That(errors, Does.Contain("sprite alpha must be 0 or 255"));
            Assert.That(errors, Does.Contain("mask contains active pixel outside opaque sprite source"));
            Object.DestroyImmediate(source); Object.DestroyImmediate(mask);
        }

        [Test]
        public void DeriveMaskRemovesCheckedInExclusionPixels()
        {
            var source = Texture(2, new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255));
            var exclusion = Texture(2, new Color32(0, 0, 0, 0), new Color32(255, 255, 255, 255));
            var mask = WeaponPixelAssetImporter.DeriveMask(source, exclusion);
            Assert.That(mask.GetPixels32()[0].a, Is.EqualTo(255));
            Assert.That(mask.GetPixels32()[1].a, Is.EqualTo(0));
            Object.DestroyImmediate(source); Object.DestroyImmediate(exclusion); Object.DestroyImmediate(mask);
        }

        private static Texture2D Texture(int width, params Color32[] pixels)
        {
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels); texture.Apply();
            return texture;
        }
    }
}
