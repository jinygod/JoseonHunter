using System;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class ViewportSpawnGeometry
    {
        public static Vector2 PointOnExpandedPerimeter(Rect bounds, int side, float t, float margin)
        {
            t = Mathf.Clamp01(t);
            margin = Mathf.Max(0f, margin);
            return side switch
            {
                0 => new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, t), bounds.yMax + margin),
                1 => new Vector2(bounds.xMax + margin, Mathf.Lerp(bounds.yMin, bounds.yMax, t)),
                2 => new Vector2(Mathf.Lerp(bounds.xMax, bounds.xMin, t), bounds.yMin - margin),
                3 => new Vector2(bounds.xMin - margin, Mathf.Lerp(bounds.yMax, bounds.yMin, t)),
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };
        }
    }
}
