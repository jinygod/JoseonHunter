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

        public static bool TryResolve(in DamageRequest request, out DamageResult result)
        {
            if (request.BaseDamage < 0 || request.FlatBonus < 0 || !IsFinite(request.Multiplier))
            {
                result = default;
                return false;
            }

            var damage = ((double)request.BaseDamage + request.FlatBonus) * request.Multiplier;
            if (double.IsNaN(damage) || double.IsInfinity(damage) || damage > int.MaxValue)
            {
                result = default;
                return false;
            }

            var roundedDamage = (int)Math.Round(damage, MidpointRounding.AwayFromZero);
            result = new DamageResult(Math.Max(1, roundedDamage), request.IsCritical);
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
