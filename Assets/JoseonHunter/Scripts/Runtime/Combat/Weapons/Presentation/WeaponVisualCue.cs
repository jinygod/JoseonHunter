using JoseonHunter.Domain.Combat;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons.Presentation
{
    public enum WeaponVisualStage
    {
        Windup,
        Projectile,
        Trail,
        Impact,
        Field,
        Detonation
    }

    public readonly struct WeaponVisualCue
    {
        public WeaponVisualCue(
            WeaponId weaponId,
            WeaponVisualStage stage,
            int level,
            bool evolved,
            float baseScale,
            float lifetime)
        {
            WeaponId = weaponId;
            Stage = stage;
            Level = Mathf.Clamp(level, 1, 5);
            Evolved = evolved;
            ResolvedScale = Mathf.Max(.01f, baseScale) *
                (1f + (Level >= 3 ? .12f : 0f) + (Level >= 5 ? .12f : 0f) + (Evolved ? .16f : 0f));
            ResolvedLifetime = Mathf.Min(.32f, Mathf.Max(.04f, lifetime) * (Evolved ? 1.25f : 1f));
        }

        public WeaponId WeaponId { get; }
        public WeaponVisualStage Stage { get; }
        public int Level { get; }
        public bool Evolved { get; }
        public float ResolvedScale { get; }
        public float ResolvedLifetime { get; }
    }
}
