using System;
using System.Collections;
using System.Collections.Generic;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public sealed class WeaponRackPresenter : MonoBehaviour
    {
        private const float SlotSize = 112f;
        private const float SlotGap = 18f;
        private const int Columns = 4;

        private sealed class Slot
        {
            public GameObject Root;
            public Image Border;
            public Image Icon;
            public Image[] PotentialCells;
            public string WeaponId;
            public Coroutine PulseRoutine;
            public Button Button;
            public WeaponSlotView View;
        }

        private readonly List<Slot> slots = new();
        private readonly Dictionary<string, Slot> slotsByWeaponId = new();
        private Sprite frameSprite;
        private WeaponAffixPresentationCatalogAsset affixCatalog;

        public event Action<WeaponSlotView> WeaponSelected;

        public void Render(IReadOnlyList<WeaponSlotView> weapons)
        {
            ResolveAssets();
            while (slots.Count > weapons.Count)
            {
                var slot = slots[^1];
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
            slot.Border.color = LevelBorder(newLevel);
            StopPulse(slot);
            slot.PulseRoutine = StartCoroutine(PulseRoutine(slot));
        }

        public void ResetPulses()
        {
            foreach (var slot in slots) StopPulse(slot);
        }

        private void OnDisable() => ResetPulses();

        private void ResolveAssets()
        {
            if (frameSprite == null) frameSprite = Resources.Load<Sprite>("UI/compact_weapon_slot");
            if (affixCatalog == null)
                affixCatalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
        }

        private Slot CreateSlot(int index)
        {
            var slot = new Slot();
            var rootImage = RuntimeUiFactory.Image("Weapon Slot " + index, transform, Color.clear);
            slot.Root = rootImage.gameObject;
            slot.Button = slot.Root.AddComponent<Button>();
            slot.Button.transition = Selectable.Transition.ColorTint;
            slot.Button.onClick.AddListener(() => WeaponSelected?.Invoke(slot.View));
            var rect = slot.Root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);

            slot.Border = RuntimeUiFactory.Image("Level Border", slot.Root.transform, Color.white);
            slot.Border.sprite = frameSprite;
            slot.Border.type = Image.Type.Sliced;
            RuntimeUiFactory.Stretch(slot.Border.rectTransform, 0f, 0f, 0f, 0f);
            slot.Button.targetGraphic = slot.Border;

            slot.Icon = RuntimeUiFactory.Image("Icon", slot.Root.transform, Color.white);
            slot.Icon.rectTransform.anchorMin = slot.Icon.rectTransform.anchorMax = new Vector2(.5f, .5f);
            slot.Icon.rectTransform.pivot = new Vector2(.5f, .5f);
            slot.Icon.rectTransform.anchoredPosition = new Vector2(0f, 5f);
            slot.Icon.rectTransform.sizeDelta = new Vector2(70f, 70f);
            slot.Icon.preserveAspect = true;
            slot.Icon.raycastTarget = false;

            slot.PotentialCells = new Image[3];
            for (var potentialIndex = 0; potentialIndex < slot.PotentialCells.Length; potentialIndex++)
            {
                var cell = RuntimeUiFactory.Image("Potential Cell " + potentialIndex, slot.Root.transform, Color.white);
                cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = new Vector2(.5f, 0f);
                cell.rectTransform.pivot = new Vector2(.5f, 0f);
                cell.rectTransform.anchoredPosition = new Vector2((potentialIndex - 1) * 24f, 5f);
                cell.rectTransform.sizeDelta = new Vector2(20f, 20f);
                cell.preserveAspect = true;
                cell.raycastTarget = false;
                cell.gameObject.SetActive(false);
                slot.PotentialCells[potentialIndex] = cell;
            }
            return slot;
        }

        public void ApplyPortraitLayout()
        {
            for (var index = 0; index < slots.Count; index++) LayoutSlot(slots[index], index, slots.Count);
        }

        private static void LayoutSlot(Slot slot, int index, int count)
        {
            if (slot.Root == null) return;
            var rect = slot.Root.GetComponent<RectTransform>();
            var row = index / Columns;
            var column = index % Columns;
            var rowStart = row * Columns;
            var rowCount = Mathf.Min(Columns, count - rowStart);
            var x = (column - (rowCount - 1) * .5f) * (SlotSize + SlotGap);
            rect.anchoredPosition = new Vector2(x,
                PortraitUiMetrics.BottomMargin + row * (SlotSize + SlotGap));
            rect.sizeDelta = new Vector2(SlotSize, SlotSize);
        }

        private void PopulateSlot(Slot slot, WeaponSlotView weapon)
        {
            if (!string.IsNullOrEmpty(slot.WeaponId)) slotsByWeaponId.Remove(slot.WeaponId);
            slot.WeaponId = weapon.Id;
            slot.View = weapon;
            if (!string.IsNullOrEmpty(slot.WeaponId)) slotsByWeaponId[slot.WeaponId] = slot;
            slot.Border.color = LevelBorder(weapon.Level);
            slot.Icon.sprite = weapon.Icon;
            slot.Icon.enabled = weapon.Icon != null;

            for (var index = 0; index < slot.PotentialCells.Length; index++)
            {
                var cell = slot.PotentialCells[index];
                var active = index < weapon.PotentialIds.Count;
                cell.sprite = active && affixCatalog != null
                    ? affixCatalog.SpriteForPotential(weapon.PotentialIds[index])
                    : null;
                cell.gameObject.SetActive(active && cell.sprite != null);
            }
        }

        private static Color LevelBorder(int level) => level switch
        {
            <= 1 => new Color(.72f, .68f, .58f, 1f),
            2 => new Color(.22f, .72f, .60f, 1f),
            3 => new Color(.25f, .48f, .90f, 1f),
            4 => new Color(.63f, .36f, .82f, 1f),
            _ => new Color(.90f, .65f, .20f, 1f)
        };

        private IEnumerator PulseRoutine(Slot slot)
        {
            var elapsed = 0f;
            while (elapsed < .24f)
            {
                elapsed += Time.unscaledDeltaTime;
                var pulse = 1f + Mathf.Sin(Mathf.Clamp01(elapsed / .24f) * Mathf.PI) * .12f;
                slot.Border.transform.localScale = Vector3.one * pulse;
                yield return null;
            }

            slot.PulseRoutine = null;
            if (slot.Border != null) slot.Border.transform.localScale = Vector3.one;
        }

        private void StopPulse(Slot slot)
        {
            if (slot.PulseRoutine != null) StopCoroutine(slot.PulseRoutine);
            slot.PulseRoutine = null;
            if (slot.Root != null) slot.Root.transform.localScale = Vector3.one;
            if (slot.Border != null) slot.Border.transform.localScale = Vector3.one;
        }
    }
}
