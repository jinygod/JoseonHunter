using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Runs
{
    public enum RunOutcome
    {
        InProgress,
        Victory,
        DefeatTimeout,
        DefeatDeath
    }

    public readonly struct RunTick
    {
        public RunTick(float elapsedSeconds, float remainingSeconds, string bossWarningId, string bossSpawnId,
            bool normalWavesStopped, RunOutcome outcome)
        {
            ElapsedSeconds = elapsedSeconds;
            RemainingSeconds = remainingSeconds;
            BossWarningId = bossWarningId;
            BossSpawnId = bossSpawnId;
            NormalWavesStopped = normalWavesStopped;
            Outcome = outcome;
        }

        public float ElapsedSeconds { get; }
        public float RemainingSeconds { get; }
        public string BossWarningId { get; }
        public string BossSpawnId { get; }
        public bool NormalWavesStopped { get; }
        public RunOutcome Outcome { get; }
    }

    public sealed class RunSession
    {
        private readonly RunProfile profile;
        private readonly RunClock clock;
        private readonly HashSet<string> emittedWarnings = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> emittedSpawns = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> defeatedBosses = new HashSet<string>(StringComparer.Ordinal);
        private RunOutcome outcome;

        public RunSession(RunProfile profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            clock = new RunClock(profile.DurationSeconds);
            outcome = RunOutcome.InProgress;
        }

        public RunTick Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Run session delta must be finite.");

            if (outcome != RunOutcome.InProgress)
                return CreateTick(null, null);

            clock.Advance(deltaSeconds);
            string bossWarningId = null;
            string bossSpawnId = null;

            foreach (var boss in profile.Bosses)
            {
                if (clock.ElapsedSeconds >= boss.WarningSeconds && emittedWarnings.Add(boss.BossId))
                    bossWarningId = boss.BossId;
                if (clock.ElapsedSeconds >= boss.SpawnSeconds && emittedSpawns.Add(boss.BossId))
                    bossSpawnId = boss.BossId;
            }

            if (clock.ElapsedSeconds >= profile.DurationSeconds && HasTimedOut())
                outcome = RunOutcome.DefeatTimeout;

            return CreateTick(bossWarningId, bossSpawnId);
        }

        public RunOutcome MarkBossDefeated(string bossId)
        {
            if (bossId == null) throw new ArgumentNullException(nameof(bossId));
            if (outcome != RunOutcome.InProgress) return outcome;

            foreach (var boss in profile.Bosses)
            {
                if (!string.Equals(boss.BossId, bossId, StringComparison.Ordinal)) continue;

                defeatedBosses.Add(bossId);
                if (boss.IsFinal) outcome = RunOutcome.Victory;
                return outcome;
            }

            throw new ArgumentException("The boss is not part of this run profile.", nameof(bossId));
        }

        public RunOutcome MarkPlayerDefeated()
        {
            if (outcome == RunOutcome.InProgress) outcome = RunOutcome.DefeatDeath;
            return outcome;
        }

        private bool HasTimedOut()
        {
            foreach (var boss in profile.Bosses)
            {
                if (boss.IsFinal)
                    return boss.SpawnSeconds < profile.DurationSeconds && !defeatedBosses.Contains(boss.BossId);
            }

            return false;
        }

        private RunTick CreateTick(string bossWarningId, string bossSpawnId) => new RunTick(
            clock.ElapsedSeconds,
            Math.Max(0f, profile.DurationSeconds - clock.ElapsedSeconds),
            bossWarningId,
            bossSpawnId,
            HasFinalBossSpawned(),
            outcome);

        private bool HasFinalBossSpawned()
        {
            foreach (var boss in profile.Bosses)
            {
                if (boss.IsFinal) return emittedSpawns.Contains(boss.BossId);
            }

            return false;
        }
    }
}
