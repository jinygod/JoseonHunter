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
            var stageScale = stage == WeaponVisualStage.Projectile ? 0.14f :
                stage == WeaponVisualStage.Trail ? 0.12f :
                stage == WeaponVisualStage.Windup ? 0.16f :
                stage == WeaponVisualStage.Impact ? 0.22f : 0.32f;
            var weaponScale = weaponId.Equals(WeaponId.GakgungShot) ? 0.58f :
                weaponId.Equals(WeaponId.SingijeonVolley) ? 0.56f :
                weaponId.Equals(WeaponId.HwandoFlyingBlade) ? 0.72f :
                weaponId.Equals(WeaponId.TalismanThrow) ? 0.66f :
                weaponId.Equals(WeaponId.ThunderCrashBomb) ? 0.70f :
                weaponId.Equals(WeaponId.JangseungWard) ? 0.68f :
                weaponId.Equals(WeaponId.FrostFlask) ? 0.66f : 0.64f;
            var powerScale = 1f +
                (level >= 3 ? 0.12f : 0f) +
                (level >= 5 ? 0.12f : 0f) +
                (evolved ? 0.16f : 0f);

            return Mathf.Max(0.05f, authoredScale) * stageScale * weaponScale * powerScale;
        }
    }
}
