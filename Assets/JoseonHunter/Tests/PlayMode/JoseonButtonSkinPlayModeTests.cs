using System.Collections;
using System.Linq;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class JoseonButtonSkinPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator ApplyCreatesOneSlicedFrameAndOneSemanticIconIdempotently()
        {
            var root = new GameObject("Button Skin Test Root", typeof(RectTransform));
            var button = RuntimeUiFactory.Button("Action", root.transform, Color.black);
            var label = RuntimeUiFactory.Text("Label", button.transform, "확인", 24f,
                TMPro.TextAlignmentOptions.Center);
            RuntimeUiFactory.Stretch(label.rectTransform, 8f, 4f, 8f, 4f);

            JoseonButtonSkin.Apply(button, JoseonButtonStyle.Primary, JoseonButtonIcon.Continue);
            JoseonButtonSkin.Apply(button, JoseonButtonStyle.Primary, JoseonButtonIcon.Continue);

            var background = button.targetGraphic as Image;
            Assert.That(background, Is.Not.Null);
            Assert.That(background.sprite, Is.Not.Null);
            Assert.That(background.sprite.name, Is.EqualTo("primary_red_button"));
            Assert.That(background.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(button.transform.Cast<Transform>().Count(child => child.name == "Action Icon"),
                Is.EqualTo(1));
            Assert.That(button.transform.Find("Action Icon").GetComponent<Image>().sprite.name,
                Is.EqualTo("icon_continue"));
            Assert.That(label.color, Is.EqualTo(JoseonUiPalette.Hanji));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseAndResultButtonsUseFinishedFramesAndSemanticIcons()
        {
            MetaGameSession.EnsureExists();
            var pauseRoot = new GameObject("Pause Skin Test Root", typeof(RectTransform));
            var pause = pauseRoot.AddComponent<AbandonRunPresenter>();
            pause.Open();

            AssertAction(pauseRoot, "Continue Combat Button", "primary_red_button", "icon_continue");
            AssertAction(pauseRoot, "Confirm Return Button", "secondary_dark_button", "icon_lobby");

            var resultRoot = new GameObject("Result Skin Test Root", typeof(RectTransform));
            var result = resultRoot.AddComponent<RunResultPresenter>();
            result.Render(new FirstPlayableUiState(3, 0, 10, 4, 12,
                30f, 900f, 100f, 100f, false, false, 0f, 0f,
                System.Array.Empty<WeaponSlotView>(), runEnded: true, victory: false));

            AssertAction(resultRoot, "Lobby Return Button", "secondary_dark_button", "icon_lobby");

            Object.Destroy(pauseRoot);
            Object.Destroy(resultRoot);
            yield return null;
        }

        private static void AssertAction(GameObject root, string buttonName, string frameName, string iconName)
        {
            var button = root.GetComponentsInChildren<Button>(true)
                .Single(candidate => candidate.name == buttonName);
            var background = button.targetGraphic as Image;
            Assert.That(background, Is.Not.Null, buttonName);
            Assert.That(background.sprite, Is.Not.Null, buttonName);
            Assert.That(background.sprite.name, Is.EqualTo(frameName), buttonName);
            Assert.That(background.type, Is.EqualTo(Image.Type.Sliced), buttonName);
            var icon = button.transform.Find("Action Icon")?.GetComponent<Image>();
            Assert.That(icon, Is.Not.Null, buttonName);
            Assert.That(icon.sprite, Is.Not.Null, buttonName);
            Assert.That(icon.sprite.name, Is.EqualTo(iconName), buttonName);
        }
    }
}
