using JoseonHunter.Runtime.Meta;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.Audio;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyBootstrap : MonoBehaviour
    {
        private TMP_Text coinText;
        private TMP_Text accountLevelText;
        private TMP_Text accountExperienceText;
        private Image accountExperienceFill;
        private GameObject settingsOverlay;
        private AudioSettingsPresenter audioSettings;
        private RectTransform safeArea;
        private Rect lastSafeArea;

        private void Awake()
        {
            GameMusicDirector.EnsureExists();
            GameMusicDirector.Instance?.Request(GameMusicRole.Lobby, .8f);
            BuildShell();
            GameAudioButtonFeedback.AttachAll(transform);
            Bind(MetaGameSession.EnsureExists());
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea) ApplySafeArea();
        }

        public void BuildShell()
        {
            if (transform.Find("Safe Area") != null)
            {
                safeArea = transform.Find("Safe Area") as RectTransform;
                ApplyPremiumShell();
                EnsureAccountHeader();
                EnsureCurrencyHeader();
                EnsureSettingsShell();
                return;
            }
            var canvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            var background = LobbyUiFactory.Image("Lobby Background", transform,
                new Color(.12f, .13f, .16f, 1f));
            LobbyUiFactory.Stretch(background.rectTransform);
            background.sprite = Resources.Load<Sprite>("Lobby/lobby_courtyard");
            background.preserveAspect = false;

            safeArea = LobbyUiFactory.Rect("Safe Area", transform);
            LobbyUiFactory.Stretch(safeArea);

            var header = LobbyUiFactory.Image("Header", safeArea, LobbyUiFactory.NightInk);
            PremiumPixelUiSkin.ApplyFrame(header, PremiumFrame.HeaderBar);
            LobbyUiFactory.Anchor(header.rectTransform, new Vector2(.025f, .91f), new Vector2(.975f, .985f),
                Vector2.zero, Vector2.zero);
            LobbyUiFactory.AddGoldRule(header.transform, new Vector2(0f, 0f), new Vector2(1f, .025f));
            var title = LobbyUiFactory.Text("Lobby Title", header.transform, "조선 요괴 사냥꾼", 31f,
                TextAlignmentOptions.Left, true);
            title.color = LobbyUiFactory.HanjiLight;
            LobbyUiFactory.Anchor(title.rectTransform, new Vector2(.04f, .1f), new Vector2(.65f, .9f),
                Vector2.zero, Vector2.zero);
            coinText = LobbyUiFactory.Text("Coin Text", header.transform, "0", 25f,
                TextAlignmentOptions.Right);
            coinText.color = LobbyUiFactory.Gold;
            var coinIcon = LobbyUiFactory.Image("Coin Icon", header.transform, Color.white);
            coinIcon.preserveAspect = true;
            LobbyUiFactory.Anchor(coinIcon.rectTransform, new Vector2(.69f, .23f), new Vector2(.76f, .77f),
                Vector2.zero, Vector2.zero);
            LobbyUiFactory.Anchor(coinText.rectTransform, new Vector2(.76f, .1f), new Vector2(.89f, .9f),
                Vector2.zero, Vector2.zero);
            EnsureAccountHeader();
            EnsureCurrencyHeader();

            var stageContent = LobbyUiFactory.Rect("Stage Content", safeArea);
            LobbyUiFactory.Anchor(stageContent, new Vector2(.04f, .105f), new Vector2(.96f, .895f),
                Vector2.zero, Vector2.zero);
            var research = Panel("Weapon Research Panel", stageContent);
            var patrol = Panel("Patrol Panel", stageContent);
            var training = Panel("Common Training Panel", stageContent);
            research.gameObject.AddComponent<WeaponResearchPresenter>().Build();
            patrol.gameObject.AddComponent<PatrolPresenter>().Build();
            training.gameObject.AddComponent<CommonTrainingPresenter>().Build();

            var navigation = LobbyUiFactory.Image("Bottom Navigation", safeArea, LobbyUiFactory.NightInk);
            navigation.color = Color.clear;
            LobbyUiFactory.Anchor(navigation.rectTransform, new Vector2(.04f, .02f), new Vector2(.96f, .095f),
                Vector2.zero, Vector2.zero);
            var layout = navigation.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            var researchButton = LobbyUiFactory.Button("Weapon Research Navigation", navigation.transform,
                string.Empty, 21f, LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            var patrolButton = LobbyUiFactory.Button("Patrol Navigation", navigation.transform,
                string.Empty, 24f, LobbyUiFactory.Crimson, LobbyUiFactory.Gold);
            var trainingButton = LobbyUiFactory.Button("Common Training Navigation", navigation.transform,
                string.Empty, 21f, LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            navigation.gameObject.AddComponent<LobbyNavigationPresenter>().Initialize(
                research.gameObject, patrol.gameObject, training.gameObject,
                researchButton, patrolButton, trainingButton);

            EnsureSettingsShell();
            EnsureEventSystem();
        }

        private static RectTransform Panel(string name, Transform parent)
        {
            var panel = LobbyUiFactory.Image(name, parent, LobbyUiFactory.NightInk);
            LobbyUiFactory.Stretch(panel.rectTransform);
            return panel.rectTransform;
        }

        private void ApplyPremiumShell()
        {
            var header = transform.Find("Safe Area/Header")?.GetComponent<Image>();
            PremiumPixelUiSkin.ApplyFrame(header, PremiumFrame.HeaderBar);
            if (header != null)
                LobbyUiFactory.Anchor(header.rectTransform, new Vector2(.025f, .91f), new Vector2(.975f, .985f),
                    Vector2.zero, Vector2.zero);
            var stageContent = transform.Find("Safe Area/Stage Content") as RectTransform;
            if (stageContent != null)
                LobbyUiFactory.Anchor(stageContent, new Vector2(.04f, .105f), new Vector2(.96f, .895f),
                    Vector2.zero, Vector2.zero);
            foreach (var panelName in new[]
                     {
                         "Weapon Research Panel", "Patrol Panel", "Common Training Panel"
                     })
            {
                var panel = transform.Find("Safe Area/Stage Content/" + panelName)?.GetComponent<Image>();
                if (panel != null) panel.sprite = null;
            }

            var navigation = transform.Find("Safe Area/Bottom Navigation")?.GetComponent<Image>();
            if (navigation != null)
            {
                navigation.sprite = null;
                navigation.color = Color.clear;
                LobbyUiFactory.Anchor(navigation.rectTransform, new Vector2(.04f, .02f), new Vector2(.96f, .095f),
                    Vector2.zero, Vector2.zero);
                var layout = navigation.GetComponent<HorizontalLayoutGroup>();
                if (layout != null) layout.spacing = 6f;
            }
        }

        private void Bind(MetaGameSession session)
        {
            safeArea = transform.Find("Safe Area") as RectTransform;
            EnsureAccountHeader();
            EnsureCurrencyHeader();
            coinText = transform.Find("Safe Area/Header/Currency Capsule/Coin Text")?.GetComponent<TMP_Text>();
            accountLevelText = transform.Find("Safe Area/Header/Account Profile/Account Badge/Account Level")?.GetComponent<TMP_Text>();
            accountExperienceText = transform.Find("Safe Area/Header/Account Profile/Account Experience/Account Experience Text")?.GetComponent<TMP_Text>();
            accountExperienceFill = transform.Find("Safe Area/Header/Account Profile/Account Experience/Account Experience Fill")?.GetComponent<Image>();
            foreach (var research in GetComponentsInChildren<WeaponResearchPresenter>(true)) research.Initialize(session, RefreshHeader);
            foreach (var patrol in GetComponentsInChildren<PatrolPresenter>(true)) patrol.Initialize(session, RefreshHeader);
            foreach (var training in GetComponentsInChildren<CommonTrainingPresenter>(true)) training.Initialize(session, RefreshHeader);
            if (audioSettings != null) audioSettings.Initialize(session, true);
            else AudioSettingsPresenter.ApplySavedVolumes(session);
            RefreshHeader();
        }

        private void EnsureSettingsShell()
        {
            var header = transform.Find("Safe Area/Header");
            if (header == null || safeArea == null) return;
            var existingButton = header.Find("Settings Button")?.GetComponent<Button>();
            var settingsButton = existingButton ?? LobbyUiFactory.Button(
                "Settings Button", header, string.Empty, 18f, LobbyUiFactory.Brown, LobbyUiFactory.Gold);
            LobbyUiFactory.Anchor(settingsButton.GetComponent<RectTransform>(),
                new Vector2(.90f, .14f), new Vector2(.975f, .86f), Vector2.zero, Vector2.zero);
            PremiumPixelUiSkin.ApplyAction(settingsButton, PremiumActionStyle.Secondary);
            EnsureSettingsIcon(settingsButton.transform);
            settingsButton.onClick.RemoveListener(OpenSettings);
            settingsButton.onClick.AddListener(OpenSettings);

            var existingOverlay = safeArea.Find("Audio Settings Overlay");
            if (existingOverlay != null)
            {
                settingsOverlay = existingOverlay.gameObject;
                audioSettings = settingsOverlay.GetComponentInChildren<AudioSettingsPresenter>(true);
                if (audioSettings != null)
                {
                    audioSettings.CloseRequested -= CloseSettings;
                    audioSettings.CloseRequested += CloseSettings;
                }
                settingsOverlay.transform.SetAsLastSibling();
                return;
            }

            var overlay = LobbyUiFactory.Image("Audio Settings Overlay", safeArea,
                new Color(.01f, .012f, .02f, .88f), true);
            LobbyUiFactory.Stretch(overlay.rectTransform);
            settingsOverlay = overlay.gameObject;
            var panel = LobbyUiFactory.Image("Audio Settings Panel", overlay.transform, LobbyUiFactory.HanjiLight, true);
            LobbyUiFactory.Anchor(panel.rectTransform, new Vector2(.08f, .31f), new Vector2(.92f, .69f),
                Vector2.zero, Vector2.zero);
            var content = LobbyUiFactory.Rect("Audio Settings Content", panel.transform);
            LobbyUiFactory.Stretch(content, 20f, 20f, 20f, 20f);
            audioSettings = content.gameObject.AddComponent<AudioSettingsPresenter>();
            audioSettings.CloseRequested += CloseSettings;
            settingsOverlay.SetActive(false);
            settingsOverlay.transform.SetAsLastSibling();
        }

        private void OpenSettings()
        {
            if (settingsOverlay == null) return;
            settingsOverlay.transform.SetAsLastSibling();
            settingsOverlay.SetActive(true);
        }

        private void CloseSettings()
        {
            if (settingsOverlay != null) settingsOverlay.SetActive(false);
        }

        private static void EnsureSettingsIcon(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                if (!child.name.StartsWith("Gear Tooth") && child.name != "Gear Hub") continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            var icon = parent.Find("Settings Icon")?.GetComponent<Image>() ??
                       LobbyUiFactory.Image("Settings Icon", parent, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            PremiumPixelUiSkin.ApplyIcon(icon, PremiumIcon.Settings);
            LobbyUiFactory.Stretch(icon.rectTransform, 10f, 10f, 10f, 10f);
        }

        private void RefreshHeader()
        {
            var session = MetaGameSession.Current;
            if (session == null) return;
            if (coinText != null) coinText.text = $"{session.Data.Coins:N0}";

            var account = AccountProgression.StateFor(session.Data.AccountExperience);
            if (accountLevelText != null) accountLevelText.text = account.Level.ToString();
            if (accountExperienceText != null)
                accountExperienceText.text = account.IsMaximumLevel
                    ? "최대 레벨"
                    : $"{account.CurrentLevelExperience:N0} / {account.NextLevelRequirement:N0}";
            if (accountExperienceFill != null)
                accountExperienceFill.fillAmount = account.IsMaximumLevel
                    ? 1f
                    : account.NextLevelRequirement <= 0
                        ? 0f
                        : (float)account.CurrentLevelExperience / account.NextLevelRequirement;
        }

        private void EnsureAccountHeader()
        {
            var header = transform.Find("Safe Area/Header");
            if (header == null) return;
            var oldTitle = header.Find("Lobby Title");
            if (oldTitle != null) oldTitle.gameObject.SetActive(false);

            var profile = header.Find("Account Profile")?.GetComponent<RectTransform>();
            if (profile == null)
            {
                var profileImage = LobbyUiFactory.Image("Account Profile", header,
                    new Color(.025f, .031f, .048f, .96f));
                profile = profileImage.rectTransform;
            }
            LobbyUiFactory.Anchor(profile, new Vector2(.025f, .12f), new Vector2(.59f, .88f),
                Vector2.zero, Vector2.zero);

            var badge = profile.Find("Account Badge")?.GetComponent<Image>() ??
                        header.Find("Account Badge")?.GetComponent<Image>();
            if (badge == null) badge = LobbyUiFactory.Image("Account Badge", profile, LobbyUiFactory.Gold);
            else if (badge.transform.parent != profile) badge.transform.SetParent(profile, false);
            badge.color = LobbyUiFactory.Gold;
            LobbyUiFactory.Anchor(badge.rectTransform, new Vector2(.02f, .08f), new Vector2(.17f, .92f),
                Vector2.zero, Vector2.zero);

            var badgeInner = badge.transform.Find("Account Badge Inner")?.GetComponent<Image>() ??
                             LobbyUiFactory.Image("Account Badge Inner", badge.transform, LobbyUiFactory.NightInk);
            LobbyUiFactory.Stretch(badgeInner.rectTransform, 3f, 3f, 3f, 3f);
            badgeInner.transform.SetAsFirstSibling();

            accountLevelText = badge.transform.Find("Account Level")?.GetComponent<TMP_Text>() ??
                               LobbyUiFactory.Text("Account Level", badge.transform, "1", 25f,
                                   TextAlignmentOptions.Center, true);
            accountLevelText.color = LobbyUiFactory.Gold;
            LobbyUiFactory.Stretch(accountLevelText.rectTransform);
            accountLevelText.transform.SetAsLastSibling();

            var accountName = profile.Find("Account Name")?.GetComponent<TMP_Text>() ??
                              header.Find("Account Name")?.GetComponent<TMP_Text>();
            if (accountName == null)
                accountName = LobbyUiFactory.Text("Account Name", profile, "요괴 사냥꾼", 19f,
                    TextAlignmentOptions.Left, true);
            else if (accountName.transform.parent != profile) accountName.transform.SetParent(profile, false);
            accountName.color = LobbyUiFactory.HanjiLight;
            accountName.fontSize = 19f;
            accountName.textWrappingMode = TextWrappingModes.NoWrap;
            LobbyUiFactory.Anchor(accountName.rectTransform, new Vector2(.20f, .51f), new Vector2(.98f, .93f),
                Vector2.zero, Vector2.zero);

            var experience = profile.Find("Account Experience")?.GetComponent<Image>() ??
                             header.Find("Account Experience")?.GetComponent<Image>();
            if (experience == null)
                experience = LobbyUiFactory.Image("Account Experience", profile,
                    new Color(.018f, .024f, .035f, 1f));
            else if (experience.transform.parent != profile) experience.transform.SetParent(profile, false);
            experience.color = new Color(.018f, .024f, .035f, 1f);
            LobbyUiFactory.Anchor(experience.rectTransform, new Vector2(.20f, .13f), new Vector2(.98f, .43f),
                Vector2.zero, Vector2.zero);

            accountExperienceFill = experience.transform.Find("Account Experience Fill")?.GetComponent<Image>() ??
                                    LobbyUiFactory.Image("Account Experience Fill", experience.transform,
                                        new Color(.20f, .72f, .35f, 1f));
            accountExperienceFill.color = new Color(.20f, .72f, .35f, 1f);
            LobbyUiFactory.Stretch(accountExperienceFill.rectTransform);
            accountExperienceFill.type = Image.Type.Filled;
            accountExperienceFill.fillMethod = Image.FillMethod.Horizontal;
            accountExperienceFill.fillOrigin = 0;
            accountExperienceText = experience.transform.Find("Account Experience Text")?.GetComponent<TMP_Text>() ??
                                    LobbyUiFactory.Text("Account Experience Text", experience.transform,
                                        "0 / 100", 12f, TextAlignmentOptions.Center, true);
            accountExperienceText.color = LobbyUiFactory.HanjiLight;
            accountExperienceText.fontSize = 12f;
            LobbyUiFactory.Stretch(accountExperienceText.rectTransform);
            accountExperienceText.transform.SetAsLastSibling();
        }

        private void EnsureCurrencyHeader()
        {
            var header = transform.Find("Safe Area/Header");
            if (header == null) return;

            var capsule = header.Find("Currency Capsule")?.GetComponent<Image>();
            if (capsule == null)
                capsule = LobbyUiFactory.Image("Currency Capsule", header,
                    new Color(.055f, .047f, .055f, .98f));
            capsule.color = new Color(.055f, .047f, .055f, .98f);
            LobbyUiFactory.Anchor(capsule.rectTransform, new Vector2(.62f, .17f), new Vector2(.875f, .83f),
                Vector2.zero, Vector2.zero);

            var icon = capsule.transform.Find("Coin Icon")?.GetComponent<Image>() ??
                       header.Find("Coin Icon")?.GetComponent<Image>();
            if (icon == null) icon = LobbyUiFactory.Image("Coin Icon", capsule.transform, Color.white);
            else if (icon.transform.parent != capsule.transform) icon.transform.SetParent(capsule.transform, false);
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            LobbyUiFactory.Anchor(icon.rectTransform, new Vector2(.035f, .10f), new Vector2(.34f, .90f),
                Vector2.zero, Vector2.zero);

            coinText = capsule.transform.Find("Coin Text")?.GetComponent<TMP_Text>() ??
                       header.Find("Coin Text")?.GetComponent<TMP_Text>();
            if (coinText == null)
                coinText = LobbyUiFactory.Text("Coin Text", capsule.transform, "0", 23f,
                    TextAlignmentOptions.Center, true);
            else if (coinText.transform.parent != capsule.transform) coinText.transform.SetParent(capsule.transform, false);
            coinText.color = new Color(1f, .74f, .24f, 1f);
            coinText.fontSize = 23f;
            coinText.textWrappingMode = TextWrappingModes.NoWrap;
            LobbyUiFactory.Anchor(coinText.rectTransform, new Vector2(.32f, .08f), new Vector2(.96f, .92f),
                Vector2.zero, Vector2.zero);
        }

        private void ApplySafeArea()
        {
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafeArea = Screen.safeArea;
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null) eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

    }
}
