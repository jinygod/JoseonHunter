using System;
using System.Collections.Generic;
using System.Collections;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
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
            public TextMeshProUGUI Totals;
            public Image[] PotentialCells;
            public string WeaponId;
            public Coroutine PulseRoutine;
            public Button Button;
            public WeaponSlotView View;
        }

        private readonly List<Slot> slots = new();
        private readonly Dictionary<string, Slot> slotsByWeaponId = new();
        public event Action<WeaponSlotView> WeaponSelected;

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
            ApplyPortraitLayout();
        }

        public void Pulse(string weaponId, int newLevel, int newPotentialCount = 0)
        {
            if (string.IsNullOrEmpty(weaponId) || !slotsByWeaponId.TryGetValue(weaponId, out var slot)) return;
            slot.Level.text = $"레벨 {newLevel}";
            StopPulse(slot);
            slot.PulseRoutine = StartCoroutine(PulseRoutine(slot, newPotentialCount));
        }

        public void ResetPulses()
        {
            foreach (var slot in slots) StopPulse(slot);
        }

        private void OnDisable() => ResetPulses();

        private Slot CreateSlot(int index)
        {
            var slot = new Slot();
            slot.Root = RuntimeUiFactory.Image("Weapon Slot " + index, transform, JoseonUiPalette.Ink).gameObject;
            slot.Button = slot.Root.AddComponent<Button>();
            slot.Button.targetGraphic = slot.Root.GetComponent<Image>();
            slot.Button.transition = Selectable.Transition.ColorTint;
            slot.Button.onClick.AddListener(() => WeaponSelected?.Invoke(slot.View));
            var rect = slot.Root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            LayoutSlot(slot, index);
            slot.Accent = RuntimeUiFactory.Image("Accent", slot.Root.transform, JoseonUiPalette.Gold);
            var accentRect = slot.Accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.sizeDelta = new Vector2(6f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            slot.Icon = RuntimeUiFactory.Image("Icon", slot.Root.transform, Color.white);
            slot.Icon.rectTransform.anchorMin = slot.Icon.rectTransform.anchorMax = new Vector2(0f, .5f);
            slot.Icon.rectTransform.pivot = new Vector2(0f, .5f);
            slot.Icon.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            slot.Icon.rectTransform.sizeDelta = new Vector2(50f, 50f);
            slot.Icon.preserveAspect = true;
            slot.Name = RuntimeUiFactory.Text("Name", slot.Root.transform, string.Empty, 18f, TextAlignmentOptions.Left);
            slot.Name.rectTransform.anchorMin = slot.Name.rectTransform.anchorMax = new Vector2(0f, .5f);
            slot.Name.rectTransform.pivot = new Vector2(0f, .5f);
            slot.Name.rectTransform.anchoredPosition = new Vector2(78f, 15f);
            slot.Name.rectTransform.sizeDelta = new Vector2(224f, 26f);
            slot.Level = RuntimeUiFactory.Text("Level", slot.Root.transform, string.Empty, 14f, TextAlignmentOptions.Left);
            slot.Level.rectTransform.anchorMin = slot.Level.rectTransform.anchorMax = new Vector2(0f, .5f);
            slot.Level.rectTransform.pivot = new Vector2(0f, .5f);
            slot.Level.rectTransform.anchoredPosition = new Vector2(78f, -11f);
            slot.Level.rectTransform.sizeDelta = new Vector2(224f, 20f);
            slot.Totals = RuntimeUiFactory.Text("Affix Totals", slot.Root.transform, string.Empty, 11f, TextAlignmentOptions.Left);
            slot.Totals.rectTransform.anchorMin = slot.Totals.rectTransform.anchorMax = new Vector2(0f, .5f);
            slot.Totals.rectTransform.pivot = new Vector2(0f, .5f);
            slot.Totals.rectTransform.anchoredPosition = new Vector2(78f, -30f);
            slot.Totals.rectTransform.sizeDelta = new Vector2(224f, 16f);
            slot.PotentialCells = new Image[3];
            for (var potentialIndex = 0; potentialIndex < slot.PotentialCells.Length; potentialIndex++)
            {
                var cell = RuntimeUiFactory.Image("Potential Cell " + potentialIndex, slot.Root.transform, Color.white);
                cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = new Vector2(1f, .5f);
                cell.rectTransform.pivot = new Vector2(1f, .5f);
                cell.rectTransform.anchoredPosition = new Vector2(-12f - potentialIndex * 27f, 0f);
                cell.rectTransform.sizeDelta = new Vector2(15f, 15f);
                cell.preserveAspect = true;
                slot.PotentialCells[potentialIndex] = cell;
            }
            return slot;
        }

        public void ApplyPortraitLayout()
        {
            for (var index = 0; index < slots.Count; index++) LayoutSlot(slots[index], index);
        }

        private void LayoutSlot(Slot slot, int index)
        {
            if (slot.Root == null) return;
            var rect = slot.Root.GetComponent<RectTransform>();
            var rackRect = transform as RectTransform;
            var availableWidth = rackRect == null ? 0f : rackRect.rect.width;
            var width = availableWidth <= 0f ? PortraitUiMetrics.RackSlotWidth : Mathf.Min(
                PortraitUiMetrics.RackSlotWidth, Mathf.Max(0f, (availableWidth - 24f) * .5f));
            var column = index % 2;
            var row = index / 2;
            var x = (width + 24f) * .5f * (column == 0 ? -1f : 1f);
            rect.anchoredPosition = new Vector2(x, PortraitUiMetrics.BottomMargin + row *
                (PortraitUiMetrics.RackSlotHeight + 24f));
            rect.sizeDelta = new Vector2(width, PortraitUiMetrics.RackSlotHeight);
        }

        private void PopulateSlot(Slot slot, WeaponSlotView weapon)
        {
            if (!string.IsNullOrEmpty(slot.WeaponId)) slotsByWeaponId.Remove(slot.WeaponId);
            slot.WeaponId = weapon.Id;
            slot.View = weapon;
            if (!string.IsNullOrEmpty(slot.WeaponId)) slotsByWeaponId[slot.WeaponId] = slot;
            slot.Accent.color = JoseonUiPalette.WeaponAccent(new WeaponId(weapon.Id));
            slot.Icon.sprite = weapon.Icon;
            slot.Icon.enabled = weapon.Icon != null;
            slot.Name.text = weapon.DisplayName;
            slot.Level.text = $"레벨 {weapon.Level}";
            slot.Totals.text = weapon.GeneralAffixSummary;
            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            if (catalog == null || !catalog.HasRequiredUiSprites)
            {
                Debug.LogError("Weapon rack requires the imported PixelLab slot-kit catalog.", this);
                return;
            }
            for (var index = 0; index < slot.PotentialCells.Length; index++)
            {
                var cell = slot.PotentialCells[index];
                cell.sprite = index < weapon.PotentialIds.Count && catalog != null
                    ? catalog.SpriteForPotential(weapon.PotentialIds[index])
                    : catalog.EmptyLineFrame;
                cell.enabled = true;
            }
        }

        private IEnumerator PulseRoutine(Slot slot, int newPotentialCount)
        {
            var elapsed = 0f;
            while (elapsed < .24f)
            {
                elapsed += Time.unscaledDeltaTime;
                var pulse = 1f + Mathf.Sin(Mathf.Clamp01(elapsed / .24f) * Mathf.PI) * .12f;
                slot.Accent.transform.localScale = Vector3.one * pulse;
                var potentialIndex = newPotentialCount - 1;
                if (potentialIndex >= 0 && potentialIndex < slot.PotentialCells.Length)
                    slot.PotentialCells[potentialIndex].transform.localScale = Vector3.one * pulse;
                yield return null;
            }

            slot.PulseRoutine = null;
            if (slot.Root != null) slot.Root.transform.localScale = Vector3.one;
            if (slot.Accent != null) slot.Accent.transform.localScale = Vector3.one;
            if (slot.PotentialCells != null)
                foreach (var cell in slot.PotentialCells) if (cell != null) cell.transform.localScale = Vector3.one;
        }

        private void StopPulse(Slot slot)
        {
            if (slot.PulseRoutine != null) StopCoroutine(slot.PulseRoutine);
            slot.PulseRoutine = null;
            if (slot.Root != null) slot.Root.transform.localScale = Vector3.one;
            if (slot.Accent != null) slot.Accent.transform.localScale = Vector3.one;
            if (slot.PotentialCells != null)
                foreach (var cell in slot.PotentialCells) if (cell != null) cell.transform.localScale = Vector3.one;
        }
    }
}
