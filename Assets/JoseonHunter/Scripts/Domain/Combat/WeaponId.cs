using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Combat
{
    public readonly struct WeaponId : IEquatable<WeaponId>
    {
        public static readonly WeaponId HwandoFlyingBlade = new("hwando_flying_blade");
        public static readonly WeaponId GakgungShot = new("gakgung_shot");
        public static readonly WeaponId TalismanThrow = new("talisman_throw");
        public static readonly WeaponId ThunderCrashBomb = new("thunder_crash_bomb");
        public static readonly WeaponId JangseungWard = new("jangseung_ward");
        public static readonly WeaponId SingijeonVolley = new("singijeon_volley");
        public static readonly WeaponId FrostFlask = new("frost_flask");
        public static readonly WeaponId WindThunderFan = new("wind_thunder_fan");

        public WeaponId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Weapon ID is required.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool Equals(WeaponId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is WeaponId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public static class WeaponRoster
    {
        public static readonly IReadOnlyList<WeaponId> All = new[]
        {
            WeaponId.HwandoFlyingBlade,
            WeaponId.GakgungShot,
            WeaponId.TalismanThrow,
            WeaponId.ThunderCrashBomb,
            WeaponId.JangseungWard,
            WeaponId.SingijeonVolley,
            WeaponId.FrostFlask,
            WeaponId.WindThunderFan
        };
    }
}
