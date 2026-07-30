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
            ResolvedScale = WeaponPresentationScale.For(
                WeaponId,
                Stage,
                baseScale,
                Level,
                Evolved);
            var evolutionLifetime = !Evolved ? 1f :
                Stage == WeaponVisualStage.Detonation || Stage == WeaponVisualStage.Field ? 1.5f :
                Stage == WeaponVisualStage.Impact ? 1.3f : 1.12f;
            var maximumLifetime =
                Stage == WeaponVisualStage.Detonation || Stage == WeaponVisualStage.Field ? .40f : .32f;
            ResolvedLifetime = Mathf.Min(
                maximumLifetime,
                Mathf.Max(.04f, lifetime) * evolutionLifetime);
        }

        public WeaponId WeaponId { get; }
        public WeaponVisualStage Stage { get; }
        public int Level { get; }
        public bool Evolved { get; }
        public float ResolvedScale { get; }
        public float ResolvedLifetime { get; }
    }
}
