using JoseonHunter.Domain.Combat;
using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public static class JoseonUiPalette
    {
        public static readonly Color Ink = new(0.055f, 0.064f, 0.082f, 0.96f);
        public static readonly Color Hanji = new(0.91f, 0.86f, 0.72f, 1f);
        public static readonly Color Crimson = new(0.72f, 0.12f, 0.13f, 1f);
        public static readonly Color Jade = new(0.20f, 0.72f, 0.68f, 1f);
        public static readonly Color Gold = new(0.94f, 0.67f, 0.20f, 1f);

        public static Color WeaponAccent(WeaponId id)
        {
            if (id.Equals(WeaponId.FrostFlask)) return Jade;
            if (id.Equals(WeaponId.ThunderCrashBomb) || id.Equals(WeaponId.WindThunderFan))
                return new Color(0.62f, 0.42f, 0.94f, 1f);
            if (id.Equals(WeaponId.SingijeonVolley)) return new Color(0.94f, 0.34f, 0.18f, 1f);
            return Gold;
        }
    }
}
