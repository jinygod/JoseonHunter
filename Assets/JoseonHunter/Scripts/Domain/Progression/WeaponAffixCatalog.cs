using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public static class WeaponAffixCatalog
    {
        private static readonly IReadOnlyList<WeaponAffixStat> AllStats = Array.AsReadOnly(new[]
        {
            WeaponAffixStat.Damage, WeaponAffixStat.Cooldown, WeaponAffixStat.Area,
            WeaponAffixStat.ProjectileSpeed, WeaponAffixStat.Duration
        });

        private static readonly IReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponAffixStat>> StatMap = CreateStatMap();

        private static readonly IReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponPotentialId>> PotentialMap =
            new ReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponPotentialId>>(new Dictionary<WeaponId, IReadOnlyList<WeaponPotentialId>>
            {
                [WeaponId.HwandoFlyingBlade] = Array.AsReadOnly(new[] { WeaponPotentialId.HwandoVenomFang, WeaponPotentialId.HwandoReturningAfterimage, WeaponPotentialId.HwandoFlyingBladeDance }),
                [WeaponId.GakgungShot] = Array.AsReadOnly(new[] { WeaponPotentialId.GakgungArmorBreakArrowhead, WeaponPotentialId.GakgungSplitFletching, WeaponPotentialId.GakgungFullDraw }),
                [WeaponId.TalismanThrow] = Array.AsReadOnly(new[] { WeaponPotentialId.TalismanFiveElementCycle, WeaponPotentialId.TalismanSealTransfer, WeaponPotentialId.TalismanVengefulGhostBurst }),
                [WeaponId.ThunderCrashBomb] = Array.AsReadOnly(new[] { WeaponPotentialId.ThunderEarthCurrent, WeaponPotentialId.ThunderOverchargedCore, WeaponPotentialId.ThunderLightningRod }),
                [WeaponId.JangseungWard] = Array.AsReadOnly(new[] { WeaponPotentialId.JangseungGhostFace, WeaponPotentialId.JangseungFourDirectionBarrier, WeaponPotentialId.JangseungGuardianDescent }),
                [WeaponId.SingijeonVolley] = Array.AsReadOnly(new[] { WeaponPotentialId.SingijeonPowderTrail, WeaponPotentialId.SingijeonSubmunitionSplit, WeaponPotentialId.SingijeonChainIgnition }),
                [WeaponId.FrostFlask] = Array.AsReadOnly(new[] { WeaponPotentialId.FrostCrackMark, WeaponPotentialId.FrostSpread, WeaponPotentialId.FrostMist }),
                [WeaponId.WindThunderFan] = Array.AsReadOnly(new[] { WeaponPotentialId.FanVacuumEdge, WeaponPotentialId.FanDistantThunder, WeaponPotentialId.FanReturningChain })
            });

        static WeaponAffixCatalog()
        {
            if (PotentialMap.Count != WeaponRoster.All.Count || PotentialMap.Any(pair => pair.Value.Count != 3 || pair.Value.Distinct().Count() != 3))
                throw new InvalidOperationException("Every launch weapon requires three distinct potentials.");
        }

        public static IReadOnlyList<WeaponAffixStat> CompatibleStats(WeaponId id) =>
            StatMap.TryGetValue(id, out var stats) ? stats : throw new ArgumentException("Unknown weapon ID.", nameof(id));

        public static IReadOnlyList<WeaponPotentialId> CompatiblePotentials(WeaponId id) =>
            PotentialMap.TryGetValue(id, out var potentials) ? potentials : throw new ArgumentException("Unknown weapon ID.", nameof(id));

        private static IReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponAffixStat>> CreateStatMap()
        {
            var map = new Dictionary<WeaponId, IReadOnlyList<WeaponAffixStat>>();
            foreach (var id in WeaponRoster.All)
            {
                var excludesProjectileSpeed = id.Equals(WeaponId.ThunderCrashBomb) || id.Equals(WeaponId.JangseungWard) ||
                    id.Equals(WeaponId.FrostFlask) || id.Equals(WeaponId.WindThunderFan);
                var excludesDuration = id.Equals(WeaponId.HwandoFlyingBlade) || id.Equals(WeaponId.GakgungShot) ||
                    id.Equals(WeaponId.TalismanThrow) || id.Equals(WeaponId.SingijeonVolley);
                map[id] = Array.AsReadOnly(AllStats.Where(stat =>
                    (stat != WeaponAffixStat.ProjectileSpeed || !excludesProjectileSpeed) &&
                    (stat != WeaponAffixStat.Duration || !excludesDuration)).ToArray());
            }

            return new ReadOnlyDictionary<WeaponId, IReadOnlyList<WeaponAffixStat>>(map);
        }
    }
}
