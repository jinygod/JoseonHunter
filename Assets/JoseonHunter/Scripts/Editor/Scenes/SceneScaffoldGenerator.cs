using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Editor.Scenes
{
    public static class SceneScaffoldGenerator
    {
        public const string BootstrapScenePath = "Assets/JoseonHunter/Scenes/Bootstrap.unity";
        public const string LobbyScenePath = "Assets/JoseonHunter/Scenes/Lobby.unity";
        public const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";

        private static readonly string[] ScenePaths =
        {
            BootstrapScenePath,
            LobbyScenePath,
            GameplayScenePath
        };

        [MenuItem("JoseonHunter/Setup/Generate Foundation Scenes")]
        public static void Generate()
        {
            RefuseDirtyOpenFoundationScenes();
            EnsureScenesFolder();

            foreach (var scenePath in ScenePaths)
            {
                GenerateScene(scenePath, scenePath == GameplayScenePath);
                Debug.Log($"Generated foundation scene: {scenePath}");
            }

            EditorBuildSettings.scenes = ScenePaths
                .Select(scenePath => new EditorBuildSettingsScene(scenePath, true))
                .ToArray();
        }

        private static void RefuseDirtyOpenFoundationScenes()
        {
            foreach (var scenePath in ScenePaths)
            {
                var openScene = SceneManager.GetSceneByPath(scenePath);
                if (openScene.IsValid() && openScene.isLoaded && openScene.isDirty)
                {
                    throw new InvalidOperationException(
                        $"Cannot overwrite dirty open foundation scene: {scenePath}");
                }
            }
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/JoseonHunter/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/JoseonHunter", "Scenes");
            }
        }

        private static void GenerateScene(string scenePath, bool isGameplay)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var sceneRoot = new GameObject("SceneRoot");
            SceneManager.MoveGameObjectToScene(sceneRoot, scene);

            if (isGameplay)
            {
                new GameObject("World").transform.SetParent(sceneRoot.transform);
                new GameObject("UI").transform.SetParent(sceneRoot.transform);
            }

            EditorSceneManager.SaveScene(scene, scenePath);
        }
    }
}
