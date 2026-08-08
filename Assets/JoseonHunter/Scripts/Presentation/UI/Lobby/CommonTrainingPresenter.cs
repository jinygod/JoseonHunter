using System;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
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
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button resetButton;
        private MetaGameSession session;
        private Action refreshHeader;
        private CommonTrainingId selected;

        public string CurrentTextForTests => currentText.text;
        public string NextTextForTests => nextText.text;
        public string CostTextForTests => costText.text;
        public string CapacityTextForTests => capacityText.text;
        public string FeedbackTextForTests => feedbackText.text;
        public bool PurchaseInteractableForTests => purchaseButton.interactable;
        public string ButtonTextForTests(CommonTrainingId id) =>
            trainingButtons[(int)id].GetComponentInChildren<TMP_Text>().text;

        public void Build()
        {
            if (transform.Find("Training Summary Backplate") != null)
            {
                EnsureExpandedView();
                return;
            }
            ArchiveLegacyLayoutIfPresent();
            var title = LobbyUiFactory.Text("Training Title", transform, "수련", 34f,
                TextAlignmentOptions.Center, true);
            title.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(title.rectTransform, new Vector2(.04f, .91f), new Vector2(.96f, .96f),
                Vector2.zero, Vector2.zero);
            var description = LobbyUiFactory.Text("Training Description", transform,
                "수련 효과는 모든 출전에 적용되며, 항목별 최대치는 15%입니다.", 19f);
            description.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(description.rectTransform, new Vector2(.06f, .875f), new Vector2(.94f, .91f),
                Vector2.zero, Vector2.zero);
            capacityText = LobbyUiFactory.Text("Training Capacity", transform, string.Empty, 18f,
                TextAlignmentOptions.Center, true);
            capacityText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(capacityText.rectTransform, new Vector2(.06f, .84f), new Vector2(.94f, .875f),
                Vector2.zero, Vector2.zero);

            var contentPanel = LobbyUiFactory.Rect("Training Content Panel", transform);
            LobbyUiFactory.Anchor(contentPanel, new Vector2(.04f, .10f), new Vector2(.96f, .96f),
                Vector2.zero, Vector2.zero);

            var gridRoot = LobbyUiFactory.Rect("Training Grid", contentPanel);
            LobbyUiFactory.Anchor(gridRoot, new Vector2(.02173913f, .55813956f), new Vector2(.97826087f, .8372093f),
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
                PremiumPixelUiSkin.ApplyFrame(trainingButtons[index].GetComponent<Image>(), PremiumFrame.SmallItem);
            }

            var detail = LobbyUiFactory.Image("Training Summary Backplate", contentPanel, Color.white);
            PremiumPixelUiSkin.ApplyFrame(detail, PremiumFrame.ContentBackplate);
            LobbyUiFactory.Anchor(detail.rectTransform, new Vector2(.0326087f, .24418605f), new Vector2(.9673913f, .53488374f),
                Vector2.zero, Vector2.zero);
            currentText = DetailText("Current", detail.transform, .68f, .94f);
            nextText = DetailText("Next", detail.transform, .39f, .66f);
            costText = DetailText("Cost", detail.transform, .10f, .37f);

            purchaseButton = LobbyUiFactory.Button("Purchase Training", contentPanel, "수련하기", 25f,
                LobbyUiFactory.Crimson, LobbyUiFactory.HanjiLight);
            LobbyUiFactory.Anchor(purchaseButton.GetComponent<RectTransform>(), new Vector2(.04347826f, .02325581f),
                new Vector2(.6195652f, .19767442f), Vector2.zero, Vector2.zero);
            resetButton = LobbyUiFactory.Button("Reset Training", contentPanel, "전체 초기화", 21f,
                LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            LobbyUiFactory.Anchor(resetButton.GetComponent<RectTransform>(), new Vector2(.6630435f, .02325581f),
                new Vector2(.95652174f, .19767442f), Vector2.zero, Vector2.zero);
            feedbackText = LobbyUiFactory.Text("Training Feedback", contentPanel, string.Empty, 18f);
            feedbackText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.01086957f, -.08139535f), new Vector2(.98913043f, .00581395f),
                Vector2.zero, Vector2.zero);
        }

        private void ArchiveLegacyLayoutIfPresent()
        {
            if (transform.Find("Training Title") == null) return;
            var archive = LobbyUiFactory.Rect("Legacy Training Layout", transform);
            var legacyChildren = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform)
                if (child != archive) legacyChildren.Add(child);
            foreach (var child in legacyChildren)
                child.SetParent(archive, false);
            archive.gameObject.SetActive(false);
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
            Build();
            EnsureExpandedView();
            JoseonButtonSkin.Apply(purchaseButton, JoseonButtonStyle.Primary);
            JoseonButtonSkin.Apply(resetButton, JoseonButtonStyle.Secondary);
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
                    : result.Error == ProgressionError.AccountLevelRequired
                        ? CapacityFeedback(new CommonTrainingProgression(session.Data))
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
            var progression = new CommonTrainingProgression(session.Data);
            var rank = progression.Rank(selected);
            var effect = LobbyViewModels.TrainingEffect(selected);
            var trackMaximum = rank >= CommonTrainingProgression.MaximumRankPerTrack;
            var capacityReached = progression.TotalRanks >= progression.Capacity;
            capacityText.text = $"총 수련 {progression.TotalRanks}/{progression.Capacity} · 계정 {AccountProgression.StateFor(session.Data.AccountExperience).Level}레벨 한도";
            currentText.text = $"현재 {effect} +{FormatPercent(CommonTrainingProgression.BonusForRank(rank))}%";
            nextText.text = trackMaximum
                ? "최대 단계에 도달했습니다"
                : $"강화 후 {effect} +{FormatPercent(CommonTrainingProgression.BonusForRank(rank + 1))}%";
            var cost = trackMaximum ? 0 : CommonTrainingProgression.CostForRank(rank + 1);
            costText.text = trackMaximum
                ? "추가 엽전 필요 없음"
                : $"필요 엽전 {cost:N0} · 강화 후 {Math.Max(0, session.Data.Coins - cost):N0}";
            purchaseButton.interactable = !trackMaximum && !capacityReached;
            if (!trackMaximum && capacityReached)
                feedbackText.text = CapacityFeedback(progression);
            for (var index = 0; index < trainingButtons.Length; index++)
            {
                var id = (CommonTrainingId)index;
                var idRank = progression.Rank(id);
                trainingButtons[index].GetComponentInChildren<TMP_Text>().text =
                    $"{LobbyViewModels.TrainingName(id)}\n{idRank}/{CommonTrainingProgression.MaximumRankPerTrack}";
            }
        }

        private void EnsureExpandedView()
        {
            var description = transform.Find("Training Description")?.GetComponent<TMP_Text>();
            if (description != null)
                description.text = "수련 효과는 모든 출전에 적용되며, 항목별 최대치는 15%입니다.";
            capacityText = transform.Find("Training Capacity")?.GetComponent<TMP_Text>();
            if (capacityText != null) return;
            capacityText = LobbyUiFactory.Text("Training Capacity", transform, string.Empty, 18f,
                TextAlignmentOptions.Center, true);
            capacityText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(capacityText.rectTransform, new Vector2(.06f, .84f), new Vector2(.94f, .875f),
                Vector2.zero, Vector2.zero);
        }

        private static string FormatPercent(float bonus) => (bonus * 100f).ToString("0.#");

        private static string CapacityFeedback(CommonTrainingProgression progression) =>
            progression.Capacity >= CommonTrainingProgression.MaximumTotalRanks
                ? "총 수련 최대치에 도달했습니다."
                : $"계정 레벨 {progression.NextCapacityLevel}에서 추가 수련이 열립니다.";
    }
}
