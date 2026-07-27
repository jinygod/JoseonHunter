using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    /// <summary>Shared straight-line projectile simulation; weapon executors retain all targeting and volley policy.</summary>
    public sealed class LinearProjectileExecutor
    {
        public const int MaxActiveProjectiles = 32;
        public const int MaxPooledProjectiles = 32;
        public const int MaxImpactsPerProjectile = 3;
        private const int MaxSweepSamples = 64;
        private readonly WeaponRuntimeController runtime;
        private readonly List<Projectile> active = new List<Projectile>();
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly Stack<GameObject> pool = new Stack<GameObject>();
        private readonly Dictionary<Sprite, PixelHitMask> masksBySprite = new Dictionary<Sprite, PixelHitMask>();

        public LinearProjectileExecutor(WeaponRuntimeController runtime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public int ActiveCount => active.Count;
        public int PooledCount => pool.Count;
        public int ReturnedToPoolCount { get; private set; }

        public bool Launch(in WeaponExecutionContext context, in LinearProjectileSpec spec)
        {
            if (active.Count >= MaxActiveProjectiles) return false;
            var visual = Acquire(context, spec.WeaponId, spec.VisualName);
            visual.transform.position = new Vector3(spec.Position.X, spec.Position.Y, 0f);
            active.Add(new Projectile(spec, visual, ResolveMask(visual.GetComponent<SpriteRenderer>())));
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
                var scale = renderer.transform.lossyScale;
                var pixelWorldSize = Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) / projectile.Mask.PixelsPerUnit;
                var stepSize = Mathf.Max(0.01f, pixelWorldSize * 0.5f);
                var maxSimulationTime = stepSize * (MaxSweepSamples - 1) / projectile.Speed;
                var processedTime = Mathf.Min(projectile.PendingSimulationTime, projectile.RemainingLifetime, maxSimulationTime);
                var travel = projectile.Speed * processedTime;
                projectile.Position = new Float2(
                    previousPosition.X + projectile.Direction.X * travel,
                    previousPosition.Y + projectile.Direction.Y * travel);
                projectile.Visual.transform.position = new Vector3(projectile.Position.X, projectile.Position.Y, 0f);
                if (processedTime > 0f)
                {
                    projectile.PendingSimulationTime -= processedTime;
                    projectile.RemainingLifetime -= processedTime;
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
        }

        private void SweepDamageContacts(Projectile projectile, Float2 previousPosition, in WeaponExecutionContext context)
        {
            var renderer = projectile.Visual.GetComponent<SpriteRenderer>();
            runtime.Targets.CopyTo(targets);
            var deltaX = projectile.Position.X - previousPosition.X;
            var deltaY = projectile.Position.Y - previousPosition.Y;
            var distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
            var scale = renderer.transform.lossyScale;
            var pixelWorldSize = Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) / projectile.Mask.PixelsPerUnit;
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
                    if (!runtime.DamageService.TryApply(
                            WeaponDamageRequest.Create(projectile.Attack, projectile.WeaponId, target, projectile.Damage, false, contact, ContactPhase.Direct, context.SimulationTick),
                            out _)) continue;
                    projectile.ImpactCount++;
                    if (projectile.ImpactCount >= projectile.MaxImpacts) return;
                }
            }
        }

        private GameObject Acquire(in WeaponExecutionContext context, WeaponId weaponId, string visualName)
        {
            var visual = pool.Count > 0 ? pool.Pop() : new GameObject(visualName);
            visual.transform.SetParent(context.PresentationRoot, false);
            var renderer = visual.GetComponent<SpriteRenderer>() ?? visual.AddComponent<SpriteRenderer>();
            renderer.sprite = context.SpriteFor(weaponId);
            renderer.sortingOrder = context.SortingOrder;
            renderer.color = Color.white;
            visual.transform.localScale = Vector3.one;
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
                Visual = visual; Mask = mask;
            }
            public AttackInstance Attack { get; }
            public WeaponId WeaponId { get; }
            public Float2 Position { get; set; }
            public Float2 Direction { get; }
            public float Speed { get; }
            public float RemainingLifetime { get; set; }
            public float PendingSimulationTime { get; set; }
            public int Damage { get; }
            public int MaxImpacts { get; }
            public int ImpactCount { get; set; }
            public GameObject Visual { get; }
            public PixelHitMask Mask { get; }
        }
    }

    public readonly struct LinearProjectileSpec
    {
        public LinearProjectileSpec(AttackInstance attack, WeaponId weaponId, Float2 position, Float2 direction, float speed, float lifetime, int damage, int maxImpacts, string visualName)
        {
            Attack = attack ?? throw new ArgumentNullException(nameof(attack));
            WeaponId = weaponId; Position = position; Direction = direction; Speed = Mathf.Max(0.01f, speed);
            Lifetime = Mathf.Max(0.01f, lifetime); Damage = Mathf.Max(1, damage); MaxImpacts = Mathf.Clamp(maxImpacts, 1, LinearProjectileExecutor.MaxImpactsPerProjectile);
            VisualName = string.IsNullOrEmpty(visualName) ? "Linear Projectile" : visualName;
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
    }
}
