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
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
        }
    }
}
