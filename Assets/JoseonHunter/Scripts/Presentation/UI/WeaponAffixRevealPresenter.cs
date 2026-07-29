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
        private GameObject root;
        private CanvasGroup group;
        private RectTransform panelRect;
        private Image shell;
        private Image rarityFrame;
        private Image burst;
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private readonly Image[] reelWindows = new Image[ReelCount];
        private readonly Image[,] spinningSymbols = new Image[ReelCount, 2];
        private readonly Image[] finalSymbols = new Image[ReelCount];
        private readonly Image[] lockedSlots = new Image[3];
        private readonly Image[] stopFlashes = new Image[ReelCount];
        private readonly TextMeshProUGUI[] potentialLabels = new TextMeshProUGUI[3];
        private Coroutine routine;
        private WeaponAffixRollResult activeResult;
        private WeaponAffixPresentationCatalogAsset activeCatalog;
        private WeaponAffixRevealTimeline timeline;
        private float elapsed;
        private float finishAt;
        private float skipSourceElapsed;
        private float skipTargetElapsed;
        private bool skipActive;
        private bool completed;
        private WeaponAffixPresentationCatalogAsset catalogForTests;

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
        public event Action RevealCompleted;

        public void Play(WeaponAffixRollResult result)
        {
            if (result == null)
            {
                HideImmediately();
                return;
            }

            Build();
            HideImmediately();
            activeResult = result;
            activeCatalog = catalogForTests ??
                Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            if (activeCatalog == null || !activeCatalog.HasRequiredUiSprites)
            {
                Debug.LogError("Weapon affix reveal requires the imported PixelLab micro-slot catalog.", this);
                LastCompletedResult = result;
                RevealCompleted?.Invoke();
                return;
            }

            completed = false;
            elapsed = 0f;
            skipActive = false;
            timeline = WeaponAffixRevealTimeline.For(result);
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
            skipActive = true;
            skipSourceElapsed = elapsed;
            skipTargetElapsed = timeline.SkipFinishAt(elapsed);
            finishAt = skipTargetElapsed;
        }

        public void HideImmediately()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = null;
            activeResult = null;
            activeCatalog = null;
            completed = false;
            skipActive = false;
            Phase = RevealPhase.Hidden;
            VisiblePotentialCount = 0;
            if (root != null)
                root.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData) => Skip();
        private void OnDisable() => HideImmediately();

        public static float DurationFor(WeaponAffixRollResult result) =>
            WeaponAffixRevealTimeline.For(result).Duration;

        private IEnumerator RevealRoutine()
        {
            while (elapsed < finishAt)
            {
                elapsed += Time.unscaledDeltaTime;
                UpdateVisualState(PresentationTime());
                yield return null;
            }

            UpdateVisualState(timeline.Duration);
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
            var opening = Mathf.Clamp01(time / .10f);
            var closing = Mathf.InverseLerp(timeline.Duration, timeline.CloseStartsAt, time);
            group.alpha = Phase == RevealPhase.Opening ? EaseOutCubic(opening) :
                Phase == RevealPhase.Closing ? Mathf.Clamp01(closing) : 1f;

            var openingScale = Mathf.Lerp(.94f, 1f, EaseOutBack(opening));
            TensionScale = TensionScaleAt(time);
            var shake = JackpotShakeAt(time);
            panelRect.anchoredPosition = shake;
            panelRect.localScale = Vector3.one * (Phase == RevealPhase.Opening ? openingScale : TensionScale);

            UpdateSpinningSymbols(time);
            SetFinalAffixVisible(time >= timeline.AffixStopsAt);
            VisiblePotentialCount = 0;
            for (var index = 0; index < 3; index++)
            {
                var awarded = index < activeResult.NewPotentials.Count;
                var opened = awarded && time >= timeline.PotentialStopsAt(index);
                lockedSlots[index].enabled = !opened;
                finalSymbols[index + 1].enabled = opened;
                potentialLabels[index].gameObject.SetActive(opened);
                if (opened)
                    VisiblePotentialCount++;
                UpdateStopFlash(index + 1, time, timeline.PotentialStopsAt(index), opened);
            }

            UpdateStopFlash(0, time, timeline.AffixStopsAt, IsFinalAffixVisible);
            var jackpotReady = activeResult.NewPotentials.Count > 0 &&
                VisiblePotentialCount == activeResult.NewPotentials.Count;
            burst.enabled = jackpotReady;
            if (jackpotReady)
            {
                var pulse = 1f + Mathf.Sin((time - timeline.ReadStartsAt) * 18f) *
                    Mathf.Clamp01((timeline.ReadStartsAt + .24f - time) / .24f) * .08f;
                burst.rectTransform.localScale = Vector3.one * pulse;
            }
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
            {
                var stopAt = reel == 0 ? timeline.AffixStopsAt : timeline.PotentialStopsAt(reel - 1);
                var awarded = reel == 0 || reel - 1 < activeResult.NewPotentials.Count;
                var spinning = awarded && time < stopAt;
                for (var symbolIndex = 0; symbolIndex < 2; symbolIndex++)
                {
                    var symbol = spinningSymbols[reel, symbolIndex];
                    symbol.enabled = spinning;
                    if (!spinning)
                        continue;
                    var height = reel == 0 ? 72f : 54f;
                    var offset = Mathf.Repeat(time * (reel == 0 ? 520f : 610f) + symbolIndex * height,
                        height * 2f) - height;
                    symbol.rectTransform.anchoredPosition = new Vector2(0f, offset);
                    var edgeFade = 1f - Mathf.Clamp01(Mathf.Abs(offset) / height);
                    symbol.color = new Color(1f, 1f, 1f, .45f + edgeFade * .55f);
                }
            }
        }

        private void SetFinalAffixVisible(bool visible)
        {
            finalSymbols[0].enabled = visible;
            rarityFrame.enabled = visible;
            title.gameObject.SetActive(visible);
            detail.gameObject.SetActive(visible);
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
            shell.sprite = activeCatalog.SlotMachineShell;
            rarityFrame.sprite = activeCatalog.SpriteForAffix(activeResult.General.Tier);
            finalSymbols[0].sprite = activeResult.General.Tier == WeaponAffixTier.Standard
                ? activeCatalog.ReelSymbolStat
                : activeCatalog.ReelSymbolRarity;
            for (var reel = 0; reel < ReelCount; reel++)
            {
                reelWindows[reel].sprite = activeCatalog.ReelWindow;
                reelWindows[reel].enabled = false;
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
                var awarded = index < activeResult.NewPotentials.Count;
                finalSymbols[index + 1].sprite = awarded
                    ? activeCatalog.SpriteForPotential(activeResult.NewPotentials[index])
                    : null;
                potentialLabels[index].text = awarded ? PotentialName(activeResult.NewPotentials[index]) : string.Empty;
            }

            burst.sprite = activeResult.NewPotentials.Count > 0
                ? activeCatalog.JackpotBurstFor(activeResult.NewPotentials.Count)
                : null;
            title.text = TierName(activeResult.General.Tier);
            detail.text = Describe(activeResult.General);
        }

        private void ResetVisuals()
        {
            group.alpha = 0f;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localScale = Vector3.one * .94f;
            rarityFrame.enabled = false;
            burst.enabled = false;
            title.gameObject.SetActive(false);
            detail.gameObject.SetActive(false);
            for (var reel = 0; reel < ReelCount; reel++)
            {
                finalSymbols[reel].enabled = false;
                stopFlashes[reel].enabled = false;
                for (var symbol = 0; symbol < 2; symbol++)
                    spinningSymbols[reel, symbol].enabled = true;
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

            root = RuntimeUiFactory.Image("Weapon Affix Micro Slot", transform,
                new Color(.008f, .012f, .022f, .84f)).gameObject;
            RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            group = root.AddComponent<CanvasGroup>();

            shell = RuntimeUiFactory.Image("PixelLab Slot Shell", root.transform, Color.white);
            panelRect = shell.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.sizeDelta = new Vector2(760f, 428f);
            shell.preserveAspect = true;

            burst = RuntimeUiFactory.Image("Jackpot Burst", shell.transform, Color.white);
            burst.rectTransform.anchorMin = burst.rectTransform.anchorMax = new Vector2(.5f, .5f);
            burst.rectTransform.anchoredPosition = new Vector2(0f, 24f);
            burst.rectTransform.sizeDelta = new Vector2(226f, 136f);
            burst.preserveAspect = true;
            burst.transform.SetAsFirstSibling();

            title = Label("Affix Title", shell.transform, new Vector2(0f, 244f),
                new Vector2(620f, 42f), 29f, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, .84f, .38f);
            detail = Label("Affix Detail", shell.transform, new Vector2(0f, 207f),
                new Vector2(620f, 30f), 21f, TextAlignmentOptions.Center);

            BuildReel(0, new Vector2(0f, 38f), new Vector2(330f, 82f), new Vector2(88f, 68f));
            rarityFrame = RuntimeUiFactory.Image("Affix Rarity Frame", finalSymbols[0].transform, Color.white);
            RuntimeUiFactory.Stretch(rarityFrame.rectTransform, -12f, -12f, -12f, -12f);
            rarityFrame.preserveAspect = true;
            rarityFrame.transform.SetAsFirstSibling();

            var positions = new[] { new Vector2(-194f, -102f), new Vector2(0f, -102f), new Vector2(194f, -102f) };
            for (var index = 0; index < 3; index++)
            {
                BuildReel(index + 1, positions[index], new Vector2(150f, 92f), new Vector2(116f, 58f));
                lockedSlots[index] = RuntimeUiFactory.Image("Locked Potential " + index,
                    reelWindows[index + 1].transform, Color.white);
                RuntimeUiFactory.Stretch(lockedSlots[index].rectTransform, 8f, 8f, 8f, 8f);
                lockedSlots[index].preserveAspect = true;
                potentialLabels[index] = Label("Potential Label " + index, shell.transform,
                    positions[index] + new Vector2(0f, -73f), new Vector2(184f, 32f), 19f,
                    TextAlignmentOptions.Center);
                potentialLabels[index].fontStyle = FontStyles.Bold;
                potentialLabels[index].color = new Color(.88f, .95f, 1f);
            }

            root.SetActive(false);
        }

        private void BuildReel(int index, Vector2 position, Vector2 windowSize, Vector2 symbolSize)
        {
            var window = RuntimeUiFactory.Image("Reel Window " + index, shell.transform, Color.white);
            window.rectTransform.anchorMin = window.rectTransform.anchorMax = new Vector2(.5f, .5f);
            window.rectTransform.anchoredPosition = position;
            window.rectTransform.sizeDelta = windowSize;
            window.preserveAspect = true;
            reelWindows[index] = window;

            var viewport = RuntimeUiFactory.Image("Reel Viewport " + index, window.transform,
                new Color(1f, 1f, 1f, .001f));
            RuntimeUiFactory.Stretch(viewport.rectTransform, 13f, 10f, 13f, 10f);
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

        private static string Describe(WeaponAffixRoll roll) =>
            roll.Stat + " +" + Mathf.RoundToInt((float)(roll.Value * 100d)) + "%";

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
    }
}
