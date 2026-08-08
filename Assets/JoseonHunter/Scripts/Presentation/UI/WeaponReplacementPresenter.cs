using System;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class WeaponReplacementPresenter : MonoBehaviour
    {
        private sealed class Card
        {
            public Button Button;
            public Image Icon;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Detail;
            public string WeaponId;
        }

        private readonly Card[] cards = new Card[4];
        private GameObject root;
        private RectTransform panel;
        private Button cancelButton;
        private TextMeshProUGUI newWeaponLabel;
        private Func<string, bool> choose;
        private Func<bool> cancel;
        private bool locked;

        public bool IsOpen { get; private set; }
        public event Action PresentationClosed;

        public void Build()
        {
            if (root != null) return;
            root = RuntimeUiFactory.Image("Weapon Replacement Overlay", transform,
                new Color(.008f, .012f, .022f, .9f)).gameObject;
            RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            panel = RuntimeUiFactory.Image("Replacement Panel", root.transform, JoseonUiPalette.Hanji).rectTransform;
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(PortraitUiMetrics.ModalWidth, 1240f);

            var heading = RuntimeUiFactory.Text("Replacement Heading", panel, "버릴 무기를 선택하세요", 40f,
                TextAlignmentOptions.Center, RuntimeFontRole.Title);
            Position(heading.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -38f),
                new Vector2(820f, 64f), new Vector2(.5f, 1f));
            newWeaponLabel = RuntimeUiFactory.Text("New Weapon Label", panel, string.Empty, 24f,
                TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            Position(newWeaponLabel.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -105f),
                new Vector2(820f, 38f), new Vector2(.5f, 1f));
            var guide = RuntimeUiFactory.Text("Replacement Guide", panel,
                "버린 무기는 이번 출정에서 다시 나오지 않습니다", 20f, TextAlignmentOptions.Center);
            Position(guide.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -150f),
                new Vector2(820f, 40f), new Vector2(.5f, 1f));
            guide.color = JoseonUiPalette.HanjiMutedInk;

            for (var index = 0; index < cards.Length; index++) cards[index] = CreateCard(index);
            cancelButton = RuntimeUiFactory.Button("Cancel Replacement", panel, JoseonUiPalette.AppraisalResult);
            Position(cancelButton.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(0f, -520f),
                new Vector2(420f, 72f), new Vector2(.5f, .5f));
            var cancelLabel = RuntimeUiFactory.Text("Cancel Replacement Label", cancelButton.transform,
                "교체하지 않기", 23f, TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            RuntimeUiFactory.Stretch(cancelLabel.rectTransform, 10f, 8f, 10f, 8f);
            cancelButton.onClick.AddListener(Cancel);
            JoseonButtonSkin.Apply(cancelButton, JoseonButtonStyle.Secondary);
            ApplyPortraitLayout();
            root.SetActive(false);
        }

        public void Open(WeaponReplacementState state, Func<string, bool> chooseWeapon, Func<bool> cancelChoice)
        {
            Build();
            CloseImmediately();
            if (state == null || chooseWeapon == null || cancelChoice == null || state.Choices.Count != 4) return;
            choose = chooseWeapon;
            cancel = cancelChoice;
            locked = false;
            IsOpen = true;
            newWeaponLabel.text = "새 무기 · " + state.NewWeaponName;
            for (var index = 0; index < cards.Length; index++) Populate(cards[index], state.Choices[index]);
            cancelButton.interactable = true;
            root.SetActive(true);
        }

        public void CloseImmediately()
        {
            IsOpen = false;
            locked = false;
            choose = null;
            cancel = null;
            if (root != null) root.SetActive(false);
        }

        public void ApplyPortraitLayout()
        {
            if (panel == null) return;
            panel.sizeDelta = new Vector2(PortraitUiMetrics.ContainedWidth(transform as RectTransform,
                PortraitUiMetrics.ModalWidth), 1240f);
        }

        private Card CreateCard(int index)
        {
            var card = new Card
            {
                Button = RuntimeUiFactory.Button("Replacement Choice " + index, panel,
                    JoseonUiPalette.AppraisalInset)
            };
            Position(card.Button.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
                new Vector2(0f, 340f - index * 205f), new Vector2(860f, 176f), new Vector2(.5f, .5f));
            card.Icon = RuntimeUiFactory.Image("Replacement Icon " + index, card.Button.transform, Color.white);
            Position(card.Icon.rectTransform, new Vector2(0f, .5f), new Vector2(34f, 0f),
                new Vector2(102f, 102f), new Vector2(0f, .5f));
            card.Icon.preserveAspect = true;
            card.Name = Label("Replacement Name " + index, card.Button.transform,
                new Vector2(164f, -30f), new Vector2(470f, 42f), 29f, RuntimeFontRole.Title);
            card.Detail = Label("Replacement Detail " + index, card.Button.transform,
                new Vector2(164f, -88f), new Vector2(470f, 36f), 20f);
            var action = RuntimeUiFactory.Text("Replacement Action " + index, card.Button.transform,
                "이 무기를 버림", 20f, TextAlignmentOptions.Center, RuntimeFontRole.BodyEmphasis);
            Position(action.rectTransform, new Vector2(1f, .5f), new Vector2(-24f, 0f),
                new Vector2(184f, 54f), new Vector2(1f, .5f));
            action.color = JoseonUiPalette.SealCrimson;
            card.Button.onClick.AddListener(() => Choose(card));
            return card;
        }

        private static void Populate(Card card, WeaponReplacementChoiceView choice)
        {
            card.WeaponId = choice.WeaponId;
            card.Icon.sprite = choice.Icon;
            card.Icon.enabled = choice.Icon != null;
            card.Name.text = choice.DisplayName;
            card.Detail.text = string.IsNullOrEmpty(choice.LegacyName)
                ? $"레벨 {choice.Level} · 전승 미선택"
                : $"레벨 {choice.Level} · {choice.LegacyName}";
            card.Button.interactable = true;
        }

        private void Choose(Card card)
        {
            if (!IsOpen || locked || choose == null) return;
            locked = true;
            if (!choose(card.WeaponId))
            {
                locked = false;
                return;
            }
            if (!IsOpen) return;
            CloseImmediately();
            PresentationClosed?.Invoke();
        }

        private void Cancel()
        {
            if (!IsOpen || locked || cancel == null) return;
            locked = true;
            if (!cancel())
            {
                locked = false;
                return;
            }
            CloseImmediately();
        }

        private void OnDisable() => CloseImmediately();
        private void OnDestroy() => CloseImmediately();

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size,
            float fontSize, RuntimeFontRole role = RuntimeFontRole.Body)
        {
            var text = RuntimeUiFactory.Text(name, parent, string.Empty, fontSize, TextAlignmentOptions.Left, role);
            Position(text.rectTransform, new Vector2(0f, 1f), position, size, new Vector2(0f, 1f));
            return text;
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
