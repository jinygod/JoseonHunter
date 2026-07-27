using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JoseonHunter.Domain.Geumjul
{
    public static class GeumjulMastery
    {
        public static MasteryState ForClosures(int successfulClosures, SealBranch selectedBranch = SealBranch.None) => new MasteryState(successfulClosures, selectedBranch);
    }

    public sealed class MasteryState
    {
        private static readonly IReadOnlyList<SealBranch> NoBranches = new ReadOnlyCollection<SealBranch>(new List<SealBranch>());
        private static readonly IReadOnlyList<SealBranch> ChoiceBranches = new ReadOnlyCollection<SealBranch>(new List<SealBranch> { SealBranch.FireMark, SealBranch.IceBind });

        internal MasteryState(int successfulClosures, SealBranch selectedBranch)
        {
            if (successfulClosures < 0) throw new ArgumentOutOfRangeException(nameof(successfulClosures));
            SuccessfulClosures = successfulClosures;
            if (successfulClosures >= 20)
            {
                if (selectedBranch != SealBranch.None && selectedBranch != SealBranch.FiveColorBarrier) throw new ArgumentException("Five-Color Barrier cannot select a Fire Mark or Ice Bind branch.", nameof(selectedBranch));
                ActiveBranch = SealBranch.FiveColorBarrier;
                return;
            }

            if (successfulClosures >= 14)
            {
                if (selectedBranch != SealBranch.None && selectedBranch != SealBranch.FireMark && selectedBranch != SealBranch.IceBind) throw new ArgumentException("Only Fire Mark or Ice Bind can be selected at this mastery.", nameof(selectedBranch));
                ActiveBranch = selectedBranch;
                return;
            }

            if (selectedBranch != SealBranch.None) throw new ArgumentException("A branch cannot be selected before fourteen successful closures.", nameof(selectedBranch));
            ActiveBranch = SealBranch.None;
        }

        public int SuccessfulClosures { get; }
        public float ClosureTolerance => SuccessfulClosures >= 3 ? 0.25f : 0.15f;
        public float MaxTrailLength => SuccessfulClosures >= 8 ? 8.5f : 7f;
        public float AreaMultiplier => SuccessfulClosures >= 8 ? 1.15f : 1f;
        public int BaseDamage => SuccessfulClosures >= 20 ? 40 : SuccessfulClosures >= 3 ? 26 : 20;
        public SealBranch ActiveBranch { get; }
        public bool RequiresBranchChoice => SuccessfulClosures >= 14 && SuccessfulClosures < 20 && ActiveBranch == SealBranch.None;
        public IReadOnlyList<SealBranch> AvailableBranches => SuccessfulClosures >= 14 && SuccessfulClosures < 20 ? ChoiceBranches : NoBranches;
        public MasteryState WithSelectedBranch(SealBranch branch) => new MasteryState(SuccessfulClosures, branch);
    }
}
