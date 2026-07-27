using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public sealed class SingijeonExecutor : IWeaponExecutor
    {
        private const float BucketDegrees = 30f;
        private const int BucketCount = 12;
        public const int MaxLaneCount = 6;
        private readonly WeaponRuntimeController runtime;
        private readonly LinearProjectileExecutor projectiles;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private float cooldown;

        public SingijeonExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int laneCount, int level)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            projectiles = new LinearProjectileExecutor(runtime);
            BaseDamage = Mathf.Max(1f, baseDamage); CooldownSeconds = Mathf.Max(0.01f, cooldownSeconds);
            Range = Mathf.Max(0.01f, range); Speed = Mathf.Max(0.01f, speed); LaneCount = Mathf.Clamp(laneCount, 1, MaxLaneCount); Level = Mathf.Clamp(level, 1, 5);
        }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float Speed { get; }
        public int LaneCount { get; }
        public int Level { get; }
        public int LastLaunchCount { get; private set; }
        public int ActiveProjectileCount => projectiles.ActiveCount;
        public Float2 LastDirection { get; private set; }

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            cooldown -= deltaTime;
            if (cooldown <= 0f && TryFindDensestDirection(context.OwnerPosition, out var direction))
            {
                cooldown = CooldownSeconds;
                Launch(context, direction);
            }
            projectiles.Tick(deltaTime, context);
        }

        public void Reset()
        {
            cooldown = 0f; LastLaunchCount = 0; LastDirection = default; projectiles.Reset();
        }

        private bool TryFindDensestDirection(Float2 origin, out Float2 direction)
        {
            targets.Clear(); runtime.Targets.CopyTo(targets);
            var counts = new Dictionary<int, int>();
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;
                var x = target.WorldPosition.X - origin.X; var y = target.WorldPosition.Y - origin.Y;
                if (x * x + y * y < 0.0001f) continue;
                var rawBucket = Mathf.FloorToInt((Mathf.Atan2(y, x) * Mathf.Rad2Deg + BucketDegrees * 0.5f) / BucketDegrees);
                var bucket = ((rawBucket % BucketCount) + BucketCount) % BucketCount;
                counts.TryGetValue(bucket, out var count); counts[bucket] = count + 1;
            }
            var selectedBucket = 0; var highestCount = 0;
            foreach (var pair in counts)
            {
                if (pair.Value > highestCount || pair.Value == highestCount && pair.Key < selectedBucket)
                {
                    selectedBucket = pair.Key; highestCount = pair.Value;
                }
            }
            if (highestCount == 0) { direction = default; return false; }
            var radians = selectedBucket * BucketDegrees * Mathf.Deg2Rad;
            direction = new Float2(Mathf.Cos(radians), Mathf.Sin(radians));
            return true;
        }

        private void Launch(in WeaponExecutionContext context, Float2 direction)
        {
            LastDirection = direction;
            var rows = Level == 5 ? 3 : 1;
            LastLaunchCount = rows * LaneCount;
            for (var row = 0; row < rows; row++)
            for (var lane = 0; lane < LaneCount; lane++)
            {
                var laneOffset = lane - (LaneCount - 1) * 0.5f;
                var rowOffset = (row - (rows - 1) * 0.5f) * 0.1f;
                var perpendicular = new Float2(-direction.Y, direction.X);
                var position = new Float2(context.OwnerPosition.X + perpendicular.X * laneOffset * 0.12f - direction.X * rowOffset,
                    context.OwnerPosition.Y + perpendicular.Y * laneOffset * 0.12f - direction.Y * rowOffset);
                projectiles.Launch(context, new LinearProjectileSpec(
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), WeaponId.SingijeonVolley,
                    position, direction, Speed, Range / Speed, Mathf.CeilToInt(BaseDamage), 1, "Singijeon Rocket"));
            }
        }
    }
}
