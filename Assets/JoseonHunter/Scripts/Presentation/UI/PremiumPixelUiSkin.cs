using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public enum PremiumFrame
    {
        ThinOuter,
        HeaderBar,
        StageTitlePlate,
        ContentBackplate,
        DifficultyIdle,
        DifficultySelected,
        DifficultyLocked,
        WeaponSelector,
        TabIdle,
        TabSelected,
        SmallItem,
        HeroOval,

        // Existing presenters retain these names while sharing the thin semantic frames.
        Panel = ThinOuter,
        StagePlaque = StageTitlePlate,
        CardIdle = DifficultyIdle,
        CardSelected = DifficultySelected,
        NavigationIdle = TabIdle,
        NavigationSelected = TabSelected
    }

    public enum PremiumActionStyle
    {
        Primary,
        Secondary
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

        public static void ApplyFrame(Image image, PremiumFrame frame)
        {
            if (image == null) return;
            var resourceName = FrameName(frame);
            if (string.IsNullOrEmpty(resourceName)) return;
            LoadAndApply(image, resourceName, sliced: frame != PremiumFrame.HeroOval);
        }

        public static void ApplyAction(Button button, PremiumActionStyle style)
        {
            if (button == null) return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null) return;
            LoadAndApply(image, style == PremiumActionStyle.Primary
                ? "primary_red_button"
                : "secondary_dark_button", sliced: true);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ActionColors();
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
            ApplyFrame(background, selected ? PremiumFrame.TabSelected : PremiumFrame.TabIdle);
            var tint = Color.white;
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
            var frame = locked
                ? PremiumFrame.DifficultyLocked
                : selected ? PremiumFrame.DifficultySelected : PremiumFrame.DifficultyIdle;
            ApplyFrame(background, frame);
            var tint = Color.white;
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ColorsFor(tint);

            var slash = EnsureImage(button.transform, "Lock Slash");
            slash.sprite = null;
            slash.color = new Color(.92f, .63f, .18f, .95f);
            slash.raycastTarget = false;
            var slashLayout = slash.GetComponent<LockSlashConstraint>();
            if (slashLayout == null) slashLayout = slash.gameObject.AddComponent<LockSlashConstraint>();
            slashLayout.Configure();
            slash.gameObject.SetActive(locked);

            var lockIcon = EnsureImage(button.transform, "Lock Icon");
            var lockRect = lockIcon.rectTransform;
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(.5f, .5f);
            lockRect.pivot = new Vector2(.5f, .5f);
            lockRect.anchoredPosition = Vector2.zero;
            var lockSize = background.rectTransform.rect.height * .3f;
            lockRect.sizeDelta = new Vector2(lockSize, lockSize);
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

        private static ColorBlock ActionColors()
        {
            return new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, .9f, .86f, 1f),
                pressedColor = new Color(.72f, .72f, .72f, 1f),
                selectedColor = new Color(1f, .94f, .9f, 1f),
                disabledColor = new Color(1f, 1f, 1f, .45f),
                colorMultiplier = 1f,
                fadeDuration = .08f
            };
        }

        private static string FrameName(PremiumFrame frame)
        {
            return frame switch
            {
                PremiumFrame.ThinOuter => "thin_outer_frame",
                PremiumFrame.HeaderBar => "header_bar",
                PremiumFrame.StageTitlePlate => "stage_title_plate",
                PremiumFrame.ContentBackplate => "content_backplate",
                PremiumFrame.DifficultyIdle => "difficulty_idle",
                PremiumFrame.DifficultySelected => "difficulty_selected",
                PremiumFrame.DifficultyLocked => "difficulty_locked",
                PremiumFrame.WeaponSelector => "weapon_selector_frame",
                PremiumFrame.TabIdle => "tab_idle",
                PremiumFrame.TabSelected => "tab_selected",
                PremiumFrame.SmallItem => "small_item_frame",
                PremiumFrame.HeroOval => "hero_oval_frame",
                _ => null
            };
        }

        private static void LoadAndApply(Image image, string resourceName, bool sliced)
        {
            var sprite = Resources.Load<Sprite>(ResourceRoot + resourceName);
            if (sprite == null)
            {
                Debug.LogError($"Missing premium UI frame: {resourceName}");
                return;
            }

            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.color = Color.white;
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
