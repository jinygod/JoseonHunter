using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    /// <summary>Shared straight-line projectile simulation; weapon executors retain all targeting and volley policy.</summary>
    public sealed class LinearProjectileExecutor
    {
        public const int MaxActiveProjectiles = 32;
        public const int MaxPooledProjectiles = 32;
        public const int MaxImpactsPerProjectile = 3;
        public const int MaxExtendedImpactsPerProjectile = 8;
        private const int MaxSweepSamples = 64;
        private readonly WeaponRuntimeController runtime;
        private readonly List<Projectile> active = new List<Projectile>();
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly Stack<GameObject> pool = new Stack<GameObject>();
        private readonly Dictionary<Sprite, PixelHitMask> masksBySprite = new Dictionary<Sprite, PixelHitMask>();
        public event Action<LinearProjectileTravel> ProjectileTravelled;

        public LinearProjectileExecutor(WeaponRuntimeController runtime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public int ActiveCount => active.Count;
        public int PooledCount => pool.Count;
        public int ReturnedToPoolCount { get; private set; }
        /// <summary>Latest active visual scale, retained for deterministic combat telemetry.</summary>
        public float LastVisualScale { get; private set; } = 1f;
#if UNITY_INCLUDE_TESTS
        public bool HasLastImpactContactForTests { get; private set; }
        public Float2 LastImpactContactForTests { get; private set; }
#endif

        public bool Launch(in WeaponExecutionContext context, in LinearProjectileSpec spec)
        {
            if (active.Count >= MaxActiveProjectiles) return false;
            var visual = Acquire(context, spec.WeaponId, spec.VisualName, spec.Scale);
            visual.transform.position = new Vector3(spec.Position.X, spec.Position.Y, 0f);
            var renderer = visual.GetComponent<SpriteRenderer>();
            var mask = context.MaskFor(spec.WeaponId) ?? ResolveMask(renderer);
            if (spec.VisualFrameCount > 1)
                renderer.sprite = context.PresentationSpriteFor(spec.WeaponId, spec.VisualPartStart);
            active.Add(new Projectile(spec, visual, mask));
            return true;
        }

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var projectile = active[index];
                projectile.PendingSimulationTime += Mathf.Max(0f, deltaTime);
                var previousPosition = projectile.Position;
                var renderer = projectile.Visual.GetComponent<SpriteRenderer>();
                var pixelWorldSize = 1f / projectile.Mask.PixelsPerUnit;
                var stepSize = Mathf.Max(0.01f, pixelWorldSize * 0.5f);
                var maxSimulationTime = stepSize * (MaxSweepSamples - 1) / projectile.Speed;
                var processedTime = Mathf.Min(projectile.PendingSimulationTime, projectile.RemainingLifetime, maxSimulationTime);
                projectile.Elapsed += processedTime;
                projectile.VisualAge += processedTime;
                var normalizedTime = Mathf.Clamp01(projectile.Elapsed / projectile.InitialLifetime);
                var travelProgress = Mathf.Lerp(
                    normalizedTime,
                    normalizedTime * normalizedTime,
                    projectile.Acceleration);
                var forwardDistance = projectile.AllowedRange * travelProgress;
                var lateralDistance = Mathf.Sin(normalizedTime * Mathf.PI) * projectile.ArcAmplitude;
                projectile.Position = new Float2(
                    projectile.Origin.X + projectile.Direction.X * forwardDistance -
                    projectile.Direction.Y * lateralDistance,
                    projectile.Origin.Y + projectile.Direction.Y * forwardDistance +
                    projectile.Direction.X * lateralDistance);
                projectile.Visual.transform.position = new Vector3(projectile.Position.X, projectile.Position.Y, 0f);
                var visualDelta = new Vector2(
                    projectile.Position.X - previousPosition.X,
                    projectile.Position.Y - previousPosition.Y);
                if (visualDelta.sqrMagnitude > 0.000001f)
                    projectile.Visual.transform.rotation =
                        Quaternion.Euler(0f, 0f, Mathf.Atan2(visualDelta.y, visualDelta.x) * Mathf.Rad2Deg);
                if (projectile.FullDraw)
                    projectile.Visual.transform.localScale = Vector3.one * Mathf.Clamp(
                        projectile.BaseScale * (1f + .35f * FullDrawProgress(projectile)),
                        projectile.BaseScale * .92f,
                        projectile.BaseScale * 1.35f);
                if (projectile.VisualFrameCount > 1)
                {
                    var frame = Mathf.FloorToInt(projectile.VisualAge / projectile.VisualFrameSeconds)
                        % projectile.VisualFrameCount;
                    renderer.sprite = context.PresentationSpriteFor(
                        projectile.WeaponId,
                        projectile.VisualPartStart + frame);
                }
                LastVisualScale = projectile.Visual.transform.localScale.x;
                if (processedTime > 0f)
                {
                    projectile.PendingSimulationTime -= processedTime;
                    projectile.RemainingLifetime -= processedTime;
                    ProjectileTravelled?.Invoke(new LinearProjectileTravel(projectile.Attack.InstanceId, projectile.WeaponId, previousPosition, projectile.Position));
                    SweepDamageContacts(projectile, previousPosition, context);
                }
                if (projectile.RemainingLifetime <= 0f || projectile.ImpactCount >= projectile.MaxImpacts)
                {
                    Release(projectile.Visual);
                    runtime.DamageService.RetireAttack(projectile.Attack.InstanceId);
                    active.RemoveAt(index);
                }
            }
        }

        public void Reset()
        {
            foreach (var projectile in active)
            {
                Release(projectile.Visual);
                runtime.DamageService.RetireAttack(projectile.Attack.InstanceId);
            }
            active.Clear();
            LastVisualScale = 1f;
#if UNITY_INCLUDE_TESTS
            HasLastImpactContactForTests = false;
            LastImpactContactForTests = default;
#endif
        }

        /// <summary>Terminal cleanup for a containing executor; pooled visuals must not survive runtime replacement.</summary>
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

        private void SweepDamageContacts(Projectile projectile, Float2 previousPosition, in WeaponExecutionContext context)
        {
            var renderer = projectile.Visual.GetComponent<SpriteRenderer>();
            runtime.Targets.CopyTo(targets);
            var deltaX = projectile.Position.X - previousPosition.X;
            var deltaY = projectile.Position.Y - previousPosition.Y;
            var distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
            var pixelWorldSize = 1f / projectile.Mask.PixelsPerUnit;
            var stepSize = Mathf.Max(0.01f, pixelWorldSize * 0.5f);
            var steps = Mathf.Clamp(Mathf.CeilToInt(distance / stepSize), 1, MaxSweepSamples - 1);
            for (var step = 0; step <= steps; step++)
            {
                var fraction = step / (float)steps;
                var sample = new Float2(previousPosition.X + deltaX * fraction, previousPosition.Y + deltaY * fraction);
                var attackTransform = TransformFor(renderer, sample);
                foreach (var target in targets)
                {
                    if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                    if (!PixelMaskContactService.TryFindContact(projectile.Mask, attackTransform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                    var damage = projectile.Damage;
                    var potentialContact = projectile.FullDraw && PotentialMaskOverlaps(projectile, target, contact);
                    if (potentialContact)
                    {
                        damage = Mathf.CeilToInt(damage * (1f + .6f * FullDrawProgress(projectile, sample)));
                    }
                    if (!runtime.DamageService.TryApply(
                            WeaponDamageRequest.Create(projectile.Attack, projectile.WeaponId, target, damage, false, contact, ContactPhase.Direct, context.SimulationTick),
                            out _)) continue;
#if UNITY_INCLUDE_TESTS
                    HasLastImpactContactForTests = true;
                    LastImpactContactForTests = contact;
#endif
                    projectile.ImpactCount++;
                    if (projectile.ImpactCount >= projectile.MaxImpacts) return;
                }
            }
        }
        private static float FullDrawProgress(Projectile projectile) => FullDrawProgress(projectile, projectile.Position);
        private static float FullDrawProgress(Projectile projectile, Float2 position)
        {
            var x = position.X - projectile.Origin.X; var y = position.Y - projectile.Origin.Y;
            return Mathf.Clamp01(Mathf.Sqrt(x * x + y * y) / Mathf.Max(.01f, projectile.AllowedRange * .8f));
        }
        private static bool PotentialMaskOverlaps(Projectile projectile, ICombatTarget target, Float2 contact) => projectile.PotentialMask != null &&
            PixelMaskContactService.TryFindContact(projectile.PotentialMask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _);

        private GameObject Acquire(in WeaponExecutionContext context, WeaponId weaponId, string visualName, float scale)
        {
            var visual = pool.Count > 0 ? pool.Pop() : new GameObject(visualName);
            visual.transform.SetParent(context.PresentationRoot, false);
            var renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = context.SpriteFor(weaponId);
            renderer.sortingOrder = context.SortingOrder;
            renderer.color = Color.white;
            visual.transform.localScale = Vector3.one * WeaponPresentationScale.For(
                weaponId,
                WeaponVisualStage.Projectile,
                scale,
                1,
                false);
            visual.SetActive(true);
            return visual;
        }

        private void Release(GameObject visual)
        {
            if (visual == null) return;
            visual.SetActive(false);
            if (pool.Count < MaxPooledProjectiles) pool.Push(visual);
            else UnityEngine.Object.Destroy(visual);
            ReturnedToPoolCount++;
        }

        private PixelHitMask ResolveMask(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return runtime.BladeMask;
            if (masksBySprite.TryGetValue(renderer.sprite, out var mask)) return mask;
            try { mask = PixelHitMask.FromSprite(renderer.sprite); }
            catch (UnityException) { mask = PixelHitMask.OpaqueSpriteRect(renderer.sprite); }
            masksBySprite.Add(renderer.sprite, mask);
            return mask;
        }

        private static PixelMaskTransform TransformFor(SpriteRenderer renderer, Float2 position)
        {
            var scale = renderer.transform.lossyScale;
            return new PixelMaskTransform(position, Mathf.RoundToInt(renderer.transform.eulerAngles.z), renderer.flipX,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
        }

        private sealed class Projectile
        {
            public Projectile(LinearProjectileSpec spec, GameObject visual, PixelHitMask mask)
            {
                Attack = spec.Attack; WeaponId = spec.WeaponId; Position = spec.Position; Direction = spec.Direction;
                Speed = spec.Speed; RemainingLifetime = spec.Lifetime; Damage = spec.Damage; MaxImpacts = spec.MaxImpacts;
                Visual = visual; Mask = mask; Origin = spec.Position; InitialLifetime = spec.Lifetime; AllowedRange = spec.AllowedRange; BaseScale = visual.transform.localScale.x; FullDraw = spec.FullDraw; PotentialMask = spec.PotentialMask;
                ArcAmplitude = spec.ArcAmplitude; Acceleration = spec.Acceleration;
                VisualPartStart = spec.VisualPartStart; VisualFrameCount = spec.VisualFrameCount;
                VisualFrameSeconds = spec.VisualFrameSeconds;
            }
            public AttackInstance Attack { get; }
            public WeaponId WeaponId { get; }
            public Float2 Position { get; set; }
            public Float2 Direction { get; }
            public float Speed { get; }
            public float RemainingLifetime { get; set; }
            public float PendingSimulationTime { get; set; }
            public float Elapsed { get; set; }
            public int Damage { get; }
            public int MaxImpacts { get; }
            public int ImpactCount { get; set; }
            public GameObject Visual { get; }
            public PixelHitMask Mask { get; }
            public Float2 Origin { get; }
            public float InitialLifetime { get; } public float AllowedRange { get; } public float BaseScale { get; } public bool FullDraw { get; } public PixelHitMask PotentialMask { get; }
            public float ArcAmplitude { get; }
            public float Acceleration { get; }
            public int VisualPartStart { get; }
            public int VisualFrameCount { get; }
            public float VisualFrameSeconds { get; }
            public float VisualAge { get; set; }
        }
    }

    public readonly struct LinearProjectileTravel
    {
        public LinearProjectileTravel(int attackInstanceId, WeaponId weaponId, Float2 previous, Float2 current) { AttackInstanceId = attackInstanceId; WeaponId = weaponId; Previous = previous; Current = current; }
        public int AttackInstanceId { get; } public WeaponId WeaponId { get; } public Float2 Previous { get; } public Float2 Current { get; }
    }

    public readonly struct LinearProjectileSpec
    {
        public LinearProjectileSpec(
            AttackInstance attack,
            WeaponId weaponId,
            Float2 position,
            Float2 direction,
            float speed,
            float lifetime,
            int damage,
            int maxImpacts,
            string visualName,
            float scale = 1f,
            bool allowExtendedImpacts = false,
            bool fullDraw = false,
            PixelHitMask potentialMask = null,
            float arcAmplitude = 0f,
            float acceleration = 0f,
            int visualPartStart = 0,
            int visualFrameCount = 1,
            float visualFrameSeconds = .05f)
        {
            Attack = attack ?? throw new ArgumentNullException(nameof(attack));
            WeaponId = weaponId; Position = position; Direction = direction; Speed = Mathf.Max(0.01f, speed);
            Lifetime = Mathf.Max(0.01f, lifetime); Damage = Mathf.Max(1, damage);
            MaxImpacts = Mathf.Clamp(maxImpacts, 1, allowExtendedImpacts ? LinearProjectileExecutor.MaxExtendedImpactsPerProjectile : LinearProjectileExecutor.MaxImpactsPerProjectile);
            VisualName = string.IsNullOrEmpty(visualName) ? "Linear Projectile" : visualName;
            Scale = Mathf.Max(0.01f, scale);
            FullDraw = fullDraw; PotentialMask = potentialMask; AllowedRange = Speed * Lifetime;
            ArcAmplitude = arcAmplitude;
            Acceleration = Mathf.Clamp01(acceleration);
            VisualPartStart = Mathf.Max(0, visualPartStart);
            VisualFrameCount = Mathf.Max(1, visualFrameCount);
            VisualFrameSeconds = Mathf.Max(.01f, visualFrameSeconds);
        }
        public AttackInstance Attack { get; }
        public WeaponId WeaponId { get; }
        public Float2 Position { get; }
        public Float2 Direction { get; }
        public float Speed { get; }
        public float Lifetime { get; }
        public int Damage { get; }
        public int MaxImpacts { get; }
        public string VisualName { get; }
        public float Scale { get; }
        public bool FullDraw { get; } public PixelHitMask PotentialMask { get; } public float AllowedRange { get; }
        public float ArcAmplitude { get; }
        public float Acceleration { get; }
        public int VisualPartStart { get; }
        public int VisualFrameCount { get; }
        public float VisualFrameSeconds { get; }
    }
}
