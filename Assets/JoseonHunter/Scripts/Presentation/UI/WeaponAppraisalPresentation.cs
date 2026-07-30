using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public enum WeaponPotentialSlotKind
    {
        Existing,
        Shaking,
        Revealed,
        Empty
    }

    public static class WeaponAppraisalPresentation
    {
        public static int DisplayValueAt(double target, float progress)
        {
            var clamped = Mathf.Clamp01(progress);
            var inverse = 1f - clamped;
            var eased = 1f - inverse * inverse * inverse;
            return Mathf.RoundToInt((float)target * eased);
        }

        public static WeaponPotentialSlotKind ResolveSlot(
            int slotIndex,
            int existingPotentialCount,
            int awardedPotentialCount,
            float time,
            WeaponAffixRevealTimeline timeline)
        {
            if (slotIndex < 0 || slotIndex >= 3)
                return WeaponPotentialSlotKind.Empty;

            existingPotentialCount = Mathf.Clamp(existingPotentialCount, 0, 3);
            awardedPotentialCount = Mathf.Clamp(awardedPotentialCount, 0, 3 - existingPotentialCount);
            if (slotIndex < existingPotentialCount)
                return WeaponPotentialSlotKind.Existing;

            var awardedIndex = slotIndex - existingPotentialCount;
            if (awardedIndex < awardedPotentialCount)
                return time >= timeline.PotentialStopsAt(awardedIndex)
                    ? WeaponPotentialSlotKind.Revealed
                    : WeaponPotentialSlotKind.Shaking;

            var isAttemptedEmptySlot = awardedPotentialCount == 0 && awardedIndex == 0;
            if (isAttemptedEmptySlot && time >= timeline.AffixStopsAt && time < timeline.ReadStartsAt)
                return WeaponPotentialSlotKind.Shaking;

            return WeaponPotentialSlotKind.Empty;
        }
    }
}
