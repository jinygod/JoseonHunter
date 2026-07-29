using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
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

        private float animationTime;
        private int frameIndex = -1;
        private bool wasMoving;

        private CombatantVisualRig(
            Transform logicalRoot,
            Transform visualPivot,
            SpriteRenderer renderer,
            CombatMotionSet motionSet,
            MotionWeight weight,
            float phaseOffset)
        {
            this.logicalRoot = logicalRoot;
            this.visualPivot = visualPivot;
            this.renderer = renderer;
            this.motionSet = motionSet;
            this.phaseOffset = Mathf.Repeat(phaseOffset, 1f);
            motionState = new CombatantMotionState(this.phaseOffset);
            baseScale = visualPivot.localScale;
            animationTime = this.phaseOffset;
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
            float phaseOffset = 0f)
        {
            var pivot = new GameObject("Visual Pivot").transform;
            pivot.SetParent(logicalRoot.transform, false);
            var renderer = pivot.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return new CombatantVisualRig(logicalRoot.transform, pivot, renderer, motionSet, weight, phaseOffset);
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
                if (sprite != null) renderer.sprite = sprite;
            }

            visualPivot.localPosition = new Vector3(pose.VisualOffset.x, pose.VisualOffset.y, 0f);
            visualPivot.localRotation = Quaternion.Euler(0f, 0f, pose.TiltDegrees);
            visualPivot.localScale = Vector3.Scale(baseScale, new Vector3(pose.Scale.x, pose.Scale.y, 1f));
            renderer.flipX = pose.FacingLeft;
            var color = renderer.color;
            color.a = 1f - pose.DeathProgress;
            renderer.color = color;
        }

        public void ShowHit(Vector2 incomingDirection, float strength) =>
            motionState.Hit(incomingDirection, strength);

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
    }
}
