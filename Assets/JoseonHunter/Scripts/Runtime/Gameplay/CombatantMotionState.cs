using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public enum MotionWeight
    {
        Light,
        Medium,
        Heavy
    }

    public readonly struct CombatantMotionPose
    {
        public CombatantMotionPose(
            Vector2 visualOffset,
            float tiltDegrees,
            Vector2 scale,
            bool facingLeft,
            float normalizedSpeed,
            float footstepPulse,
            float deathProgress)
        {
            VisualOffset = visualOffset;
            TiltDegrees = tiltDegrees;
            Scale = scale;
            FacingLeft = facingLeft;
            NormalizedSpeed = normalizedSpeed;
            FootstepPulse = footstepPulse;
            DeathProgress = deathProgress;
        }

        public Vector2 VisualOffset { get; }
        public float TiltDegrees { get; }
        public Vector2 Scale { get; }
        public bool FacingLeft { get; }
        public float NormalizedSpeed { get; }
        public float FootstepPulse { get; }
        public float DeathProgress { get; }
    }

    /// <summary>
    /// Allocation-free presentation motion. It never owns or mutates the logical combat position.
    /// </summary>
    public sealed class CombatantMotionState
    {
        private const float MaximumVisualOffset = 0.12f;
        private const float MaximumTilt = 4f;
        private const float DeathDuration = 0.28f;

        private readonly float phaseSeed;
        private Vector2 displayedVelocity;
        private Vector2 recoil;
        private float phase;
        private float deathElapsed;
        private bool facingLeft;
        private bool dying;

        public CombatantMotionState(float phaseSeed)
        {
            this.phaseSeed = Mathf.Repeat(phaseSeed, 1f) * Mathf.PI * 2f;
            phase = this.phaseSeed;
        }

        public CombatantMotionPose Step(Vector2 desiredVelocity, float deltaTime, MotionWeight weight)
        {
            deltaTime = Mathf.Max(0f, deltaTime);
            var response = weight == MotionWeight.Heavy ? 7f : weight == MotionWeight.Light ? 14f : 10f;
            var velocityBlend = 1f - Mathf.Exp(-response * deltaTime);
            displayedVelocity = Vector2.Lerp(displayedVelocity, desiredVelocity, velocityBlend);

            if (displayedVelocity.x > 0.06f) facingLeft = false;
            else if (displayedVelocity.x < -0.06f) facingLeft = true;

            var speed = Mathf.Clamp01(displayedVelocity.magnitude / 2.4f);
            var cadence = weight == MotionWeight.Heavy ? 5.2f : weight == MotionWeight.Light ? 8.8f : 7f;
            phase += deltaTime * Mathf.Lerp(1.4f, cadence, speed);
            var stepWave = Mathf.Sin(phase * Mathf.PI * 2f);
            var footstepPulse = speed * Mathf.Max(0f, -stepWave);
            var bobAmplitude = weight == MotionWeight.Heavy ? 0.026f : weight == MotionWeight.Light ? 0.045f : 0.035f;
            var bob = Mathf.Abs(stepWave) * bobAmplitude * speed;

            var recoilBlend = 1f - Mathf.Exp(-18f * deltaTime);
            recoil = Vector2.Lerp(recoil, Vector2.zero, recoilBlend);
            recoil = Vector2.ClampMagnitude(recoil, MaximumVisualOffset);

            var tilt = Mathf.Clamp(-displayedVelocity.x * 1.35f + recoil.x * 10f, -MaximumTilt, MaximumTilt);
            var squash = footstepPulse * (weight == MotionWeight.Heavy ? 0.045f : 0.03f);
            var scale = new Vector2(1f + squash, 1f - squash);
            var deathProgress = 0f;
            if (dying)
            {
                deathElapsed = Mathf.Min(DeathDuration, deathElapsed + deltaTime);
                deathProgress = deathElapsed / DeathDuration;
                recoil *= 1f - deathProgress;
                scale *= Mathf.Lerp(1f, 0.12f, deathProgress * deathProgress);
                tilt += (facingLeft ? 1f : -1f) * 72f * deathProgress;
            }

            var offset = Vector2.ClampMagnitude(recoil + Vector2.down * bob, MaximumVisualOffset);
            return new CombatantMotionPose(offset, tilt, scale, facingLeft, speed, footstepPulse, deathProgress);
        }

        public void Hit(Vector2 incomingDirection, float strength)
        {
            if (dying) return;
            var direction = incomingDirection.sqrMagnitude > 0.0001f
                ? incomingDirection.normalized
                : Vector2.right;
            recoil = Vector2.ClampMagnitude(recoil + direction * Mathf.Clamp(strength, 0.02f, MaximumVisualOffset), MaximumVisualOffset);
        }

        public void Kill()
        {
            dying = true;
            deathElapsed = 0f;
        }
    }
}
