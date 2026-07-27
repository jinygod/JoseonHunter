using System;
using UnityEngine;

namespace JoseonHunter.Content.Weapons
{
    [Serializable]
    public sealed class WeaponLevelData
    {
        [SerializeField] private string weaponId;
        [SerializeField] private int level;
        [SerializeField] private float baseDamage;
        [SerializeField] private float cooldownSeconds;
        [SerializeField] private float range;
        [SerializeField] private int projectileCount;
        [SerializeField] private float speed;
        [SerializeField] private float durationSeconds;
        [SerializeField] private int pierce;
        [SerializeField] private int chainCount;
        [SerializeField] private float knockback;
        [SerializeField] private float slowFraction;
        [SerializeField] private float criticalChance;

        public WeaponLevelData(
            string weaponId, int level, float baseDamage, float cooldownSeconds, float range,
            int projectileCount, float speed, float durationSeconds, int pierce, int chainCount,
            float knockback, float slowFraction, float criticalChance)
        {
            this.weaponId = weaponId;
            this.level = level;
            this.baseDamage = baseDamage;
            this.cooldownSeconds = cooldownSeconds;
            this.range = range;
            this.projectileCount = projectileCount;
            this.speed = speed;
            this.durationSeconds = durationSeconds;
            this.pierce = pierce;
            this.chainCount = chainCount;
            this.knockback = knockback;
            this.slowFraction = slowFraction;
            this.criticalChance = criticalChance;
        }

        public string WeaponId => weaponId;
        public int Level => level;
        public float BaseDamage => baseDamage;
        public float CooldownSeconds => cooldownSeconds;
        public float Range => range;
        public int ProjectileCount => projectileCount;
        public float Speed => speed;
        public float DurationSeconds => durationSeconds;
        public int Pierce => pierce;
        public int ChainCount => chainCount;
        public float Knockback => knockback;
        public float SlowFraction => slowFraction;
        public float CriticalChance => criticalChance;

        internal string Validate(string owningWeaponId, int expectedLevel)
        {
            if (!string.Equals(weaponId, owningWeaponId, StringComparison.Ordinal))
            {
                return $"level {expectedLevel} weapon ID must match definition ID '{owningWeaponId}'";
            }

            if (level != expectedLevel) return $"level row must be numbered {expectedLevel}";
            if (!IsFiniteNonNegative(baseDamage) || !IsFiniteNonNegative(range) ||
                !IsFiniteNonNegative(speed) || !IsFiniteNonNegative(durationSeconds) ||
                !IsFiniteNonNegative(knockback) || !IsFiniteNonNegative(slowFraction) ||
                !IsFiniteNonNegative(criticalChance) || projectileCount < 0 || pierce < 0 || chainCount < 0)
            {
                return $"level {expectedLevel} contains a non-finite or negative value";
            }

            return cooldownSeconds <= 0f || !IsFinite(cooldownSeconds)
                ? $"level {expectedLevel} cooldown must be finite and greater than zero"
                : null;
        }

        private static bool IsFiniteNonNegative(float value) => IsFinite(value) && value >= 0f;
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
