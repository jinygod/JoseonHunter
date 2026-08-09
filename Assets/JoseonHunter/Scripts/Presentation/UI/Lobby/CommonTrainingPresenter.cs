using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class CommonTrainingPresenter : MonoBehaviour
    {
        [SerializeField] private TrainingPageView view;
        [SerializeField] private Button[] trainingButtons;
        [SerializeField] private TMP_Text currentText;
        [SerializeField] private TMP_Text nextText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button resetButton;

        private readonly Dictionary<Button, UnityAction> rowActions = new();
        private MetaGameSession session;
        private Action refreshHeader;
        private CommonTrainingId selected;
        private UnityAction purchaseAction;
        private UnityAction resetAction;

        public string CurrentTextForTests => CurrentText.text;
        public string NextTextForTests => NextText.text;
        public string CostTextForTests => CostText.text;
        public string CapacityTextForTests => CapacityText.text;
        public string FeedbackTextForTests => FeedbackText.text;
        public bool PurchaseInteractableForTests => PurchaseButton.interactable;
        public string ButtonTextForTests(CommonTrainingId id) =>
            view != null ? view.Row(id).RankText.text : trainingButtons[(int)id].GetComponentInChildren<TMP_Text>().text;

        public void ConfigureView(TrainingPageView authoredView) => view = authoredView;

        public void InitializeAuthored(MetaGameSession value, Action onChanged)
        {
            if (view == null || !view.HasRequiredBindings)
                throw new InvalidOperationException("TrainingPageView is incomplete.");

            UnbindListeners();
            BindAuthoredView();
            InitializeBoundView(value, onChanged, true);
        }

        // Transitional adapter for the pre-Task-7 runtime-built Lobby shell.
        // Task 7 must remove this path once Lobby.unity owns a complete TrainingPageView hierarchy.
        public void Initialize(MetaGameSession value, Action onChanged) => InitializeLegacyRuntimeBuiltView(value, onChanged);

        public void InitializeLegacyRuntimeBuiltView(MetaGameSession value, Action onChanged)
        {
            UnbindListeners();
            Build();
            InitializeBoundView(value, onChanged, false);
        }

        public void Build()
        {
            if (transform.Find("Training Content Panel") != null)
            {
                EnsureLegacyPageView();
                return;
            }

            var title = LobbyUiFactory.Text("Training Title", transform, "수련", 34f,
                TextAlignmentOptions.Center, true);
            title.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(title.rectTransform, new Vector2(.04f, .91f), new Vector2(.96f, .96f), Vector2.zero, Vector2.zero);
            var description = LobbyUiFactory.Text("Training Description", transform,
                "수련 효과는 모든 출전에 적용되며, 항목별 최대치는 15%입니다.", 19f);
            description.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(description.rectTransform, new Vector2(.06f, .875f), new Vector2(.94f, .91f), Vector2.zero, Vector2.zero);
            capacityText = LobbyUiFactory.Text("Training Capacity", transform, string.Empty, 18f, TextAlignmentOptions.Center, true);
            capacityText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(capacityText.rectTransform, new Vector2(.06f, .84f), new Vector2(.94f, .875f), Vector2.zero, Vector2.zero);

            var contentPanel = LobbyUiFactory.Rect("Training Content Panel", transform);
            LobbyUiFactory.Anchor(contentPanel, new Vector2(.04f, .10f), new Vector2(.96f, .96f), Vector2.zero, Vector2.zero);
            var gridRoot = LobbyUiFactory.Rect("Training Grid", contentPanel);
            LobbyUiFactory.Anchor(gridRoot, new Vector2(.02173913f, .55813956f), new Vector2(.97826087f, .8372093f), Vector2.zero, Vector2.zero);
            var grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(190f, 86f);
            grid.spacing = new Vector2(14f, 12f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            var legacyRows = new LobbyTrainingRowView[6];
            trainingButtons = new Button[6];
            for (var index = 0; index < legacyRows.Length; index++)
            {
                var id = (CommonTrainingId)index;
                legacyRows[index] = CreateLegacyRow(gridRoot, id);
                trainingButtons[index] = legacyRows[index].Button;
            }

            var detail = LobbyUiFactory.Image("Training Summary Backplate", contentPanel, Color.white);
            PremiumPixelUiSkin.ApplyFrame(detail, PremiumFrame.ContentBackplate);
            LobbyUiFactory.Anchor(detail.rectTransform, new Vector2(.0326087f, .24418605f), new Vector2(.9673913f, .53488374f), Vector2.zero, Vector2.zero);
            currentText = DetailText("Current", detail.transform, .68f, .94f);
            nextText = DetailText("Next", detail.transform, .39f, .66f);
            costText = DetailText("Cost", detail.transform, .10f, .37f);
            purchaseButton = LobbyUiFactory.Button("Purchase Training", contentPanel, "수련하기", 25f, LobbyUiFactory.Crimson, LobbyUiFactory.HanjiLight);
            LobbyUiFactory.Anchor(purchaseButton.GetComponent<RectTransform>(), new Vector2(.04347826f, .02325581f), new Vector2(.6195652f, .19767442f), Vector2.zero, Vector2.zero);
            resetButton = LobbyUiFactory.Button("Reset Training", contentPanel, "전체 초기화", 21f, LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            LobbyUiFactory.Anchor(resetButton.GetComponent<RectTransform>(), new Vector2(.6630435f, .02325581f), new Vector2(.95652174f, .19767442f), Vector2.zero, Vector2.zero);
            feedbackText = LobbyUiFactory.Text("Training Feedback", contentPanel, string.Empty, 18f);
            feedbackText.color = LobbyUiFactory.AntiqueGold;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.01086957f, -.08139535f), new Vector2(.98913043f, .00581395f), Vector2.zero, Vector2.zero);
            EnsureLegacyPageView();
        }

        private LobbyTrainingRowView CreateLegacyRow(Transform parent, CommonTrainingId id)
        {
            var root = LobbyUiFactory.Rect("Training Row " + id, parent);
            var button = LobbyUiFactory.Button("Button", root, string.Empty, 21f, LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            PremiumPixelUiSkin.ApplyFrame(button.GetComponent<Image>(), PremiumFrame.SmallItem);
            LobbyUiFactory.Anchor(button.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var icon = LobbyUiFactory.Image("Icon", root, Color.white);
            icon.preserveAspect = true;
            LobbyUiFactory.Anchor(icon.rectTransform, new Vector2(.06f, .18f), new Vector2(.28f, .82f), Vector2.zero, Vector2.zero);
            var label = LobbyUiFactory.Text("Name", root, LobbyViewModels.TrainingName(id), 18f, TextAlignmentOptions.Left);
            LobbyUiFactory.Anchor(label.rectTransform, new Vector2(.32f, .51f), new Vector2(.92f, .91f), Vector2.zero, Vector2.zero);
            var rank = LobbyUiFactory.Text("Rank", root, "0 / 20", 16f, TextAlignmentOptions.Left);
            LobbyUiFactory.Anchor(rank.rectTransform, new Vector2(.32f, .09f), new Vector2(.92f, .47f), Vector2.zero, Vector2.zero);
            var progressRoot = LobbyUiFactory.Rect("Progress", root);
            LobbyUiFactory.Anchor(progressRoot, new Vector2(.32f, .08f), new Vector2(.92f, .18f), Vector2.zero, Vector2.zero);
            var track = LobbyUiFactory.Image("Track", progressRoot, Color.white);
            PremiumPixelUiSkin.ApplyFrame(track, PremiumFrame.SmallItem);
            LobbyUiFactory.Anchor(track.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fill = LobbyUiFactory.Image("Fill", progressRoot, new Color(.78f, .54f, .20f, 1f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            LobbyUiFactory.Anchor(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var progressValue = LobbyUiFactory.Text("Value", progressRoot, string.Empty, 1f);
            progressValue.gameObject.SetActive(false);
            var progress = progressRoot.gameObject.AddComponent<LobbyProgressBarView>();
            progress.Configure(fill, progressValue);
            var row = root.gameObject.AddComponent<LobbyTrainingRowView>();
            row.Configure(id, button, label, icon, rank, progress);
            return row;
        }

        private void EnsureLegacyPageView()
        {
            view = GetComponent<TrainingPageView>();
            if (view == null) view = gameObject.AddComponent<TrainingPageView>();
            var rows = GetComponentsInChildren<LobbyTrainingRowView>(true);
            if (rows.Length == 0)
            {
                var grid = transform.Find("Training Content Panel/Training Grid");
                if (grid != null)
                {
                    var legacyButtons = grid.GetComponentsInChildren<Button>(false);
                    for (var index = 0; index < legacyButtons.Length && index < 6; index++)
                        ConvertLegacyButtonToRow(legacyButtons[index], (CommonTrainingId)index);
                }
                rows = GetComponentsInChildren<LobbyTrainingRowView>(true);
            }
            Array.Sort(rows, (left, right) => left.TrainingId.CompareTo(right.TrainingId));
            var icons = new Sprite[6];
            view.Configure(rows, icons, currentText, nextText, costText, capacityText, purchaseButton, resetButton, feedbackText);
        }

        private static void ConvertLegacyButtonToRow(Button button, CommonTrainingId id)
        {
            button.name = "Training Row " + id;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.name = "Name";
            label.alignment = TextAlignmentOptions.Left;
            LobbyUiFactory.Anchor(label.rectTransform, new Vector2(.30f, .51f), new Vector2(.92f, .91f), Vector2.zero, Vector2.zero);
            var icon = LobbyUiFactory.Image("Icon", button.transform, Color.white);
            icon.preserveAspect = true;
            LobbyUiFactory.Anchor(icon.rectTransform, new Vector2(.06f, .18f), new Vector2(.26f, .82f), Vector2.zero, Vector2.zero);
            var rank = LobbyUiFactory.Text("Rank", button.transform, "0 / 20", 16f, TextAlignmentOptions.Left);
            LobbyUiFactory.Anchor(rank.rectTransform, new Vector2(.30f, .22f), new Vector2(.92f, .51f), Vector2.zero, Vector2.zero);
            var progressRoot = LobbyUiFactory.Rect("Progress", button.transform);
            LobbyUiFactory.Anchor(progressRoot, new Vector2(.30f, .08f), new Vector2(.92f, .18f), Vector2.zero, Vector2.zero);
            var track = LobbyUiFactory.Image("Track", progressRoot, Color.white);
            PremiumPixelUiSkin.ApplyFrame(track, PremiumFrame.SmallItem);
            LobbyUiFactory.Anchor(track.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fill = LobbyUiFactory.Image("Fill", progressRoot, new Color(.78f, .54f, .20f, 1f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            LobbyUiFactory.Anchor(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var value = LobbyUiFactory.Text("Value", progressRoot, string.Empty, 1f);
            value.gameObject.SetActive(false);
            var progress = progressRoot.gameObject.AddComponent<LobbyProgressBarView>();
            progress.Configure(fill, value);
            button.gameObject.AddComponent<LobbyTrainingRowView>().Configure(id, button, label, icon, rank, progress);
        }

        private void BindAuthoredView()
        {
            trainingButtons = null;
            currentText = view.CurrentEffectText;
            nextText = view.NextEffectText;
            costText = view.CostText;
            capacityText = view.CapacityText;
            purchaseButton = view.PurchaseButton;
            resetButton = view.ResetButton;
            feedbackText = view.FeedbackText;
        }

        private void InitializeBoundView(MetaGameSession value, Action onChanged, bool authored)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            session = value;
            refreshHeader = onChanged;
            if (authored)
            {
                SetButtonLabel(purchaseButton, "수련하기");
                SetButtonLabel(resetButton, "전체 초기화");
                PremiumPixelUiSkin.ApplyAction(purchaseButton, PremiumActionStyle.Primary);
                PremiumPixelUiSkin.ApplyAction(resetButton, PremiumActionStyle.Secondary);
            }
            else
            {
                JoseonButtonSkin.Apply(purchaseButton, JoseonButtonStyle.Primary);
                JoseonButtonSkin.Apply(resetButton, JoseonButtonStyle.Secondary);
            }
            BindListeners();
            Refresh();
        }

        private void BindListeners()
        {
            foreach (var row in Rows)
            {
                var id = row.TrainingId;
                UnityAction action = () => Select(id);
                row.Button.onClick.AddListener(action);
                rowActions.Add(row.Button, action);
            }
            purchaseAction = Purchase;
            resetAction = ResetAll;
            PurchaseButton.onClick.AddListener(purchaseAction);
            ResetButton.onClick.AddListener(resetAction);
        }

        private void UnbindListeners()
        {
            foreach (var binding in rowActions) RemoveOwnedListener(binding.Key, binding.Value);
            rowActions.Clear();
            RemoveOwnedListener(purchaseButton, purchaseAction);
            RemoveOwnedListener(resetButton, resetAction);
            purchaseAction = null;
            resetAction = null;
        }

        public void SelectForTests(CommonTrainingId id) => Select(id);
        public void PurchaseForTests() => Purchase();
        public void ResetForTests() => ResetAll();

        private void Select(CommonTrainingId id)
        {
            selected = id;
            FeedbackText.text = string.Empty;
            Refresh();
        }

        private void Purchase()
        {
            var result = session.PurchaseTraining(selected);
            FeedbackText.text = result.Success ? "수련 성과가 모든 출전에 적용됩니다."
                : result.Error == ProgressionError.InsufficientCoins ? "엽전이 부족합니다."
                : result.Error == ProgressionError.AccountLevelRequired ? CapacityFeedback(new CommonTrainingProgression(session.Data))
                : result.Error == ProgressionError.MaximumReached ? "이미 최대 단계입니다."
                : "저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            Refresh();
        }

        private void ResetAll()
        {
            var refund = 0;
            foreach (var spent in session.Data.CommonTrainingSpentCoins.Values) refund += spent;
            var result = session.ResetTraining();
            FeedbackText.text = result.Success ? $"수련을 초기화하고 엽전 {refund:N0}을 돌려받았습니다." : "저장하지 못했습니다. 다시 시도해 주세요.";
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
            CapacityText.text = $"총 수련 {progression.TotalRanks}/{progression.Capacity} · 계정 {AccountProgression.StateFor(session.Data.AccountExperience).Level}레벨 한도";
            CurrentText.text = $"현재 {effect} +{FormatPercent(CommonTrainingProgression.BonusForRank(rank))}%";
            NextText.text = trackMaximum ? "최대 단계에 도달했습니다" : $"강화 후 {effect} +{FormatPercent(CommonTrainingProgression.BonusForRank(rank + 1))}%";
            var cost = trackMaximum ? 0 : CommonTrainingProgression.CostForRank(rank + 1);
            CostText.text = trackMaximum ? "추가 엽전 필요 없음" : $"필요 엽전 {cost:N0} · 강화 후 {Math.Max(0, session.Data.Coins - cost):N0}";
            PurchaseButton.interactable = !trackMaximum && !capacityReached;
            if (!trackMaximum && capacityReached) FeedbackText.text = CapacityFeedback(progression);
            foreach (var row in Rows)
            {
                var id = row.TrainingId;
                row.Render(LobbyViewModels.TrainingName(id), view.Icon(id), progression.Rank(id),
                    CommonTrainingProgression.MaximumRankPerTrack, selected == id);
            }
        }

        private IEnumerable<LobbyTrainingRowView> Rows => view != null && view.Rows != null ? view.Rows : Array.Empty<LobbyTrainingRowView>();
        private TMP_Text CurrentText => view != null ? view.CurrentEffectText : currentText;
        private TMP_Text NextText => view != null ? view.NextEffectText : nextText;
        private TMP_Text CostText => view != null ? view.CostText : costText;
        private TMP_Text CapacityText => view != null ? view.CapacityText : capacityText;
        private TMP_Text FeedbackText => view != null ? view.FeedbackText : feedbackText;
        private Button PurchaseButton => view != null ? view.PurchaseButton : purchaseButton;
        private Button ResetButton => view != null ? view.ResetButton : resetButton;

        private static void SetButtonLabel(Button button, string value)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = value;
        }

        private static TMP_Text DetailText(string name, Transform parent, float minY, float maxY)
        {
            var text = LobbyUiFactory.Text(name, parent, string.Empty, 24f);
            text.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(text.rectTransform, new Vector2(.05f, minY), new Vector2(.95f, maxY), Vector2.zero, Vector2.zero);
            return text;
        }

        private static void RemoveOwnedListener(Button button, UnityAction action)
        {
            if (button != null && action != null) button.onClick.RemoveListener(action);
        }

        private static string FormatPercent(float bonus) => (bonus * 100f).ToString("0.#");

        private static string CapacityFeedback(CommonTrainingProgression progression) =>
            progression.Capacity >= CommonTrainingProgression.MaximumTotalRanks
                ? "총 수련 최대치에 도달했습니다."
                : $"계정 레벨 {progression.NextCapacityLevel}에서 추가 수련이 열립니다.";

        private void OnDestroy() => UnbindListeners();
    }
}
