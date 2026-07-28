using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
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
        private float cooldown;

        public ThunderBombExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float lobDuration, float fuseDuration, float blastRadius, int level, bool evolved = false)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            BaseDamage = Mathf.Max(1f, baseDamage); CooldownSeconds = Mathf.Max(0.01f, cooldownSeconds); Range = Mathf.Max(0.01f, range);
            LobDuration = Mathf.Max(0.01f, lobDuration); FuseDuration = Mathf.Max(0f, fuseDuration); BlastRadius = Mathf.Max(0.01f, blastRadius); Level = Mathf.Clamp(level, 1, 5);
            IsEvolved = evolved;
        }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float LobDuration { get; }
        public float FuseDuration { get; }
        public float BlastRadius { get; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public int ActiveBombCount => bombs.Count;
        public ThunderBombState LastState { get; private set; } = ThunderBombState.Complete;
        public Float2 LastLandingPosition { get; private set; }
        public IReadOnlyList<string> StateOrder => stateOrder;

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            var step = Mathf.Max(0f, deltaTime);
            cooldown -= step;
            if (cooldown <= 0f && bombs.Count < MaximumBombs && (!IsEvolved || bombs.Count == 0) && TryFindPredictedCrowd(context.OwnerPosition, out var landing))
            {
                cooldown = CooldownSeconds;
                if (IsEvolved) stateOrder.Clear();
                if (IsEvolved) landing = Lerp(context.OwnerPosition, landing, 0.5f);
                bombs.Add(new Bomb(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), context.OwnerPosition, landing));
                LastLandingPosition = landing;
            }

            for (var index = bombs.Count - 1; index >= 0; index--)
            {
                var bomb = bombs[index];
                Advance(bomb, step, context);
                LastState = bomb.State;
                if (bomb.State != ThunderBombState.Complete) continue;
                runtime.DamageService.RetireAttack(bomb.Attack.InstanceId);
                bombs.RemoveAt(index);
            }
        }

        public void Reset()
        {
            foreach (var bomb in bombs) runtime.DamageService.RetireAttack(bomb.Attack.InstanceId);
            bombs.Clear(); stateOrder.Clear(); cooldown = 0f; LastState = ThunderBombState.Complete; LastLandingPosition = default;
        }

        public void Dispose() => Reset();

        private void Advance(Bomb bomb, float step, in WeaponExecutionContext context)
        {
            if (IsEvolved)
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
                    bomb.Height = 4f * progress * (1f - progress) * Mathf.Min(0.75f, Range * 0.25f);
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
                        if (Level == 5) { bomb.State = ThunderBombState.SecondaryShockwave; bomb.Elapsed = 0f; }
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
                        bomb.Height = 4f * progress * (1f - progress) * Mathf.Min(0.75f, Range * 0.25f);
                        if (bomb.Elapsed >= LobDuration) Transition(bomb, ThunderBombState.Pull);
                        break;
                    case ThunderBombState.Pull:
                        var pullSlice = Mathf.Min(remaining, PullDuration - bomb.Elapsed);
                        PullTargets(bomb, pullSlice);
                        bomb.Elapsed += pullSlice;
                        remaining -= pullSlice;
                        if (bomb.Elapsed >= PullDuration) Transition(bomb, ThunderBombState.CompressionDelay);
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

        private void PullTargets(Bomb bomb, float step)
        {
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets)
            {
                if (!IsInRange(target, bomb.Landing, BlastRadius)) continue;
                var direction = new Float2(bomb.Landing.X - target.WorldPosition.X, bomb.Landing.Y - target.WorldPosition.Y);
                var distance = Mathf.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
                if (distance <= 0.0001f) continue;
                target.ApplyKnockback(new Float2(direction.X / distance, direction.Y / distance), Mathf.Min(distance, PullSpeed * step));
            }
        }

        private void ResolveCompressedBlast(Bomb bomb, in WeaponExecutionContext context)
        {
            runtime.Targets.CopyTo(targets);
            var transform = new PixelMaskTransform(bomb.Landing, 0, false, new Vector2(BlastRadius * 2f, BlastRadius * 2f));
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                if (!PixelMaskContactService.TryFindContact(compressedMask, transform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                runtime.DamageService.TryApply(WeaponDamageRequest.Create(bomb.Attack, WeaponId.ThunderCrashBomb, target, Mathf.CeilToInt(BaseDamage) * 2, false, contact, ContactPhase.Blast, context.SimulationTick), out _);
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
                runtime.DamageService.TryApply(WeaponDamageRequest.Create(bomb.Attack, WeaponId.ThunderCrashBomb, target, Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.Blast, context.SimulationTick), out _);
            }
            bomb.SweptRadius = end;
            return bomb.SweptRadius + 0.0001f >= desiredRadius;
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
            public ThunderBombState State { get; set; } = ThunderBombState.Lob;
        }
    }
}
