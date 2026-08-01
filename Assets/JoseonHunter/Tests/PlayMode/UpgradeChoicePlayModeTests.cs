using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class UpgradeChoicePlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Upgrade_open_animates_on_unscaled_time_without_owning_game_time()
        {
            var go = new GameObject("Upgrade Presenter");
            var presenter = go.AddComponent<UpgradeChoicePresenter>();
            presenter.BuildForTests();
            presenter.Open(Choices(), _ => true);

            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            yield return new WaitForSecondsRealtime(.20f);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(presenter.IsOpen, Is.True);

            presenter.CloseImmediately();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator First_accepted_card_click_locks_further_choices_until_closed()
        {
            var go = new GameObject("Upgrade Presenter");
            var presenter = go.AddComponent<UpgradeChoicePresenter>();
            presenter.BuildForTests();
            var calls = 0;
            presenter.Open(Choices(), _ =>
            {
                calls++;
                return true;
            });

            yield return new WaitForSecondsRealtime(.35f);
            var cards = go.GetComponentsInChildren<Button>(true);
            Assert.That(cards, Has.Length.EqualTo(3));
            cards[0].onClick.Invoke();
            cards[1].onClick.Invoke();

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(presenter.IsChoiceLocked, Is.True);

            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(presenter.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Cards_use_three_portrait_rows_within_the_modal_width()
        {
            var go = new GameObject("Portrait Upgrade Presenter");
            var presenter = go.AddComponent<UpgradeChoicePresenter>();
            presenter.BuildForTests();
            var cards = go.GetComponentsInChildren<Button>(true);

            Assert.That(cards, Has.Length.EqualTo(3));
            Assert.That(cards[0].GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(936f, 236f)));
            Assert.That(cards[0].GetComponent<RectTransform>().anchoredPosition.y, Is.GreaterThan(0f));
            Assert.That(cards[1].GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(0f));
            Assert.That(cards[2].GetComponent<RectTransform>().anchoredPosition.y, Is.LessThan(0f));
            Assert.That(cards[0].GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(0f));
            Object.Destroy(go);
            yield return null;
        }

        [Test]
        public void Cards_have_an_opaque_hanji_interior_and_a_centered_text_safe_area()
        {
            var go = new GameObject("Upgrade Card Readability Test");
            var presenter = go.AddComponent<UpgradeChoicePresenter>();
            presenter.BuildForTests();
            presenter.Open(Choices(), _ => true);
            var card = go.GetComponentsInChildren<Button>(true)[0];
            var fill = card.transform.Find("Hanji Interior")?.GetComponent<Image>();

            Assert.That(fill, Is.Not.Null, "The transparent frame needs an opaque interior layer.");
            Assert.That(fill.color.a, Is.EqualTo(1f));
            Assert.That(fill.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(fill.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(fill.rectTransform.offsetMin.x, Is.GreaterThanOrEqualTo(14f));
            Assert.That(fill.rectTransform.offsetMax.x, Is.LessThanOrEqualTo(-14f));

            foreach (var labelName in new[] { "Category", "Name", "Behavior", "Delta" })
            {
                var label = card.transform.Find(labelName)?.GetComponent<RectTransform>();
                Assert.That(label, Is.Not.Null, labelName);
                Assert.That(label.anchoredPosition.x, Is.GreaterThanOrEqualTo(210f), labelName);
                Assert.That(label.anchoredPosition.x + label.sizeDelta.x,
                    Is.LessThanOrEqualTo(880f), labelName);
            }
            Assert.That(card.transform.Find("Category").GetComponent<Graphic>().color.grayscale,
                Is.LessThan(.5f));
            Assert.That(card.transform.Find("Delta").GetComponent<Graphic>().color.grayscale,
                Is.LessThan(.5f));

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator Rejected_card_click_releases_the_choice_lock()
        {
            var go = new GameObject("Upgrade Presenter");
            var presenter = go.AddComponent<UpgradeChoicePresenter>();
            presenter.BuildForTests();
            var calls = 0;
            presenter.Open(Choices(), _ => ++calls > 1);

            yield return new WaitForSecondsRealtime(.35f);
            var cards = go.GetComponentsInChildren<Button>(true);
            cards[0].onClick.Invoke();
            Assert.That(presenter.IsChoiceLocked, Is.False);
            cards[1].onClick.Invoke();

            Assert.That(calls, Is.EqualTo(2));
            Assert.That(presenter.IsChoiceLocked, Is.True);
            Object.Destroy(go);
        }

        private static UpgradeChoiceState Choices() => new UpgradeChoiceState(2, new[]
        {
            new UpgradeChoiceView("gakgung_shot", UpgradeKind.Weapon, 1, "NEW WEAPON", "GAKGUNG", "Piercing arrow attack", "UNLOCK", null),
            new UpgradeChoiceView("boots", UpgradeKind.Support, 1, "SUPPORT", "LIGHT STEPS", "Move faster", "+12%", null),
            new UpgradeChoiceView("talisman", UpgradeKind.Support, 1, "SUPPORT", "TALISMAN", "Increase maximum health", "+20", null)
        });
    }
}
