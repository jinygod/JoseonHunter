using JoseonHunter.Domain.Progression;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public static class WeaponAffixDisplayFormatter
    {
        public static string Describe(WeaponAffixRoll roll, int displayedValue)
        {
            return Describe(roll.Stat, displayedValue);
        }

        public static string Describe(WeaponAffixStat stat, int displayedValue)
        {
            var sign = displayedValue >= 0 ? "+" : string.Empty;
            return $"{KoreanName(stat)} {sign}{displayedValue}%";
        }

        public static string KoreanName(WeaponAffixStat stat)
        {
            switch (stat)
            {
                case WeaponAffixStat.Damage: return "피해량";
                case WeaponAffixStat.Cooldown: return "재사용 대기시간";
                case WeaponAffixStat.Area: return "공격 범위";
                case WeaponAffixStat.ProjectileSpeed: return "투사체 속도";
                case WeaponAffixStat.Duration: return "지속 시간";
                default: return stat.ToString();
            }
        }
    }
}
