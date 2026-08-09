using System.IO;
using System.Linq;
using JoseonHunter.Presentation.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class PremiumLobbyAssetContractTests
    {
        private const string ResourceHeroPath =
            "Assets/JoseonHunter/Resources/Lobby/han_yeonhwa_hero.png";
        private const string FramePath =
            "Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_frame.png";
        private const string PrimaryButtonPath =
            "Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_primary_button.png";
        private const string SecondaryButtonPath =
            "Assets/JoseonHunter/Art/UI/Lobby/premium_lobby_secondary_button.png";
        private const string CompactWeaponSlotResourcePath =
            "Assets/JoseonHunter/Resources/UI/compact_weapon_slot.png";
        private const string LockSlashConstraintPath =
            "Assets/JoseonHunter/Scripts/Presentation/UI/LockSlashConstraint.cs";

        [Test]
        public void LockSlashConstraintIsAStandaloneSerializableMonoScript()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(LockSlashConstraintPath);

            Assert.That(script, Is.Not.Null, LockSlashConstraintPath);
            Assert.That(script.GetClass(), Is.Not.Null, "The lock slash component must be serializable on LobbyShell.prefab.");
            Assert.That(script.GetClass().FullName, Is.EqualTo("JoseonHunter.Presentation.UI.LockSlashConstraint"));
        }

        [TestCase(720f, 1280f)]
        [TestCase(1080f, 2340f)]
        public void LockedDifficultyDecorationStaysInsidePortraitCard(float width, float height)
        {
            var root = new GameObject("Portrait Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvasRect = root.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(width, height);

                var cardObject = new GameObject(
                    "Difficulty Card",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                cardObject.transform.SetParent(root.transform, false);
                var cardRect = cardObject.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(width - 64f, 112f);
                var button = cardObject.GetComponent<Button>();
                button.targetGraphic = cardObject.GetComponent<Image>();

                PremiumPixelUiSkin.ApplyDifficulty(button, selected: false, locked: true);

                AssertRectInside(cardObject.transform.Find("Lock Slash").GetComponent<RectTransform>(), cardRect);
                AssertRectInside(cardObject.transform.Find("Lock Icon").GetComponent<RectTransform>(), cardRect);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PremiumRuntimeAssetsDoNotHaveByteIdenticalArtCopies()
        {
            foreach (var resourcePath in new[] { ResourceHeroPath, CompactWeaponSlotResourcePath })
            {
                var resourceBytes = File.ReadAllBytes(resourcePath);
                var duplicatePaths = Directory
                    .GetFiles("Assets/JoseonHunter/Art", Path.GetFileName(resourcePath), SearchOption.AllDirectories)
                    .Where(path => File.ReadAllBytes(path).SequenceEqual(resourceBytes))
                    .ToArray();

                Assert.That(duplicatePaths, Is.Empty,
                    $"{resourcePath} must be the single canonical runtime copy; duplicates: {string.Join(", ", duplicatePaths)}");
            }
        }

        [Test]
        public void PremiumLobbyArtExistsAndIsMobileBounded()
        {
            foreach (var path in new[] { ResourceHeroPath, FramePath, PrimaryButtonPath })
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
            foreach (var path in new[] { SecondaryButtonPath, CompactWeaponSlotResourcePath })
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

        private static void AssertRectInside(RectTransform child, RectTransform parent)
        {
            var childCorners = new Vector3[4];
            var parentCorners = new Vector3[4];
            child.GetWorldCorners(childCorners);
            parent.GetWorldCorners(parentCorners);
            foreach (var corner in childCorners)
            {
                Assert.That(corner.x, Is.InRange(parentCorners[0].x, parentCorners[2].x));
                Assert.That(corner.y, Is.InRange(parentCorners[0].y, parentCorners[2].y));
            }
        }
    }
}
