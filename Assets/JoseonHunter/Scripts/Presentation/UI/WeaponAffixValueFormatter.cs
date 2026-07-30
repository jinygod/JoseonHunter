using JoseonHunter.Domain.Progression;
using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public static class WeaponAffixValueFormatter
    {
        public static string Describe(WeaponAffixRoll roll)
        {
            return Describe(roll, Mathf.RoundToInt((float)roll.Value));
        }

        public static string Describe(WeaponAffixRoll roll, int displayedValue)
        {
            var sign = displayedValue >= 0 ? "+" : string.Empty;
            return $"{roll.Stat} {sign}{displayedValue}%";
        }
    }
}
