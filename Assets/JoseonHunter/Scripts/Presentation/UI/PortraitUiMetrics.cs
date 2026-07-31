using UnityEngine;

namespace JoseonHunter.Presentation.UI
{
    public static class PortraitUiMetrics
    {
        public static readonly Vector2 ReferenceResolution = new(1080f, 1920f);
        public static readonly Vector2Int[] ValidationResolutions =
        {
            new(720, 1280), new(1080, 1920), new(1080, 2340), new(1170, 2532), new(1440, 3200)
        };

        public const float SideMargin = 48f;
        public const float TopMargin = 32f;
        public const float BottomMargin = 36f;
        public const float ModalWidth = 936f;
        public const float UpgradeCardHeight = 236f;
        public const float RackSlotWidth = 474f;
        public const float RackSlotHeight = 104f;

        public static float ContainedWidth(RectTransform parent, float maximum)
        {
            return ContainedWidth(parent == null ? 0f : parent.rect.width, maximum);
        }

        public static float ContainedWidth(float availableWidth, float maximum) =>
            availableWidth <= 0f ? maximum : Mathf.Min(maximum, availableWidth);

        public static Vector2 CanvasSizeFor(Vector2 pixelSize)
        {
            var widthScale = pixelSize.x / ReferenceResolution.x;
            var heightScale = pixelSize.y / ReferenceResolution.y;
            var scale = Mathf.Sqrt(widthScale * heightScale);
            return pixelSize / scale;
        }
    }
}
