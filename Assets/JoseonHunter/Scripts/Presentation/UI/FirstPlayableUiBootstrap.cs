using JoseonHunter.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace JoseonHunter.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class FirstPlayableUiBootstrap : MonoBehaviour
    {
        private const float RenderInterval = .1f;

        private static FirstPlayableUiBootstrap instance;
        private FirstPlayableController boundController;
        private CombatHudPresenter combatHud;
        private WeaponRackPresenter weaponRack;
        private RewardRevealPresenter rewardReveal;
        private WeaponAffixRevealPresenter affixReveal;
        private UpgradeChoicePresenter upgradeChoice;
        private RectTransform safeAreaContainer;
        private Rect lastSafeArea;
        private Vector2 lastScreenSize;
        private float nextRenderTime;
        private int weaponSignature = int.MinValue;
        private bool waitingForRewardReveal;
        private bool waitingForChoiceClose;
        private ProgressionRewardEvent pendingReward;
        private bool hasPendingReward;

        public FirstPlayableController BoundController => boundController;
        public RectTransform SafeAreaContainer => safeAreaContainer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindObjectsByType<FirstPlayableUiBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
                return;

            new GameObject("First Playable UI").AddComponent<FirstPlayableUiBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            BuildCanvas();
            EnsureEventSystem();
            safeAreaContainer = RuntimeUiFactory.Rect("Safe Area", transform);
            ApplySafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height));

            var hudRoot = RuntimeUiFactory.Rect("Combat HUD", safeAreaContainer);
            RuntimeUiFactory.Stretch(hudRoot, 0f, 0f, 0f, 0f);
            combatHud = hudRoot.gameObject.AddComponent<CombatHudPresenter>();
            combatHud.Build();
            var rackRoot = RuntimeUiFactory.Rect("Weapon Rack", safeAreaContainer);
            RuntimeUiFactory.Stretch(rackRoot, 0f, 0f, 0f, 0f);
            weaponRack = rackRoot.gameObject.AddComponent<WeaponRackPresenter>();
            var rewardRoot = RuntimeUiFactory.Rect("Reward Reveal", safeAreaContainer);
            RuntimeUiFactory.Stretch(rewardRoot, 0f, 0f, 0f, 0f);
            rewardReveal = rewardRoot.gameObject.AddComponent<RewardRevealPresenter>();
            rewardReveal.RevealCompleted += OnRewardRevealCompleted;
            affixReveal = rewardRoot.gameObject.AddComponent<WeaponAffixRevealPresenter>();
            affixReveal.RevealCompleted += OnRewardRevealCompleted;
            var upgradeRoot = RuntimeUiFactory.Rect("Upgrade Choice", safeAreaContainer);
            RuntimeUiFactory.Stretch(upgradeRoot, 0f, 0f, 0f, 0f);
            upgradeChoice = upgradeRoot.gameObject.AddComponent<UpgradeChoicePresenter>();
            upgradeChoice.Build();
            upgradeChoice.PresentationClosed += NotifyUpgradePresentationClosed;
        }

        private void OnDestroy()
        {
            UnbindController();
            if (upgradeChoice != null) upgradeChoice.PresentationClosed -= NotifyUpgradePresentationClosed;
            if (rewardReveal != null) rewardReveal.RevealCompleted -= OnRewardRevealCompleted;
            if (affixReveal != null) affixReveal.RevealCompleted -= OnRewardRevealCompleted;
            if (instance == this) instance = null;
        }

        private void OnDisable()
        {
            upgradeChoice?.CloseImmediately();
            CloseRewardReveal();
        }

        private void Update()
        {
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2(Screen.width, Screen.height);
            if (safeArea != lastSafeArea || screenSize != lastScreenSize) ApplySafeArea(safeArea, screenSize);

            if (boundController == null) BindController(FindFirstObjectByType<FirstPlayableController>());
            if (boundController == null || Time.unscaledTime < nextRenderTime) return;

            nextRenderTime = Time.unscaledTime + RenderInterval;
            var state = boundController.UiState;
            combatHud.Render(state);
            var signature = WeaponSignature(state);
            if (signature == weaponSignature) return;
            weaponSignature = signature;
            weaponRack.Render(state.Weapons);
        }

        private void BindController(FirstPlayableController controller)
        {
            if (boundController == controller) return;
            UnbindController();
            boundController = controller;
            if (boundController == null) return;
            boundController.UpgradeOpened += OpenUpgradeChoice;
            boundController.UpgradeChosen += OnUpgradeChosen;
            boundController.RunReset += CloseUpgradeChoice;
            boundController.RunReset += CloseRewardReveal;
        }

        private void UnbindController()
        {
            if (boundController == null) return;
            boundController.UpgradeOpened -= OpenUpgradeChoice;
            boundController.UpgradeChosen -= OnUpgradeChosen;
            boundController.RunReset -= CloseUpgradeChoice;
            boundController.RunReset -= CloseRewardReveal;
            boundController = null;
        }

        private void OpenUpgradeChoice(UpgradeChoiceState state)
        {
            upgradeChoice?.Open(state, boundController.TryChooseUpgrade);
        }

        private void CloseUpgradeChoice()
        {
            upgradeChoice?.CloseImmediately();
        }

        private void OnUpgradeChosen(ProgressionRewardEvent reward)
        {
            waitingForRewardReveal = true;
            waitingForChoiceClose = true;
            pendingReward = reward;
            hasPendingReward = true;
            if (reward.Kind != ProgressionRewardKind.Support)
            {
                var state = boundController.UiState;
                weaponSignature = WeaponSignature(state);
                weaponRack.Render(state.Weapons);
                weaponRack.Pulse(reward.WeaponId, reward.NewLevel, reward.AffixResult?.NewPotentials.Count ?? 0);
            }

        }

        private void CloseRewardReveal()
        {
            waitingForRewardReveal = false;
            waitingForChoiceClose = false;
            rewardReveal?.HideImmediately();
            affixReveal?.HideImmediately();
            hasPendingReward = false;
            weaponRack?.ResetPulses();
        }

        private void NotifyUpgradePresentationClosed()
        {
            waitingForChoiceClose = false;
            PlayPendingRewardAfterChoiceClose();
            NotifyUpgradeWhenPresentationComplete();
        }

        private void PlayPendingRewardAfterChoiceClose()
        {
            if (!hasPendingReward) return;
            var reward = pendingReward;
            hasPendingReward = false;
            // Weapon selections use only the affix reel. Support/evolution keep their existing reveal.
            if (reward.Kind != ProgressionRewardKind.Support && reward.Kind != ProgressionRewardKind.Evolution && reward.AffixResult != null)
                affixReveal.Play(reward.AffixResult);
            else
                rewardReveal.Play(reward);
        }

        private void OnRewardRevealCompleted()
        {
            waitingForRewardReveal = false;
            NotifyUpgradeWhenPresentationComplete();
        }

        private void NotifyUpgradeWhenPresentationComplete()
        {
            if (waitingForChoiceClose || waitingForRewardReveal) return;
            boundController?.NotifyUpgradePresentationClosed();
        }

        public void ApplySafeArea(Rect safeArea, Vector2 screenSize)
        {
            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            if (safeAreaContainer == null || screenSize.x <= 0f || screenSize.y <= 0f) return;

            safeAreaContainer.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            safeAreaContainer.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            safeAreaContainer.offsetMin = Vector2.zero;
            safeAreaContainer.offsetMax = Vector2.zero;
        }

        private static int WeaponSignature(FirstPlayableUiState state)
        {
            unchecked
            {
                var signature = state.Weapons.Count;
                for (var index = 0; index < state.Weapons.Count; index++)
                {
                    var weapon = state.Weapons[index];
                    signature = signature * 31 + StableContentHash(weapon.Id);
                    signature = signature * 31 + weapon.Level;
                    signature = signature * 31 + StableContentHash(weapon.GeneralAffixSummary);
                    signature = signature * 31 + weapon.PotentialIds.Count;
                    for (var potentialIndex = 0; potentialIndex < weapon.PotentialIds.Count; potentialIndex++)
                        signature = signature * 31 + StableContentHash(weapon.PotentialIds[potentialIndex].Value);
                    signature = signature * 31 + weapon.GeneralAffixTiers.Count;
                    for (var tierIndex = 0; tierIndex < weapon.GeneralAffixTiers.Count; tierIndex++)
                        signature = signature * 31 + (int)weapon.GeneralAffixTiers[tierIndex];
                }

                return signature;
            }
        }

        private static int StableContentHash(string value)
        {
            unchecked
            {
                var hash = 17;
                if (value == null) return hash;
                for (var index = 0; index < value.Length; index++) hash = hash * 31 + value[index];
                return hash;
            }
        }

#if UNITY_INCLUDE_TESTS
        public static int WeaponSignatureForTests(FirstPlayableUiState state) => WeaponSignature(state);
#endif

        private void BuildCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private static void EnsureEventSystem()
        {
            var systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EventSystem eventSystem = systems.Length > 0 ? systems[0] : null;
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }

            for (var index = 1; index < systems.Length; index++)
            {
                if (systems[index] != null && systems[index] != eventSystem)
                    Destroy(systems[index].gameObject);
            }

            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null) inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
                if (module != inputModule) Destroy(module);
        }
    }
}
