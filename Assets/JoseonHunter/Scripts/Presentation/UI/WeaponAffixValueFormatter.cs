using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons;
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
            return WeaponAffixDisplayFormatter.Describe(roll, displayedValue);
        }
    }
}
