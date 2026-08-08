using System.Collections;
using System.Linq;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class PremiumPauseUiPlayModeTests
    {
        [UnityTest]
        public IEnumerator PauseUsesThinFrameBackplateAndOnlyItsTwoRunActions()
        {
            MetaGameSession.EnsureExists();
            var root = new GameObject("Pause Root", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = PortraitUiMetrics.ReferenceResolution;
            var presenter = root.AddComponent<AbandonRunPresenter>();
            presenter.Open();

            var panel = root.transform.Find("Abandon Run Root/Abandon Panel").GetComponent<Image>();
            Assert.That(panel.sprite, Is.Not.Null);
            Assert.That(panel.sprite.name, Is.EqualTo("thin_outer_frame"));
            var backplate = panel.transform.Find("Pause Backplate")?.GetComponent<Image>();
            Assert.That(backplate, Is.Not.Null);
            Assert.That(backplate.sprite, Is.Not.Null);
            Assert.That(backplate.sprite.name, Is.EqualTo("content_backplate"));
            Assert.That(backplate.transform.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(root.GetComponentsInChildren<Slider>(true).Length, Is.EqualTo(2));
            Assert.That(root.GetComponentsInChildren<Button>(true).Select(button => button.name),
                Is.EquivalentTo(new[] { "Continue Combat Button", "Confirm Return Button" }));
            Assert.That(root.GetComponentsInChildren<Transform>(true).Any(item => item.name == "Settings Button"),
                Is.False);
            Assert.That(panel.transform.Find("Pause Divider"), Is.Not.Null);

            AssertBoundsWithinPanel(panel.rectTransform, FindText(root, "Abandon Title").rectTransform);
            AssertBoundsWithinPanel(panel.rectTransform, FindText(root, "Abandon Message").rectTransform);

            var continueButton = FindButton(root, "Continue Combat Button");
            var returnButton = FindButton(root, "Confirm Return Button");
            Assert.That(continueButton.GetComponent<RectTransform>().anchoredPosition.y,
                Is.GreaterThan(returnButton.GetComponent<RectTransform>().anchoredPosition.y));
            Assert.That(((Image)continueButton.targetGraphic).sprite, Is.Not.Null);
            Assert.That(((Image)returnButton.targetGraphic).sprite, Is.Not.Null);

            var musicLabel = FindText(root, "Music Volume Slider Label");
            var effectsLabel = FindText(root, "Sound Effect Volume Slider Label");
            Assert.That(musicLabel.color.grayscale, Is.GreaterThan(.55f));
            Assert.That(effectsLabel.color.grayscale, Is.GreaterThan(.55f));

            Object.Destroy(root);
            yield return null;
        }

        private static Button FindButton(GameObject root, string name) =>
            root.GetComponentsInChildren<Button>(true).Single(button => button.name == name);

        private static TMP_Text FindText(GameObject root, string name) =>
            root.GetComponentsInChildren<TMP_Text>(true).Single(text => text.name == name);

        private static void AssertBoundsWithinPanel(RectTransform panel, RectTransform child)
        {
            var panelHalfSize = panel.sizeDelta * .5f;
            var childHalfSize = child.sizeDelta * .5f;
            var childMin = child.anchoredPosition - childHalfSize;
            var childMax = child.anchoredPosition + childHalfSize;
            Assert.That(childMin.x, Is.GreaterThanOrEqualTo(-panelHalfSize.x), child.name);
            Assert.That(childMax.x, Is.LessThanOrEqualTo(panelHalfSize.x), child.name);
            Assert.That(childMin.y, Is.GreaterThanOrEqualTo(-panelHalfSize.y), child.name);
            Assert.That(childMax.y, Is.LessThanOrEqualTo(panelHalfSize.y), child.name);
        }
    }
}
