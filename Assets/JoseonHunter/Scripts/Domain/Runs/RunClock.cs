using System;

namespace JoseonHunter.Domain.Runs
{
    public sealed class RunClock
    {
        private float elapsedSeconds;

        public RunPhase Advance(float deltaSeconds)
        {
            elapsedSeconds = Math.Max(0f, elapsedSeconds + deltaSeconds);
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
