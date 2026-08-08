using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CombatantVisualView : MonoBehaviour
    {
        [Header("Motion-bound renderers")]
        [SerializeField] private Transform visualPivot;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private SpriteRenderer outlineRenderer;
        [SerializeField] private SpriteRenderer auraRenderer;

        [Header("Editable world-bar anchors")]
        [SerializeField] private Transform healthBarAnchor;
        [SerializeField] private Transform shieldBarAnchor;

        public Transform VisualPivot => visualPivot;
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public SpriteRenderer ShadowRenderer => shadowRenderer;
        public SpriteRenderer OutlineRenderer => outlineRenderer;
        public SpriteRenderer AuraRenderer => auraRenderer;
        public Transform HealthBarAnchor => healthBarAnchor;
        public Transform ShieldBarAnchor => shieldBarAnchor;

        public bool HasRequiredBindings(CombatantVisualRole role)
        {
            if (visualPivot == null || bodyRenderer == null || shadowRenderer == null ||
                outlineRenderer == null || healthBarAnchor == null)
                return false;
            if (visualPivot.parent != transform || bodyRenderer.transform != visualPivot) return false;
            if (shadowRenderer.transform.parent != transform || outlineRenderer.transform.parent != transform)
                return false;
            if (healthBarAnchor.parent != transform) return false;
            if (role == CombatantVisualRole.Player)
                return auraRenderer != null && auraRenderer.transform.parent == transform && shieldBarAnchor == null;
            return auraRenderer == null && shieldBarAnchor != null && shieldBarAnchor.parent == transform;
        }

        public void Configure(
            Transform pivot,
            SpriteRenderer body,
            SpriteRenderer shadow,
            SpriteRenderer outline,
            SpriteRenderer aura,
            Transform healthAnchor,
            Transform shieldAnchor)
        {
            visualPivot = pivot;
            bodyRenderer = body;
            shadowRenderer = shadow;
            outlineRenderer = outline;
            auraRenderer = aura;
            healthBarAnchor = healthAnchor;
            shieldBarAnchor = shieldAnchor;
        }
    }
}
