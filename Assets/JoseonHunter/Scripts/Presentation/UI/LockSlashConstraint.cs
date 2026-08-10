using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public sealed class LockSlashConstraint : MonoBehaviour
    {
        private const float SlashAngle = -16f;
        private static readonly Vector2 DecorationAnchor = new(.5f, .7f);
        private bool applying;

        public void Configure() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (applying || transform.parent is not RectTransform card) return;
            applying = true;
            try
            {
                var rect = (RectTransform)transform;
                var cardSize = card.rect.size;
                var slashThickness = Mathf.Min(5f, cardSize.y * .08f);
                var radians = Mathf.Abs(SlashAngle) * Mathf.Deg2Rad;
                var safeWidth = cardSize.x * .38f;
                var safeHeight = cardSize.y * .30f;
                var maxLengthByWidth = (safeWidth - slashThickness * Mathf.Sin(radians)) / Mathf.Cos(radians);
                var maxLengthByHeight = (safeHeight - slashThickness * Mathf.Cos(radians)) / Mathf.Sin(radians);
                var slashLength = Mathf.Max(0f, Mathf.Min(safeWidth, maxLengthByWidth, maxLengthByHeight));
                rect.anchorMin = rect.anchorMax = DecorationAnchor;
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(slashLength, slashThickness);
                rect.localEulerAngles = new Vector3(0f, 0f, SlashAngle);
            }
            finally
            {
                applying = false;
            }
        }
    }
}
