using System;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Runtime.Combat.Weapons.Presentation
{
    public static class WeaponVisualPartIndex
    {
        public static class Hwando
        {
            public const int Projectile = 0;
            public const int ProjectileFrameCount = 4;
            public const int Trail = 4;
            public const int TrailFrameCount = 4;
            public const int Impact = 8;
            public const int ImpactFrameCount = 4;
            public const int RequiredCount = 12;
        }

        public static class Gakgung
        {
            public const int Windup = 0;
            public const int WindupFrameCount = 3;
            public const int Projectile = 3;
            public const int ProjectileFrameCount = 3;
            public const int Trail = Projectile;
            public const int Impact = 6;
            public const int ImpactFrameCount = 5;
            public const int RequiredCount = 11;
        }

        public static class Talisman
        {
            public const int Projectile = 0;
            public const int ProjectileFrameCount = 4;
            public const int Field = 4;
            public const int SealPulse = Field;
            public const int FieldFrameCount = 5;
            public const int Impact = 9;
            public const int Binding = Impact;
            public const int ImpactFrameCount = 5;
            public const int RequiredCount = 14;
        }

        public static class ThunderCrash
        {
            public const int Projectile = 0;
            public const int ProjectileFrameCount = 6;
            public const int Windup = 6;
            public const int WindupFrameCount = 4;
            public const int Detonation = 10;
            public const int DetonationFrameCount = 6;
            public const int Field = 16;
            public const int FieldFrameCount = 5;
            public const int RequiredCount = 21;
        }

        public static class Jangseung
        {
            public const int Windup = 0;
            public const int WindupFrameCount = 5;
            public const int Field = 5;
            public const int FieldFrameCount = 4;
            public const int Impact = 9;
            public const int ImpactFrameCount = 5;
            public const int RequiredCount = 14;
        }

        public static class Singijeon
        {
            public const int Projectile = 0;
            public const int Windup = Projectile;
            public const int ProjectileFrameCount = 4;
            public const int Trail = 4;
            public const int TrailFrameCount = 5;
            public const int Detonation = 9;
            public const int DetonationFrameCount = 6;
            public const int RequiredCount = 15;
        }

        public static class FrostFlask
        {
            public const int Projectile = 0;
            public const int ProjectileFrameCount = 6;
            public const int Field = 6;
            public const int FieldFrameCount = 5;
            public const int Impact = 11;
            public const int ImpactFrameCount = 6;
            public const int RequiredCount = 17;
        }

        public static class WindThunderFan
        {
            public const int Projectile = 0;
            public const int Gust = Projectile;
            public const int Trail = Projectile;
            public const int ProjectileFrameCount = 5;
            public const int Field = 5;
            public const int Target = Field;
            public const int FieldFrameCount = 4;
            public const int Impact = 9;
            public const int Lightning = Impact;
            public const int ImpactFrameCount = 6;
            public const int RequiredCount = 15;
        }

        public static int RequiredCount(WeaponId weaponId)
        {
            if (weaponId.Equals(WeaponId.HwandoFlyingBlade)) return Hwando.RequiredCount;
            if (weaponId.Equals(WeaponId.GakgungShot)) return Gakgung.RequiredCount;
            if (weaponId.Equals(WeaponId.TalismanThrow)) return Talisman.RequiredCount;
            if (weaponId.Equals(WeaponId.ThunderCrashBomb)) return ThunderCrash.RequiredCount;
            if (weaponId.Equals(WeaponId.JangseungWard)) return Jangseung.RequiredCount;
            if (weaponId.Equals(WeaponId.SingijeonVolley)) return Singijeon.RequiredCount;
            if (weaponId.Equals(WeaponId.FrostFlask)) return FrostFlask.RequiredCount;
            if (weaponId.Equals(WeaponId.WindThunderFan)) return WindThunderFan.RequiredCount;
            throw new ArgumentOutOfRangeException(nameof(weaponId), weaponId, "Weapon is not in the launch roster.");
        }
    }
}
