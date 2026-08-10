using System.IO;
using System.Linq;
using System.Reflection;
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
        private const string LockSlashConstraintGuid =
            "27d3aac4fd2d4fce9b5ebdd62d8a41a1";
        private const string DifficultyCardPrefabPath =
            "Assets/JoseonHunter/Prefabs/UI/Lobby/Modules/DifficultyCard.prefab";

        [TestCase(720f, 1280f)]
        [TestCase(1080f, 2340f)]
        public void DifficultyCardPrefabKeepsSerializedLockDecorationInsideAuthoredCard(
            float width,
            float height)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DifficultyCardPrefabPath);
            var expectedScript = AssetDatabase.LoadAssetAtPath<MonoScript>(LockSlashConstraintPath);

            Assert.That(prefab, Is.Not.Null, DifficultyCardPrefabPath);
            Assert.That(expectedScript, Is.Not.Null, LockSlashConstraintPath);
            Assert.That(expectedScript.GetClass(), Is.EqualTo(typeof(LockSlashConstraint)));
            Assert.That(AssetDatabase.AssetPathToGUID(LockSlashConstraintPath),
                Is.EqualTo(LockSlashConstraintGuid));
            Assert.That(prefab.GetComponentsInChildren<LockSlashConstraint>(true).Length,
                Is.EqualTo(1));
            Assert.That(prefab.GetComponent<RectTransform>().sizeDelta,
                Is.EqualTo(new Vector2(280f, 100f)));

            var canvasObject = new GameObject(
                "Portrait Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(width, height);

                var instance = PrefabUtility.InstantiatePrefab(
                    prefab,
                    canvasObject.transform) as GameObject;
                Assert.That(instance, Is.Not.Null, DifficultyCardPrefabPath);

                var cardRect = instance.GetComponent<RectTransform>();
                var buttonTransform = instance.transform.Find("Button");
                var button = buttonTransform?.GetComponent<Button>();
                var buttonRect = buttonTransform as RectTransform;
                Assert.That(cardRect.sizeDelta, Is.EqualTo(new Vector2(280f, 100f)));
                Assert.That(button, Is.Not.Null, "DifficultyCard.prefab must retain its authored Button.");
                Assert.That(buttonRect, Is.Not.Null);
                Assert.That(buttonRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(buttonRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(buttonRect.sizeDelta, Is.EqualTo(Vector2.zero));

                var constraint = instance
                    .GetComponentsInChildren<LockSlashConstraint>(true)
                    .Single();
                var actualScript = MonoScript.FromMonoBehaviour(constraint);
                Assert.That(actualScript, Is.SameAs(expectedScript),
                    "DifficultyCard.prefab must serialize the standalone lock constraint script.");
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(actualScript)),
                    Is.EqualTo(LockSlashConstraintGuid));

                constraint.Configure();
                Canvas.ForceUpdateCanvases();

                var slashRect = constraint.GetComponent<RectTransform>();
                var lockIconRect = buttonTransform.Find("Lock Icon") as RectTransform;
                Assert.That(slashRect, Is.Not.Null);
                Assert.That(lockIconRect, Is.Not.Null);
                Assert.That(buttonRect.rect.size, Is.EqualTo(new Vector2(280f, 100f)));

                AssertRectInside(slashRect, buttonRect);
                AssertRectInside(lockIconRect, buttonRect);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
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

        [Test]
        public void AuthoredLobbyCapturePlanHasTheExactTwelvePremiumPortraitOutputs()
        {
            var planProperty = typeof(JoseonHunter.Editor.Scenes.LobbySceneBuilder).GetProperty(
                "CapturePlan",
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(planProperty, Is.Not.Null,
                "Authored lobby capture must expose a deterministic pure capture plan.");
            var plan = ((System.Collections.IEnumerable)planProperty.GetValue(null)).Cast<object>().ToArray();
            Assert.That(plan.Select(item => (Vector2Int)item.GetType().GetProperty("Resolution").GetValue(item)).ToArray(),
                Is.EqualTo(new[]
                {
                    new Vector2Int(720, 1280), new Vector2Int(720, 1280), new Vector2Int(720, 1280), new Vector2Int(720, 1280),
                    new Vector2Int(1080, 1920), new Vector2Int(1080, 1920), new Vector2Int(1080, 1920), new Vector2Int(1080, 1920),
                    new Vector2Int(1080, 2340), new Vector2Int(1080, 2340), new Vector2Int(1080, 2340), new Vector2Int(1080, 2340)
                }));
            Assert.That(plan.Select(item => (string)item.GetType().GetProperty("Page").GetValue(item)).ToArray(),
                Is.EqualTo(new[]
                {
                    "Home", "Training", "Patrol", "Research-ready",
                    "Home", "Training", "Patrol", "Research-ready",
                    "Home", "Training", "Patrol", "Research-ready"
                }));
            Assert.That(plan.Select(item => (string)item.GetType().GetProperty("FileName").GetValue(item)).ToArray(),
                Is.EqualTo(new[]
                {
                    "720x1280-home.png", "720x1280-training.png", "720x1280-patrol.png", "720x1280-research-ready.png",
                    "1080x1920-home.png", "1080x1920-training.png", "1080x1920-patrol.png", "1080x1920-research-ready.png",
                    "1080x2340-home.png", "1080x2340-training.png", "1080x2340-patrol.png", "1080x2340-research-ready.png"
                }));
        }

        [TestCase(720, 1280, 720, 1280)]
        [TestCase(1080, 1920, 720, 1280)]
        [TestCase(1080, 2340, 720, 1560)]
        public void AuthoredLobbyCaptureUsesDeterministicPortraitCanvasSize(
            int width, int height, int expectedLogicalWidth, int expectedLogicalHeight)
        {
            var method = typeof(JoseonHunter.Editor.Scenes.LobbySceneBuilder).GetMethod(
                "LogicalCaptureSize",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null,
                "Capture must not inherit the current editor GameView dimensions.");
            var logical = (Vector2)method.Invoke(null, new object[] { new Vector2Int(width, height) });
            Assert.That(logical.x, Is.EqualTo(expectedLogicalWidth).Within(.01f));
            Assert.That(logical.y, Is.EqualTo(expectedLogicalHeight).Within(.01f));
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
