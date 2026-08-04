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
        private const string SecondaryButtonPath =
            "Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_secondary_button.png";
        private const string CompactWeaponSlotPath =
            "Assets/JoseonHunter/Art/UI/Combat/compact_weapon_slot.png";

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

        [Test]
        public void SecondaryButtonAndCompactSlotAreSlicedMobileSprites()
        {
            foreach (var path in new[] { SecondaryButtonPath, CompactWeaponSlotPath })
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(sprite.border.sqrMagnitude, Is.GreaterThan(0f), path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(1024), path);
            }
        }
    }
}
