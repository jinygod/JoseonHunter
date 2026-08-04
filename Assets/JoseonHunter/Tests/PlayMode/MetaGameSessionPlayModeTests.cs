using System.Collections;
using JoseonHunter.Domain.Save;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class MetaGameSessionPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator SessionLoadsInjectedRepositoryAndSurvivesSceneChange()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 432;
            var session = MetaGameSession.EnsureExists(new MemoryRepository(data));

            SceneManager.LoadScene("Lobby");
            yield return null;
            SceneManager.LoadScene("Gameplay");
            yield return null;

            Assert.That(MetaGameSession.Current, Is.SameAs(session));
            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(432));
        }

        [Test]
        public void ActiveLoadoutNormalizesLockedAndMismatchedStylesToBase()
        {
            var data = SaveDataV1.CreateDefaults();
            data.PatrolLoadouts[0].StartingWeaponId = "gakgung_shot";
            data.PatrolLoadouts[0].WeaponStyleIds["gakgung_shot"] = "hwando_venom";
            var session = MetaGameSession.EnsureExists(new MemoryRepository(data));

            Assert.That(session.ActiveLoadout.StartingWeapon.Value, Is.EqualTo("gakgung_shot"));
            Assert.That(session.ActiveLoadout.StyleFor(session.ActiveLoadout.StartingWeapon).Value, Is.Null);
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
