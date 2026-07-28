using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WeaponAffixRevealPlayModeTests
    {
        [TestCase(WeaponAffixTier.Standard, 0, .95f)]
        [TestCase(WeaponAffixTier.High, 0, 1.15f)]
        [TestCase(WeaponAffixTier.Perfect, 0, 1.35f)]
        [TestCase(WeaponAffixTier.Standard, 1, 1.3f)]
        [TestCase(WeaponAffixTier.Standard, 2, 1.6f)]
        [TestCase(WeaponAffixTier.Standard, 3, 1.9f)]
        public void Duration_uses_the_exact_affix_and_jackpot_caps(WeaponAffixTier tier, int potentialCount, float expected)
        {
            Assert.That(WeaponAffixRevealPresenter.DurationFor(Result(tier, potentialCount)), Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator Skip_is_idempotent_and_does_not_change_the_roll_result()
        {
            var presenter = new GameObject("Affix Reveal Test").AddComponent<WeaponAffixRevealPresenter>();
            var result = Result(WeaponAffixTier.Perfect, 3);
            var completions = 0;
            presenter.RevealCompleted += () => completions++;
            Time.timeScale = 0f;
            presenter.Play(result);
            presenter.Skip(); presenter.Skip();
            yield return new WaitForSecondsRealtime(.72f);
            Assert.That(presenter.IsRevealing, Is.False);
            Assert.That(presenter.LastCompletedResult, Is.SameAs(result));
            Assert.That(completions, Is.EqualTo(1));
            Time.timeScale = 1f;
            Object.Destroy(presenter.gameObject);
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

        private static WeaponAffixRollResult Result(WeaponAffixTier tier, int potentialCount)
        {
            var potentials = new WeaponPotentialId[potentialCount];
            for (var index = 0; index < potentialCount; index++)
                potentials[index] = new WeaponPotentialId("test_potential_" + index);
            return new WeaponAffixRollResult(new WeaponAffixRoll(WeaponAffixStat.Damage, tier, .2d), potentials);
        }
    }
}
