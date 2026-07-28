using System;
using System.Collections;
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
        private const float SlowdownDuration = .3f;
        private const float CloseDuration = .15f;

        private sealed class Card
        {
            public Button Button;
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
        private GameObject cardsRoot;
        private CanvasGroup overlay;
        private TextMeshProUGUI heading;
        private Coroutine openRoutine;
        private Coroutine closeRoutine;
        private Func<int, bool> choose;
        private bool selectedChoice;

        public bool IsOpen { get; private set; }
        public bool IsChoiceLocked { get; private set; }
        public event Action PresentationClosed;

        public void BuildForTests() => Build();

        public void Build()
        {
            if (root != null) return;

            root = RuntimeUiFactory.Image("Upgrade Choice Overlay", transform, new Color(0.025f, .03f, .045f, .9f)).gameObject;
            RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            overlay = root.AddComponent<CanvasGroup>();
            overlay.alpha = 0f;
            cardsRoot = RuntimeUiFactory.Rect("Upgrade Cards", root.transform).gameObject;
            RuntimeUiFactory.Stretch(cardsRoot.GetComponent<RectTransform>(), 36f, 150f, 36f, 150f);
            heading = RuntimeUiFactory.Text("Heading", cardsRoot.transform, "CHOOSE A BLESSING", 38f, TextAlignmentOptions.Center);
            Position(heading.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -12f), new Vector2(820f, 64f), new Vector2(.5f, 1f));

            for (var index = 0; index < cards.Length; index++) cards[index] = CreateCard(index);
            root.SetActive(false);
        }

        public void Open(UpgradeChoiceState state, Func<int, bool> chooseChoice)
        {
            Build();
            CloseImmediately();
            if (state == null || chooseChoice == null || state.Choices.Count == 0) return;

            choose = chooseChoice;
            selectedChoice = false;
            IsChoiceLocked = false;
            IsOpen = true;
            heading.text = $"LEVEL {state.Level}  ·  CHOOSE A BLESSING";
            for (var index = 0; index < cards.Length; index++)
            {
                var hasChoice = index < state.Choices.Count;
                cards[index].Button.gameObject.SetActive(hasChoice);
                if (hasChoice) PopulateCard(cards[index], state.Choices[index]);
            }

            root.SetActive(true);
            cardsRoot.SetActive(false);
            overlay.alpha = 0f;
            Time.timeScale = 1f;
            openRoutine = StartCoroutine(OpenRoutine());
        }

        public void CloseImmediately()
        {
            if (openRoutine != null) StopCoroutine(openRoutine);
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            openRoutine = null;
            closeRoutine = null;
            Time.timeScale = 1f;
            IsOpen = false;
            IsChoiceLocked = false;
            selectedChoice = false;
            choose = null;
            if (root != null) root.SetActive(false);
        }

        private IEnumerator OpenRoutine()
        {
            var elapsed = 0f;
            while (elapsed < SlowdownDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / SlowdownDuration);
                Time.timeScale = Mathf.Lerp(1f, .08f, progress);
                overlay.alpha = progress;
                yield return null;
            }

            Time.timeScale = 0f;
            overlay.alpha = 1f;
            cardsRoot.SetActive(true);
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
            closeRoutine = StartCoroutine(CloseRoutine());
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
            var rect = card.Button.GetComponent<RectTransform>();
            Position(rect, new Vector2(.5f, 1f), new Vector2(0f, -112f - index * 255f), new Vector2(900f, 228f), new Vector2(.5f, 1f));
            card.Accent = RuntimeUiFactory.Image("Accent", card.Button.transform, JoseonUiPalette.Hanji);
            card.Accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            card.Accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            card.Accent.rectTransform.sizeDelta = new Vector2(10f, 0f);
            card.Icon = RuntimeUiFactory.Image("Icon", card.Button.transform, Color.white);
            Position(card.Icon.rectTransform, new Vector2(0f, .5f), new Vector2(42f, 0f), new Vector2(108f, 108f), new Vector2(0f, .5f));
            card.Icon.preserveAspect = true;
            card.Glyph = RuntimeUiFactory.Text("Glyph", card.Button.transform, string.Empty, 72f, TextAlignmentOptions.Center);
            Position(card.Glyph.rectTransform, new Vector2(0f, .5f), new Vector2(42f, 0f), new Vector2(108f, 108f), new Vector2(0f, .5f));
            card.Category = Label("Category", card.Button.transform, new Vector2(176f, -24f), new Vector2(660f, 30f), 19f, TextAlignmentOptions.Left);
            card.Name = Label("Name", card.Button.transform, new Vector2(176f, -62f), new Vector2(660f, 45f), 31f, TextAlignmentOptions.Left);
            card.Behavior = Label("Behavior", card.Button.transform, new Vector2(176f, -116f), new Vector2(660f, 50f), 20f, TextAlignmentOptions.Left);
            card.Behavior.enableWordWrapping = true;
            card.Delta = Label("Delta", card.Button.transform, new Vector2(176f, -184f), new Vector2(660f, 30f), 22f, TextAlignmentOptions.Left);
            card.Button.onClick.AddListener(() => Choose(choiceIndex));
            return card;
        }

        private static void PopulateCard(Card card, UpgradeChoiceView choice)
        {
            var accent = AccentFor(choice.Kind);
            card.Accent.color = accent;
            card.Category.color = accent;
            card.Delta.color = accent;
            card.Category.text = choice.Category;
            card.Name.text = choice.Name;
            card.Behavior.text = choice.Behavior;
            card.Delta.text = choice.Delta;
            card.Icon.sprite = choice.Icon;
            card.Icon.enabled = choice.Icon != null;
            card.Glyph.gameObject.SetActive(choice.Icon == null);
            card.Glyph.text = GlyphFor(choice.Kind);
            card.Button.interactable = true;
        }

        private static Color AccentFor(UpgradeKind kind) => kind == UpgradeKind.Evolution ? JoseonUiPalette.Gold :
            kind == UpgradeKind.Weapon ? JoseonUiPalette.Jade : JoseonUiPalette.Hanji;

        private static string GlyphFor(UpgradeKind kind) => kind == UpgradeKind.Evolution ? "龍" :
            kind == UpgradeKind.Weapon ? "弓" : "福";

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size,
            float fontSize, TextAlignmentOptions alignment)
        {
            var label = RuntimeUiFactory.Text(name, parent, string.Empty, fontSize, alignment);
            Position(label.rectTransform, new Vector2(0f, 1f), position, size, new Vector2(0f, 1f));
            return label;
        }

        private static void Position(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
