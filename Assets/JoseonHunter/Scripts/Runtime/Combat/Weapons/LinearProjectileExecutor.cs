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
        public int ReturnedToPoolCount { get; private set; }

        public void Launch(in WeaponExecutionContext context, in LinearProjectileSpec spec)
        {
            var visual = Acquire(context, spec.VisualName);
            visual.transform.position = new Vector3(spec.Position.X, spec.Position.Y, 0f);
            active.Add(new Projectile(spec, visual, ResolveMask(visual.GetComponent<SpriteRenderer>())));
        }

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var projectile = active[index];
                projectile.RemainingLifetime -= deltaTime;
                projectile.Position = new Float2(
                    projectile.Position.X + projectile.Direction.X * projectile.Speed * deltaTime,
                    projectile.Position.Y + projectile.Direction.Y * projectile.Speed * deltaTime);
                projectile.Visual.transform.position = new Vector3(projectile.Position.X, projectile.Position.Y, 0f);
                TryDamageContacts(projectile, context);
                if (projectile.RemainingLifetime <= 0f || projectile.ImpactCount >= projectile.MaxImpacts)
                {
                    Release(projectile.Visual);
                    active.RemoveAt(index);
                }
            }
        }

        public void Reset()
        {
            foreach (var projectile in active) Release(projectile.Visual);
            active.Clear();
        }

        private void TryDamageContacts(Projectile projectile, in WeaponExecutionContext context)
        {
            var renderer = projectile.Visual.GetComponent<SpriteRenderer>();
            var attackTransform = TransformFor(renderer, projectile.Position);
            runtime.Targets.CopyTo(targets);
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

        private GameObject Acquire(in WeaponExecutionContext context, string visualName)
        {
            var visual = pool.Count > 0 ? pool.Pop() : new GameObject(visualName);
            visual.transform.SetParent(context.PresentationRoot, false);
            var renderer = visual.GetComponent<SpriteRenderer>() ?? visual.AddComponent<SpriteRenderer>();
            renderer.sprite = context.BladeSprite;
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
            pool.Push(visual);
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
            Lifetime = Mathf.Max(0.01f, lifetime); Damage = Mathf.Max(1, damage); MaxImpacts = Mathf.Max(1, maxImpacts);
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
