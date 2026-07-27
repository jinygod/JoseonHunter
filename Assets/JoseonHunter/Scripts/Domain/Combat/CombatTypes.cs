using System;

namespace JoseonHunter.Domain.Combat
{
    public readonly struct DamageRequest
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
