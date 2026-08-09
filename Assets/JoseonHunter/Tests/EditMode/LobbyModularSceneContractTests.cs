using System;
using System.Linq;
using System.Reflection;
using JoseonHunter.Editor.Scenes;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class LobbyModularSceneContractTests
    {
        private const string ScenePath = "Assets/JoseonHunter/Scenes/Lobby.unity";
        private const string ModulesPath = "Assets/JoseonHunter/Prefabs/UI/Lobby/Modules";

        [Test]
        public void LobbySceneIsDirectAuthoredModularComposition()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            Assert.That(roots.Select(root => root.name), Is.EquivalentTo(new[] { "Lobby Camera", "Lobby Canvas", "EventSystem" }));
            var canvas = roots.Single(root => root.name == "Lobby Canvas");
            var safeArea = canvas.transform.Find("Safe Area");
            Assert.That(safeArea, Is.Not.Null);
            Assert.That(safeArea.Cast<Transform>().Select(child => child.name), Is.EquivalentTo(new[]
            {
                "Background", "Common Header", "Home Page", "Training Page", "Patrol Page", "Research Page", "Settings Overlay"
            }));
            Assert.That(canvas.transform.Find("Bottom Navigation"), Is.Null);
            Assert.That(canvas.GetComponentsInChildren<LobbyRootView>(true), Has.Length.EqualTo(1));
            Assert.That(canvas.GetComponentsInChildren<LobbyBootstrap>(true), Has.Length.EqualTo(1));
            Assert.That(canvas.GetComponentsInChildren<LobbyNavigationPresenter>(true), Has.Length.EqualTo(1));
            Assert.That(canvas.GetComponentInChildren<LobbyRootView>(true).HasRequiredBindings, Is.True);
            Assert.That(safeArea.Find("Home Page").GetComponentsInChildren<LobbyMenuCardView>(true), Has.Length.EqualTo(3));
            Assert.That(safeArea.Find("Home Page").GetComponentsInChildren<TMPro.TMP_Text>(true)
                .Select(text => text.text), Has.None.Contains("환도 비검 연구"));
            foreach (var page in new[] { "Training Page", "Patrol Page", "Research Page" })
                Assert.That(safeArea.Find(page).GetComponentsInChildren<LobbyPageHeaderView>(true), Has.Length.EqualTo(1), page);
            foreach (var module in canvas.GetComponentsInChildren<LobbyMenuCardView>(true).Cast<Component>()
                         .Concat(canvas.GetComponentsInChildren<LobbyPageHeaderView>(true))
                         .Concat(canvas.GetComponentsInChildren<LobbyHeaderView>(true))
                         .Concat(canvas.GetComponentsInChildren<LobbyProgressBarView>(true))
                         .Concat(canvas.GetComponentsInChildren<LobbyDifficultyCardView>(true)))
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(module.gameObject), Does.StartWith(ModulesPath), module.name);
            Assert.That(roots.Single(root => root.name == "EventSystem").GetComponent<EventSystem>(), Is.Not.Null);
            Assert.That(roots.Single(root => root.name == "EventSystem").GetComponents<Component>()
                .Any(component => component.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule"), Is.True);
            Assert.That(canvas.GetComponentsInChildren<AudioListener>(true), Has.Length.LessThanOrEqualTo(1));
            Assert.That(canvas.GetComponentsInChildren<Transform>(true)
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)), Is.Zero);
        }

        [Test]
        public void AuthoredLobbyStartsAtHomeWithDistinctBoundedModules()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().Single(root => root.name == "Lobby Canvas");
            var safeArea = canvas.transform.Find("Safe Area");
            var pages = new[] { "Home Page", "Training Page", "Patrol Page", "Research Page" }
                .Select(name => safeArea.Find(name).gameObject).ToArray();
            Assert.That(pages.Count(page => page.activeSelf), Is.EqualTo(1));
            Assert.That(pages[0].activeSelf, Is.True);

            var cards = pages[0].GetComponentsInChildren<LobbyMenuCardView>(true);
            Assert.That(cards.Select(card => card.Title.text), Is.EquivalentTo(new[] { "수련", "출전", "연구" }));
            Assert.That(cards.Select(card => card.Icon.sprite), Is.All.Not.Null);
            Assert.That(cards.Select(card => card.GetComponent<RectTransform>().anchorMin.x), Is.Unique);
            Assert.That(canvas.GetComponentInChildren<LobbyHomePresenter>(true).GetComponent<LobbyHomeView>(), Is.Not.Null);
            foreach (var page in pages.Skip(1))
            {
                var header = page.GetComponentInChildren<LobbyPageHeaderView>(true);
                Assert.That(header.Title.text, Is.Not.Empty);
                Assert.That(header.Icon.sprite, Is.Not.Null);
                Assert.That(header.GetComponent<RectTransform>().anchorMin.y, Is.GreaterThan(.7f));
            }
            var commonHeader = safeArea.Find("Common Header").GetComponent<RectTransform>();
            Assert.That(commonHeader.anchorMin.y, Is.GreaterThan(.85f));
            Assert.That(commonHeader.anchorMax.y, Is.GreaterThan(.9f));
            Assert.That(safeArea.Find("Common Header/Header/Settings Button"), Is.Not.Null);
            Assert.That(safeArea.Find("Settings Overlay").GetComponent<LobbyAudioSettingsView>(), Is.Not.Null);
            Assert.That(safeArea.Find("Settings Overlay").GetComponent<LobbyAudioSettingsView>().HasRequiredBindings, Is.True);
            var settingsOverlay = safeArea.Find("Settings Overlay");
            Assert.That(settingsOverlay.GetComponentsInChildren<Button>(true)
                .Where(button => button.name == "Close Audio Settings").ToArray(), Has.Length.EqualTo(1));
            var audioSliders = settingsOverlay.GetComponentsInChildren<Slider>(true)
                .OrderBy(slider => slider.name).ToArray();
            Assert.That(audioSliders.Select(slider => slider.name), Is.EqualTo(new[]
                { "Music Volume Slider", "Sound Effect Volume Slider" }));
            foreach (var slider in audioSliders)
            {
                Assert.That(slider.fillRect, Is.Not.Null, slider.name);
                Assert.That(slider.handleRect, Is.Not.Null, slider.name);
                Assert.That(slider.targetGraphic, Is.SameAs(slider.handleRect.GetComponent<Image>()), slider.name);
                Assert.That(slider.handleRect.sizeDelta.x, Is.InRange(12f, 24f), slider.name);
                Assert.That(slider.handleRect.sizeDelta.y, Is.InRange(12f, 28f), slider.name);
                var handleArea = (RectTransform)slider.handleRect.parent;
                Assert.That(handleArea.offsetMin.x, Is.GreaterThanOrEqualTo(12f), slider.name);
                Assert.That(handleArea.offsetMax.x, Is.LessThanOrEqualTo(-12f), slider.name);
            }
            Assert.That(safeArea.Cast<Transform>().Select(child => child.name).ToArray(), Is.EqualTo(new[]
                { "Background", "Common Header", "Home Page", "Training Page", "Patrol Page", "Research Page", "Settings Overlay" }));
            foreach (var card in cards)
            {
                Assert.That(card.Title.fontSize, Is.GreaterThanOrEqualTo(22f));
                Assert.That(card.Description.fontSize, Is.GreaterThanOrEqualTo(16f));
                Assert.That(card.Icon.rectTransform.anchorMin.y, Is.GreaterThan(.5f));
            }
            Assert.That(canvas.GetComponentsInChildren<HorizontalLayoutGroup>(true), Is.Empty);
            Assert.That(canvas.GetComponentsInChildren<Button>(true).Where(button =>
                button.name.Contains("Navigation")).ToArray(), Is.Empty);
        }

        [Test]
        public void AuthoredDetailPagesUseOnlyConnectedModulesWithTouchSafeTrainingActions()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().Single(root => root.name == "Lobby Canvas");
            var safe = canvas.transform.Find("Safe Area");
            var trainingPage = safe.Find("Training Page");
            var researchPage = safe.Find("Research Page");
            var training = trainingPage.GetComponent<TrainingPageView>();
            var research = researchPage.GetComponent<ResearchPageView>();

            Assert.That(training, Is.Not.Null);
            Assert.That(training.HasRequiredBindings, Is.True);
            Assert.That(training.Rows, Has.Length.EqualTo(6));
            Assert.That(training.Rows, Is.Unique);
            Assert.That(training.Rows.Select(row => row.Button), Is.Unique);
            Assert.That(trainingPage.GetComponentsInChildren<LobbyTrainingRowView>(true), Has.Length.EqualTo(6));
            Assert.That(FindNamed(trainingPage, "Training Grid"), Is.Empty);

            var canvasScaler = canvas.GetComponent<CanvasScaler>();
            var scaleTo720 = 720f / canvasScaler.referenceResolution.x;
            var expectedRows = new[] { (.71f, .80f), (.615f, .705f), (.52f, .61f), (.425f, .515f), (.33f, .42f), (.235f, .325f) };
            for (var index = 0; index < training.Rows.Length; index++)
            {
                var rowRect = training.Rows[index].GetComponent<RectTransform>();
                Assert.That(rowRect.anchorMin.y, Is.EqualTo(expectedRows[index].Item1).Within(.0001f), training.Rows[index].name);
                Assert.That(rowRect.anchorMax.y, Is.EqualTo(expectedRows[index].Item2).Within(.0001f), training.Rows[index].name);
                Assert.That(rowRect.rect.height * scaleTo720, Is.GreaterThanOrEqualTo(64f), training.Rows[index].name);
            }
            foreach (var action in new[] { training.PurchaseButton, training.ResetButton })
            {
                var actionRect = action.GetComponent<RectTransform>();
                Assert.That(actionRect.anchorMin.y, Is.EqualTo(.05f).Within(.0001f), action.name);
                Assert.That(actionRect.anchorMax.y, Is.EqualTo(.15f).Within(.0001f), action.name);
                Assert.That(actionRect.rect.height * scaleTo720, Is.GreaterThanOrEqualTo(64f), action.name);
            }
            var summary = FindNamed(trainingPage, "Training Summary Backplate").Single().GetComponent<RectTransform>();
            var feedback = training.FeedbackText.rectTransform;
            Assert.That(summary.anchorMin.y, Is.EqualTo(.16f).Within(.0001f));
            Assert.That(summary.anchorMax.y, Is.EqualTo(.23f).Within(.0001f));
            var layoutRects = training.Rows.Select(row => (Name: row.name, Rect: row.GetComponent<RectTransform>()))
                .Concat(new[]
                {
                    (training.PurchaseButton.name, training.PurchaseButton.GetComponent<RectTransform>()),
                    (training.ResetButton.name, training.ResetButton.GetComponent<RectTransform>()),
                    ("Training Summary Backplate", summary),
                    ("Training Feedback", feedback)
                }).ToArray();
            for (var first = 0; first < layoutRects.Length; first++)
            {
                for (var second = first + 1; second < layoutRects.Length; second++)
                {
                    if ((layoutRects[first].Item1 == "Training Summary Backplate" && layoutRects[second].Item1 == "Training Feedback") ||
                        (layoutRects[first].Item1 == "Training Feedback" && layoutRects[second].Item1 == "Training Summary Backplate")) continue;
                    AssertWorldRectsDoNotOverlap(layoutRects[first].Item2, layoutRects[second].Item2,
                        layoutRects[first].Item1, layoutRects[second].Item1);
                }
            }
            Assert.That(training.Rows.First().GetComponent<RectTransform>().anchorMax.y,
                Is.LessThan(trainingPage.GetComponentInChildren<LobbyPageHeaderView>(true).GetComponent<RectTransform>().anchorMin.y));
            foreach (var row in training.Rows)
            {
                var image = row.Button.GetComponent<Image>();
                Assert.That(image.sprite, Is.Not.Null, row.name);
                Assert.That(image.sprite.name, Is.EqualTo("small_item_frame"), row.name);
            }

            var patrolPage = safe.Find("Patrol Page");
            var patrol = patrolPage.GetComponent<PatrolPageView>();
            Assert.That(patrol, Is.Not.Null);
            Assert.That(patrol.HasRequiredBindings, Is.True);
            var difficultyModules = patrolPage.GetComponentsInChildren<LobbyDifficultyCardView>(true);
            var selectorModules = patrolPage.GetComponentsInChildren<LobbyWeaponSelectorCardView>(true);
            Assert.That(difficultyModules, Has.Length.EqualTo(3));
            Assert.That(selectorModules, Has.Length.EqualTo(1));
            Assert.That(difficultyModules, Is.EquivalentTo(new[]
                { patrol.NormalDifficulty, patrol.OmenDifficulty, patrol.GreatOmenDifficulty }));
            Assert.That(selectorModules[0], Is.SameAs(patrol.WeaponSelector));
            foreach (var module in difficultyModules.Cast<Component>().Concat(selectorModules))
            {
                Assert.That(module.transform.parent, Is.SameAs(patrolPage));
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(module.gameObject),
                    Does.StartWith(ModulesPath), module.name);
            }
            foreach (var name in new[] { "Difficulty Normal", "Difficulty Omen", "Difficulty Great Omen" })
                Assert.That(FindNamed(patrolPage, name)
                    .Where(item => item.GetComponent<LobbyDifficultyCardView>() == null).ToArray(), Is.Empty, name);
            Assert.That(FindNamed(patrolPage, "Starting Weapon Selector")
                .Where(item => item.GetComponent<LobbyWeaponSelectorCardView>() == null).ToArray(), Is.Empty);

            AssertAnchors(patrol.NormalDifficulty.GetComponent<RectTransform>(), .06f, .405f, .353f, .495f);
            AssertAnchors(patrol.OmenDifficulty.GetComponent<RectTransform>(), .353f, .405f, .647f, .495f);
            AssertAnchors(patrol.GreatOmenDifficulty.GetComponent<RectTransform>(), .647f, .405f, .94f, .495f);
            AssertAnchors(patrol.WeaponSelector.GetComponent<RectTransform>(), .06f, .275f, .94f, .365f);
            var plaque = FindNamed(patrolPage, "Stage Plaque").Single().GetComponent<RectTransform>();
            AssertAnchors(plaque, .04f, .735f, .96f, .815f);
            foreach (var control in new[]
                     { patrol.StageName.rectTransform, patrol.PreviousStageButton.GetComponent<RectTransform>(), patrol.NextStageButton.GetComponent<RectTransform>() })
            {
                Assert.That(control.anchorMin.x, Is.GreaterThanOrEqualTo(plaque.anchorMin.x));
                Assert.That(control.anchorMax.x, Is.LessThanOrEqualTo(plaque.anchorMax.x));
                Assert.That(control.anchorMin.y, Is.GreaterThanOrEqualTo(plaque.anchorMin.y));
                Assert.That(control.anchorMax.y, Is.LessThanOrEqualTo(plaque.anchorMax.y));
            }
            Assert.That(plaque.anchorMax.y,
                Is.LessThan(patrol.PageHeader.GetComponent<RectTransform>().anchorMin.y));

            Assert.That(research, Is.Not.Null);
            Assert.That(research.HasRequiredBindings, Is.True);
            Assert.That(research.WeaponSelectors, Has.Length.EqualTo(8));
            Assert.That(research.WeaponSelectors, Is.Unique);
            Assert.That(research.WeaponSelectors.Select(selector => selector.Button), Is.Unique);
            Assert.That(research.Rows, Has.Length.EqualTo(3));
            Assert.That(research.Rows, Is.Unique);
            Assert.That(research.Rows.Select(row => row.ActionButton), Is.Unique);
            Assert.That(research.MasteryProgress, Is.Not.Null);
            var fill = research.MasteryProgress.GetComponentsInChildren<Image>(true)
                .Single(image => image.name == "Fill");
            Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
            foreach (var legacyRoot in new[]
                     { "Research Progress Backplate", "Weapon Grid", "Style Card 0", "Style Card 1", "Style Card 2" })
                Assert.That(FindNamed(researchPage, legacyRoot), Is.Empty, legacyRoot);
        }

        private static Transform[] FindNamed(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();

        private static void AssertAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(minX, minY)), rect.name + " anchor minimum");
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(maxX, maxY)), rect.name + " anchor maximum");
        }

        private static void AssertWorldRectsDoNotOverlap(RectTransform first, RectTransform second, string firstName, string secondName)
        {
            var firstCorners = new Vector3[4];
            var secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners);
            second.GetWorldCorners(secondCorners);
            var firstRect = Rect.MinMaxRect(firstCorners[0].x, firstCorners[0].y, firstCorners[2].x, firstCorners[2].y);
            var secondRect = Rect.MinMaxRect(secondCorners[0].x, secondCorners[0].y, secondCorners[2].x, secondCorners[2].y);
            Assert.That(firstRect.Overlaps(secondRect), Is.False, firstName + " overlaps " + secondName + ".");
        }

        [Test]
        public void RebuildRefusesDirtyLobbyWithoutMutatingLoadedScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var stale in scene.GetRootGameObjects().Where(root => root.name.StartsWith("Task7 Dirty Marker")))
                UnityEngine.Object.DestroyImmediate(stale);
            var marker = new GameObject("Task7 Dirty Marker " + Guid.NewGuid());
            SceneManager.MoveGameObjectToScene(marker, scene);
            var roots = scene.GetRootGameObjects();
            var markerTransform = marker.transform;
            EditorSceneManager.MarkSceneDirty(scene);

            try
            {
                Assert.Throws<InvalidOperationException>(LobbySceneBuilder.Build);
                Assert.That(scene.isDirty, Is.True);
                Assert.That(marker.transform, Is.SameAs(markerTransform));
                Assert.That(scene.GetRootGameObjects(), Is.EquivalentTo(roots));
            }
            finally { UnityEngine.Object.DestroyImmediate(marker); }
        }

        [Test]
        public void RepairPreflightRefusesIncompleteAuthoredCloneWithoutMutation()
        {
            var temporaryPath = $"Assets/JoseonHunter/Scenes/__LobbyRepairPreflight_{Guid.NewGuid():N}.unity";
            Assert.That(AssetDatabase.CopyAsset(ScenePath, temporaryPath), Is.True);
            var temporary = EditorSceneManager.OpenScene(temporaryPath, OpenSceneMode.Single);

            try
            {
                var canvas = temporary.GetRootGameObjects().Single(root => root.name == "Lobby Canvas");
                var safe = canvas.transform.Find("Safe Area");
                safe.Find("Settings Overlay").SetSiblingIndex(0);
                var bootstrap = canvas.GetComponent<LobbyBootstrap>();
                var bootstrapObject = new SerializedObject(bootstrap);
                bootstrapObject.FindProperty("weaponCatalog").objectReferenceValue = null;
                bootstrapObject.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(safe.Find("Research Page").gameObject);
                Assert.That(EditorSceneManager.SaveScene(temporary, temporaryPath), Is.True);
                Assert.That(temporary.isDirty, Is.False);

                var remainingOrder = safe.Cast<Transform>().Select(item => item.name).ToArray();
                var remainingActive = safe.Cast<Transform>().Select(item => item.gameObject.activeSelf).ToArray();
                var hierarchy = SnapshotHierarchy(canvas);
                var components = canvas.GetComponentsInChildren<Component>(true)
                    .Where(component => component != null).Select(component => component.GetEntityId()).ToArray();
                var repair = typeof(LobbySceneBuilder).GetMethod("RepairAuthoredHierarchy", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(repair, Is.Not.Null);
                var thrown = Assert.Throws<TargetInvocationException>(() => repair.Invoke(null, new object[] { canvas }));
                Assert.That(thrown.InnerException, Is.TypeOf<InvalidOperationException>());
                Assert.That(thrown.InnerException.Message, Does.Contain("Research Page"));
                Assert.That(safe.Cast<Transform>().Select(item => item.name), Is.EqualTo(remainingOrder));
                Assert.That(safe.Cast<Transform>().Select(item => item.gameObject.activeSelf), Is.EqualTo(remainingActive));
                Assert.That(bootstrapObject.FindProperty("weaponCatalog").objectReferenceValue, Is.Null);
                Assert.That(SnapshotHierarchy(canvas), Is.EqualTo(hierarchy));
                Assert.That(canvas.GetComponentsInChildren<Component>(true)
                    .Where(component => component != null).Select(component => component.GetEntityId()), Is.EqualTo(components));
                Assert.That(temporary.isDirty, Is.False, "repair preflight failure must preserve a clean scene");
            }
            finally
            {
                if (temporary.IsValid() && temporary.isLoaded)
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                AssetDatabase.DeleteAsset(temporaryPath);
            }
        }

        private static string SnapshotHierarchy(GameObject root) => string.Join("\n",
            root.GetComponentsInChildren<Transform>(true).Select(item => string.Join("|",
                item.GetEntityId(), item.parent == null ? "root" : item.parent.GetEntityId().ToString(),
                item.GetSiblingIndex(), item.localPosition, item.localRotation, item.localScale,
                item.gameObject.activeSelf)));

        [Test]
        public void RebuildAuthoredLobbyIsIdempotentAndKeepsHomeActive()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().Single(root => root.name == "Lobby Canvas");
            var cards = canvas.GetComponentsInChildren<LobbyMenuCardView>(true).Select(card => card.gameObject).ToArray();
            LobbySceneBuilder.Build();
            Assert.That(canvas.GetComponentsInChildren<LobbyMenuCardView>(true).Select(card => card.gameObject), Is.EquivalentTo(cards));
            var safe = canvas.transform.Find("Safe Area");
            Assert.That(safe.Find("Home Page").gameObject.activeSelf, Is.True);
            Assert.That(new[] { "Training Page", "Patrol Page", "Research Page" }.Count(name => safe.Find(name).gameObject.activeSelf), Is.Zero);
            Assert.That(canvas.GetComponentsInChildren<HorizontalLayoutGroup>(true), Is.Empty);
        }

        [Test]
        public void ValidateUsesLobbySceneCountsWhenGameplayIsAdditivelyLoaded()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene("Assets/JoseonHunter/Scenes/Gameplay.unity", OpenSceneMode.Additive);
            Assert.DoesNotThrow(LobbySceneBuilder.Validate);
        }

        [Test]
        public void ComposeAuthoredHierarchyAuthorsCompleteSettingsViewForLegacyLikeScene()
        {
            var lobby = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var sourceCanvas = lobby.GetRootGameObjects().Single(root => root.name == "Lobby Canvas");
            var canvas = UnityEngine.Object.Instantiate(sourceCanvas);
            SceneManager.MoveGameObjectToScene(canvas, temporary);

            try
            {
                var safe = canvas.transform.Find("Safe Area");
                var background = safe.Find("Background");
                background.SetParent(canvas.transform, false);
                background.name = "Lobby Background";
                var header = safe.Find("Common Header");
                header.SetParent(safe, false);
                header.name = "Header";
                var stage = new GameObject("Stage Content", typeof(RectTransform)).transform;
                stage.SetParent(safe, false);
                foreach (var pageName in new[] { "Patrol Page", "Training Page", "Research Page" })
                    safe.Find(pageName).SetParent(stage, false);
                stage.Find("Patrol Page").name = "Patrol Panel";
                stage.Find("Training Page").name = "Common Training Panel";
                stage.Find("Research Page").name = "Weapon Research Panel";
                UnityEngine.Object.DestroyImmediate(stage.Find("Patrol Panel").GetComponent<PatrolPageView>());
                UnityEngine.Object.DestroyImmediate(stage.Find("Common Training Panel").GetComponent<TrainingPageView>());
                UnityEngine.Object.DestroyImmediate(stage.Find("Weapon Research Panel").GetComponent<ResearchPageView>());
                UnityEngine.Object.DestroyImmediate(safe.Find("Home Page").gameObject);
                UnityEngine.Object.DestroyImmediate(canvas.GetComponent<LobbyRootView>());

                var settings = safe.Find("Settings Overlay");
                settings.name = "Audio Settings Overlay";
                UnityEngine.Object.DestroyImmediate(settings.GetComponent<LobbyAudioSettingsView>());
                var dialog = settings.GetComponentsInChildren<RectTransform>(true)
                    .Single(item => item.name == "Audio Settings Panel");
                while (dialog.childCount > 0) UnityEngine.Object.DestroyImmediate(dialog.GetChild(0).gameObject);
                settings.gameObject.AddComponent<AudioSettingsPresenter>();

                var compose = typeof(LobbySceneBuilder).GetMethod("ComposeAuthoredHierarchy",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(compose, Is.Not.Null);
                compose.Invoke(null, new object[] { canvas });

                var overlay = safe.Find("Settings Overlay");
                var view = overlay.GetComponent<LobbyAudioSettingsView>();
                Assert.That(view, Is.Not.Null);
                Assert.That(view.HasRequiredBindings, Is.True);
                Assert.That(overlay.GetComponentsInChildren<Slider>(true), Has.Length.EqualTo(2));
                Assert.That(overlay.GetComponentsInChildren<Button>(true)
                    .Where(button => button.name == "Close Audio Settings").ToArray(), Has.Length.EqualTo(1));
            }
            finally
            {
                if (temporary.IsValid() && temporary.isLoaded) EditorSceneManager.CloseScene(temporary, true);
            }
        }
    }
}
