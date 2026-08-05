using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    /// <summary>Optional target capability so frost can affect runtime actors without expanding the universal damage target contract.</summary>
    public interface IFrostStatusTarget
    {
        void ApplyFrostSlow(int sourceId, float strength);
        void RemoveFrostSlow(int sourceId, float decaySeconds);
        void ApplyFreeze(int sourceId, float durationSeconds);
    }

    /// <summary>Bounded persistent frost fields with timed contact damage and independent master ice-spike attacks.</summary>
    public sealed class FrostFlaskExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        public const int MaximumFields = 4;
        private const float TickInterval = 0.5f;
        private const float FreezeResidence = 0.75f;
        private const float SlowDecaySeconds = 0.35f;
        private readonly WeaponRuntimeController runtime;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<Field> fields = new List<Field>();
        private readonly List<SpreadResidence> spreadResidences = new List<SpreadResidence>();
        private readonly List<FrostBloom> legacyBlooms = new List<FrostBloom>();
        private readonly PixelHitMask diskMask = CreateDiskMask();
        private readonly PixelHitMask spikeMask = CreateSpikeMask();
        private WeaponTransientVisualPool transientVisuals;
        private Transform transientVisualRoot;
        private float cooldown;

        public FrostFlaskExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float lobDuration, float duration, float radius, int fieldCapacity, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default, float slowFraction = .5f)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            LegacySourceDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage));
            var mist = modifiers.Legacy.Is(WeaponLegacyPathId.FrostMist);
            var shatter = modifiers.Legacy.Is(WeaponLegacyPathId.FrostShatter);
            BaseDamage = LegacySourceDamage * (mist ? .65f : 1f); CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds)); Range = Mathf.Max(0.01f, modifiers.ScaleArea(range));
            LobDuration = Mathf.Max(0.01f, modifiers.ScaleDuration(lobDuration)); Duration = Mathf.Max(0.01f, modifiers.ScaleDuration(duration) * (shatter ? .5f : 1f)); Radius = Mathf.Max(0.01f, modifiers.ScaleArea(radius) * (mist ? 1.35f : 1f)); FieldCapacity = Mathf.Clamp(fieldCapacity, 1, MaximumFields); Level = Mathf.Clamp(level, 1, 5); Potentials = modifiers;
            SlowFraction = Mathf.Clamp01(mist ? .45f : slowFraction);
            IsEvolved = evolved;
        }

        public float BaseDamage { get; }
        private float LegacySourceDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float LobDuration { get; }
        public float Duration { get; }
        public float Radius { get; }
        public int FieldCapacity { get; }
        public float SlowFraction { get; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public int ActiveFieldCount => fields.Count;
        public int ExpiredFieldCount { get; private set; }
        public int LastStoredFrozenTargetCount { get; private set; }
        public int LastResolvedStoredTargetCount { get; private set; }
        public bool AllStoredTargetsResolvedOnce { get; private set; }
        public float LastFieldVisualScale { get; private set; } = 1f;
#if UNITY_INCLUDE_TESTS
        public float LegacyLandingDamageForTests => Potentials.Legacy.Is(WeaponLegacyPathId.FrostShatter) ? LegacySourceDamage * 1.5f : BaseDamage * .2f;
        public int CompletedBloomCountForTests { get; private set; }
        public int LastLegacyShatterTargetCountForTests { get; private set; }
        public int FirstVisualPartIndexForTests => fields.Count == 0 ? -1 : fields[0].VisualPartIndex;
        public int ConfirmedStoredShatterVisualCountForTests { get; private set; }
        public Float2 LastConfirmedStoredShatterPositionForTests { get; private set; }
        public int ActiveSpreadResidenceCountForTests => spreadResidences.Count;
        public float FirstSpreadRemainingForTests => spreadResidences.Count == 0 ? 0f : spreadResidences[0].Remaining;
        public bool SuppressNewCastsForTests { get; set; }
#endif

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            var step = Mathf.Max(0f, deltaTime);
            EnsureTransientVisuals(context.PresentationRoot);
            transientVisuals?.Tick(step);
            cooldown -= step;
            AdvanceSpreadResidences(step);
            AdvanceLegacyBlooms(step, context);
            if (cooldown <= 0f
#if UNITY_INCLUDE_TESTS
                && !SuppressNewCastsForTests
#endif
                && TryFindCrowd(context.OwnerPosition, out var landing))
            {
                cooldown = CooldownSeconds;
                if (fields.Count >= FieldCapacity)
                {
                    // Existing spreads were advanced before this new eviction; a spread born
                    // from the evicted field must consume this frame exactly once here.
                    Expire(fields[0], context, step);
                    fields.RemoveAt(0);
                }
                var field = new Field(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, TickInterval), context.OwnerPosition, landing);
                CreateVisual(field, context);
                fields.Add(field);
            }
            for (var index = fields.Count - 1; index >= 0; index--)
            {
                var field = fields[index]; Advance(field, step, context);
                if (field.Expired) { fields.RemoveAt(index); continue; }
            }
        }

        public void Reset()
        {
            // Reset is a terminal cleanup path too: every live field must release only its own status source.
            foreach (var field in fields) { CleanupFieldStatus(field); Retire(field); }
            foreach (var spread in spreadResidences) if (runtime.Targets.TryGet(spread.TargetId, out var target) && target is IFrostStatusTarget status) status.RemoveFrostSlow(spread.SourceId, SlowDecaySeconds);
            spreadResidences.Clear();
            foreach (var bloom in legacyBlooms) runtime.DamageService.RetireAttack(bloom.Attack.InstanceId);
            legacyBlooms.Clear();
            fields.Clear(); cooldown = 0f; ExpiredFieldCount = 0;
            LastStoredFrozenTargetCount = 0; LastResolvedStoredTargetCount = 0; AllStoredTargetsResolvedOnce = false;
            LastFieldVisualScale = 1f;
#if UNITY_INCLUDE_TESTS
            CompletedBloomCountForTests = 0;
            LastLegacyShatterTargetCountForTests = 0;
            ConfirmedStoredShatterVisualCountForTests = 0;
            LastConfirmedStoredShatterPositionForTests = default;
#endif
            transientVisuals?.Dispose(); transientVisuals = null; transientVisualRoot = null;
        }

        public void Dispose() => Reset();

        private void Advance(Field field, float step, in WeaponExecutionContext context)
        {
            field.Age += step;
            if (!field.Active)
            {
                var progress = Mathf.Clamp01(field.Age / LobDuration);
                field.Position = Lerp(field.Start, field.Landing, progress);
                field.Height = 4f * progress * (1f - progress) * Mathf.Min(0.6f, Range * 0.2f);
                UpdateFlightVisual(field, context, progress);
                if (progress >= 1f)
                {
                    field.Active = true; field.Age = 0f;
                    LastFieldVisualScale = .65f;
                    UpdateFieldVisual(field, context, .65f);
                    ResolveLandingBurst(field, context);
                    if (Potentials.Legacy.Is(WeaponLegacyPathId.FrostMist) && Potentials.Legacy.Stage == WeaponLegacyStage.Completed)
                        ScheduleLegacyBlooms(field.Landing);
                    PlayLandingFragments(field, context);
                }
                return;
            }
            var activeStep = Mathf.Min(step, Mathf.Max(0f, Duration - field.ActiveAge));
            field.ActiveAge += activeStep;
            runtime.Targets.CopyTo(targets);
            var mistMask = diskMask;
            var visualScale = Mathf.Lerp(.65f, 1f, Mathf.Clamp01(field.ActiveAge / .18f));
            var radiusScale = 1f;
            if (Potentials.HasPotential(WeaponPotentialId.FrostMist) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FrostMist, out _, out var authoredMistMask))
            {
                mistMask = authoredMistMask;
                radiusScale *= Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(field.ActiveAge / Duration));
            }
            LastFieldVisualScale = visualScale * radiusScale;
            UpdateFieldVisual(field, context, LastFieldVisualScale);
            var transform = new PixelMaskTransform(field.Landing, 0, false, new Vector2(Radius * 2f * radiusScale, Radius * 2f * radiusScale));
            var inside = field.InsideScratch;
            inside.Clear();
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                if (!PixelMaskContactService.TryFindContact(mistMask, transform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                inside.Add(target.RuntimeId);
                field.Residence.TryGetValue(target.RuntimeId, out var residence); residence += activeStep; field.Residence[target.RuntimeId] = residence;
                if (Potentials.HasPotential(WeaponPotentialId.FrostCrackMark) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FrostCrackMark, out _, out var crackMask) &&
                    PixelMaskContactService.TryFindContact(crackMask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _))
                {
                    field.CrackElapsed.TryGetValue(target.RuntimeId, out var crackElapsed); crackElapsed += activeStep;
                    while (crackElapsed + .00001f >= .5f) { crackElapsed -= .5f; field.CrackStacks.TryGetValue(target.RuntimeId, out var stacks); field.CrackStacks[target.RuntimeId] = Mathf.Min(3, stacks + 1); }
                    field.CrackElapsed[target.RuntimeId] = crackElapsed;
                }
                if (target is IFrostStatusTarget status)
                {
                    status.ApplyFrostSlow(field.Attack.InstanceId, SlowFraction);
                    if (residence >= FreezeResidence && field.Frozen.Add(target.RuntimeId))
                    {
                        status.ApplyFreeze(field.Attack.InstanceId, 0.3f);
                        if (IsEvolved) field.StoredFrozen.Add(target.RuntimeId);
                    }
                }
                if (field.ActiveAge + 0.0001f >= field.NextDamageAge &&
                    runtime.DamageService.TryApply(WeaponDamageRequest.Create(field.Attack, WeaponId.FrostFlask, target, Mathf.CeilToInt(BaseDamage * .2f), false, contact, ContactPhase.Tick, context.SimulationTick,
                        traits: Potentials.Legacy.Is(WeaponLegacyPathId.FrostShatter) ? WeaponHitTrait.None : WeaponHitTrait.Explosion,
                        attackOrigin: field.Landing), out _))
                    RecordLegacyFrostHit(target);
            }
            foreach (var previous in field.Inside)
                if (!inside.Contains(previous) && runtime.Targets.TryGet(previous, out var target) && target is IFrostStatusTarget status) status.RemoveFrostSlow(field.Attack.InstanceId, SlowDecaySeconds);
            field.Inside.Clear(); foreach (var id in inside) field.Inside.Add(id);
            if (field.ActiveAge + 0.0001f >= field.NextDamageAge) field.NextDamageAge += TickInterval;
            if (field.ActiveAge + .00001f >= Duration) Expire(field, context, step - activeStep);
        }

        private void RaiseSpike(Field field, in WeaponExecutionContext context, bool expirySpike)
        {
            var spike = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
            runtime.Targets.CopyTo(targets);
            var transform = new PixelMaskTransform(field.Landing, 0, false, new Vector2(Radius * 2f, Radius * 2f));
            var hitConfirmed = false;
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                if (!PixelMaskContactService.TryFindContact(spikeMask, transform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                var damage = BaseDamage;
                if (Potentials.HasPotential(WeaponPotentialId.FrostCrackMark) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FrostCrackMark, out _, out var crackMask) &&
                    PixelMaskContactService.TryFindContact(crackMask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _))
                {
                    field.CrackStacks.TryGetValue(target.RuntimeId, out var stacks); damage *= 1f + stacks * .25f; field.CrackStacks.Remove(target.RuntimeId); field.CrackElapsed.Remove(target.RuntimeId);
                }
                var hit = runtime.DamageService.TryApply(WeaponDamageRequest.Create(spike, WeaponId.FrostFlask, target, Mathf.CeilToInt(damage), false, contact, ContactPhase.Blast, context.SimulationTick), out _);
                hitConfirmed |= hit;
                if (hit && expirySpike && Potentials.HasPotential(WeaponPotentialId.FrostSpread) && !field.SpreadResolved && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FrostSpread, out _, out var spreadMask))
                {
                    field.SpreadResolved = true;
                    foreach (var other in targets)
                    {
                        if (other == null || !other.IsAlive || other.HurtMask == null || other.RuntimeId == target.RuntimeId) continue;
                        var dx = other.WorldPosition.X - target.WorldPosition.X; var dy = other.WorldPosition.Y - target.WorldPosition.Y;
                        if (dx * dx + dy * dy > 2.25f || !PixelMaskContactService.TryFindContact(spreadMask, PixelMaskTransform.Translation(other.WorldPosition.X, other.WorldPosition.Y), other.HurtMask, other.HurtMaskTransform, out _)) continue;
                        var spreadSource = runtime.AllocateAttackInstanceId();
                        var remaining = Mathf.Max(0f, .25f - field.ExpiryResidual);
                        if (remaining > 0f) { spreadResidences.Add(new SpreadResidence(other.RuntimeId, spreadSource, remaining)); if (other is IFrostStatusTarget status) status.ApplyFrostSlow(spreadSource, .5f); }
                    }
                }
            }
            if (hitConfirmed) PlayConfirmedShatter(field, context);
            runtime.DamageService.RetireAttack(spike.InstanceId);
        }

        private void ResolveLandingBurst(Field field, in WeaponExecutionContext context)
        {
            if (Potentials.Legacy.Is(WeaponLegacyPathId.FrostShatter))
                ResolveLegacyShatter(field.Landing, context);
            var burst = new AttackInstance(
                runtime.AllocateAttackInstanceId(),
                RepeatHitPolicy.OncePerInstance,
                0f);
            runtime.Targets.CopyTo(targets);
            var transform = new PixelMaskTransform(
                field.Landing,
                0,
                false,
                new Vector2(Radius * 2f, Radius * 2f));
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                if (!PixelMaskContactService.TryFindContact(
                        diskMask,
                        transform,
                        target.HurtMask,
                        target.HurtMaskTransform,
                        out var contact)) continue;
                if (runtime.DamageService.TryApply(
                    WeaponDamageRequest.Create(
                        burst,
                        WeaponId.FrostFlask,
                        target,
                        Mathf.CeilToInt(Potentials.Legacy.Is(WeaponLegacyPathId.FrostShatter) ? LegacySourceDamage * 1.5f : BaseDamage * .2f),
                        false,
                        contact,
                        ContactPhase.Blast,
                        context.SimulationTick,
                        traits: Potentials.Legacy.Is(WeaponLegacyPathId.FrostShatter) ? WeaponHitTrait.None : WeaponHitTrait.Explosion,
                        attackOrigin: field.Landing),
                    out _)) RecordLegacyFrostHit(target);
            }
            runtime.DamageService.RetireAttack(burst.InstanceId);
        }

        private void RecordLegacyFrostHit(ICombatTarget target)
        {
            if (!Potentials.Legacy.Is(WeaponLegacyPathId.FrostMist) || target == null) return;
            var frozen = runtime.AffixStatuses.RecordFrostContact(target.RuntimeId, WeaponId.FrostFlask);
            if (frozen && Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced)
                runtime.AffixStatuses.ApplyFrostVulnerability(target.RuntimeId, 2f);
        }

        private void ScheduleLegacyBlooms(Float2 center)
        {
            for (var index = 0; index < 3; index++)
                legacyBlooms.Add(new FrostBloom(center, .15f + index * .4f,
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f)));
        }

        private void AdvanceLegacyBlooms(float step, in WeaponExecutionContext context)
        {
            for (var index = legacyBlooms.Count - 1; index >= 0; index--)
            {
                var bloom = legacyBlooms[index];
                bloom.Remaining -= step;
                if (bloom.Remaining > .00001f) { legacyBlooms[index] = bloom; continue; }
                runtime.Targets.CopyTo(targets);
                foreach (var target in targets)
                {
                    if (target == null || !target.IsAlive || DistanceSquared(bloom.Center, target.WorldPosition) > Radius * Radius) continue;
                    var contact = target.WorldPosition;
                    if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(bloom.Attack, WeaponId.FrostFlask, target,
                        Mathf.RoundToInt(LegacySourceDamage * .6f), false, contact, ContactPhase.PotentialBlast,
                        context.SimulationTick, traits: WeaponHitTrait.Explosion, attackOrigin: bloom.Center), out _))
                        RecordLegacyFrostHit(target);
                }
                runtime.DamageService.RetireAttack(bloom.Attack.InstanceId);
                legacyBlooms.RemoveAt(index);
