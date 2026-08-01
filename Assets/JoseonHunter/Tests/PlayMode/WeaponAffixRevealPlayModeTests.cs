using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WeaponAffixRevealPlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale() => Time.timeScale = 1f;
        [TestCase(WeaponAffixTier.Standard, 0, 1.82f)]
        [TestCase(WeaponAffixTier.High, 0, 2.03f)]
        [TestCase(WeaponAffixTier.Perfect, 0, 2.08f)]
        [TestCase(WeaponAffixTier.Standard, 1, 2.25f)]
        [TestCase(WeaponAffixTier.Standard, 2, 2.43f)]
        [TestCase(WeaponAffixTier.Standard, 3, 2.61f)]
        public void Duration_uses_the_exact_affix_and_jackpot_caps(WeaponAffixTier tier, int potentialCount, float expected)
        {
            Assert.That(WeaponAffixRevealPresenter.DurationFor(Result(tier, potentialCount)),
                Is.EqualTo(expected).Within(.001f));
        }

        [Test]
        public void ResultValueUsesAReadableMobileFontSize()
        {
            var presenter = new GameObject("Affix Readability Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Result(WeaponAffixTier.High, 0));

            Assert.That(presenter.DetailFontSize, Is.GreaterThanOrEqualTo(26f));
            Object.DestroyImmediate(presenter.gameObject);
        }

        [Test]
        public void AppraisalUsesLargeVerticalDetailLayoutAndStartsAtZero()
        {
            var presenter = new GameObject("Appraisal Layout Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Result(WeaponAffixTier.High, 0));

            Assert.That(presenter.PanelSize.x, Is.GreaterThanOrEqualTo(900f));
            Assert.That(presenter.PanelSize.y, Is.GreaterThanOrEqualTo(720f));
            Assert.That(presenter.PotentialRowY(0), Is.GreaterThan(presenter.PotentialRowY(1)));
            Assert.That(presenter.PotentialRowY(1), Is.GreaterThan(presenter.PotentialRowY(2)));
            Assert.That(presenter.DisplayedAffixText, Does.Contain("+0%"));
            Assert.That(TextValue(RectNamed(presenter, "Affix Title")), Is.EqualTo("높은 추가옵션"));
            Assert.That(TextValue(RectNamed(presenter, "Confirm Label")), Is.EqualTo("확인"));
            Object.DestroyImmediate(presenter.gameObject);
        }

        [Test]
        public void Appraisal_uses_complete_scroll_and_visible_reel_rows_without_floating_decorations()
        {
            var presenter = new GameObject("Appraisal Composition Test")
                .AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            var result = Result(WeaponAffixTier.Perfect, 1);
            presenter.PreviewAtForEditor(result, WeaponAffixRevealPresenter.DurationFor(result));

            Assert.That(ImageNamed(presenter, "Top Scroll Roller").enabled, Is.False);
            Assert.That(ImageNamed(presenter, "Bottom Scroll Roller").enabled, Is.False);
            Assert.That(ImageNamed(presenter, "Jackpot Burst").enabled, Is.False);
            Assert.That(ImageNamed(presenter, "Rare Appraisal Stamp").enabled, Is.False);
            for (var index = 0; index < 4; index++)
            {
                var window = ImageNamed(presenter, "Reel Window " + index);
                Assert.That(window.enabled, Is.True, "Reel " + index);
                Assert.That(window.sprite, Is.Not.Null, "Reel " + index);
                Assert.That(window.preserveAspect, Is.False,
                    "The frame must fill its row so the icon well stays inside it.");
            }
            var lockedLabel = RectNamed(presenter, "Potential Label 1");
            Assert.That(lockedLabel.gameObject.activeSelf, Is.True);
            Assert.That(TextValue(lockedLabel), Does.Contain("잠김"));

            Object.DestroyImmediate(presenter.gameObject);
        }

        [Test]
        public void Appraisal_uses_a_solid_hanji_panel_without_a_stretched_pixel_background()
        {
            var catalog = Resources.Load<JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset>(
                "WeaponAffixPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var presenter = new GameObject("Appraisal Scroll Crop Test")
                .AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(catalog);
            presenter.ShowDetails(new WeaponSlotView(
                WeaponId.HwandoFlyingBlade.Value, "Hwando Flying Blade", 1, null));

            var panel = ImageNamed(presenter, "PixelLab Appraisal Sheet");
            Assert.That(panel.sprite, Is.Null);
            Assert.That(panel.color.a, Is.EqualTo(1f));
            Assert.That(RectNamed(presenter, "Hanji Border Top"), Is.Not.Null);
            Assert.That(RectNamed(presenter, "Hanji Border Bottom"), Is.Not.Null);

            Object.DestroyImmediate(presenter.gameObject);
        }

        [Test]
        public void General_affix_uses_a_readable_Korean_seal_instead_of_the_coin_symbol()
        {
            var presenter = new GameObject("Appraisal Seal Test")
                .AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            var result = Result(WeaponAffixTier.Perfect, 0);
            presenter.PreviewAtForEditor(result, WeaponAffixRevealPresenter.DurationFor(result));

            Assert.That(ImageNamed(presenter, "Final Symbol 0").sprite, Is.Null);
            Assert.That(TextValue(RectNamed(presenter, "Rarity Seal Label")), Is.EqualTo("최대"));

            Object.DestroyImmediate(presenter.gameObject);
        }

        [Test]
        public void General_affix_counts_from_zero_after_stop_before_confirmation_appears()
        {
            var presenter = new GameObject("Appraisal Count Up Test")
                .AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            var result = new WeaponAffixRollResult(
                new WeaponAffixRoll(WeaponAffixStat.Area, WeaponAffixTier.Perfect, 20d),
                System.Array.Empty<WeaponPotentialId>());
            var timeline = WeaponAffixRevealTimeline.For(result);

            presenter.PreviewAtForEditor(result, timeline.CountStartsAt - .01f);
            Assert.That(presenter.DisplayedAffixText, Is.EqualTo("공격 범위 +0%"));
            Assert.That(RectNamed(presenter, "Confirm Result").gameObject.activeSelf, Is.False);

            presenter.PreviewAtForEditor(result, (timeline.CountStartsAt + timeline.CountEndsAt) * .5f);
            Assert.That(presenter.DisplayedAffixText, Is.Not.EqualTo("공격 범위 +0%"));
            Assert.That(presenter.DisplayedAffixText, Is.Not.EqualTo("공격 범위 +20%"));
            Assert.That(RectNamed(presenter, "Confirm Result").gameObject.activeSelf, Is.False);

            presenter.PreviewAtForEditor(result, timeline.CountEndsAt);
            Assert.That(presenter.DisplayedAffixText, Is.EqualTo("공격 범위 +20%"));
            Assert.That(RectNamed(presenter, "Confirm Result").gameObject.activeSelf, Is.False);

            presenter.PreviewAtForEditor(result, timeline.ReadStartsAt);
            Assert.That(RectNamed(presenter, "Confirm Result").gameObject.activeSelf, Is.True);

            Object.DestroyImmediate(presenter.gameObject);
        }

        [Test]
        public void Weapon_detail_keeps_the_icon_and_text_inside_separate_safe_columns()
        {
            var presenter = new GameObject("Weapon Detail Layout Test")
                .AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.ShowDetails(new WeaponSlotView(
                WeaponId.HwandoFlyingBlade.Value,
                "Hwando Flying Blade",
                3,
                null,
                "Damage +24%",
                potentialIds: new[] { WeaponPotentialId.HwandoVenomFang },
                behavior: "Returning blade"));

            var icon = RectNamed(presenter, "Weapon Icon");
            var name = RectNamed(presenter, "Weapon Name");
            var generalWindow = RectNamed(presenter, "Reel Window 0");
            var generalTitle = RectNamed(presenter, "Affix Title");
            var generalDetail = RectNamed(presenter, "Affix Detail");
            Assert.That(icon.anchoredPosition.x, Is.GreaterThanOrEqualTo(-350f));
            Assert.That(name.anchoredPosition.x - name.sizeDelta.x * .5f,
                Is.GreaterThanOrEqualTo(icon.anchoredPosition.x + icon.sizeDelta.x * .5f + 24f));
            Assert.That(generalTitle.GetSiblingIndex(), Is.GreaterThan(generalWindow.GetSiblingIndex()),
                "The general-affix text must render above its row frame.");
            Assert.That(TextColor(generalTitle).grayscale, Is.GreaterThan(.45f));
            Assert.That(TextColor(generalDetail).grayscale, Is.GreaterThan(.65f));
            Assert.That(ImageNamed(presenter, "Confirm Result").sprite, Is.Not.Null,
                "Detail mode must bind the framed confirm button instead of showing a white rectangle.");

            for (var index = 1; index < 4; index++)
            {
                var window = ImageNamed(presenter, "Reel Window " + index);
                var viewport = RectNamed(presenter, "Reel Viewport " + index);
                Assert.That(window.enabled, Is.True, "Reel " + index);
                Assert.That(window.sprite, Is.Not.Null, "Reel " + index);
                Assert.That(viewport.anchorMin, Is.EqualTo(new Vector2(.5f, .5f)));
                Assert.That(viewport.anchorMax, Is.EqualTo(new Vector2(.5f, .5f)));
                Assert.That(viewport.anchoredPosition.x, Is.LessThanOrEqualTo(-280f));
                Assert.That(viewport.sizeDelta.x, Is.LessThanOrEqualTo(130f));
            }
            for (var index = 0; index < 4; index++)
            {
                Assert.That(ImageNamed(presenter, "Stop Flash " + index).enabled, Is.False,
                    "An unbound flash image becomes an opaque white panel in detail mode.");
                Assert.That(ImageNamed(presenter, "Spin Symbol " + index + "-0").enabled, Is.False);
                Assert.That(ImageNamed(presenter, "Spin Symbol " + index + "-1").enabled, Is.False);
            }
            var emptyPotential = RectNamed(presenter, "Potential Label 1");
            Assert.That(emptyPotential.gameObject.activeSelf, Is.True);
            Assert.That(TextValue(emptyPotential), Does.Contain("잠김"));
            Assert.That(TextColor(emptyPotential).grayscale, Is.GreaterThan(.5f));

            Object.DestroyImmediate(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator RepeatUpgradeOpensQuicklyAndShowsAccumulatedTotal()
        {
            var presenter = new GameObject("Repeat Appraisal Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Model(2, ProgressionRewardKind.WeaponLevel, WeaponAffixTier.Standard));

            Assert.That(presenter.ScrollOpenFraction, Is.GreaterThan(.5f));
            Assert.That(presenter.AccumulatedSummary, Does.Contain("Damage +24%"));
            yield return new WaitForSecondsRealtime(.14f);
            Assert.That(presenter.ScrollOpenFraction, Is.EqualTo(1f).Within(.01f));
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator NewWeaponUnfurlsFromTheCenter()
        {
            var presenter = new GameObject("New Weapon Appraisal Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Model(1, ProgressionRewardKind.NewWeapon, WeaponAffixTier.Standard));

            Assert.That(presenter.ScrollOpenFraction, Is.LessThan(.15f));
            yield return new WaitForSecondsRealtime(.38f);
            Assert.That(presenter.ScrollOpenFraction, Is.EqualTo(1f).Within(.01f));
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Skip_is_idempotent_and_does_not_change_the_roll_result()
        {
            var presenter = new GameObject("Affix Reveal Test").AddComponent<WeaponAffixRevealPresenter>();
            var result = Result(WeaponAffixTier.Perfect, 3);
            var completions = 0;
            presenter.RevealCompleted += () => completions++;
            Time.timeScale = 0f;
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(result);
            presenter.Skip(); presenter.Skip();
            yield return new WaitForSecondsRealtime(1.16f);
            Assert.That(presenter.IsAwaitingConfirmation, Is.True);
            presenter.Confirm();
            yield return new WaitForSecondsRealtime(.16f);
            Assert.That(presenter.IsRevealing, Is.False);
            Assert.That(presenter.LastCompletedResult, Is.SameAs(result));
            Assert.That(completions, Is.EqualTo(1));
            Time.timeScale = 1f;
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Pointer_dispatch_skip_is_idempotent()
        {
            var eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            var presenter = new GameObject("Pointer Skip Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            var result = Result(WeaponAffixTier.Standard, 0);
            presenter.Play(result);
            var pointer = new PointerEventData(eventSystem);
            ExecuteEvents.Execute<IPointerClickHandler>(presenter.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute<IPointerClickHandler>(presenter.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.86f);
            ExecuteEvents.Execute<IPointerClickHandler>(presenter.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.16f);
            Assert.That(presenter.LastCompletedResult, Is.SameAs(result));
            Object.Destroy(presenter.gameObject); Object.Destroy(eventSystem.gameObject);
        }

        [UnityTest]
        public IEnumerator Weapon_reveal_waits_for_choice_close_then_opens_one_queued_choice_after_skip()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null; yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            var generic = Object.FindFirstObjectByType<RewardRevealPresenter>();
            var affix = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            affix.SetCatalogForTests(TestCatalog());
            controller.OpenUpgradeForTests();
            controller.SetUpgradeOffersForTests(new UpgradeOffer(WeaponId.GakgungShot.Value, UpgradeKind.Weapon, 1));
            yield return new WaitForSecondsRealtime(.35f);
            var completions = 0;
            var queuedOpens = 0;
            affix.RevealCompleted += () => completions++;
            controller.UpgradeOpened += _ => queuedOpens++;
            var card = choice.GetComponentInChildren<Button>(true);
            ExecuteEvents.Execute<IPointerClickHandler>(card.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            controller.AddExperienceForTests(100);
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(choice.IsOpen, Is.True);
            Assert.That(affix.IsRevealing, Is.False);
            Assert.That(generic.IsRevealing, Is.False);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(affix.IsRevealing, Is.True);
            affix.Skip();
            affix.Skip();
            yield return new WaitForSecondsRealtime(1.16f);
            Assert.That(affix.IsAwaitingConfirmation, Is.True);
            affix.Confirm();
            yield return new WaitForSecondsRealtime(.16f);
            Assert.That(controller.IsUpgradeOpen, Is.True);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(queuedOpens, Is.EqualTo(1));
            yield return null;
            Assert.That(controller.IsUpgradeOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator Support_and_evolution_keep_generic_reward_reveal()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null; yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            var generic = Object.FindFirstObjectByType<RewardRevealPresenter>();
            var affix = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            affix.SetCatalogForTests(TestCatalog());

            yield return ChooseThroughVisibleCard(controller, choice, new UpgradeOffer("boots", UpgradeKind.Support, 1));
            Assert.That(generic.IsRevealing, Is.True);
            Assert.That(affix.IsRevealing, Is.False);
            yield return new WaitForSecondsRealtime(.5f);

            yield return ChooseThroughVisibleCard(controller, choice, new UpgradeOffer("gakgung_sun_piercer", UpgradeKind.Evolution, 5));
            Assert.That(generic.IsRevealing, Is.True);
            Assert.That(affix.IsRevealing, Is.False);
        }

        [UnityTest]
        public IEnumerator Hide_cancels_without_a_completion_notification()
        {
            var presenter = new GameObject("Affix Reveal Cancel Test").AddComponent<WeaponAffixRevealPresenter>();
            var completions = 0;
            presenter.RevealCompleted += () => completions++;
            presenter.Play(Result(WeaponAffixTier.Standard, 0));
            presenter.HideImmediately();
            yield return null;
            Assert.That(completions, Is.Zero);
            Assert.That(presenter.IsRevealing, Is.False);
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Every_result_holds_until_explicit_confirmation()
        {
            var presenter = new GameObject("Boundary Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            Time.timeScale = 0f;
            foreach (var result in new[] { Result(WeaponAffixTier.Standard, 0), Result(WeaponAffixTier.High, 0), Result(WeaponAffixTier.Perfect, 0), Result(WeaponAffixTier.Standard, 1), Result(WeaponAffixTier.Standard, 2), Result(WeaponAffixTier.Standard, 3) })
            {
                presenter.Play(result);
                yield return new WaitForSecondsRealtime(WeaponAffixRevealPresenter.DurationFor(result) + .04f);
                Assert.That(presenter.IsAwaitingConfirmation, Is.True);
                Assert.That(presenter.LastCompletedResult, Is.Not.SameAs(result));
                presenter.Confirm();
                yield return new WaitForSecondsRealtime(.16f);
                Assert.That(presenter.IsRevealing, Is.False);
                Assert.That(presenter.LastCompletedResult, Is.SameAs(result));
            }
            Object.Destroy(presenter.gameObject);
        }

        [TestCase(WeaponAffixTier.Standard, 0, false)]
        [TestCase(WeaponAffixTier.High, 0, true)]
        [TestCase(WeaponAffixTier.Perfect, 0, true)]
        [TestCase(WeaponAffixTier.Standard, 1, true)]
        public void Tension_is_reserved_for_high_perfect_or_potential(WeaponAffixTier tier, int potentialCount, bool expected)
        {
            var presenter = new GameObject("Tension Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.Play(Result(tier, potentialCount));
            Assert.That(presenter.IsTensionActive, Is.EqualTo(expected));
            Object.DestroyImmediate(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Tension_changes_the_reel_transform_but_standard_does_not()
        {
            var presenter = new GameObject("Tension Motion Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Result(WeaponAffixTier.Standard, 0));
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(presenter.TensionScale, Is.EqualTo(1f));
            var highResult = Result(WeaponAffixTier.High, 0);
            var highTimeline = WeaponAffixRevealTimeline.For(highResult);
            presenter.Play(highResult);
            yield return new WaitForSecondsRealtime(highTimeline.ReadStartsAt + .08f);
            Assert.That(presenter.TensionScale, Is.Not.EqualTo(1f));
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Final_affix_is_hidden_while_spinning_then_locks_in()
        {
            var presenter = new GameObject("Slot Phase Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Result(WeaponAffixTier.Standard, 0));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(presenter.Phase, Is.EqualTo(WeaponAffixRevealPresenter.RevealPhase.Spinning));
            Assert.That(presenter.IsFinalAffixVisible, Is.False);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(presenter.IsFinalAffixVisible, Is.True);
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Jackpot_lines_unlock_in_order()
        {
            var presenter = new GameObject("Slot Lines Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            var result = Result(WeaponAffixTier.Perfect, 3);
            var timeline = WeaponAffixRevealTimeline.For(result);
            presenter.Play(result);
            yield return new WaitForSecondsRealtime(timeline.PotentialStopsAt(0) + .04f);
            Assert.That(presenter.VisiblePotentialCount, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(
                timeline.PotentialStopsAt(1) - timeline.PotentialStopsAt(0) + .02f);
            Assert.That(presenter.VisiblePotentialCount, Is.EqualTo(2));
            yield return new WaitForSecondsRealtime(
                timeline.PotentialStopsAt(2) - timeline.PotentialStopsAt(1) + .02f);
            Assert.That(presenter.VisiblePotentialCount, Is.EqualTo(3));
            Object.Destroy(presenter.gameObject);
        }

        private static WeaponAffixRollResult Result(WeaponAffixTier tier, int potentialCount)
        {
            var potentials = new WeaponPotentialId[potentialCount];
            for (var index = 0; index < potentialCount; index++)
                potentials[index] = new WeaponPotentialId("test_potential_" + index);
            return new WeaponAffixRollResult(new WeaponAffixRoll(WeaponAffixStat.Damage, tier, .2d), potentials);
        }

        private static JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset TestCatalog()
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
            var catalog = ScriptableObject.CreateInstance<JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset>();
            catalog.SetSlotKitForTests(sprite, sprite, sprite, sprite, sprite);
            catalog.SetAppraisalKitForImport(sprite, sprite, sprite, sprite);
            return catalog;
        }

        private static Image ImageNamed(Component root, string objectName) =>
            RectNamed(root, objectName).GetComponent<Image>();

        private static RectTransform RectNamed(Component root, string objectName)
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == objectName)
                    return rect;
            Assert.Fail("Missing UI object: " + objectName);
            return null;
        }

        private static string TextValue(RectTransform rect)
        {
            var text = rect.GetComponent("TextMeshProUGUI");
            Assert.That(text, Is.Not.Null, rect.name);
            return (string)text.GetType().GetProperty("text").GetValue(text);
        }

        private static Color TextColor(RectTransform rect)
        {
            var text = rect.GetComponent("TextMeshProUGUI");
            Assert.That(text, Is.Not.Null, rect.name);
            return (Color)text.GetType().GetProperty("color").GetValue(text);
        }

        private static WeaponAppraisalViewModel Model(
            int level,
            ProgressionRewardKind kind,
            WeaponAffixTier tier)
        {
            var result = new WeaponAffixRollResult(
                new WeaponAffixRoll(WeaponAffixStat.Damage, tier, 23.88d),
                System.Array.Empty<WeaponPotentialId>());
            var reward = new ProgressionRewardEvent(
                WeaponId.HwandoFlyingBlade.Value,
                WeaponId.HwandoFlyingBlade.Value,
                level,
                kind,
                "Hwando Flying Blade",
                "Level " + level,
                null,
                result);
            var slot = new WeaponSlotView(
                WeaponId.HwandoFlyingBlade.Value,
                "Hwando Flying Blade",
                level,
                null,
                "Damage +24%",
                behavior: "Returning blade");
            return WeaponAppraisalViewModel.From(reward, slot);
        }

        private static IEnumerator ChooseThroughVisibleCard(FirstPlayableController controller, UpgradeChoicePresenter choice, UpgradeOffer offer)
        {
            controller.OpenUpgradeForTests();
            controller.SetUpgradeOffersForTests(offer);
            yield return new WaitForSecondsRealtime(.35f);
            var card = choice.GetComponentInChildren<Button>(true);
            ExecuteEvents.Execute<IPointerClickHandler>(card.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.2f);
        }
    }
}
