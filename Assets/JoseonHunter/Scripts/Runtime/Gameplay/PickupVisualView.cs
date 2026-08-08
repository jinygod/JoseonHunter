using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PickupVisualView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField, Min(.01f)] private float baseScale = 1f;

        public SpriteRenderer VisualRenderer => visualRenderer;
        public TrailRenderer TrailRenderer => trailRenderer;
        public float BaseScale => Mathf.Max(.01f, baseScale);
        public bool HasRequiredBindings => visualRenderer != null &&
                                           visualRenderer.transform.parent == transform &&
                                           (trailRenderer == null || trailRenderer.transform == transform);

        public void Configure(SpriteRenderer visual, TrailRenderer trail, float authoredBaseScale)
        {
            visualRenderer = visual;
            trailRenderer = trail;
            baseScale = Mathf.Max(.01f, authoredBaseScale);
        }
    }
}
