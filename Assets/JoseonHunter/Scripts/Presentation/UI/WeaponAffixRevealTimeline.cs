using JoseonHunter.Domain.Progression;
using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public readonly struct WeaponAffixRevealTimeline
    {
        private readonly int potentialCount;
        private readonly float potential0StopsAt;
        private readonly float potential1StopsAt;
        private readonly float potential2StopsAt;

        private WeaponAffixRevealTimeline(
            int awardedPotentials,
            float duration,
            float spinEndsAt,
            float affixStopsAt,
            float countStartsAt,
            float countEndsAt,
            float firstPotentialStopsAt,
            float secondPotentialStopsAt,
            float thirdPotentialStopsAt,
            float readStartsAt,
            float closeStartsAt)
        {
            potentialCount = awardedPotentials;
            Duration = duration;
            SpinEndsAt = spinEndsAt;
            AffixStopsAt = affixStopsAt;
            CountStartsAt = countStartsAt;
            CountEndsAt = countEndsAt;
            potential0StopsAt = firstPotentialStopsAt;
            potential1StopsAt = secondPotentialStopsAt;
            potential2StopsAt = thirdPotentialStopsAt;
            ReadStartsAt = readStartsAt;
            CloseStartsAt = closeStartsAt;
        }

        public float Duration { get; }
        public float SpinEndsAt { get; }
        public float AffixStopsAt { get; }
        public float CountStartsAt { get; }
        public float CountEndsAt { get; }
        public float ReadStartsAt { get; }
        public float CloseStartsAt { get; }

        public static WeaponAffixRevealTimeline For(WeaponAffixRollResult result)
        {
            if (result == null)
                return default;

            var count = Mathf.Clamp(result.NewPotentials.Count, 0, 3);
            var countDuration = CountDurationFor(result.General.Tier);
            if (count > 0)
            {
                const float affixStop = .78f;
                var countEnd = affixStop + countDuration;
                var firstStop = countEnd + .17f;
                var secondStop = count > 1 ? firstStop + .18f : float.PositiveInfinity;
                var thirdStop = count > 2 ? secondStop + .18f : float.PositiveInfinity;
                var lastStop = count == 1 ? firstStop : count == 2 ? secondStop : thirdStop;
                var readStart = lastStop + .18f;
                return new WeaponAffixRevealTimeline(count, readStart + .37f, .52f, affixStop,
                    affixStop, countEnd, firstStop, secondStop, thirdStop, readStart,
                    float.PositiveInfinity);
            }

            var baseAffixStop = result.General.Tier == WeaponAffixTier.Perfect ? .86f :
                result.General.Tier == WeaponAffixTier.High ? .82f : .76f;
            var spinEnd = result.General.Tier == WeaponAffixTier.Perfect ? .48f :
                result.General.Tier == WeaponAffixTier.High ? .46f : .42f;
            var countEndAt = baseAffixStop + countDuration;
            var readStartAt = countEndAt + .12f;
            return new WeaponAffixRevealTimeline(0, readStartAt + .19f, spinEnd, baseAffixStop,
                baseAffixStop, countEndAt, float.PositiveInfinity, float.PositiveInfinity,
                float.PositiveInfinity, readStartAt, float.PositiveInfinity);
        }

        public static WeaponAffixRevealTimeline For(WeaponAppraisalViewModel model)
        {
            if (model?.Result == null)
                return default;
            if (!model.HasWeaponContext)
                return For(model.Result);
            var profile = WeaponAppraisalPresentation.ProfileFor(model);
            if (profile == WeaponAppraisalRevealProfile.FirstAcquisition)
            {
                var countEnd = .78f + CountDurationFor(model.Result.General.Tier);
                return new WeaponAffixRevealTimeline(0, countEnd + .31f, .42f, .78f, .78f,
                    countEnd, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity,
                    countEnd + .12f, float.PositiveInfinity);
            }
            if (profile != WeaponAppraisalRevealProfile.RepeatStandard)
                return For(model.Result);
            const float repeatCountEnd = .52f + 1.40f;
            return new WeaponAffixRevealTimeline(0, repeatCountEnd + .31f, .24f, .52f, .52f,
                repeatCountEnd, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity,
                repeatCountEnd + .12f,
                float.PositiveInfinity);
        }

        private static float CountDurationFor(WeaponAffixTier tier) =>
            tier == WeaponAffixTier.Standard ? 1.40f : 1.60f;

        public float PotentialStopsAt(int index)
        {
            if (index < 0 || index >= potentialCount)
                return float.PositiveInfinity;
            return index == 0 ? potential0StopsAt : index == 1 ? potential1StopsAt : potential2StopsAt;
        }

        public float SkipFinishAt(float elapsed)
        {
            var skipCap = potentialCount == 3 ? 1.10f : potentialCount == 2 ? 1.02f :
                potentialCount == 1 ? .94f : .82f;
            return Mathf.Min(Duration, Mathf.Max(elapsed + .12f, skipCap));
        }
    }

    public static class WeaponAffixReelMotion
    {
        public static float TravelAt(float time, float spinEndsAt, float stopAt, int reel)
        {
            var startSpeed = 520f + Mathf.Clamp(reel, 0, 3) * 30f;
            var clampedTime = Mathf.Max(0f, time);
            if (clampedTime <= spinEndsAt)
                return clampedTime * startSpeed;

            var decelerationDuration = Mathf.Max(.001f, stopAt - spinEndsAt);
            var decelerationTime = Mathf.Clamp(clampedTime - spinEndsAt, 0f, decelerationDuration);
            const float finalSpeed = 64f;
            var travelDuringDeceleration = startSpeed * decelerationTime +
                .5f * (finalSpeed - startSpeed) * decelerationTime * decelerationTime /
                decelerationDuration;
            return spinEndsAt * startSpeed + travelDuringDeceleration;
        }
    }
}
