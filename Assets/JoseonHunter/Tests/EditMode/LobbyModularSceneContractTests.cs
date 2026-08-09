using System;
using System.Linq;
using JoseonHunter.Editor.Scenes;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
    }
}
