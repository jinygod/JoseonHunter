using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public sealed class PatrolLoadout
    {
        private readonly IReadOnlyDictionary<WeaponId, WeaponLegacyPathId> styles;

        public PatrolLoadout(
            string name,
            WeaponId startingWeapon,
            IReadOnlyDictionary<WeaponId, WeaponLegacyPathId> styles,
            string difficultyId)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Loadout name is required.", nameof(name));
            if (!WeaponRoster.All.Contains(startingWeapon)) throw new ArgumentException("Unknown starting weapon.", nameof(startingWeapon));
            if (styles == null) throw new ArgumentNullException(nameof(styles));
            if (string.IsNullOrWhiteSpace(difficultyId)) throw new ArgumentException("Difficulty is required.", nameof(difficultyId));

            var copy = new Dictionary<WeaponId, WeaponLegacyPathId>();
            foreach (var pair in styles)
            {
                if (!WeaponRoster.All.Contains(pair.Key)) throw new ArgumentException("Unknown style weapon.", nameof(styles));
                if (!string.IsNullOrEmpty(pair.Value.Value) &&
                    (!WeaponLegacyCatalog.TryGet(pair.Value, out var definition) || !definition.WeaponId.Equals(pair.Key)))
                    throw new ArgumentException("Style does not belong to its weapon.", nameof(styles));
                copy[pair.Key] = pair.Value;
            }

            Name = name.Trim();
            StartingWeapon = startingWeapon;
            DifficultyId = difficultyId.Trim();
            this.styles = new ReadOnlyDictionary<WeaponId, WeaponLegacyPathId>(copy);
        }

        public string Name { get; }
        public WeaponId StartingWeapon { get; }
        public string DifficultyId { get; }
        public IReadOnlyDictionary<WeaponId, WeaponLegacyPathId> Styles => styles;
        public WeaponLegacyPathId StyleFor(WeaponId weaponId) =>
            styles.TryGetValue(weaponId, out var style) ? style : default;
    }
}
