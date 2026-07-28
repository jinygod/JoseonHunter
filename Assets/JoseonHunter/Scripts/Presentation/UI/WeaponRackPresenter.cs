using System.Collections.Generic;
using System.Collections;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public sealed class WeaponRackPresenter : MonoBehaviour
    {
        private sealed class Slot
        {
            public GameObject Root;
            public Image Accent;
            public Image Icon;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Level;
            public string WeaponId;
        }

        private readonly List<Slot> slots = new();
        private readonly Dictionary<string, Slot> slotsByWeaponId = new();

        public void Render(IReadOnlyList<WeaponSlotView> weapons)
        {
            while (slots.Count > weapons.Count)
            {
                var slot = slots[slots.Count - 1];
                if (!string.IsNullOrEmpty(slot.WeaponId)) slotsByWeaponId.Remove(slot.WeaponId);
                Destroy(slot.Root);
                slots.RemoveAt(slots.Count - 1);
            }

            for (var index = 0; index < weapons.Count; index++)
            {
                if (index == slots.Count) slots.Add(CreateSlot(index));
                PopulateSlot(slots[index], weapons[index]);
            }
        }

        public void Pulse(string weaponId, int newLevel)
        {
            if (string.IsNullOrEmpty(weaponId) || !slotsByWeaponId.TryGetValue(weaponId, out var slot)) return;
            slot.Level.text = $"LEVEL {newLevel}";
            StartCoroutine(PulseRoutine(slot));
        }

        private Slot CreateSlot(int index)
        {
            var slot = new Slot();
            slot.Root = RuntimeUiFactory.Image("Weapon Slot " + index, transform, JoseonUiPalette.Ink).gameObject;
            var rect = slot.Root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(42f, 42f + index * 118f);
            rect.sizeDelta = new Vector2(390f, 102f);
            slot.Accent = RuntimeUiFactory.Image("Accent", slot.Root.transform, JoseonUiPalette.Gold);
            var accentRect = slot.Accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.sizeDelta = new Vector2(8f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            slot.Icon = RuntimeUiFactory.Image("Icon", slot.Root.transform, Color.white);
            slot.Icon.rectTransform.anchorMin = slot.Icon.rectTransform.anchorMax = new Vector2(0f, .5f);
            slot.Icon.rectTransform.pivot = new Vector2(0f, .5f);
            slot.Icon.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            slot.Icon.rectTransform.sizeDelta = new Vector2(70f, 70f);
            slot.Icon.preserveAspect = true;
            slot.Name = RuntimeUiFactory.Text("Name", slot.Root.transform, string.Empty, 22f, TextAlignmentOptions.Left);
            slot.Name.rectTransform.anchorMin = slot.Name.rectTransform.anchorMax = new Vector2(0f, .5f);
            slot.Name.rectTransform.pivot = new Vector2(0f, .5f);
            slot.Name.rectTransform.anchoredPosition = new Vector2(110f, 12f);
            slot.Name.rectTransform.sizeDelta = new Vector2(210f, 34f);
            slot.Level = RuntimeUiFactory.Text("Level", slot.Root.transform, string.Empty, 19f, TextAlignmentOptions.Left);
            slot.Level.rectTransform.anchorMin = slot.Level.rectTransform.anchorMax = new Vector2(0f, .5f);
            slot.Level.rectTransform.pivot = new Vector2(0f, .5f);
            slot.Level.rectTransform.anchoredPosition = new Vector2(110f, -22f);
            slot.Level.rectTransform.sizeDelta = new Vector2(210f, 28f);
            return slot;
        }

        private void PopulateSlot(Slot slot, WeaponSlotView weapon)
        {
            if (!string.IsNullOrEmpty(slot.WeaponId)) slotsByWeaponId.Remove(slot.WeaponId);
            slot.WeaponId = weapon.Id;
            if (!string.IsNullOrEmpty(slot.WeaponId)) slotsByWeaponId[slot.WeaponId] = slot;
            slot.Accent.color = JoseonUiPalette.WeaponAccent(new WeaponId(weapon.Id));
            slot.Icon.sprite = weapon.Icon;
            slot.Icon.enabled = weapon.Icon != null;
            slot.Name.text = weapon.DisplayName;
            slot.Level.text = $"LEVEL {weapon.Level}";
        }

        private static IEnumerator PulseRoutine(Slot slot)
        {
            var elapsed = 0f;
            while (elapsed < .24f)
            {
                elapsed += Time.unscaledDeltaTime;
                var pulse = 1f + Mathf.Sin(Mathf.Clamp01(elapsed / .24f) * Mathf.PI) * .12f;
                slot.Root.transform.localScale = Vector3.one * pulse;
                yield return null;
            }

            if (slot.Root != null) slot.Root.transform.localScale = Vector3.one;
        }
    }
}
