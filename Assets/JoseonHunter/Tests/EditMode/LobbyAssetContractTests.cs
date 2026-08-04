using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class LobbyAssetContractTests
    {
        [Test]
        public void LobbyBackgroundIsOpaquePortraitPixelArtWithPointFiltering()
        {
            const string path = "Assets/JoseonHunter/Art/UI/Lobby/lobby_courtyard.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(216));
            Assert.That(texture.height, Is.EqualTo(384));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.None));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.GetPlatformTextureSettings("Android").overridden, Is.False);
        }
    }
}
