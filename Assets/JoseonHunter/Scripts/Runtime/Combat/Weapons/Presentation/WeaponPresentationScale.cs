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
            var stageScale = stage == WeaponVisualStage.Projectile ? 0.40f :
                stage == WeaponVisualStage.Trail ? 0.30f :
                stage == WeaponVisualStage.Windup ? 0.42f :
                stage == WeaponVisualStage.Impact ? 0.50f : 0.58f;
            var weaponScale = weaponId.Equals(WeaponId.GakgungShot) ? 0.86f :
                weaponId.Equals(WeaponId.SingijeonVolley) ? 0.84f :
                weaponId.Equals(WeaponId.HwandoFlyingBlade) ? 0.90f :
                weaponId.Equals(WeaponId.TalismanThrow) ? 0.90f :
                weaponId.Equals(WeaponId.ThunderCrashBomb) ? 0.84f :
                weaponId.Equals(WeaponId.JangseungWard) ? 0.82f :
                weaponId.Equals(WeaponId.FrostFlask) ? 0.90f : 0.82f;
            var powerScale = 1f +
                (level >= 3 ? 0.12f : 0f) +
                (level >= 5 ? 0.12f : 0f) +
                (evolved ? 0.16f : 0f);

            return Mathf.Max(0.05f, authoredScale) * stageScale * weaponScale * powerScale;
        }
    }
}
