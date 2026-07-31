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
        private const float CardEntranceDuration = .18f;
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
            overlay.blocksRaycasts = true;
            cardsRoot = RuntimeUiFactory.Rect("Upgrade Cards", root.transform).gameObject;
            RuntimeUiFactory.Stretch(cardsRoot.GetComponent<RectTransform>(), 64f, 84f, 64f, 84f);
            heading = RuntimeUiFactory.Text("Heading", cardsRoot.transform, "CHOOSE A BLESSING", 34f, TextAlignmentOptions.Center);
            Position(heading.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -8f), new Vector2(920f, 58f), new Vector2(.5f, 1f));

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
            openRoutine = StartCoroutine(OpenRoutine());
        }

        public void CloseImmediately()
        {
            if (openRoutine != null) StopCoroutine(openRoutine);
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            openRoutine = null;
            closeRoutine = null;
            IsOpen = false;
            IsChoiceLocked = false;
            selectedChoice = false;
            choose = null;
            if (root != null) root.SetActive(false);
        }

        public void CloseAfterExternalSelection()
        {
            if (!IsOpen || IsChoiceLocked) return;
            IsChoiceLocked = true;
            selectedChoice = true;
            closeRoutine = StartCoroutine(CloseRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            var elapsed = 0f;
            overlay.alpha = 1f;
            cardsRoot.SetActive(true);
            for (var index = 0; index < cards.Length; index++)
                cards[index].Button.transform.localScale = Vector3.one * .92f;

            elapsed = 0f;
            while (elapsed < CardEntranceDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var index = 0; index < cards.Length; index++)
                {
                    var staggered = Mathf.Clamp01(elapsed / CardEntranceDuration * 1.45f - index * .18f);
                    cards[index].Button.transform.localScale =
                        Vector3.one * Mathf.LerpUnclamped(.92f, 1f, EaseOutBack(staggered));
                }
                yield return null;
            }
            for (var index = 0; index < cards.Length; index++)
                cards[index].Button.transform.localScale = Vector3.one;
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
            var cardFrame = Resources.Load<Sprite>("UI/UpgradeCardFrame");
            if (cardFrame != null)
            {
                card.Button.image.sprite = cardFrame;
                card.Button.image.color = Color.white;
                card.Button.image.type = Image.Type.Sliced;
            }
            var rect = card.Button.GetComponent<RectTransform>();
            Position(rect, new Vector2(.5f, .5f), new Vector2(0f, 220f - index * 220f),
                new Vector2(920f, 200f), new Vector2(.5f, .5f));
            card.Accent = RuntimeUiFactory.Image("Accent", card.Button.transform, JoseonUiPalette.Hanji);
            card.Accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            card.Accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            card.Accent.rectTransform.sizeDelta = new Vector2(10f, 0f);
            card.Icon = RuntimeUiFactory.Image("Icon", card.Button.transform, Color.white);
            Position(card.Icon.rectTransform, new Vector2(0f, .5f), new Vector2(38f, 0f),
                new Vector2(104f, 104f), new Vector2(0f, .5f));
            card.Icon.preserveAspect = true;
            card.Glyph = RuntimeUiFactory.Text("Glyph", card.Button.transform, string.Empty, 72f, TextAlignmentOptions.Center);
            Position(card.Glyph.rectTransform, new Vector2(0f, .5f), new Vector2(38f, 0f),
                new Vector2(104f, 104f), new Vector2(0f, .5f));
            card.Category = Label("Category", card.Button.transform, new Vector2(170f, -22f),
                new Vector2(680f, 26f), 17f, TextAlignmentOptions.Left);
            card.Name = Label("Name", card.Button.transform, new Vector2(170f, -54f),
                new Vector2(680f, 38f), 27f, TextAlignmentOptions.Left);
            card.Behavior = Label("Behavior", card.Button.transform, new Vector2(170f, -98f),
                new Vector2(680f, 48f), 18f, TextAlignmentOptions.Left);
            card.Behavior.enableWordWrapping = true;
            card.Delta = Label("Delta", card.Button.transform, new Vector2(170f, -158f),
                new Vector2(680f, 28f), 20f, TextAlignmentOptions.Left);
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
