using System;
using System.Collections;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Domain.Save;
using JoseonHunter.Runtime.Meta;
using JoseonHunter.Runtime.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class PatrolPresenter : MonoBehaviour
    {
        [SerializeField] private WeaponCatalogAsset weaponCatalog;
        [SerializeField] private Image heroImage;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TMP_Text weaponText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button weaponSelectorButton;
        [SerializeField] private GameObject weaponSelectionOverlay;
        [SerializeField] private Button closeWeaponSelectionButton;
        [SerializeField] private Button patrolButton;
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private TMP_Text stageStatusText;
        [SerializeField] private Button previousStageButton;
        [SerializeField] private Button nextStageButton;
        [SerializeField] private Button normalDifficultyButton;
        [SerializeField] private Button omenDifficultyButton;
        [SerializeField] private Button greatOmenDifficultyButton;

        private MetaGameSession session;
        private Action refreshHeader;
        private WeaponId selectedWeapon = WeaponId.HwandoFlyingBlade;
        private int viewedStageIndex;
        private StageDifficulty viewedDifficulty = StageDifficulty.Normal;
        private string stageFeedback = string.Empty;

        public void Build()
        {
            if (transform.Find("Stage Name") != null)
            {
                BindExistingView();
                EnsureStageControls();
                return;
            }

            var title = LobbyUiFactory.Text("Stage Name", transform, "1장 · 귀곡 들판", 34f,
                TextAlignmentOptions.Center, true);
            stageNameText = title;
            title.color = LobbyUiFactory.Gold;
            Anchor(title.rectTransform, new Vector2(.18f, .875f), new Vector2(.82f, .95f));

            previousStageButton = LobbyUiFactory.Button("Previous Stage", transform, "◀", 28f,
                LobbyUiFactory.NightInk, LobbyUiFactory.Gold);
            Anchor(previousStageButton.GetComponent<RectTransform>(), new Vector2(.04f, .875f), new Vector2(.16f, .95f));
            nextStageButton = LobbyUiFactory.Button("Next Stage", transform, "▶", 28f,
                LobbyUiFactory.NightInk, LobbyUiFactory.Gold);
            Anchor(nextStageButton.GetComponent<RectTransform>(), new Vector2(.84f, .875f), new Vector2(.96f, .95f));

            stageStatusText = LobbyUiFactory.Text("Stage Status", transform, string.Empty, 17f,
                TextAlignmentOptions.Center, true);
            stageStatusText.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(stageStatusText.rectTransform, new Vector2(.08f, .81f), new Vector2(.92f, .87f),
                Vector2.zero, Vector2.zero);

            var shadowRect = new GameObject("Patrol Hero Shadow", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(PixelOvalGraphic)).GetComponent<RectTransform>();
            shadowRect.SetParent(transform, false);
            shadowRect.GetComponent<PixelOvalGraphic>().color = new Color(0f, 0f, 0f, .17f);
            shadowRect.GetComponent<PixelOvalGraphic>().raycastTarget = false;
            Anchor(shadowRect, new Vector2(.40f, .57f), new Vector2(.60f, .60f));

            heroImage = LobbyUiFactory.Image("Patrol Hero", transform, Color.white);
            heroImage.preserveAspect = true;
            Anchor(heroImage.rectTransform, new Vector2(.33f, .58f), new Vector2(.67f, .81f));

            normalDifficultyButton = CreateDifficultyButton(
                "Difficulty Normal", "보통", new Vector2(.07f, .445f), new Vector2(.35f, .56f));
            omenDifficultyButton = CreateDifficultyButton(
                "Difficulty Omen", "흉조", new Vector2(.36f, .445f), new Vector2(.64f, .56f));
            greatOmenDifficultyButton = CreateDifficultyButton(
                "Difficulty Great Omen", "대흉", new Vector2(.65f, .445f), new Vector2(.93f, .56f));

            Anchor(normalDifficultyButton.GetComponent<RectTransform>(), new Vector2(.055f, .43f), new Vector2(.35f, .535f));
            Anchor(omenDifficultyButton.GetComponent<RectTransform>(), new Vector2(.352f, .43f), new Vector2(.648f, .535f));
            Anchor(greatOmenDifficultyButton.GetComponent<RectTransform>(), new Vector2(.65f, .43f), new Vector2(.945f, .535f));

            weaponSelectorButton = LobbyUiFactory.Button("Starting Weapon Selector", transform, string.Empty, 23f);
            Anchor(weaponSelectorButton.GetComponent<RectTransform>(), new Vector2(.12f, .285f), new Vector2(.88f, .405f));
            var selectorLabel = weaponSelectorButton.GetComponentInChildren<TMP_Text>(true);
            selectorLabel.name = "Starting Weapon Name";
            selectorLabel.alignment = TextAlignmentOptions.MidlineLeft;
            LobbyUiFactory.Anchor(selectorLabel.rectTransform, new Vector2(.35f, .12f), new Vector2(.86f, .88f),
                Vector2.zero, Vector2.zero);
            var caption = LobbyUiFactory.Text("Starting Weapon Caption", weaponSelectorButton.transform,
                "시작 무기", 15f, TextAlignmentOptions.MidlineLeft);
            caption.color = LobbyUiFactory.Hanji;
            LobbyUiFactory.Anchor(caption.rectTransform, new Vector2(.18f, .58f), new Vector2(.35f, .91f),
                Vector2.zero, Vector2.zero);
            var chevron = LobbyUiFactory.Text("Starting Weapon Chevron", weaponSelectorButton.transform,
                "〉", 28f, TextAlignmentOptions.Center, true);
            chevron.color = LobbyUiFactory.Gold;
            LobbyUiFactory.Anchor(chevron.rectTransform, new Vector2(.86f, .1f), new Vector2(.98f, .9f),
                Vector2.zero, Vector2.zero);
            weaponIcon = LobbyUiFactory.Image("Starting Weapon Icon", weaponSelectorButton.transform, Color.white);
            weaponIcon.preserveAspect = true;
            LobbyUiFactory.Anchor(weaponIcon.rectTransform, new Vector2(.03f, .12f), new Vector2(.17f, .88f),
                Vector2.zero, Vector2.zero);
            weaponText = selectorLabel;

            patrolButton = LobbyUiFactory.Button("Start Patrol", transform, "출전", 31f,
                LobbyUiFactory.Gold, LobbyUiFactory.Ink);
            Anchor(patrolButton.GetComponent<RectTransform>(), new Vector2(.20f, .09f), new Vector2(.80f, .235f));
            feedbackText = LobbyUiFactory.Text("Patrol Feedback", transform, string.Empty, 18f);
            feedbackText.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(feedbackText.rectTransform, new Vector2(.04f, .03f), new Vector2(.96f, .09f),
                Vector2.zero, Vector2.zero);

            EnsurePremiumPresentation();
            BuildWeaponSelectionOverlay();
        }

        private Button CreateDifficultyButton(string name, string label, Vector2 minimum, Vector2 maximum)
        {
            var button = LobbyUiFactory.Button(name, transform, label, 21f,
                LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            LobbyUiFactory.Anchor(button.GetComponent<RectTransform>(), minimum, maximum,
                Vector2.zero, Vector2.zero);
            return button;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max) =>
            LobbyUiFactory.Anchor(rect, min, max, Vector2.zero, Vector2.zero);

        private void BindExistingView()
        {
            stageNameText = transform.Find("Stage Name")?.GetComponent<TMP_Text>();
            heroImage = transform.Find("Patrol Hero")?.GetComponent<Image>();
            weaponSelectorButton = transform.Find("Starting Weapon Selector")?.GetComponent<Button>();
            weaponIcon = transform.Find("Starting Weapon Selector/Starting Weapon Icon")?.GetComponent<Image>();
            weaponText = transform.Find("Starting Weapon Selector/Starting Weapon Name")?.GetComponent<TMP_Text>();
            feedbackText = transform.Find("Patrol Feedback")?.GetComponent<TMP_Text>();
            patrolButton = transform.Find("Start Patrol")?.GetComponent<Button>();
            weaponSelectionOverlay = transform.Find("Weapon Selection Overlay")?.gameObject;
            closeWeaponSelectionButton = transform.Find(
                "Weapon Selection Overlay/Weapon Selection Panel/Close Weapon Selection")?.GetComponent<Button>();
        }

        private void EnsureStageControls()
        {
            if (stageNameText != null)
                Anchor(stageNameText.rectTransform, new Vector2(.18f, .875f), new Vector2(.82f, .95f));

            previousStageButton = transform.Find("Previous Stage")?.GetComponent<Button>() ??
                LobbyUiFactory.Button("Previous Stage", transform, "◀", 28f,
                    LobbyUiFactory.NightInk, LobbyUiFactory.Gold);
            Anchor(previousStageButton.GetComponent<RectTransform>(), new Vector2(.04f, .875f), new Vector2(.16f, .95f));
            nextStageButton = transform.Find("Next Stage")?.GetComponent<Button>() ??
                LobbyUiFactory.Button("Next Stage", transform, "▶", 28f,
                    LobbyUiFactory.NightInk, LobbyUiFactory.Gold);
            Anchor(nextStageButton.GetComponent<RectTransform>(), new Vector2(.84f, .875f), new Vector2(.96f, .95f));

            stageStatusText = transform.Find("Stage Status")?.GetComponent<TMP_Text>();
            if (stageStatusText == null)
            {
                stageStatusText = LobbyUiFactory.Text("Stage Status", transform, string.Empty, 17f,
                    TextAlignmentOptions.Center, true);
                stageStatusText.color = LobbyUiFactory.HanjiLight;
            }
            LobbyUiFactory.Anchor(stageStatusText.rectTransform, new Vector2(.08f, .81f), new Vector2(.92f, .87f),
                Vector2.zero, Vector2.zero);

            normalDifficultyButton = transform.Find("Difficulty Normal")?.GetComponent<Button>() ??
                CreateDifficultyButton("Difficulty Normal", "보통", new Vector2(.07f, .445f), new Vector2(.35f, .56f));
            omenDifficultyButton = transform.Find("Difficulty Omen")?.GetComponent<Button>() ??
                CreateDifficultyButton("Difficulty Omen", "흉조", new Vector2(.36f, .445f), new Vector2(.64f, .56f));
            greatOmenDifficultyButton = transform.Find("Difficulty Great Omen")?.GetComponent<Button>() ??
                CreateDifficultyButton("Difficulty Great Omen", "대흉", new Vector2(.65f, .445f), new Vector2(.93f, .56f));

            Anchor(normalDifficultyButton.GetComponent<RectTransform>(), new Vector2(.055f, .43f), new Vector2(.35f, .535f));
            Anchor(omenDifficultyButton.GetComponent<RectTransform>(), new Vector2(.352f, .43f), new Vector2(.648f, .535f));
            Anchor(greatOmenDifficultyButton.GetComponent<RectTransform>(), new Vector2(.65f, .43f), new Vector2(.945f, .535f));

            var shadow = transform.Find("Patrol Hero Shadow") as RectTransform;
            if (shadow != null)
                Anchor(shadow, new Vector2(.40f, .57f), new Vector2(.60f, .60f));
            if (heroImage != null)
                Anchor(heroImage.rectTransform, new Vector2(.33f, .58f), new Vector2(.67f, .81f));
            if (weaponSelectorButton != null)
                Anchor(weaponSelectorButton.GetComponent<RectTransform>(), new Vector2(.12f, .285f), new Vector2(.88f, .405f));
            if (patrolButton != null)
                Anchor(patrolButton.GetComponent<RectTransform>(), new Vector2(.20f, .09f), new Vector2(.80f, .235f));

            EnsurePremiumPresentation();
        }

        private void EnsurePremiumPresentation()
        {
            EnsureStagePlaque();
            EnsureStageArrow(previousStageButton, PremiumIcon.Previous);
            EnsureStageArrow(nextStageButton, PremiumIcon.Next);
            EnsureHeroFrame();

            if (weaponSelectorButton != null)
            {
                var selectorImage = weaponSelectorButton.targetGraphic as Image ??
                                    weaponSelectorButton.GetComponent<Image>();
                PremiumPixelUiSkin.ApplyFrame(selectorImage, PremiumFrame.WeaponSelector);
                weaponSelectorButton.targetGraphic = selectorImage;
            }
        }

        private void EnsureStagePlaque()
        {
            var plaque = transform.Find("Stage Plaque")?.GetComponent<Image>() ??
                         LobbyUiFactory.Image("Stage Plaque", transform, Color.white);
            Anchor(plaque.rectTransform, new Vector2(.18f, .875f), new Vector2(.82f, .95f));
            plaque.raycastTarget = false;
            PremiumPixelUiSkin.ApplyFrame(plaque, PremiumFrame.StagePlaque);
            if (stageNameText != null) stageNameText.color = new Color(.26f, .08f, .035f, 1f);
            plaque.transform.SetAsFirstSibling();
        }

        private void EnsureStageArrow(Button button, PremiumIcon icon)
        {
            if (button == null) return;
            var background = button.targetGraphic as Image ?? button.GetComponent<Image>();
            PremiumPixelUiSkin.ApplyFrame(background, PremiumFrame.CardIdle);
            background.color = new Color(.72f, .64f, .52f, 1f);
            button.targetGraphic = background;

            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                label.gameObject.SetActive(false);

            var iconImage = button.transform.Find("Premium Icon")?.GetComponent<Image>() ??
                            LobbyUiFactory.Image("Premium Icon", button.transform, Color.white);
            var rect = iconImage.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(46f, 46f);
            PremiumPixelUiSkin.ApplyIcon(iconImage, icon);
            iconImage.transform.SetAsLastSibling();
        }

        private void EnsureHeroFrame()
        {
            if (heroImage == null) return;
            var frame = transform.Find("Patrol Hero Frame")?.GetComponent<Image>() ??
                        LobbyUiFactory.Image("Patrol Hero Frame", transform, Color.white);
            Anchor(frame.rectTransform, new Vector2(.30f, .55f), new Vector2(.70f, .84f));
            frame.raycastTarget = false;
            PremiumPixelUiSkin.ApplyFrame(frame, PremiumFrame.HeroOval);
            frame.transform.SetSiblingIndex(heroImage.transform.GetSiblingIndex());
        }

        private void BuildWeaponSelectionOverlay()
        {
            var overlay = LobbyUiFactory.Image("Weapon Selection Overlay", transform,
                new Color(.02f, .025f, .04f, .96f), true);
            LobbyUiFactory.Stretch(overlay.rectTransform, 8f, 8f, 8f, 8f);
            weaponSelectionOverlay = overlay.gameObject;

            var panel = LobbyUiFactory.Image("Weapon Selection Panel", overlay.transform,
                new Color(.08f, .065f, .08f, 1f));
            LobbyUiFactory.Anchor(panel.rectTransform, new Vector2(.06f, .12f), new Vector2(.94f, .88f),
                Vector2.zero, Vector2.zero);
            var heading = LobbyUiFactory.Text("Weapon Selection Title", panel.transform, "시작 무기 선택", 28f,
                TextAlignmentOptions.Center, true);
            heading.color = LobbyUiFactory.Gold;
            LobbyUiFactory.Anchor(heading.rectTransform, new Vector2(.08f, .84f), new Vector2(.92f, .97f),
                Vector2.zero, Vector2.zero);

            closeWeaponSelectionButton = LobbyUiFactory.Button("Close Weapon Selection", panel.transform, "닫기", 20f);
            LobbyUiFactory.Anchor(closeWeaponSelectionButton.GetComponent<RectTransform>(), new Vector2(.34f, .04f),
                new Vector2(.66f, .14f), Vector2.zero, Vector2.zero);

            var gridRect = LobbyUiFactory.Rect("Weapon Grid", panel.transform);
            LobbyUiFactory.Anchor(gridRect, new Vector2(.07f, .17f), new Vector2(.93f, .82f),
                Vector2.zero, Vector2.zero);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(246f, 110f);
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            foreach (var weaponId in WeaponRoster.All)
            {
                var option = LobbyUiFactory.Button($"Weapon Option {weaponId.Value}", gridRect,
                    LobbyViewModels.WeaponName(weaponId), 18f);
                var label = option.GetComponentInChildren<TMP_Text>(true);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                LobbyUiFactory.Anchor(label.rectTransform, new Vector2(.32f, .08f), new Vector2(.96f, .92f),
                    Vector2.zero, Vector2.zero);
                var icon = LobbyUiFactory.Image("Weapon Option Icon", option.transform, Color.white);
                icon.preserveAspect = true;
                LobbyUiFactory.Anchor(icon.rectTransform, new Vector2(.04f, .12f), new Vector2(.28f, .88f),
                    Vector2.zero, Vector2.zero);
            }

            weaponSelectionOverlay.SetActive(false);
        }

        public void Initialize(MetaGameSession value, Action onChanged)
        {
            Build();
            JoseonButtonSkin.Apply(patrolButton, JoseonButtonStyle.Primary);
            JoseonButtonSkin.Apply(closeWeaponSelectionButton, JoseonButtonStyle.Secondary);
            session = value;
            refreshHeader = onChanged;
            GameAudioButtonFeedback.Attach(patrolButton, GameAudioCueId.UiConfirm);
            LoadCurrentWeapon();
            LoadCurrentStage();

            weaponSelectorButton.onClick.RemoveAllListeners();
            closeWeaponSelectionButton.onClick.RemoveAllListeners();
            patrolButton.onClick.RemoveAllListeners();
            previousStageButton.onClick.RemoveAllListeners();
            nextStageButton.onClick.RemoveAllListeners();
            normalDifficultyButton.onClick.RemoveAllListeners();
            omenDifficultyButton.onClick.RemoveAllListeners();
            greatOmenDifficultyButton.onClick.RemoveAllListeners();
            weaponSelectorButton.onClick.AddListener(OpenWeaponSelection);
            closeWeaponSelectionButton.onClick.AddListener(CloseWeaponSelection);
            patrolButton.onClick.AddListener(StartPatrol);
            previousStageButton.onClick.AddListener(() => BrowseStage(-1));
            nextStageButton.onClick.AddListener(() => BrowseStage(1));
            normalDifficultyButton.onClick.AddListener(() => SelectDifficulty(StageDifficulty.Normal));
            omenDifficultyButton.onClick.AddListener(() => SelectDifficulty(StageDifficulty.Omen));
            greatOmenDifficultyButton.onClick.AddListener(() => SelectDifficulty(StageDifficulty.GreatOmen));
            BindWeaponOptions();
            Refresh();
        }

        public void ConfigureCatalog(WeaponCatalogAsset value) => weaponCatalog = value;

        public void SelectStartingWeaponForTests(WeaponId weaponId)
        {
            selectedWeapon = weaponId;
            SaveCurrentWeapon();
            Refresh();
        }

        private void BindWeaponOptions()
        {
            foreach (var weaponId in WeaponRoster.All)
            {
                var id = weaponId;
                var optionTransform = weaponSelectionOverlay.transform.Find(
                    $"Weapon Selection Panel/Weapon Grid/Weapon Option {id.Value}");
                if (optionTransform == null) continue;
                var option = optionTransform.GetComponent<Button>();
                option.onClick.RemoveAllListeners();
                option.onClick.AddListener(() => SelectWeapon(id));
                var icon = optionTransform.Find("Weapon Option Icon")?.GetComponent<Image>();
                if (icon == null) continue;
                icon.sprite = ResolveWeaponSprite(id);
                icon.enabled = icon.sprite != null;
            }
        }

        private void LoadCurrentWeapon()
        {
            var id = session.ActiveLoadout.StartingWeapon.Value;
            selectedWeapon = WeaponRoster.All.FirstOrDefault(weapon => weapon.Value == id);
            if (string.IsNullOrEmpty(selectedWeapon.Value)) selectedWeapon = WeaponId.HwandoFlyingBlade;
        }

        private void LoadCurrentStage()
        {
            var selection = session.ActiveStageSelection;
            viewedStageIndex = Mathf.Max(0, StageCatalog.IndexOf(selection.StageId));
            viewedDifficulty = selection.Difficulty;
            stageFeedback = string.Empty;
        }

        private void BrowseStage(int direction)
        {
            viewedStageIndex = Mathf.Clamp(viewedStageIndex + direction, 0, StageCatalog.All.Count - 1);
            viewedDifficulty = StageDifficulty.Normal;
            stageFeedback = string.Empty;
            Refresh();
        }

        private void SelectDifficulty(StageDifficulty difficulty)
        {
            var selection = new StageSelection(StageCatalog.All[viewedStageIndex].Id, difficulty);
            var records = StageClearRecordData.DomainRecords(session.Data.StageClearRecords);
            if (!StageUnlockRules.IsUnlocked(selection, records))
            {
                stageFeedback = StageUnlockRules.LockReason(selection, records);
                Refresh();
                return;
            }

            var result = session.SaveStageSelection(selection);
            if (!result.Success)
            {
                stageFeedback = "출전 정보를 저장하지 못했습니다";
                Refresh();
                return;
            }

            viewedDifficulty = difficulty;
            stageFeedback = string.Empty;
            refreshHeader?.Invoke();
            Refresh();
        }

        private void OpenWeaponSelection()
        {
            weaponSelectionOverlay.transform.SetAsLastSibling();
            weaponSelectionOverlay.SetActive(true);
        }

        private void CloseWeaponSelection() => weaponSelectionOverlay.SetActive(false);

        private void SelectWeapon(WeaponId weaponId)
        {
            selectedWeapon = weaponId;
            if (!SaveCurrentWeapon()) return;
            Refresh();
            CloseWeaponSelection();
        }

        private bool SaveCurrentWeapon()
        {
            var current = session.ActiveLoadout;
            var loadout = new PatrolLoadout(current.Name, selectedWeapon, current.Styles, current.DifficultyId);
            var result = session.SaveLoadout(session.Data.ActivePatrolLoadoutIndex, loadout);
            feedbackText.text = result.Success ? string.Empty : "무기를 저장하지 못했습니다. 다시 시도해 주세요.";
            refreshHeader?.Invoke();
            return result.Success;
        }

        private void StartPatrol()
        {
            if (session.Router.IsRouting) return;
            var definition = StageCatalog.All[viewedStageIndex];
            var selection = new StageSelection(definition.Id, viewedDifficulty);
            var records = StageClearRecordData.DomainRecords(session.Data.StageClearRecords);
            if (!StageUnlockRules.IsUnlocked(selection, records))
            {
                stageFeedback = StageUnlockRules.LockReason(selection, records);
                Refresh();
                return;
            }
            if (!definition.HasPlayableContent)
            {
                stageFeedback = "아직 준비 중인 지역입니다";
                Refresh();
                return;
            }
            if (!session.SaveStageSelection(selection).Success || !SaveCurrentWeapon()) return;
            patrolButton.interactable = false;
            session.SetPendingDestination("Gameplay");
            StartCoroutine(LoadBootstrap());
        }

        private IEnumerator LoadBootstrap()
        {
            yield return session.Router.LoadBootstrap();
            if (patrolButton != null) patrolButton.interactable = true;
        }

        private Sprite ResolveWeaponSprite(WeaponId id)
        {
            if (weaponCatalog == null || !weaponCatalog.TryGet(id, out var definition)) return null;
            return definition.UiIcon != null ? definition.UiIcon : definition.PresentationSprites.FirstOrDefault();
        }

        private void Refresh()
        {
            if (session == null) return;
            weaponText.text = LobbyViewModels.WeaponName(selectedWeapon);
            weaponIcon.sprite = ResolveWeaponSprite(selectedWeapon);
            weaponIcon.enabled = weaponIcon.sprite != null;

            var definition = StageCatalog.All[viewedStageIndex];
            var selection = new StageSelection(definition.Id, viewedDifficulty);
            var records = StageClearRecordData.DomainRecords(session.Data.StageClearRecords);
            var unlocked = StageUnlockRules.IsUnlocked(selection, records);
            stageNameText.text = $"{viewedStageIndex + 1}장 · {definition.DisplayName}";
            stageStatusText.text = string.Empty;
            stageStatusText.gameObject.SetActive(false);
            previousStageButton.interactable = viewedStageIndex > 0;
            nextStageButton.interactable = viewedStageIndex < StageCatalog.All.Count - 1;
            RefreshDifficultyButton(normalDifficultyButton, StageDifficulty.Normal, records);
            RefreshDifficultyButton(omenDifficultyButton, StageDifficulty.Omen, records);
            RefreshDifficultyButton(greatOmenDifficultyButton, StageDifficulty.GreatOmen, records);
            patrolButton.interactable = unlocked && definition.HasPlayableContent && !session.Router.IsRouting;

            if (!string.IsNullOrEmpty(stageFeedback))
                feedbackText.text = stageFeedback;
            else if (!unlocked)
                feedbackText.text = StageUnlockRules.LockReason(selection, records);
            else if (!definition.HasPlayableContent)
                feedbackText.text = "아직 준비 중인 지역입니다";
            else
                feedbackText.text = string.Empty;
        }

        private void RefreshDifficultyButton(
            Button button,
            StageDifficulty difficulty,
            System.Collections.Generic.IReadOnlyCollection<StageClearRecord> records)
        {
            var selection = new StageSelection(StageCatalog.All[viewedStageIndex].Id, difficulty);
            var unlocked = StageUnlockRules.IsUnlocked(selection, records);
            var selected = viewedDifficulty == difficulty;
            button.interactable = true;
            var background = button.targetGraphic as Image;
            if (background != null)
                background.color = Color.white;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = StageDifficultyNames.DisplayName(difficulty);
                label.color = LobbyUiFactory.HanjiLight;
            }
            LobbySelectionChrome.Apply(button, selected, !unlocked);
        }

        private void OnDisable()
        {
            if (weaponSelectionOverlay != null) weaponSelectionOverlay.SetActive(false);
        }
    }
}
