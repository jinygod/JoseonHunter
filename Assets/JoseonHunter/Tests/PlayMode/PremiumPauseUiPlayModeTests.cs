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
        public IEnumerator PauseIsDarkSettingsWindowWithTwoVerticalActionsAndNoSettingsButton()
        {
            MetaGameSession.EnsureExists();
            var root = new GameObject("Pause Root", typeof(RectTransform));
            var presenter = root.AddComponent<AbandonRunPresenter>();
            presenter.Open();

            var panel = root.transform.Find("Abandon Run Root/Abandon Panel").GetComponent<Image>();
            Assert.That(panel.sprite, Is.Not.Null);
            Assert.That(panel.sprite.name, Is.EqualTo("panel_frame"));
            Assert.That(root.GetComponentsInChildren<Slider>(true).Length, Is.EqualTo(2));
            Assert.That(root.GetComponentsInChildren<Button>(true).Select(button => button.name),
                Is.EquivalentTo(new[] { "Continue Combat Button", "Confirm Return Button" }));
            Assert.That(root.GetComponentsInChildren<Transform>(true).Any(item => item.name == "Settings Button"),
                Is.False);
            Assert.That(panel.transform.Find("Pause Divider"), Is.Not.Null);

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
    }
}
