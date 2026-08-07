using System;
using System.Collections;
using System.Collections.Generic;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    public sealed class WeaponRackPresenter : MonoBehaviour
    {
        private const float SlotSize = 124f;
        private const float SlotGap = 16f;
        private const int Columns = 4;

        private sealed class Slot
        {
            public GameObject Root;
            public Image[] QualityFrameParts;
            public Image Icon;
            public TextMeshProUGUI LevelStars;
            public Image[] PotentialCells;
            public string WeaponId;
            public Coroutine PulseRoutine;
            public Button Button;
            public WeaponSlotView View;
        }

        private readonly List<Slot> slots = new();
        private readonly Dictionary<string, Slot> slotsByWeaponId = new();
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
            slot.Button.targetGraphic = rootImage;
            var rect = slot.Root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);

            CreateFrameEdge("Frame Shadow Top", slot.Root.transform, FrameSide.Top, 2f, 10f,
                new Color(.035f, .025f, .02f, .92f));
            CreateFrameEdge("Frame Shadow Bottom", slot.Root.transform, FrameSide.Bottom, 2f, 10f,
                new Color(.035f, .025f, .02f, .92f));
            CreateFrameEdge("Frame Shadow Left", slot.Root.transform, FrameSide.Left, 2f, 10f,
                new Color(.035f, .025f, .02f, .92f));
            CreateFrameEdge("Frame Shadow Right", slot.Root.transform, FrameSide.Right, 2f, 10f,
                new Color(.035f, .025f, .02f, .92f));

            slot.QualityFrameParts = new Image[8];
            slot.QualityFrameParts[0] = CreateFrameEdge("Quality Frame Top", slot.Root.transform,
                FrameSide.Top, 5f, 5f, Color.white);
            slot.QualityFrameParts[1] = CreateFrameEdge("Quality Frame Bottom", slot.Root.transform,
                FrameSide.Bottom, 5f, 5f, Color.white);
            slot.QualityFrameParts[2] = CreateFrameEdge("Quality Frame Left", slot.Root.transform,
                FrameSide.Left, 5f, 5f, Color.white);
            slot.QualityFrameParts[3] = CreateFrameEdge("Quality Frame Right", slot.Root.transform,
                FrameSide.Right, 5f, 5f, Color.white);
            slot.QualityFrameParts[4] = CreateCorner("Quality Corner 0", slot.Root.transform,
                new Vector2(0f, 1f), new Vector2(1f, -1f));
            slot.QualityFrameParts[5] = CreateCorner("Quality Corner 1", slot.Root.transform,
                Vector2.one, new Vector2(-1f, -1f));
            slot.QualityFrameParts[6] = CreateCorner("Quality Corner 2", slot.Root.transform,
                Vector2.zero, Vector2.one);
            slot.QualityFrameParts[7] = CreateCorner("Quality Corner 3", slot.Root.transform,
                new Vector2(1f, 0f), new Vector2(-1f, 1f));

            slot.Icon = RuntimeUiFactory.Image("Icon", slot.Root.transform, Color.white);
            slot.Icon.rectTransform.anchorMin = slot.Icon.rectTransform.anchorMax = new Vector2(.5f, .5f);
            slot.Icon.rectTransform.pivot = new Vector2(.5f, .5f);
            slot.Icon.rectTransform.anchoredPosition = new Vector2(0f, 9f);
            slot.Icon.rectTransform.sizeDelta = new Vector2(68f, 68f);
            slot.Icon.preserveAspect = true;
            slot.Icon.raycastTarget = false;

            slot.LevelStars = RuntimeUiFactory.Text("Level Stars", slot.Root.transform, string.Empty, 17f,
                TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            slot.LevelStars.color = JoseonUiPalette.Gold;
            slot.LevelStars.textWrappingMode = TextWrappingModes.NoWrap;
            slot.LevelStars.raycastTarget = false;
            slot.LevelStars.rectTransform.anchorMin = slot.LevelStars.rectTransform.anchorMax = new Vector2(.5f, 0f);
            slot.LevelStars.rectTransform.pivot = new Vector2(.5f, 0f);
            slot.LevelStars.rectTransform.anchoredPosition = new Vector2(0f, 5f);
            slot.LevelStars.rectTransform.sizeDelta = new Vector2(104f, 22f);

            slot.PotentialCells = new Image[3];
            for (var potentialIndex = 0; potentialIndex < slot.PotentialCells.Length; potentialIndex++)
            {
                var cell = RuntimeUiFactory.Image("Potential Cell " + potentialIndex, slot.Root.transform, Color.white);
                cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = Vector2.one;
                cell.rectTransform.pivot = Vector2.one;
                cell.rectTransform.anchoredPosition = new Vector2(-9f, -9f - potentialIndex * 21f);
                cell.rectTransform.sizeDelta = new Vector2(18f, 18f);
                cell.preserveAspect = true;
                cell.raycastTarget = false;
                cell.gameObject.SetActive(false);
                slot.PotentialCells[potentialIndex] = cell;
            }
            return slot;
        }

        private enum FrameSide
        {
            Top,
            Bottom,
            Left,
            Right
        }

        private static Image CreateFrameEdge(
            string name,
            Transform parent,
            FrameSide side,
            float inset,
            float thickness,
            Color color)
        {
            var image = RuntimeUiFactory.Image(name, parent, color);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            if (side == FrameSide.Top || side == FrameSide.Bottom)
            {
                var anchorY = side == FrameSide.Top ? 1f : 0f;
                rect.anchorMin = new Vector2(0f, anchorY);
                rect.anchorMax = new Vector2(1f, anchorY);
                rect.pivot = new Vector2(.5f, anchorY);
                rect.offsetMin = new Vector2(inset, side == FrameSide.Top ? -thickness - inset : inset);
                rect.offsetMax = new Vector2(-inset, side == FrameSide.Top ? -inset : thickness + inset);
            }
            else
            {
                var anchorX = side == FrameSide.Right ? 1f : 0f;
                rect.anchorMin = new Vector2(anchorX, 0f);
                rect.anchorMax = new Vector2(anchorX, 1f);
                rect.pivot = new Vector2(anchorX, .5f);
                rect.offsetMin = new Vector2(side == FrameSide.Right ? -thickness - inset : inset, inset);
                rect.offsetMax = new Vector2(side == FrameSide.Right ? -inset : thickness + inset, -inset);
            }
            return image;
        }

        private static Image CreateCorner(string name, Transform parent, Vector2 anchor, Vector2 inwardDirection)
        {
            var image = RuntimeUiFactory.Image(name, parent, Color.white);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(14f, 14f);
            rect.anchoredPosition = inwardDirection * 3f;
            return image;
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
            var qualityColor = ColorFor(WeaponAffixQuality.BandFor(
                WeaponAffixQuality.Score(weapon.GeneralAffixRolls)));
            for (var index = 0; index < slot.QualityFrameParts.Length; index++)
                slot.QualityFrameParts[index].color = qualityColor;
            slot.Icon.sprite = weapon.Icon;
            slot.Icon.enabled = weapon.Icon != null;
            slot.LevelStars.text = new string('★', Mathf.Clamp(weapon.Level, 1, 5));

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

        public static Color ColorFor(WeaponAffixQualityBand quality) => quality switch
        {
            WeaponAffixQualityBand.Green => new Color(.30f, .78f, .46f, 1f),
            WeaponAffixQualityBand.Blue => new Color(.30f, .58f, .96f, 1f),
            WeaponAffixQualityBand.Crimson => new Color(.82f, .28f, .28f, 1f),
            WeaponAffixQualityBand.Gold => new Color(1f, .72f, .18f, 1f),
            _ => new Color(.72f, .70f, .66f, 1f)
        };

        private IEnumerator PulseRoutine(Slot slot)
        {
            var elapsed = 0f;
            while (elapsed < .24f)
            {
                elapsed += Time.unscaledDeltaTime;
                var pulse = 1f + Mathf.Sin(Mathf.Clamp01(elapsed / .24f) * Mathf.PI) * .12f;
                slot.Root.transform.localScale = Vector3.one * pulse;
                yield return null;
            }

            slot.PulseRoutine = null;
            if (slot.Root != null) slot.Root.transform.localScale = Vector3.one;
        }

        private void StopPulse(Slot slot)
        {
            if (slot.PulseRoutine != null) StopCoroutine(slot.PulseRoutine);
            slot.PulseRoutine = null;
            if (slot.Root != null) slot.Root.transform.localScale = Vector3.one;
        }
    }
}
