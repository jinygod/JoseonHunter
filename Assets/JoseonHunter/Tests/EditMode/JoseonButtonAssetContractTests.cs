using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class JoseonButtonAssetContractTests
    {
        [TestCase("Assets/JoseonHunter/Resources/UI/Buttons/button_primary_frame.png", true)]
        [TestCase("Assets/JoseonHunter/Resources/UI/Buttons/button_secondary_frame.png", true)]
        [TestCase("Assets/JoseonHunter/Resources/UI/Buttons/icon_continue.png", false)]
        [TestCase("Assets/JoseonHunter/Resources/UI/Buttons/icon_lobby.png", false)]
        [TestCase("Assets/JoseonHunter/Resources/Lobby/icon_lock.png", false)]
        public void ButtonResourceUsesCrispSpriteImport(string assetPath, bool sliced)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Assert.That(sprite, Is.Not.Null, assetPath);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null, assetPath);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), assetPath);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), assetPath);
            Assert.That(importer.mipmapEnabled, Is.False, assetPath);
            Assert.That(importer.alphaIsTransparency, Is.True, assetPath);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), assetPath);

            if (sliced)
                Assert.That(sprite.border, Is.EqualTo(new Vector4(8f, 8f, 8f, 8f)), assetPath);
        }
    }
}
