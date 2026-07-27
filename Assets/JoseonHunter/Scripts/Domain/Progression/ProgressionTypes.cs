using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JoseonHunter.Domain.Progression
{
    public readonly struct UpgradeOffer : IEquatable<UpgradeOffer>
    {
        public UpgradeOffer(string id, UpgradeKind kind, int nextLevel)
        {
            Id = id;
            Kind = kind;
            NextLevel = nextLevel;
        }

        public string Id { get; }
        public UpgradeKind Kind { get; }
        public int NextLevel { get; }
        public bool Equals(UpgradeOffer other) => Id == other.Id && Kind == other.Kind && NextLevel == other.NextLevel;
        public override bool Equals(object obj) => obj is UpgradeOffer other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Kind, NextLevel);
    }

    public enum UpgradeKind { Weapon, Support, Evolution }

    public interface IUpgradeIdSet : IReadOnlyCollection<string>
    {
        bool Contains(string item);
    }

    public sealed class UpgradeState
    {
        public UpgradeState(
            IReadOnlyDictionary<string, int> weaponLevels,
            IReadOnlyDictionary<string, int> supportLevels,
            IUpgradeIdSet unlockedIds)
            : this(weaponLevels, supportLevels, unlockedIds, new SnapshotSet(Array.Empty<string>()))
        {
        }

        public UpgradeState(
            IReadOnlyDictionary<string, int> weaponLevels,
            IReadOnlyDictionary<string, int> supportLevels,
            IUpgradeIdSet unlockedIds,
            IUpgradeIdSet acquiredEvolutionIds)
        {
            if (weaponLevels == null) throw new ArgumentNullException(nameof(weaponLevels));
            if (supportLevels == null) throw new ArgumentNullException(nameof(supportLevels));
            if (unlockedIds == null) throw new ArgumentNullException(nameof(unlockedIds));
            if (acquiredEvolutionIds == null) throw new ArgumentNullException(nameof(acquiredEvolutionIds));

            WeaponLevels = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(weaponLevels));
            SupportLevels = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(supportLevels));
            UnlockedIds = new SnapshotSet(unlockedIds);
            AcquiredEvolutionIds = new SnapshotSet(acquiredEvolutionIds);
        }

        public UpgradeState(
            IReadOnlyDictionary<string, int> weaponLevels,
            IReadOnlyDictionary<string, int> supportLevels,
            ISet<string> unlockedIds)
            : this(weaponLevels, supportLevels, new SnapshotSet(unlockedIds), new SnapshotSet(Array.Empty<string>()))
        {
        }

        public UpgradeState(
            IReadOnlyDictionary<string, int> weaponLevels,
            IReadOnlyDictionary<string, int> supportLevels,
            ISet<string> unlockedIds,
            ISet<string> acquiredEvolutionIds)
            : this(weaponLevels, supportLevels, new SnapshotSet(unlockedIds), new SnapshotSet(acquiredEvolutionIds))
        {
        }

        public IReadOnlyDictionary<string, int> WeaponLevels { get; }
        public IReadOnlyDictionary<string, int> SupportLevels { get; }
        public IUpgradeIdSet UnlockedIds { get; }
        public IUpgradeIdSet AcquiredEvolutionIds { get; }

        private sealed class SnapshotSet : IUpgradeIdSet
        {
            private readonly HashSet<string> values;

            public SnapshotSet(IEnumerable<string> source)
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                values = new HashSet<string>(source);
            }

            public int Count => values.Count;
            public bool Contains(string item) => values.Contains(item);
            public IEnumerator<string> GetEnumerator() => values.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
