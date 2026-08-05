using System;

namespace JoseonHunter.Domain.Runs
{
    public sealed class RunClock
    {
        private float elapsedSeconds;

        public RunClock(float maximumSeconds = 960f)
        {
            if (float.IsNaN(maximumSeconds) || float.IsInfinity(maximumSeconds) || maximumSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumSeconds), "Run clock maximum must be finite and positive.");
            MaximumSeconds = maximumSeconds;
        }

        public float ElapsedSeconds => elapsedSeconds;
        public float MaximumSeconds { get; }

        public RunPhase Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Run clock delta must be finite.");
            elapsedSeconds = Math.Min(MaximumSeconds, Math.Max(0f, elapsedSeconds + deltaSeconds));
            return PhaseAt(elapsedSeconds);
        }

        public static RunPhase PhaseAt(float seconds) =>
            PhaseAt(seconds, StagePacingTimeline.CanonicalDurationSeconds);

        public static RunPhase PhaseAt(float seconds, float maximumSeconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds), "Run time must be finite and non-negative.");
            if (float.IsNaN(maximumSeconds) || float.IsInfinity(maximumSeconds) || maximumSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumSeconds), "Run maximum must be finite and positive.");

            var timeline = StagePacingTimeline.ForDuration(maximumSeconds);
            if (seconds >= maximumSeconds + timeline.ToRunSeconds(60f)) return RunPhase.Expired;
            if (seconds >= timeline.ToRunSeconds(900f)) return RunPhase.Boss;
            if (seconds >= timeline.ToRunSeconds(840f)) return RunPhase.BossWarning;
            if (seconds >= timeline.ToRunSeconds(600f)) return RunPhase.Peak;
            if (seconds >= timeline.ToRunSeconds(300f)) return RunPhase.WaveThree;
            if (seconds >= timeline.ToRunSeconds(120f)) return RunPhase.WaveTwo;
            return RunPhase.WaveOne;
        }
    }
}
