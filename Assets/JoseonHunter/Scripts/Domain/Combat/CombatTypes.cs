using System;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Domain.Combat
{
    [Flags]
    public enum WeaponHitTrait
    {
        None = 0,
        Slash = 1 << 0,
        Pierce = 1 << 1,
        Explosion = 1 << 2,
        Heavy = 1 << 3,
        Wind = 1 << 4,
        Pull = 1 << 5,
        Barrier = 1 << 6,
        Knockback = 1 << 7,
        Reaction = 1 << 8
    }

    public enum CombatStatusKind { Poison, Burn, Seal, ArmorBreak, Shock, Freeze, Bleed }
    public enum StatusReactionKind { None, IceShatter, FireWind, FormationBreak, Overload }

    public readonly struct StatusReactionResult : IEquatable<StatusReactionResult>
    {
        public StatusReactionResult(StatusReactionKind kind, Float2 worldPosition, int affectedCount)
        {
            Kind = kind;
            WorldPosition = worldPosition;
            AffectedCount = Math.Max(0, affectedCount);
        }

        public StatusReactionKind Kind { get; }
        public Float2 WorldPosition { get; }
        public int AffectedCount { get; }
        public bool Equals(StatusReactionResult other) => Kind == other.Kind &&
            WorldPosition.Equals(other.WorldPosition) && AffectedCount == other.AffectedCount;
        public override bool Equals(object obj) => obj is StatusReactionResult other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Kind, WorldPosition, AffectedCount);
    }

    public readonly struct StatusReactionEvent
    {
        public StatusReactionEvent(StatusReactionKind kind, Float2 worldPosition, int affectedCount)
        {
            Kind = kind;
            WorldPosition = worldPosition;
            AffectedCount = affectedCount;
        }

        public StatusReactionKind Kind { get; }
        public Float2 WorldPosition { get; }
        public int AffectedCount { get; }
    }

    public readonly struct DamageRequest : IEquatable<DamageRequest>
    {
        public DamageRequest(int baseDamage, int flatBonus, bool isCritical, float multiplier)
        {
            BaseDamage = baseDamage;
            FlatBonus = flatBonus;
            IsCritical = isCritical;
            Multiplier = multiplier;
        }

        public int BaseDamage { get; }
        public int FlatBonus { get; }
        public bool IsCritical { get; }
        public float Multiplier { get; }

        public bool Equals(DamageRequest other) =>
            BaseDamage == other.BaseDamage && FlatBonus == other.FlatBonus &&
            IsCritical == other.IsCritical && Multiplier.Equals(other.Multiplier);

        public override bool Equals(object obj) => obj is DamageRequest other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(BaseDamage, FlatBonus, IsCritical, Multiplier);
        public static bool operator ==(DamageRequest left, DamageRequest right) => left.Equals(right);
        public static bool operator !=(DamageRequest left, DamageRequest right) => !left.Equals(right);

        public void Deconstruct(out int baseDamage, out int flatBonus, out bool isCritical, out float multiplier)
        {
            baseDamage = BaseDamage;
            flatBonus = FlatBonus;
            isCritical = IsCritical;
            multiplier = Multiplier;
        }
    }

    public readonly struct DamageResult : IEquatable<DamageResult>
    {
        public DamageResult(int finalDamage, bool isCritical)
        {
            FinalDamage = finalDamage;
            IsCritical = isCritical;
        }

        public int FinalDamage { get; }
        public bool IsCritical { get; }
        public bool Equals(DamageResult other) => FinalDamage == other.FinalDamage && IsCritical == other.IsCritical;
        public override bool Equals(object obj) => obj is DamageResult other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(FinalDamage, IsCritical);
    }
}
