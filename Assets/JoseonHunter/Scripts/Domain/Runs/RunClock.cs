using System;

namespace JoseonHunter.Domain.Runs
{
    public sealed class RunClock
    {
        private float elapsedSeconds;

        public RunClock(float maximumSeconds = 240f)
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
            return ToPhase(elapsedSeconds);
        }

        private static RunPhase ToPhase(float seconds)
        {
            if (seconds >= 240f) return RunPhase.Expired;
            if (seconds >= 180f) return RunPhase.Boss;
            if (seconds >= 165f) return RunPhase.BossWarning;
            if (seconds >= 135f) return RunPhase.Peak;
            if (seconds >= 90f) return RunPhase.WaveThree;
            if (seconds >= 45f) return RunPhase.WaveTwo;
            return RunPhase.WaveOne;
        }
    }
}
