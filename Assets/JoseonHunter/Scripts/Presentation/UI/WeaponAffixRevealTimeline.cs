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
            potential0StopsAt = firstPotentialStopsAt;
            potential1StopsAt = secondPotentialStopsAt;
            potential2StopsAt = thirdPotentialStopsAt;
            ReadStartsAt = readStartsAt;
            CloseStartsAt = closeStartsAt;
        }

        public float Duration { get; }
        public float SpinEndsAt { get; }
        public float AffixStopsAt { get; }
        public float ReadStartsAt { get; }
        public float CloseStartsAt { get; }

        public static WeaponAffixRevealTimeline For(WeaponAffixRollResult result)
        {
            if (result == null)
                return default;

            var count = Mathf.Clamp(result.NewPotentials.Count, 0, 3);
            if (count == 1)
                return new WeaponAffixRevealTimeline(1, 2.10f, .52f, .78f, 1.22f, float.PositiveInfinity,
                    float.PositiveInfinity, 1.40f, float.PositiveInfinity);
            if (count == 2)
                return new WeaponAffixRevealTimeline(2, 2.28f, .52f, .78f, 1.22f, 1.40f,
                    float.PositiveInfinity, 1.58f, float.PositiveInfinity);
            if (count == 3)
                return new WeaponAffixRevealTimeline(3, 2.40f, .52f, .78f, 1.22f, 1.40f, 1.58f, 1.76f,
                    float.PositiveInfinity);

            if (result.General.Tier == WeaponAffixTier.Perfect)
                return new WeaponAffixRevealTimeline(0, 1.55f, .48f, .86f, float.PositiveInfinity,
                    float.PositiveInfinity, float.PositiveInfinity, 1.30f, float.PositiveInfinity);
            if (result.General.Tier == WeaponAffixTier.High)
                return new WeaponAffixRevealTimeline(0, 1.45f, .46f, .82f, float.PositiveInfinity,
                    float.PositiveInfinity, float.PositiveInfinity, 1.22f, float.PositiveInfinity);

            return new WeaponAffixRevealTimeline(0, 1.25f, .42f, .76f, float.PositiveInfinity,
                float.PositiveInfinity, float.PositiveInfinity, 1.06f, float.PositiveInfinity);
        }

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
