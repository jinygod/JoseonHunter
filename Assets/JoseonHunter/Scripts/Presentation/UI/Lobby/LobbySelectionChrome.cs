using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    public static class LobbySelectionChrome
    {
        private static readonly Color SelectedGold = new(1f, .69f, .12f, 1f);
        private static readonly Color SelectedJade = new(.12f, .80f, .68f, 1f);
        private static readonly Color IdleBrown = new(.30f, .20f, .14f, 1f);
        private static Sprite lockSprite;

        public static void Apply(Button button, bool selected, bool locked = false)
        {
            if (button == null) return;
            var outer = EnsureFrame(button.transform, "Selection Outer Border", SelectedGold, 5f, 0f);
            var inner = EnsureFrame(button.transform, "Selection Inner Border", SelectedJade, 2f, 7f);
            var idle = EnsureFrame(button.transform, "Idle Border", IdleBrown, 2f, 1f);
            outer.SetActive(selected);
            inner.SetActive(selected);
            idle.SetActive(!selected);

            var slash = EnsureSlash(button.transform);
            var lockIcon = EnsureLockIcon(button.transform);
            slash.gameObject.SetActive(locked);
            lockIcon.gameObject.SetActive(locked);
            outer.transform.SetAsLastSibling();
            inner.transform.SetAsLastSibling();
            slash.transform.SetAsLastSibling();
            lockIcon.transform.SetAsLastSibling();
        }

        private static GameObject EnsureFrame(Transform parent, string name, Color color,
            float thickness, float inset)
        {
            var existing = parent.Find(name)?.gameObject;
            if (existing != null) return existing;

            var root = RuntimeUiFactory.Rect(name, parent);
            RuntimeUiFactory.Stretch(root, inset, inset, inset, inset);
            Rail("Top", root, color, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -thickness), new Vector2(0f, thickness));
            Rail("Bottom", root, color, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(0f, thickness), new Vector2(0f, thickness));
            Rail("Left", root, color, Vector2.zero, new Vector2(0f, 1f),
                new Vector2(thickness, 0f), new Vector2(thickness, 0f));
            Rail("Right", root, color, new Vector2(1f, 0f), Vector2.one,
                new Vector2(-thickness, 0f), new Vector2(thickness, 0f));
            return root.gameObject;
        }

        private static void Rail(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var image = RuntimeUiFactory.Image(name, parent, color);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image EnsureSlash(Transform parent)
        {
            var existing = parent.Find("Lock Slash")?.GetComponent<Image>();
            if (existing != null) return existing;
            var slash = RuntimeUiFactory.Image("Lock Slash", parent, SelectedGold);
            slash.raycastTarget = false;
            var rect = slash.rectTransform;
            rect.anchorMin = new Vector2(.08f, .5f);
            rect.anchorMax = new Vector2(.92f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 5f);
            rect.localEulerAngles = new Vector3(0f, 0f, 12f);
            return slash;
        }

        private static Image EnsureLockIcon(Transform parent)
        {
            var existing = parent.Find("Lock Icon")?.GetComponent<Image>();
            if (existing != null) return existing;
            var icon = RuntimeUiFactory.Image("Lock Icon", parent, Color.white);
            icon.sprite = lockSprite ??= Resources.Load<Sprite>("Lobby/icon_lock");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(28f, 28f);
            return icon;
        }
    }
}
