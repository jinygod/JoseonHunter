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
        public enum RevealPhase
        {
            Hidden,
            Opening,
            Spinning,
            Stopping,
            Reading,
            Closing
        }

        private const int ReelCount = 4;
        private const float AppraisalWidth = PortraitUiMetrics.ModalWidth;
        private const float AppraisalHeight = 1320f;
        private static readonly Rect AppraisalScrollContentUv =
            new Rect(97f / 600f, 11f / 448f, 405f / 600f, 210f / 448f);
        private GameObject root;
        private CanvasGroup group;
        private RectTransform panelRect;
        private RectTransform scrollViewport;
        private Image shell;
        private Image topRoller;
        private Image bottomRoller;
        private Image rareStamp;
        private Image rarityFrame;
        private Image burst;
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private TextMeshProUGUI weaponName;
        private TextMeshProUGUI weaponLevel;
        private TextMeshProUGUI weaponBehavior;
        private TextMeshProUGUI accumulatedAffixSummary;
        private Image weaponIcon;
        private Button confirmButton;
        private TextMeshProUGUI confirmLabel;
        private readonly Image[] reelWindows = new Image[ReelCount];
        private readonly Image[,] spinningSymbols = new Image[ReelCount, 2];
        private readonly Image[] finalSymbols = new Image[ReelCount];
        private readonly Image[] lockedSlots = new Image[3];
        private readonly Image[] stopFlashes = new Image[ReelCount];
        private readonly TextMeshProUGUI[] potentialLabels = new TextMeshProUGUI[3];
        private Coroutine routine;
        private WeaponAffixRollResult activeResult;
        private WeaponAppraisalViewModel activeModel;
        private WeaponAffixPresentationCatalogAsset activeCatalog;
        private WeaponAffixRevealTimeline timeline;
        private WeaponAppraisalRevealProfile revealProfile;
        private float elapsed;
        private float finishAt;
        private float skipSourceElapsed;
        private float skipTargetElapsed;
        private float appraisalWidth = AppraisalWidth;
        private float appraisalHeight = AppraisalHeight;
        private bool skipActive;
        private bool confirmRequested;
        private bool completed;
        private bool readOnlyDetail;
        private WeaponAffixPresentationCatalogAsset catalogForTests;
        private Sprite croppedAppraisalScroll;
        private Sprite croppedAppraisalSource;

#if UNITY_INCLUDE_TESTS
        public void SetCatalogForTests(WeaponAffixPresentationCatalogAsset catalog) => catalogForTests = catalog;
#endif
#if UNITY_EDITOR
        public void PreviewAtForEditor(WeaponAffixRollResult result, float presentationTime)
        {
            Play(result);
            if (routine != null)
                StopCoroutine(routine);
            routine = null;
            UpdateVisualState(Mathf.Clamp(presentationTime, 0f, timeline.Duration));
        }
