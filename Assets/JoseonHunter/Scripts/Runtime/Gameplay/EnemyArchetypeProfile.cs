using System;
using JoseonHunter.Domain.Combat;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public readonly struct ShieldGuardHitResult
    {
        public ShieldGuardHitResult(int remainingCharges, bool blocked, bool broke)
        {
            RemainingCharges = Mathf.Clamp(remainingCharges, 0, ShieldDokkaebiGuard.MaximumCharges);
            Blocked = blocked;
            Broke = broke;
        }

        public int RemainingCharges { get; }
        public bool Blocked { get; }
        public bool Broke { get; }
    }

    public static class ShieldDokkaebiGuard
    {
        public const int MaximumCharges = 6;
        public const float BlockedDamageMultiplier = .15f;
        private const WeaponHitTrait BypassTraits = WeaponHitTrait.Explosion | WeaponHitTrait.Pull |
                                                   WeaponHitTrait.Reaction;

        public static float IncomingDamageMultiplier(int remainingCharges, Vector2 facing,
            Vector2 attackDirection, WeaponHitTrait traits) =>
            Blocks(remainingCharges, facing, attackDirection, traits) ? BlockedDamageMultiplier : 1f;

        public static ShieldGuardHitResult ConfirmHit(int remainingCharges, Vector2 facing,
            Vector2 attackDirection, WeaponHitTrait traits)
        {
            if (!Blocks(remainingCharges, facing, attackDirection, traits))
                return new ShieldGuardHitResult(remainingCharges, false, false);
            var next = Mathf.Max(0, remainingCharges - 1);
            return new ShieldGuardHitResult(next, true, next == 0);
        }

        private static bool Blocks(int remainingCharges, Vector2 facing, Vector2 attackDirection,
            WeaponHitTrait traits)
        {
            if (remainingCharges <= 0 || (traits & BypassTraits) != 0 ||
                facing.sqrMagnitude < .0001f || attackDirection.sqrMagnitude < .0001f) return false;
            return Vector2.Dot(facing.normalized, attackDirection.normalized) >= .5f;
        }
    }

    public enum EnemyArchetype
    {
        Normal,
        ShieldDokkaebi,
        SpiritShaman,
        ChargingHornGhost,
        SplittingRat,
        StoneThrower,
        RedHornElite,
        TombArcherGhost,
        RedLanternWraith,
        CurseShaman,
        GraveAmbusherElite
    }

    /// <summary>Small immutable ruleset for readable special enemies. No profile grants immunity.</summary>
    public sealed class EnemyArchetypeProfile
    {
        private EnemyArchetypeProfile(EnemyArchetype archetype, string contentId, float healthMultiplier,
            float speedMultiplier, float contactMultiplier, float displayScaleMultiplier = 1f)
        {
            Archetype = archetype; ContentId = contentId; HealthMultiplier = healthMultiplier;
            SpeedMultiplier = speedMultiplier; ContactMultiplier = contactMultiplier;
            DisplayScaleMultiplier = displayScaleMultiplier;
        }

        public EnemyArchetype Archetype { get; }
        public string ContentId { get; }
        public float HealthMultiplier { get; }
        public float SpeedMultiplier { get; }
        public float ContactMultiplier { get; }
        public float DisplayScaleMultiplier { get; }
        public bool IsSpecial => Archetype != EnemyArchetype.Normal;

        public static EnemyArchetypeProfile ForContentId(string contentId) => contentId switch
        {
            "plague_rat" => new EnemyArchetypeProfile(EnemyArchetype.Normal, contentId, .75f, 1.10f, .80f),
            "vengeful_spirit" => new EnemyArchetypeProfile(EnemyArchetype.Normal, contentId, .55f, 1.65f, .75f),
            "dokkaebi" => new EnemyArchetypeProfile(EnemyArchetype.Normal, contentId, 2.60f, .55f, 1.35f, 1.15f),
            "shield_dokkaebi" => new EnemyArchetypeProfile(EnemyArchetype.ShieldDokkaebi, contentId, 1.45f, .82f, 1.05f),
            "spirit_shaman" => new EnemyArchetypeProfile(EnemyArchetype.SpiritShaman, contentId, 1.15f, .78f, .9f),
            "charging_horn_ghost" => new EnemyArchetypeProfile(EnemyArchetype.ChargingHornGhost, contentId, 1.3f, .92f, 1.15f),
            "splitting_rat" => new EnemyArchetypeProfile(EnemyArchetype.SplittingRat, contentId, 1.1f, 1.05f, .85f),
            "club_dokkaebi" => new EnemyArchetypeProfile(EnemyArchetype.Normal, contentId, 2.0f, .65f, 1.15f, 1.08f),
            "shield_guard_dokkaebi" => new EnemyArchetypeProfile(EnemyArchetype.ShieldDokkaebi, contentId, 2.2f, .55f, 1.1f, 1.12f),
            "iron_horn_dokkaebi" => new EnemyArchetypeProfile(EnemyArchetype.ChargingHornGhost, contentId, 1.8f, .8f, 1.25f, 1.1f),
            "stone_thrower_dokkaebi" => new EnemyArchetypeProfile(EnemyArchetype.StoneThrower, contentId, 1.35f, .62f, .85f, 1.05f),
            "red_horn_elite" => new EnemyArchetypeProfile(EnemyArchetype.RedHornElite, contentId, 3.2f, .85f, 1.4f, 1.25f),
            "tomb_attendant" => new EnemyArchetypeProfile(EnemyArchetype.Normal, contentId, 1.35f, .86f, 1.05f),
            "tomb_archer_ghost" => new EnemyArchetypeProfile(EnemyArchetype.TombArcherGhost, contentId, 1.5f, .68f, .75f, 1.06f),
            "red_lantern_wraith" => new EnemyArchetypeProfile(EnemyArchetype.RedLanternWraith, contentId, 1.7f, 1.15f, .95f, 1.08f),
            "curse_shaman" => new EnemyArchetypeProfile(EnemyArchetype.CurseShaman, contentId, 2f, .62f, .8f, 1.12f),
            "grave_ambusher_elite" => new EnemyArchetypeProfile(EnemyArchetype.GraveAmbusherElite, contentId, 3.8f, .75f, 1.45f, 1.3f),
            _ => new EnemyArchetypeProfile(EnemyArchetype.Normal, contentId ?? string.Empty, 1f, 1f, 1f)
        };

        public float IncomingDamageMultiplier(Vector2 facing, Vector2 attackDirection, WeaponHitTrait traits)
        {
            return Archetype == EnemyArchetype.ShieldDokkaebi
                ? ShieldDokkaebiGuard.IncomingDamageMultiplier(ShieldDokkaebiGuard.MaximumCharges,
                    facing, attackDirection, traits)
                : 1f;
        }
    }
}
