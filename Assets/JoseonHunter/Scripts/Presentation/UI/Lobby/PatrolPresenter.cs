using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class PatrolPresenter : MonoBehaviour
    {
        [SerializeField] private Image constableImage;
        [SerializeField] private TMP_Text presetText;
        [SerializeField] private TMP_Text weaponText;
        [SerializeField] private TMP_Text styleText;
        [SerializeField] private TMP_Text recordText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button previousPresetButton;
        [SerializeField] private Button nextPresetButton;
        [SerializeField] private Button previousWeaponButton;
        [SerializeField] private Button nextWeaponButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button patrolButton;
        private MetaGameSession session;
        private Action refreshHeader;
        private int selectedPreset;
        private WeaponId selectedWeapon = WeaponId.HwandoFlyingBlade;

        public Image ConstableImage => constableImage;

        public void Build()
        {
            if (transform.Find("Patrol Title") != null) return;
            var title = LobbyUiFactory.Text("Patrol Title", transform, "출전 준비", 36f,
                TextAlignmentOptions.Center, true);
            LobbyUiFactory.Anchor(title.rectTransform, new Vector2(.04f, .91f), new Vector2(.96f, .985f),
                Vector2.zero, Vector2.zero);

            constableImage = LobbyUiFactory.Image("Rookie Constable", transform, Color.white);
            constableImage.preserveAspect = true;
            LobbyUiFactory.Anchor(constableImage.rectTransform, new Vector2(.05f, .30f), new Vector2(.43f, .87f),
                Vector2.zero, Vector2.zero);

            var detail = LobbyUiFactory.Image("Patrol Detail", transform, LobbyUiFactory.Hanji);
            LobbyUiFactory.Anchor(detail.rectTransform, new Vector2(.43f, .31f), new Vector2(.95f, .87f),
                Vector2.zero, Vector2.zero);
            presetText = TextLine("Preset", detail.transform, .78f, .95f, 28f, true);
            weaponText = TextLine("Starting Weapon", detail.transform, .56f, .76f, 27f);
            styleText = TextLine("Style", detail.transform, .36f, .55f, 22f);
            recordText = TextLine("Record", detail.transform, .17f, .35f, 20f);
            var difficulty = TextLine("Difficulty", detail.transform, .04f, .16f, 20f);
            difficulty.text = "난이도 · 보통";

            previousPresetButton = SmallButton("Previous Preset", "이전 편성", .05f, .23f, .205f, .29f);
            nextPresetButton = SmallButton("Next Preset", "다음 편성", .25f, .43f, .205f, .29f);
            previousWeaponButton = SmallButton("Previous Weapon", "이전 무기", .45f, .63f, .205f, .29f);
            nextWeaponButton = SmallButton("Next Weapon", "다음 무기", .65f, .83f, .205f, .29f);
            saveButton = SmallButton("Save Preset", "편성 저장", .05f, .43f, .115f, .19f);
            patrolButton = SmallButton("Start Patrol", "출전", .45f, .95f, .115f, .19f, 29f);
            feedbackText = LobbyUiFactory.Text("Patrol Feedback", transform, string.Empty, 18f);
            feedbackText.color = LobbyUiFactory.Brown;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.05f, .035f), new Vector2(.95f, .105f),
                Vector2.zero, Vector2.zero);
        }

        private TMP_Text TextLine(string name, Transform parent, float minY, float maxY, float size, bool title = false)
        {
            var text = LobbyUiFactory.Text(name, parent, string.Empty, size, TextAlignmentOptions.Center, title);
            LobbyUiFactory.Anchor(text.rectTransform, new Vector2(.05f, minY), new Vector2(.95f, maxY),
                Vector2.zero, Vector2.zero);
            return text;
        }

        private Button SmallButton(string name, string label, float minX, float maxX, float minY, float maxY,
            float size = 20f)
        {
            var button = LobbyUiFactory.Button(name, transform, label, size);
            LobbyUiFactory.Anchor(button.GetComponent<RectTransform>(), new Vector2(minX, minY),
                new Vector2(maxX, maxY), Vector2.zero, Vector2.zero);
            return button;
        }

        public void Initialize(MetaGameSession value, Action onChanged)
        {
            session = value;
            refreshHeader = onChanged;
            selectedPreset = Mathf.Clamp(session.Data.ActivePatrolLoadoutIndex, 0, 2);
            LoadSelectedPreset();
            previousPresetButton.onClick.RemoveAllListeners();
            nextPresetButton.onClick.RemoveAllListeners();
            previousWeaponButton.onClick.RemoveAllListeners();
            nextWeaponButton.onClick.RemoveAllListeners();
            saveButton.onClick.RemoveAllListeners();
            patrolButton.onClick.RemoveAllListeners();
            previousPresetButton.onClick.AddListener(() => SelectPreset((selectedPreset + 2) % 3));
            nextPresetButton.onClick.AddListener(() => SelectPreset((selectedPreset + 1) % 3));
            previousWeaponButton.onClick.AddListener(() => CycleWeapon(-1));
            nextWeaponButton.onClick.AddListener(() => CycleWeapon(1));
            saveButton.onClick.AddListener(() => Save());
            patrolButton.onClick.AddListener(StartPatrol);
            Refresh();
        }

        public void SelectPresetForTests(int index) => SelectPreset(index);
        public void SelectStartingWeaponForTests(WeaponId weaponId) { selectedWeapon = weaponId; Refresh(); }
        public bool SaveForTests() => Save();

        private void SelectPreset(int index)
        {
            selectedPreset = Mathf.Clamp(index, 0, 2);
            LoadSelectedPreset();
            feedbackText.text = string.Empty;
            Refresh();
        }

        private void LoadSelectedPreset()
        {
            var id = session.Data.PatrolLoadouts[selectedPreset].StartingWeaponId;
            selectedWeapon = WeaponRoster.All.FirstOrDefault(weapon => weapon.Value == id);
            if (string.IsNullOrEmpty(selectedWeapon.Value)) selectedWeapon = WeaponId.HwandoFlyingBlade;
        }

        private void CycleWeapon(int direction)
        {
            var index = WeaponRoster.All.ToList().FindIndex(id => id.Equals(selectedWeapon));
            index = (index + direction + WeaponRoster.All.Count) % WeaponRoster.All.Count;
            selectedWeapon = WeaponRoster.All[index];
            feedbackText.text = "편성 저장을 누르면 적용됩니다.";
            Refresh();
        }

        private bool Save()
        {
            var dto = session.Data.PatrolLoadouts[selectedPreset];
            var styles = new Dictionary<WeaponId, WeaponLegacyPathId>();
            foreach (var weaponId in WeaponRoster.All)
            {
                if (!dto.WeaponStyleIds.TryGetValue(weaponId.Value, out var value) || string.IsNullOrEmpty(value) ||
                    !session.Data.UnlockedWeaponStyles.Contains(value)) continue;
                var path = new WeaponLegacyPathId(value);
                if (WeaponLegacyCatalog.TryGet(path, out var definition) && definition.WeaponId.Equals(weaponId))
                    styles[weaponId] = path;
            }
            var loadout = new PatrolLoadout(dto.Name, selectedWeapon, styles,
                string.IsNullOrWhiteSpace(dto.DifficultyId) ? "normal" : dto.DifficultyId);
            var result = session.SaveLoadout(selectedPreset, loadout);
            feedbackText.text = result.Success ? "편성을 저장했습니다." : "저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            Refresh();
            return result.Success;
        }

        private void StartPatrol()
        {
            if (session.Router.IsRouting || !Save()) return;
            patrolButton.interactable = false;
            StartCoroutine(LoadGameplay());
        }

        private IEnumerator LoadGameplay()
        {
            yield return session.Router.LoadGameplay();
            if (patrolButton != null) patrolButton.interactable = true;
        }

        private void Refresh()
        {
            if (session == null) return;
            var dto = session.Data.PatrolLoadouts[selectedPreset];
            presetText.text = $"편성 {selectedPreset + 1} · {dto.Name}";
            weaponText.text = $"시작 무기\n{LobbyViewModels.WeaponName(selectedWeapon)}";
            var styleName = "기본식";
            if (dto.WeaponStyleIds.TryGetValue(selectedWeapon.Value, out var styleId) &&
                !string.IsNullOrEmpty(styleId) && session.Data.UnlockedWeaponStyles.Contains(styleId) &&
                WeaponLegacyCatalog.TryGet(new WeaponLegacyPathId(styleId), out var definition))
                styleName = definition.DisplayName;
            styleText.text = $"운용법 · {styleName}";
            var best = session.Data.BestPatrolResults.TryGetValue("victory_kills", out var value) ? value : 0;
            recordText.text = best > 0 ? $"최고 승리 처치 {best:N0}" : "아직 승리 기록이 없습니다";
        }
    }
}
