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
        public IEnumerator PatrolEditorPersistsThreeIndependentPresetSelections()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindFirstObjectByType<PatrolPresenter>();

            presenter.SelectPresetForTests(0);
            presenter.SelectStartingWeaponForTests(WeaponId.GakgungShot);
            Assert.That(presenter.SaveForTests(), Is.True);
            presenter.SelectPresetForTests(1);
            presenter.SelectStartingWeaponForTests(WeaponId.FrostFlask);
            Assert.That(presenter.SaveForTests(), Is.True);

            Assert.That(MetaGameSession.Current.Data.PatrolLoadouts[0].StartingWeaponId,
                Is.EqualTo(WeaponId.GakgungShot.Value));
            Assert.That(MetaGameSession.Current.Data.PatrolLoadouts[1].StartingWeaponId,
                Is.EqualTo(WeaponId.FrostFlask.Value));
            Assert.That(MetaGameSession.Current.Data.PatrolLoadouts, Has.Count.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator PatrolHomePresentsStageAndLargePrimaryAction()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;

            var stage = GameObject.Find("Stage Name");
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("귀곡 야행"));

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
