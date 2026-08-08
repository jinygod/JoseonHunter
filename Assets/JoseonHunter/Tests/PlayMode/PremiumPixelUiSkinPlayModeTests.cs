using System.Collections;
using System.Linq;
using JoseonHunter.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class PremiumPixelUiSkinPlayModeTests
    {
        [Test]
        public void ApplyFrameMapsSemanticFramesToThinPixelLabSprites()
        {
            Assert.That(ApplyFrameAndReturnSprite(PremiumFrame.ThinOuter).name, Is.EqualTo("thin_outer_frame"));
            Assert.That(ApplyFrameAndReturnSprite(PremiumFrame.HeaderBar).name, Is.EqualTo("header_bar"));
            Assert.That(ApplyFrameAndReturnSprite(PremiumFrame.ContentBackplate).name, Is.EqualTo("content_backplate"));
        }

        [Test]
        public void ApplyActionMapsPrimaryToSemanticButtonSprite()
        {
            var root = new GameObject("Action Test", typeof(RectTransform));
            var button = RuntimeUiFactory.Button("Primary", root.transform, Color.black);

            PremiumPixelUiSkin.ApplyAction(button, PremiumActionStyle.Primary);

            Assert.That(((Image)button.targetGraphic).sprite.name, Is.EqualTo("primary_red_button"));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyDifficultyMapsSelectedAndLockedToDistinctSemanticSprites()
        {
            var root = new GameObject("Difficulty Test", typeof(RectTransform));
            var button = RuntimeUiFactory.Button("Difficulty", root.transform, Color.black);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 100f);

            PremiumPixelUiSkin.ApplyDifficulty(button, selected: true, locked: false);
            Assert.That(((Image)button.targetGraphic).sprite.name, Is.EqualTo("difficulty_selected"));

            PremiumPixelUiSkin.ApplyDifficulty(button, selected: false, locked: true);
            Assert.That(((Image)button.targetGraphic).sprite.name, Is.EqualTo("difficulty_locked"));
            var slashRect = button.transform.Find("Lock Slash").GetComponent<RectTransform>();
            var lockRect = button.transform.Find("Lock Icon").GetComponent<RectTransform>();
            Assert.That(slashRect.anchorMin.x, Is.EqualTo(.12f));
            Assert.That(slashRect.anchorMax.x, Is.EqualTo(.88f));
            Assert.That(lockRect.sizeDelta.x, Is.EqualTo(30f).Within(.001f));
            Assert.That(lockRect.sizeDelta.y, Is.EqualTo(30f).Within(.001f));
            AssertRectInsideButton(slashRect, button.GetComponent<RectTransform>());
            AssertRectInsideButton(lockRect, button.GetComponent<RectTransform>());

            Object.DestroyImmediate(root);
        }

        [TestCase(320f, 72f)]
        [TestCase(480f, 64f)]
        public void ApplyDifficultyKeepsRotatedLockSlashInsideShortWideCards(float width, float height)
        {
            var root = new GameObject("Wide Difficulty Test", typeof(RectTransform));
            var button = RuntimeUiFactory.Button("Difficulty", root.transform, Color.black);
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(width, height);

            PremiumPixelUiSkin.ApplyDifficulty(button, selected: false, locked: true);
            PremiumPixelUiSkin.ApplyDifficulty(button, selected: false, locked: true);

            var slashRect = button.transform.Find("Lock Slash").GetComponent<RectTransform>();
            Assert.That(button.transform.Cast<Transform>().Count(t => t.name == "Lock Slash"), Is.EqualTo(1));
            Assert.That(slashRect.localEulerAngles.z, Is.EqualTo(344f).Within(.001f));
            AssertRectInsideButton(slashRect, buttonRect);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyNavigationMapsIdleAndSelectedToSemanticTabSprites()
        {
            var root = new GameObject("Navigation Test", typeof(RectTransform));
            var button = RuntimeUiFactory.Button("Navigation", root.transform, Color.black);

            PremiumPixelUiSkin.ApplyNavigation(button, PremiumIcon.Patrol, selected: false);
            Assert.That(((Image)button.targetGraphic).sprite.name, Is.EqualTo("tab_idle"));

            PremiumPixelUiSkin.ApplyNavigation(button, PremiumIcon.Patrol, selected: true);
            Assert.That(((Image)button.targetGraphic).sprite.name, Is.EqualTo("tab_selected"));
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator NavigationUsesOnlyOneCenteredPixelIconAndNoVisibleLabel()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var button = RuntimeUiFactory.Button("Patrol Navigation", root.transform, Color.black);
            var label = RuntimeUiFactory.Text("Label", button.transform, "출전", 24f,
                TextAlignmentOptions.Center);

            PremiumPixelUiSkin.ApplyNavigation(button, PremiumIcon.Patrol, true);
            PremiumPixelUiSkin.ApplyNavigation(button, PremiumIcon.Patrol, true);

            Assert.That(label.gameObject.activeSelf, Is.False);
            Assert.That(button.transform.Cast<Transform>().Count(t => t.name == "Premium Icon"),
                Is.EqualTo(1));
            var icon = button.transform.Find("Premium Icon").GetComponent<Image>();
            Assert.That(icon.sprite, Is.Not.Null);
            Assert.That(icon.sprite.name, Is.EqualTo("icon_patrol"));
            Assert.That(icon.rectTransform.anchorMin, Is.EqualTo(new Vector2(.5f, .5f)));
            Assert.That(((Image)button.targetGraphic).sprite.name,
                Is.EqualTo("tab_selected"));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedDifficultyUsesIdleCardLockAndSlash()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var button = RuntimeUiFactory.Button("Difficulty Great Omen", root.transform, Color.black);

            PremiumPixelUiSkin.ApplyDifficulty(button, false, true);
            PremiumPixelUiSkin.ApplyDifficulty(button, false, true);

            Assert.That(((Image)button.targetGraphic).sprite.name,
                Is.EqualTo("difficulty_locked"));
            Assert.That(button.transform.Cast<Transform>().Count(t => t.name == "Lock Slash"),
                Is.EqualTo(1));
            Assert.That(button.transform.Find("Lock Slash").gameObject.activeSelf, Is.True);
            var icon = button.transform.Find("Lock Icon").GetComponent<Image>();
            Assert.That(icon.sprite, Is.Not.Null);
            Assert.That(icon.sprite.name, Is.EqualTo("icon_lock"));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissingResourceLeavesExistingGraphicOperable()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var image = RuntimeUiFactory.Image("Graphic", root.transform, Color.magenta);
            var original = image.color;

            PremiumPixelUiSkin.ApplyFrame(image, (PremiumFrame)999);

            Assert.That(image.color, Is.EqualTo(original));
            Object.Destroy(root);
            yield return null;
        }

        private static Sprite ApplyFrameAndReturnSprite(PremiumFrame frame)
        {
            var root = new GameObject("Frame Test", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var image = root.GetComponent<Image>();
            PremiumPixelUiSkin.ApplyFrame(image, frame);
            var sprite = image.sprite;
            Object.DestroyImmediate(root);
            return sprite;
        }

        private static void AssertRectInsideButton(RectTransform child, RectTransform button)
        {
            var childCorners = new Vector3[4];
            var buttonCorners = new Vector3[4];
            child.GetWorldCorners(childCorners);
            button.GetWorldCorners(buttonCorners);
            foreach (var corner in childCorners)
            {
                Assert.That(corner.x, Is.InRange(buttonCorners[0].x, buttonCorners[2].x));
                Assert.That(corner.y, Is.InRange(buttonCorners[0].y, buttonCorners[2].y));
            }
        }
    }
}
