using System;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class WeaponLegacyChoicePresenter : MonoBehaviour
    {
        private sealed class Card
        {
            public Button Button;
            public Image Icon;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI CombatStyle;
            public TextMeshProUGUI Benefit;
            public TextMeshProUGUI Cost;
            public WeaponLegacyPathId PathId;
        }

        private readonly Card[] cards = new Card[2];
        private GameObject root;
        private RectTransform panel;
        private Func<WeaponLegacyPathId, bool> choose;
        private bool locked;

        public bool IsOpen { get; private set; }
        public event Action PresentationClosed;

        public void Build()
        {
            if (root != null) return;
            root = RuntimeUiFactory.Image("Weapon Legacy Overlay", transform,
                new Color(.008f, .012f, .022f, .9f)).gameObject;
            RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            panel = RuntimeUiFactory.Image("Legacy Cards", root.transform, JoseonUiPalette.Hanji).rectTransform;
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(PortraitUiMetrics.ModalWidth, 980f);
            BuildBorder(panel);

            var heading = Text("Legacy Heading", panel, "전승 경로를 선택하세요", 40f,
                TextAlignmentOptions.Center, RuntimeFontRole.Title);
            Position(heading.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -42f),
                new Vector2(820f, 64f), new Vector2(.5f, 1f));
            var guide = Text("Legacy Guide", panel, "한 번 선택한 전승은 이번 출정에서 바꿀 수 없습니다", 20f,
                TextAlignmentOptions.Center);
            Position(guide.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -112f),
                new Vector2(820f, 40f), new Vector2(.5f, 1f));
            guide.color = JoseonUiPalette.HanjiMutedInk;

            for (var index = 0; index < cards.Length; index++) cards[index] = CreateCard(index);
            ApplyPortraitLayout();
            root.SetActive(false);
        }

        public void Open(WeaponLegacyChoiceState state, Func<WeaponLegacyPathId, bool> choosePath)
        {
            Build();
            CloseImmediately();
            if (state == null || choosePath == null || state.Choices.Count != 2) return;
            choose = choosePath;
            locked = false;
            IsOpen = true;
            for (var index = 0; index < cards.Length; index++) Populate(cards[index], state.Choices[index]);
            root.SetActive(true);
        }

        public void CloseImmediately()
        {
            IsOpen = false;
            locked = false;
            choose = null;
            if (root != null) root.SetActive(false);
        }

        public void ApplyPortraitLayout()
        {
            if (panel == null) return;
            panel.sizeDelta = new Vector2(PortraitUiMetrics.ContainedWidth(transform as RectTransform,
                PortraitUiMetrics.ModalWidth), 980f);
        }

        private Card CreateCard(int index)
        {
            var card = new Card
            {
                Button = RuntimeUiFactory.Button("Legacy Choice " + index, panel, JoseonUiPalette.AppraisalInset)
            };
            var frame = Resources.Load<Sprite>("UI/UpgradeCardFrame");
            if (frame != null)
            {
                card.Button.image.sprite = frame;
                card.Button.image.type = Image.Type.Sliced;
                card.Button.image.color = Color.white;
            }
            Position(card.Button.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
                new Vector2(0f, 178f - index * 380f), new Vector2(860f, 344f), new Vector2(.5f, .5f));
            card.Icon = RuntimeUiFactory.Image("Legacy Icon " + index, card.Button.transform, Color.white);
            Position(card.Icon.rectTransform, new Vector2(0f, .5f), new Vector2(38f, 0f),
                new Vector2(112f, 112f), new Vector2(0f, .5f));
            card.Icon.preserveAspect = true;
            card.Name = CardText("Legacy Name " + index, card.Button.transform, new Vector2(182f, -28f),
                new Vector2(620f, 44f), 31f, RuntimeFontRole.Title);
            card.CombatStyle = CardText("Combat Style", card.Button.transform, new Vector2(182f, -92f),
                new Vector2(620f, 52f), 21f);
            card.Benefit = CardText("Benefit", card.Button.transform, new Vector2(182f, -164f),
                new Vector2(620f, 48f), 21f);
            card.Cost = CardText("Cost", card.Button.transform, new Vector2(182f, -226f),
                new Vector2(620f, 48f), 21f);
            card.Cost.color = JoseonUiPalette.SealCrimson;
            card.Button.onClick.AddListener(() => Choose(card));
            return card;
        }

        private static void Populate(Card card, WeaponLegacyChoiceView choice)
        {
            card.PathId = choice.PathId;
            card.Icon.sprite = choice.Icon;
            card.Icon.enabled = choice.Icon != null;
            card.Name.text = choice.DisplayName;
            card.CombatStyle.text = "전투 방식 · " + choice.CombatStyle;
            card.Benefit.text = "강점 · " + choice.Benefit;
            card.Cost.text = "약점 · " + choice.Cost;
            card.Button.interactable = true;
        }

        private void Choose(Card card)
        {
            if (!IsOpen || locked || choose == null) return;
            locked = true;
            if (!choose(card.PathId))
            {
                locked = false;
                return;
            }
            if (!IsOpen) return;
            CloseImmediately();
            PresentationClosed?.Invoke();
        }

        private void OnDisable() => CloseImmediately();
        private void OnDestroy() => CloseImmediately();

        private static TextMeshProUGUI CardText(string name, Transform parent, Vector2 position, Vector2 size,
            float fontSize, RuntimeFontRole role = RuntimeFontRole.BodyEmphasis)
        {
            var text = Text(name, parent, string.Empty, fontSize, TextAlignmentOptions.Left, role);
            Position(text.rectTransform, new Vector2(0f, 1f), position, size, new Vector2(0f, 1f));
            text.enableWordWrapping = true;
            return text;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string value, float fontSize,
            TextAlignmentOptions alignment, RuntimeFontRole role = RuntimeFontRole.Body) =>
            RuntimeUiFactory.Text(name, parent, value, fontSize, alignment, role);

        private static void BuildBorder(Transform parent)
        {
            Border("Legacy Border Top", parent, new Vector2(.5f, 1f), new Vector2(0f, -4f),
                new Vector2(1f, 0f), new Vector2(-16f, 5f));
            Border("Legacy Border Bottom", parent, new Vector2(.5f, 0f), new Vector2(0f, 4f),
                new Vector2(1f, 0f), new Vector2(-16f, 5f));
            Border("Legacy Border Left", parent, new Vector2(0f, .5f), new Vector2(4f, 0f),
                new Vector2(0f, 1f), new Vector2(5f, -16f));
            Border("Legacy Border Right", parent, new Vector2(1f, .5f), new Vector2(-4f, 0f),
                new Vector2(0f, 1f), new Vector2(5f, -16f));
        }

        private static void Border(string name, Transform parent, Vector2 anchor, Vector2 position,
            Vector2 stretchAxis, Vector2 size)
        {
            var border = RuntimeUiFactory.Image(name, parent, JoseonUiPalette.AppraisalBorder);
            border.rectTransform.anchorMin = anchor - stretchAxis * .5f;
            border.rectTransform.anchorMax = anchor + stretchAxis * .5f;
            border.rectTransform.anchoredPosition = position;
            border.rectTransform.sizeDelta = size;
            border.raycastTarget = false;
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
