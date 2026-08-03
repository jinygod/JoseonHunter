using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Runtime.Combat
{
    public interface ICombatTarget
    {
        int RuntimeId { get; }
        bool IsAlive { get; }
        int Health { get; }
        bool IsBoss { get; }
        bool IsElite { get; }
        float ThreatScore { get; }
        Float2 WorldPosition { get; }
        PixelHitMask HurtMask { get; }
        PixelMaskTransform HurtMaskTransform { get; }
        void ApplyResolvedDamage(int damage);
        void ApplyKnockback(Float2 direction, float force);
    }

    public interface IControlStatusTarget
    {
        void ApplyStagger(float durationSeconds);
    }

    public interface IIncomingDamageResistanceTarget
    {
        float IncomingDamageMultiplier(Float2 attackOrigin, WeaponHitTrait traits);
    }
}
