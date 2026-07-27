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
            if (request.BaseDamage < 0 || request.FlatBonus < 0 || request.Multiplier < 0f || !IsFinite(request.Multiplier))
            {
                result = default;
                return false;
            }

            var damage = ((double)request.BaseDamage + request.FlatBonus) * request.Multiplier;
            var roundedDamage = Math.Round(damage, MidpointRounding.AwayFromZero);
            if (double.IsNaN(roundedDamage) || double.IsInfinity(roundedDamage) || roundedDamage < int.MinValue || roundedDamage > int.MaxValue)
            {
                result = default;
                return false;
            }

            result = new DamageResult(Math.Max(1, (int)roundedDamage), request.IsCritical);
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
