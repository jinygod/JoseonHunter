using System;
using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class UpgradeChoicePresenter : MonoBehaviour
    {
        private const float CardEntranceDuration = .18f;
        private const float FinalIntroLockDuration = .2f;
        private const float CloseDuration = .15f;
        private static readonly Color StandardOverlay = new(.025f, .03f, .045f, .9f);
        private static readonly Color FinalOverlay = new(.035f, .018f, .025f, 1f);
        private static readonly Color FinalInterior = new(.22f, .055f, .035f, 1f);
        private static readonly Color MutedInterior = new(.47f, .43f, .34f, 1f);

        private sealed class Card
        {
            public Button Button;
            public Image Interior;
            public Image Accent;
            public Image Icon;
            public TextMeshProUGUI Glyph;
            public TextMeshProUGUI Category;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Behavior;
            public TextMeshProUGUI Delta;
        }

        private readonly Card[] cards = new Card[3];
        private GameObject root;
        private Image rootImage;
        private GameObject cardsRoot;
        private CanvasGroup overlay;
        private TextMeshProUGUI heading;
        private Coroutine openRoutine;
        private Coroutine closeRoutine;
        private Coroutine pulseRoutine;
        private Func<int, bool> choose;
        private bool selectedChoice;
        private bool finalEvolutionPresentation;
        private Card finalEvolutionCard;

        public bool IsOpen { get; private set; }
        public bool IsChoiceLocked { get; private set; }
        public bool IsFinalEvolutionPresentationForTests => finalEvolutionPresentation;
        public string HeadingForTests => heading != null ? heading.text : string.Empty;
        public event Action PresentationClosed;

        public void BuildForTests() => Build();

        public void Build()
        {
            if (root != null) return;

            rootImage = RuntimeUiFactory.Image(
                "Upgrade Choice Overlay", transform, StandardOverlay);
            root = rootImage.gameObject;
            RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            overlay = root.AddComponent<CanvasGroup>();
            overlay.alpha = 0f;
            overlay.blocksRaycasts = true;
            cardsRoot = RuntimeUiFactory.Rect("Upgrade Cards", root.transform).gameObject;
            RuntimeUiFactory.Stretch(cardsRoot.GetComponent<RectTransform>(), PortraitUiMetrics.SideMargin,
                PortraitUiMetrics.BottomMargin, PortraitUiMetrics.SideMargin, PortraitUiMetrics.TopMargin);
            heading = RuntimeUiFactory.Text("Heading", cardsRoot.transform, "강화를 선택하세요", 34f,
                TextAlignmentOptions.Center, RuntimeFontRole.Title);
            Position(heading.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -8f), new Vector2(920f, 58f), new Vector2(.5f, 1f));

            for (var index = 0; index < cards.Length; index++) cards[index] = CreateCard(index);
            ApplyPortraitLayout();
            root.SetActive(false);
        }

        public void Open(UpgradeChoiceState state, Func<int, bool> chooseChoice)
        {
            Build();
            CloseImmediately();
            if (state == null || chooseChoice == null || state.Choices.Count == 0) return;

            choose = chooseChoice;
            selectedChoice = false;
            finalEvolutionPresentation = state.Choices.Any(choice =>
                choice.PresentationTier == UpgradePresentationTier.FinalEvolution);
            IsChoiceLocked = finalEvolutionPresentation;
            IsOpen = true;
            rootImage.color = finalEvolutionPresentation ? FinalOverlay : StandardOverlay;
            heading.text = finalEvolutionPresentation
                ? "최종 진화가 깨어납니다"
                : $"레벨 {state.Level} · 강화를 선택하세요";
            for (var index = 0; index < cards.Length; index++)
            {
                var hasChoice = index < state.Choices.Count;
                cards[index].Button.gameObject.SetActive(hasChoice);
                if (hasChoice) PopulateCard(cards[index], state.Choices[index]);
            }

            root.SetActive(true);
            cardsRoot.SetActive(false);
            overlay.alpha = 0f;
            openRoutine = StartCoroutine(OpenRoutine());
        }

        public void CloseImmediately()
        {
            if (openRoutine != null) StopCoroutine(openRoutine);
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            openRoutine = null;
            closeRoutine = null;
            pulseRoutine = null;
            IsOpen = false;
            IsChoiceLocked = false;
            selectedChoice = false;
            choose = null;
            finalEvolutionPresentation = false;
            finalEvolutionCard = null;
            for (var index = 0; index < cards.Length; index++)
                if (cards[index] != null)
                    cards[index].Button.transform.localScale = Vector3.one;
            if (root != null) root.SetActive(false);
        }

        public void CloseAfterExternalSelection()
        {
            if (!IsOpen || IsChoiceLocked) return;
            IsChoiceLocked = true;
            selectedChoice = true;
            closeRoutine = StartCoroutine(CloseRoutine());
        }

        public void ApplyPortraitLayout()
        {
            if (cardsRoot == null) return;
            var width = PortraitUiMetrics.ContainedWidth(transform as RectTransform, PortraitUiMetrics.ModalWidth);
            for (var index = 0; index < cards.Length; index++)
                if (cards[index] != null)
                    cards[index].Button.GetComponent<RectTransform>().sizeDelta =
                        new Vector2(width, PortraitUiMetrics.UpgradeCardHeight);
        }

        private IEnumerator OpenRoutine()
        {
            var elapsed = 0f;
            overlay.alpha = 1f;
            cardsRoot.SetActive(true);
            for (var index = 0; index < cards.Length; index++)
                cards[index].Button.transform.localScale = Vector3.one * .92f;

            elapsed = 0f;
            var entranceDuration = finalEvolutionPresentation
                ? FinalIntroLockDuration
                : CardEntranceDuration;
            while (elapsed < entranceDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var index = 0; index < cards.Length; index++)
                {
                    var staggered = Mathf.Clamp01(
                        elapsed / entranceDuration * 1.45f - index * .18f);
                    cards[index].Button.transform.localScale =
                        Vector3.one * Mathf.LerpUnclamped(.92f, 1f, EaseOutBack(staggered));
                }
                yield return null;
            }
            for (var index = 0; index < cards.Length; index++)
                cards[index].Button.transform.localScale = Vector3.one;
            if (finalEvolutionPresentation)
            {
                IsChoiceLocked = false;
                if (finalEvolutionCard != null)
                    pulseRoutine = StartCoroutine(FinalPulseRoutine(finalEvolutionCard));
            }
            openRoutine = null;
        }

        private void Choose(int index)
        {
            if (!IsOpen || IsChoiceLocked || choose == null) return;
            IsChoiceLocked = true;
            if (!choose(index))
            {
                IsChoiceLocked = false;
                return;
            }

            selectedChoice = true;
            StopFinalPulse();
            closeRoutine = StartCoroutine(CloseRoutine());
        }

        private IEnumerator FinalPulseRoutine(Card card)
        {
            var elapsed = 0f;
            while (IsOpen && finalEvolutionPresentation && card != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var pulse = .5f + .5f * Mathf.Sin(elapsed * 4.5f);
                card.Button.transform.localScale =
                    Vector3.one * Mathf.Lerp(1f, 1.025f, pulse);
                yield return null;
            }
            if (card != null) card.Button.transform.localScale = Vector3.one;
            pulseRoutine = null;
        }

        private void StopFinalPulse()
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = null;
            if (finalEvolutionCard != null)
                finalEvolutionCard.Button.transform.localScale = Vector3.one;
        }

        private IEnumerator CloseRoutine()
        {
            var elapsed = 0f;
            while (elapsed < CloseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.alpha = 1f - Mathf.Clamp01(elapsed / CloseDuration);
                yield return null;
            }

            var notifyClosed = selectedChoice;
            CloseImmediately();
            if (notifyClosed) PresentationClosed?.Invoke();
        }

        private void OnDisable() => CloseImmediately();

        private void OnDestroy() => CloseImmediately();

        private Card CreateCard(int index)
        {
            var choiceIndex = index;
            var card = new Card { Button = RuntimeUiFactory.Button("Upgrade Card " + index, cardsRoot.transform, JoseonUiPalette.Ink) };
            var cardFrame = Resources.Load<Sprite>("UI/UpgradeCardFrame");
            if (cardFrame != null)
            {
                card.Button.image.sprite = cardFrame;
                card.Button.image.color = Color.white;
                card.Button.image.type = Image.Type.Sliced;
            }
            card.Interior = RuntimeUiFactory.Image("Hanji Interior", card.Button.transform,
                JoseonUiPalette.Hanji);
            RuntimeUiFactory.Stretch(card.Interior.rectTransform, 18f, 18f, 18f, 18f);
            card.Interior.transform.SetAsFirstSibling();
            card.Interior.raycastTarget = false;
            var rect = card.Button.GetComponent<RectTransform>();
            Position(rect, new Vector2(.5f, .5f), new Vector2(0f, 264f - index *
                (PortraitUiMetrics.UpgradeCardHeight + 28f)), new Vector2(PortraitUiMetrics.ModalWidth,
                PortraitUiMetrics.UpgradeCardHeight), new Vector2(.5f, .5f));
            card.Accent = RuntimeUiFactory.Image("Accent", card.Button.transform, JoseonUiPalette.Hanji);
            card.Accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            card.Accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            card.Accent.rectTransform.sizeDelta = new Vector2(10f, 0f);
            card.Icon = RuntimeUiFactory.Image("Icon", card.Button.transform, Color.white);
            Position(card.Icon.rectTransform, new Vector2(0f, .5f), new Vector2(38f, 0f),
                new Vector2(104f, 104f), new Vector2(0f, .5f));
            card.Icon.preserveAspect = true;
            card.Glyph = RuntimeUiFactory.Text("Glyph", card.Button.transform, string.Empty, 72f,
                TextAlignmentOptions.Center, RuntimeFontRole.Title);
            Position(card.Glyph.rectTransform, new Vector2(0f, .5f), new Vector2(38f, 0f),
                new Vector2(104f, 104f), new Vector2(0f, .5f));
            card.Category = Label("Category", card.Button.transform, new Vector2(220f, -30f),
                new Vector2(620f, 26f), 17f, TextAlignmentOptions.Left, RuntimeFontRole.BodyEmphasis);
            card.Name = Label("Name", card.Button.transform, new Vector2(220f, -66f),
                new Vector2(620f, 38f), 27f, TextAlignmentOptions.Left, RuntimeFontRole.BodyEmphasis);
            card.Name.color = JoseonUiPalette.Ink;
            card.Behavior = Label("Behavior", card.Button.transform, new Vector2(220f, -114f),
                new Vector2(620f, 48f), 18f, TextAlignmentOptions.Left);
            card.Behavior.color = JoseonUiPalette.Ink;
            card.Behavior.textWrappingMode = TextWrappingModes.Normal;
            card.Delta = Label("Delta", card.Button.transform, new Vector2(220f, -190f),
                new Vector2(620f, 28f), 20f, TextAlignmentOptions.Left, RuntimeFontRole.BodyEmphasis);
            card.Glyph.color = JoseonUiPalette.Ink;
            card.Button.onClick.AddListener(() => Choose(choiceIndex));
            return card;
        }

        private void PopulateCard(Card card, UpgradeChoiceView choice)
        {
            var isFinal = choice.PresentationTier == UpgradePresentationTier.FinalEvolution;
            var accent = AccentFor(choice.Kind);
            if (isFinal) accent = JoseonUiPalette.Gold;
            var readableAccent = Color.Lerp(accent, JoseonUiPalette.Ink, .45f);
            readableAccent.a = 1f;
            card.Accent.color = accent;
            card.Category.color = readableAccent;
            card.Delta.color = readableAccent;
            card.Category.text = choice.Category;
            card.Name.text = choice.Name;
            card.Behavior.text = choice.Behavior;
            card.Delta.text = choice.Delta;
            card.Interior.color = isFinal
                ? FinalInterior
                : finalEvolutionPresentation
                    ? MutedInterior
                    : JoseonUiPalette.Hanji;
            card.Button.image.color = isFinal ? JoseonUiPalette.Gold : Color.white;
            card.Name.color = isFinal ? JoseonUiPalette.Hanji : JoseonUiPalette.Ink;
            card.Behavior.color = isFinal ? JoseonUiPalette.Hanji : JoseonUiPalette.Ink;
            card.Glyph.color = isFinal ? JoseonUiPalette.Hanji : JoseonUiPalette.Ink;
            card.Icon.sprite = choice.Icon;
            card.Icon.enabled = choice.Icon != null;
            card.Glyph.gameObject.SetActive(choice.Icon == null);
            card.Glyph.text = GlyphFor(choice.Kind);
            card.Button.interactable = true;
            if (isFinal) finalEvolutionCard = card;
        }

        private static Color AccentFor(UpgradeKind kind) => kind == UpgradeKind.Evolution ? JoseonUiPalette.Gold :
            kind == UpgradeKind.Weapon ? JoseonUiPalette.Jade : JoseonUiPalette.Hanji;

        private static string GlyphFor(UpgradeKind kind) => kind == UpgradeKind.Evolution ? "龍" :
            kind == UpgradeKind.Weapon ? "弓" : "福";

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size,
            float fontSize, TextAlignmentOptions alignment, RuntimeFontRole role = RuntimeFontRole.Body)
        {
            var label = RuntimeUiFactory.Text(name, parent, string.Empty, fontSize, alignment, role);
            Position(label.rectTransform, new Vector2(0f, 1f), position, size, new Vector2(0f, 1f));
            return label;
        }

        private static TextMeshProUGUI CenteredLabel(string name, Transform parent, Vector2 position,
            Vector2 size, float fontSize)
        {
            var label = RuntimeUiFactory.Text(name, parent, string.Empty, fontSize, TextAlignmentOptions.Center);
            Position(label.rectTransform, new Vector2(.5f, .5f), position, size, new Vector2(.5f, .5f));
            return label;
        }

        private static void Position(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static float EaseOutBack(float value)
        {
            value = Mathf.Clamp01(value);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(value - 1f, 3f) + c1 * Mathf.Pow(value - 1f, 2f);
        }
    }
}
