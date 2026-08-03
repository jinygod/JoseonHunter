using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public enum CombatantVisualRole
    {
        Player,
        Enemy,
        Elite,
        Boss
    }

    /// <summary>Owns presentation below a stable gameplay root.</summary>
    public sealed class CombatantVisualRig
    {
        private readonly Transform logicalRoot;
        private readonly Transform visualPivot;
        private readonly SpriteRenderer renderer;
        private readonly CombatMotionSet motionSet;
        private readonly CombatantMotionState motionState;
        private readonly Vector3 baseScale;
        private readonly float phaseOffset;
        private readonly SpriteRenderer shadowRenderer;
        private readonly SpriteRenderer outlineRenderer;
        private readonly SpriteRenderer auraRenderer;
        private readonly float shadowBaseAlpha;
        private readonly float auraBaseAlpha;
        private const float HitFlashDuration = .095f;
        private static readonly Color HitFlashColor = new Color(1f, .64f, .32f, 1f);
        private static readonly Color HitOutlineFlashColor = new Color(1f, .88f, .52f, 1f);
        private static readonly Color GuardFlashColor = new Color(.76f, .45f, .16f, 1f);
        private static readonly Color GuardOutlineFlashColor = new Color(.55f, .32f, .12f, 1f);

        private float animationTime;
        private int frameIndex = -1;
        private bool wasMoving;
        private float hitFlashRemaining;
        private bool hitFlashActive;
        private Color hitBaseColor;
        private Color hitOutlineBaseColor;
        private Color activeFlashColor = HitFlashColor;
        private Color activeOutlineFlashColor = HitOutlineFlashColor;
        private float activeFlashDuration = HitFlashDuration;

        private CombatantVisualRig(
            Transform logicalRoot,
            Transform visualPivot,
            SpriteRenderer renderer,
            CombatMotionSet motionSet,
            MotionWeight weight,
            float phaseOffset,
            SpriteRenderer shadowRenderer,
            SpriteRenderer outlineRenderer,
            SpriteRenderer auraRenderer)
        {
            this.logicalRoot = logicalRoot;
            this.visualPivot = visualPivot;
            this.renderer = renderer;
            this.motionSet = motionSet;
            this.phaseOffset = Mathf.Repeat(phaseOffset, 1f);
            motionState = new CombatantMotionState(this.phaseOffset);
            baseScale = visualPivot.localScale;
            animationTime = this.phaseOffset;
            this.shadowRenderer = shadowRenderer;
            this.outlineRenderer = outlineRenderer;
            this.auraRenderer = auraRenderer;
            shadowBaseAlpha = shadowRenderer == null ? 0f : shadowRenderer.color.a;
            auraBaseAlpha = auraRenderer == null ? 0f : auraRenderer.color.a;
        }

        public SpriteRenderer Renderer => renderer;
        public Transform LogicalRoot => logicalRoot;
        public bool FacingLeft => renderer != null && renderer.flipX;

        public static CombatantVisualRig Create(
            GameObject logicalRoot,
            Sprite sprite,
            int sortingOrder,
            CombatMotionSet motionSet,
            MotionWeight weight,
            float phaseOffset = 0f,
            CombatantVisualRole role = CombatantVisualRole.Enemy)
        {
            var shadow = CreateLayer(
                logicalRoot.transform,
                "Soft Shadow",
                sprite,
                sortingOrder - 3,
                new Color(0.025f, 0.035f, 0.04f, role == CombatantVisualRole.Boss ? 0.48f : 0.36f));
            shadow.transform.localPosition = new Vector3(0f, -0.10f, 0f);
            shadow.transform.localScale = role == CombatantVisualRole.Boss
                ? new Vector3(0.90f, 0.18f, 1f)
                : new Vector3(0.72f, 0.14f, 1f);

            var outline = CreateLayer(
                logicalRoot.transform,
                "Silhouette Outline",
                sprite,
                sortingOrder - 1,
                new Color(0.025f, 0.04f, 0.055f, 0.92f));

            SpriteRenderer aura = null;
            if (role == CombatantVisualRole.Player)
            {
                aura = CreateLayer(
                    logicalRoot.transform,
                    "Player Aura",
                    sprite,
                    sortingOrder - 2,
                    new Color(0.18f, 0.94f, 0.88f, 0.16f));
            }

            var pivot = new GameObject("Visual Pivot").transform;
            pivot.SetParent(logicalRoot.transform, false);
            var renderer = pivot.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return new CombatantVisualRig(
                logicalRoot.transform,
                pivot,
                renderer,
                motionSet,
                weight,
                phaseOffset,
                shadow,
                outline,
                aura);
        }

        public void Tick(Vector2 desiredVelocity, float deltaTime, MotionWeight weight)
        {
            if (renderer == null || visualPivot == null) return;
            var pose = motionState.Step(desiredVelocity, deltaTime, weight);
            var moving = pose.NormalizedSpeed > 0.075f;
            if (moving != wasMoving)
            {
                animationTime = phaseOffset;
                frameIndex = -1;
                wasMoving = moving;
            }

            animationTime += deltaTime * (motionSet == null
                ? 1f
                : moving ? motionSet.MoveFramesPerSecond : motionSet.IdleFramesPerSecond);
            var nextFrame = Mathf.FloorToInt(animationTime);
            if (nextFrame != frameIndex)
            {
                frameIndex = nextFrame;
                var sprite = motionSet == null ? null : motionSet.Frame(moving, nextFrame);
                if (sprite != null)
                {
                    renderer.sprite = sprite;
                    shadowRenderer.sprite = sprite;
                    outlineRenderer.sprite = sprite;
                    if (auraRenderer != null) auraRenderer.sprite = sprite;
                }
            }

            visualPivot.localPosition = new Vector3(pose.VisualOffset.x, pose.VisualOffset.y, 0f);
            visualPivot.localRotation = Quaternion.Euler(0f, 0f, pose.TiltDegrees);
            visualPivot.localScale = Vector3.Scale(baseScale, new Vector3(pose.Scale.x, pose.Scale.y, 1f));
            renderer.flipX = pose.FacingLeft;
            SyncFollower(outlineRenderer, pose, 1.045f);
            if (auraRenderer != null) SyncFollower(auraRenderer, pose, 1.13f);
            shadowRenderer.flipX = pose.FacingLeft;
            UpdateHitFlash(deltaTime, pose.DeathProgress);
        }

        public void ShowHit(Vector2 incomingDirection, float strength)
        {
            motionState.Hit(incomingDirection, strength);
            BeginHitFlash(HitFlashColor, HitOutlineFlashColor, HitFlashDuration);
        }

        public void ShowGuardHit(Vector2 incomingDirection)
        {
            motionState.Hit(incomingDirection, .055f);
            BeginHitFlash(GuardFlashColor, GuardOutlineFlashColor, .08f);
        }

        private void BeginHitFlash(Color bodyColor, Color outlineColor, float duration)
        {
            if (!hitFlashActive)
            {
                hitBaseColor = renderer.color;
                hitOutlineBaseColor = outlineRenderer.color;
            }

            hitFlashActive = true;
            activeFlashColor = bodyColor;
            activeOutlineFlashColor = outlineColor;
            activeFlashDuration = Mathf.Max(.001f, duration);
            hitFlashRemaining = activeFlashDuration;
        }

        public void PlayDeath() => motionState.Kill();

        public PixelMaskTransform CollisionTransform(Float2 logicalPosition)
        {
            var scale = logicalRoot == null ? Vector3.one : logicalRoot.lossyScale;
            return new PixelMaskTransform(
                logicalPosition,
                0,
                FacingLeft,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
        }

        private static SpriteRenderer CreateLayer(
            Transform parent,
            string name,
            Sprite sprite,
            int sortingOrder,
            Color color)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            var result = layer.AddComponent<SpriteRenderer>();
            result.sprite = sprite;
            result.sortingOrder = sortingOrder;
            result.color = color;
            return result;
        }

        private static void SyncFollower(SpriteRenderer follower, CombatantMotionPose pose, float scale)
        {
            follower.transform.localPosition = new Vector3(pose.VisualOffset.x, pose.VisualOffset.y, 0f);
            follower.transform.localRotation = Quaternion.Euler(0f, 0f, pose.TiltDegrees);
            follower.transform.localScale = new Vector3(pose.Scale.x * scale, pose.Scale.y * scale, 1f);
            follower.flipX = pose.FacingLeft;
        }

        private void UpdateHitFlash(float deltaTime, float deathProgress)
        {
            var aliveAlpha = 1f - deathProgress;
            if (hitFlashActive)
            {
                hitFlashRemaining = Mathf.Max(0f, hitFlashRemaining - Mathf.Max(0f, deltaTime));
                var flash = Mathf.Clamp01(hitFlashRemaining / activeFlashDuration);
                var body = Color.Lerp(hitBaseColor, activeFlashColor, flash);
                body.a = hitBaseColor.a * aliveAlpha;
                renderer.color = body;

                var outline = Color.Lerp(hitOutlineBaseColor, activeOutlineFlashColor, flash * .7f);
                outline.a = hitOutlineBaseColor.a * aliveAlpha;
                outlineRenderer.color = outline;
                if (hitFlashRemaining <= 0f) hitFlashActive = false;
            }
            else
            {
                var body = renderer.color;
                body.a = aliveAlpha;
                renderer.color = body;
                var outline = outlineRenderer.color;
                outline.a = aliveAlpha * .92f;
                outlineRenderer.color = outline;
            }

            var shadow = shadowRenderer.color;
            shadow.a = shadowBaseAlpha * aliveAlpha;
            shadowRenderer.color = shadow;
            if (auraRenderer == null) return;
            var aura = auraRenderer.color;
            aura.a = auraBaseAlpha * aliveAlpha;
            auraRenderer.color = aura;
        }
    }
}
