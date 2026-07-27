using UnityEngine;

namespace JoseonHunter.Presentation
{
    public sealed class StaticSpriteMotionState
    {
        private const float BobAmplitude = 1f / 32f;
        private const float BobFrequency = 6f;
        private const float MaximumTiltDegrees = 2f;

        private Vector2 velocity;
        private float elapsedTime;

        public bool FlipX { get; private set; }
        public float BobOffset { get; private set; }
        public float TiltDegrees { get; private set; }

        public void SetVelocity(Vector2 newVelocity)
        {
            velocity = newVelocity;

            if (velocity.x > 0f)
            {
                FlipX = false;
            }
            else if (velocity.x < 0f)
            {
                FlipX = true;
            }
        }

        public void Step(float deltaTime)
        {
            if (velocity == Vector2.zero)
            {
                BobOffset = 0f;
                TiltDegrees = 0f;
                return;
            }

            elapsedTime += deltaTime;
            BobOffset = Mathf.Sin(elapsedTime * BobFrequency * Mathf.PI * 2f) * BobAmplitude;
            TiltDegrees = Mathf.Clamp(-velocity.x, -MaximumTiltDegrees, MaximumTiltDegrees);
        }

        public void Reset()
        {
            velocity = Vector2.zero;
            elapsedTime = 0f;
            BobOffset = 0f;
            TiltDegrees = 0f;
        }
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StaticSpriteMotionPresenter : MonoBehaviour
    {
        private const float HitDuration = 0.08f;
        private const float DeathDuration = 0.35f;
        private const float DeathSettleDistance = 2f / 32f;

        private readonly StaticSpriteMotionState motionState = new StaticSpriteMotionState();

        private SpriteRenderer spriteRenderer;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private Vector3 originalLocalScale;
        private Color originalColor;
        private float hitElapsed;
        private float deathElapsed;
        private bool isDying;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            originalLocalScale = transform.localScale;
            originalColor = spriteRenderer.color;
        }

        private void Update()
        {
            if (isDying)
            {
                UpdateDeath();
                return;
            }

            motionState.Step(Time.deltaTime);
            transform.localPosition = originalLocalPosition + Vector3.up * motionState.BobOffset;
            transform.localRotation = originalLocalRotation * Quaternion.Euler(0f, 0f, motionState.TiltDegrees);
            transform.localScale = originalLocalScale;
            spriteRenderer.flipX = motionState.FlipX;

            if (hitElapsed < HitDuration)
            {
                hitElapsed += Time.deltaTime;
                spriteRenderer.color = Color.Lerp(Color.white, originalColor, hitElapsed / HitDuration);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }

        public void SetVelocity(Vector2 velocity)
        {
            if (isDying)
            {
                return;
            }

            motionState.SetVelocity(velocity);
        }

        public void ShowHit()
        {
            if (isDying)
            {
                return;
            }

            hitElapsed = 0f;
            spriteRenderer.color = Color.white;
        }

        public void PlayDeath()
        {
            isDying = true;
            hitElapsed = HitDuration;
            deathElapsed = 0f;
            motionState.Reset();
        }

        private void UpdateDeath()
        {
            deathElapsed = Mathf.Min(deathElapsed + Time.deltaTime, DeathDuration);
            var progress = deathElapsed / DeathDuration;
            transform.localPosition = originalLocalPosition + Vector3.down * (DeathSettleDistance * progress);
            transform.localRotation = originalLocalRotation;
            transform.localScale = originalLocalScale * (1f - progress);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * (1f - progress));
        }
    }
}
