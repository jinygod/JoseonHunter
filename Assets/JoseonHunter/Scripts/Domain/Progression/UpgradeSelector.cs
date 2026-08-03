using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public static class UpgradeSelector
    {
        private const int MaxLevel = 5;
        private static readonly string[] WeaponIds = WeaponRoster.All.Select(id => id.Value).ToArray();
        private static readonly string[] SupportIds = { "talisman", "boots", "warding_bell" };

        public static IReadOnlyList<UpgradeOffer> Select(UpgradeState state, int seed)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var eligible = EligibleOffers(state).ToList();
            if (eligible.Count < 3)
            {
                throw new InvalidOperationException("At least three distinct eligible upgrades are required.");
            }

            var random = new Random(seed);
            var offers = new List<UpgradeOffer>(3);
            var ownedWeapons = WeaponIds
                .Where(id => state.WeaponLevels.TryGetValue(id, out var level) && level < MaxLevel)
                .Select(id => WeaponOffer(id, state.WeaponLevels[id], false))
                .ToList();

            if (ownedWeapons.Count > 0)
            {
                offers.Add(ownedWeapons[random.Next(ownedWeapons.Count)]);
            }

            eligible = eligible
                .Where(offer => !offers.Any(selected => selected.Id == offer.Id))
                .ToList();

            var unownedWeapons = eligible
                .Where(offer => offer.Kind == UpgradeKind.Weapon && offer.NextLevel == 1)
                .ToList();
            if (unownedWeapons.Count > 0)
            {
                offers.Add(unownedWeapons[random.Next(unownedWeapons.Count)]);
                eligible = eligible
                    .Where(offer => !offers.Any(selected => selected.Id == offer.Id))
                    .ToList();
            }

            Shuffle(eligible, random);

            foreach (var offer in eligible)
            {
                if (offers.Count == 3) break;
                offers.Add(offer);
            }

            return new ReadOnlyCollection<UpgradeOffer>(offers);
        }

        private static IEnumerable<UpgradeOffer> EligibleOffers(UpgradeState state)
        {
            var ownedWeaponCount = state.WeaponLevels.Count(pair => pair.Value > 0);
            foreach (var id in WeaponIds)
            {
                if (state.DiscardedWeaponIds.Contains(id)) continue;
                var level = state.WeaponLevels.TryGetValue(id, out var currentLevel) ? currentLevel : 0;
                if (level < MaxLevel)
                    yield return WeaponOffer(id, level,
                        level == 0 && ownedWeaponCount >= RunLoadoutRules.WeaponSlotLimit);
            }

            var ownedSupportCount = state.SupportLevels.Count(pair => pair.Value > 0);
            foreach (var id in SupportIds)
            {
                var level = state.SupportLevels.TryGetValue(id, out var currentLevel) ? currentLevel : 0;
                if (level == 0 && ownedSupportCount >= RunLoadoutRules.SupportSlotLimit) continue;
                if (level < MaxLevel) yield return new UpgradeOffer(id, UpgradeKind.Support, level + 1);
            }
        }

        private static UpgradeOffer WeaponOffer(string id, int currentLevel, bool requiresReplacement) =>
            new(id, UpgradeKind.Weapon, currentLevel + 1, requiresReplacement);

        private static void Shuffle<T>(IList<T> items, Random random)
        {
            for (var index = items.Count - 1; index > 0; index--)
            {
                var nextIndex = random.Next(index + 1);
                (items[index], items[nextIndex]) = (items[nextIndex], items[index]);
            }
        }
    }
}
