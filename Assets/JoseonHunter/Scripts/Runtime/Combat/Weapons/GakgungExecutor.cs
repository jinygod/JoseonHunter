using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public sealed class GakgungExecutor : IWeaponExecutor
    {
        private readonly WeaponRuntimeController runtime;
        private readonly LinearProjectileExecutor projectiles;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private float cooldown;

        public GakgungExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int level)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            projectiles = new LinearProjectileExecutor(runtime);
            BaseDamage = Mathf.Max(1f, baseDamage); CooldownSeconds = Mathf.Max(0.01f, cooldownSeconds);
            Range = Mathf.Max(0.01f, range); Speed = Mathf.Max(0.01f, speed); Level = Mathf.Clamp(level, 1, 5);
        }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float Speed { get; }
        public int Level { get; }
        public int LastSelectedTargetRuntimeId { get; private set; }
        public int LastLaunchCount { get; private set; }
        public int ActiveProjectileCount => projectiles.ActiveCount;

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            cooldown -= deltaTime;
            if (cooldown <= 0f && TrySelectTarget(out var target))
            {
                cooldown = CooldownSeconds;
                Launch(context, target);
            }
            projectiles.Tick(deltaTime, context);
        }

        public void Reset()
        {
            cooldown = 0f; LastLaunchCount = 0; LastSelectedTargetRuntimeId = 0; projectiles.Reset();
        }

        private bool TrySelectTarget(out ICombatTarget selected)
        {
            selected = null;
            runtime.Targets.CopyTo(targets);
            foreach (var candidate in targets)
            {
                if (candidate == null || !candidate.IsAlive) continue;
                if (selected == null || IsHigherPriority(candidate, selected)) selected = candidate;
            }
            return selected != null;
        }

        private static bool IsHigherPriority(ICombatTarget candidate, ICombatTarget current)
        {
            if (candidate.IsBoss != current.IsBoss) return candidate.IsBoss;
            if (candidate.IsElite != current.IsElite) return candidate.IsElite;
            var threat = candidate.ThreatScore.CompareTo(current.ThreatScore);
            return threat != 0 ? threat > 0 : candidate.RuntimeId < current.RuntimeId;
        }

        private void Launch(in WeaponExecutionContext context, ICombatTarget target)
        {
            var direction = Direction(context.OwnerPosition, target.WorldPosition);
            LastSelectedTargetRuntimeId = target.RuntimeId;
            LastLaunchCount = Level == 5 ? 3 : 1;
            LaunchArrow(context, direction, 0f, Level == 5 ? 3 : 1);
            if (Level != 5) return;
            LaunchArrow(context, direction, -8f, 1);
            LaunchArrow(context, direction, 8f, 1);
        }

        private void LaunchArrow(in WeaponExecutionContext context, Float2 direction, float degrees, int impacts)
        {
            var shotDirection = Rotate(direction, degrees);
            projectiles.Launch(context, new LinearProjectileSpec(
                new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), WeaponId.GakgungShot,
                context.OwnerPosition, shotDirection, Speed, Range / Speed, Mathf.CeilToInt(BaseDamage), impacts, "Gakgung Arrow"));
        }

        private static Float2 Direction(Float2 origin, Float2 target)
        {
            var x = target.X - origin.X; var y = target.Y - origin.Y; var length = Mathf.Sqrt(x * x + y * y);
            return length > 0.001f ? new Float2(x / length, y / length) : new Float2(1f, 0f);
        }
        private static Float2 Rotate(Float2 value, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad; var cosine = Mathf.Cos(radians); var sine = Mathf.Sin(radians);
            return new Float2(value.X * cosine - value.Y * sine, value.X * sine + value.Y * cosine);
        }
    }
}