#endif

        public bool IsRevealing => routine != null;
        public WeaponAffixRollResult LastCompletedResult { get; private set; }
        public bool IsTensionActive => activeResult != null &&
            (activeResult.General.Tier != WeaponAffixTier.Standard || activeResult.NewPotentials.Count > 0);
        public float TensionScale { get; private set; } = 1f;
        public RevealPhase Phase { get; private set; } = RevealPhase.Hidden;
        public int VisiblePotentialCount { get; private set; }
        public bool IsFinalAffixVisible => finalSymbols[0] != null && finalSymbols[0].enabled;
        public bool IsAwaitingConfirmation => routine != null && Phase == RevealPhase.Reading;
        public bool IsDetailOpen => readOnlyDetail && root != null && root.activeSelf;
        public float DetailFontSize => detail == null ? 0f : detail.fontSize;
        public Vector2 PanelSize => panelRect == null ? Vector2.zero : panelRect.sizeDelta;
        public string DisplayedAffixText => detail == null ? string.Empty : detail.text;
        public string AccumulatedSummary => accumulatedAffixSummary == null
            ? string.Empty
            : accumulatedAffixSummary.text;
        public float ScrollOpenFraction { get; private set; } = 1f;
        public float PotentialRowY(int index) =>
            index < 0 || index >= 3 || reelWindows[index + 1] == null
                ? float.NegativeInfinity
                : reelWindows[index + 1].rectTransform.anchoredPosition.y;
        public event Action RevealCompleted;
        public event Action DetailClosed;

        public void Play(WeaponAffixRollResult result)
        {
            Play(WeaponAppraisalViewModel.ForResult(result));
        }

        public void Play(WeaponAppraisalViewModel model)
        {
            if (model?.Result == null)
            {
                HideImmediately();
                return;
            }

            Build();
            HideImmediately();
            activeModel = model;
            activeResult = model.Result;
            activeCatalog = catalogForTests ??
                Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            if (activeCatalog == null || !activeCatalog.HasRequiredUiSprites)
            {
                Debug.LogError("Weapon affix reveal requires the imported PixelLab micro-slot catalog.", this);
                LastCompletedResult = activeResult;
                RevealCompleted?.Invoke();
                return;
            }

            completed = false;
            elapsed = 0f;
            skipActive = false;
            confirmRequested = false;
            revealProfile = WeaponAppraisalPresentation.ProfileFor(activeModel);
            timeline = WeaponAffixRevealTimeline.For(activeModel);
            finishAt = timeline.Duration;
            TensionScale = 1f;
            VisiblePotentialCount = 0;

            BindSprites();
            ResetVisuals();
            root.SetActive(true);
            routine = StartCoroutine(RevealRoutine());
        }

        public void Skip()
        {
            if (routine == null || completed || skipActive)
                return;
            if (Phase == RevealPhase.Reading)
            {
                Confirm();
                return;
            }
            skipActive = true;
            skipSourceElapsed = elapsed;
            skipTargetElapsed = timeline.SkipFinishAt(elapsed);
            finishAt = skipTargetElapsed;
        }

        public void Confirm()
        {
            if (routine == null || completed || Phase != RevealPhase.Reading)
                return;
            confirmRequested = true;
            if (confirmButton != null)
                confirmButton.interactable = false;
        }

        public void ShowDetails(WeaponSlotView weapon)
        {
            Build();
            HideImmediately();
            activeCatalog = catalogForTests ??
                Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            if (activeCatalog == null || !activeCatalog.HasRequiredUiSprites)
            {
                Debug.LogError("Weapon details require the imported PixelLab appraisal catalog.", this);
                return;
            }

            readOnlyDetail = true;
            BindAppraisalScroll(activeCatalog.AppraisalScroll);
            topRoller.sprite = activeCatalog.AppraisalRoller;
            bottomRoller.sprite = activeCatalog.AppraisalRoller;
            topRoller.enabled = false;
            bottomRoller.enabled = false;
            rareStamp.enabled = false;
            confirmButton.image.sprite = activeCatalog.ReelWindow;
            confirmButton.image.preserveAspect = false;
            SetScrollOpen(1f);
            weaponIcon.sprite = weapon.Icon != null ? weapon.Icon : activeCatalog.ReelSymbolStat;
            weaponIcon.enabled = weaponIcon.sprite != null;
            weaponName.text = weapon.DisplayName;
            weaponLevel.text = $"LEVEL {weapon.Level} · 현재 무기";
            weaponBehavior.text = weapon.Behavior;
            title.text = "현재 추가옵션";
            detail.text = string.IsNullOrEmpty(weapon.GeneralAffixSummary)
                ? "추가옵션 없음"
                : weapon.GeneralAffixSummary;
            accumulatedAffixSummary.text = string.Empty;
            rarityFrame.enabled = false;
            finalSymbols[0].enabled = false;
            burst.enabled = false;
            for (var reel = 0; reel < ReelCount; reel++)
            {
                reelWindows[reel].sprite = activeCatalog.ReelWindow;
                reelWindows[reel].enabled = reelWindows[reel].sprite != null;
                stopFlashes[reel].enabled = false;
                for (var symbol = 0; symbol < 2; symbol++)
                    spinningSymbols[reel, symbol].enabled = false;
            }
            for (var index = 0; index < 3; index++)
            {
                var hasPotential = index < weapon.PotentialIds.Count;
                var potential = hasPotential ? weapon.PotentialIds[index] : default;
                lockedSlots[index].sprite = activeCatalog.LockedPotentialSlot;
                lockedSlots[index].enabled = !hasPotential;
                finalSymbols[index + 1].sprite = hasPotential
                    ? activeCatalog.SpriteForPotential(potential)
                    : null;
                finalSymbols[index + 1].enabled = hasPotential;
                potentialLabels[index].text = hasPotential
                    ? PotentialName(potential) + "\n" + PotentialDescription(potential)
                    : $"잠재 능력 슬롯 {index + 1} · 잠김";
                potentialLabels[index].gameObject.SetActive(true);
                reelWindows[index + 1].rectTransform.anchoredPosition = PotentialRowPosition(index);
            }

            group.alpha = 1f;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localScale = Vector3.one;
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = true;
            root.SetActive(true);
            Phase = RevealPhase.Reading;
        }

        public void HideImmediately()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = null;
            activeResult = null;
            activeModel = null;
            readOnlyDetail = false;
            activeCatalog = null;
            completed = false;
            skipActive = false;
            confirmRequested = false;
            Phase = RevealPhase.Hidden;
            VisiblePotentialCount = 0;
            if (root != null)
                root.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (readOnlyDetail)
            {
                DismissDetails();
                return;
            }
            if (Phase == RevealPhase.Reading)
                Confirm();
            else
                Skip();
        }
        private void OnDisable() => HideImmediately();

        private void OnDestroy()
        {
            HideImmediately();
            ReleaseCroppedAppraisalScroll();
        }

        public static float DurationFor(WeaponAffixRollResult result) =>
            WeaponAffixRevealTimeline.For(result).Duration;

        private IEnumerator RevealRoutine()
        {
            // Keep the fully composed starting pose for one frame so the scroll
            // visibly begins closed instead of skipping ahead on a slow frame.
            yield return null;
            while (elapsed < finishAt)
            {
                elapsed += Time.unscaledDeltaTime;
                UpdateVisualState(PresentationTime());
                yield return null;
            }

            UpdateVisualState(timeline.Duration);
            while (!confirmRequested)
                yield return null;

            Phase = RevealPhase.Closing;
            var closeElapsed = 0f;
            const float closeDuration = .14f;
            while (closeElapsed < closeDuration)
            {
                closeElapsed += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(closeElapsed / closeDuration);
                yield return null;
            }
            Complete();
        }

        private float PresentationTime()
        {
            if (!skipActive)
                return elapsed;
            var compressedDuration = Mathf.Max(.001f, skipTargetElapsed - skipSourceElapsed);
            var progress = Mathf.Clamp01((elapsed - skipSourceElapsed) / compressedDuration);
            return Mathf.Lerp(skipSourceElapsed, timeline.Duration, EaseOutCubic(progress));
        }

        private void UpdateVisualState(float time)
        {
            Phase = PhaseAt(time);
            SetScrollOpen(WeaponAppraisalPresentation.ScrollOpenAt(revealProfile, time));
            var opening = Mathf.Clamp01(time / .10f);
            group.alpha = Phase == RevealPhase.Opening ? EaseOutCubic(opening) : 1f;

            var openingScale = Mathf.Lerp(.94f, 1f, EaseOutBack(opening));
            TensionScale = TensionScaleAt(time);
            var shake = JackpotShakeAt(time);
            panelRect.anchoredPosition = shake;
            panelRect.localScale = Vector3.one * (Phase == RevealPhase.Opening ? openingScale : TensionScale);

            UpdateSpinningSymbols(time);
            SetFinalAffixVisible(time >= timeline.AffixStopsAt);
            var countProgress = Mathf.InverseLerp(.10f, timeline.AffixStopsAt, time);
            detail.text = WeaponAffixValueFormatter.Describe(
                activeResult.General,
                WeaponAppraisalPresentation.DisplayValueAt(activeResult.General.Value, countProgress));
            VisiblePotentialCount = 0;
            for (var index = 0; index < 3; index++)
            {
                var existingCount = activeModel?.ExistingPotentialCount ?? 0;
                var awardedIndex = index - existingCount;
                var existing = index < existingCount;
                var awarded = awardedIndex >= 0 && awardedIndex < activeResult.NewPotentials.Count;
                var opened = awarded && time >= timeline.PotentialStopsAt(awardedIndex);
                lockedSlots[index].enabled = !existing && !opened;
                finalSymbols[index + 1].enabled = existing || opened;
                potentialLabels[index].gameObject.SetActive(existing || opened || time >= timeline.ReadStartsAt);
                if (opened)
                    VisiblePotentialCount++;
                var slotState = WeaponAppraisalPresentation.ResolveSlot(
                    index, existingCount, activeResult.NewPotentials.Count, time, timeline);
                var rowShake = slotState == WeaponPotentialSlotKind.Shaking
                    ? Mathf.Sin(time * 56f + index * 1.7f) *
                      Mathf.Clamp01((timeline.ReadStartsAt - time) / .28f) * 6f
                    : 0f;
                reelWindows[index + 1].rectTransform.anchoredPosition =
                    PotentialRowPosition(index) + new Vector2(rowShake, 0f);
                UpdateStopFlash(index + 1, time,
                    awarded ? timeline.PotentialStopsAt(awardedIndex) : float.PositiveInfinity, opened);
            }

            UpdateStopFlash(0, time, timeline.AffixStopsAt, IsFinalAffixVisible);
            // The imported ritual seal includes an opaque white square and the
            // decorative stamp reads as a detached slot symbol at portrait scale.
            burst.enabled = false;
            rareStamp.enabled = false;

            var canConfirm = Phase == RevealPhase.Reading;
            confirmButton.gameObject.SetActive(canConfirm);
            confirmButton.interactable = canConfirm && !confirmRequested;
        }

        private RevealPhase PhaseAt(float time)
        {
            if (time < .10f) return RevealPhase.Opening;
            if (time < timeline.SpinEndsAt) return RevealPhase.Spinning;
            if (time < timeline.ReadStartsAt) return RevealPhase.Stopping;
            if (time < timeline.CloseStartsAt) return RevealPhase.Reading;
            return RevealPhase.Closing;
        }

        private void UpdateSpinningSymbols(float time)
        {
            for (var reel = 0; reel < ReelCount; reel++)
                for (var symbolIndex = 0; symbolIndex < 2; symbolIndex++)
                    spinningSymbols[reel, symbolIndex].enabled = false;
        }

        private void SetFinalAffixVisible(bool visible)
        {
            finalSymbols[0].enabled = visible;
            rarityFrame.enabled = visible;
            title.gameObject.SetActive(true);
            detail.gameObject.SetActive(true);
        }

        private void UpdateStopFlash(int reelIndex, float time, float stopAt, bool canShow)
        {
            if (!canShow || float.IsPositiveInfinity(stopAt))
            {
                stopFlashes[reelIndex].enabled = false;
                return;
            }

            var age = time - stopAt;
            var visible = age >= 0f && age <= .14f;
            stopFlashes[reelIndex].enabled = visible;
            if (!visible)
                return;
            var progress = Mathf.Clamp01(age / .14f);
            stopFlashes[reelIndex].color = new Color(1f, 1f, 1f, 1f - progress);
            stopFlashes[reelIndex].rectTransform.localScale =
                Vector3.one * Mathf.Lerp(.65f, 1.25f, EaseOutCubic(progress));
            finalSymbols[reelIndex].rectTransform.localScale =
                Vector3.one * (progress < .55f
                    ? Mathf.Lerp(.92f, 1.08f, progress / .55f)
                    : Mathf.Lerp(1.08f, 1f, (progress - .55f) / .45f));
        }

        private float TensionScaleAt(float time)
        {
            if (!IsTensionActive || time < timeline.ReadStartsAt || time > timeline.ReadStartsAt + .22f)
                return 1f;
            var progress = Mathf.InverseLerp(timeline.ReadStartsAt, timeline.ReadStartsAt + .22f, time);
            return 1f + Mathf.Sin(progress * Mathf.PI) * .045f;
        }

        private void SetScrollOpen(float fraction)
        {
            ScrollOpenFraction = Mathf.Clamp01(fraction);
            if (scrollViewport == null)
                return;
            var visibleHeight = Mathf.Max(8f, appraisalHeight * ScrollOpenFraction);
            scrollViewport.sizeDelta = new Vector2(appraisalWidth, visibleHeight);
            var rollerY = Mathf.Max(0f, visibleHeight * .5f - 16f);
            if (topRoller != null)
                topRoller.rectTransform.anchoredPosition = new Vector2(0f, rollerY);
            if (bottomRoller != null)
                bottomRoller.rectTransform.anchoredPosition = new Vector2(0f, -rollerY);
        }

        private Vector2 JackpotShakeAt(float time)
        {
            if (activeResult.NewPotentials.Count == 0 || time < timeline.ReadStartsAt ||
                time > timeline.ReadStartsAt + .18f)
                return Vector2.zero;
            var strength = activeResult.NewPotentials.Count * 1.6f *
                (1f - Mathf.InverseLerp(timeline.ReadStartsAt, timeline.ReadStartsAt + .18f, time));
            return new Vector2(Mathf.Sin(time * 92f), Mathf.Cos(time * 76f)) * strength;
        }

        private void BindSprites()
        {
            BindAppraisalScroll(activeCatalog.AppraisalScroll);
            topRoller.sprite = activeCatalog.AppraisalRoller;
            bottomRoller.sprite = activeCatalog.AppraisalRoller;
            topRoller.enabled = false;
            bottomRoller.enabled = false;
            rareStamp.sprite = activeCatalog.RareAppraisalStamp;
            confirmButton.image.sprite = activeCatalog.ReelWindow;
            rarityFrame.sprite = activeCatalog.SpriteForAffix(activeResult.General.Tier);
            finalSymbols[0].sprite = activeResult.General.Tier == WeaponAffixTier.Standard
                ? activeCatalog.ReelSymbolStat
                : activeCatalog.ReelSymbolRarity;
            for (var reel = 0; reel < ReelCount; reel++)
            {
                reelWindows[reel].sprite = activeCatalog.ReelWindow;
                reelWindows[reel].enabled = reelWindows[reel].sprite != null;
                stopFlashes[reel].sprite = activeCatalog.ReelStopFlash;
                var spinSprite = reel == 0
                    ? activeResult.General.Tier == WeaponAffixTier.Standard
                        ? activeCatalog.ReelSymbolStat
                        : activeCatalog.ReelSymbolRarity
                    : activeCatalog.ReelSymbolPotential;
                spinningSymbols[reel, 0].sprite = spinSprite;
                spinningSymbols[reel, 1].sprite = reel == 0
                    ? activeCatalog.ReelSymbolRarity
                    : activeCatalog.ReelSymbolStat;
            }

            for (var index = 0; index < 3; index++)
            {
                lockedSlots[index].sprite = activeCatalog.LockedPotentialSlot;
                var potential = index < activeModel.CurrentPotentials.Count
                    ? activeModel.CurrentPotentials[index]
                    : default;
                var hasPotential = !string.IsNullOrEmpty(potential.Value);
                finalSymbols[index + 1].sprite = hasPotential
                    ? activeCatalog.SpriteForPotential(potential)
                    : null;
                potentialLabels[index].text = hasPotential
                    ? PotentialName(potential) + "\n" + PotentialDescription(potential)
                    : $"잠재 능력 슬롯 {index + 1} · 잠김";
            }

            burst.sprite = activeResult.NewPotentials.Count > 0
                ? activeCatalog.PotentialRitualSeal
                : null;
            title.text = TierName(activeResult.General.Tier);
            detail.text = WeaponAffixValueFormatter.Describe(activeResult.General, 0);
            weaponName.text = activeModel.DisplayName;
            weaponLevel.text = activeModel.IsNewAcquisition
                ? $"LEVEL {activeModel.Level} · 신규 무기"
                : $"LEVEL {activeModel.Level} · 강화 감정";
            weaponBehavior.text = activeModel.Behavior;
            accumulatedAffixSummary.text = string.IsNullOrEmpty(activeModel.AccumulatedAffixSummary)
                ? string.Empty
                : "누적  " + activeModel.AccumulatedAffixSummary;
            weaponIcon.sprite = activeModel.Icon != null ? activeModel.Icon : activeCatalog.ReelSymbolStat;
            weaponIcon.enabled = weaponIcon.sprite != null;
        }

        private void ResetVisuals()
        {
            group.alpha = 0f;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localScale = Vector3.one * .94f;
            rarityFrame.enabled = false;
            burst.enabled = false;
            rareStamp.enabled = false;
            SetScrollOpen(WeaponAppraisalPresentation.ScrollOpenAt(revealProfile, 0f));
            title.gameObject.SetActive(true);
            detail.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
            for (var reel = 0; reel < ReelCount; reel++)
            {
                finalSymbols[reel].enabled = false;
                stopFlashes[reel].enabled = false;
                for (var symbol = 0; symbol < 2; symbol++)
                    spinningSymbols[reel, symbol].enabled = false;
            }
            for (var index = 0; index < 3; index++)
            {
                lockedSlots[index].enabled = true;
                potentialLabels[index].gameObject.SetActive(false);
            }
        }

        private void Complete()
        {
            if (completed)
                return;
            completed = true;
            TensionScale = 1f;
            LastCompletedResult = activeResult;
            routine = null;
            Phase = RevealPhase.Hidden;
            if (root != null)
                root.SetActive(false);
            RevealCompleted?.Invoke();
        }

        private void Build()
        {
            if (root != null && finalSymbols[0] != null)
                return;
            if (root != null)
            {
                Destroy(root);
                root = null;
            }

            root = RuntimeUiFactory.Image("Weapon Appraisal Overlay", transform,
                new Color(.008f, .012f, .022f, .90f)).gameObject;
            RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            group = root.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;

            panelRect = RuntimeUiFactory.Rect("Weapon Appraisal Panel", root.transform);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.sizeDelta = new Vector2(AppraisalWidth, AppraisalHeight);

            scrollViewport = RuntimeUiFactory.Rect("Scroll Reveal Viewport", panelRect);
            scrollViewport.anchorMin = scrollViewport.anchorMax = new Vector2(.5f, .5f);
            scrollViewport.sizeDelta = panelRect.sizeDelta;
            scrollViewport.gameObject.AddComponent<RectMask2D>();

            shell = RuntimeUiFactory.Image("PixelLab Appraisal Sheet", scrollViewport, Color.white);
            shell.rectTransform.anchorMin = shell.rectTransform.anchorMax = new Vector2(.5f, .5f);
            shell.rectTransform.sizeDelta = panelRect.sizeDelta;
            shell.preserveAspect = false;

            topRoller = RuntimeUiFactory.Image("Top Scroll Roller", panelRect, Color.white);
            topRoller.rectTransform.anchorMin = topRoller.rectTransform.anchorMax = new Vector2(.5f, .5f);
            topRoller.rectTransform.sizeDelta = new Vector2(896f, 82f);
            topRoller.preserveAspect = false;
            bottomRoller = RuntimeUiFactory.Image("Bottom Scroll Roller", panelRect, Color.white);
            bottomRoller.rectTransform.anchorMin = bottomRoller.rectTransform.anchorMax = new Vector2(.5f, .5f);
            bottomRoller.rectTransform.sizeDelta = new Vector2(896f, 82f);
            bottomRoller.preserveAspect = false;

            weaponIcon = RuntimeUiFactory.Image("Weapon Icon", shell.transform, Color.white);
            weaponIcon.rectTransform.anchorMin = weaponIcon.rectTransform.anchorMax = new Vector2(.5f, .5f);
            weaponIcon.rectTransform.anchoredPosition = new Vector2(-330f, 270f);
            weaponIcon.rectTransform.sizeDelta = new Vector2(132f, 132f);
            weaponIcon.preserveAspect = true;
            weaponName = Label("Weapon Name", shell.transform, new Vector2(80f, 318f),
                new Vector2(620f, 48f), 34f, TextAlignmentOptions.Left);
            weaponName.fontStyle = FontStyles.Bold;
            weaponName.color = new Color(.08f, .15f, .18f);
            weaponLevel = Label("Weapon Level", shell.transform, new Vector2(80f, 276f),
                new Vector2(620f, 30f), 21f, TextAlignmentOptions.Left);
            weaponLevel.color = new Color(.42f, .23f, .08f);
            weaponBehavior = Label("Weapon Behavior", shell.transform, new Vector2(80f, 236f),
                new Vector2(620f, 42f), 20f, TextAlignmentOptions.Left);
            weaponBehavior.color = new Color(.24f, .27f, .24f);

            burst = RuntimeUiFactory.Image("Jackpot Burst", shell.transform, Color.white);
            burst.rectTransform.anchorMin = burst.rectTransform.anchorMax = new Vector2(.5f, .5f);
            burst.rectTransform.anchoredPosition = new Vector2(0f, -150f);
            burst.rectTransform.sizeDelta = new Vector2(920f, 430f);
            burst.preserveAspect = true;
            burst.transform.SetAsFirstSibling();

            rareStamp = RuntimeUiFactory.Image("Rare Appraisal Stamp", shell.transform, Color.white);
            rareStamp.rectTransform.anchorMin = rareStamp.rectTransform.anchorMax = new Vector2(.5f, .5f);
            rareStamp.rectTransform.anchoredPosition = new Vector2(402f, 266f);
            rareStamp.rectTransform.sizeDelta = new Vector2(108f, 108f);
            rareStamp.preserveAspect = true;

            title = Label("Affix Title", shell.transform, new Vector2(80f, 158f),
                new Vector2(600f, 36f), 24f, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            title.color = JoseonUiPalette.Gold;
            detail = Label("Affix Detail", shell.transform, new Vector2(80f, 105f),
                new Vector2(600f, 62f), 42f, TextAlignmentOptions.Left);
            detail.fontStyle = FontStyles.Bold;
            detail.color = JoseonUiPalette.Hanji;
            accumulatedAffixSummary = Label("Accumulated Affix Summary", shell.transform,
                new Vector2(-80f, 60f), new Vector2(740f, 28f), 18f,
                TextAlignmentOptions.Left);
            accumulatedAffixSummary.fontStyle = FontStyles.Bold;
            accumulatedAffixSummary.color = new Color(.12f, .28f, .28f);

            BuildReel(0, new Vector2(0f, 126f), new Vector2(820f, 128f), new Vector2(92f, 82f));
            rarityFrame = RuntimeUiFactory.Image("Affix Rarity Frame", finalSymbols[0].transform, Color.white);
            RuntimeUiFactory.Stretch(rarityFrame.rectTransform, -12f, -12f, -12f, -12f);
            rarityFrame.preserveAspect = true;
            rarityFrame.transform.SetAsFirstSibling();

            for (var index = 0; index < 3; index++)
            {
                var position = PotentialRowPosition(index);
                BuildReel(index + 1, position, new Vector2(820f, 108f), new Vector2(92f, 72f));
                lockedSlots[index] = RuntimeUiFactory.Image("Locked Potential " + index,
                    finalSymbols[index + 1].transform.parent, Color.white);
                RuntimeUiFactory.Stretch(lockedSlots[index].rectTransform, 8f, 6f, 8f, 6f);
                lockedSlots[index].preserveAspect = false;
                potentialLabels[index] = Label("Potential Label " + index, shell.transform,
                    position + new Vector2(80f, 0f), new Vector2(600f, 48f), 23f,
                    TextAlignmentOptions.Left);
                potentialLabels[index].fontStyle = FontStyles.Bold;
                potentialLabels[index].color = JoseonUiPalette.Hanji;
            }

            confirmButton = RuntimeUiFactory.Button("Confirm Result", shell.transform, Color.white);
            var confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = confirmRect.anchorMax = new Vector2(.5f, .5f);
            confirmRect.anchoredPosition = new Vector2(0f, -345f);
            confirmRect.sizeDelta = new Vector2(310f, 62f);
            confirmButton.image.preserveAspect = false;
            confirmButton.onClick.AddListener(OnConfirmButton);
            confirmLabel = RuntimeUiFactory.Text("Confirm Label", confirmButton.transform, "확인  ·  계속", 21f,
                TextAlignmentOptions.Center);
            RuntimeUiFactory.Stretch(confirmLabel.rectTransform, 12f, 5f, 12f, 5f);
            confirmLabel.fontStyle = FontStyles.Bold;
            confirmLabel.color = new Color(1f, .91f, .58f);

            title.transform.SetAsLastSibling();
            detail.transform.SetAsLastSibling();
            accumulatedAffixSummary.transform.SetAsLastSibling();
            for (var index = 0; index < potentialLabels.Length; index++)
                potentialLabels[index].transform.SetAsLastSibling();
            confirmButton.transform.SetAsLastSibling();

            ApplyPortraitLayout();
            root.SetActive(false);
        }

        public void ApplyPortraitLayout()
        {
            if (panelRect == null) return;
            appraisalWidth = PortraitUiMetrics.ContainedWidth(transform as RectTransform, AppraisalWidth);
            var parentRect = transform as RectTransform;
            appraisalHeight = PortraitUiMetrics.ContainedWidth(parentRect == null ? 0f : parentRect.rect.height,
                AppraisalHeight);
            panelRect.sizeDelta = new Vector2(appraisalWidth, appraisalHeight);
            if (shell != null) shell.rectTransform.sizeDelta = panelRect.sizeDelta;
            SetScrollOpen(ScrollOpenFraction);
        }

        private void OnConfirmButton()
        {
            if (readOnlyDetail)
                DismissDetails();
            else
                Confirm();
        }

        private void DismissDetails()
        {
            if (!readOnlyDetail) return;
            HideImmediately();
            DetailClosed?.Invoke();
        }

        private static Vector2 PotentialRowPosition(int index) =>
            new Vector2(0f, -20f - Mathf.Clamp(index, 0, 2) * 128f);

        private void BuildReel(int index, Vector2 position, Vector2 windowSize, Vector2 symbolSize)
        {
            var window = RuntimeUiFactory.Image("Reel Window " + index, shell.transform, Color.white);
            window.rectTransform.anchorMin = window.rectTransform.anchorMax = new Vector2(.5f, .5f);
            window.rectTransform.anchoredPosition = position;
            window.rectTransform.sizeDelta = windowSize;
            window.preserveAspect = false;
            reelWindows[index] = window;

            var viewport = RuntimeUiFactory.Image("Reel Viewport " + index, window.transform,
                new Color(1f, 1f, 1f, .001f));
            viewport.rectTransform.anchorMin = viewport.rectTransform.anchorMax = new Vector2(.5f, .5f);
            viewport.rectTransform.anchoredPosition = new Vector2(-windowSize.x * .5f + 72f, 0f);
            viewport.rectTransform.sizeDelta = new Vector2(112f, Mathf.Min(windowSize.y - 20f, 92f));
            viewport.gameObject.AddComponent<RectMask2D>();

            for (var symbolIndex = 0; symbolIndex < 2; symbolIndex++)
            {
                var spin = RuntimeUiFactory.Image("Spin Symbol " + index + "-" + symbolIndex,
                    viewport.transform, Color.white);
                spin.rectTransform.anchorMin = spin.rectTransform.anchorMax = new Vector2(.5f, .5f);
                spin.rectTransform.sizeDelta = symbolSize;
                spin.preserveAspect = true;
                spinningSymbols[index, symbolIndex] = spin;
            }

            var final = RuntimeUiFactory.Image("Final Symbol " + index, viewport.transform, Color.white);
            final.rectTransform.anchorMin = final.rectTransform.anchorMax = new Vector2(.5f, .5f);
            final.rectTransform.sizeDelta = symbolSize;
            final.preserveAspect = true;
            finalSymbols[index] = final;

            var flash = RuntimeUiFactory.Image("Stop Flash " + index, window.transform, Color.white);
            flash.rectTransform.anchorMin = flash.rectTransform.anchorMax = new Vector2(.5f, .5f);
            flash.rectTransform.sizeDelta = windowSize + new Vector2(26f, 26f);
            flash.preserveAspect = true;
            stopFlashes[index] = flash;
        }

        private void BindAppraisalScroll(Sprite source)
        {
            if (source == null)
            {
                ReleaseCroppedAppraisalScroll();
                shell.sprite = null;
                return;
            }

            if (croppedAppraisalSource == source && croppedAppraisalScroll != null)
            {
                shell.sprite = croppedAppraisalScroll;
                return;
            }

            ReleaseCroppedAppraisalScroll();
            croppedAppraisalSource = source;
            var sourceRect = source.textureRect;
            if (sourceRect.width < 64f || sourceRect.height < 64f)
            {
                shell.sprite = source;
                return;
            }

            var contentRect = new Rect(
                sourceRect.x + sourceRect.width * AppraisalScrollContentUv.x,
                sourceRect.y + sourceRect.height * AppraisalScrollContentUv.y,
                sourceRect.width * AppraisalScrollContentUv.width,
                sourceRect.height * AppraisalScrollContentUv.height);
            croppedAppraisalScroll = Sprite.Create(source.texture, contentRect, new Vector2(.5f, .5f),
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            croppedAppraisalScroll.name = source.name + " Content";
            shell.sprite = croppedAppraisalScroll;
        }

        private void ReleaseCroppedAppraisalScroll()
        {
            croppedAppraisalSource = null;
            if (croppedAppraisalScroll == null) return;
            if (Application.isPlaying)
                Destroy(croppedAppraisalScroll);
            else
                DestroyImmediate(croppedAppraisalScroll);
            croppedAppraisalScroll = null;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 position,
            Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var label = RuntimeUiFactory.Text(name, parent, string.Empty, fontSize, alignment);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = size;
            return label;
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseOutBack(float value)
        {
            value = Mathf.Clamp01(value);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(value - 1f, 3f) + c1 * Mathf.Pow(value - 1f, 2f);
        }

        private static string TierName(WeaponAffixTier tier) =>
            tier == WeaponAffixTier.Perfect ? "완벽한 추가옵션" :
            tier == WeaponAffixTier.High ? "높은 추가옵션" : "추가옵션";

        private static string PotentialName(WeaponPotentialId id)
        {
            switch (id.Value)
            {
                case "hwando_venom_fang": return "독니";
                case "hwando_returning_afterimage": return "회귀잔영";
                case "hwando_flying_blade_dance": return "비검난무";
                case "gakgung_armor_break_arrowhead": return "파갑촉";
                case "gakgung_split_fletching": return "갈래깃";
                case "gakgung_full_draw": return "만력개궁";
                case "talisman_five_element_cycle": return "오행순환";
                case "talisman_seal_transfer": return "주인전이";
                case "talisman_vengeful_ghost_burst": return "원귀폭발";
                case "thunder_earth_current": return "지맥전류";
                case "thunder_overcharged_core": return "과충전핵";
                case "thunder_lightning_rod": return "뇌침표식";
                case "jangseung_ghost_face": return "귀면장승";
                case "jangseung_four_direction_barrier": return "사방결계";
                case "jangseung_guardian_descent": return "수호신강림";
                case "singijeon_powder_trail": return "화약궤적";
                case "singijeon_submunition_split": return "자탄분열";
                case "singijeon_chain_ignition": return "연쇄점화";
                case "frost_crack_mark": return "균열표식";
                case "frost_spread": return "서리전염";
                case "frost_mist": return "빙무";
                case "fan_vacuum_edge": return "진공날";
                case "fan_distant_thunder": return "원뢰증폭";
                case "fan_returning_chain": return "회천연쇄";
                default: return id.Value.Replace('_', ' ');
            }
        }

        private static string PotentialDescription(WeaponPotentialId id)
        {
            switch (id.Value)
            {
                case "hwando_venom_fang": return "비검이 적에게 중독을 남깁니다";
                case "hwando_returning_afterimage": return "귀환 궤적이 한 번 더 베어냅니다";
                case "hwando_flying_blade_dance": return "비검이 주변 적 사이를 연쇄 도약합니다";
                case "gakgung_armor_break_arrowhead": return "첫 타격이 적의 방어를 무너뜨립니다";
                case "gakgung_split_fletching": return "명중한 화살이 갈라져 다른 적을 노립니다";
                case "gakgung_full_draw": return "먼 거리의 적에게 더 강한 피해를 줍니다";
                case "talisman_five_element_cycle": return "오행 부적이 순환하며 연쇄 폭발합니다";
                case "talisman_seal_transfer": return "봉인이 가까운 적에게 옮겨갑니다";
                case "talisman_vengeful_ghost_burst": return "봉인 종료 시 원귀가 폭발합니다";
                case "thunder_earth_current": return "낙뢰 뒤 지면에 전류가 남습니다";
                case "thunder_overcharged_core": return "폭발 중심부 피해가 크게 증가합니다";
                case "thunder_lightning_rod": return "표식이 다음 번개를 끌어당깁니다";
                case "jangseung_ghost_face": return "장승의 귀면이 적을 위협합니다";
                case "jangseung_four_direction_barrier": return "네 방향에 추가 결계를 세웁니다";
                case "jangseung_guardian_descent": return "수호령이 내려와 적을 짓누릅니다";
                case "singijeon_powder_trail": return "화약 궤적이 닿은 적을 태웁니다";
                case "singijeon_submunition_split": return "폭발이 작은 신기전으로 분열합니다";
                case "singijeon_chain_ignition": return "처치 폭발이 주변 적에게 이어집니다";
                case "frost_crack_mark": return "빙결 파열이 추가 피해를 남깁니다";
                case "frost_spread": return "얼음 파편이 가까운 적에게 퍼집니다";
                case "frost_mist": return "빙무가 오래 남아 적을 둔화합니다";
                case "fan_vacuum_edge": return "진공 칼날이 적을 끌어당깁니다";
                case "fan_distant_thunder": return "먼 적에게 추가 낙뢰가 떨어집니다";
                case "fan_returning_chain": return "처치 후 칼날이 다음 적에게 되돌아갑니다";
                default: return "무기의 고유 공격 방식을 강화합니다";
            }
        }
    }
}
