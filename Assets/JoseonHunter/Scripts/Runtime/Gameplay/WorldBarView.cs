using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WorldBarView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer fillRenderer;
        [SerializeField] private Vector3 fullFillLocalScale = new Vector3(2f, .14f, 1f);
        [SerializeField] private Vector3 fullFillLocalPosition = Vector3.zero;

        public SpriteRenderer BackgroundRenderer => backgroundRenderer;
        public SpriteRenderer FillRenderer => fillRenderer;
        public Transform Fill => fillRenderer == null ? null : fillRenderer.transform;
        public Vector3 FullFillLocalScale => fullFillLocalScale;
        public Vector3 FullFillLocalPosition => fullFillLocalPosition;

        public bool HasRequiredBindings => backgroundRenderer != null && fillRenderer != null &&
                                           backgroundRenderer != fillRenderer &&
                                           backgroundRenderer.transform.parent == transform &&
                                           fillRenderer.transform.parent == transform;

        public void Configure(SpriteRenderer background, SpriteRenderer fill)
        {
            backgroundRenderer = background;
            fillRenderer = fill;
            CaptureAuthoredFillGeometry();
        }

        public void Prepare(Sprite sharedSprite)
        {
            if (sharedSprite == null) return;
            if (backgroundRenderer != null && backgroundRenderer.sprite == null)
                backgroundRenderer.sprite = sharedSprite;
            if (fillRenderer != null && fillRenderer.sprite == null)
                fillRenderer.sprite = sharedSprite;
        }

        public void CaptureAuthoredFillGeometry()
        {
            if (fillRenderer == null) return;
            fullFillLocalScale = fillRenderer.transform.localScale;
            fullFillLocalPosition = fillRenderer.transform.localPosition;
        }

        public void SetNormalizedValue(float normalizedValue)
        {
            if (fillRenderer == null) return;
            var ratio = Mathf.Clamp01(normalizedValue);
            var scale = fullFillLocalScale;
            scale.x *= ratio;
            fillRenderer.transform.localScale = scale;

            var position = fullFillLocalPosition;
            position.x += fullFillLocalScale.x * (ratio - 1f) * .5f;
            fillRenderer.transform.localPosition = position;
        }

        private void OnValidate()
        {
            if (backgroundRenderer == null)
            {
                var background = transform.Find("Background");
                if (background != null) backgroundRenderer = background.GetComponent<SpriteRenderer>();
            }
            if (fillRenderer == null)
            {
                var fill = transform.Find("Fill");
                if (fill != null) fillRenderer = fill.GetComponent<SpriteRenderer>();
            }
            if (!Application.isPlaying) CaptureAuthoredFillGeometry();
        }
    }
}
