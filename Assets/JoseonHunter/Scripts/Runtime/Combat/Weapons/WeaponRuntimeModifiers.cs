using System.Collections.Generic;
using JoseonHunter.Domain.Progression;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    /// <summary>Immutable, run-scoped affix totals. The default value intentionally changes no existing weapon behavior.</summary>
    public readonly struct WeaponRuntimeModifiers
    {
        private readonly HashSet<WeaponPotentialId> potentialIds;

        private WeaponRuntimeModifiers(float damageBonus, float cooldownReduction, float areaBonus, float speedBonus, float durationBonus, HashSet<WeaponPotentialId> potentialIds)
        {
            DamageBonus = damageBonus;
            CooldownReduction = cooldownReduction;
            AreaBonus = areaBonus;
            SpeedBonus = speedBonus;
            DurationBonus = durationBonus;
            this.potentialIds = potentialIds;
        }

        public float DamageBonus { get; }
        public float CooldownReduction { get; }
        public float AreaBonus { get; }
        public float SpeedBonus { get; }
        public float DurationBonus { get; }

        public static WeaponRuntimeModifiers From(WeaponRunAffixProfile profile)
        {
            if (profile == null) return default;
            var damage = 0f;
            var cooldown = 0f;
            var area = 0f;
            var speed = 0f;
            var duration = 0f;
            foreach (var roll in profile.GeneralRolls)
            {
                var value = (float)(roll.Value * .01d);
                if (float.IsNaN(value) || float.IsInfinity(value)) continue;
                switch (roll.Stat)
                {
                    case WeaponAffixStat.Damage: damage += value; break;
                    case WeaponAffixStat.Cooldown: cooldown -= value; break;
                    case WeaponAffixStat.Area: area += value; break;
                    case WeaponAffixStat.ProjectileSpeed: speed += value; break;
                    case WeaponAffixStat.Duration: duration += value; break;
                }
            }

            return new WeaponRuntimeModifiers(damage, cooldown, area, speed, duration,
                profile.PotentialIds.Count == 0 ? null : new HashSet<WeaponPotentialId>(profile.PotentialIds));
        }

        public float ScaleDamage(float value) => value * (1f + DamageBonus);
        public float ScaleCooldown(float value) => Mathf.Max(.01f, value * (1f - Mathf.Clamp(CooldownReduction, 0f, .75f)));
        public float ScaleArea(float value) => value * (1f + AreaBonus);
        public float ScaleSpeed(float value) => value * (1f + SpeedBonus);
        public float ScaleDuration(float value) => value * (1f + DurationBonus);
        public bool HasPotential(WeaponPotentialId id) => potentialIds != null && potentialIds.Contains(id);
    }
}
