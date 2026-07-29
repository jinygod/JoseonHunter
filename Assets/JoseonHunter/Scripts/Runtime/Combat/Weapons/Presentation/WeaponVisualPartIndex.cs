using System;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Runtime.Combat.Weapons.Presentation
{
    public static class WeaponVisualPartIndex
    {
        public static class Hwando
        {
            public const int Projectile = 0;
            public const int Trail = 1;
            public const int Impact = 2;
        }

        public static class Gakgung
        {
            public const int Projectile = 0;
            public const int Windup = 1;
            public const int Impact = 2;
            public const int Trail = 3;
        }

        public static class Talisman
        {
            public const int Projectile = 0;
            public const int Impact = 1;
        }

        public static class ThunderCrash
        {
            public const int Windup = 0;
            public const int Projectile = 1;
            public const int Detonation = 2;
        }

        public static class Jangseung
        {
            public const int Windup = 0;
            public const int Field = 1;
            public const int Impact = 2;
        }

        public static class Singijeon
        {
            public const int Windup = 0;
            public const int Projectile = 1;
            public const int Trail = 2;
            public const int Detonation = 3;
        }

        public static class FrostFlask
        {
            public const int Projectile = 0;
            public const int Field = 1;
            public const int Impact = 2;
        }

        public static class WindThunderFan
        {
            public const int Projectile = 0;
            public const int Trail = 1;
            public const int Impact = 2;
        }

        public static int RequiredCount(WeaponId weaponId)
        {
            if (weaponId.Equals(WeaponId.HwandoFlyingBlade)) return Hwando.Impact + 1;
            if (weaponId.Equals(WeaponId.GakgungShot)) return Gakgung.Trail + 1;
            if (weaponId.Equals(WeaponId.TalismanThrow)) return Talisman.Impact + 1;
            if (weaponId.Equals(WeaponId.ThunderCrashBomb)) return ThunderCrash.Detonation + 1;
            if (weaponId.Equals(WeaponId.JangseungWard)) return Jangseung.Impact + 1;
            if (weaponId.Equals(WeaponId.SingijeonVolley)) return Singijeon.Detonation + 1;
            if (weaponId.Equals(WeaponId.FrostFlask)) return FrostFlask.Impact + 1;
            if (weaponId.Equals(WeaponId.WindThunderFan)) return WindThunderFan.Impact + 1;
            throw new ArgumentOutOfRangeException(nameof(weaponId), weaponId, "Weapon is not in the launch roster.");
        }
    }
}
