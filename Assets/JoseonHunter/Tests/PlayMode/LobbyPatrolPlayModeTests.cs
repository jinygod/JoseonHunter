using System.Collections;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Save;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class LobbyPatrolPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator CyclingCurrentWeaponImmediatelySavesActiveLoadout()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindFirstObjectByType<PatrolPresenter>();

            presenter.SelectStartingWeaponForTests(WeaponId.GakgungShot);

            var active = MetaGameSession.Current.Data.ActivePatrolLoadoutIndex;
            Assert.That(MetaGameSession.Current.Data.PatrolLoadouts[active].StartingWeaponId,
                Is.EqualTo(WeaponId.GakgungShot.Value));
            Assert.That(GameObject.Find("Previous Preset"), Is.Null);
            Assert.That(GameObject.Find("Next Preset"), Is.Null);
            Assert.That(GameObject.Find("Save Preset"), Is.Null);
        }

        [UnityTest]
        public IEnumerator PatrolHomePresentsStageAndLargePrimaryAction()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;

            var stage = GameObject.Find("Stage Name");
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("출전 준비"));

            var start = GameObject.Find("Start Patrol");
            Assert.That(start, Is.Not.Null);
            Assert.That(start.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("출전"));
            Assert.That(start.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(76f));
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data) { stored = data.Copy(); return new SaveResult(true, SaveError.None); }
        }
    }
}
