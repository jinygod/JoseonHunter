using System.Collections;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WeaponAffixProgressionPlayModeTests
    {
        [Test]
        public void Stable_seed_is_repeatable_and_includes_all_progression_inputs()
        {
            var seed = WeaponAffixRoller.StableSeed(WeaponId.GakgungShot, 2, 17, 4);
            Assert.That(WeaponAffixRoller.StableSeed(WeaponId.GakgungShot, 2, 17, 4), Is.EqualTo(seed));
            Assert.That(WeaponAffixRoller.StableSeed(WeaponId.GakgungShot, 3, 17, 4), Is.Not.EqualTo(seed));
            Assert.That(WeaponAffixRoller.StableSeed(WeaponId.TalismanThrow, 2, 17, 4), Is.Not.EqualTo(seed));
        }

        [UnityTest]
        public IEnumerator Weapon_offers_roll_once_rebuild_once_and_evolution_preserves_the_profile()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.SetAffixRandomFactoryForTests((_, _, _, _) => new JackpotAffixRandom());
            for (var weaponLevel = 1; weaponLevel <= 5; weaponLevel++)
            {
                var oldRuntime = controller.WeaponRuntime;
                var rebuilds = controller.WeaponRebuildCountForTests;
                controller.SetUpgradeOffersForTests(new UpgradeOffer(WeaponId.GakgungShot.Value, UpgradeKind.Weapon, weaponLevel));
                Assert.That(controller.TryChooseUpgrade(0), Is.True);
                if (weaponLevel == 3)
                {
                    Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.WeaponLegacySelection));
                    Assert.That(controller.TryChooseWeaponLegacy(WeaponLegacyPathId.GakgungSunPiercer), Is.True);
                }
                Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count, Is.EqualTo(weaponLevel));
                Assert.That(controller.WeaponRebuildCountForTests, Is.EqualTo(rebuilds + 1));
                Assert.That(oldRuntime.IsDisposedForTests, Is.True);
                Assert.That(controller.WeaponRuntime.RegistrationCountForTests(WeaponId.GakgungShot), Is.EqualTo(1));
                Assert.That(controller.CombatDamageService.AttachedAffixStatusesForTests, Is.SameAs(controller.WeaponRuntime.AffixStatuses));
            }

            var beforeEvolution = controller.WeaponRuntime;
            var profile = controller.AffixProfileForTests(WeaponId.GakgungShot);
            controller.SetUpgradeOffersForTests(new UpgradeOffer("gakgung_sun_piercer", UpgradeKind.Evolution, 5));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            Assert.That(beforeEvolution.IsDisposedForTests, Is.True);
            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count, Is.EqualTo(5));
            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).PotentialIds, Is.EqualTo(profile.PotentialIds));
            Assert.That(controller.WeaponRuntime.RegistrationCountForTests(WeaponId.GakgungShot), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Weapon_reward_exposes_the_exact_roll_result_stored_by_the_controller()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.SetAffixRandomFactoryForTests((_, _, _, _) => new FixedAffixRandom());
            ProgressionRewardEvent reward = default;
            controller.UpgradeChosen += candidate => reward = candidate;
            controller.SetAffixRandomFactoryForTests((_, _, _, _) => new JackpotAffixRandom());
            controller.SetUpgradeOffersForTests(new UpgradeOffer(WeaponId.GakgungShot.Value, UpgradeKind.Weapon, 1));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            Assert.That(reward.AffixResult, Is.Not.Null);
            Assert.That(reward.AffixResult.General,
                Is.EqualTo(controller.AffixProfileForTests(new WeaponId(reward.WeaponId)).GeneralRolls[^1]));
            Assert.That(reward.AffixResult.NewPotentials,
                Is.EqualTo(controller.AffixProfileForTests(new WeaponId(reward.WeaponId)).PotentialIds));
        }

        [Test]
        public void Weapon_signature_changes_for_affix_totals_and_potentials()
        {
            var baseState = new FirstPlayableUiState(1, 0, 1, 0, 0, 0f, 1f, 1f, 1f, false, false, 0f, 0f,
                new[] { new WeaponSlotView("gakgung_shot", "Gakgung", 1, null) });
            var affixedState = new FirstPlayableUiState(1, 0, 1, 0, 0, 0f, 1f, 1f, 1f, false, false, 0f, 0f,
                new[] { new WeaponSlotView("gakgung_shot", "Gakgung", 1, null, "Damage +17%", new[] { WeaponPotentialId.GakgungFullDraw }) });
            Assert.That(FirstPlayableUiBootstrap.WeaponSignatureForTests(baseState),
                Is.Not.EqualTo(FirstPlayableUiBootstrap.WeaponSignatureForTests(affixedState)));
        }

        [Test]
        public void Weapon_signature_changes_when_only_the_affix_tier_changes()
        {
            var standard = new FirstPlayableUiState(1, 0, 1, 0, 0, 0f, 1f, 1f, 1f, false, false, 0f, 0f,
                new[] { new WeaponSlotView("gakgung_shot", "Gakgung", 1, null, "Damage +17%", null, new[] { WeaponAffixTier.Standard }) });
            var perfect = new FirstPlayableUiState(1, 0, 1, 0, 0, 0f, 1f, 1f, 1f, false, false, 0f, 0f,
                new[] { new WeaponSlotView("gakgung_shot", "Gakgung", 1, null, "Damage +17%", null, new[] { WeaponAffixTier.Perfect }) });
            Assert.That(FirstPlayableUiBootstrap.WeaponSignatureForTests(standard),
                Is.Not.EqualTo(FirstPlayableUiBootstrap.WeaponSignatureForTests(perfect)));
        }

        [UnityTest]
        public IEnumerator Run_reset_clears_weapon_affix_profiles()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.SetAffixRandomFactoryForTests((_, _, _, _) => new JackpotAffixRandom());
            controller.SetUpgradeOffersForTests(new UpgradeOffer(WeaponId.GakgungShot.Value, UpgradeKind.Weapon, 1));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            var rolls = controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count;
            controller.SetUpgradeOffersForTests(new UpgradeOffer("boots", UpgradeKind.Support, 1));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count, Is.EqualTo(rolls));

            controller.SetWeaponLevelForTests(WeaponId.GakgungShot, 1);
            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count, Is.EqualTo(rolls));

            controller.ResetRunForTests();
            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot), Is.Null);
        }

        private sealed class FixedAffixRandom : IAffixRandom
        {
            public double NextUnit() => .5d;
            public int NextIndex(int exclusiveMax) => 0;
        }

        private sealed class JackpotAffixRandom : IAffixRandom
        {
            private int calls;
            public double NextUnit() => calls++ == 0 ? .5d : 0d;
            public int NextIndex(int exclusiveMax) => 0;
        }
    }
}
