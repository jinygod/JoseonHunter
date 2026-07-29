using JoseonHunter.Domain.Progression;
using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public static class WeaponAffixValueFormatter
    {
        public static string Describe(WeaponAffixRoll roll)
        {
            var value = Mathf.RoundToInt((float)roll.Value);
            var sign = value > 0 ? "+" : string.Empty;
            return $"{roll.Stat} {sign}{value}%";
        }
    }
}
