using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public sealed class GakgungExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        private readonly WeaponRuntimeController runtime;
        private readonly LinearProjectileExecutor projectiles;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private float cooldown;
        private int shotSequence;
        private readonly Dictionary<int, ArrowInfo> primaryArrows = new Dictionary<int, ArrowInfo>();
        private readonly HashSet<int> firstImpacts = new HashSet<int>();
        private readonly List<SplitArrow> splitArrows = new List<SplitArrow>();
        private Transform presentationRoot;
        private Sprite impactSprite;
        private int effectSortingOrder;
        private WeaponTransientVisualPool transientVisuals;
        private Transform transientVisualRoot;
#if UNITY_INCLUDE_TESTS
        private readonly List<int> splitChildAttackIdsForTests = new List<int>();
        private readonly List<int> levelFiveSideArrowAttackIdsForTests = new List<int>();
        private readonly List<int> armorBreakApplicationAttackIdsForTests = new List<int>();
#endif

        public GakgungExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            projectiles = new LinearProjectileExecutor(runtime);
            var splitDamage = modifiers.Legacy.Is(WeaponLegacyPathId.GakgungSplitFletching) ? .75f : 1f;
            var sunCadence = modifiers.Legacy.Is(WeaponLegacyPathId.GakgungSunPiercer) ? 1.25f : 1f;
            BaseDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage) * splitDamage);
            CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds) * sunCadence);
            Range = Mathf.Max(0.01f, modifiers.ScaleArea(range)); Speed = Mathf.Max(0.01f, modifiers.ScaleSpeed(speed)); Level = Mathf.Clamp(level, 1, 5); Potentials = modifiers;
            IsEvolved = evolved;
            runtime.DamageService.DamageConfirmed += OnDamageConfirmed;
        }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float Speed { get; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public int LastSelectedTargetRuntimeId { get; private set; }
        public int LastLaunchCount { get; private set; }
        public int ActiveProjectileCount => projectiles.ActiveCount;
        public int LastProjectileMaximumImpacts { get; private set; }
        public float LastProjectileScale { get; private set; } = 1f;
#if UNITY_INCLUDE_TESTS
        public int LastArmorBreakPrimaryAttackIdForTests { get; private set; }
        public IReadOnlyList<int> SplitChildAttackIdsForTests => splitChildAttackIdsForTests;
        public IReadOnlyList<int> LevelFiveSideArrowAttackIdsForTests => levelFiveSideArrowAttackIdsForTests;
        public IReadOnlyList<int> ArmorBreakApplicationAttackIdsForTests => armorBreakApplicationAttackIdsForTests;
#endif

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            EnsureTransientVisuals(context.PresentationRoot);
            transientVisuals?.Tick(deltaTime);
            presentationRoot = context.PresentationRoot;
            impactSprite = context.PresentationSpriteFor(
                WeaponId.GakgungShot,
                WeaponVisualPartIndex.Gakgung.Impact);
            effectSortingOrder = context.SortingOrder + 2;
            cooldown -= deltaTime;
            if (cooldown <= 0f && TrySelectTarget(context.OwnerPosition, out var target))
            {
                cooldown = CooldownSeconds;
                Launch(context, target);
            }
            projectiles.Tick(deltaTime, context);
            for (var index = splitArrows.Count - 1; index >= 0; index--)
            {
                var child = splitArrows[index]; child.Delay -= Mathf.Max(0f, deltaTime);
                if (child.Delay > 0f) { splitArrows[index] = child; continue; }
                var travel = Mathf.Min(child.RemainingRange, Speed * Mathf.Max(0f, deltaTime));
                child.Position = new Float2(child.Position.X + child.Direction.X * travel, child.Position.Y + child.Direction.Y * travel);
                child.RemainingRange -= travel;
                runtime.Targets.CopyTo(targets);
                var hit = false;
                foreach (var splitTarget in targets)
                {
                    if (splitTarget == null || !splitTarget.IsAlive || splitTarget.HurtMask == null || !PixelMaskContactService.TryFindContact(child.Mask, PixelMaskTransform.Translation(child.Position.X, child.Position.Y), splitTarget.HurtMask, splitTarget.HurtMaskTransform, out var contact)) continue;
                    hit = runtime.DamageService.TryApply(WeaponDamageRequest.Create(child.Attack,
                        WeaponId.GakgungShot, splitTarget, Mathf.CeilToInt(BaseDamage * .45f), false,
                        contact, ContactPhase.PotentialChain, context.SimulationTick, true,
                        WeaponHitTrait.Pierce, child.Position), out _);
                    if (hit) break;
                }
                if (!hit && child.RemainingRange > 0f) { splitArrows[index] = child; continue; }
                runtime.DamageService.RetireAttack(child.Attack.InstanceId); splitArrows.RemoveAt(index);
            }
        }

        public void Reset()
        {
            cooldown = 0f; shotSequence = 0; LastLaunchCount = 0; LastSelectedTargetRuntimeId = 0;
            LastProjectileMaximumImpacts = 0; LastProjectileScale = 1f; projectiles.Reset(); primaryArrows.Clear(); firstImpacts.Clear();
            foreach (var child in splitArrows) runtime.DamageService.RetireAttack(child.Attack.InstanceId); splitArrows.Clear();
            transientVisuals?.Dispose();
            transientVisuals = null;
            transientVisualRoot = null;
#if UNITY_INCLUDE_TESTS
            LastArmorBreakPrimaryAttackIdForTests = 0; splitChildAttackIdsForTests.Clear();
            levelFiveSideArrowAttackIdsForTests.Clear(); armorBreakApplicationAttackIdsForTests.Clear();
#endif
        }

        public void Dispose() { runtime.DamageService.DamageConfirmed -= OnDamageConfirmed; Reset(); projectiles.Dispose(); }

        private bool TrySelectTarget(Float2 ownerPosition, out ICombatTarget selected)
        {
            selected = null;
            runtime.Targets.CopyTo(targets);
            var rangeSquared = Range * Range;
            foreach (var candidate in targets)
            {
                if (candidate == null || !candidate.IsAlive ||
                    !runtime.IsTargetVisible(candidate.WorldPosition) ||
                    DistanceSquared(ownerPosition, candidate.WorldPosition) > rangeSquared) continue;
                if (selected == null || IsHigherPriority(candidate, selected, ownerPosition)) selected = candidate;
            }
            return selected != null;
        }

        private static bool IsHigherPriority(ICombatTarget candidate, ICombatTarget current, Float2 ownerPosition)
        {
            if (candidate.IsBoss != current.IsBoss) return candidate.IsBoss;
            if (candidate.IsElite != current.IsElite) return candidate.IsElite;
            var threat = candidate.ThreatScore.CompareTo(current.ThreatScore);
            if (threat != 0) return threat > 0;
            var distance = DistanceSquared(ownerPosition, candidate.WorldPosition)
                .CompareTo(DistanceSquared(ownerPosition, current.WorldPosition));
            return distance != 0 ? distance < 0 : candidate.RuntimeId < current.RuntimeId;
        }

        private static float DistanceSquared(Float2 first, Float2 second)
        {
            var x = first.X - second.X;
            var y = first.Y - second.Y;
            return x * x + y * y;
        }

        private void Launch(in WeaponExecutionContext context, ICombatTarget target)
        {
            var windupCue = new WeaponVisualCue(
                WeaponId.GakgungShot,
                WeaponVisualStage.Windup,
                Level,
                IsEvolved,
                .24f,
                .07f);
            transientVisuals?.Play(
                context.PresentationSpriteFor(
                    WeaponId.GakgungShot,
                    WeaponVisualPartIndex.Gakgung.Windup),
                new Vector3(target.WorldPosition.X, target.WorldPosition.Y, 0f),
                Quaternion.identity,
                Vector3.one * windupCue.ResolvedScale,
                Color.white,
                .07f,
                context.SortingOrder + 1);
            var direction = Direction(context.OwnerPosition, target.WorldPosition);
            var targetDelta = new Float2(target.WorldPosition.X - context.OwnerPosition.X, target.WorldPosition.Y - context.OwnerPosition.Y);
            var targetDistance = Mathf.Sqrt(targetDelta.X * targetDelta.X + targetDelta.Y * targetDelta.Y);
            shotSequence++;
            if (Potentials.Legacy.Is(WeaponLegacyPathId.GakgungSplitFletching))
            {
                LaunchSplitLegacyVolley(context, direction, targetDistance, target);
                return;
            }
            var sunLegacy = Potentials.Legacy.Is(WeaponLegacyPathId.GakgungSunPiercer);
            var sunPiercer = IsEvolved && shotSequence % 4 == 0;
            var impacts = sunPiercer ? 8 : sunLegacy ? 4 : (Level == 5 ? 3 : 1);
            var damage = Mathf.CeilToInt(BaseDamage * (sunPiercer ? 3f : 1f));
            var scale = Mathf.Clamp(sunPiercer ? 1.08f : 1f, .72f, 1.08f);
            var speed = sunPiercer ? Speed * 0.7f : Speed;
            LastSelectedTargetRuntimeId = target.RuntimeId;
            LastLaunchCount = sunLegacy ? 1 : Level == 5 ? 3 : 1;
            LastProjectileMaximumImpacts = impacts;
            LastProjectileScale = scale;
            LaunchArrow(context, direction, 0f, impacts, damage, speed, scale, sunPiercer || sunLegacy,
                true, targetDistance, target, sunLegacy ? .15f : 0f,
                sunLegacy && Potentials.Legacy.Stage == WeaponLegacyStage.Completed ? 1.3f : 1f);
            if (Level != 5 || sunLegacy) return;
            LaunchArrow(context, direction, -8f, 1, Mathf.CeilToInt(BaseDamage), Speed, 1f, false, false, targetDistance, target);
            LaunchArrow(context, direction, 8f, 1, Mathf.CeilToInt(BaseDamage), Speed, 1f, false, false, targetDistance, target);
            LastProjectileScale = scale;
        }

        private void LaunchArrow(in WeaponExecutionContext context, Float2 direction, float degrees, int impacts,
            int damage, float speed, float scale, bool allowExtendedImpacts, bool primary,
            float targetDistance, ICombatTarget target, float damagePerImpactBonus = 0f,
            float bossDamageMultiplier = 1f)
        {
            var shotDirection = Rotate(direction, degrees);
            PixelHitMask drawMask = null;
            var fullDraw = primary && Potentials.HasPotential(WeaponPotentialId.GakgungFullDraw) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.GakgungFullDraw, out _, out drawMask);
            var attack = new AttackInstance(runtime.AllocateAttackInstanceId(),
                impacts > 1 ? RepeatHitPolicy.OncePerPhase : RepeatHitPolicy.OncePerInstance, 0f);
