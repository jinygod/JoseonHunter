using System;
using JoseonHunter.Domain.Combat;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public enum EnemyArchetype
    {
        Normal,
        ShieldDokkaebi,
        SpiritShaman,
        ChargingHornGhost,
        SplittingRat
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
            _ => new EnemyArchetypeProfile(EnemyArchetype.Normal, contentId ?? string.Empty, 1f, 1f, 1f)
        };

        public float IncomingDamageMultiplier(Vector2 facing, Vector2 attackDirection, WeaponHitTrait traits)
        {
            if (Archetype != EnemyArchetype.ShieldDokkaebi ||
                (traits & (WeaponHitTrait.Explosion | WeaponHitTrait.Pull | WeaponHitTrait.Reaction)) != 0)
                return 1f;
            if (facing.sqrMagnitude < .0001f || attackDirection.sqrMagnitude < .0001f) return 1f;
            return Vector2.Dot(facing.normalized, attackDirection.normalized) >= .5f ? .65f : 1f;
        }
    }
}
