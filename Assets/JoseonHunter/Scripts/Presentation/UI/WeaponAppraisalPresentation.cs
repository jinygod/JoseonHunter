using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public enum WeaponAppraisalRevealProfile
    {
        FirstAcquisition,
        RepeatStandard,
        Ceremonial
    }

    public enum WeaponPotentialSlotKind
    {
        Existing,
        Shaking,
        Revealed,
        Empty
    }

    public static class WeaponAppraisalPresentation
    {
        public static WeaponAppraisalRevealProfile ProfileFor(WeaponAppraisalViewModel model)
        {
            if (model == null || model.Result == null)
                return WeaponAppraisalRevealProfile.RepeatStandard;
            if (!model.HasWeaponContext)
                return WeaponAppraisalRevealProfile.FirstAcquisition;
            if (model.Result.NewPotentials.Count > 0 ||
                model.Result.General.Tier != Domain.Progression.WeaponAffixTier.Standard)
                return WeaponAppraisalRevealProfile.Ceremonial;
            return model.IsNewAcquisition
                ? WeaponAppraisalRevealProfile.FirstAcquisition
                : WeaponAppraisalRevealProfile.RepeatStandard;
        }

        public static float ScrollOpenAt(WeaponAppraisalRevealProfile profile, float time)
        {
            var start = profile == WeaponAppraisalRevealProfile.RepeatStandard ? .58f :
                profile == WeaponAppraisalRevealProfile.Ceremonial ? .12f : .06f;
            var duration = profile == WeaponAppraisalRevealProfile.RepeatStandard ? .12f :
                profile == WeaponAppraisalRevealProfile.Ceremonial ? .30f : .36f;
            var progress = Mathf.Clamp01(time / duration);
            var inverse = 1f - progress;
            var eased = 1f - inverse * inverse * inverse;
            return Mathf.Lerp(start, 1f, eased);
        }

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
