using System.Collections;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class PortraitUiLayoutPlayModeTests
    {
        [UnityTest]
        public IEnumerator Portrait_canvas_safe_areas_and_runtime_font_cover_every_validation_resolution()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            yield return null;

            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            var scaler = bootstrap.GetComponent<CanvasScaler>();
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(bootstrap.transform.Find("Modal Layer"), Is.Not.Null);
            Assert.That(bootstrap.ModalSafeAreaContainer, Is.Not.Null);

            var resolutions = new[]
            {
                new Vector2Int(720, 1280), new Vector2Int(1080, 1920), new Vector2Int(1080, 2340),
                new Vector2Int(1170, 2532), new Vector2Int(1440, 3200)
            };
            foreach (var size in resolutions)
            {
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                yield return null;
                bootstrap.ApplySafeArea(new Rect(0f, size.y * .04f, size.x, size.y * .925f), size);
                Canvas.ForceUpdateCanvases();
                foreach (var button in bootstrap.GetComponentsInChildren<Button>(true))
                {
                    var owner = button.transform.IsChildOf(bootstrap.ModalSafeAreaContainer)
                        ? bootstrap.ModalSafeAreaContainer
                        : bootstrap.SafeAreaContainer;
                    Assert.That(IsContained(owner, button.GetComponent<RectTransform>()), Is.True, button.name);
                }
            }

            foreach (var text in bootstrap.GetComponentsInChildren<Component>(true))
            {
                if (text.GetType().Name != "TextMeshProUGUI") continue;
                var font = text.GetType().GetProperty("font").GetValue(text) as Object;
                Assert.That(font.name, Is.EqualTo("NotoSansKR-Dynamic SDF"), text.name);
            }
        }

        [UnityTest]
        public IEnumerator Portrait_modal_cards_and_confirmation_stay_inside_the_safe_area()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            yield return null;

            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            var upgrade = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            upgrade.BuildForTests();
            Canvas.ForceUpdateCanvases();
            foreach (var card in upgrade.GetComponentsInChildren<Button>(true))
            {
                Assert.That(card.GetComponent<RectTransform>().rect.width, Is.LessThanOrEqualTo(936f), card.name);
                Assert.That(IsContained(bootstrap.ModalSafeAreaContainer, card.GetComponent<RectTransform>()), Is.True,
                    card.name);
            }

            var appraisal = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            Assert.That(appraisal.transform.IsChildOf(bootstrap.ModalSafeAreaContainer), Is.True);

            var reward = Object.FindFirstObjectByType<RewardRevealPresenter>();
            reward.Play(new ProgressionRewardEvent("boots", null, 1, ProgressionRewardKind.Support,
                "능력 강화", "+12%", null));
            yield return null;
            var confirm = System.Array.Find(reward.GetComponentsInChildren<Button>(true),
                button => button.name == "Confirm Reward");
            Assert.That(IsContained(bootstrap.ModalSafeAreaContainer, confirm.GetComponent<RectTransform>()), Is.True);

            var heading = System.Array.Find(upgrade.GetComponentsInChildren<Component>(true),
                component => component.name == "Heading" && component.GetType().Name == "TextMeshProUGUI");
            Assert.That(heading.GetType().GetProperty("text").GetValue(heading), Is.EqualTo("강화를 선택하세요"));
        }

        private static bool IsContained(RectTransform parent, RectTransform child)
        {
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, child);
            return parent.rect.Contains((Vector2)bounds.min) && parent.rect.Contains((Vector2)bounds.max);
        }
    }
}
