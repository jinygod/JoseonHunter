using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class ExperiencePickupMotion
    {
        private const float AccelerationDuration = .32f;

        public static float SpeedAt(float attractionAge, bool forceCollect) =>
            forceCollect
                ? 24f
                : Mathf.Lerp(4f, 14f, Mathf.Clamp01(attractionAge / AccelerationDuration));

        public static Vector2 Step(
            Vector2 current,
            Vector2 target,
            float attractionAge,
            float deltaTime,
            bool forceCollect) =>
            Vector2.MoveTowards(
                current,
                target,
                SpeedAt(attractionAge, forceCollect) * Mathf.Max(0f, deltaTime));

        public static Vector3 StretchAt(Vector2 direction, float attractionAge)
        {
            if (direction.sqrMagnitude <= .0001f)
                return Vector3.one;
            var progress = Mathf.Clamp01(attractionAge / AccelerationDuration);
            return new Vector3(
                Mathf.Lerp(1f, 1.42f, progress),
                Mathf.Lerp(1f, .82f, progress),
                1f);
        }
    }
}
