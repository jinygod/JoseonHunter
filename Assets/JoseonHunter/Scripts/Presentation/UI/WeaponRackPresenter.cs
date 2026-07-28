using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public sealed class WeaponRackPresenter : MonoBehaviour
    {
        private readonly List<GameObject> slots = new();

        public void Render(IReadOnlyList<WeaponSlotView> weapons)
        {
            while (slots.Count > weapons.Count)
            {
                Destroy(slots[slots.Count - 1]);
                slots.RemoveAt(slots.Count - 1);
            }

            for (var index = 0; index < weapons.Count; index++)
            {
                if (index == slots.Count) slots.Add(CreateSlot(index));
                PopulateSlot(slots[index], weapons[index]);
            }
        }

        private GameObject CreateSlot(int index)
        {
            var root = RuntimeUiFactory.Image("Weapon Slot " + index, transform, JoseonUiPalette.Ink).gameObject;
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(42f, 42f + index * 118f);
            rect.sizeDelta = new Vector2(390f, 102f);
            var accent = RuntimeUiFactory.Image("Accent", root.transform, JoseonUiPalette.Gold);
            var accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.sizeDelta = new Vector2(8f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            var icon = RuntimeUiFactory.Image("Icon", root.transform, Color.white);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, .5f);
            icon.rectTransform.pivot = new Vector2(0f, .5f);
            icon.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(70f, 70f);
            icon.preserveAspect = true;
            var label = RuntimeUiFactory.Text("Name", root.transform, string.Empty, 22f, TextAlignmentOptions.Left);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, .5f);
            label.rectTransform.pivot = new Vector2(0f, .5f);
            label.rectTransform.anchoredPosition = new Vector2(110f, 12f);
            label.rectTransform.sizeDelta = new Vector2(210f, 34f);
            var level = RuntimeUiFactory.Text("Level", root.transform, string.Empty, 19f, TextAlignmentOptions.Left);
            level.rectTransform.anchorMin = level.rectTransform.anchorMax = new Vector2(0f, .5f);
            level.rectTransform.pivot = new Vector2(0f, .5f);
            level.rectTransform.anchoredPosition = new Vector2(110f, -22f);
            level.rectTransform.sizeDelta = new Vector2(210f, 28f);
            return root;
        }

        private static void PopulateSlot(GameObject root, WeaponSlotView weapon)
        {
            root.transform.Find("Accent").GetComponent<Image>().color = JoseonUiPalette.WeaponAccent(new WeaponId(weapon.Id));
            var icon = root.transform.Find("Icon").GetComponent<Image>();
            icon.sprite = weapon.Icon;
            icon.enabled = weapon.Icon != null;
            root.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = weapon.DisplayName;
            root.transform.Find("Level").GetComponent<TextMeshProUGUI>().text = $"LEVEL {weapon.Level}";
        }
    }
}
