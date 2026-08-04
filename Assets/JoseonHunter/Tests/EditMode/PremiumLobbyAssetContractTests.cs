using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class PremiumLobbyAssetContractTests
    {
        private const string HeroPath =
            "Assets/JoseonHunter/Art/Characters/Lobby/han_yeonhwa_hero.png";
        private const string ResourceHeroPath =
            "Assets/JoseonHunter/Resources/Lobby/han_yeonhwa_hero.png";
        private const string FramePath =
            "Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_frame.png";
        private const string PrimaryButtonPath =
            "Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_primary_button.png";

        [Test]
        public void PremiumLobbyArtExistsAndIsMobileBounded()
        {
            foreach (var path in new[] { HeroPath, ResourceHeroPath, FramePath, PrimaryButtonPath })
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null, path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
            }
        }

        [Test]
        public void PremiumLobbyUiSpritesHaveSlicingBorders()
        {
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
            var button = AssetDatabase.LoadAssetAtPath<Sprite>(PrimaryButtonPath);
            Assert.That(frame, Is.Not.Null, FramePath);
            Assert.That(button, Is.Not.Null, PrimaryButtonPath);
            Assert.That(
                frame.border,
                Is.EqualTo(new Vector4(48f, 48f, 48f, 48f)));
            Assert.That(
                button.border,
                Is.EqualTo(new Vector4(32f, 32f, 32f, 32f)));
        }
    }
}
