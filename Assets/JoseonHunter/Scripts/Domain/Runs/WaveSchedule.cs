using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Runs
{
    public enum RunPhase
    {
        WaveOne, WaveTwo, WaveThree, Peak, BossWarning, Boss, Expired
    }

    public readonly struct WaveDefinition
    {
        public WaveDefinition(int activeCap, IReadOnlyList<string> contentIds)
        {
            ActiveCap = activeCap;
            ContentIds = contentIds;
        }

        public int ActiveCap { get; }
        public IReadOnlyList<string> ContentIds { get; }
    }

    public static class WaveSchedule
    {
        public static WaveDefinition For(RunPhase phase) => phase switch
        {
            RunPhase.WaveOne => Definition(28, "plague_rat"),
            RunPhase.WaveTwo => Definition(36, "plague_rat", "vengeful_spirit"),
            RunPhase.WaveThree => Definition(48, "vengeful_spirit", "sakkat_specter"),
            RunPhase.Peak => Definition(64, "sakkat_specter", "dokkaebi", "bandit"),
            RunPhase.BossWarning => Definition(0, "fallen_general"),
            RunPhase.Boss => Definition(36, "fallen_general"),
            RunPhase.Expired => Definition(0),
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };

        private static WaveDefinition Definition(int activeCap, params string[] contentIds) =>
            new(activeCap, Array.AsReadOnly(contentIds));
    }
}
