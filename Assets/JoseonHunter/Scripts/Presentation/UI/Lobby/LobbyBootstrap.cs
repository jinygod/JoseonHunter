using JoseonHunter.Runtime.Meta;
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
        private RectTransform safeArea;
        private Rect lastSafeArea;

        private void Awake()
        {
            BuildShell();
            Bind(MetaGameSession.EnsureExists());
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea) ApplySafeArea();
        }

        public void BuildShell()
        {
            if (transform.Find("Safe Area") != null) return;
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
            LobbyUiFactory.Anchor(header.rectTransform, new Vector2(.02f, .905f), new Vector2(.98f, .985f),
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
            LobbyUiFactory.Anchor(coinIcon.rectTransform, new Vector2(.72f, .23f), new Vector2(.80f, .77f),
                Vector2.zero, Vector2.zero);
            LobbyUiFactory.Anchor(coinText.rectTransform, new Vector2(.80f, .1f), new Vector2(.96f, .9f),
                Vector2.zero, Vector2.zero);

            var stageContent = LobbyUiFactory.Rect("Stage Content", safeArea);
            LobbyUiFactory.Anchor(stageContent, new Vector2(.02f, .12f), new Vector2(.98f, .895f),
                Vector2.zero, Vector2.zero);
            var research = Panel("Weapon Research Panel", stageContent);
            var patrol = Panel("Patrol Panel", stageContent);
            var training = Panel("Common Training Panel", stageContent);
            research.gameObject.AddComponent<WeaponResearchPresenter>().Build();
            patrol.gameObject.AddComponent<PatrolPresenter>().Build();
            training.gameObject.AddComponent<CommonTrainingPresenter>().Build();

            var navigation = LobbyUiFactory.Image("Bottom Navigation", safeArea, LobbyUiFactory.NightInk);
            LobbyUiFactory.Anchor(navigation.rectTransform, new Vector2(.02f, .015f), new Vector2(.98f, .105f),
                Vector2.zero, Vector2.zero);
            var layout = navigation.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            var researchButton = LobbyUiFactory.Button("Weapon Research Navigation", navigation.transform,
                "무기 연구", 21f, LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            var patrolButton = LobbyUiFactory.Button("Patrol Navigation", navigation.transform,
                "출전", 24f, LobbyUiFactory.Crimson, LobbyUiFactory.Gold);
            var trainingButton = LobbyUiFactory.Button("Common Training Navigation", navigation.transform,
                "수련", 21f, LobbyUiFactory.NightInk, LobbyUiFactory.HanjiLight);
            navigation.gameObject.AddComponent<LobbyNavigationPresenter>().Initialize(
                research.gameObject, patrol.gameObject, training.gameObject,
                researchButton, patrolButton, trainingButton);

            EnsureEventSystem();
        }

        private static RectTransform Panel(string name, Transform parent)
        {
            var panel = LobbyUiFactory.Image(name, parent, LobbyUiFactory.NightInk);
            LobbyUiFactory.Stretch(panel.rectTransform);
            return panel.rectTransform;
        }

        private void Bind(MetaGameSession session)
        {
            safeArea = transform.Find("Safe Area") as RectTransform;
            coinText = transform.Find("Safe Area/Header/Coin Text")?.GetComponent<TMP_Text>();
            foreach (var research in GetComponentsInChildren<WeaponResearchPresenter>(true)) research.Initialize(session, RefreshHeader);
            foreach (var patrol in GetComponentsInChildren<PatrolPresenter>(true)) patrol.Initialize(session, RefreshHeader);
            foreach (var training in GetComponentsInChildren<CommonTrainingPresenter>(true)) training.Initialize(session, RefreshHeader);
            RefreshHeader();
        }

        private void RefreshHeader()
        {
            if (coinText != null && MetaGameSession.Current != null)
                coinText.text = $"{MetaGameSession.Current.Data.Coins:N0}";
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
