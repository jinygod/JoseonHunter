using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public sealed class FlyingBladeExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        private const float ArrivalDistance = 0.08f;
        private readonly WeaponRuntimeController runtime;
        private readonly List<Blade> active = new List<Blade>();
        private readonly List<MoonCast> moonCasts = new List<MoonCast>();
        private readonly List<Afterimage> afterimages = new List<Afterimage>();
        private readonly List<ICombatTarget> targetBuffer = new List<ICombatTarget>();
        private readonly Stack<GameObject> pool = new Stack<GameObject>();
        private readonly Dictionary<Sprite, PixelHitMask> masksBySprite = new Dictionary<Sprite, PixelHitMask>();
        private WeaponTransientVisualPool transientVisuals;
        private Transform transientVisualRoot;
        private float cooldown;

        public FlyingBladeExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int bladeCount, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
            : this(runtime, baseDamage, cooldownSeconds, range, speed, bladeCount, bladeCount, evolved, modifiers)
        {
        }

        public FlyingBladeExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int bladeCount, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            Reconfigure(modifiers.ScaleDamage(baseDamage), modifiers.ScaleCooldown(cooldownSeconds), modifiers.ScaleArea(range), modifiers.ScaleSpeed(speed), bladeCount);
            Level = Mathf.Max(1, level);
            Potentials = modifiers;
            IsEvolved = evolved;
        }

        public void Reconfigure(float baseDamage, float cooldownSeconds, float range, float speed, int bladeCount)
        {
            BaseDamage = Mathf.Max(0f, baseDamage);
            CooldownSeconds = Mathf.Max(0.01f, cooldownSeconds);
            Range = Mathf.Max(0.01f, range);
            Speed = Mathf.Max(0.01f, speed);
            BladeCount = Mathf.Max(1, bladeCount);
        }

        public float BaseDamage { get; private set; }
        public float CooldownSeconds { get; private set; }
        public float Range { get; private set; }
        public float Speed { get; private set; }
        public int BladeCount { get; private set; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public int ActiveBladeCount => active.Count;
        public int PooledBladeCount => pool.Count;
        public int LastVolleyLaunchCount { get; private set; }
        public int ReturnedToPoolCount { get; private set; }
#if UNITY_INCLUDE_TESTS
        public int PendingAfterimageCountForTests => afterimages.Count;
        public Float2 FirstActivePositionForTests => active.Count > 0 ? active[0].Position : default;
        public bool FirstActiveInboundForTests => active.Count > 0 && active[0].Inbound;
#endif
        public float MaximumDistanceFromLaunch { get; private set; }
        public int DelayedBladeCount
        {
            get
            {
                var count = 0;
                foreach (var blade in active) if (blade.Delay > 0f) count++;
                return count;
            }
        }

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            EnsureTransientVisuals(context.PresentationRoot);
            transientVisuals?.Tick(deltaTime);
            cooldown -= deltaTime;
            if (cooldown <= 0f && TryFindTarget(context.OwnerPosition, out var target))
            {
                cooldown = CooldownSeconds;
                LaunchVolley(context, target);
            }

            for (var index = active.Count - 1; index >= 0; index--)
            {
                var blade = active[index];
                Advance(blade, deltaTime, context);
                if (blade.Returned)
                {
                    Release(blade.Visual);
                    runtime.DamageService.RetireAttack(blade.Attack.InstanceId);
                    active.RemoveAt(index);
                }
            }

            for (var index = moonCasts.Count - 1; index >= 0; index--)
            {
                var cast = moonCasts[index];
                if (!cast.MoonBlastResolved && TryFindReturnCrossing(cast, out var crossing))
                {
                    cast.MoonBlastResolved = true;
                    ResolveMoonBlast(crossing, context);
                }
                if (cast.AllReturned) moonCasts.RemoveAt(index);
            }

            for (var index = afterimages.Count - 1; index >= 0; index--)
            {
                var shadow = afterimages[index]; shadow.Delay -= Mathf.Max(0f, deltaTime);
                if (shadow.Delay > 0f) continue;
                if (runtime.Targets.TryGet(shadow.TargetRuntimeId, out var resolvedTarget) && resolvedTarget != null && resolvedTarget.IsAlive && resolvedTarget.HurtMask != null &&
                    PixelMaskContactService.TryFindContact(shadow.Mask, PixelMaskTransform.Translation(shadow.Contact.X, shadow.Contact.Y), resolvedTarget.HurtMask, resolvedTarget.HurtMaskTransform, out var contact))
                    runtime.DamageService.TryApply(WeaponDamageRequest.Create(shadow.Attack, WeaponId.HwandoFlyingBlade, resolvedTarget, Mathf.CeilToInt(BaseDamage * .55f), false, contact, ContactPhase.PotentialChain, context.SimulationTick), out _);
                runtime.DamageService.RetireAttack(shadow.Attack.InstanceId); afterimages.RemoveAt(index);
            }

        }

        public void Reset()
        {
            foreach (var blade in active)
            {
                Release(blade.Visual);
                runtime.DamageService.RetireAttack(blade.Attack.InstanceId);
            }
            active.Clear();
            foreach (var afterimage in afterimages) runtime.DamageService.RetireAttack(afterimage.Attack.InstanceId);
            afterimages.Clear();
            moonCasts.Clear();
            cooldown = 0f;
            MaximumDistanceFromLaunch = 0f;
            transientVisuals?.Dispose();
            transientVisuals = null;
            transientVisualRoot = null;
        }

        public void Dispose()
        {
            Reset();
            while (pool.Count > 0)
            {
                var visual = pool.Pop();
                if (visual != null) UnityEngine.Object.Destroy(visual);
            }
            masksBySprite.Clear();
            transientVisuals?.Dispose();
            transientVisuals = null;
            transientVisualRoot = null;
        }

        private bool TryFindTarget(Float2 owner, out ICombatTarget target)
        {
            target = null;
            var bestDistanceSquared = Range * Range;
            runtime.Targets.CopyTo(targetBuffer);
            foreach (var candidate in targetBuffer)
            {
                if (candidate == null || !candidate.IsAlive) continue;
                var delta = Subtract(candidate.WorldPosition, owner);
                var distanceSquared = delta.X * delta.X + delta.Y * delta.Y;
                if (distanceSquared > bestDistanceSquared) continue;
                bestDistanceSquared = distanceSquared;
                target = candidate;
            }
            return target != null;
        }

        private void LaunchVolley(in WeaponExecutionContext context, ICombatTarget target)
        {
            var launch = context.OwnerPosition;
            var toTarget = Subtract(target.WorldPosition, launch);
            var targetDistance = Mathf.Sqrt(toTarget.X * toTarget.X + toTarget.Y * toTarget.Y);
            var direction = targetDistance > 0.001f ? new Float2(toTarget.X / targetDistance, toTarget.Y / targetDistance) : new Float2(1f, 0f);
            var count = IsEvolved ? 4 : BladeCount;
            var cast = IsEvolved ? new MoonCast() : null;
            var castHits = new HashSet<int>();
            LastVolleyLaunchCount = count;
            for (var index = 0; index < count; index++)
            {
                var radialDirection = IsEvolved ? Rotate(direction, index * 90f) : direction;
                var distance = IsEvolved ? Range : Mathf.Min(Range, targetDistance);
                var endpoint = new Float2(launch.X + radialDirection.X * distance, launch.Y + radialDirection.Y * distance);
                var stagger = IsEvolved ? 0f : index * 0.1f;
                var visual = Acquire(context);
                visual.transform.position = new Vector3(launch.X, launch.Y, 0f);
                var arcSign = count > 1 && (index & 1) != 0 ? -1f : 1f;
                var blade = new Blade(
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f),
                    launch, endpoint, stagger, visual, context.MaskFor(WeaponId.HwandoFlyingBlade) ?? ResolveMask(visual.GetComponent<SpriteRenderer>()), Range, IsEvolved, castHits, arcSign);
                active.Add(blade);
                cast?.Blades.Add(blade);
            }
            if (cast != null) moonCasts.Add(cast);
        }

        private void Advance(Blade blade, float deltaTime, in WeaponExecutionContext context)
        {
            if (blade.Delay > 0f)
            {
                blade.Delay -= deltaTime;
                return;
            }

            var previousPosition = blade.Position;
            blade.VisualAge += Mathf.Max(0f, deltaTime);
            var bladeRenderer = blade.Visual.GetComponent<SpriteRenderer>();
            var frame = Mathf.FloorToInt(blade.VisualAge / .05f)
                % WeaponVisualPartIndex.Hwando.ProjectileFrameCount;
            bladeRenderer.sprite = context.PresentationSpriteFor(
                WeaponId.HwandoFlyingBlade,
                WeaponVisualPartIndex.Hwando.Projectile + frame);
            var trailRenderer = blade.Visual.transform.Find("Blade Afterimage")?.GetComponent<SpriteRenderer>();
            if (trailRenderer != null)
                trailRenderer.sprite = context.PresentationSpriteFor(
                    WeaponId.HwandoFlyingBlade,
                    WeaponVisualPartIndex.Hwando.Trail +
                    frame % WeaponVisualPartIndex.Hwando.TrailFrameCount);
            var contactPhase = blade.Inbound ? ContactPhase.Inbound : ContactPhase.Outbound;
            if (!blade.Inbound)
            {
                blade.OutboundProgress += deltaTime * Speed / Mathf.Max(0.01f, blade.Distance);
                var progress = Mathf.Clamp01(blade.OutboundProgress);
                var easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                blade.Position = CurvedPosition(
                    blade.Start,
                    blade.End,
                    progress,
                    easedProgress,
                    blade.ArcSign,
                    blade.Range);
                if (progress >= 1f)
                {
                    blade.Inbound = true;
                    blade.ReturnStart = blade.Position;
                    blade.ReturnDistance = Mathf.Max(
                        ArrivalDistance,
                        Mathf.Sqrt(
                            (blade.Start.X - blade.ReturnStart.X) * (blade.Start.X - blade.ReturnStart.X) +
                            (blade.Start.Y - blade.ReturnStart.Y) * (blade.Start.Y - blade.ReturnStart.Y)));
                }
            }
            else
            {
                blade.ReturnProgress += deltaTime * Speed / blade.ReturnDistance;
                var progress = Mathf.Clamp01(blade.ReturnProgress);
                if (progress >= 1f)
                {
                    blade.Returned = true;
                    return;
                }
                blade.ReturnSegmentStart = blade.Position;
                var inboundSign = ResolveInboundArcSign(blade);
                blade.Position = CurvedPosition(
                    blade.ReturnStart,
                    blade.Start,
                    progress,
                    Mathf.SmoothStep(0f, 1f, progress),
                    inboundSign,
                    blade.Range);
                blade.HasReturnSegment = true;
            }

            blade.Visual.transform.position = new Vector3(blade.Position.X, blade.Position.Y, 0f);
            var visualDirection = new Vector2(
                blade.Position.X - previousPosition.X,
                blade.Position.Y - previousPosition.Y);
            if (visualDirection.sqrMagnitude > 0.000001f)
                blade.Visual.transform.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(visualDirection.y, visualDirection.x) * Mathf.Rad2Deg);
            var launchDelta = Subtract(blade.Position, blade.Start);
            MaximumDistanceFromLaunch = Mathf.Max(MaximumDistanceFromLaunch, Mathf.Sqrt(launchDelta.X * launchDelta.X + launchDelta.Y * launchDelta.Y));
            TryDamageContacts(blade, context, blade.IsMoonBlade && !blade.Inbound ? ContactPhase.Direct : contactPhase);
        }

        private bool TryFindReturnCrossing(MoonCast cast, out Float2 crossing)
        {
            crossing = default;
            for (var first = 0; first < cast.Blades.Count; first++)
            {
                var left = cast.Blades[first];
                if (!left.HasReturnSegment) continue;
                for (var second = first + 1; second < cast.Blades.Count; second++)
                {
                    var right = cast.Blades[second];
                    if (!right.HasReturnSegment) continue;
                    if (TryFindSegmentCrossing(left.ReturnSegmentStart, left.Position, right.ReturnSegmentStart, right.Position, out crossing)) return true;
                }
            }
            return false;
        }

        private void ResolveMoonBlast(Float2 crossing, in WeaponExecutionContext context)
        {
            var blast = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
            var blastTransform = PixelMaskTransform.Translation(crossing.X, crossing.Y);
            runtime.Targets.CopyTo(targetBuffer);
            foreach (var target in targetBuffer)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                if (!PixelMaskContactService.TryFindContact(runtime.BladeMask, blastTransform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                runtime.DamageService.TryApply(
                    WeaponDamageRequest.Create(blast, WeaponId.HwandoFlyingBlade, target, Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.Blast, context.SimulationTick),
                    out _);
            }
            runtime.DamageService.RetireAttack(blast.InstanceId);
        }

        private void TryDamageContacts(Blade blade, in WeaponExecutionContext context, ContactPhase phase)
        {
            var attackTransform = TransformFor(blade.Visual.GetComponent<SpriteRenderer>(), blade.Position);
            runtime.Targets.CopyTo(targetBuffer);
            foreach (var target in targetBuffer)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                if (!PixelMaskContactService.TryFindContact(blade.Mask, attackTransform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                var danceContact = Potentials.HasPotential(WeaponPotentialId.HwandoFlyingBladeDance) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.HwandoFlyingBladeDance, out _, out var danceMask) &&
                    PixelMaskContactService.TryFindContact(danceMask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _);
                var ramp = danceContact ? 1f + Mathf.Min(.60f, blade.CastHits.Count * .15f) : 1f;
                if (!runtime.DamageService.TryApply(
                        WeaponDamageRequest.Create(blade.Attack, WeaponId.HwandoFlyingBlade, target, Mathf.CeilToInt(BaseDamage * ramp), false, contact, phase, context.SimulationTick),
                        out _)) continue;
                SpawnImpact(context, contact);
                if (danceContact) blade.CastHits.Add(target.RuntimeId);
                if (Potentials.HasPotential(WeaponPotentialId.HwandoVenomFang) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.HwandoVenomFang, out _, out var venomMask) &&
                    PixelMaskContactService.TryFindContact(venomMask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _))
                {
                    var poison = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, .5f);
                    runtime.AffixStatuses.ApplyOrRefreshPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId, contact,
                        Mathf.CeilToInt(BaseDamage * .20f), 3, poison, true, ContactPhase.Poison));
                }
                if (blade.Inbound && Potentials.HasPotential(WeaponPotentialId.HwandoReturningAfterimage) &&
                    WeaponPotentialVisuals.TryGet(WeaponPotentialId.HwandoReturningAfterimage, out _, out var shadowMask))
                    afterimages.Add(new Afterimage(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), target.RuntimeId, contact, shadowMask));
            }
        }

        private GameObject Acquire(in WeaponExecutionContext context)
        {
            var visual = pool.Count > 0 ? pool.Pop() : new GameObject("Hwando Flying Blade");
            visual.transform.SetParent(context.PresentationRoot, false);
            var renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = context.PresentationSpriteFor(
                WeaponId.HwandoFlyingBlade,
                WeaponVisualPartIndex.Hwando.Projectile);
            renderer.color = Color.white;
            renderer.sortingOrder = context.SortingOrder;
            visual.transform.localScale = Vector3.one;
            var trail = visual.transform.Find("Blade Afterimage");
            if (trail == null)
            {
                trail = new GameObject("Blade Afterimage").transform;
                trail.SetParent(visual.transform, false);
            }
            var trailRenderer = trail.GetComponent<SpriteRenderer>();
            if (trailRenderer == null) trailRenderer = trail.gameObject.AddComponent<SpriteRenderer>();
            trailRenderer.sprite = context.PresentationSpriteFor(
                WeaponId.HwandoFlyingBlade,
                WeaponVisualPartIndex.Hwando.Trail);
            trailRenderer.color = new Color(1f, 1f, 1f, 0.58f);
            trailRenderer.sortingOrder = context.SortingOrder - 1;
            trail.localPosition = new Vector3(-0.13f, 0f, 0f);
            trail.localRotation = Quaternion.identity;
            trail.localScale = Vector3.one;
            visual.SetActive(true);
            return visual;
        }

        private void SpawnImpact(in WeaponExecutionContext context, Float2 contact)
        {
            EnsureTransientVisuals(context.PresentationRoot);
            var sprite = context.PresentationSpriteFor(
                WeaponId.HwandoFlyingBlade,
                WeaponVisualPartIndex.Hwando.Impact);
            var cue = new WeaponVisualCue(
                WeaponId.HwandoFlyingBlade,
                WeaponVisualStage.Impact,
                Level,
                IsEvolved,
                .34f,
                .12f);
            transientVisuals?.Play(
                sprite,
                new Vector3(contact.X, contact.Y, 0f),
                Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0, 4) * 90f),
                Vector3.one * cue.ResolvedScale,
                new Color(1f, 1f, 1f, .82f),
                cue.ResolvedLifetime,
                context.SortingOrder + 2);
        }

        private void EnsureTransientVisuals(Transform root)
        {
            if (root == null || root == transientVisualRoot) return;
            transientVisuals?.Dispose();
            transientVisualRoot = root;
            transientVisuals = new WeaponTransientVisualPool(root);
        }

        private void Release(GameObject visual)
        {
            if (visual == null) return;
            visual.SetActive(false);
            pool.Push(visual);
            ReturnedToPoolCount++;
        }

        private static Float2 ClampToRange(Float2 origin, Float2 candidate, float range)
        {
            var delta = Subtract(candidate, origin);
            var distance = Mathf.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            return distance <= range ? candidate : new Float2(origin.X + delta.X / distance * range, origin.Y + delta.Y / distance * range);
        }

        private static Float2 Subtract(Float2 left, Float2 right) => new Float2(left.X - right.X, left.Y - right.Y);

        private Float2 CurvedPosition(
            Float2 start,
            Float2 end,
            float progress,
            float easedProgress,
            float arcSign,
            float range)
        {
            var direction = Subtract(end, start);
            var length = Mathf.Max(
                .01f,
                Mathf.Sqrt(direction.X * direction.X + direction.Y * direction.Y));
            direction = new Float2(direction.X / length, direction.Y / length);
            var arc = Mathf.Sin(progress * Mathf.PI) *
                (.10f + .025f * Mathf.Min(4, BladeCount));
            var perpendicular = new Float2(-direction.Y, direction.X);
            var straight = new Float2(
                Mathf.Lerp(start.X, end.X, easedProgress),
                Mathf.Lerp(start.Y, end.Y, easedProgress));
            return ClampToRange(
                start,
                new Float2(
                    straight.X + perpendicular.X * arc * arcSign,
                    straight.Y + perpendicular.Y * arc * arcSign),
                range);
        }

        private float ResolveInboundArcSign(Blade blade) =>
            blade.IsMoonBlade
                ? -blade.ArcSign
                : blade.ArcSign;

        private static Float2 Rotate(Float2 value, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            return new Float2(value.X * cosine - value.Y * sine, value.X * sine + value.Y * cosine);
        }

        private static bool TryFindSegmentCrossing(Float2 a, Float2 b, Float2 c, Float2 d, out Float2 crossing)
        {
            crossing = default;
            var abX = b.X - a.X; var abY = b.Y - a.Y;
            var cdX = d.X - c.X; var cdY = d.Y - c.Y;
            var denominator = abX * cdY - abY * cdX;
            if (Mathf.Abs(denominator) < 0.0001f) return false;
            var acX = c.X - a.X; var acY = c.Y - a.Y;
            var first = (acX * cdY - acY * cdX) / denominator;
            var second = (acX * abY - acY * abX) / denominator;
            if (first < 0f || first > 1f || second < 0f || second > 1f) return false;
            crossing = new Float2(a.X + abX * first, a.Y + abY * first);
            return true;
        }

        private PixelHitMask ResolveMask(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return runtime.BladeMask;
            if (masksBySprite.TryGetValue(renderer.sprite, out var mask)) return mask;
            try
            {
                mask = PixelHitMask.FromSprite(renderer.sprite);
            }
            catch (UnityException)
            {
                // Imported prototype sprites are not guaranteed readable; retain their exact world rect as an opaque fallback.
                mask = PixelHitMask.OpaqueSpriteRect(renderer.sprite);
            }
            masksBySprite.Add(renderer.sprite, mask);
            return mask;
        }

        private static PixelMaskTransform TransformFor(SpriteRenderer renderer, Float2 position)
        {
            var transform = renderer.transform;
            var scale = transform.lossyScale;
            return new PixelMaskTransform(
                position,
                Mathf.RoundToInt(transform.eulerAngles.z),
                renderer.flipX,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
        }

        private sealed class Blade
        {
            public Blade(AttackInstance attack, Float2 start, Float2 end, float delay, GameObject visual, PixelHitMask mask, float range, bool isMoonBlade, HashSet<int> castHits, float arcSign)
            {
                Attack = attack; Start = start; End = end; Delay = delay; Visual = visual; Mask = mask; Range = range;
                Position = start;
                IsMoonBlade = isMoonBlade;
                CastHits = castHits;
                ArcSign = arcSign;
                Distance = Mathf.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
            }
            public AttackInstance Attack { get; }
            public Float2 Start { get; }
            public Float2 End { get; }
            public float Delay { get; set; }
            public GameObject Visual { get; }
            public PixelHitMask Mask { get; }
            public float Range { get; }
            public float Distance { get; }
            public Float2 Position { get; set; }
            public float OutboundProgress { get; set; }
            public bool Inbound { get; set; }
            public bool Returned { get; set; }
            public bool IsMoonBlade { get; }
            public Float2 ReturnSegmentStart { get; set; }
            public bool HasReturnSegment { get; set; }
            public Float2 ReturnStart { get; set; }
            public float ReturnDistance { get; set; }
            public float ReturnProgress { get; set; }
            public HashSet<int> CastHits { get; }
            public float ArcSign { get; }
            public float VisualAge { get; set; }
        }

        private sealed class Afterimage
        {
            public Afterimage(AttackInstance attack, int targetRuntimeId, Float2 contact, PixelHitMask mask) { Attack = attack; TargetRuntimeId = targetRuntimeId; Contact = contact; Mask = mask; Delay = .12f; }
            public AttackInstance Attack { get; } public int TargetRuntimeId { get; } public Float2 Contact { get; } public PixelHitMask Mask { get; } public float Delay { get; set; }
        }

        private sealed class MoonCast
        {
            public List<Blade> Blades { get; } = new List<Blade>();
            public bool MoonBlastResolved { get; set; }
            public bool AllReturned
            {
                get
                {
                    foreach (var blade in Blades) if (!blade.Returned) return false;
                    return true;
                }
            }
        }
    }
}
