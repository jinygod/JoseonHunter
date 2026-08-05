using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class EnemyHealthCurve
    {
        public const float DurationSeconds = 900f;

        public static float BaseHealthAt(float elapsedSeconds)
        {
            var time = Mathf.Clamp(elapsedSeconds, 0f, DurationSeconds);
            if (time <= 180f)
                return Mathf.Lerp(18f, 42f, time / 180f);
            if (time <= 360f)
                return Mathf.Lerp(42f, 68f, (time - 180f) / 180f);
            if (time <= 600f)
                return Mathf.Lerp(68f, 105f, (time - 360f) / 240f);
            return Mathf.Lerp(105f, 155f, (time - 600f) / 300f);
        }
    }
}
