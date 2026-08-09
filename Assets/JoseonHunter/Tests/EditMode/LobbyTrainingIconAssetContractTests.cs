using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class LobbyTrainingIconAssetContractTests
    {
        private const string TrainingIconRoot =
            "Assets/JoseonHunter/Art/UI/Lobby/Training/";

        [TestCase("training_vitality.png")]
        [TestCase("training_power.png")]
        [TestCase("training_footwork.png")]
        [TestCase("training_learning.png")]
        [TestCase("training_guard.png")]
        [TestCase("training_resonance.png")]
        public void TrainingIconIsTransparentPointFilteredSprite(string fileName)
        {
            var path = TrainingIconRoot + fileName;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(32), path);
            Assert.That(texture.height, Is.EqualTo(32), path);
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.alphaSource,
                Is.EqualTo(TextureImporterAlphaSource.FromInput), path);
            Assert.That(importer.alphaIsTransparency, Is.True, path);
            Assert.That(importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed), path);

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(source, File.ReadAllBytes(path)), Is.True, path);
                var pixels = source.GetPixels32();
                Assert.That(pixels.Any(pixel => pixel.a == 0), Is.True,
                    $"{path} must keep a transparent background.");
                Assert.That(pixels.Any(pixel => pixel.a > 0), Is.True,
                    $"{path} must contain a visible icon.");
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        [TestCase("Assets/JoseonHunter/Art/UI/Lobby/lobby_courtyard.png")]
        [TestCase("Assets/JoseonHunter/Art/UI/Lobby/settings_gear.png")]
        public void OpaqueLobbyArtKeepsItsExistingAlphaImportContract(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.None), path);
            Assert.That(importer.alphaIsTransparency, Is.False, path);
        }
    }
}
