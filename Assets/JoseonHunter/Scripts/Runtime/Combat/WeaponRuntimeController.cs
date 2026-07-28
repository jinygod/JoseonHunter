using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat.Weapons;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat
{
    /// <summary>Owns weapon cooldowns and provides the small executor seam used by every runtime weapon.</summary>
    public interface IWeaponExecutor : IDisposable
    {
        void Tick(float deltaTime, in WeaponExecutionContext context);
        void Reset();
    }

    public readonly struct WeaponExecutionContext
    {
        public WeaponExecutionContext(Float2 ownerPosition, Transform presentationRoot, Sprite bladeSprite, int sortingOrder, int simulationTick)
            : this(ownerPosition, presentationRoot, bladeSprite, null, null, sortingOrder, simulationTick) { }

        public WeaponExecutionContext(Float2 ownerPosition, Transform presentationRoot, Sprite bladeSprite, Func<WeaponId, Sprite> spriteResolver, int sortingOrder, int simulationTick)
            : this(ownerPosition, presentationRoot, bladeSprite, spriteResolver, null, sortingOrder, simulationTick) { }

        public WeaponExecutionContext(Float2 ownerPosition, Transform presentationRoot, Sprite bladeSprite, Func<WeaponId, Sprite> spriteResolver, Func<WeaponId, PixelHitMask> maskResolver, int sortingOrder, int simulationTick)
        {
            OwnerPosition = ownerPosition;
            PresentationRoot = presentationRoot;
            BladeSprite = bladeSprite;
            this.spriteResolver = spriteResolver;
            this.maskResolver = maskResolver;
            SortingOrder = sortingOrder;
            SimulationTick = simulationTick;
        }

        public Float2 OwnerPosition { get; }
        public Transform PresentationRoot { get; }
        public Sprite BladeSprite { get; }
        private readonly Func<WeaponId, Sprite> spriteResolver;
        private readonly Func<WeaponId, PixelHitMask> maskResolver;
        public Sprite SpriteFor(WeaponId weaponId) => spriteResolver?.Invoke(weaponId) ?? BladeSprite;
        public PixelHitMask MaskFor(WeaponId weaponId) => maskResolver?.Invoke(weaponId);
        public int SortingOrder { get; }
        public int SimulationTick { get; }
    }

    public sealed class WeaponRuntimeController
    {
        private readonly List<IWeaponExecutor> executors = new List<IWeaponExecutor>();
        private readonly Dictionary<WeaponId, IWeaponExecutor> executorsByWeapon = new Dictionary<WeaponId, IWeaponExecutor>();
        private int simulationTick;
        private int nextAttackInstanceId = 1;
        private Func<WeaponId, Sprite> spriteResolver;
        private Func<WeaponId, PixelHitMask> maskResolver;
        private bool disposed;

        public WeaponRuntimeController(CombatTargetRegistry targets, CombatDamageService damageService, PixelHitMask bladeMask)
        {
            Targets = targets ?? throw new ArgumentNullException(nameof(targets));
            DamageService = damageService ?? throw new ArgumentNullException(nameof(damageService));
            BladeMask = bladeMask ?? throw new ArgumentNullException(nameof(bladeMask));
            AffixStatuses = new WeaponAffixStatusService(Targets, DamageService);
            DamageService.AttachAffixStatuses(AffixStatuses);
            Targets.TargetUnregistered += OnTargetUnregistered;
        }

        public CombatTargetRegistry Targets { get; }
        public CombatDamageService DamageService { get; }
        public PixelHitMask BladeMask { get; }
        public WeaponAffixStatusService AffixStatuses { get; }

        /// <summary>Allocates attack IDs across every executor sharing this combat runtime.</summary>
        public int AllocateAttackInstanceId()
        {
            if (nextAttackInstanceId == int.MaxValue) throw new InvalidOperationException("Attack instance ID space exhausted.");
            return nextAttackInstanceId++;
        }

        public void Register(IWeaponExecutor executor)
        {
            if (disposed) throw new ObjectDisposedException(nameof(WeaponRuntimeController));
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            executors.Add(executor);
        }

        public void Register(WeaponId weaponId, IWeaponExecutor executor)
        {
            if (executorsByWeapon.ContainsKey(weaponId))
                throw new InvalidOperationException($"Weapon '{weaponId}' is already registered.");
            Register(executor);
            executorsByWeapon.Add(weaponId, executor);
        }

        internal bool IsDisposedForTests => disposed;
        internal int RegisteredExecutorSlotCountForTests => executors.Count;
        internal int RegistrationCountForTests(WeaponId weaponId) => executorsByWeapon.ContainsKey(weaponId) ? 1 : 0;
        internal IWeaponExecutor ExecutorForTests(WeaponId weaponId) =>
            executorsByWeapon.TryGetValue(weaponId, out var executor) ? executor : null;
        internal bool IsEvolvedForTests(WeaponId weaponId) =>
            executorsByWeapon.TryGetValue(weaponId, out var executor) &&
            executor is IWeaponEvolutionProfile profile && profile.IsEvolved;

        public void SetSpriteResolver(Func<WeaponId, Sprite> resolver) => spriteResolver = resolver;
        public void SetMaskResolver(Func<WeaponId, PixelHitMask> resolver) => maskResolver = resolver;

        public void Tick(float deltaTime, Vector2 ownerPosition, Transform presentationRoot, Sprite bladeSprite, int sortingOrder)
        {
            if (disposed || deltaTime < 0f || presentationRoot == null) return;
            simulationTick++;
            AffixStatuses.Tick(deltaTime, simulationTick);
            var context = new WeaponExecutionContext(
                new Float2(ownerPosition.x, ownerPosition.y), presentationRoot, bladeSprite, spriteResolver, maskResolver, sortingOrder, simulationTick);
            foreach (var executor in executors) executor.Tick(deltaTime, context);
        }

        public void Reset()
        {
            if (disposed) return;
            simulationTick = 0;
            foreach (var executor in executors) executor.Reset();
            AffixStatuses.Reset();
            DamageService.ClearAttacks();
        }

        /// <summary>Terminal cleanup for a runtime replacement: executors release presentation objects and retire attacks.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var executor in executors) executor.Dispose();
            executors.Clear();
            executorsByWeapon.Clear();
            AffixStatuses.Reset();
            Targets.TargetUnregistered -= OnTargetUnregistered;
            DamageService.DetachAffixStatuses(AffixStatuses);
            DamageService.ClearAttacks();
            spriteResolver = null;
            maskResolver = null;
        }

        private void OnTargetUnregistered(ICombatTarget target)
        {
            if (target != null) AffixStatuses.ClearTarget(target.RuntimeId);
        }
    }
}
