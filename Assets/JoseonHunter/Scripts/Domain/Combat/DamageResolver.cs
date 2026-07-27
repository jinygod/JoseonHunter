using System;

namespace JoseonHunter.Domain.Combat
{
    public static class DamageResolver
    {
        public static DamageResult Resolve(in DamageRequest request)
        {
            var damage = (request.BaseDamage + request.FlatBonus) * request.Multiplier;
            var roundedDamage = (int)Math.Round(damage, MidpointRounding.AwayFromZero);
            return new DamageResult(Math.Max(1, roundedDamage), request.IsCritical);
        }
    }
}
