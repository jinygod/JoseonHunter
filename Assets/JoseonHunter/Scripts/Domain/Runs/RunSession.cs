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
        private readonly HashSet<string> queuedWarnings = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> queuedSpawns = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> emittedSpawns = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> defeatedBosses = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<PendingTransition> pendingTransitions = new Queue<PendingTransition>();
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
            QueueCrossedTransitions();
            var transition = pendingTransitions.Count > 0 ? pendingTransitions.Dequeue() : default;
            if (transition.IsSpawn) emittedSpawns.Add(transition.BossId);

            if (clock.ElapsedSeconds >= profile.DurationSeconds && HasTimedOut())
                outcome = RunOutcome.DefeatTimeout;

            return CreateTick(
                transition.IsWarning ? transition.BossId : null,
                transition.IsSpawn ? transition.BossId : null);
        }

        public RunOutcome MarkBossDefeated(string bossId)
        {
            if (bossId == null) throw new ArgumentNullException(nameof(bossId));
            if (outcome != RunOutcome.InProgress) return outcome;

            foreach (var boss in profile.Bosses)
            {
                if (!string.Equals(boss.BossId, bossId, StringComparison.Ordinal)) continue;

                if (!emittedSpawns.Contains(bossId)) return outcome;
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
                    return !IsUntimedFinalFight(boss) && !defeatedBosses.Contains(boss.BossId);
            }

            return false;
        }

        private bool IsUntimedFinalFight(BossScheduleEntry finalBoss) =>
            finalBoss.SpawnSeconds >= profile.DurationSeconds;

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

        private void QueueCrossedTransitions()
        {
            while (TryFindNextCrossedTransition(out var transition))
            {
                pendingTransitions.Enqueue(transition);
                if (transition.IsWarning) queuedWarnings.Add(transition.BossId);
                else queuedSpawns.Add(transition.BossId);
            }
        }

        private bool TryFindNextCrossedTransition(out PendingTransition next)
        {
            next = default;
            var found = false;
            foreach (var boss in profile.Bosses)
            {
                if (clock.ElapsedSeconds >= boss.WarningSeconds && !queuedWarnings.Contains(boss.BossId))
                    ChooseEarlier(new PendingTransition(boss.BossId, boss.WarningSeconds, true), ref next, ref found);
                if (clock.ElapsedSeconds >= boss.SpawnSeconds && !queuedSpawns.Contains(boss.BossId))
                    ChooseEarlier(new PendingTransition(boss.BossId, boss.SpawnSeconds, false), ref next, ref found);
            }

            return found;
        }

        private static void ChooseEarlier(PendingTransition candidate, ref PendingTransition current, ref bool found)
        {
            if (!found || candidate.Seconds < current.Seconds ||
                (candidate.Seconds == current.Seconds && candidate.IsWarning && !current.IsWarning))
            {
                current = candidate;
                found = true;
            }
        }

        private readonly struct PendingTransition
        {
            public PendingTransition(string bossId, float seconds, bool isWarning)
            {
                BossId = bossId;
                Seconds = seconds;
                IsWarning = isWarning;
            }

            public string BossId { get; }
            public float Seconds { get; }
            public bool IsWarning { get; }
            public bool IsSpawn => !IsWarning;
        }
    }
}
