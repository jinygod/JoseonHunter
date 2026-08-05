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
        [TestCase(WeaponAffixTier.Standard, 0, 2.55f)]
        [TestCase(WeaponAffixTier.High, 0, 2.81f)]
        [TestCase(WeaponAffixTier.Perfect, 0, 2.85f)]
        [TestCase(WeaponAffixTier.Standard, 1, 2.91f)]
        [TestCase(WeaponAffixTier.Standard, 2, 3.09f)]
        [TestCase(WeaponAffixTier.Standard, 3, 3.27f)]
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
            Assert.That(TextValue(RectNamed(presenter, "Affix Title")), Is.EqualTo("추가옵션 감정 중"));
            Assert.That(RectNamed(presenter, "Rarity Seal Label").gameObject.activeSelf, Is.False);
            Assert.That(TextValue(RectNamed(presenter, "Confirm Label")), Is.EqualTo("확인"));
            var growthGuide = RectNamed(presenter, "Growth Guide");
            Assert.That(growthGuide.anchoredPosition, Is.EqualTo(new Vector2(80f, 202f)));
            Assert.That(growthGuide.sizeDelta, Is.EqualTo(new Vector2(620f, 20f)));
            Assert.That(TextValue(growthGuide),
                Is.EqualTo("무기 3레벨에 성장 방식을 선택하고, 4·5레벨에 선택한 효과가 강화됩니다."));
            Assert.That(RectNamed(presenter, "Reel Window 0").anchoredPosition.y, Is.EqualTo(126f));
            Assert.That(presenter.PotentialRowY(0), Is.EqualTo(-32f));
            Assert.That(presenter.PotentialRowY(1), Is.EqualTo(-160f));
            Assert.That(presenter.PotentialRowY(2), Is.EqualTo(-288f));
            Assert.That(RectNamed(presenter, "Confirm Result").anchoredPosition.y, Is.EqualTo(-385f));
            Object.DestroyImmediate(presenter.gameObject);
        }

        [Test]
        public void Appraisal_uses_warm_flat_rows_without_ornate_slot_sprites()
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
                Assert.That(window.sprite, Is.Null, "Reel " + index);
                var expected = index == 0
                    ? new Color(.22f, .14f, .09f, 1f)
                    : new Color(.82f, .74f, .57f, 1f);
                Assert.That(window.color, Is.EqualTo(expected), "Reel " + index);
            }
            Assert.That(ImageNamed(presenter, "Locked Potential 1").sprite, Is.Null);
            Assert.That(ImageNamed(presenter, "Stop Flash 0").sprite, Is.Null);
            Assert.That(TextValue(RectNamed(presenter, "Rarity Seal Label")), Is.EqualTo("최대"));
            var growth = RectNamed(presenter, "Growth Summary Row");
            Assert.That(growth.gameObject.activeSelf, Is.True);
            Assert.That(TextValue(growth), Does.Contain("성장 방식"));
            Assert.That(TextValue(growth), Does.Contain("선택 전"));
            Assert.That(TextColor(growth).grayscale, Is.LessThan(.5f));

            Object.DestroyImmediate(presenter.gameObject);
        }

        [Test]
        public void Accumulated_summary_and_rows_have_dedicated_vertical_space()
        {
            var presenter = new GameObject("Appraisal Spacing Test")
                .AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.ShowDetails(new WeaponSlotView(
                WeaponId.HwandoFlyingBlade.Value, "Hwando Flying Blade", 2, null,
                "재사용 대기시간 -8%", behavior: "Returning blade"));

            var main = RectNamed(presenter, "Reel Window 0");
            var summary = RectNamed(presenter, "Effect Summary Title");
            var firstPotential = RectNamed(presenter, "Reel Window 1");
            var lastPotential = RectNamed(presenter, "Reel Window 3");
            var confirm = RectNamed(presenter, "Confirm Result");

            Assert.That(Bottom(main) - Top(summary), Is.GreaterThanOrEqualTo(6f));
            Assert.That(Bottom(summary) - Top(firstPotential), Is.GreaterThanOrEqualTo(6f));
            Assert.That(Bottom(lastPotential) - Top(confirm), Is.GreaterThanOrEqualTo(6f));
            Assert.That(summary.anchoredPosition, Is.EqualTo(new Vector2(0f, 44f)));
            Assert.That(summary.sizeDelta, Is.EqualTo(new Vector2(740f, 24f)));

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
            Assert.That(TextValue(RectNamed(presenter, "Affix Title")), Is.EqualTo("추가옵션 감정 중"));
            Assert.That(RectNamed(presenter, "Rarity Seal Label").gameObject.activeSelf, Is.False);
            Assert.That(RectNamed(presenter, "Confirm Result").gameObject.activeSelf, Is.False);

            presenter.PreviewAtForEditor(result, (timeline.CountStartsAt + timeline.CountEndsAt) * .5f);
            Assert.That(presenter.DisplayedAffixText, Is.Not.EqualTo("공격 범위 +0%"));
            Assert.That(presenter.DisplayedAffixText, Is.Not.EqualTo("공격 범위 +20%"));
            Assert.That(RectNamed(presenter, "Confirm Result").gameObject.activeSelf, Is.False);

            presenter.PreviewAtForEditor(result, timeline.CountEndsAt);
            Assert.That(presenter.DisplayedAffixText, Is.EqualTo("공격 범위 +20%"));
            Assert.That(TextValue(RectNamed(presenter, "Affix Title")), Is.EqualTo("추가옵션 감정 중"));
            Assert.That(RectNamed(presenter, "Rarity Seal Label").gameObject.activeSelf, Is.False);
            Assert.That(RectNamed(presenter, "Confirm Result").gameObject.activeSelf, Is.False);

            presenter.PreviewAtForEditor(result, timeline.TierRevealsAt);
            Assert.That(TextValue(RectNamed(presenter, "Affix Title")), Is.EqualTo("최대 추가옵션"));
            Assert.That(TextValue(RectNamed(presenter, "Rarity Seal Label")), Is.EqualTo("최대"));
            Assert.That(RectNamed(presenter, "Rarity Seal Label").gameObject.activeSelf, Is.True);

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
                behavior: "Returning blade",
                legacyName: "빙무",
                legacyStageName: "선택",
                nextLegacyMilestone: "4레벨 · 강화"));

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
            Assert.That(TextValue(generalTitle), Is.EqualTo("현재 적용 효과"));
            Assert.That(TextColor(generalTitle).grayscale, Is.GreaterThan(.45f));
            Assert.That(TextColor(generalDetail).grayscale, Is.GreaterThan(.65f));
            Assert.That(ImageNamed(presenter, "Confirm Result").sprite, Is.Null);
            Assert.That(ImageNamed(presenter, "Confirm Result").color,
                Is.EqualTo(new Color(.22f, .14f, .09f, 1f)));

            for (var index = 1; index < 4; index++)
            {
                var window = ImageNamed(presenter, "Reel Window " + index);
                var viewport = RectNamed(presenter, "Reel Viewport " + index);
                Assert.That(window.enabled, Is.True, "Reel " + index);
                Assert.That(window.sprite, Is.Null, "Reel " + index);
                Assert.That(window.color, Is.EqualTo(new Color(.82f, .74f, .57f, 1f)));
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
            Assert.That(TextValue(RectNamed(presenter, "Affix Summary Row")),
                Is.EqualTo("누적 추가옵션\nDamage +24%"));
            Assert.That(TextValue(RectNamed(presenter, "Growth Summary Row")),
                Is.EqualTo("성장 방식\n빙무 · 선택"));
            Assert.That(TextValue(RectNamed(presenter, "Potential Summary Row")),
                Is.EqualTo("잠재 능력\n독니"));
            Assert.That(TextColor(RectNamed(presenter, "Growth Summary Row")).grayscale, Is.LessThan(.5f));

            Object.DestroyImmediate(presenter.gameObject);
        }

        [Test]
        public void Completed_growth_uses_a_final_state_row_instead_of_promising_another_upgrade()
        {
            var presenter = new GameObject("Completed Growth Detail Test")
                .AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.ShowDetails(new WeaponSlotView(
                WeaponId.GakgungShot.Value, "각궁", 5, null,
                legacyName: "관일", legacyStageName: "최종 효과 완성",
                nextLegacyMilestone: "최종 효과 적용 중"));

            Assert.That(TextValue(RectNamed(presenter, "Growth Summary Row")),
                Is.EqualTo("성장 방식\n관일 · 최종 효과 완성"));
            Object.DestroyImmediate(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator RepeatUpgradeOpensQuicklyAndShowsAccumulatedTotal()
        {
            var presenter = new GameObject("Repeat Appraisal Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Model(2, ProgressionRewardKind.WeaponLevel, WeaponAffixTier.Standard));

            Assert.That(presenter.ScrollOpenFraction, Is.GreaterThan(.5f));
            Assert.That(presenter.AccumulatedSummary, Is.EqualTo("적용 후 누적 효과"));
            Assert.That(TextValue(RectNamed(presenter, "Affix Summary Row")),
                Does.Contain("Damage +24%"));
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
            Assert.That(controller.IsUpgradeOpen, Is.False);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(queuedOpens, Is.EqualTo(0));
            controller.TickGameplayIfRunningForTests(1.01f);
            yield return null;
            Assert.That(controller.IsUpgradeOpen, Is.True);
            Assert.That(queuedOpens, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Support_applies_immediately_while_evolution_keeps_generic_reward_reveal()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null; yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            var generic = Object.FindFirstObjectByType<RewardRevealPresenter>();
            var affix = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            affix.SetCatalogForTests(TestCatalog());

            yield return ChooseThroughVisibleCard(controller, choice, new UpgradeOffer("boots", UpgradeKind.Support, 1));
            Assert.That(generic.IsRevealing, Is.False);
            Assert.That(affix.IsRevealing, Is.False);
            controller.TickGameplayIfRunningForTests(1.01f);
            yield return null;

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
            var result = Result(WeaponAffixTier.Standard, 0);
            var timeline = WeaponAffixRevealTimeline.For(result);
            presenter.Play(result);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(presenter.Phase, Is.EqualTo(WeaponAffixRevealPresenter.RevealPhase.Spinning));
            Assert.That(presenter.IsFinalAffixVisible, Is.False);
            yield return new WaitForSecondsRealtime(timeline.TierRevealsAt - .3f + .04f);
            Assert.That(presenter.IsFinalAffixVisible, Is.True);
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Effect_summary_rows_appear_together_when_the_appraisal_can_be_read()
        {
            var presenter = new GameObject("Slot Lines Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            var result = Result(WeaponAffixTier.Perfect, 3);
            var timeline = WeaponAffixRevealTimeline.For(result);
            presenter.Play(result);
            yield return new WaitForSecondsRealtime(timeline.ReadStartsAt + .04f);
            Assert.That(RectNamed(presenter, "Affix Summary Row").gameObject.activeSelf, Is.True);
            Assert.That(RectNamed(presenter, "Growth Summary Row").gameObject.activeSelf, Is.True);
            Assert.That(RectNamed(presenter, "Potential Summary Row").gameObject.activeSelf, Is.True);
            Assert.That(presenter.VisiblePotentialCount, Is.EqualTo(0));
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

        private static float Top(RectTransform rect) =>
            rect.anchoredPosition.y + rect.sizeDelta.y * .5f;

        private static float Bottom(RectTransform rect) =>
            rect.anchoredPosition.y - rect.sizeDelta.y * .5f;

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
