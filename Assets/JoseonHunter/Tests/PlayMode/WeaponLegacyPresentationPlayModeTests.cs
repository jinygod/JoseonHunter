using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WeaponLegacyPresentationPlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale() => Time.timeScale = 1f;

        [Test]
        public void Legacy_modal_renders_two_complete_Korean_path_cards()
        {
            var root = CreateRoot("Legacy Test Root");
            try
            {
                var presenter = root.gameObject.AddComponent<WeaponLegacyChoicePresenter>();
                presenter.Build();
                presenter.Open(new WeaponLegacyChoiceState("frost_flask", "서리병", new[]
                {
                    new WeaponLegacyChoiceView(WeaponLegacyPathId.FrostMist, "빙무", "넓은 서리 안개로 제어",
                        "넓은 둔화와 빙결", "직접 피해 감소", null),
                    new WeaponLegacyChoiceView(WeaponLegacyPathId.FrostShatter, "파쇄", "짧고 강한 착지 폭발",
                        "순간 폭발과 연쇄 파쇄", "장판 지속시간 감소", null)
                }), _ => true);
                Canvas.ForceUpdateCanvases();

                Assert.That(TextValue(TextNamed(root, "Legacy Heading")), Is.EqualTo("전승 경로를 선택하세요"));
                Assert.That(TextColor(TextNamed(root, "Legacy Heading")), Is.EqualTo(JoseonUiPalette.HanjiInk));
                Assert.That(ButtonsNamed(root, "Legacy Choice").Count(), Is.EqualTo(2));
                for (var index = 0; index < 2; index++)
                {
                    var card = root.Find("Weapon Legacy Overlay/Legacy Cards/Legacy Choice " + index);
                    var background = card.Find("Legacy Card Background " + index).GetComponent<Image>();
                    Assert.That(background.sprite, Is.Null);
                    Assert.That(background.color.a, Is.EqualTo(1f));
                    Assert.That(background.transform.GetSiblingIndex(), Is.Zero);
                    Assert.That(background.rectTransform.offsetMin, Is.EqualTo(new Vector2(12f, 12f)));
                    Assert.That(background.rectTransform.offsetMax, Is.EqualTo(new Vector2(-12f, -12f)));
                    Assert.That(TextValue(TextNamed(card, "Combat Style")), Does.StartWith("전투 방식 · "));
                    Assert.That(TextValue(TextNamed(card, "Benefit")), Does.StartWith("강점 · "));
                    Assert.That(TextValue(TextNamed(card, "Cost")), Does.StartWith("약점 · "));
                    Assert.That(TextColor(TextNamed(card, "Combat Style")), Is.EqualTo(JoseonUiPalette.HanjiInk));
                    Assert.That(TextColor(TextNamed(card, "Cost")), Is.EqualTo(JoseonUiPalette.SealCrimson));
                    Assert.That(IsContained(root, card.GetComponent<RectTransform>()), Is.True);
                }
                AssertVisibleCopyIsKorean(root);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        [Test]
        public void Replacement_modal_renders_four_owned_weapons_and_cancel()
        {
            var root = CreateRoot("Replacement Test Root");
            try
            {
                var presenter = root.gameObject.AddComponent<WeaponReplacementPresenter>();
                presenter.Build();
                presenter.Open(new WeaponReplacementState("frost_flask", "서리병", new[]
                {
                    Choice("hwando", "환도 비검", 4, "월식"),
                    Choice("gakgung", "각궁", 3, "관일"),
                    Choice("talisman", "주술 부적", 2, ""),
                    Choice("bomb", "벽력탄", 1, "")
                }), _ => true, () => true);
                Canvas.ForceUpdateCanvases();

                Assert.That(TextValue(TextNamed(root, "Replacement Heading")), Is.EqualTo("버릴 무기를 선택하세요"));
                Assert.That(TextValue(TextNamed(root, "New Weapon Label")), Is.EqualTo("새 무기 · 서리병"));
                Assert.That(ButtonsNamed(root, "Replacement Choice").Count(), Is.EqualTo(4));
                Assert.That(TextValue(TextNamed(root, "Replacement Detail 0")), Is.EqualTo("레벨 4 · 월식"));
                Assert.That(TextValue(TextNamed(root, "Cancel Replacement Label")), Is.EqualTo("교체하지 않기"));
                Assert.That(IsContained(root, root.Find("Weapon Replacement Overlay/Replacement Panel/Cancel Replacement")
                    .GetComponent<RectTransform>()), Is.True);
                AssertVisibleCopyIsKorean(root);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_routes_replacement_into_legacy_then_appraisal()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            controller.SetWeaponLevelForTests(JoseonHunter.Domain.Combat.WeaponId.HwandoFlyingBlade, 4);
            controller.SetWeaponLevelForTests(JoseonHunter.Domain.Combat.WeaponId.GakgungShot, 2);
            controller.SetWeaponLevelForTests(JoseonHunter.Domain.Combat.WeaponId.TalismanThrow, 2);
            controller.SetWeaponLevelForTests(JoseonHunter.Domain.Combat.WeaponId.ThunderCrashBomb, 2);
            controller.OpenUpgradeOffersForTests(new UpgradeOffer(
                JoseonHunter.Domain.Combat.WeaponId.FrostFlask.Value, UpgradeKind.Weapon, 1,
                requiresReplacement: true));
            controller.TryChooseUpgrade(0);
            yield return null;

            var replacement = Object.FindAnyObjectByType<WeaponReplacementPresenter>();
            Assert.That(replacement.IsOpen, Is.True);
            replacement.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Replacement Choice 0").onClick.Invoke();
            yield return null;

            var legacy = Object.FindAnyObjectByType<WeaponLegacyChoicePresenter>();
            Assert.That(replacement.IsOpen, Is.False);
            Assert.That(legacy.IsOpen, Is.True);
            legacy.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Legacy Choice 0").onClick.Invoke();
            yield return null;

            Assert.That(legacy.IsOpen, Is.False);
            Assert.That(controller.Flow.State, Is.EqualTo(JoseonHunter.Domain.Runs.GameFlowState.AugmentResult));
            var appraisal = Object.FindAnyObjectByType<WeaponAffixRevealPresenter>();
            Assert.That(appraisal.IsRevealing, Is.True);
            appraisal.HideImmediately();
            var bootstrap = Object.FindAnyObjectByType<FirstPlayableUiBootstrap>();
            Object.Destroy(controller.gameObject);
            if (bootstrap != null) Object.Destroy(bootstrap.gameObject);
            yield return null;
        }

        private static WeaponReplacementChoiceView Choice(string id, string name, int level, string legacy) =>
            new(id, name, level, legacy, null);

        private static RectTransform CreateRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(1080f, 1920f);
            return root;
        }

        private static Component TextNamed(Transform root, string name) =>
            root.GetComponentsInChildren<Component>(true)
                .Single(component => component.name == name && component.GetType().Name == "TextMeshProUGUI");

        private static string TextValue(Component text) =>
            (string)text.GetType().GetProperty("text").GetValue(text);

        private static Color TextColor(Component text) =>
            (Color)text.GetType().GetProperty("color").GetValue(text);

        private static System.Collections.Generic.IEnumerable<Button> ButtonsNamed(Transform root, string prefix) =>
            root.GetComponentsInChildren<Button>(true).Where(button => button.name.StartsWith(prefix));

        private static void AssertVisibleCopyIsKorean(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<Component>(true))
                if (text.GetType().Name == "TextMeshProUGUI" && text.gameObject.activeInHierarchy)
                    Assert.That(Regex.IsMatch(TextValue(text), "[A-Za-z]"), Is.False,
                        text.name + ": " + TextValue(text));
        }

        private static bool IsContained(RectTransform parent, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = parent.InverseTransformPoint(corner);
                if (!parent.rect.Contains(point)) return false;
            }
            return true;
        }
    }
}
