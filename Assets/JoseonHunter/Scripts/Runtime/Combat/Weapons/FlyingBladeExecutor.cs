using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public sealed class FlyingBladeExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        private const float ArrivalDistance = 0.08f;
        private readonly WeaponRuntimeController runtime;
        private readonly List<Blade> active = new List<Blade>();
        private readonly List<MoonCast> moonCasts = new List<MoonCast>();
        private readonly List<ICombatTarget> targetBuffer = new List<ICombatTarget>();
        private readonly Stack<GameObject> pool = new Stack<GameObject>();
        private readonly Dictionary<Sprite, PixelHitMask> masksBySprite = new Dictionary<Sprite, PixelHitMask>();
        private float cooldown;

        public FlyingBladeExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int bladeCount, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            Reconfigure(modifiers.ScaleDamage(baseDamage), modifiers.ScaleCooldown(cooldownSeconds), modifiers.ScaleArea(range), modifiers.ScaleSpeed(speed), bladeCount);
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
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public int ActiveBladeCount => active.Count;
        public int PooledBladeCount => pool.Count;
        public int LastVolleyLaunchCount { get; private set; }
        public int ReturnedToPoolCount { get; private set; }
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
        }

        public void Reset()
        {
            foreach (var blade in active)
            {
                Release(blade.Visual);
                runtime.DamageService.RetireAttack(blade.Attack.InstanceId);
            }
            active.Clear();
            moonCasts.Clear();
            cooldown = 0f;
            MaximumDistanceFromLaunch = 0f;
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
            LastVolleyLaunchCount = count;
            for (var index = 0; index < count; index++)
            {
                var radialDirection = IsEvolved ? Rotate(direction, index * 90f) : direction;
                var distance = IsEvolved ? Range : Mathf.Min(Range, targetDistance);
                var endpoint = new Float2(launch.X + radialDirection.X * distance, launch.Y + radialDirection.Y * distance);
                var stagger = IsEvolved ? 0f : index * 0.1f;
                var visual = Acquire(context);
                visual.transform.position = new Vector3(launch.X, launch.Y, 0f);
                var blade = new Blade(
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f),
                    launch, endpoint, stagger, visual, context.MaskFor(WeaponId.HwandoFlyingBlade) ?? ResolveMask(visual.GetComponent<SpriteRenderer>()), Range, IsEvolved);
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

            var contactPhase = blade.Inbound ? ContactPhase.Inbound : ContactPhase.Outbound;
            if (!blade.Inbound)
            {
                blade.OutboundProgress += deltaTime * Speed / Mathf.Max(0.01f, blade.Distance);
                var t = Mathf.Clamp01(blade.OutboundProgress);
                var arc = Mathf.Sin(t * Mathf.PI) * Mathf.Min(0.35f, blade.Distance * 0.12f);
                var direction = Subtract(blade.End, blade.Start);
                var length = Mathf.Max(0.01f, blade.Distance);
                var position = new Float2(
                    Mathf.Lerp(blade.Start.X, blade.End.X, t) - direction.Y / length * arc,
                    Mathf.Lerp(blade.Start.Y, blade.End.Y, t) + direction.X / length * arc);
                blade.Position = ClampToRange(blade.Start, position, blade.Range);
                if (t >= 1f) blade.Inbound = true;
            }
            else
            {
                // Returning to the launch point keeps a moving player from stretching the path beyond its configured range.
                var owner = blade.Start;
                var delta = Subtract(owner, blade.Position);
                var distance = Mathf.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                if (distance <= ArrivalDistance)
                {
                    blade.Returned = true;
                    return;
                }
                var step = Mathf.Min(distance, Speed * deltaTime);
                blade.ReturnSegmentStart = blade.Position;
                blade.Position = new Float2(blade.Position.X + delta.X / distance * step, blade.Position.Y + delta.Y / distance * step);
                blade.HasReturnSegment = true;
            }

            blade.Visual.transform.position = new Vector3(blade.Position.X, blade.Position.Y, 0f);
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
                runtime.DamageService.TryApply(
                    WeaponDamageRequest.Create(blade.Attack, WeaponId.HwandoFlyingBlade, target, Mathf.CeilToInt(BaseDamage), false, contact, phase, context.SimulationTick),
                    out _);
            }
        }

        private GameObject Acquire(in WeaponExecutionContext context)
        {
            var visual = pool.Count > 0 ? pool.Pop() : new GameObject("Hwando Flying Blade");
            visual.transform.SetParent(context.PresentationRoot, false);
            var renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = context.SpriteFor(WeaponId.HwandoFlyingBlade);
            renderer.color = new Color(1f, 0.9f, 0.35f, 1f);
            renderer.sortingOrder = context.SortingOrder;
            visual.transform.localScale = Vector3.one;
            visual.SetActive(true);
            return visual;
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
            public Blade(AttackInstance attack, Float2 start, Float2 end, float delay, GameObject visual, PixelHitMask mask, float range, bool isMoonBlade)
            {
                Attack = attack; Start = start; End = end; Delay = delay; Visual = visual; Mask = mask; Range = range;
                Position = start;
                IsMoonBlade = isMoonBlade;
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
