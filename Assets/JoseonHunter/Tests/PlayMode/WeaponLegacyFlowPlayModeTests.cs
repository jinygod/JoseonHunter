using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Domain.Save;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WeaponLegacyFlowPlayModeTests
    {
        [SetUp]
        public void ClearMetaSession()
        {
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [TearDown]
        public void RestoreState()
        {
            Time.timeScale = 1f;
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator Meta_equipped_venom_activates_only_at_levels_four_and_five()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();

            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 3);
            Assert.That(controller.LegacySnapshotForTests(
                WeaponId.HwandoFlyingBlade).Stage, Is.EqualTo(WeaponLegacyStage.None));
            var dormant = controller.UiState.Weapons.Single(
                weapon => weapon.Id == WeaponId.HwandoFlyingBlade.Value);
            Assert.That(dormant.LegacyName, Is.EqualTo("독니"));
            Assert.That(dormant.NextLegacyMilestone, Does.Contain("4레벨에 발현"));

            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 4);
            Assert.That(controller.LegacySnapshotForTests(
                WeaponId.HwandoFlyingBlade).Stage, Is.EqualTo(WeaponLegacyStage.Reinforced));
            var reinforced = controller.UiState.Weapons.Single(
                weapon => weapon.Id == WeaponId.HwandoFlyingBlade.Value);
            Assert.That(reinforced.LegacyStageName, Is.EqualTo("독니 발현"));
            Assert.That(reinforced.NextLegacyMilestone, Does.Contain("혈독난무"));

            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 5);
            Assert.That(controller.LegacySnapshotForTests(
                WeaponId.HwandoFlyingBlade).Stage, Is.EqualTo(WeaponLegacyStage.Completed));
            var completed = controller.UiState.Weapons.Single(
                weapon => weapon.Id == WeaponId.HwandoFlyingBlade.Value);
            Assert.That(completed.LegacyStageName, Is.EqualTo("혈독난무 완성"));
        }

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

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;

            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() =>
                new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data)
            {
                stored = data.Copy();
                return new SaveResult(true, SaveError.None);
            }
        }
    }
}
