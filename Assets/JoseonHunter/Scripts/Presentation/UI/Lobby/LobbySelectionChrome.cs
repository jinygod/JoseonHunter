using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    public static class LobbySelectionChrome
    {
        public static void Apply(Button button, bool selected, bool locked = false)
        {
            PremiumPixelUiSkin.ApplyDifficulty(button, selected, locked);
        }

        public static void ApplyRow(Button button, bool selected)
        {
            if (button == null) return;
            PremiumPixelUiSkin.ApplyFrame(button.GetComponent<Image>(), PremiumFrame.SmallItem);
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = selected ? new Color(1f, .82f, .42f, 1f) : Color.white,
                highlightedColor = selected ? new Color(1f, .9f, .62f, 1f) : new Color(1f, .92f, .8f, 1f),
                pressedColor = new Color(.72f, .72f, .72f, 1f),
                selectedColor = selected ? new Color(1f, .86f, .52f, 1f) : new Color(1f, .95f, .88f, 1f),
                disabledColor = new Color(1f, 1f, 1f, .45f),
                colorMultiplier = 1f,
                fadeDuration = .08f
            };
        }

        public static void ApplyWeaponSelector(Button button, bool selected)
        {
            if (button == null) return;
            PremiumPixelUiSkin.ApplyFrame(button.GetComponent<Image>(), PremiumFrame.WeaponSelector);
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = selected ? new Color(1f, .82f, .42f, 1f) : Color.white,
                highlightedColor = selected ? new Color(1f, .9f, .62f, 1f) : new Color(1f, .92f, .8f, 1f),
                pressedColor = new Color(.72f, .72f, .72f, 1f),
                selectedColor = selected ? new Color(1f, .86f, .52f, 1f) : new Color(1f, .95f, .88f, 1f),
                disabledColor = new Color(1f, 1f, 1f, .45f),
                colorMultiplier = 1f,
                fadeDuration = .08f
            };
        }

        public static void ApplyNavigation(Button button, PremiumIcon icon, bool selected)
        {
            PremiumPixelUiSkin.ApplyNavigation(button, icon, selected);
        }
    }
}
