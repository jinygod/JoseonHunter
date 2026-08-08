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
                Is.EqualTo("nav_selected_frame"));

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
                Is.EqualTo("card_idle_frame"));
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
    }
}
