using System.Collections;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Domain.Save;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Runtime.Audio;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
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

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameMusicDirector.Instance != null)
            {
                GameMusicDirector.Instance.FadeOut(0f);
                Object.Destroy(GameMusicDirector.Instance.gameObject);
            }
            if (MetaGameSession.Current != null)
                Object.Destroy(MetaGameSession.Current.gameObject);

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

        [UnityTest]
        public IEnumerator LaterStagesStartTheirOwnThemeAndStillYieldToBossMusic()
        {
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
            LoadStage(StageId.DokkaebiPass);
            yield return null;
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();

            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.DokkaebiPass));
            controller.AdvanceStageForTests(0f, 300f);
            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.MidBoss));

            Object.DestroyImmediate(MetaGameSession.Current.gameObject);
            LoadStage(StageId.MoonlitTomb);
            yield return null;
            yield return null;

            Assert.That(GameMusicDirector.Instance.CurrentRole, Is.EqualTo(GameMusicRole.MoonlitTomb));
        }

        private static void LoadStage(StageId stageId)
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35)));
            if (stageId.Equals(StageId.MoonlitTomb))
                data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                    new StageSelection(StageId.DokkaebiPass, StageDifficulty.Normal), 900f, 500, 35)));
            data.SelectedStageId = stageId.Value;
            data.SelectedStageDifficulty = "normal";
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Gameplay");
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
