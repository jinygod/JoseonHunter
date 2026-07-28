using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public enum WeaponAffixStat { Damage, Cooldown, Area, ProjectileSpeed, Duration }

    public enum WeaponAffixTier { Standard, High, Perfect }

    public readonly struct WeaponPotentialId : IEquatable<WeaponPotentialId>
    {
        public static readonly WeaponPotentialId HwandoVenomFang = new("hwando_venom_fang");
        public static readonly WeaponPotentialId HwandoReturningAfterimage = new("hwando_returning_afterimage");
        public static readonly WeaponPotentialId HwandoFlyingBladeDance = new("hwando_flying_blade_dance");
        public static readonly WeaponPotentialId GakgungArmorBreakArrowhead = new("gakgung_armor_break_arrowhead");
        public static readonly WeaponPotentialId GakgungSplitFletching = new("gakgung_split_fletching");
        public static readonly WeaponPotentialId GakgungFullDraw = new("gakgung_full_draw");
        public static readonly WeaponPotentialId TalismanFiveElementCycle = new("talisman_five_element_cycle");
        public static readonly WeaponPotentialId TalismanSealTransfer = new("talisman_seal_transfer");
        public static readonly WeaponPotentialId TalismanVengefulGhostBurst = new("talisman_vengeful_ghost_burst");
        public static readonly WeaponPotentialId ThunderEarthCurrent = new("thunder_earth_current");
        public static readonly WeaponPotentialId ThunderOverchargedCore = new("thunder_overcharged_core");
        public static readonly WeaponPotentialId ThunderLightningRod = new("thunder_lightning_rod");
        public static readonly WeaponPotentialId JangseungGhostFace = new("jangseung_ghost_face");
        public static readonly WeaponPotentialId JangseungFourDirectionBarrier = new("jangseung_four_direction_barrier");
        public static readonly WeaponPotentialId JangseungGuardianDescent = new("jangseung_guardian_descent");
        public static readonly WeaponPotentialId SingijeonPowderTrail = new("singijeon_powder_trail");
        public static readonly WeaponPotentialId SingijeonSubmunitionSplit = new("singijeon_submunition_split");
        public static readonly WeaponPotentialId SingijeonChainIgnition = new("singijeon_chain_ignition");
        public static readonly WeaponPotentialId FrostCrackMark = new("frost_crack_mark");
        public static readonly WeaponPotentialId FrostSpread = new("frost_spread");
        public static readonly WeaponPotentialId FrostMist = new("frost_mist");
        public static readonly WeaponPotentialId FanVacuumEdge = new("fan_vacuum_edge");
        public static readonly WeaponPotentialId FanDistantThunder = new("fan_distant_thunder");
        public static readonly WeaponPotentialId FanReturningChain = new("fan_returning_chain");

        public WeaponPotentialId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Potential ID is required.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool Equals(WeaponPotentialId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is WeaponPotentialId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public readonly struct WeaponAffixRoll : IEquatable<WeaponAffixRoll>
    {
        public WeaponAffixRoll(WeaponAffixStat stat, WeaponAffixTier tier, double value)
        {
            Stat = stat;
            Tier = tier;
            Value = value;
        }

        public WeaponAffixStat Stat { get; }
        public WeaponAffixTier Tier { get; }
        public double Value { get; }
        public bool Equals(WeaponAffixRoll other) => Stat == other.Stat && Tier == other.Tier && Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is WeaponAffixRoll other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Stat, Tier, Value);
    }

    public sealed class WeaponAffixRollResult
    {
        public WeaponAffixRollResult(WeaponAffixRoll general, IReadOnlyList<WeaponPotentialId> newPotentials)
        {
            General = general;
            NewPotentials = newPotentials ?? throw new ArgumentNullException(nameof(newPotentials));
        }

        public WeaponAffixRoll General { get; }
        public IReadOnlyList<WeaponPotentialId> NewPotentials { get; }
    }

    public sealed class WeaponRunAffixProfile
    {
        private readonly List<WeaponAffixRoll> generalRolls = new();
        private readonly List<WeaponPotentialId> potentialIds = new();

        public IReadOnlyList<WeaponAffixRoll> GeneralRolls => generalRolls;
        public IReadOnlyList<WeaponPotentialId> PotentialIds => potentialIds;
        internal void AddGeneral(WeaponAffixRoll roll) => generalRolls.Add(roll);

        internal bool AddPotential(WeaponPotentialId id)
        {
            if (potentialIds.Count >= 3 || potentialIds.Contains(id)) return false;
            potentialIds.Add(id);
            return true;
        }
    }

    public sealed class WeaponRunAffixState
    {
        private readonly Dictionary<WeaponId, WeaponRunAffixProfile> profiles = new();

        public WeaponRunAffixProfile ProfileFor(WeaponId id) =>
            profiles.TryGetValue(id, out var profile) ? profile : profiles[id] = new WeaponRunAffixProfile();

        public void Clear() => profiles.Clear();
    }
}
