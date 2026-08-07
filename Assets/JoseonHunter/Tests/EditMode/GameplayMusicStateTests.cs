using JoseonHunter.Runtime.Audio;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameplayMusicStateTests
    {
        [Test]
        public void ResetStartsWithEarlyCombatMusic()
        {
            var state = new GameplayMusicState();

            state.Reset();

            Assert.That(state.CurrentRole, Is.EqualTo(GameMusicRole.CombatEarly));
        }

        [Test]
        public void PhaseChangesSelectTheMatchingCombatRole()
        {
            var state = new GameplayMusicState();
            state.Reset();

            state.SetPhase(CombatMusicPhase.Mid);
            Assert.That(state.CurrentRole, Is.EqualTo(GameMusicRole.CombatMid));

            state.SetPhase(CombatMusicPhase.Late);
            Assert.That(state.CurrentRole, Is.EqualTo(GameMusicRole.CombatLate));
        }

        [Test]
        public void MidBossOverridesThePhaseUntilAllMidBossesAreDefeated()
        {
            var state = new GameplayMusicState();
            state.Reset();
            state.SetPhase(CombatMusicPhase.Mid);

            state.EnterMidBoss();
            state.EnterMidBoss();
            Assert.That(state.CurrentRole, Is.EqualTo(GameMusicRole.MidBoss));

            state.ExitMidBoss();
            Assert.That(state.CurrentRole, Is.EqualTo(GameMusicRole.MidBoss));

            state.ExitMidBoss();
            state.ExitMidBoss();
            Assert.That(state.CurrentRole, Is.EqualTo(GameMusicRole.CombatMid));
        }

        [Test]
        public void FinalBossOverridesMidBossAndPhase()
        {
            var state = new GameplayMusicState();
            state.Reset();
            state.EnterMidBoss();

            state.EnterFinalBoss();

            Assert.That(state.CurrentRole, Is.EqualTo(GameMusicRole.FinalBoss));
        }

        [Test]
        public void EndRunSilencesEveryStateUntilReset()
        {
            var state = new GameplayMusicState();
            state.Reset();
            state.EnterFinalBoss();

            state.EndRun();
            state.SetPhase(CombatMusicPhase.Late);
            state.EnterMidBoss();

            Assert.That(state.CurrentRole, Is.EqualTo(GameMusicRole.None));

            state.Reset();
            Assert.That(state.CurrentRole, Is.EqualTo(GameMusicRole.CombatEarly));
        }
    }
}
