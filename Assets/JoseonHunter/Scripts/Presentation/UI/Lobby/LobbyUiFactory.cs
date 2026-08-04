using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    internal static class LobbyUiFactory
    {
        internal static readonly Color Hanji = new Color(.88f, .80f, .62f, 1f);
        internal static readonly Color HanjiLight = new Color(.95f, .89f, .73f, 1f);
        internal static readonly Color Ink = new Color(.11f, .075f, .055f, 1f);
        internal static readonly Color Brown = new Color(.25f, .12f, .075f, 1f);
        internal static readonly Color Jade = new Color(.22f, .42f, .36f, 1f);
        internal static readonly Color Gold = new Color(.78f, .52f, .16f, 1f);

        internal static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        internal static Image Image(string name, Transform parent, Color color, bool raycast = false)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = rect.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal static TMP_Text Text(string name, Transform parent, string value, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool title = false)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var text = rect.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = Ink;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            var font = RuntimeFontCatalog.For(title ? RuntimeFontRole.Title : RuntimeFontRole.Body);
            if (font != null) text.font = font;
            return text;
        }

        internal static Button Button(string name, Transform parent, string label, float size = 24f)
        {
            var image = Image(name, parent, Brown, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.16f, 1.08f, 1.02f, 1f);
            colors.pressedColor = new Color(.72f, .72f, .72f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            var text = Text("Label", image.transform, label, size);
            text.color = HanjiLight;
            Stretch(text.rectTransform, 12f, 6f, 12f, 6f);
            return button;
        }

        internal static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f,
            float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static void Anchor(RectTransform rect, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
