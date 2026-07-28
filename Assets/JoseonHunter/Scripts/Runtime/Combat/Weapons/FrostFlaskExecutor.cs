using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
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
        private const float TickInterval = 0.25f;
        private const float FreezeResidence = 0.75f;
        private const float SlowDecaySeconds = 0.35f;
        private readonly WeaponRuntimeController runtime;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<Field> fields = new List<Field>();
        private readonly List<SpreadResidence> spreadResidences = new List<SpreadResidence>();
        private readonly PixelHitMask diskMask = CreateDiskMask();
        private readonly PixelHitMask spikeMask = CreateSpikeMask();
        private float cooldown;

        public FrostFlaskExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float lobDuration, float duration, float radius, int fieldCapacity, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            BaseDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage)); CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds)); Range = Mathf.Max(0.01f, modifiers.ScaleArea(range));
            LobDuration = Mathf.Max(0.01f, modifiers.ScaleDuration(lobDuration)); Duration = Mathf.Max(0.01f, modifiers.ScaleDuration(duration)); Radius = Mathf.Max(0.01f, modifiers.ScaleArea(radius)); FieldCapacity = Mathf.Clamp(fieldCapacity, 1, MaximumFields); Level = Mathf.Clamp(level, 1, 5); Potentials = modifiers;
            IsEvolved = evolved;
        }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float LobDuration { get; }
        public float Duration { get; }
        public float Radius { get; }
        public int FieldCapacity { get; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public int ActiveFieldCount => fields.Count;
        public int ExpiredFieldCount { get; private set; }
        public int LastStoredFrozenTargetCount { get; private set; }
        public int LastResolvedStoredTargetCount { get; private set; }
        public bool AllStoredTargetsResolvedOnce { get; private set; }
        public float LastFieldVisualScale { get; private set; } = 1f;

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            var step = Mathf.Max(0f, deltaTime); cooldown -= step;
            if (cooldown <= 0f && TryFindCrowd(context.OwnerPosition, out var landing))
            {
                cooldown = CooldownSeconds;
                if (fields.Count >= FieldCapacity)
                {
                    Expire(fields[0], context);
                    fields.RemoveAt(0);
                }
                fields.Add(new Field(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, TickInterval), context.OwnerPosition, landing));
            }
            for (var index = fields.Count - 1; index >= 0; index--)
            {
                var field = fields[index]; Advance(field, step, context);
                if (field.Expired) { fields.RemoveAt(index); continue; }
            }
            AdvanceSpreadResidences(step);
        }

        public void Reset()
        {
            // Reset is a terminal cleanup path too: every live field must release only its own status source.
            foreach (var field in fields) { CleanupFieldStatus(field); Retire(field); }
            foreach (var spread in spreadResidences) if (runtime.Targets.TryGet(spread.TargetId, out var target) && target is IFrostStatusTarget status) status.RemoveFrostSlow(spread.SourceId, SlowDecaySeconds);
            spreadResidences.Clear();
            fields.Clear(); cooldown = 0f; ExpiredFieldCount = 0;
            LastStoredFrozenTargetCount = 0; LastResolvedStoredTargetCount = 0; AllStoredTargetsResolvedOnce = false;
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
                if (progress >= 1f) { field.Active = true; field.Age = 0f; }
                return;
            }
            field.ActiveAge += step;
            if (field.ActiveAge >= Duration) { Expire(field, context); return; }
            runtime.Targets.CopyTo(targets);
            var mistMask = diskMask;
            var radiusScale = 1f;
            if (Potentials.HasPotential(WeaponPotentialId.FrostMist) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FrostMist, out _, out var authoredMistMask)) { mistMask = authoredMistMask; radiusScale = Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(field.ActiveAge / Duration)); }
            LastFieldVisualScale = radiusScale;
            if (Potentials.HasPotential(WeaponPotentialId.FrostMist) && field.Visual == null && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FrostMist, out var mistSprite, out _))
            { field.Visual = new GameObject("Frost Mist Field"); field.Visual.transform.SetParent(context.PresentationRoot, false); field.Visual.AddComponent<SpriteRenderer>().sprite = mistSprite; }
            if (field.Visual != null) { field.Visual.transform.position = new Vector3(field.Landing.X, field.Landing.Y, 0f); field.Visual.transform.localScale = Vector3.one * Radius * 2f * radiusScale; }
            var transform = new PixelMaskTransform(field.Landing, 0, false, new Vector2(Radius * 2f * radiusScale, Radius * 2f * radiusScale));
            var inside = field.InsideScratch;
            inside.Clear();
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                if (!PixelMaskContactService.TryFindContact(mistMask, transform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                inside.Add(target.RuntimeId);
                field.Residence.TryGetValue(target.RuntimeId, out var residence); residence += step; field.Residence[target.RuntimeId] = residence;
                if (Potentials.HasPotential(WeaponPotentialId.FrostCrackMark) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FrostCrackMark, out _, out var crackMask) &&
                    PixelMaskContactService.TryFindContact(crackMask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _))
                {
                    field.CrackElapsed.TryGetValue(target.RuntimeId, out var crackElapsed); crackElapsed += step;
                    while (crackElapsed + .00001f >= .5f) { crackElapsed -= .5f; field.CrackStacks.TryGetValue(target.RuntimeId, out var stacks); field.CrackStacks[target.RuntimeId] = Mathf.Min(3, stacks + 1); }
                    field.CrackElapsed[target.RuntimeId] = crackElapsed;
                }
                if (target is IFrostStatusTarget status)
                {
                    status.ApplyFrostSlow(field.Attack.InstanceId, 0.5f);
                    if (residence >= FreezeResidence && field.Frozen.Add(target.RuntimeId))
                    {
                        status.ApplyFreeze(field.Attack.InstanceId, 0.2f);
                        if (IsEvolved) field.StoredFrozen.Add(target.RuntimeId);
                    }
                }
                if (field.ActiveAge + 0.0001f >= field.NextDamageAge)
                    runtime.DamageService.TryApply(WeaponDamageRequest.Create(field.Attack, WeaponId.FrostFlask, target, Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.Tick, context.SimulationTick), out _);
            }
            foreach (var previous in field.Inside)
                if (!inside.Contains(previous) && runtime.Targets.TryGet(previous, out var target) && target is IFrostStatusTarget status) status.RemoveFrostSlow(field.Attack.InstanceId, SlowDecaySeconds);
            field.Inside.Clear(); foreach (var id in inside) field.Inside.Add(id);
            if (field.ActiveAge + 0.0001f >= field.NextDamageAge) field.NextDamageAge += TickInterval;
            if (!IsEvolved && Level == 5)
            {
                field.SpikeTimer += step;
                while (field.SpikeTimer >= 0.5f) { field.SpikeTimer -= 0.5f; RaiseSpike(field, context, false); }
            }
        }

        private void RaiseSpike(Field field, in WeaponExecutionContext context, bool expirySpike)
        {
            var spike = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
            runtime.Targets.CopyTo(targets);
            var transform = new PixelMaskTransform(field.Landing, 0, false, new Vector2(Radius * 2f, Radius * 2f));
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
                if (hit && expirySpike && Potentials.HasPotential(WeaponPotentialId.FrostSpread) && !field.SpreadResolved && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FrostSpread, out _, out var spreadMask))
                {
                    field.SpreadResolved = true;
                    foreach (var other in targets)
                    {
                        if (other == null || !other.IsAlive || other.HurtMask == null || other.RuntimeId == target.RuntimeId) continue;
                        var dx = other.WorldPosition.X - target.WorldPosition.X; var dy = other.WorldPosition.Y - target.WorldPosition.Y;
                        if (dx * dx + dy * dy > 2.25f || !PixelMaskContactService.TryFindContact(spreadMask, PixelMaskTransform.Translation(other.WorldPosition.X, other.WorldPosition.Y), other.HurtMask, other.HurtMaskTransform, out _)) continue;
                        spreadResidences.Add(new SpreadResidence(other.RuntimeId, field.Attack.InstanceId, .25f));
                        if (other is IFrostStatusTarget status) status.ApplyFrostSlow(field.Attack.InstanceId, .5f);
                    }
                }
            }
            runtime.DamageService.RetireAttack(spike.InstanceId);
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

        private void Expire(Field field, in WeaponExecutionContext context)
        {
            if (field.Expired) return;
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
                    if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(spike, WeaponId.FrostFlask, target, Mathf.CeilToInt(damage), false, contact, ContactPhase.Blast, context.SimulationTick), out _)) LastResolvedStoredTargetCount++;
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
            if (field.Visual != null) UnityEngine.Object.Destroy(field.Visual);
            field.AttackRetired = true;
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
            public float NextDamageAge { get; set; }
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
            public GameObject Visual { get; set; }
        }
        private struct SpreadResidence { public SpreadResidence(int targetId, int sourceId, float remaining) { TargetId = targetId; SourceId = sourceId; Remaining = remaining; } public int TargetId; public int SourceId; public float Remaining; }
    }
}
