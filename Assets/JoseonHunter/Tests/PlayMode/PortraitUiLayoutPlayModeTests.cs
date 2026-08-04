using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class PortraitUiLayoutPlayModeTests
    {
        private Vector2Int originalResolution;
        private Rect originalSafeArea;

        [UnitySetUp]
        public IEnumerator CaptureScreenState()
        {
            originalResolution = new Vector2Int(Screen.width, Screen.height);
            originalSafeArea = Screen.safeArea;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreScreenAndModalState()
        {
            Screen.SetResolution(originalResolution.x, originalResolution.y, FullScreenMode.Windowed);
            for (var frame = 0; frame < 12 && (Screen.width != originalResolution.x || Screen.height != originalResolution.y); frame++)
                yield return null;
            Assert.That(Application.isBatchMode || new Vector2Int(Screen.width, Screen.height) == originalResolution, Is.True);
            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            if (bootstrap != null) bootstrap.ApplySafeArea(originalSafeArea, originalResolution);
            foreach (var reward in Object.FindObjectsByType<RewardRevealPresenter>(FindObjectsInactive.Include,
                FindObjectsSortMode.None)) reward.HideImmediately();
            foreach (var affix in Object.FindObjectsByType<WeaponAffixRevealPresenter>(FindObjectsInactive.Include,
                FindObjectsSortMode.None)) affix.HideImmediately();
            bootstrap?.BoundController?.CancelUiModalPresentation();
            Canvas.ForceUpdateCanvases();
            yield return null;
        }
        [UnityTest]
        public IEnumerator Portrait_canvas_safe_areas_and_runtime_font_cover_every_validation_resolution()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            yield return null;

            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            var scaler = bootstrap.GetComponent<CanvasScaler>();
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(.5f));
            var canvasRect = bootstrap.transform as RectTransform;
            var actualCanvas = PortraitUiMetrics.CanvasSizeFor(new Vector2(Screen.width, Screen.height));
            Assert.That(canvasRect.rect.size.x, Is.EqualTo(actualCanvas.x).Within(.1f));
            Assert.That(canvasRect.rect.size.y, Is.EqualTo(actualCanvas.y).Within(.1f));
            Assert.That(bootstrap.transform.Find("Modal Layer"), Is.Not.Null);
            Assert.That(bootstrap.ModalSafeAreaContainer, Is.Not.Null);
            var reward = Object.FindFirstObjectByType<RewardRevealPresenter>();
            reward.Play(new ProgressionRewardEvent("boots", null, 1, ProgressionRewardKind.Support,
                "능력 강화", "+12%", null));
            var appraisal = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            appraisal.ShowDetails(controller.UiState.Weapons[0]);
            var legacy = Object.FindFirstObjectByType<WeaponLegacyChoicePresenter>();
            legacy.Open(new WeaponLegacyChoiceState("frost_flask", "서리병", new[]
            {
                new WeaponLegacyChoiceView(WeaponLegacyPathId.FrostMist, "빙무", "서리 안개",
                    "넓은 둔화", "직접 피해 감소", null),
                new WeaponLegacyChoiceView(WeaponLegacyPathId.FrostShatter, "파쇄", "착지 폭발",
                    "연쇄 파쇄", "지속시간 감소", null)
            }), _ => true);
            var replacement = Object.FindFirstObjectByType<WeaponReplacementPresenter>();
            replacement.Open(new WeaponReplacementState("frost_flask", "서리병", new[]
            {
                new WeaponReplacementChoiceView("one", "환도 비검", 4, "월식", null),
                new WeaponReplacementChoiceView("two", "각궁", 3, "관일", null),
                new WeaponReplacementChoiceView("three", "주술 부적", 2, "", null),
                new WeaponReplacementChoiceView("four", "벽력탄", 1, "", null)
            }), _ => true, () => true);
            yield return null;

            try
            {
                foreach (var requested in PortraitUiMetrics.ValidationResolutions)
                {
                    Screen.SetResolution(requested.x, requested.y, FullScreenMode.Windowed);
                    var frames = 0;
                    while ((Screen.width != requested.x || Screen.height != requested.y) && frames++ < 12)
                        yield return null;
                    var applied = new Vector2Int(Screen.width, Screen.height) == requested;
                    Assert.That(applied || Application.isBatchMode, Is.True,
                        "Screen.SetResolution must apply outside Unity batchmode.");
                    if (!applied)
                    {
                        var simulatedCanvas = PortraitUiMetrics.CanvasSizeFor(requested);
                        Assert.That(simulatedCanvas.x, Is.GreaterThan(0f));
                    }

                    var actual = applied ? new Vector2(Screen.width, Screen.height) : PortraitUiMetrics.CanvasSizeFor(requested);
                    bootstrap.ApplySafeArea(new Rect(0f, actual.y * .04f, actual.x, actual.y * .925f), actual);
                    Canvas.ForceUpdateCanvases();
                    AssertBounds(bootstrap, "Vitals", bootstrap.SafeAreaContainer);
                    AssertBounds(bootstrap, "Pause Button", bootstrap.SafeAreaContainer);
                    AssertBounds(bootstrap, "Weapon Rack", bootstrap.SafeAreaContainer);
                    AssertBounds(bootstrap, "Weapon Slot 0", bootstrap.SafeAreaContainer);
                    AssertBounds(bootstrap, "Upgrade Card 0", bootstrap.ModalSafeAreaContainer);
                    AssertBounds(bootstrap, "Confirm Reward", bootstrap.ModalSafeAreaContainer);
                    AssertBounds(bootstrap, "Weapon Appraisal Panel", bootstrap.ModalSafeAreaContainer);
                    AssertBounds(bootstrap, "Legacy Choice 0", bootstrap.ModalSafeAreaContainer);
                    AssertBounds(bootstrap, "Legacy Choice 1", bootstrap.ModalSafeAreaContainer);
                    for (var index = 0; index < 4; index++)
                        AssertBounds(bootstrap, "Replacement Choice " + index, bootstrap.ModalSafeAreaContainer);
                    AssertBounds(bootstrap, "Cancel Replacement", bootstrap.ModalSafeAreaContainer);
                }
            }
            finally
            {
                appraisal.HideImmediately();
                reward.HideImmediately();
                legacy.CloseImmediately();
                replacement.CloseImmediately();
            }

            foreach (var text in bootstrap.GetComponentsInChildren<Component>(true))
            {
                if (text.GetType().Name != "TextMeshProUGUI") continue;
                var font = text.GetType().GetProperty("font").GetValue(text) as Object;
                CollectionAssert.Contains(new[]
                {
                    "MaruBuri-Regular-Dynamic SDF",
                    "MaruBuri-SemiBold-Dynamic SDF",
                    "ChosunGs-Dynamic SDF"
                }, font.name, text.name);
                if (text.name == "Heading")
                    Assert.That(font.name, Is.EqualTo("ChosunGs-Dynamic SDF"), text.name);
            }
        }

        [UnityTest]
        public IEnumerator Portrait_modal_cards_and_confirmation_stay_inside_the_safe_area()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            yield return null;

            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            var upgrade = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            upgrade.BuildForTests();
            Canvas.ForceUpdateCanvases();
            foreach (var card in upgrade.GetComponentsInChildren<Button>(true))
            {
                Assert.That(card.GetComponent<RectTransform>().rect.width, Is.LessThanOrEqualTo(936f), card.name);
                Assert.That(IsContained(bootstrap.ModalSafeAreaContainer, card.GetComponent<RectTransform>()), Is.True,
                    card.name);
            }

            var appraisal = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            Assert.That(appraisal.transform.IsChildOf(bootstrap.ModalSafeAreaContainer), Is.True);

            var reward = Object.FindFirstObjectByType<RewardRevealPresenter>();
            reward.Play(new ProgressionRewardEvent("boots", null, 1, ProgressionRewardKind.Support,
                "능력 강화", "+12%", null));
            yield return null;
            var confirm = System.Array.Find(reward.GetComponentsInChildren<Button>(true),
                button => button.name == "Confirm Reward");
            Assert.That(IsContained(bootstrap.ModalSafeAreaContainer, confirm.GetComponent<RectTransform>()), Is.True);

            var heading = System.Array.Find(upgrade.GetComponentsInChildren<Component>(true),
                component => component.name == "Heading" && component.GetType().Name == "TextMeshProUGUI");
            Assert.That(heading.GetType().GetProperty("text").GetValue(heading), Is.EqualTo("강화를 선택하세요"));
        }

        [Test]
        public void Controlled_rect_transform_harness_keeps_production_hud_rack_and_cards_inside_every_target_safe_area()
        {
            foreach (var target in PortraitUiMetrics.ValidationResolutions)
            {
                RectTransform root = null;
                GameObject cameraObject = null;
                RenderTexture targetTexture = null;
                try
                {
                root = new GameObject("Virtual Canvas " + target, typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler)).GetComponent<RectTransform>();
                var canvas = root.GetComponent<Canvas>();
                cameraObject = new GameObject("Virtual Canvas Camera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                targetTexture = new RenderTexture(target.x, target.y, 16);
                camera.targetTexture = targetTexture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                var harnessScaler = root.GetComponent<CanvasScaler>();
                harnessScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                harnessScaler.referenceResolution = PortraitUiMetrics.ReferenceResolution;
                harnessScaler.matchWidthOrHeight = .5f;
                root.pivot = new Vector2(.5f, .5f);
                root.localScale = Vector3.one;
                Canvas.ForceUpdateCanvases();
                Assert.That(Vector2.Distance(canvas.renderingDisplaySize, target), Is.LessThan(.1f));
                var safe = Child("Safe", root);
                safe.anchorMin = new Vector2(0f, .04f);
                safe.anchorMax = new Vector2(1f, .965f);
                safe.offsetMin = safe.offsetMax = Vector2.zero;
                Assert.That(root.rect.width, Is.EqualTo(PortraitUiMetrics.CanvasSizeFor(target).x).Within(.1f));
                Assert.That(root.rect.height, Is.EqualTo(PortraitUiMetrics.CanvasSizeFor(target).y).Within(.1f));
                Assert.That(safe.rect.width, Is.EqualTo(root.rect.width).Within(.1f));
                var modal = Child("Modal Safe", root);
                modal.anchorMin = safe.anchorMin;
                modal.anchorMax = safe.anchorMax;
                modal.offsetMin = modal.offsetMax = Vector2.zero;
                var hud = Child("HUD", safe).gameObject.AddComponent<CombatHudPresenter>();
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(safe);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(modal);
                hud.Build(); hud.ApplyPortraitLayout();
                var rack = Child("Rack", safe).gameObject.AddComponent<WeaponRackPresenter>();
                var weapons = new WeaponSlotView[8];
                for (var index = 0; index < weapons.Length; index++) weapons[index] =
                    new WeaponSlotView("weapon" + index, "Weapon", 1, null);
                rack.Render(weapons); rack.ApplyPortraitLayout();
                var upgrade = Child("Upgrade", modal).gameObject.AddComponent<UpgradeChoicePresenter>();
                upgrade.BuildForTests(); upgrade.ApplyPortraitLayout();
                var reward = Child("Reward", modal).gameObject.AddComponent<RewardRevealPresenter>();
                reward.Play(new ProgressionRewardEvent("boots", null, 1, ProgressionRewardKind.Support,
                    "능력 강화", "+12%", null));
                var affix = Child("Affix", modal).gameObject.AddComponent<WeaponAffixRevealPresenter>();
                var catalog = Resources.Load<JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
                Assert.That(catalog, Is.Not.Null); Assert.That(catalog.HasRequiredUiSprites, Is.True);
                affix.SetCatalogForTests(catalog);
                affix.Play(TestModel());
                Canvas.ForceUpdateCanvases();
                hud.ApplyPortraitLayout(); rack.ApplyPortraitLayout(); upgrade.ApplyPortraitLayout();
                AssertBounds(root, "Vitals", safe);
                AssertBounds(root, "HUD", safe);
                AssertBounds(root, "Boss Health", safe);
                AssertBounds(root, "Rack", safe);
                for (var index = 0; index < 8; index++) AssertBounds(root, "Weapon Slot " + index, safe);
                for (var index = 0; index < 3; index++)
                {
                    var card = AssertBounds(root, "Upgrade Card " + index, modal);
                    Assert.That(card.rect.width, Is.LessThanOrEqualTo(936f));
                }
                AssertButtonsContained(rack.transform, safe, 8);
                AssertButtonsContained(upgrade.transform, modal, 3);
                AssertButtonsContained(reward.transform, modal, 1);
                AssertBounds(root, "Confirm Reward", modal);
                var panel = AssertBounds(root, "Weapon Appraisal Panel", modal);
                Assert.That(panel.rect.width, Is.LessThanOrEqualTo(936f));
                Assert.That(panel.rect.height, Is.LessThanOrEqualTo(1320f));
                AssertBounds(root, "Confirm Result", modal);
                AssertButtonsContained(affix.transform, modal, 1);
                }
                finally
                {
                    if (targetTexture != null)
                    {
                        if (targetTexture.IsCreated()) targetTexture.Release();
                        Object.DestroyImmediate(targetTexture);
                    }
                    if (cameraObject != null) Object.DestroyImmediate(cameraObject);
                    if (root != null) Object.DestroyImmediate(root.gameObject);
                }
            }
        }

        private static bool IsContained(RectTransform parent, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            for (var index = 0; index < corners.Length; index++)
            {
                var point = parent.InverseTransformPoint(corners[index]);
                var rect = parent.rect;
                if (point.x < rect.xMin - .1f || point.x > rect.xMax + .1f ||
                    point.y < rect.yMin - .1f || point.y > rect.yMax + .1f) return false;
            }
            return true;
        }

        private static void AssertBounds(FirstPlayableUiBootstrap bootstrap, string name, RectTransform owner)
        {
            var child = bootstrap.GetComponentsInChildren<RectTransform>(true);
            var match = System.Array.Find(child, rect => rect.name == name);
            Assert.That(match, Is.Not.Null, name);
            Assert.That(IsContained(owner, match), Is.True, name);
        }

        private static RectTransform Child(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(parent, false);
            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.one;
            child.offsetMin = child.offsetMax = Vector2.zero;
            return child;
        }

        private static RectTransform AssertBounds(RectTransform root, string name, RectTransform owner)
        {
            var match = System.Array.Find(root.GetComponentsInChildren<RectTransform>(true), rect => rect.name == name);
            Assert.That(match, Is.Not.Null, name);
            Assert.That(IsContained(owner, match), Is.True, name);
            return match;
        }

        private static void AssertButtonsContained(Transform root, RectTransform owner, int minimum)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Length, Is.GreaterThanOrEqualTo(minimum));
            foreach (var button in buttons) Assert.That(IsContained(owner, button.GetComponent<RectTransform>()), Is.True,
                button.name);
        }

        private static JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset TestCatalog()
        {
            var texture = new Texture2D(2, 2); var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
            var catalog = ScriptableObject.CreateInstance<JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset>();
            catalog.SetSlotKitForTests(sprite, sprite, sprite, sprite, sprite); catalog.SetAppraisalKitForImport(sprite, sprite, sprite, sprite);
            return catalog;
        }

        private static WeaponAppraisalViewModel TestModel()
        {
            var result = new JoseonHunter.Domain.Progression.WeaponAffixRollResult(new JoseonHunter.Domain.Progression.WeaponAffixRoll(JoseonHunter.Domain.Progression.WeaponAffixStat.Damage, JoseonHunter.Domain.Progression.WeaponAffixTier.High, 23d), System.Array.Empty<JoseonHunter.Domain.Progression.WeaponPotentialId>());
            return WeaponAppraisalViewModel.From(new ProgressionRewardEvent("hwando_flying_blade", "hwando_flying_blade", 2, ProgressionRewardKind.WeaponLevel, "Hwando", "Level 2", null, result), new WeaponSlotView("hwando_flying_blade", "Hwando", 2, null));
        }
    }
}
