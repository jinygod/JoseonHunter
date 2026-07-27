using System;
using System.Collections.Generic;

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
        {
            WeaponLevels = weaponLevels;
            SupportLevels = supportLevels;
            UnlockedIds = unlockedIds;
        }

        public UpgradeState(
            IReadOnlyDictionary<string, int> weaponLevels,
            IReadOnlyDictionary<string, int> supportLevels,
            ISet<string> unlockedIds)
            : this(weaponLevels, supportLevels, new SetView(unlockedIds))
        {
        }

        public IReadOnlyDictionary<string, int> WeaponLevels { get; }
        public IReadOnlyDictionary<string, int> SupportLevels { get; }
        public IUpgradeIdSet UnlockedIds { get; }

        private sealed class SetView : IUpgradeIdSet
        {
            private readonly ISet<string> source;

            public SetView(ISet<string> source) => this.source = source;
            public int Count => source.Count;
            public bool Contains(string item) => source.Contains(item);
            public IEnumerator<string> GetEnumerator() => source.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
