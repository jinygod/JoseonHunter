using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public enum PremiumFrame
    {
        Panel,
        StagePlaque,
        CardIdle,
        CardSelected,
        NavigationIdle,
        NavigationSelected,
        HeroOval
    }

    public enum PremiumIcon
    {
        Research,
        Patrol,
        Training,
        Previous,
        Next,
        Settings,
        Lock
    }

    public static class PremiumPixelUiSkin
    {
        private const string ResourceRoot = "UI/PremiumJoseon/";
        private const string PremiumIconName = "Premium Icon";
        private static readonly Color IdleTint = new(.54f, .48f, .40f, 1f);
        private static readonly Color LockedTint = new(.30f, .29f, .28f, 1f);

        public static void ApplyFrame(Image image, PremiumFrame frame)
        {
            if (image == null) return;
            var resourceName = FrameName(frame);
            if (string.IsNullOrEmpty(resourceName)) return;
            var sprite = Resources.Load<Sprite>(ResourceRoot + resourceName);
            if (sprite == null)
            {
                Debug.LogError($"Missing premium UI frame: {resourceName}");
                return;
            }

            image.sprite = sprite;
            image.type = frame == PremiumFrame.HeroOval ? Image.Type.Simple : Image.Type.Sliced;
            image.preserveAspect = frame == PremiumFrame.HeroOval;
            image.color = Color.white;
        }

        public static void ApplyIcon(Image image, PremiumIcon icon)
        {
            if (image == null) return;
            var resourceName = IconName(icon);
            if (string.IsNullOrEmpty(resourceName)) return;
            var sprite = Resources.Load<Sprite>(ResourceRoot + resourceName);
            if (sprite == null)
            {
                Debug.LogError($"Missing premium UI icon: {resourceName}");
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            image.rectTransform.localScale = icon == PremiumIcon.Next
                ? new Vector3(-1f, 1f, 1f)
                : Vector3.one;
        }

        public static void ApplyNavigation(Button button, PremiumIcon icon, bool selected)
        {
            if (button == null) return;
            var background = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (background == null) return;
            ApplyFrame(background, selected ? PremiumFrame.NavigationSelected : PremiumFrame.NavigationIdle);
            var tint = selected ? Color.white : IdleTint;
            background.color = tint;
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ColorsFor(tint);

            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                label.gameObject.SetActive(false);

            var iconImage = EnsureImage(button.transform, PremiumIconName);
            var rect = iconImage.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(58f, 58f);
            ApplyIcon(iconImage, icon);
            iconImage.transform.SetAsLastSibling();
        }

        public static void ApplyDifficulty(Button button, bool selected, bool locked)
        {
            if (button == null) return;
            var background = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (background == null) return;
            ApplyFrame(background, selected ? PremiumFrame.CardSelected : PremiumFrame.CardIdle);
            var tint = selected ? Color.white : locked ? LockedTint : IdleTint;
            background.color = tint;
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ColorsFor(tint);

            var slash = EnsureImage(button.transform, "Lock Slash");
            slash.sprite = null;
            slash.color = new Color(.92f, .63f, .18f, .95f);
            slash.raycastTarget = false;
            var slashRect = slash.rectTransform;
            slashRect.anchorMin = new Vector2(.08f, .5f);
            slashRect.anchorMax = new Vector2(.92f, .5f);
            slashRect.anchoredPosition = Vector2.zero;
            slashRect.sizeDelta = new Vector2(0f, 5f);
            slashRect.localEulerAngles = new Vector3(0f, 0f, -16f);
            slash.gameObject.SetActive(locked);

            var lockIcon = EnsureImage(button.transform, "Lock Icon");
            var lockRect = lockIcon.rectTransform;
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(.5f, .5f);
            lockRect.pivot = new Vector2(.5f, .5f);
            lockRect.anchoredPosition = Vector2.zero;
            lockRect.sizeDelta = new Vector2(34f, 34f);
            ApplyIcon(lockIcon, PremiumIcon.Lock);
            lockIcon.gameObject.SetActive(locked);
            slash.transform.SetAsLastSibling();
            lockIcon.transform.SetAsLastSibling();
        }

        private static Image EnsureImage(Transform parent, string name)
        {
            return parent.Find(name)?.GetComponent<Image>() ??
                   RuntimeUiFactory.Image(name, parent, Color.white);
        }

        private static ColorBlock ColorsFor(Color normal)
        {
            return new ColorBlock
            {
                normalColor = normal,
                highlightedColor = Color.Lerp(normal, Color.white, .16f),
                pressedColor = Color.Lerp(normal, Color.black, .25f),
                selectedColor = Color.Lerp(normal, Color.white, .12f),
                disabledColor = new Color(normal.r, normal.g, normal.b, .45f),
                colorMultiplier = 1f,
                fadeDuration = .08f
            };
        }

        private static string FrameName(PremiumFrame frame)
        {
            return frame switch
            {
                PremiumFrame.Panel => "panel_frame",
                PremiumFrame.StagePlaque => "stage_plaque_frame",
                PremiumFrame.CardIdle => "card_idle_frame",
                PremiumFrame.CardSelected => "card_selected_frame",
                PremiumFrame.NavigationIdle => "nav_idle_frame",
                PremiumFrame.NavigationSelected => "nav_selected_frame",
                PremiumFrame.HeroOval => "hero_oval_frame",
                _ => null
            };
        }

        private static string IconName(PremiumIcon icon)
        {
            return icon switch
            {
                PremiumIcon.Research => "icon_research",
                PremiumIcon.Patrol => "icon_patrol",
                PremiumIcon.Training => "icon_training",
                PremiumIcon.Previous => "icon_previous",
                PremiumIcon.Next => "icon_next",
                PremiumIcon.Settings => "icon_settings",
                PremiumIcon.Lock => "icon_lock",
                _ => null
            };
        }
    }
}
