using JoseonHunter.Runtime.Gameplay;
using UnityEngine;
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
        private RectTransform safeAreaContainer;
        private Rect lastSafeArea;
        private Vector2 lastScreenSize;
        private float nextRenderTime;
        private int weaponSignature = int.MinValue;

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
            safeAreaContainer = RuntimeUiFactory.Rect("Safe Area", transform);
            ApplySafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height));

            var hudRoot = RuntimeUiFactory.Rect("Combat HUD", safeAreaContainer);
            RuntimeUiFactory.Stretch(hudRoot, 0f, 0f, 0f, 0f);
            combatHud = hudRoot.gameObject.AddComponent<CombatHudPresenter>();
            combatHud.Build();
            var rackRoot = RuntimeUiFactory.Rect("Weapon Rack", safeAreaContainer);
            RuntimeUiFactory.Stretch(rackRoot, 0f, 0f, 0f, 0f);
            weaponRack = rackRoot.gameObject.AddComponent<WeaponRackPresenter>();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Update()
        {
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2(Screen.width, Screen.height);
            if (safeArea != lastSafeArea || screenSize != lastScreenSize) ApplySafeArea(safeArea, screenSize);

            if (boundController == null) boundController = FindFirstObjectByType<FirstPlayableController>();
            if (boundController == null || Time.unscaledTime < nextRenderTime) return;

            nextRenderTime = Time.unscaledTime + RenderInterval;
            var state = boundController.UiState;
            combatHud.Render(state);
            var signature = WeaponSignature(state);
            if (signature == weaponSignature) return;
            weaponSignature = signature;
            weaponRack.Render(state.Weapons);
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
                    signature = signature * 31 + (weapon.Id == null ? 0 : weapon.Id.GetHashCode());
                    signature = signature * 31 + weapon.Level;
                    signature = signature * 31 + (weapon.Icon == null ? 0 : weapon.Icon.GetInstanceID());
                }

                return signature;
            }
        }

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
    }
}