#if UNITY_INCLUDE_TESTS
            if (!primary) levelFiveSideArrowAttackIdsForTests.Add(attack.InstanceId);
#endif
            if (primary) primaryArrows[attack.InstanceId] = new ArrowInfo(context.OwnerPosition, Range, impacts);
            projectiles.Launch(context, new LinearProjectileSpec(
                attack, WeaponId.GakgungShot,
                context.OwnerPosition, shotDirection, speed, Range / speed, damage, impacts, "Gakgung Arrow", scale,
                 allowExtendedImpacts, fullDraw, fullDraw ? drawMask : null,
                 degrees == 0f ? 0.18f : Mathf.Sign(degrees) * 0.11f,
                 0.28f,
                 WeaponVisualPartIndex.Gakgung.Projectile,
                 WeaponVisualPartIndex.Gakgung.ProjectileFrameCount,
                 .05f,
                 WeaponHitTrait.Pierce,
                 damagePerImpactBonus,
                 bossDamageMultiplier));
        }

        private void LaunchSplitLegacyVolley(in WeaponExecutionContext context, Float2 direction,
            float targetDistance, ICombatTarget target)
        {
            var completedBurst = Potentials.Legacy.Stage == WeaponLegacyStage.Completed && shotSequence % 4 == 0;
            var count = completedBurst ? 7 : Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced ? 5 : 3;
            var sideMultiplier = completedBurst ? .55f : Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced
                ? .6f
                : .7f;
            LastSelectedTargetRuntimeId = target.RuntimeId;
            LastLaunchCount = count;
            LastProjectileMaximumImpacts = 1;
            LastProjectileScale = 1f;
            var middle = count / 2;
            for (var index = 0; index < count; index++)
            {
                var offset = (index - middle) * 9f;
                var multiplier = index == middle && !completedBurst ? 1f : sideMultiplier;
                LaunchArrow(context, direction, offset, 1, Mathf.CeilToInt(BaseDamage * multiplier),
                    Speed, 1f, false, index == middle, targetDistance, target);
            }
        }

        private void OnDamageConfirmed(ConfirmedDamageEvent damage)
        {
            if (!damage.WeaponId.Equals(WeaponId.GakgungShot) || !primaryArrows.TryGetValue(damage.AttackInstanceId, out var arrow)) return;
            arrow.ImpactCount++;
            var vectorFromLaunch = new Float2(
                damage.ContactPoint.X - arrow.Start.X,
                damage.ContactPoint.Y - arrow.Start.Y);
            var rotation = Mathf.Atan2(vectorFromLaunch.Y, vectorFromLaunch.X) * Mathf.Rad2Deg;
            var impactCue = new WeaponVisualCue(
                WeaponId.GakgungShot,
                WeaponVisualStage.Impact,
                Level,
                IsEvolved,
                .28f,
                .14f);
            EnsureTransientVisuals(presentationRoot);
            transientVisuals?.Play(
                impactSprite,
                new Vector3(damage.ContactPoint.X, damage.ContactPoint.Y, 0f),
                Quaternion.Euler(0f, 0f, rotation),
                Vector3.one * impactCue.ResolvedScale,
                Color.white,
                impactCue.ResolvedLifetime,
                effectSortingOrder);
            if (Potentials.Legacy.Is(WeaponLegacyPathId.GakgungSunPiercer) &&
                Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced &&
                firstImpacts.Add(damage.AttackInstanceId))
            {
                runtime.AffixStatuses.ApplyTimedStatus(damage.TargetRuntimeId, CombatStatusKind.ArmorBreak,
                    2.5f, 1, WeaponId.GakgungShot);
            }
            if (Potentials.Legacy.Is(WeaponLegacyPathId.GakgungSunPiercer) &&
                Potentials.Legacy.Stage == WeaponLegacyStage.Completed && arrow.ImpactCount >= arrow.MaxImpacts)
                ResolveSunPiercerBlast(damage, arrow);
            if (Potentials.HasPotential(WeaponPotentialId.GakgungArmorBreakArrowhead) && firstImpacts.Add(damage.AttackInstanceId) &&
                WeaponPotentialVisuals.TryGet(WeaponPotentialId.GakgungArmorBreakArrowhead, out _, out var armorMask) && runtime.Targets.TryGet(damage.TargetRuntimeId, out var armorTarget) && armorTarget != null && armorTarget.HurtMask != null &&
                PixelMaskContactService.TryFindContact(armorMask, PixelMaskTransform.Translation(damage.ContactPoint.X, damage.ContactPoint.Y), armorTarget.HurtMask, armorTarget.HurtMaskTransform, out _))
            {
                if (runtime.AffixStatuses.ApplyVulnerability(damage.TargetRuntimeId, damage.ContactPoint, 2f, true))
                {
#if UNITY_INCLUDE_TESTS
                    LastArmorBreakPrimaryAttackIdForTests = damage.AttackInstanceId;
                    armorBreakApplicationAttackIdsForTests.Add(damage.AttackInstanceId);
#endif
                }
            }
            if (Potentials.HasPotential(WeaponPotentialId.GakgungSplitFletching) && firstImpacts.Add(-damage.AttackInstanceId) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.GakgungSplitFletching, out _, out var splitMask))
            {
                var vector = new Float2(damage.ContactPoint.X - arrow.Start.X, damage.ContactPoint.Y - arrow.Start.Y);
                var length = Mathf.Max(.001f, Mathf.Sqrt(vector.X * vector.X + vector.Y * vector.Y));
                var direction = new Float2(vector.X / length, vector.Y / length);
                var left = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
                var right = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
                splitArrows.Add(new SplitArrow(left, damage.ContactPoint, Rotate(direction, -25f), splitMask, arrow.Range * .65f));
                splitArrows.Add(new SplitArrow(right, damage.ContactPoint, Rotate(direction, 25f), splitMask, arrow.Range * .65f));
#if UNITY_INCLUDE_TESTS
                splitChildAttackIdsForTests.Add(left.InstanceId); splitChildAttackIdsForTests.Add(right.InstanceId);
#endif
            }
        }

        private static Float2 Direction(Float2 origin, Float2 target)
        {
            var x = target.X - origin.X; var y = target.Y - origin.Y; var length = Mathf.Sqrt(x * x + y * y);
            return length > 0.001f ? new Float2(x / length, y / length) : new Float2(1f, 0f);
        }
        private void EnsureTransientVisuals(Transform root)
        {
            if (root == null || root == transientVisualRoot) return;
            transientVisuals?.Dispose();
            transientVisualRoot = root;
            transientVisuals = new WeaponTransientVisualPool(root);
        }
        private static Float2 Rotate(Float2 value, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad; var cosine = Mathf.Cos(radians); var sine = Mathf.Sin(radians);
            return new Float2(value.X * cosine - value.Y * sine, value.X * sine + value.Y * cosine);
        }
        private void ResolveSunPiercerBlast(ConfirmedDamageEvent damage, ArrowInfo arrow)
        {
            var attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f);
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;
                var delta = new Float2(target.WorldPosition.X - damage.ContactPoint.X,
                    target.WorldPosition.Y - damage.ContactPoint.Y);
                if (delta.X * delta.X + delta.Y * delta.Y > 1f) continue;
                runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.GakgungShot,
                    target, Mathf.CeilToInt(BaseDamage * 1.8f), false, target.WorldPosition,
                    ContactPhase.Blast, damage.SimulationTick, true,
                    WeaponHitTrait.Explosion | WeaponHitTrait.Pierce, arrow.Start), out _);
            }
            runtime.DamageService.RetireAttack(attack.InstanceId);
        }

        private sealed class ArrowInfo
        {
            public ArrowInfo(Float2 start, float range, int maxImpacts)
            { Start = start; Range = range; MaxImpacts = maxImpacts; }
            public Float2 Start { get; }
            public float Range { get; }
            public int MaxImpacts { get; }
            public int ImpactCount { get; set; }
        }
        private struct SplitArrow { public SplitArrow(AttackInstance attack, Float2 position, Float2 direction, PixelHitMask mask, float range) { Attack = attack; Position = position; Direction = direction; Mask = mask; RemainingRange = range; Delay = .05f; } public AttackInstance Attack; public Float2 Position; public Float2 Direction; public PixelHitMask Mask; public float RemainingRange; public float Delay; }
    }
}
