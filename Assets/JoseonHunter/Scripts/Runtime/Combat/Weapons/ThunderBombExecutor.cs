using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public enum ThunderBombState { Lob, Fuse, Blast, SecondaryShockwave, Pull, CompressionDelay, CompressedBlast, Complete }

    /// <summary>A deterministic lob followed by a one-shot, pixel-confirmed expanding blast ring.</summary>
    public sealed class ThunderBombExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        private const int MaximumBombs = 4;
        private readonly WeaponRuntimeController runtime;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<Bomb> bombs = new List<Bomb>();
        private readonly PixelHitMask ringMask = CreateRingMask();
        private readonly PixelHitMask compressedMask = CreateCompressedMask();
        private readonly List<string> stateOrder = new List<string>();
        private readonly List<DelayedPotentialStrike> delayedStrikes = new List<DelayedPotentialStrike>();
        private readonly List<GroundCurrent> groundCurrents = new List<GroundCurrent>();
        private readonly Dictionary<int, EarthCurrentContact> earthCurrentContacts = new Dictionary<int, EarthCurrentContact>();
        private readonly List<int> earthContactIds = new List<int>();
        private readonly List<ICombatTarget> propagationTargets = new List<ICombatTarget>(5);
        private WeaponTransientVisualPool transientVisuals;
        private Transform transientVisualRoot;
        private float cooldown;

        public ThunderBombExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float lobDuration, float fuseDuration, float blastRadius, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            LegacySourceDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage));
            var legacyDamage = modifiers.Legacy.Is(WeaponLegacyPathId.ThunderEarthCurrent) ? .7f : 1f;
            var legacyCooldown = modifiers.Legacy.Is(WeaponLegacyPathId.ThunderPrison) ? 1.25f : 1f;
            BaseDamage = LegacySourceDamage * legacyDamage; CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds) * legacyCooldown); Range = Mathf.Max(0.01f, modifiers.ScaleArea(range));
            LobDuration = Mathf.Max(0.01f, modifiers.ScaleDuration(lobDuration)); FuseDuration = Mathf.Max(0f, fuseDuration); BlastRadius = Mathf.Max(0.01f, modifiers.ScaleArea(blastRadius)); Level = Mathf.Clamp(level, 1, 5); Potentials = modifiers;
            IsEvolved = evolved;
            runtime.DamageService.DamageConfirmed += OnDamageConfirmed;
        }

        public float BaseDamage { get; }
        private float LegacySourceDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float LobDuration { get; }
        public float FuseDuration { get; }
        public float BlastRadius { get; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public int ActiveBombCount => bombs.Count;
        public ThunderBombState LastState { get; private set; } = ThunderBombState.Complete;
        public Float2 LastLandingPosition { get; private set; }
        public IReadOnlyList<string> StateOrder => stateOrder;
        public int LastPulledTargetCount { get; private set; }
        public int LastLightningRodTargetRuntimeId { get; private set; }
        private bool UsesPrison => IsEvolved || Potentials.Legacy.Is(WeaponLegacyPathId.ThunderPrison);
#if UNITY_INCLUDE_TESTS
        public float FirstBombVisualHeightForTests => bombs.Count == 0 ? 0f : bombs[0].Height;
        public int FirstVisualPartIndexForTests => bombs.Count == 0 ? -1 : bombs[0].VisualPartIndex;
        public int PendingEarthCurrentCountForTests
        {
            get
            {
                var count = 0;
                foreach (var strike in delayedStrikes)
                    if (strike.Phase == ContactPhase.PotentialBlast) count++;
                return count;
            }
        }
        public float LastPullDurationForTests { get; private set; }
        public int MaximumCurrentTargetsQueriedForTests { get; private set; }
        public int ActiveGroundCurrentCountForTests => groundCurrents.Count;
        public int LastEarthPropagationCountForTests { get; private set; }
        public int LastEarthCurrentTargetRuntimeIdForTests { get; private set; }
#endif

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            var step = Mathf.Max(0f, deltaTime);
            EnsureTransientVisuals(context.PresentationRoot);
            transientVisuals?.Tick(step);
            cooldown -= step;
            if (cooldown <= 0f && bombs.Count < MaximumBombs && (!UsesPrison || bombs.Count == 0) && TryFindPredictedCrowd(context.OwnerPosition, out var landing))
            {
                cooldown = CooldownSeconds;
                if (UsesPrison) stateOrder.Clear();
                if (IsEvolved) landing = Lerp(context.OwnerPosition, landing, 0.5f);
                var bomb = new Bomb(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), context.OwnerPosition, landing);
                CreateVisual(bomb, context);
                bombs.Add(bomb);
                LastLandingPosition = landing;
            }

            for (var index = bombs.Count - 1; index >= 0; index--)
            {
                var bomb = bombs[index];
                Advance(bomb, step, context);
                UpdateVisual(bomb, context);
                bomb.SecondaryPresentationPending = false;
                LastState = bomb.State;
                if (bomb.State != ThunderBombState.Complete) continue;
                runtime.DamageService.RetireAttack(bomb.Attack.InstanceId);
                DestroyVisual(bomb);
                bombs.RemoveAt(index);
            }
            for (var index = delayedStrikes.Count - 1; index >= 0; index--)
            {
                var strike = delayedStrikes[index]; strike.Remaining -= step;
                if (strike.Remaining > 0f) { delayedStrikes[index] = strike; continue; }
                if (strike.TargetRuntimeId != 0)
                {
                    if (runtime.Targets.TryGet(strike.TargetRuntimeId, out var target) && target != null && target.IsAlive && target.HurtMask != null &&
                        PixelMaskContactService.TryFindContact(strike.Mask, PixelMaskTransform.Translation(strike.TrackTargetPosition ? target.WorldPosition.X : strike.Position.X, strike.TrackTargetPosition ? target.WorldPosition.Y : strike.Position.Y), target.HurtMask, target.HurtMaskTransform, out var contact))
                    {
                        var traits = strike.Phase == ContactPhase.PotentialBlast
                            ? WeaponHitTrait.Explosion : WeaponHitTrait.None;
                        if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(strike.Attack,
                                WeaponId.ThunderCrashBomb, target,
                                Mathf.CeilToInt(BaseDamage * strike.Multiplier), false, contact,
                                strike.Phase, context.SimulationTick, true, traits, strike.Position), out _) &&
                            strike.Phase == ContactPhase.PotentialBlast)
                            runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId,
                                CombatStatusKind.Shock, 2f, 1, WeaponId.ThunderCrashBomb);
                    }
                }
                else
                {
                    runtime.Targets.CopyTo(targets);
                    foreach (var target in targets)
                        if (target != null && target.IsAlive && target.HurtMask != null && PixelMaskContactService.TryFindContact(strike.Mask, PixelMaskTransform.Translation(strike.Position.X, strike.Position.Y), target.HurtMask, target.HurtMaskTransform, out var contact))
                        {
                            var traits = strike.Phase == ContactPhase.PotentialBlast
                                ? WeaponHitTrait.Explosion : WeaponHitTrait.None;
                            if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(strike.Attack,
                                    WeaponId.ThunderCrashBomb, target,
                                    Mathf.CeilToInt(BaseDamage * strike.Multiplier), false, contact,
                                    strike.Phase, context.SimulationTick, true, traits, strike.Position), out _) &&
                                strike.Phase == ContactPhase.PotentialBlast)
                                runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId,
                                    CombatStatusKind.Shock, 2f, 1, WeaponId.ThunderCrashBomb);
                        }
                }
                runtime.DamageService.RetireAttack(strike.Attack.InstanceId); delayedStrikes.RemoveAt(index);
            }
            TickGroundCurrents(step, context);
        }

        public void Reset()
        {
            foreach (var bomb in bombs)
            {
                runtime.DamageService.RetireAttack(bomb.Attack.InstanceId);
                DestroyVisual(bomb);
            }
            bombs.Clear(); stateOrder.Clear(); cooldown = 0f; LastState = ThunderBombState.Complete; LastLandingPosition = default;
            foreach (var strike in delayedStrikes) runtime.DamageService.RetireAttack(strike.Attack.InstanceId);
            delayedStrikes.Clear();
            foreach (var current in groundCurrents) runtime.DamageService.RetireAttack(current.Attack.InstanceId);
            groundCurrents.Clear(); earthCurrentContacts.Clear(); earthContactIds.Clear();
            propagationTargets.Clear();
            LastPulledTargetCount = 0;
            LastLightningRodTargetRuntimeId = 0;
#if UNITY_INCLUDE_TESTS
            LastPullDurationForTests = 0f;
            MaximumCurrentTargetsQueriedForTests = 0;
            LastEarthPropagationCountForTests = 0;
            LastEarthCurrentTargetRuntimeIdForTests = 0;
#endif
            transientVisuals?.Dispose(); transientVisuals = null; transientVisualRoot = null;
        }

        public void Dispose()
        {
            runtime.DamageService.DamageConfirmed -= OnDamageConfirmed;
            Reset();
        }

        private void Advance(Bomb bomb, float step, in WeaponExecutionContext context)
        {
            if (UsesPrison)
            {
                AdvanceEvolved(bomb, step, context);
                return;
            }
            switch (bomb.State)
            {
                case ThunderBombState.Lob:
                    bomb.Elapsed += step;
                    var progress = Mathf.Clamp01(bomb.Elapsed / LobDuration);
                    // The arc height is presentation-ready deterministic state; landing always remains the predicted center.
                    bomb.Position = Lerp(bomb.Start, bomb.Landing, progress);
                    bomb.Height = 4f * progress * (1f - progress) * .55f;
                    if (progress >= 1f) Transition(bomb, ThunderBombState.Fuse);
                    break;
                case ThunderBombState.Fuse:
                    bomb.Elapsed += step;
                    // Fuse completion only enables the ring. No damage is evaluated in this state.
                    if (bomb.Elapsed >= FuseDuration) { bomb.State = ThunderBombState.Blast; bomb.Elapsed = 0f; }
                    break;
                case ThunderBombState.Blast:
                    bomb.Elapsed += step;
                    var blastComplete = SweepRing(bomb, BlastRadius * Mathf.Clamp01(bomb.Elapsed / BlastDuration), context);
                    if (bomb.Elapsed >= BlastDuration && blastComplete)
                    {
                        if (Level == 5)
                        {
                            bomb.State = ThunderBombState.SecondaryShockwave;
                            bomb.Elapsed = 0f;
                            bomb.SecondaryPresentationPending = true;
                        }
                        else bomb.State = ThunderBombState.Complete;
                    }
                    break;
                case ThunderBombState.SecondaryShockwave:
                    bomb.Elapsed += step;
                    var secondaryComplete = SweepRing(bomb, BlastRadius * (1f + Mathf.Clamp01(bomb.Elapsed / SecondaryDuration)), context);
                    if (bomb.Elapsed >= SecondaryDuration && secondaryComplete) bomb.State = ThunderBombState.Complete;
                    break;
            }
        }

        private void AdvanceEvolved(Bomb bomb, float step, in WeaponExecutionContext context)
        {
            var remaining = step;
            while (remaining > 0f && bomb.State != ThunderBombState.Complete)
            {
                switch (bomb.State)
                {
                    case ThunderBombState.Lob:
                        var lobSlice = Mathf.Min(remaining, LobDuration - bomb.Elapsed);
                        bomb.Elapsed += lobSlice;
                        remaining -= lobSlice;
                        var progress = Mathf.Clamp01(bomb.Elapsed / LobDuration);
                        bomb.Position = Lerp(bomb.Start, bomb.Landing, progress);
                        bomb.Height = 4f * progress * (1f - progress) * .55f;
                        if (bomb.Elapsed >= LobDuration) Transition(bomb, ThunderBombState.Pull);
                        break;
                    case ThunderBombState.Pull:
                        var pullDuration = Potentials.Legacy.Is(WeaponLegacyPathId.ThunderPrison)
                            ? LegacyPullDuration : PullDuration;
                        var pullSlice = Mathf.Min(remaining, pullDuration - bomb.Elapsed);
                        PullTargets(bomb, pullSlice);
                        bomb.Elapsed += pullSlice;
                        remaining -= pullSlice;
                        if (bomb.Elapsed >= pullDuration)
                        {
#if UNITY_INCLUDE_TESTS
                            LastPullDurationForTests = pullDuration;
#endif
                            Transition(bomb, ThunderBombState.CompressionDelay);
                        }
                        break;
                    case ThunderBombState.CompressionDelay:
                        var delaySlice = Mathf.Min(remaining, CompressionDelay - bomb.Elapsed);
                        bomb.Elapsed += delaySlice;
                        remaining -= delaySlice;
                        if (bomb.Elapsed >= CompressionDelay)
                        {
                            Transition(bomb, ThunderBombState.CompressedBlast);
                            ResolveCompressedBlast(bomb, context);
                            bomb.State = ThunderBombState.Complete;
                        }
                        break;
                    case ThunderBombState.CompressedBlast:
                        ResolveCompressedBlast(bomb, context);
                        bomb.State = ThunderBombState.Complete;
                        break;
                    default:
                        bomb.State = ThunderBombState.Complete;
                        break;
                }
            }
        }

        private void Transition(Bomb bomb, ThunderBombState next)
        {
            bomb.State = next;
            bomb.Elapsed = 0f;
            if (next == ThunderBombState.Pull || next == ThunderBombState.CompressionDelay || next == ThunderBombState.CompressedBlast)
                stateOrder.Add(next.ToString());
        }

        private void CreateVisual(Bomb bomb, in WeaponExecutionContext context)
        {
            if (context.PresentationRoot == null) return;
            bomb.Visual = new GameObject("Thunder Crash Bomb");
            bomb.Visual.transform.SetParent(context.PresentationRoot, false);
            var renderer = bomb.Visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = context.SortingOrder + 1;
            var shadow = new GameObject("Bomb Shadow");
            shadow.transform.SetParent(context.PresentationRoot, false);
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.color = new Color(.08f, .09f, .12f, .32f);
            shadowRenderer.sortingOrder = context.SortingOrder - 1;
            bomb.Shadow = shadow;
            UpdateVisual(bomb, context);
        }

        private void UpdateVisual(Bomb bomb, in WeaponExecutionContext context)
        {
            if (bomb.Visual == null) return;
            var partIndex = ResolveVisualPartIndex(bomb);
            bomb.VisualPartIndex = partIndex;
            var sprite = context.PresentationSpriteFor(WeaponId.ThunderCrashBomb, partIndex);
            var renderer = bomb.Visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            bomb.Visual.transform.position = new Vector3(bomb.Position.X, bomb.Position.Y + bomb.Height, 0f);
            bomb.Visual.transform.localScale = ResolveVisualScale(bomb, sprite);

            if (bomb.Shadow == null) return;
            var shadowRenderer = bomb.Shadow.GetComponent<SpriteRenderer>();
            shadowRenderer.sprite = context.PresentationSpriteFor(
                WeaponId.ThunderCrashBomb, WeaponVisualPartIndex.ThunderCrash.Projectile);
            bomb.Shadow.transform.position = new Vector3(bomb.Position.X, bomb.Position.Y, 0f);
            bomb.Shadow.transform.localScale = ScaleSpriteToWorldDiameter(
                shadowRenderer.sprite,
                Mathf.Lerp(.48f, .28f, Mathf.Clamp01(bomb.Height / .55f)));
            bomb.Shadow.SetActive(bomb.State == ThunderBombState.Lob);
        }

        private int ResolveVisualPartIndex(Bomb bomb)
        {
            switch (bomb.State)
            {
                case ThunderBombState.Lob:
                    return WeaponVisualPartIndex.ThunderCrash.Projectile +
                        Mathf.Min(WeaponVisualPartIndex.ThunderCrash.ProjectileFrameCount - 1,
                            Mathf.FloorToInt(Mathf.Clamp01(bomb.Elapsed / LobDuration) *
                                WeaponVisualPartIndex.ThunderCrash.ProjectileFrameCount));
                case ThunderBombState.Fuse:
                case ThunderBombState.Pull:
                case ThunderBombState.CompressionDelay:
                    return WeaponVisualPartIndex.ThunderCrash.Windup +
                        Mathf.FloorToInt(bomb.Elapsed / .05f) % WeaponVisualPartIndex.ThunderCrash.WindupFrameCount;
                case ThunderBombState.Blast:
                case ThunderBombState.CompressedBlast:
                    return WeaponVisualPartIndex.ThunderCrash.Detonation +
                        Mathf.Min(WeaponVisualPartIndex.ThunderCrash.DetonationFrameCount - 1,
                            Mathf.FloorToInt(Mathf.Clamp01(bomb.Elapsed / BlastDuration) *
                                WeaponVisualPartIndex.ThunderCrash.DetonationFrameCount));
                case ThunderBombState.SecondaryShockwave:
                    if (bomb.SecondaryPresentationPending)
                        return WeaponVisualPartIndex.ThunderCrash.Detonation +
                            WeaponVisualPartIndex.ThunderCrash.DetonationFrameCount - 1;
                    return WeaponVisualPartIndex.ThunderCrash.Field +
                        Mathf.Min(WeaponVisualPartIndex.ThunderCrash.FieldFrameCount - 1,
                            Mathf.FloorToInt(Mathf.Clamp01(bomb.Elapsed / SecondaryDuration) *
                                WeaponVisualPartIndex.ThunderCrash.FieldFrameCount));
                default:
                    return WeaponVisualPartIndex.ThunderCrash.Detonation +
                        WeaponVisualPartIndex.ThunderCrash.DetonationFrameCount - 1;
            }
        }

        private Vector3 ResolveVisualScale(Bomb bomb, Sprite sprite)
        {
            float diameter;
            if (bomb.State == ThunderBombState.Blast)
                diameter = Mathf.Max(.1f, bomb.SweptRadius * 2f);
            else if (bomb.State == ThunderBombState.SecondaryShockwave)
                diameter = Mathf.Max(.1f, bomb.SweptRadius * 2f);
            else if (bomb.State == ThunderBombState.CompressedBlast)
                diameter = BlastRadius * 2f;
            else
                diameter = .62f;
            return ScaleSpriteToWorldDiameter(sprite, diameter);
        }

        private static Vector3 ScaleSpriteToWorldDiameter(Sprite sprite, float worldDiameter)
        {
            if (sprite == null) return Vector3.one;
            var nativeDiameter = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            return Vector3.one * (worldDiameter / Mathf.Max(.01f, nativeDiameter));
        }

        private void EnsureTransientVisuals(Transform root)
        {
            if (!Application.isPlaying || root == null || root == transientVisualRoot) return;
            transientVisuals?.Dispose();
            transientVisualRoot = root;
            transientVisuals = new WeaponTransientVisualPool(root);
        }

        private static void DestroyVisual(Bomb bomb)
        {
            if (bomb?.Visual != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(bomb.Visual);
                else UnityEngine.Object.DestroyImmediate(bomb.Visual);
            }
            if (bomb?.Shadow != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(bomb.Shadow);
                else UnityEngine.Object.DestroyImmediate(bomb.Shadow);
            }
            if (bomb == null) return;
            bomb.Visual = null;
            bomb.Shadow = null;
        }

        private void PullTargets(Bomb bomb, float step)
        {
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets)
            {
                if (!IsInRange(target, bomb.Landing, BlastRadius)) continue;
                var direction = new Float2(bomb.Landing.X - target.WorldPosition.X, bomb.Landing.Y - target.WorldPosition.Y);
                var distance = Mathf.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
                if (distance <= 0.0001f) continue;
                var force = Mathf.Min(distance, PullSpeed * step);
                if (force <= 0f) continue;
                var before = target.WorldPosition;
                target.ApplyKnockback(new Float2(direction.X / distance, direction.Y / distance), force);
                var after = target.WorldPosition;
                if (DistanceSquared(before, after) > .000001f) bomb.PulledTargetIds.Add(target.RuntimeId);
            }
        }

        private void ResolveCompressedBlast(Bomb bomb, in WeaponExecutionContext context)
        {
            LastPulledTargetCount = bomb.PulledTargetIds.Count;
            runtime.Targets.CopyTo(targets);
            var completedPrison = Potentials.Legacy.Is(WeaponLegacyPathId.ThunderPrison) &&
                                  Potentials.Legacy.Stage == WeaponLegacyStage.Completed;
            var resolvedRadius = completedPrison ? BlastRadius * .45f : BlastRadius;
            var transform = new PixelMaskTransform(bomb.Landing, 0, false,
                new Vector2(resolvedRadius * 2f, resolvedRadius * 2f));
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                if (!PixelMaskContactService.TryFindContact(compressedMask, transform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                var multiplier = IsEvolved ? 2f : 1f;
                if (Potentials.Legacy.Is(WeaponLegacyPathId.ThunderPrison))
                {
                    if (completedPrison) multiplier = 3f;
                    else if (Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced &&
                             DistanceSquared(target.WorldPosition, bomb.Landing) <=
                             BlastRadius * BlastRadius * .45f * .45f) multiplier = 1.6f;
                }
                var coreContact = Potentials.HasPotential(WeaponPotentialId.ThunderOverchargedCore) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.ThunderOverchargedCore, out _, out var coreMask) &&
                    PixelMaskContactService.TryFindContact(coreMask, PixelMaskTransform.Translation(bomb.Landing.X, bomb.Landing.Y), target.HurtMask, target.HurtMaskTransform, out _);
                if (coreContact) multiplier *= 1f + Mathf.Min(.80f, bomb.PulledTargetIds.Count * .08f);
                if (!runtime.DamageService.TryApply(WeaponDamageRequest.Create(bomb.Attack, WeaponId.ThunderCrashBomb, target, Mathf.CeilToInt(BaseDamage * multiplier), false, contact, ContactPhase.Blast, context.SimulationTick,
                    true, WeaponHitTrait.Explosion | WeaponHitTrait.Pull, bomb.Landing), out _)) continue;
                bomb.MainBlastConfirmed = true;
                runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Shock,
                    2f, 1, WeaponId.ThunderCrashBomb);
                PlayConfirmedBlast(context, contact);
                if (Potentials.HasPotential(WeaponPotentialId.ThunderLightningRod) && (bomb.LightningRodTarget == null || target.ThreatScore > bomb.LightningRodTarget.ThreatScore || target.ThreatScore == bomb.LightningRodTarget.ThreatScore && target.RuntimeId < bomb.LightningRodTarget.RuntimeId))
                    bomb.LightningRodTarget = target;
            }
            if (bomb.MainBlastConfirmed && Potentials.HasPotential(WeaponPotentialId.ThunderEarthCurrent) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.ThunderEarthCurrent, out _, out var earthMask))
                delayedStrikes.Add(new DelayedPotentialStrike(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), 0, bomb.Landing, earthMask, .35f, .65f, ContactPhase.PotentialBlast));
            if (bomb.LightningRodTarget != null && WeaponPotentialVisuals.TryGet(WeaponPotentialId.ThunderLightningRod, out _, out var rodMask))
            {
                LastLightningRodTargetRuntimeId = bomb.LightningRodTarget.RuntimeId;
                delayedStrikes.Add(new DelayedPotentialStrike(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), bomb.LightningRodTarget.RuntimeId, bomb.Landing, rodMask, .45f, .90f, ContactPhase.PotentialChain, true));
            }
        }

        private bool SweepRing(Bomb bomb, float desiredRadius, in WeaponExecutionContext context)
        {
            if (desiredRadius <= bomb.SweptRadius) return true;
            var radialStep = Mathf.Max(0.01f, BlastRadius / 16f);
            var end = Mathf.Min(desiredRadius, bomb.SweptRadius + radialStep * MaxRingSweepSamples);
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                var radius = Mathf.Sqrt(DistanceSquared(bomb.Landing, target.WorldPosition));
                if (radius <= bomb.SweptRadius + 0.0001f || radius > end + 0.0001f) continue;
                var scale = Mathf.Max(0.01f, radius * 2f);
                var transform = new PixelMaskTransform(bomb.Landing, 0, false, new Vector2(scale, scale));
                if (!PixelMaskContactService.TryFindContact(ringMask, transform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                if (!runtime.DamageService.TryApply(WeaponDamageRequest.Create(bomb.Attack, WeaponId.ThunderCrashBomb, target, Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.Blast, context.SimulationTick,
                    true, WeaponHitTrait.Explosion, bomb.Landing), out _)) continue;
                bomb.MainBlastConfirmed = true;
                runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Shock,
                    2f, 1, WeaponId.ThunderCrashBomb);
                PlayConfirmedBlast(context, contact);
                if (Potentials.HasPotential(WeaponPotentialId.ThunderLightningRod) && (bomb.LightningRodTarget == null || target.ThreatScore > bomb.LightningRodTarget.ThreatScore || target.ThreatScore == bomb.LightningRodTarget.ThreatScore && target.RuntimeId < bomb.LightningRodTarget.RuntimeId))
                    bomb.LightningRodTarget = target;
            }
            bomb.SweptRadius = end;
            if (bomb.SweptRadius + .0001f >= BlastRadius)
            {
                if (bomb.MainBlastConfirmed && !bomb.CurrentSpawned &&
                    Potentials.Legacy.Is(WeaponLegacyPathId.ThunderEarthCurrent))
                {
                    bomb.CurrentSpawned = true;
                    groundCurrents.Add(new GroundCurrent(
                        new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, .5f),
                        bomb.Landing,
                        Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced ? 4f : 3f,
                        Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced ? 3 : 1));
                }
                if (bomb.MainBlastConfirmed && Potentials.HasPotential(WeaponPotentialId.ThunderEarthCurrent) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.ThunderEarthCurrent, out _, out var crackMask))
                    delayedStrikes.Add(new DelayedPotentialStrike(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), 0, bomb.Landing, crackMask, .35f, .65f, ContactPhase.PotentialBlast));
                if (bomb.LightningRodTarget != null && WeaponPotentialVisuals.TryGet(WeaponPotentialId.ThunderLightningRod, out _, out var rodMask))
                {
                    LastLightningRodTargetRuntimeId = bomb.LightningRodTarget.RuntimeId;
                    delayedStrikes.Add(new DelayedPotentialStrike(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), bomb.LightningRodTarget.RuntimeId, bomb.LightningRodTarget.WorldPosition, rodMask, .45f, .90f, ContactPhase.PotentialChain, true));
                }
            }
            return bomb.SweptRadius + 0.0001f >= desiredRadius;
        }

        private void PlayConfirmedBlast(in WeaponExecutionContext context, Float2 contact)
        {
            var cue = new WeaponVisualCue(WeaponId.ThunderCrashBomb, WeaponVisualStage.Detonation, Level, IsEvolved, .72f, .10f);
            transientVisuals?.Play(
                context.PresentationSpriteFor(WeaponId.ThunderCrashBomb, WeaponVisualPartIndex.ThunderCrash.Detonation),
                new Vector3(contact.X, contact.Y, 0f), Quaternion.identity, Vector3.one * cue.ResolvedScale,
                Color.white, cue.ResolvedLifetime, context.SortingOrder + 2);
        }

        private bool TryFindPredictedCrowd(Float2 origin, out Float2 landing)
        {
            runtime.Targets.CopyTo(targets);
            ICombatTarget best = null; var mostNeighbors = -1;
            foreach (var candidate in targets)
            {
                if (!IsInRange(candidate, origin, Range)) continue;
                var neighbors = 0;
                foreach (var other in targets)
                    if (IsInRange(other, origin, Range) && DistanceSquared(candidate.WorldPosition, other.WorldPosition) <= Range * Range * 0.25f) neighbors++;
                if (neighbors > mostNeighbors || neighbors == mostNeighbors && (best == null || candidate.RuntimeId < best.RuntimeId)) { best = candidate; mostNeighbors = neighbors; }
            }
            if (best == null) { landing = default; return false; }
            var sumX = 0f; var sumY = 0f; var count = 0;
            foreach (var candidate in targets)
                if (IsInRange(candidate, origin, Range) && DistanceSquared(best.WorldPosition, candidate.WorldPosition) <= Range * Range * 0.25f) { sumX += candidate.WorldPosition.X; sumY += candidate.WorldPosition.Y; count++; }
            landing = new Float2(sumX / count, sumY / count);
            return true;
        }

        private static bool IsInRange(ICombatTarget target, Float2 origin, float range) => target != null && target.IsAlive && DistanceSquared(target.WorldPosition, origin) <= range * range;
        private static float DistanceSquared(Float2 left, Float2 right) { var x = left.X - right.X; var y = left.Y - right.Y; return x * x + y * y; }
        private static Float2 Lerp(Float2 left, Float2 right, float progress) => new Float2(Mathf.Lerp(left.X, right.X, progress), Mathf.Lerp(left.Y, right.Y, progress));
        private const float BlastDuration = 0.24f;
        private const float SecondaryDuration = 0.16f;
        private const float PullDuration = 0.25f;
        private const float LegacyPullDuration = 1f;
        private const float CompressionDelay = 0.12f;
        private const float PullSpeed = 4f;
        private const int MaxRingSweepSamples = 64;

        private static PixelHitMask CreateRingMask()
        {
            const int size = 17; const int center = 8; var packed = new uint[(size * size + 31) / 32];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var dx = x - center; var dy = y - center; var squared = dx * dx + dy * dy;
                if (squared < 42 || squared > 72) continue;
                var bit = y * size + x; packed[bit >> 5] |= 1u << (bit & 31);
            }
            return new PixelHitMask(size, size, new Vector2(center, center), 16f, packed);
        }

        private static PixelHitMask CreateCompressedMask()
        {
            const int size = 17; const int center = 8; var packed = new uint[(size * size + 31) / 32];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var dx = x - center; var dy = y - center;
                if (dx * dx + dy * dy > 64) continue;
                var bit = y * size + x; packed[bit >> 5] |= 1u << (bit & 31);
            }
            return new PixelHitMask(size, size, new Vector2(center, center), 16f, packed);
        }

        private sealed class Bomb
        {
            public Bomb(AttackInstance attack, Float2 start, Float2 landing) { Attack = attack; Start = start; Landing = landing; Position = start; }
            public AttackInstance Attack { get; }
            public Float2 Start { get; }
            public Float2 Landing { get; }
            public Float2 Position { get; set; }
            public float Height { get; set; }
            public float Elapsed { get; set; }
            public float SweptRadius { get; set; }
            public bool MainBlastConfirmed { get; set; }
            public ThunderBombState State { get; set; } = ThunderBombState.Lob;
            public HashSet<int> PulledTargetIds { get; } = new HashSet<int>();
            public ICombatTarget LightningRodTarget { get; set; }
            public GameObject Visual { get; set; }
            public GameObject Shadow { get; set; }
            public int VisualPartIndex { get; set; } = WeaponVisualPartIndex.ThunderCrash.Projectile;
            public bool SecondaryPresentationPending { get; set; }
            public bool CurrentSpawned { get; set; }
        }

        private void TickGroundCurrents(float step, in WeaponExecutionContext context)
        {
            TickEarthCurrentContacts(step);
            for (var index = groundCurrents.Count - 1; index >= 0; index--)
            {
                var current = groundCurrents[index];
                current.Remaining -= step;
                current.TickElapsed += step;
                while (current.TickElapsed + .0001f >= .5f && current.Remaining >= -.0001f)
                {
                    current.TickElapsed -= .5f;
                    current.HitTime += .5f;
                    var selected = SelectCurrentTargets(current);
#if UNITY_INCLUDE_TESTS
                    MaximumCurrentTargetsQueriedForTests = Mathf.Max(
                        MaximumCurrentTargetsQueriedForTests, selected);
#endif
                    foreach (var target in targets)
                    {
                        if (!runtime.DamageService.TryApply(WeaponDamageRequest.Create(current.Attack,
                            WeaponId.ThunderCrashBomb, target,
                            Mathf.CeilToInt(LegacySourceDamage * .3f), false, target.WorldPosition,
                            current.IsPropagation ? ContactPhase.PotentialChain : ContactPhase.PotentialBlast,
                            context.SimulationTick, current.HitTime, true, WeaponHitTrait.Explosion,
                            current.Center), out _)) continue;
                        runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Shock,
                            2f, 1, WeaponId.ThunderCrashBomb);
#if UNITY_INCLUDE_TESTS
                        LastEarthCurrentTargetRuntimeIdForTests = target.RuntimeId;
#endif
                        earthCurrentContacts[target.RuntimeId] = new EarthCurrentContact(
                            Mathf.Max(0f, current.Remaining), !current.IsPropagation);
                    }
                }
                if (current.Remaining > 0f)
                {
                    groundCurrents[index] = current;
                    continue;
                }
                runtime.DamageService.RetireAttack(current.Attack.InstanceId);
                groundCurrents.RemoveAt(index);
            }
        }

        private int SelectCurrentTargets(GroundCurrent current)
        {
            targets.Clear();
            if (current.TargetRuntimeId != 0)
            {
                if (runtime.Targets.TryGet(current.TargetRuntimeId, out var fixedTarget) &&
                    fixedTarget != null && fixedTarget.IsAlive) targets.Add(fixedTarget);
                return targets.Count;
            }
            runtime.Targets.CopyTo(targets);
            for (var index = targets.Count - 1; index >= 0; index--)
            {
                var candidate = targets[index];
                if (candidate == null || !candidate.IsAlive ||
                    DistanceSquared(candidate.WorldPosition, current.Center) > BlastRadius * BlastRadius)
                    targets.RemoveAt(index);
            }
            targets.Sort((left, right) =>
            {
                var distance = DistanceSquared(left.WorldPosition, current.Center)
                    .CompareTo(DistanceSquared(right.WorldPosition, current.Center));
                return distance != 0 ? distance : left.RuntimeId.CompareTo(right.RuntimeId);
            });
            if (targets.Count > current.TargetCap)
                targets.RemoveRange(current.TargetCap, targets.Count - current.TargetCap);
            return targets.Count;
        }

        private void TickEarthCurrentContacts(float step)
        {
            earthContactIds.Clear();
            foreach (var pair in earthCurrentContacts) earthContactIds.Add(pair.Key);
            foreach (var id in earthContactIds)
            {
                var contact = earthCurrentContacts[id];
                contact.Remaining -= step;
                if (contact.Remaining <= 0f) earthCurrentContacts.Remove(id);
                else earthCurrentContacts[id] = contact;
            }
        }

        private void OnDamageConfirmed(ConfirmedDamageEvent damage)
        {
            if (!Potentials.Legacy.Is(WeaponLegacyPathId.ThunderEarthCurrent) ||
                Potentials.Legacy.Stage != WeaponLegacyStage.Completed ||
                !earthCurrentContacts.TryGetValue(damage.TargetRuntimeId, out var contact) ||
                !contact.CanPropagate || contact.Remaining <= 0f ||
                !runtime.Targets.TryGet(damage.TargetRuntimeId, out var killed) || killed == null || killed.IsAlive)
                return;

            earthCurrentContacts.Remove(damage.TargetRuntimeId);
            propagationTargets.Clear();
            runtime.Targets.CopyTo(propagationTargets);
            propagationTargets.RemoveAll(target => target == null || !target.IsAlive ||
                target.RuntimeId == damage.TargetRuntimeId ||
                DistanceSquared(target.WorldPosition, damage.ContactPoint) > Range * Range);
            propagationTargets.Sort((left, right) =>
            {
                var distance = DistanceSquared(left.WorldPosition, damage.ContactPoint)
                    .CompareTo(DistanceSquared(right.WorldPosition, damage.ContactPoint));
                return distance != 0 ? distance : left.RuntimeId.CompareTo(right.RuntimeId);
            });
            if (propagationTargets.Count > 5)
                propagationTargets.RemoveRange(5, propagationTargets.Count - 5);
#if UNITY_INCLUDE_TESTS
            LastEarthPropagationCountForTests = propagationTargets.Count;
#endif
            foreach (var target in propagationTargets)
                groundCurrents.Add(new GroundCurrent(
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, .5f),
                    target.WorldPosition, contact.Remaining, 1, true, target.RuntimeId));
        }

        private struct DelayedPotentialStrike
        {
            public DelayedPotentialStrike(AttackInstance attack, int targetRuntimeId, Float2 position, PixelHitMask mask, float remaining, float multiplier, ContactPhase phase, bool trackTargetPosition = false)
            { Attack = attack; TargetRuntimeId = targetRuntimeId; Position = position; Mask = mask; Remaining = remaining; Multiplier = multiplier; Phase = phase; TrackTargetPosition = trackTargetPosition; }
            public AttackInstance Attack; public int TargetRuntimeId; public Float2 Position; public PixelHitMask Mask; public float Remaining; public float Multiplier; public ContactPhase Phase; public bool TrackTargetPosition;
        }

        private struct GroundCurrent
        {
            public GroundCurrent(AttackInstance attack, Float2 center, float remaining, int targetCap,
                bool isPropagation = false, int targetRuntimeId = 0)
            { Attack = attack; Center = center; Remaining = remaining; TargetCap = targetCap;
                IsPropagation = isPropagation; TargetRuntimeId = targetRuntimeId; TickElapsed = 0f;
                HitTime = 0f; }
            public AttackInstance Attack;
            public Float2 Center;
            public float Remaining;
            public int TargetCap;
            public bool IsPropagation;
            public int TargetRuntimeId;
            public float TickElapsed;
            public float HitTime;
        }

        private struct EarthCurrentContact
        {
            public EarthCurrentContact(float remaining, bool canPropagate)
            { Remaining = remaining; CanPropagate = canPropagate; }
            public float Remaining;
            public bool CanPropagate;
        }
    }
}
