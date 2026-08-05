using System.Collections;
using System.Linq;
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
    public sealed class WeaponLegacyFlowPlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator Level_three_upgrade_waits_for_one_matching_legacy_choice()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            controller.SetWeaponLevelForTests(WeaponId.GakgungShot, 2);
            WeaponLegacyChoiceState opened = null;
            controller.WeaponLegacyOpened += state => opened = state;

            controller.OpenUpgradeOffersForTests(new UpgradeOffer(
                WeaponId.GakgungShot.Value, UpgradeKind.Weapon, 3));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);

            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.WeaponLegacySelection));
            Assert.That(controller.WeaponLevelForTests(WeaponId.GakgungShot), Is.EqualTo(2));
            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(0));
            Assert.That(opened, Is.Not.Null);
            Assert.That(opened.Choices, Has.Count.EqualTo(2));
            Assert.That(controller.TryChooseWeaponLegacy(WeaponLegacyPathId.FrostMist), Is.False);

            Assert.That(controller.TryChooseWeaponLegacy(WeaponLegacyPathId.GakgungSunPiercer), Is.True);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.AugmentResult));
            Assert.That(controller.WeaponLevelForTests(WeaponId.GakgungShot), Is.EqualTo(3));
            Assert.That(controller.LegacySnapshotForTests(WeaponId.GakgungShot).Stage,
                Is.EqualTo(WeaponLegacyStage.Chosen));
            var chosen = controller.UiState.Weapons.Single(weapon => weapon.Id == WeaponId.GakgungShot.Value);
            Assert.That(chosen.LegacyStageName, Is.EqualTo("성장 방향 선택 완료"));
            Assert.That(chosen.NextLegacyMilestone, Is.EqualTo("무기 4레벨 달성 시 효과 강화"));

            controller.SetWeaponLevelForTests(WeaponId.GakgungShot, 4);
            var reinforced = controller.UiState.Weapons.Single(weapon => weapon.Id == WeaponId.GakgungShot.Value);
            Assert.That(reinforced.LegacyStageName, Is.EqualTo("선택 효과 강화됨"));
            Assert.That(reinforced.NextLegacyMilestone, Is.EqualTo("무기 5레벨 달성 시 최종 효과 완성"));

            controller.SetWeaponLevelForTests(WeaponId.GakgungShot, 5);
            var completed = controller.UiState.Weapons.Single(weapon => weapon.Id == WeaponId.GakgungShot.Value);
            Assert.That(completed.LegacyStageName, Is.EqualTo("최종 효과 완성"));
            Assert.That(completed.NextLegacyMilestone, Is.EqualTo("최종 효과 적용 중"));
            Assert.That(controller.TryChooseWeaponLegacy(WeaponLegacyPathId.GakgungSplitFletching), Is.False);
            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Run_reset_clears_selected_and_discarded_legacy_state()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            controller.SetWeaponLevelForTests(WeaponId.GakgungShot, 2);
            controller.OpenUpgradeOffersForTests(new UpgradeOffer(
                WeaponId.GakgungShot.Value, UpgradeKind.Weapon, 3));
            controller.TryChooseUpgrade(0);
            controller.TryChooseWeaponLegacy(WeaponLegacyPathId.GakgungSunPiercer);

            controller.ResetRunForTests();

            Assert.That(controller.LegacySnapshotForTests(WeaponId.GakgungShot).HasPath, Is.False);
            Assert.That(controller.IsWeaponDiscardedForTests(WeaponId.GakgungShot), Is.False);
        }
    }
}
