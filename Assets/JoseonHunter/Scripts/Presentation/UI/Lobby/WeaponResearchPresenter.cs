using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class WeaponResearchPresenter : MonoBehaviour
    {
        [SerializeField] private Button[] weaponButtons;
        [SerializeField] private Button[] styleButtons;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text masteryText;
        [SerializeField] private TMP_Text feedbackText;
        private MetaGameSession session;
        private Action refreshHeader;
        private int selectedWeaponIndex;

        public int WeaponCountForTests => WeaponRoster.All.Count;
        public int StyleCountForTests => styleButtons?.Length ?? 0;

        public void Build()
        {
            if (transform.Find("Research Title") != null) return;
            titleText = LobbyUiFactory.Text("Research Title", transform, "무기 연구", 34f,
                TextAlignmentOptions.Center, true);
            LobbyUiFactory.Anchor(titleText.rectTransform, new Vector2(.04f, .91f), new Vector2(.96f, .985f),
                Vector2.zero, Vector2.zero);

            var weaponGrid = LobbyUiFactory.Rect("Weapon Grid", transform);
            LobbyUiFactory.Anchor(weaponGrid, new Vector2(.04f, .68f), new Vector2(.96f, .90f),
                Vector2.zero, Vector2.zero);
            var grid = weaponGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(145f, 66f);
            grid.spacing = new Vector2(10f, 8f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            weaponButtons = new Button[WeaponRoster.All.Count];
            for (var index = 0; index < weaponButtons.Length; index++)
                weaponButtons[index] = LobbyUiFactory.Button("Weapon " + index, weaponGrid,
                    LobbyViewModels.WeaponName(WeaponRoster.All[index]), 19f);

            masteryText = LobbyUiFactory.Text("Mastery", transform, string.Empty, 23f);
            LobbyUiFactory.Anchor(masteryText.rectTransform, new Vector2(.06f, .61f), new Vector2(.94f, .675f),
                Vector2.zero, Vector2.zero);

            var styles = LobbyUiFactory.Rect("Style Cards", transform);
            LobbyUiFactory.Anchor(styles, new Vector2(.05f, .14f), new Vector2(.95f, .60f),
                Vector2.zero, Vector2.zero);
            var layout = styles.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            styleButtons = new Button[3];
            for (var index = 0; index < styleButtons.Length; index++)
                styleButtons[index] = LobbyUiFactory.Button("Style " + index, styles, string.Empty, 20f);

            feedbackText = LobbyUiFactory.Text("Research Feedback", transform, string.Empty, 19f);
            feedbackText.color = LobbyUiFactory.Brown;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.05f, .045f), new Vector2(.95f, .13f),
                Vector2.zero, Vector2.zero);
        }

        public void Initialize(MetaGameSession value, Action onChanged)
        {
            session = value;
            refreshHeader = onChanged;
            for (var index = 0; index < weaponButtons.Length; index++)
            {
                var captured = index;
                weaponButtons[index].onClick.RemoveAllListeners();
                weaponButtons[index].onClick.AddListener(() => SelectWeapon(captured));
            }
            for (var index = 0; index < styleButtons.Length; index++)
            {
                var captured = index;
                styleButtons[index].onClick.RemoveAllListeners();
                styleButtons[index].onClick.AddListener(() => ActivateStyle(captured));
            }
            Refresh();
        }

        public string SelectedStyleStateForTests(int index) => StateFor(index);
        public void ActivateStyleForTests(int index) => ActivateStyle(index);

        private void SelectWeapon(int index)
        {
            selectedWeaponIndex = Mathf.Clamp(index, 0, WeaponRoster.All.Count - 1);
            feedbackText.text = string.Empty;
            Refresh();
        }

        private void ActivateStyle(int styleIndex)
        {
            var weaponId = WeaponRoster.All[selectedWeaponIndex];
            var styles = WeaponMasteryCatalog.StylesFor(weaponId);
            if (styleIndex < 0 || styleIndex >= styles.Count) return;
            var style = styles[styleIndex];
            if (!style.IsBase && !session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value))
            {
                var purchase = session.PurchaseStyle(weaponId, style.LegacyPathId);
                if (!purchase.Success)
                {
                    feedbackText.text = purchase.Error == ProgressionError.InsufficientMastery
                        ? "숙련도가 부족합니다. 이 무기로 막타를 더 기록하세요."
                        : purchase.Error == ProgressionError.InsufficientCoins
                            ? "엽전이 부족합니다."
                            : "아직 해금할 수 없습니다.";
                    Refresh();
                    return;
                }
            }

            var loadout = session.ActiveLoadout;
            var equipped = new Dictionary<WeaponId, WeaponLegacyPathId>(loadout.Styles);
            if (style.IsBase) equipped.Remove(weaponId);
            else equipped[weaponId] = style.LegacyPathId;
            var updated = new PatrolLoadout(loadout.Name, loadout.StartingWeapon, equipped, loadout.DifficultyId);
            var saved = session.SaveLoadout(session.Data.ActivePatrolLoadoutIndex, updated);
            feedbackText.text = saved.Success ? "운용법을 장착했습니다." : "저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            Refresh();
        }

        private string StateFor(int styleIndex)
        {
            var weaponId = WeaponRoster.All[selectedWeaponIndex];
            var style = WeaponMasteryCatalog.StylesFor(weaponId)[styleIndex];
            var equipped = session.ActiveLoadout.StyleFor(weaponId);
            if (style.IsBase) return string.IsNullOrEmpty(equipped.Value) ? "장착 중" : "기본식";
            if (equipped.Equals(style.LegacyPathId)) return "장착 중";
            if (session.Data.UnlockedWeaponStyles.Contains(style.LegacyPathId.Value)) return "해금 완료";
            var mastery = session.Data.WeaponMasteryPoints.TryGetValue(weaponId.Value, out var points) ? points : 0;
            if (mastery < style.RequiredMastery) return "연구 중";
            return session.Data.Coins >= style.CoinCost ? "해금 가능" : "엽전 부족";
        }

        private void Refresh()
        {
            if (session == null) return;
            var weaponId = WeaponRoster.All[selectedWeaponIndex];
            var mastery = session.Data.WeaponMasteryPoints.TryGetValue(weaponId.Value, out var value) ? value : 0;
            titleText.text = $"{LobbyViewModels.WeaponName(weaponId)} 연구";
            masteryText.text = $"숙련도 {mastery:N0} · 막타를 기록하면 이 무기의 숙련도가 오릅니다";
            var styles = WeaponMasteryCatalog.StylesFor(weaponId);
            for (var index = 0; index < styleButtons.Length; index++)
            {
                var style = styles[index];
                var requirement = style.IsBase ? "처음부터 사용 가능" : $"숙련도 {style.RequiredMastery:N0} · 엽전 {style.CoinCost:N0}";
                styleButtons[index].GetComponentInChildren<TMP_Text>().text =
                    $"{style.DisplayName}  ·  {StateFor(index)}\n{style.Benefit} / {style.Tradeoff}\n{requirement}";
            }
        }
    }
}
