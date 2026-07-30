using JoseonHunter.Editor.AssetProduction;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class JangseungGeumjulAssetTests
    {
        [Test]
        public void VisualLibraryContainsReadablePointFilteredAssets()
        {
            var library = AssetDatabase.LoadAssetAtPath<JangseungGeumjulVisualLibrary>(
                "Assets/JoseonHunter/Content/Presentation/JangseungGeumjulVisualLibrary.asset");
            Assert.That(library, Is.Not.Null);
            Assert.That(library.GeumjulRopeTexture, Is.Not.Null);
            Assert.That(library.GeumjulAnchor, Is.Not.Null);
            Assert.That(library.GeumjulKnotVariants.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(library.GeumjulClosureFrames.Length, Is.EqualTo(6));
            Assert.That(library.JangseungDustFrames.Length, Is.EqualTo(4));
            Assert.That(library.JangseungCrossingFrames.Length, Is.EqualTo(4));

            var path = AssetDatabase.GetAssetPath(library.GeumjulRopeTexture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(path, Does.StartWith(JangseungGeumjulAssetImporter.ArtRoot));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            AssertAndroidImportIsUncompressed(importer, path);

            AssertSpriteUsesCanonicalPath(library.GeumjulAnchor);
            AssertSpriteCollectionUsesCanonicalPaths(library.GeumjulKnotVariants);
            AssertSpriteCollectionUsesCanonicalPaths(library.GeumjulClosureFrames);
            AssertSpriteCollectionUsesCanonicalPaths(library.JangseungDustFrames);
            AssertSpriteCollectionUsesCanonicalPaths(library.JangseungCrossingFrames);
        }

        private static void AssertSpriteCollectionUsesCanonicalPaths(Sprite[] sprites)
        {
            foreach (var sprite in sprites) AssertSpriteUsesCanonicalPath(sprite);
        }

        private static void AssertSpriteUsesCanonicalPath(Sprite sprite)
        {
            Assert.That(sprite, Is.Not.Null);
            var path = AssetDatabase.GetAssetPath(sprite);
            Assert.That(path, Does.StartWith(JangseungGeumjulAssetImporter.ArtRoot));
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            AssertAndroidImportIsUncompressed(importer, path);
        }

        private static void AssertAndroidImportIsUncompressed(TextureImporter importer, string path)
        {
            var androidSettings = importer.GetPlatformTextureSettings("Android");
            Assert.That(androidSettings.overridden, Is.True, path);
            Assert.That(
                androidSettings.format,
                Is.EqualTo(TextureImporterFormat.RGBA32),
                path);
            Assert.That(
                androidSettings.crunchedCompression,
                Is.False,
                path);
        }
    }
}
