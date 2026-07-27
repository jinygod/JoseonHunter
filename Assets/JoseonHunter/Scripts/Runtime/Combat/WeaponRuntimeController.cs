using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat.Weapons;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat
{
    /// <summary>Owns weapon cooldowns and provides the small executor seam used by every runtime weapon.</summary>
    public interface IWeaponExecutor
    {
        void Tick(float deltaTime, in WeaponExecutionContext context);
        void Reset();
    }

    public readonly struct WeaponExecutionContext
    {
        public WeaponExecutionContext(Float2 ownerPosition, Transform presentationRoot, Sprite bladeSprite, int sortingOrder, int simulationTick)
        {
            OwnerPosition = ownerPosition;
            PresentationRoot = presentationRoot;
            BladeSprite = bladeSprite;
            SortingOrder = sortingOrder;
            SimulationTick = simulationTick;
        }

        public Float2 OwnerPosition { get; }
        public Transform PresentationRoot { get; }
        public Sprite BladeSprite { get; }
        public int SortingOrder { get; }
        public int SimulationTick { get; }
    }

    public sealed class WeaponRuntimeController
    {
        private readonly List<IWeaponExecutor> executors = new List<IWeaponExecutor>();
        private int simulationTick;

        public WeaponRuntimeController(CombatTargetRegistry targets, CombatDamageService damageService, PixelHitMask bladeMask)
        {
            Targets = targets ?? throw new ArgumentNullException(nameof(targets));
            DamageService = damageService ?? throw new ArgumentNullException(nameof(damageService));
            BladeMask = bladeMask ?? throw new ArgumentNullException(nameof(bladeMask));
        }

        public CombatTargetRegistry Targets { get; }
        public CombatDamageService DamageService { get; }
        public PixelHitMask BladeMask { get; }

        public void Register(IWeaponExecutor executor)
        {
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            executors.Add(executor);
        }

        public void Tick(float deltaTime, Vector2 ownerPosition, Transform presentationRoot, Sprite bladeSprite, int sortingOrder)
        {
            if (deltaTime < 0f || presentationRoot == null) return;
            simulationTick++;
            var context = new WeaponExecutionContext(
                new Float2(ownerPosition.x, ownerPosition.y), presentationRoot, bladeSprite, sortingOrder, simulationTick);
            foreach (var executor in executors) executor.Tick(deltaTime, context);
        }

        public void Reset()
        {
            simulationTick = 0;
            foreach (var executor in executors) executor.Reset();
        }
    }
}
