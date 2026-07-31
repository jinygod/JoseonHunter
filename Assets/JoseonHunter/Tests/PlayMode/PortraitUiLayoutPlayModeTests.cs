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
            var reward = Object.FindFirstObjectByType<RewardRevealPresenter>();
            reward.Play(new ProgressionRewardEvent("boots", null, 1, ProgressionRewardKind.Support,
                "능력 강화", "+12%", null));
            var appraisal = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            appraisal.ShowDetails(controller.UiState.Weapons[0]);
            yield return null;

            var originalSize = new Vector2Int(Screen.width, Screen.height);
            var originalSafeArea = Screen.safeArea;
            try
            {
                foreach (var requested in PortraitUiMetrics.ValidationResolutions)
                {
                    Screen.SetResolution(requested.x, requested.y, FullScreenMode.Windowed);
                    var frames = 0;
                    while ((Screen.width != requested.x || Screen.height != requested.y) && frames++ < 12)
                        yield return null;
                    var applied = new Vector2Int(Screen.width, Screen.height) == requested;
                    Assert.That(applied || Application.isBatchMode, Is.True,
                        "Screen.SetResolution must apply outside Unity batchmode.");
                    if (!applied)
                    {
                        var simulatedCanvas = PortraitUiMetrics.CanvasSizeFor(requested);
                        var simulatedSafeWidth = simulatedCanvas.x * .925f;
                        Assert.That(PortraitUiMetrics.ContainedWidth(simulatedSafeWidth, 984f),
                            Is.LessThanOrEqualTo(simulatedSafeWidth));
                        Assert.That(PortraitUiMetrics.ContainedWidth(simulatedSafeWidth, 936f),
                            Is.LessThanOrEqualTo(simulatedSafeWidth));
                        Assert.That(Mathf.Min(PortraitUiMetrics.RackSlotWidth,
                            (simulatedSafeWidth - 24f) * .5f) * 2f + 24f,
                            Is.LessThanOrEqualTo(simulatedSafeWidth));
                    }

                    var actual = new Vector2(Screen.width, Screen.height);
                    bootstrap.ApplySafeArea(new Rect(0f, actual.y * .04f, actual.x, actual.y * .925f), actual);
                    Canvas.ForceUpdateCanvases();
                    AssertBounds(bootstrap, "Vitals", bootstrap.SafeAreaContainer);
                    AssertBounds(bootstrap, "Weapon Rack", bootstrap.SafeAreaContainer);
                    AssertBounds(bootstrap, "Weapon Slot 0", bootstrap.SafeAreaContainer);
                    AssertBounds(bootstrap, "Upgrade Card 0", bootstrap.ModalSafeAreaContainer);
                    AssertBounds(bootstrap, "Confirm Reward", bootstrap.ModalSafeAreaContainer);
                    AssertBounds(bootstrap, "Weapon Appraisal Panel", bootstrap.ModalSafeAreaContainer);
                }
            }
            finally
            {
                appraisal.HideImmediately();
                reward.HideImmediately();
                Screen.SetResolution(originalSize.x, originalSize.y, FullScreenMode.Windowed);
                bootstrap.ApplySafeArea(originalSafeArea, originalSize);
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
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            for (var index = 0; index < corners.Length; index++)
                if (!parent.rect.Contains(parent.InverseTransformPoint(corners[index]))) return false;
            return true;
        }

        private static void AssertBounds(FirstPlayableUiBootstrap bootstrap, string name, RectTransform owner)
        {
            var child = bootstrap.GetComponentsInChildren<RectTransform>(true);
            var match = System.Array.Find(child, rect => rect.name == name);
            Assert.That(match, Is.Not.Null, name);
            Assert.That(IsContained(owner, match), Is.True, name);
        }
    }
}
