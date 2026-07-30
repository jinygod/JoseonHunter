using JoseonHunter.Domain.Combat;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons.Presentation
{
    /// <summary>
    /// Converts legacy weapon-authored scales to compact combat presentation scales.
    /// The returned value is also used by pixel-mask transforms so visible contact remains honest.
    /// </summary>
    public static class WeaponPresentationScale
    {
        public static float For(
            WeaponId weaponId,
            WeaponVisualStage stage,
            float authoredScale,
            int level,
            bool evolved)
        {
            var stageScale = stage == WeaponVisualStage.Projectile ? .52f :
                stage == WeaponVisualStage.Trail ? .32f :
                stage == WeaponVisualStage.Windup ? .46f :
                stage == WeaponVisualStage.Impact ? .58f :
                stage == WeaponVisualStage.Field ? .62f : .68f;
            // Each weapon keeps a distinct screen-space silhouette instead of sharing one
            // correction factor. Trails stay subordinate; impact stages carry the spectacle.
            var weaponScale = weaponId.Equals(WeaponId.HwandoFlyingBlade) ? 1.05f :
                weaponId.Equals(WeaponId.GakgungShot) ? .98f :
                weaponId.Equals(WeaponId.TalismanThrow) ? 1f :
                weaponId.Equals(WeaponId.ThunderCrashBomb) ? .92f :
                weaponId.Equals(WeaponId.JangseungWard) ? .94f :
                weaponId.Equals(WeaponId.SingijeonVolley) ? .89f :
                weaponId.Equals(WeaponId.FrostFlask) ? .96f : 1.02f;
            var evolutionGrowth = evolved
                ? stage == WeaponVisualStage.Detonation ? .20f :
                    stage == WeaponVisualStage.Field ? .18f :
                    stage == WeaponVisualStage.Impact ? .14f :
                    stage == WeaponVisualStage.Trail ? .10f :
                    stage == WeaponVisualStage.Windup ? .08f : .05f
                : 0f;
            var powerScale = 1f +
                (level >= 3 ? 0.12f : 0f) +
                (level >= 5 ? 0.04f : 0f) +
                evolutionGrowth;

            return Mathf.Max(0.05f, authoredScale) * stageScale * weaponScale * powerScale;
        }
    }
}
