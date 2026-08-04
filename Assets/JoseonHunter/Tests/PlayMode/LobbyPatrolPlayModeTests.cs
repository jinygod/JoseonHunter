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

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data) { stored = data.Copy(); return new SaveResult(true, SaveError.None); }
        }
    }
}
