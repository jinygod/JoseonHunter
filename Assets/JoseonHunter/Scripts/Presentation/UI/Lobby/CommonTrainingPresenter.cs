using System;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class CommonTrainingPresenter : MonoBehaviour
    {
        [SerializeField] private Button[] trainingButtons;
        [SerializeField] private TMP_Text currentText;
        [SerializeField] private TMP_Text nextText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button resetButton;
        private MetaGameSession session;
        private Action refreshHeader;
        private CommonTrainingId selected;

        public string CurrentTextForTests => currentText.text;
        public string NextTextForTests => nextText.text;
        public string CostTextForTests => costText.text;

        public void Build()
        {
            if (transform.Find("Training Title") != null) return;
            var title = LobbyUiFactory.Text("Training Title", transform, "공통 수련", 34f,
                TextAlignmentOptions.Center, true);
            title.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(title.rectTransform, new Vector2(.04f, .90f), new Vector2(.96f, .985f),
                Vector2.zero, Vector2.zero);
            var description = LobbyUiFactory.Text("Training Description", transform,
                "모든 무기에 적용되며, 수련별 최대치는 10%입니다.", 19f);
            description.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(description.rectTransform, new Vector2(.06f, .82f), new Vector2(.94f, .90f),
                Vector2.zero, Vector2.zero);

            var gridRoot = LobbyUiFactory.Rect("Training Grid", transform);
            LobbyUiFactory.Anchor(gridRoot, new Vector2(.06f, .55f), new Vector2(.94f, .81f),
                Vector2.zero, Vector2.zero);
            var grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(190f, 86f);
            grid.spacing = new Vector2(14f, 12f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            trainingButtons = new Button[6];
            for (var index = 0; index < trainingButtons.Length; index++)
            {
                var id = (CommonTrainingId)index;
                trainingButtons[index] = LobbyUiFactory.Button("Training " + id, gridRoot,
                    LobbyViewModels.TrainingName(id), 21f,
                    LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            }

            var detail = LobbyUiFactory.Image("Training Detail", transform, LobbyUiFactory.NightInk);
            LobbyUiFactory.Anchor(detail.rectTransform, new Vector2(.07f, .29f), new Vector2(.93f, .53f),
                Vector2.zero, Vector2.zero);
            currentText = DetailText("Current", detail.transform, .68f, .94f);
            nextText = DetailText("Next", detail.transform, .39f, .66f);
            costText = DetailText("Cost", detail.transform, .10f, .37f);

            purchaseButton = LobbyUiFactory.Button("Purchase Training", transform, "수련하기", 25f,
                LobbyUiFactory.Crimson, LobbyUiFactory.HanjiLight);
            LobbyUiFactory.Anchor(purchaseButton.GetComponent<RectTransform>(), new Vector2(.12f, .09f),
                new Vector2(.61f, .27f), Vector2.zero, Vector2.zero);
            resetButton = LobbyUiFactory.Button("Reset Training", transform, "전체 초기화", 21f,
                LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            LobbyUiFactory.Anchor(resetButton.GetComponent<RectTransform>(), new Vector2(.63f, .09f),
                new Vector2(.88f, .27f), Vector2.zero, Vector2.zero);
            feedbackText = LobbyUiFactory.Text("Training Feedback", transform, string.Empty, 18f);
            feedbackText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.05f, .01f), new Vector2(.95f, .08f),
                Vector2.zero, Vector2.zero);
        }

        private static TMP_Text DetailText(string name, Transform parent, float minY, float maxY)
        {
            var text = LobbyUiFactory.Text(name, parent, string.Empty, 24f);
            text.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(text.rectTransform, new Vector2(.05f, minY), new Vector2(.95f, maxY),
                Vector2.zero, Vector2.zero);
            return text;
        }

        public void Initialize(MetaGameSession value, Action onChanged)
        {
            session = value;
            refreshHeader = onChanged;
            for (var index = 0; index < trainingButtons.Length; index++)
            {
                var captured = (CommonTrainingId)index;
                trainingButtons[index].onClick.RemoveAllListeners();
                trainingButtons[index].onClick.AddListener(() => Select(captured));
            }
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(Purchase);
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetAll);
            Refresh();
        }

        public void SelectForTests(CommonTrainingId id) => Select(id);
        public void PurchaseForTests() => Purchase();
        public void ResetForTests() => ResetAll();

        private void Select(CommonTrainingId id)
        {
            selected = id;
            feedbackText.text = string.Empty;
            Refresh();
        }

        private void Purchase()
        {
            var result = session.PurchaseTraining(selected);
            feedbackText.text = result.Success
                ? "수련 성과가 모든 출전에 적용됩니다."
                : result.Error == ProgressionError.InsufficientCoins
                    ? "엽전이 부족합니다."
                    : result.Error == ProgressionError.MaximumReached
                        ? "이미 최대 단계입니다."
                        : "저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            Refresh();
        }

        private void ResetAll()
        {
            var refund = 0;
            foreach (var spent in session.Data.CommonTrainingSpentCoins.Values) refund += spent;
            var result = session.ResetTraining();
            feedbackText.text = result.Success
                ? $"수련을 초기화하고 엽전 {refund:N0}을 돌려받았습니다."
                : "저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            Refresh();
        }

        private void Refresh()
        {
            if (session == null) return;
            var key = selected.ToString();
            var rank = session.Data.CommonTrainingRanks.TryGetValue(key, out var value) ? value : 0;
            var effect = LobbyViewModels.TrainingEffect(selected);
            currentText.text = $"현재 {effect} +{rank * 2}%";
            nextText.text = rank >= 5 ? "최대 단계에 도달했습니다" : $"강화 후 {effect} +{(rank + 1) * 2}%";
            costText.text = rank >= 5 ? "추가 엽전 필요 없음" : $"필요 엽전 {CommonTrainingProgression.Costs[rank]:N0}";
            purchaseButton.interactable = rank < 5;
            for (var index = 0; index < trainingButtons.Length; index++)
            {
                var id = (CommonTrainingId)index;
                var idRank = session.Data.CommonTrainingRanks.TryGetValue(id.ToString(), out var idValue) ? idValue : 0;
                trainingButtons[index].GetComponentInChildren<TMP_Text>().text =
                    $"{LobbyViewModels.TrainingName(id)}\n{idRank}/5";
            }
        }
    }
}
