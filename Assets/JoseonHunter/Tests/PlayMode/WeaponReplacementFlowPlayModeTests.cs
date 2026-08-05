using System.Collections;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WeaponReplacementFlowPlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator Full_loadout_can_cancel_replacement_without_mutating_the_run()
        {
            yield return LoadGameplay();
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            FillFourWeaponSlots(controller);
            WeaponReplacementState replacement = null;
            controller.WeaponReplacementOpened += state => replacement = state;

            controller.OpenUpgradeOffersForTests(new UpgradeOffer(
                WeaponId.FrostFlask.Value, UpgradeKind.Weapon, 1, requiresReplacement: true));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);

            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.WeaponReplacement));
            Assert.That(replacement, Is.Not.Null);
            Assert.That(replacement.Choices, Has.Count.EqualTo(4));
            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(0));
            Assert.That(controller.HasWeaponForTests(WeaponId.FrostFlask), Is.False);

            Assert.That(controller.CancelWeaponReplacement(), Is.True);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.LevelUpSelection));
            Assert.That(controller.CurrentOffers, Has.Count.EqualTo(1));
            Assert.That(controller.CurrentOffers[0].Id, Is.EqualTo(WeaponId.FrostFlask.Value));
            Assert.That(controller.HasWeaponForTests(WeaponId.FrostFlask), Is.False);
            Assert.That(controller.IsWeaponDiscardedForTests(WeaponId.HwandoFlyingBlade), Is.False);
        }

        [UnityTest]
        public IEnumerator Level_four_discard_creates_level_three_weapon_then_requires_its_legacy()
        {
            yield return LoadGameplay();
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            FillFourWeaponSlots(controller);
            WeaponLegacyChoiceState legacyChoice = null;
            controller.WeaponLegacyOpened += state => legacyChoice = state;

            controller.OpenUpgradeOffersForTests(new UpgradeOffer(
                WeaponId.FrostFlask.Value, UpgradeKind.Weapon, 1, requiresReplacement: true));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            Assert.That(controller.TryChooseWeaponReplacement(WeaponId.HwandoFlyingBlade.Value), Is.True);

            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.WeaponLegacySelection));
            Assert.That(controller.HasWeaponForTests(WeaponId.HwandoFlyingBlade), Is.False);
            Assert.That(controller.IsWeaponDiscardedForTests(WeaponId.HwandoFlyingBlade), Is.True);
            Assert.That(controller.WeaponLevelForTests(WeaponId.FrostFlask), Is.EqualTo(3));
            Assert.That(legacyChoice, Is.Not.Null);
            Assert.That(legacyChoice.Choices, Has.Count.EqualTo(2));
            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(0));

            Assert.That(controller.TryChooseWeaponLegacy(WeaponLegacyPathId.FrostMist), Is.True);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.AugmentResult));
            Assert.That(controller.LegacySnapshotForTests(WeaponId.FrostFlask).PathId,
                Is.EqualTo(WeaponLegacyPathId.FrostMist));
            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(1));
            Assert.That(controller.AffixProfileForTests(WeaponId.FrostFlask).GeneralRolls, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Invalid_replacement_does_not_mutate_the_loadout()
        {
            yield return LoadGameplay();
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            FillFourWeaponSlots(controller);
            controller.OpenUpgradeOffersForTests(new UpgradeOffer(
                WeaponId.FrostFlask.Value, UpgradeKind.Weapon, 1, requiresReplacement: true));
            controller.TryChooseUpgrade(0);

            Assert.That(controller.TryChooseWeaponReplacement("not_owned"), Is.False);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.WeaponReplacement));
            Assert.That(controller.HasWeaponForTests(WeaponId.FrostFlask), Is.False);
            Assert.That(controller.IsWeaponDiscardedForTests(WeaponId.HwandoFlyingBlade), Is.False);
            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Discard_removes_affixes_and_legacy_evolution_compatibility_state()
        {
            yield return LoadGameplay();
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            FillFourWeaponSlots(controller);
            controller.SetAffixRandomFactoryForTests((_, _, _, _) => new FixedAffixRandom());
            controller.RollWeaponAffixForTests(WeaponId.HwandoFlyingBlade);
            controller.AcquireEvolutionForTests("hwando_moon_eclipse");
            Assert.That(controller.AffixProfileForTests(WeaponId.HwandoFlyingBlade), Is.Not.Null);
            Assert.That(controller.WeaponRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.True);

            controller.OpenUpgradeOffersForTests(new UpgradeOffer(
                WeaponId.FrostFlask.Value, UpgradeKind.Weapon, 1, requiresReplacement: true));
            controller.TryChooseUpgrade(0);
            controller.TryChooseWeaponReplacement(WeaponId.HwandoFlyingBlade.Value);
            controller.TryChooseWeaponLegacy(WeaponLegacyPathId.FrostMist);

            Assert.That(controller.AffixProfileForTests(WeaponId.HwandoFlyingBlade), Is.Null);
            Assert.That(controller.AcquiredEvolutionIds, Does.Not.Contain("hwando_moon_eclipse"));
            Assert.That(controller.WeaponRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.False);
        }

        private static void FillFourWeaponSlots(FirstPlayableController controller)
        {
            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 4);
            controller.SetWeaponLevelForTests(WeaponId.GakgungShot, 2);
            controller.SetWeaponLevelForTests(WeaponId.TalismanThrow, 2);
            controller.SetWeaponLevelForTests(WeaponId.ThunderCrashBomb, 2);
        }

        private static IEnumerator LoadGameplay()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            yield return null;
        }

        private sealed class FixedAffixRandom : IAffixRandom
        {
            public double NextUnit() => .5d;
            public int NextIndex(int exclusiveMax) => 0;
        }
    }
}
