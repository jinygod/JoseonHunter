using System.Collections;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Audio;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class GameMusicIntegrationPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LobbyStartsLobbyMusicAndGameplayStartsEarlyCombatMusic()
        {
            Assert.That(GameMusicDirector.Instance, Is.Not.Null);
            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.Lobby));

            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;

            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.CombatEarly));
        }

        [UnityTest]
        public IEnumerator StagePhasesAndBossesDriveMusicPriorityUntilVictory()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.CombatEarly));

            controller.AdvanceStageForTests(0f, 300f);
            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.MidBoss));

            controller.DefeatMidBossesForTests();
            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.CombatMid));

            controller.AdvanceStageForTests(300f, 600f);
            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.MidBoss));

            controller.DefeatMidBossesForTests();
            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.CombatLate));

            controller.AdvanceStageForTests(899f, 900f);
            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.FinalBoss));

            controller.DefeatFinalBossForTests();
            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.None));
        }
    }
}
