using System;
using System.Collections;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class WeaponAffixRevealPresenter : MonoBehaviour, IPointerClickHandler
    {
        private GameObject root;
        private CanvasGroup group;
        private Image frame;
        private Image burst;
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private readonly Image[] potentialCells = new Image[3];
        private readonly TextMeshProUGUI[] potentialLabels = new TextMeshProUGUI[3];
        private Coroutine routine;
        private WeaponAffixRollResult activeResult;
        private float elapsed;
        private float finishAt;
        private bool completed;

        public bool IsRevealing => routine != null;
        public WeaponAffixRollResult LastCompletedResult { get; private set; }
        public event Action RevealCompleted;

        public void Play(WeaponAffixRollResult result)
        {
            if (result == null) { HideImmediately(); return; }
            Build();
            HideImmediately();
            activeResult = result;
            completed = false;
            elapsed = 0f;
            finishAt = DurationFor(result);
            title.text = TierName(result.General.Tier) + " AFFINITY";
            detail.text = Describe(result.General);
            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            frame.sprite = catalog != null ? catalog.SpriteForAffix(result.General.Tier) : null;
            frame.enabled = frame.sprite != null;
            burst.sprite = catalog != null && result.NewPotentials.Count > 0 ? catalog.SpriteForAffix(WeaponAffixTier.Perfect) : null;
            burst.enabled = burst.sprite != null && result.NewPotentials.Count > 0;
            for (var i = 0; i < potentialCells.Length; i++)
            {
                var open = i < result.NewPotentials.Count;
                potentialCells[i].sprite = open && catalog != null ? catalog.SpriteForPotential(result.NewPotentials[i]) : null;
                potentialCells[i].enabled = potentialCells[i].sprite != null;
                potentialLabels[i].gameObject.SetActive(open);
                if (open) potentialLabels[i].text = PotentialName(result.NewPotentials[i]);
            }
            root.SetActive(true);
            routine = StartCoroutine(RevealRoutine());
        }

        public void Skip()
        {
            if (routine == null || completed) return;
            finishAt = Mathf.Min(finishAt, SkipCapFor(activeResult));
            if (elapsed >= finishAt) Complete();
        }

        public void HideImmediately()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            activeResult = null;
            completed = false;
            if (root != null) root.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData) => Skip();
        private void OnDisable() => HideImmediately();

        public static float DurationFor(WeaponAffixRollResult result)
        {
            if (result == null) return 0f;
            if (result.NewPotentials.Count > 0) return result.NewPotentials.Count == 1 ? 1.3f : result.NewPotentials.Count == 2 ? 1.6f : 1.9f;
            return result.General.Tier == WeaponAffixTier.High ? 1.15f : result.General.Tier == WeaponAffixTier.Perfect ? 1.35f : .95f;
        }

        private static float SkipCapFor(WeaponAffixRollResult result) => result != null && result.NewPotentials.Count >= 3 ? .7f : .3f;

        private IEnumerator RevealRoutine()
        {
            while (elapsed < finishAt)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / finishAt);
                group.alpha = Mathf.Clamp01(progress * 7f) * Mathf.Clamp01((1f - progress) * 7f);
                if (frame.enabled) frame.transform.localScale = Vector3.one * (1f + Mathf.Sin(progress * Mathf.PI) * .05f);
                yield return null;
            }
            Complete();
        }

        private void Complete()
        {
            if (completed) return;
            completed = true;
            LastCompletedResult = activeResult;
            routine = null;
            if (root != null) root.SetActive(false);
            RevealCompleted?.Invoke();
        }

        private void Build()
        {
            if (root != null) return;
            root = RuntimeUiFactory.Image("Weapon Affix Reveal", transform, new Color(.015f, .02f, .035f, .82f)).gameObject;
            RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            group = root.AddComponent<CanvasGroup>();
            var panel = RuntimeUiFactory.Image("Affix Reel", root.transform, JoseonUiPalette.Ink);
            var panelRect = panel.rectTransform; panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f,.5f); panelRect.sizeDelta = new Vector2(760f, 360f);
            frame = RuntimeUiFactory.Image("Rarity Frame", panel.transform, Color.white); RuntimeUiFactory.Stretch(frame.rectTransform, 12f, 12f, 12f, 12f); frame.preserveAspect = true;
            burst = RuntimeUiFactory.Image("Jackpot Burst", panel.transform, Color.white); burst.rectTransform.anchorMin = burst.rectTransform.anchorMax = new Vector2(.5f,.5f); burst.rectTransform.sizeDelta = new Vector2(180f,180f); burst.preserveAspect = true;
            title = Label("Affix Title", panel.transform, new Vector2(0f,104f), new Vector2(650f,48f), 34f, TextAlignmentOptions.Center);
            detail = Label("Affix Detail", panel.transform, new Vector2(0f,52f), new Vector2(650f,38f), 25f, TextAlignmentOptions.Center);
            for (var i=0;i<3;i++)
            {
                var cell = RuntimeUiFactory.Image("Potential Cell " + i, panel.transform, Color.white);
                cell.rectTransform.anchorMin=cell.rectTransform.anchorMax=new Vector2(.5f,.5f); cell.rectTransform.anchoredPosition=new Vector2(-180f+i*180f,-70f); cell.rectTransform.sizeDelta=new Vector2(72f,72f); cell.preserveAspect=true; potentialCells[i]=cell;
                potentialLabels[i]=Label("Potential Label " + i, panel.transform, new Vector2(-180f+i*180f,-130f), new Vector2(160f,36f), 16f, TextAlignmentOptions.Center);
            }
            root.SetActive(false);
        }

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 pos, Vector2 size, float font, TextAlignmentOptions align)
        { var label=RuntimeUiFactory.Text(name,parent,string.Empty,font,align); label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,.5f); label.rectTransform.anchoredPosition=pos; label.rectTransform.sizeDelta=size; return label; }
        private static string TierName(WeaponAffixTier tier) => tier == WeaponAffixTier.Perfect ? "PERFECT" : tier == WeaponAffixTier.High ? "HIGH" : "AFFIX";
        private static string Describe(WeaponAffixRoll roll) => roll.Stat + " +" + Mathf.RoundToInt((float)(roll.Value * 100d)) + "%";
        private static string PotentialName(WeaponPotentialId id) => id.Value.Replace('_', ' ').ToUpperInvariant();
    }
}
