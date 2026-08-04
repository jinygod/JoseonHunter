using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public readonly struct WeaponMasteryStyleDefinition
    {
        public WeaponMasteryStyleDefinition(
            WeaponId weaponId,
            string styleId,
            WeaponLegacyPathId legacyPathId,
            string displayName,
            string benefit,
            string tradeoff,
            int requiredMastery,
            int coinCost,
            bool isBase)
        {
            WeaponId = weaponId;
            StyleId = styleId ?? throw new ArgumentNullException(nameof(styleId));
            LegacyPathId = legacyPathId;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Benefit = benefit ?? throw new ArgumentNullException(nameof(benefit));
            Tradeoff = tradeoff ?? throw new ArgumentNullException(nameof(tradeoff));
            RequiredMastery = requiredMastery;
            CoinCost = coinCost;
            IsBase = isBase;
        }

        public WeaponId WeaponId { get; }
        public string StyleId { get; }
        public WeaponLegacyPathId LegacyPathId { get; }
        public string DisplayName { get; }
        public string Benefit { get; }
        public string Tradeoff { get; }
        public int RequiredMastery { get; }
        public int CoinCost { get; }
        public bool IsBase { get; }
    }

    public static class WeaponMasteryCatalog
    {
        private static readonly IReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponMasteryStyleDefinition>> Definitions = Build();

        public static IReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponMasteryStyleDefinition>> All => Definitions;

        public static IReadOnlyList<WeaponMasteryStyleDefinition> StylesFor(WeaponId weaponId) =>
            Definitions.TryGetValue(weaponId, out var styles)
                ? styles
                : Array.Empty<WeaponMasteryStyleDefinition>();

        public static bool TryGet(WeaponId weaponId, WeaponLegacyPathId pathId, out WeaponMasteryStyleDefinition style)
        {
            foreach (var candidate in StylesFor(weaponId))
            {
                if (!candidate.IsBase && candidate.LegacyPathId.Equals(pathId))
                {
                    style = candidate;
                    return true;
                }
            }

            style = default;
            return false;
        }

        private static IReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponMasteryStyleDefinition>> Build()
        {
            var result = new Dictionary<WeaponId, IReadOnlyList<WeaponMasteryStyleDefinition>>();
            foreach (var weaponId in WeaponRoster.All)
            {
                var legacy = WeaponLegacyCatalog.PathsFor(weaponId);
                if (legacy.Count != 2)
                    throw new InvalidOperationException($"Weapon '{weaponId.Value}' must have exactly two legacy paths.");

                result.Add(weaponId, Array.AsReadOnly(new[]
                {
                    new WeaponMasteryStyleDefinition(
                        weaponId, weaponId.Value + "_base", default, "기본식",
                        "무기의 본래 운용법", "추가 효과 없음", 0, 0, true),
                    FromLegacy(legacy[0], 2000, 800),
                    FromLegacy(legacy[1], 8000, 2400)
                }));
            }

            return new ReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponMasteryStyleDefinition>>(result);
        }

        private static WeaponMasteryStyleDefinition FromLegacy(
            WeaponLegacyDefinition legacy,
            int requiredMastery,
            int coinCost) =>
            new WeaponMasteryStyleDefinition(
                legacy.WeaponId,
                legacy.Id.Value,
                legacy.Id,
                legacy.DisplayName,
                legacy.Benefit,
                legacy.Cost,
                requiredMastery,
                coinCost,
                false);
    }
}
