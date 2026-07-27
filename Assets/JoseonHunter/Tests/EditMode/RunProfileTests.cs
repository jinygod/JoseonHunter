using System;
using System.Linq;
using JoseonHunter.Domain.Runs;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class RunProfileTests
    {
        [Test]
        public void TestProfileWarnsThenSpawnsBossAtFiftySeconds()
        {
            var session = new RunSession(RunProfile.Test60Seconds());

            Assert.That(session.Advance(45f).BossWarningId, Is.EqualTo("fallen_general_test"));
            Assert.That(session.Advance(5f).BossSpawnId, Is.EqualTo("fallen_general_test"));
            Assert.That(session.Advance(1f).BossSpawnId, Is.Null);
        }

        [Test]
        public void ProductionProfileDefinesIncreasingFiveTenFifteenMinuteBosses()
        {
            var profile = RunProfile.Production15Minutes();

            Assert.That(profile.Bosses.Select(value => value.SpawnSeconds),
                Is.EqualTo(new[] { 300f, 600f, 900f }));
            Assert.That(profile.Bosses.Select(value => value.DifficultyTier),
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(profile.Bosses[2].IsFinal, Is.True);
        }

        [Test]
        public void NegativeDeltaDoesNotAdvanceTheSession()
        {
            var session = new RunSession(RunProfile.Test60Seconds());

            var tick = session.Advance(-1f);

            Assert.That(tick.ElapsedSeconds, Is.EqualTo(0f));
            Assert.That(tick.RemainingSeconds, Is.EqualTo(60f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteDeltaThrowsWithoutAdvancingTheSession(float deltaSeconds)
        {
            var session = new RunSession(RunProfile.Test60Seconds());

            Assert.That(() => session.Advance(deltaSeconds), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(session.Advance(45f).BossWarningId, Is.EqualTo("fallen_general_test"));
        }

        [Test]
        public void WarningAndSpawnAreEachEmittedOnlyOnce()
        {
            var session = new RunSession(RunProfile.Test60Seconds());

            Assert.That(session.Advance(45f).BossWarningId, Is.EqualTo("fallen_general_test"));
            Assert.That(session.Advance(0f).BossWarningId, Is.Null);
            Assert.That(session.Advance(5f).BossSpawnId, Is.EqualTo("fallen_general_test"));
            Assert.That(session.Advance(0f).BossSpawnId, Is.Null);
        }

        [Test]
        public void TestProfileTimesOutWhenItsBossIsStillAlive()
        {
            var session = new RunSession(RunProfile.Test60Seconds());

            var tick = session.Advance(60f);

            Assert.That(tick.ElapsedSeconds, Is.EqualTo(60f));
            Assert.That(tick.RemainingSeconds, Is.EqualTo(0f));
            Assert.That(tick.Outcome, Is.EqualTo(RunOutcome.DefeatTimeout));
        }

        [Test]
        public void PlayerDeathDefeatsImmediately()
        {
            var session = new RunSession(RunProfile.Test60Seconds());

            var outcome = session.MarkPlayerDefeated();

            Assert.That(outcome, Is.EqualTo(RunOutcome.DefeatDeath));
            Assert.That(session.Advance(1f).Outcome, Is.EqualTo(RunOutcome.DefeatDeath));
        }

        [Test]
        public void DefeatingANonFinalProductionBossContinuesTheRun()
        {
            var session = new RunSession(RunProfile.Production15Minutes());
            session.Advance(300f);
            session.Advance(0f);

            var outcome = session.MarkBossDefeated("boss_first");

            Assert.That(outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(session.Advance(285f).BossWarningId, Is.EqualTo("boss_second"));
        }

        [Test]
        public void DefeatingAnUnspawnedBossIsIgnored()
        {
            var session = new RunSession(RunProfile.Production15Minutes());

            Assert.That(session.MarkBossDefeated("boss_first"), Is.EqualTo(RunOutcome.InProgress));
            Assert.That(session.MarkBossDefeated("boss_final"), Is.EqualTo(RunOutcome.InProgress));
            Assert.That(session.Advance(300f).BossWarningId, Is.EqualTo("boss_first"));
            Assert.That(session.Advance(0f).BossSpawnId, Is.EqualTo("boss_first"));
            Assert.That(session.MarkBossDefeated("boss_first"), Is.EqualTo(RunOutcome.InProgress));
        }

        [Test]
        public void DefeatingTheFinalBossWins()
        {
            var session = new RunSession(RunProfile.Production15Minutes());
            session.Advance(900f);
            session.Advance(0f);
            session.Advance(0f);
            session.Advance(0f);
            session.Advance(0f);
            session.Advance(0f);

            Assert.That(session.MarkBossDefeated("boss_final"), Is.EqualTo(RunOutcome.Victory));
        }

        [Test]
        public void LargeDeltaQueuesEveryCrossedTransitionInChronologicalOrder()
        {
            var session = new RunSession(RunProfile.Production15Minutes());

            var ticks = new[]
            {
                session.Advance(900f),
                session.Advance(0f),
                session.Advance(0f),
                session.Advance(0f),
                session.Advance(0f),
                session.Advance(0f)
            };

            Assert.That(ticks.Select(tick => tick.BossWarningId ?? tick.BossSpawnId), Is.EqualTo(new[]
            {
                "boss_first", "boss_first", "boss_second", "boss_second", "boss_final", "boss_final"
            }));
            Assert.That(ticks.Select(tick => tick.BossWarningId != null), Is.EqualTo(new[]
            {
                true, false, true, false, true, false
            }));
        }

        [Test]
        public void FinalBossSpawnStopsNormalWavesForTheUntimedFight()
        {
            var session = new RunSession(RunProfile.Production15Minutes());

            var tick = session.Advance(900f);
            session.Advance(0f);
            session.Advance(0f);
            session.Advance(0f);
            session.Advance(0f);
            tick = session.Advance(0f);

            Assert.That(tick.NormalWavesStopped, Is.True);
            Assert.That(WaveSchedule.For(RunPhase.Boss, tick).ActiveCap, Is.EqualTo(0));
        }

        [Test]
        public void DuplicateAndUnsortedBossProfilesAreRejected()
        {
            Assert.That(() => new RunProfile(60f, new[]
            {
                new BossScheduleEntry("one", 10f, 20f, 1, false),
                new BossScheduleEntry("one", 30f, 40f, 2, true)
            }), Throws.ArgumentException);
            Assert.That(() => new RunProfile(60f, new[]
            {
                new BossScheduleEntry("two", 30f, 40f, 2, true),
                new BossScheduleEntry("one", 10f, 20f, 1, false)
            }), Throws.ArgumentException);
        }
    }
}
