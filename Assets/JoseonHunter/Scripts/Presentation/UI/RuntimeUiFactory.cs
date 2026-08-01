using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public static class RuntimeUiFactory
    {
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
            TextAlignmentOptions alignment, RuntimeFontRole role = RuntimeFontRole.Body)
        {
            var result = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            var font = RuntimeFontCatalog.For(role);
            if (font != null)
                result.font = font;
            result.text = value;
            result.fontSize = size;
            result.alignment = alignment;
            result.color = JoseonUiPalette.DarkPanelText;
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
