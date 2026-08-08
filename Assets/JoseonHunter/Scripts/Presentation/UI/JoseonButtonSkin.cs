using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public enum JoseonButtonStyle
    {
        Primary,
        Secondary
    }

    public enum JoseonButtonIcon
    {
        None,
        Continue,
        Lobby
    }

    public static class JoseonButtonSkin
    {
        private const string IconName = "Action Icon";
        private static Sprite primaryFrame;
        private static Sprite secondaryFrame;
        private static Sprite continueIcon;
        private static Sprite lobbyIcon;

        public static void Apply(Button button, JoseonButtonStyle style,
            JoseonButtonIcon icon = JoseonButtonIcon.None)
        {
            if (button == null) return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null) return;

            image.sprite = FrameFor(style);
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ColorsFor(style);

            var iconImage = EnsureIcon(button.transform);
            var sprite = IconFor(icon);
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
            iconImage.gameObject.SetActive(sprite != null);
            InsetLabels(button.transform, sprite != null);
        }

        private static Sprite FrameFor(JoseonButtonStyle style)
        {
            if (style == JoseonButtonStyle.Primary)
                return primaryFrame ??= Resources.Load<Sprite>("UI/Buttons/button_primary_frame");
            return secondaryFrame ??= Resources.Load<Sprite>("UI/Buttons/button_secondary_frame");
        }

        private static Sprite IconFor(JoseonButtonIcon icon)
        {
            return icon switch
            {
                JoseonButtonIcon.Continue => continueIcon ??=
                    Resources.Load<Sprite>("UI/Buttons/icon_continue"),
                JoseonButtonIcon.Lobby => lobbyIcon ??=
                    Resources.Load<Sprite>("UI/Buttons/icon_lobby"),
                _ => null
            };
        }

        private static Image EnsureIcon(Transform parent)
        {
            var existing = parent.Find(IconName)?.GetComponent<Image>();
            if (existing != null) return existing;

            var image = RuntimeUiFactory.Image(IconName, parent, Color.white);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.pivot = new Vector2(0f, .5f);
            rect.anchoredPosition = new Vector2(18f, 0f);
            rect.sizeDelta = new Vector2(28f, 28f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void InsetLabels(Transform parent, bool hasIcon)
        {
            foreach (var label in parent.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.transform == parent) continue;
                var rect = label.rectTransform;
                rect.offsetMin = new Vector2(hasIcon ? 48f : 12f, rect.offsetMin.y);
                rect.offsetMax = new Vector2(-12f, rect.offsetMax.y);
                label.color = JoseonUiPalette.Hanji;
            }
        }

        private static ColorBlock ColorsFor(JoseonButtonStyle style)
        {
            var normal = Color.white;
            var highlighted = style == JoseonButtonStyle.Primary
                ? new Color(1f, .94f, .78f, 1f)
                : new Color(.94f, .90f, .82f, 1f);
            return new ColorBlock
            {
                normalColor = normal,
                highlightedColor = highlighted,
                pressedColor = new Color(.72f, .68f, .62f, 1f),
                selectedColor = highlighted,
                disabledColor = new Color(.48f, .46f, .43f, .58f),
                colorMultiplier = 1f,
                fadeDuration = .08f
            };
        }
    }
}
