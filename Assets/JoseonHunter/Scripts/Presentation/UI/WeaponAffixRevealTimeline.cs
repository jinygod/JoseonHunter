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
                return new WeaponAffixRevealTimeline(1, 1.38f, .52f, .60f, .76f, float.PositiveInfinity,
                    float.PositiveInfinity, .84f, 1.24f);
            if (count == 2)
                return new WeaponAffixRevealTimeline(2, 1.66f, .52f, .60f, .76f, .94f,
                    float.PositiveInfinity, 1.02f, 1.52f);
            if (count == 3)
                return new WeaponAffixRevealTimeline(3, 1.96f, .52f, .60f, .76f, .94f, 1.12f, 1.20f, 1.82f);

            if (result.General.Tier == WeaponAffixTier.Perfect)
                return new WeaponAffixRevealTimeline(0, 1.28f, .52f, .60f, float.PositiveInfinity,
                    float.PositiveInfinity, float.PositiveInfinity, .74f, 1.14f);
            if (result.General.Tier == WeaponAffixTier.High)
                return new WeaponAffixRevealTimeline(0, 1.08f, .48f, .56f, float.PositiveInfinity,
                    float.PositiveInfinity, float.PositiveInfinity, .66f, .94f);

            return new WeaponAffixRevealTimeline(0, .86f, .40f, .48f, float.PositiveInfinity,
                float.PositiveInfinity, float.PositiveInfinity, .56f, .74f);
        }

        public float PotentialStopsAt(int index)
        {
            if (index < 0 || index >= potentialCount)
                return float.PositiveInfinity;
            return index == 0 ? potential0StopsAt : index == 1 ? potential1StopsAt : potential2StopsAt;
        }

        public float SkipFinishAt(float elapsed)
        {
            var minimumReadableFinish = AffixStopsAt + .14f;
            if (potentialCount > 0)
                minimumReadableFinish = PotentialStopsAt(potentialCount - 1) + .18f;
            var skipCap = potentialCount == 3 ? .84f : potentialCount == 2 ? .76f : potentialCount == 1 ? .70f : .62f;
            return Mathf.Min(Duration, Mathf.Max(elapsed + .12f, minimumReadableFinish, skipCap));
        }
    }
}