#if UNITY_INCLUDE_TESTS
                CompletedBloomCountForTests++;
#endif
            }
        }

        private void ResolveLegacyShatter(Float2 center, in WeaponExecutionContext context)
        {
            runtime.Targets.CopyTo(targets);
            targets.RemoveAll(target => target == null || !target.IsAlive ||
                DistanceSquared(center, target.WorldPosition) > Radius * Radius ||
                !runtime.AffixStatuses.HasStatus(target.RuntimeId, CombatStatusKind.Freeze));
            targets.Sort((left, right) =>
            {
                var compared = DistanceSquared(center, left.WorldPosition).CompareTo(DistanceSquared(center, right.WorldPosition));
                return compared != 0 ? compared : left.RuntimeId.CompareTo(right.RuntimeId);
            });
            var cap = Potentials.Legacy.Stage == WeaponLegacyStage.Completed ? 5 :
                Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced ? 3 : 1;
            var resolved = 0;
            for (var index = 0; index < targets.Count && resolved < cap; index++)
            {
                var target = targets[index];
                if (!runtime.AffixStatuses.TryConsumeStatus(target.RuntimeId, CombatStatusKind.Freeze)) continue;
                var shatter = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
                runtime.DamageService.TryApply(WeaponDamageRequest.Create(shatter, WeaponId.FrostFlask, target,
                    Mathf.CeilToInt(LegacySourceDamage * 1.8f), false, target.WorldPosition, ContactPhase.PotentialChain,
                    context.SimulationTick, traits: WeaponHitTrait.Explosion, attackOrigin: center), out _);
                runtime.DamageService.RetireAttack(shatter.InstanceId);
                resolved++;
            }
#if UNITY_INCLUDE_TESTS
            LastLegacyShatterTargetCountForTests = resolved;
#endif
        }

        private static float DistanceSquared(Float2 left, Float2 right)
        {
            var x = left.X - right.X; var y = left.Y - right.Y; return x * x + y * y;
        }

        private bool TryFindCrowd(Float2 origin, out Float2 landing)
        {
            runtime.Targets.CopyTo(targets); var x = 0f; var y = 0f; var count = 0;
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;
                var dx = target.WorldPosition.X - origin.X; var dy = target.WorldPosition.Y - origin.Y;
                if (dx * dx + dy * dy > Range * Range) continue;
                x += target.WorldPosition.X; y += target.WorldPosition.Y; count++;
            }
            landing = count == 0 ? default : new Float2(x / count, y / count);
            return count > 0;
        }

        private void Expire(Field field, in WeaponExecutionContext context, float postExpiryResidual = 0f)
        {
            if (field.Expired) return;
            field.ExpiryResidual = Mathf.Max(0f, postExpiryResidual);
            if (Potentials.HasPotential(WeaponPotentialId.FrostSpread) || Potentials.HasPotential(WeaponPotentialId.FrostCrackMark)) RaiseSpike(field, context, true);
            if (IsEvolved) ResolveStoredFrozenTargets(field, context);
            CleanupFieldStatus(field);
            Retire(field); field.Expired = true; ExpiredFieldCount++;
        }

        private void ResolveStoredFrozenTargets(Field field, in WeaponExecutionContext context)
        {
            LastStoredFrozenTargetCount = field.StoredFrozen.Count;
            LastResolvedStoredTargetCount = 0;
            foreach (var targetId in field.StoredFrozen)
            {
                if (!runtime.Targets.TryGet(targetId, out var target) || target == null || !target.IsAlive || target.HurtMask == null) continue;

                // Each stored identity gets a separate, short-lived spike. Contact is checked at
                // that spike's location; storage alone never authorizes damage.
                var spike = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
                var transform = PixelMaskTransform.Translation(target.WorldPosition.X, target.WorldPosition.Y);
                if (PixelMaskContactService.TryFindContact(spikeMask, transform, target.HurtMask, target.HurtMaskTransform, out var contact))
                {
                    var damage = BaseDamage;
                    if (Potentials.HasPotential(WeaponPotentialId.FrostCrackMark) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FrostCrackMark, out _, out var crackMask) &&
                        PixelMaskContactService.TryFindContact(crackMask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _))
                    { field.CrackStacks.TryGetValue(target.RuntimeId, out var stacks); damage *= 1f + stacks * .25f; field.CrackStacks.Remove(target.RuntimeId); }
                    if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(spike, WeaponId.FrostFlask, target, Mathf.CeilToInt(damage), false, contact, ContactPhase.Blast, context.SimulationTick), out _))
                    {
                        LastResolvedStoredTargetCount++;
                        PlayConfirmedStoredShatter(context, target.WorldPosition);
                    }
                }
                runtime.DamageService.RetireAttack(spike.InstanceId);
            }
            AllStoredTargetsResolvedOnce = LastResolvedStoredTargetCount == LastStoredFrozenTargetCount;
        }

        private void CleanupFieldStatus(Field field)
        {
            if (field == null || field.Expired) return;
            foreach (var id in field.Inside)
                if (runtime.Targets.TryGet(id, out var target) && target is IFrostStatusTarget status) status.RemoveFrostSlow(field.Attack.InstanceId, SlowDecaySeconds);
        }

        private void Retire(Field field)
        {
            if (field == null || field.AttackRetired) return;
            runtime.DamageService.RetireAttack(field.Attack.InstanceId);
            if (field.Visual != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(field.Visual);
                else UnityEngine.Object.DestroyImmediate(field.Visual);
            }
            field.AttackRetired = true;
        }

        private void CreateVisual(Field field, in WeaponExecutionContext context)
        {
            if (context.PresentationRoot == null) return;
            field.Visual = new GameObject("Frost Flask");
            field.Visual.transform.SetParent(context.PresentationRoot, false);
            var renderer = field.Visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = context.SortingOrder + 1;
            UpdateFlightVisual(field, context, 0f);
        }

        private static int FrameFromProgress(int start, int count, float progress) =>
            start + Mathf.Min(count - 1, Mathf.FloorToInt(Mathf.Clamp01(progress) * count));

        private void UpdateFlightVisual(Field field, in WeaponExecutionContext context, float progress)
        {
            if (field.Visual == null) return;
            field.VisualPartIndex = FrameFromProgress(
                WeaponVisualPartIndex.FrostFlask.Projectile,
                WeaponVisualPartIndex.FrostFlask.ProjectileFrameCount,
                progress);
            var renderer = field.Visual.GetComponent<SpriteRenderer>();
            renderer.sprite = context.PresentationSpriteFor(WeaponId.FrostFlask, field.VisualPartIndex);
            field.Visual.transform.position = new Vector3(field.Position.X, field.Position.Y + field.Height, 0f);
            field.Visual.transform.localScale = Vector3.one * WeaponPresentationScale.For(
                WeaponId.FrostFlask,
                WeaponVisualStage.Projectile,
                1f,
                Level,
                IsEvolved);
        }

        private void UpdateFieldVisual(Field field, in WeaponExecutionContext context, float radiusScale)
        {
            if (field.Visual == null) return;
            field.VisualPartIndex = WeaponVisualPartIndex.FrostFlask.Impact + 3 +
                (Mathf.FloorToInt(field.ActiveAge * 4f) & 1);
            var renderer = field.Visual.GetComponent<SpriteRenderer>();
            renderer.sprite = context.PresentationSpriteFor(WeaponId.FrostFlask, field.VisualPartIndex);
            field.Visual.transform.position = new Vector3(field.Landing.X, field.Landing.Y, 0f);
            var scale = ScaleSpriteToWorldDiameter(renderer.sprite, Radius * 2f * radiusScale);
            scale.y *= .58f;
            field.Visual.transform.localScale = scale;
            renderer.color = new Color(.62f, .92f, 1f, .46f);
        }

        private void PlayLandingFragments(Field field, in WeaponExecutionContext context)
        {
            var cue = new WeaponVisualCue(WeaponId.FrostFlask, WeaponVisualStage.Impact, Level, IsEvolved, .72f, .12f);
            var impactSprite = context.PresentationSpriteFor(
                WeaponId.FrostFlask,
                WeaponVisualPartIndex.FrostFlask.Impact);
            var impactScale = ScaleSpriteToWorldDiameter(impactSprite, cue.ResolvedScale);
            transientVisuals?.Play(
                impactSprite,
                new Vector3(field.Landing.X, field.Landing.Y, 0f), Quaternion.identity,
                impactScale,
                new Color(.72f, 1f, 1f, .94f), cue.ResolvedLifetime, context.SortingOrder + 2);
            transientVisuals?.Play(
                impactSprite,
                new Vector3(field.Landing.X - Radius * .28f, field.Landing.Y + Radius * .12f, 0f),
                Quaternion.Euler(0f, 0f, -28f),
                impactScale * .48f,
                new Color(.34f, .88f, 1f, .78f), cue.ResolvedLifetime, context.SortingOrder + 3);
            transientVisuals?.Play(
                impactSprite,
                new Vector3(field.Landing.X + Radius * .26f, field.Landing.Y + Radius * .16f, 0f),
                Quaternion.Euler(0f, 0f, 34f),
                impactScale * .44f,
                new Color(.34f, .88f, 1f, .74f), cue.ResolvedLifetime, context.SortingOrder + 3);
        }

        private void PlayConfirmedShatter(Field field, in WeaponExecutionContext context)
        {
            var cue = new WeaponVisualCue(WeaponId.FrostFlask, WeaponVisualStage.Detonation, Level, IsEvolved, Radius, .16f);
            transientVisuals?.Play(
                context.PresentationSpriteFor(
                    WeaponId.FrostFlask,
                    WeaponVisualPartIndex.FrostFlask.Impact + WeaponVisualPartIndex.FrostFlask.ImpactFrameCount - 1),
                new Vector3(field.Landing.X, field.Landing.Y, 0f), Quaternion.identity,
                ScaleSpriteToWorldDiameter(
                    context.PresentationSpriteFor(
                        WeaponId.FrostFlask,
                        WeaponVisualPartIndex.FrostFlask.Impact + WeaponVisualPartIndex.FrostFlask.ImpactFrameCount - 1),
                    cue.ResolvedScale),
                new Color(1f, 1f, 1f, .72f), cue.ResolvedLifetime, context.SortingOrder + 3);
        }

        private void PlayConfirmedStoredShatter(in WeaponExecutionContext context, Float2 position)
        {
#if UNITY_INCLUDE_TESTS
            ConfirmedStoredShatterVisualCountForTests++;
            LastConfirmedStoredShatterPositionForTests = position;
#endif
            var cue = new WeaponVisualCue(WeaponId.FrostFlask, WeaponVisualStage.Detonation, Level, IsEvolved, .9f, .16f);
            transientVisuals?.Play(
                context.PresentationSpriteFor(
                    WeaponId.FrostFlask,
                    WeaponVisualPartIndex.FrostFlask.Impact + WeaponVisualPartIndex.FrostFlask.ImpactFrameCount - 1),
                new Vector3(position.X, position.Y, 0f), Quaternion.identity,
                ScaleSpriteToWorldDiameter(
                    context.PresentationSpriteFor(
                        WeaponId.FrostFlask,
                        WeaponVisualPartIndex.FrostFlask.Impact + WeaponVisualPartIndex.FrostFlask.ImpactFrameCount - 1),
                    cue.ResolvedScale),
                new Color(1f, 1f, 1f, .72f), cue.ResolvedLifetime, context.SortingOrder + 3);
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
        private void AdvanceSpreadResidences(float step)
        {
            for (var index = spreadResidences.Count - 1; index >= 0; index--)
            {
                var spread = spreadResidences[index]; spread.Remaining -= step;
                if (spread.Remaining > 0f) { spreadResidences[index] = spread; continue; }
                if (runtime.Targets.TryGet(spread.TargetId, out var target) && target is IFrostStatusTarget status) status.RemoveFrostSlow(spread.SourceId, SlowDecaySeconds);
                spreadResidences.RemoveAt(index);
            }
        }
        private static Float2 Lerp(Float2 left, Float2 right, float progress) => new Float2(Mathf.Lerp(left.X, right.X, progress), Mathf.Lerp(left.Y, right.Y, progress));
        private static PixelHitMask CreateDiskMask() => CreateCircleMask(17, 64);
        private static PixelHitMask CreateSpikeMask() => CreateCircleMask(9, 16);
        private static PixelHitMask CreateCircleMask(int size, int radiusSquared)
        {
            var center = size / 2; var packed = new uint[(size * size + 31) / 32];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var dx = x - center; var dy = y - center; if (dx * dx + dy * dy > radiusSquared) continue;
                var bit = y * size + x; packed[bit >> 5] |= 1u << (bit & 31);
            }
            return new PixelHitMask(size, size, new Vector2(center, center), 16f, packed);
        }

        private sealed class Field
        {
            public Field(AttackInstance attack, Float2 start, Float2 landing) { Attack = attack; Start = start; Landing = landing; Position = start; }
            public AttackInstance Attack { get; }
            public Float2 Start { get; }
            public Float2 Landing { get; }
            public Float2 Position { get; set; }
            public float Height { get; set; }
            public float Age { get; set; }
            public float ActiveAge { get; set; }
            public float SpikeTimer { get; set; }
            public float NextDamageAge { get; set; } = TickInterval;
            public bool Active { get; set; }
            public bool Expired { get; set; }
            public bool AttackRetired { get; set; }
            public Dictionary<int, float> Residence { get; } = new Dictionary<int, float>();
            public HashSet<int> Frozen { get; } = new HashSet<int>();
            public HashSet<int> StoredFrozen { get; } = new HashSet<int>();
            public HashSet<int> Inside { get; } = new HashSet<int>();
            public HashSet<int> InsideScratch { get; } = new HashSet<int>();
            public Dictionary<int, float> CrackElapsed { get; } = new Dictionary<int, float>();
            public Dictionary<int, int> CrackStacks { get; } = new Dictionary<int, int>();
            public bool SpreadResolved { get; set; }
            public float ExpiryResidual { get; set; }
            public GameObject Visual { get; set; }
            public int VisualPartIndex { get; set; } = WeaponVisualPartIndex.FrostFlask.Projectile;
        }
        private struct SpreadResidence { public SpreadResidence(int targetId, int sourceId, float remaining) { TargetId = targetId; SourceId = sourceId; Remaining = remaining; } public int TargetId; public int SourceId; public float Remaining; }
        private struct FrostBloom { public FrostBloom(Float2 center, float remaining, AttackInstance attack) { Center = center; Remaining = remaining; Attack = attack; } public Float2 Center; public float Remaining; public AttackInstance Attack; }
    }
}
