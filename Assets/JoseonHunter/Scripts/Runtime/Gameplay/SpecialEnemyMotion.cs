using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public enum SpecialEnemyMotionPhase { Chase, Telegraph, Dash }

    public struct SpecialEnemyMotionState
    {
        public SpecialEnemyMotionPhase Phase;
        public float Remaining;
        public float AuraElapsed;
        public Vector2 LockedDirection;
        public bool SplitResolved;
    }

    public readonly struct SpecialEnemyMotionResult
    {
        public SpecialEnemyMotionResult(Vector2 velocity, bool isTelegraphing = false, bool wasInterrupted = false,
            bool auraPulse = false, int splitChildren = 0, bool fallbackBlast = false)
        {
            Velocity = velocity; IsTelegraphing = isTelegraphing; WasInterrupted = wasInterrupted;
            AuraPulse = auraPulse; SplitChildren = splitChildren; FallbackBlast = fallbackBlast;
        }

        public Vector2 Velocity { get; }
        public bool IsTelegraphing { get; }
        public bool WasInterrupted { get; }
        public bool AuraPulse { get; }
        public int SplitChildren { get; }
        public bool FallbackBlast { get; }
    }

    /// <summary>Allocation-free special behavior state machine; movement ownership stays in the controller.</summary>
    public static class SpecialEnemyMotion
    {
        public const float TelegraphSeconds = .6f;
        public const float DashSeconds = .32f;
        public const float DashSpeed = 6f;
        public const float AuraInterval = .25f;

        public static SpecialEnemyMotionResult Tick(EnemyArchetype archetype, ref SpecialEnemyMotionState state,
            float deltaTime, Vector2 directionToPlayer, bool frozen, bool knockedBack, bool killed,
            int activeCount, int activeCap)
        {
            var step = Mathf.Max(0f, deltaTime);
            if (archetype == EnemyArchetype.SplittingRat && killed && !state.SplitResolved)
            {
                state.SplitResolved = true;
                return activeCap - activeCount >= 2
                    ? new SpecialEnemyMotionResult(Vector2.zero, splitChildren: 2)
                    : new SpecialEnemyMotionResult(Vector2.zero, fallbackBlast: true);
            }

            if (archetype == EnemyArchetype.SpiritShaman)
            {
                state.AuraElapsed += step;
                var pulse = state.AuraElapsed + .00001f >= AuraInterval;
                if (pulse) state.AuraElapsed -= AuraInterval;
                return new SpecialEnemyMotionResult(directionToPlayer.normalized * .7f, auraPulse: pulse);
            }

            if (archetype != EnemyArchetype.ChargingHornGhost)
                return new SpecialEnemyMotionResult(directionToPlayer.normalized);

            if ((frozen || knockedBack) && state.Phase != SpecialEnemyMotionPhase.Chase)
            {
                state.Phase = SpecialEnemyMotionPhase.Chase; state.Remaining = 0f;
                return new SpecialEnemyMotionResult(Vector2.zero, wasInterrupted: true);
            }
            if (frozen) return new SpecialEnemyMotionResult(Vector2.zero);

            if (state.Phase == SpecialEnemyMotionPhase.Chase)
            {
                state.Phase = SpecialEnemyMotionPhase.Telegraph;
                state.Remaining = TelegraphSeconds;
                state.LockedDirection = directionToPlayer.sqrMagnitude > .0001f ? directionToPlayer.normalized : Vector2.right;
            }
            if (state.Phase == SpecialEnemyMotionPhase.Telegraph)
            {
                state.Remaining -= step;
                if (state.Remaining > 0f) return new SpecialEnemyMotionResult(Vector2.zero, isTelegraphing: true);
                state.Phase = SpecialEnemyMotionPhase.Dash; state.Remaining = DashSeconds;
                return new SpecialEnemyMotionResult(state.LockedDirection * DashSpeed);
            }

            state.Remaining -= step;
            var velocity = state.LockedDirection * DashSpeed;
            if (state.Remaining <= 0f) { state.Phase = SpecialEnemyMotionPhase.Chase; state.Remaining = 0f; }
            return new SpecialEnemyMotionResult(velocity);
        }
    }
}
