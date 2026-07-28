using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
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
        private readonly List<TransientEffect> transientEffects = new List<TransientEffect>();
        private Transform presentationRoot;
        private Sprite impactSprite;
        private int effectSortingOrder;
#if UNITY_INCLUDE_TESTS
        private readonly List<int> splitChildAttackIdsForTests = new List<int>();
        private readonly List<int> levelFiveSideArrowAttackIdsForTests = new List<int>();
        private readonly List<int> armorBreakApplicationAttackIdsForTests = new List<int>();
#endif

        public GakgungExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            projectiles = new LinearProjectileExecutor(runtime);
            BaseDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage)); CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds));
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
            presentationRoot = context.PresentationRoot;
            impactSprite = context.PresentationSpriteFor(WeaponId.GakgungShot, 2);
            effectSortingOrder = context.SortingOrder + 2;
            cooldown -= deltaTime;
            if (cooldown <= 0f && TrySelectTarget(out var target))
            {
                cooldown = CooldownSeconds;
                Launch(context, target);
            }
            projectiles.Tick(deltaTime, context);
            LastProjectileScale = projectiles.LastVisualScale;
            for (var index = transientEffects.Count - 1; index >= 0; index--)
            {
                var effect = transientEffects[index];
                effect.Remaining -= Mathf.Max(0f, deltaTime);
                if (effect.Remaining > 0f) continue;
                if (effect.Visual != null) UnityEngine.Object.Destroy(effect.Visual);
                transientEffects.RemoveAt(index);
            }
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
                    hit = runtime.DamageService.TryApply(WeaponDamageRequest.Create(child.Attack, WeaponId.GakgungShot, splitTarget, Mathf.CeilToInt(BaseDamage * .45f), false, contact, ContactPhase.PotentialChain, context.SimulationTick), out _);
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
            foreach (var effect in transientEffects)
                if (effect.Visual != null) UnityEngine.Object.Destroy(effect.Visual);
            transientEffects.Clear();
#if UNITY_INCLUDE_TESTS
            LastArmorBreakPrimaryAttackIdForTests = 0; splitChildAttackIdsForTests.Clear();
            levelFiveSideArrowAttackIdsForTests.Clear(); armorBreakApplicationAttackIdsForTests.Clear();
#endif
        }

        public void Dispose() { runtime.DamageService.DamageConfirmed -= OnDamageConfirmed; Reset(); projectiles.Dispose(); }

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
            SpawnEffect(
                "Gakgung Aim Glint",
                context.PresentationSpriteFor(WeaponId.GakgungShot, 1),
                target.WorldPosition,
                context.PresentationRoot,
                context.SortingOrder + 1,
                0.10f);
            var direction = Direction(context.OwnerPosition, target.WorldPosition);
            var targetDelta = new Float2(target.WorldPosition.X - context.OwnerPosition.X, target.WorldPosition.Y - context.OwnerPosition.Y);
            var targetDistance = Mathf.Sqrt(targetDelta.X * targetDelta.X + targetDelta.Y * targetDelta.Y);
            shotSequence++;
            var sunPiercer = IsEvolved && shotSequence % 4 == 0;
            var impacts = sunPiercer ? 8 : (Level == 5 ? 3 : 1);
            var damage = Mathf.CeilToInt(BaseDamage * (sunPiercer ? 3f : 1f));
            var scale = sunPiercer ? 1.25f : 1f;
            var speed = sunPiercer ? Speed * 0.7f : Speed;
            LastSelectedTargetRuntimeId = target.RuntimeId;
            LastLaunchCount = Level == 5 ? 3 : 1;
            LastProjectileMaximumImpacts = impacts;
            LastProjectileScale = scale;
            LaunchArrow(context, direction, 0f, impacts, damage, speed, scale, sunPiercer, true, targetDistance, target);
            if (Level != 5) return;
            LaunchArrow(context, direction, -8f, 1, Mathf.CeilToInt(BaseDamage), Speed, 1f, false, false, targetDistance, target);
            LaunchArrow(context, direction, 8f, 1, Mathf.CeilToInt(BaseDamage), Speed, 1f, false, false, targetDistance, target);
        }

        private void LaunchArrow(in WeaponExecutionContext context, Float2 direction, float degrees, int impacts, int damage, float speed, float scale, bool allowExtendedImpacts, bool primary, float targetDistance, ICombatTarget target)
        {
            var shotDirection = Rotate(direction, degrees);
            PixelHitMask drawMask = null;
            var fullDraw = primary && Potentials.HasPotential(WeaponPotentialId.GakgungFullDraw) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.GakgungFullDraw, out _, out drawMask);
            var attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
#if UNITY_INCLUDE_TESTS
            if (!primary) levelFiveSideArrowAttackIdsForTests.Add(attack.InstanceId);
#endif
            if (primary) primaryArrows[attack.InstanceId] = new ArrowInfo(context.OwnerPosition, Range);
            projectiles.Launch(context, new LinearProjectileSpec(
                attack, WeaponId.GakgungShot,
                context.OwnerPosition, shotDirection, speed, Range / speed, damage, impacts, "Gakgung Arrow", scale,
                allowExtendedImpacts, fullDraw, fullDraw ? drawMask : null,
                degrees == 0f ? 0.18f : Mathf.Sign(degrees) * 0.11f,
                0.28f));
        }

        private void OnDamageConfirmed(ConfirmedDamageEvent damage)
        {
            if (!damage.WeaponId.Equals(WeaponId.GakgungShot) || !primaryArrows.TryGetValue(damage.AttackInstanceId, out var arrow)) return;
            SpawnEffect(
                "Gakgung Impact Splinter",
                impactSprite,
                damage.ContactPoint,
                presentationRoot,
                effectSortingOrder,
                0.14f);
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
        private void SpawnEffect(
            string objectName,
            Sprite sprite,
            Float2 position,
            Transform root,
            int sortingOrder,
            float lifetime)
        {
            if (sprite == null || root == null) return;
            var visual = new GameObject(objectName);
            visual.transform.SetParent(root, false);
            visual.transform.position = new Vector3(position.X, position.Y, 0f);
            visual.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0, 4) * 90f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            transientEffects.Add(new TransientEffect(visual, lifetime));
        }
        private static Float2 Rotate(Float2 value, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad; var cosine = Mathf.Cos(radians); var sine = Mathf.Sin(radians);
            return new Float2(value.X * cosine - value.Y * sine, value.X * sine + value.Y * cosine);
        }
        private readonly struct ArrowInfo { public ArrowInfo(Float2 start, float range) { Start = start; Range = range; } public Float2 Start { get; } public float Range { get; } }
        private struct SplitArrow { public SplitArrow(AttackInstance attack, Float2 position, Float2 direction, PixelHitMask mask, float range) { Attack = attack; Position = position; Direction = direction; Mask = mask; RemainingRange = range; Delay = .05f; } public AttackInstance Attack; public Float2 Position; public Float2 Direction; public PixelHitMask Mask; public float RemainingRange; public float Delay; }
        private sealed class TransientEffect
        {
            public TransientEffect(GameObject visual, float remaining) { Visual = visual; Remaining = remaining; }
            public GameObject Visual { get; }
            public float Remaining { get; set; }
        }
    }
}
