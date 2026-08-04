using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class PatrolLoadoutGameplayTests
    {
        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator SelectedStartingWeaponAndStyleReplaceHardCodedHwando()
        {
            var data = SaveDataV1.CreateDefaults();
            data.UnlockedWeaponStyles.Add(WeaponLegacyPathId.GakgungSunPiercer.Value);
            data.PatrolLoadouts[0].StartingWeaponId = WeaponId.GakgungShot.Value;
            data.PatrolLoadouts[0].WeaponStyleIds[WeaponId.GakgungShot.Value] =
                WeaponLegacyPathId.GakgungSunPiercer.Value;
            MetaGameSession.EnsureExists(new MemoryRepository(data));

            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();

            Assert.That(controller.RegisteredWeaponIds.Single(), Is.EqualTo(WeaponId.GakgungShot));
            Assert.That(controller.LegacySnapshotForTests(WeaponId.GakgungShot).PathId,
                Is.EqualTo(WeaponLegacyPathId.GakgungSunPiercer));
        }

        [UnityTest]
        public IEnumerator RankFiveCommonTrainingAppliesEachBonusAtTenPercentCap()
        {
            var data = SaveDataV1.CreateDefaults();
            foreach (CommonTrainingId id in System.Enum.GetValues(typeof(CommonTrainingId)))
                data.CommonTrainingRanks[id.ToString()] = 5;
            MetaGameSession.EnsureExists(new MemoryRepository(data));

            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();

            Assert.That(controller.StartingMaximumHealthForTests, Is.EqualTo(110f).Within(.01f));
            Assert.That(controller.StartingDamageMultiplierForTests, Is.EqualTo(1.10f).Within(.001f));
            Assert.That(controller.StartingMoveSpeedForTests, Is.EqualTo(2.64f).Within(.001f));
            Assert.That(controller.StartingPickupRadiusForTests, Is.EqualTo(.638f).Within(.001f));
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data)
            {
                stored = data.Copy();
                return new SaveResult(true, SaveError.None);
            }
        }
    }
}
