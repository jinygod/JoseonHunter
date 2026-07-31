using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public static class RuntimeUiFactory
    {
        private const string KoreanFontPath = "Fonts/NotoSansKR-Dynamic SDF";
        private static TMP_FontAsset koreanFont;

        private static TMP_FontAsset KoreanFont
        {
            get
            {
                if (koreanFont == null) koreanFont = Resources.Load<TMP_FontAsset>(KoreanFontPath);
                return koreanFont;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.SetParent(parent, false);
            return result;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var result = Rect(name, parent).gameObject.AddComponent<Image>();
            result.color = color;
            return result;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string value, float size,
            TextAlignmentOptions alignment)
        {
            var result = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            var font = KoreanFont;
            if (font == null)
                Debug.LogError($"Missing runtime UI font at Resources/{KoreanFontPath}.");
            else
                result.font = font;
            result.text = value;
            result.fontSize = size;
            result.alignment = alignment;
            result.color = JoseonUiPalette.Hanji;
            result.raycastTarget = false;
            return result;
        }

        public static Button Button(string name, Transform parent, Color color)
        {
            var image = Image(name, parent, color);
            return image.gameObject.AddComponent<Button>();
        }

        public static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
