using System.Collections;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Save;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class RunSettlementLobbyPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator DefeatPersistsCoinsAndMasteryOnceThenReturnsToLobby()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 100;
            var repository = new MemoryRepository(data);
            MetaGameSession.EnsureExists(repository);
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();

            controller.AwardRunProgressForTests(WeaponId.GakgungShot, 13, 21);
            controller.EndRunForTests(false);
            controller.ReturnToLobby();
            controller.ReturnToLobby();
            yield return WaitForScene("Lobby");

            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(121));
            Assert.That(MetaGameSession.Current.Data.WeaponMasteryPoints[WeaponId.GakgungShot.Value], Is.EqualTo(13));
            Assert.That(MetaGameSession.Current.Data.AccountExperience, Is.EqualTo(3));
            Assert.That(repository.SaveCalls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ConfirmedAbandonmentKeepsAllEarnedProgress()
        {
            var repository = new MemoryRepository(SaveDataV1.CreateDefaults());
            MetaGameSession.EnsureExists(repository);
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();

            controller.AwardRunProgressForTests(WeaponId.ThunderCrashBomb, 9, 7);
            controller.ConfirmAbandonAndReturn();
            yield return WaitForScene("Lobby");

            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(7));
            Assert.That(MetaGameSession.Current.Data.WeaponMasteryPoints[WeaponId.ThunderCrashBomb.Value], Is.EqualTo(9));
            Assert.That(repository.SaveCalls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FailedSettlementStaysPausedAndRetryDoesNotDuplicateRewards()
        {
            var repository = new MemoryRepository(SaveDataV1.CreateDefaults(), failFirstSave: true);
            MetaGameSession.EnsureExists(repository);
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();

            controller.AwardRunProgressForTests(WeaponId.FrostFlask, 4, 5);
            controller.EndRunForTests(false);
            Assert.That(controller.UiState.SettlementFailed, Is.True);
            Assert.That(MetaGameSession.Current.Data.Coins, Is.Zero);
            Assert.That(MetaGameSession.Current.Data.AccountExperience, Is.Zero);

            controller.ReturnToLobby();
            yield return WaitForScene("Lobby");
            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(5));
            Assert.That(MetaGameSession.Current.Data.WeaponMasteryPoints[WeaponId.FrostFlask.Value], Is.EqualTo(4));
            Assert.That(MetaGameSession.Current.Data.AccountExperience, Is.EqualTo(1));
            Assert.That(repository.SaveCalls, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator StageOneNormalVictoryResultExplainsBothNewlyUnlockedPaths()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();

            controller.AwardRunProgressForTests(WeaponId.GakgungShot, 10, 10);
            controller.EndRunForTests(true);
            yield return null;

            var summary = GameObject.Find("Result Summary")?.GetComponent<TMPro.TMP_Text>();
            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.text, Does.Contain("귀곡 들판 · 보통"));
            Assert.That(summary.text, Does.Contain("새 지역: 도깨비 고갯길 · 보통"));
            Assert.That(summary.text, Does.Contain("새 난이도: 귀곡 들판 · 흉조"));
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            for (var frame = 0; frame < 240 && SceneManager.GetActiveScene().name != sceneName; frame++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            private bool failFirstSave;

            public MemoryRepository(SaveDataV1 data, bool failFirstSave = false)
            {
                stored = data.Copy();
                this.failFirstSave = failFirstSave;
            }

            public int SaveCalls { get; private set; }
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data)
            {
                SaveCalls++;
                if (failFirstSave)
                {
                    failFirstSave = false;
                    return new SaveResult(false, SaveError.IoFailure);
                }
                stored = data.Copy();
                return new SaveResult(true, SaveError.None);
            }
        }
    }
}
